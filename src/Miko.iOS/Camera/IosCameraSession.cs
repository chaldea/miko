using AVFoundation;
using CoreFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;
using Microsoft.Extensions.Logging;
using Miko.iOS.Video;
using Miko.Platform.Camera;
using Miko.Platform.Video;
using UIKit;

namespace Miko.iOS.Camera;

/// <summary>
/// One <c>AVCaptureSession</c> on one camera.
///
/// <para><b>Frames.</b> <c>AVCaptureVideoDataOutput</c> delivers NV12 (<c>420v</c>, the camera's native
/// format, video range — what <see cref="Nv12FrameComposer"/> expects) <c>CVPixelBuffer</c>s on a capture
/// queue. The buffers go to the same frame source as video (<see cref="IosVideoFrameSource"/>), which maps
/// both planes to Metal textures through <c>CVMetalTextureCache</c> on the render thread, without copying;
/// the analysis pump reads the luma plane directly.</para>
///
/// <para><b>Orientation.</b> The capture connections rotate the buffers to the current interface
/// orientation (<c>videoOrientation</c>), so preview, analysis frames and stills are upright as delivered
/// and <see cref="Rotation"/> is always 0 — AVFoundation knows how each camera is mounted, which a
/// hand-computed rotation would have to guess. Buffers are never mirrored; <see cref="IsMirrored"/> tells
/// the engine to mirror front-camera previews. The interface orientation is read on the main thread, when
/// the engine paints.</para>
///
/// <para><b>Threads.</b> Configuration, start and stop run on a serial session queue (<c>startRunning</c>
/// blocks); frames arrive on a capture queue; control methods are called on the main thread and only record
/// the intent before handing the work to the session queue. Events are raised on any of them.</para>
/// </summary>
internal sealed class IosCameraSession : ICameraSession
{
    private const float MaxZoomCap = 10f;

    private readonly CameraSessionOptions _options;
    private readonly ILogger _logger;
    private readonly IosVideoFrameSource _frames = new();
    private readonly CameraAnalysisPump _pump = new();
    private readonly DispatchQueue _sessionQueue = new("miko.camera.session");
    private readonly DispatchQueue _captureQueue = new("miko.camera.frames");
    private readonly object _photoLock = new();
    private readonly HashSet<PhotoDelegate> _photoDelegates = [];

    // Session-queue state.
    private AVCaptureSession? _session;
    private AVCaptureDevice? _device;
    private AVCaptureDeviceInput? _input;
    private AVCaptureVideoDataOutput? _videoOutput;
    private AVCapturePhotoOutput? _photoOutput;
    private FrameDelegate? _frameDelegate;
    private NSObject[] _observers = [];

    private volatile CameraSessionState _state = CameraSessionState.Idle;
    private CameraErrorKind? _errorKind; // written before the volatile _state
    private volatile string? _errorMessage;
    private volatile CameraDeviceInfo? _deviceInfo;
    private volatile bool _paused;
    private volatile bool _disposed;
    private volatile bool _awaitingFirstFrame = true;
    private volatile bool _hasTorch;
    private volatile bool _torchOn;
    private int _frameWidth, _frameHeight;
    private float _zoom = 1, _minZoom = 1, _maxZoom = 1;

    // Interface orientation, read on the main thread and applied to the connections on the session queue.
    private AVCaptureVideoOrientation _orientation = AVCaptureVideoOrientation.Portrait;
    private long _orientationCheckedAt;

    public IosCameraSession(CameraSessionOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
        _pump.AnalyzerFailed += ex => _logger.LogWarning(ex, "Camera frame analyzer failed");
    }

    public CameraSessionState State => _state;
    public CameraErrorKind? ErrorKind => _state == CameraSessionState.Error ? _errorKind : null;
    public string? ErrorMessage => _state == CameraSessionState.Error ? _errorMessage : null;
    public CameraDeviceInfo? Device => _deviceInfo;
    public int FrameWidth => Volatile.Read(ref _frameWidth);
    public int FrameHeight => Volatile.Read(ref _frameHeight);

    /// <summary>
    /// Always 0: the connections deliver upright buffers. Reading it (the engine does, every painted frame,
    /// on the main thread) is also when a change of interface orientation is noticed and passed on.
    /// </summary>
    public int Rotation
    {
        get
        {
            TrackInterfaceOrientation();
            return 0;
        }
    }

    public bool IsMirrored => _deviceInfo?.Facing == CameraFacing.Front || (_deviceInfo == null && _options.Facing == CameraFacing.Front);
    public IVideoFrameSource FrameSource => _frames;
    public bool HasTorch => _hasTorch;
    public bool IsTorchOn => _torchOn;
    public float MinZoom => Volatile.Read(ref _minZoom);
    public float MaxZoom => Volatile.Read(ref _maxZoom);
    public float Zoom => Volatile.Read(ref _zoom);

    public event Action<CameraSessionEvent>? Event;

    // ---- Start --------------------------------------------------------------------------------

    /// <summary>
    /// Called on the main thread by the backend, which hands the session to the engine before the engine
    /// subscribes to <see cref="Event"/>. The work therefore starts from the main queue, after the current
    /// frame, and continues off it: find the camera (no permission prompt on a device without one, like
    /// the simulator), ask for permission, then configure and start.
    /// </summary>
    internal void Start()
    {
        _state = CameraSessionState.Starting;
        _orientation = CurrentInterfaceOrientation() ?? AVCaptureVideoOrientation.Portrait;
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            if (!_disposed) _ = Task.Run(StartAsync);
        });
    }

    private async Task StartAsync()
    {
        try
        {
            var device = IosCameraDevices.Resolve(_options);
            if (device == null)
            {
                Fail(CameraErrorKind.NoDevice, _options.DeviceId is { } id
                    ? $"The camera '{id}' is not connected."
                    : $"This device has no {_options.Facing.ToString().ToLowerInvariant()} camera.");
                return;
            }
            _deviceInfo = IosCameraDevices.Describe(device);

            var status = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);
            if (status == AVAuthorizationStatus.NotDetermined)
            {
                bool granted = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video).ConfigureAwait(false);
                status = granted ? AVAuthorizationStatus.Authorized : AVAuthorizationStatus.Denied;
            }
            if (_disposed) return;
            if (status != AVAuthorizationStatus.Authorized)
            {
                Fail(CameraErrorKind.PermissionDenied, status == AVAuthorizationStatus.Restricted
                    ? "Camera access is restricted on this device."
                    : "Camera access was denied. Allow it in Settings.");
                return;
            }
            _sessionQueue.DispatchAsync(() => Configure(device));
        }
        catch (Exception ex)
        {
            Fail(CameraErrorKind.Failed, ex.Message, ex);
        }
    }

    /// <summary>Builds the capture graph and starts it. Session queue.</summary>
    private void Configure(AVCaptureDevice device)
    {
        if (_disposed) return;
        try
        {
            var input = AVCaptureDeviceInput.FromDevice(device, out NSError? inputError);
            if (input == null)
            {
                Fail(CameraErrorKind.Unavailable, inputError?.LocalizedDescription ?? "The camera could not be opened.");
                return;
            }

            var session = new AVCaptureSession();
            session.BeginConfiguration();
            session.SessionPreset = PresetFor(session, _options.Resolution);
            if (!session.CanAddInput(input))
            {
                session.CommitConfiguration();
                input.Dispose();
                session.Dispose();
                Fail(CameraErrorKind.Unavailable, "The camera cannot be added to a capture session.");
                return;
            }
            session.AddInput(input);

            var videoOutput = new AVCaptureVideoDataOutput
            {
                AlwaysDiscardsLateVideoFrames = true,
                WeakVideoSettings = new NSDictionary(
                    CVPixelBuffer.PixelFormatTypeKey,
                    NSNumber.FromUInt32((uint)CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange)),
            };
            var frameDelegate = new FrameDelegate(this);
            videoOutput.SetSampleBufferDelegate(frameDelegate, _captureQueue);
            if (session.CanAddOutput(videoOutput)) session.AddOutput(videoOutput);

            var photoOutput = new AVCapturePhotoOutput();
            if (session.CanAddOutput(photoOutput)) session.AddOutput(photoOutput);
            else
            {
                photoOutput.Dispose();
                photoOutput = null;
            }

            _session = session;
            _device = device;
            _input = input;
            _videoOutput = videoOutput;
            _photoOutput = photoOutput;
            _frameDelegate = frameDelegate;
            ApplyOrientation();
            session.CommitConfiguration();

            _hasTorch = device.HasTorch && device.IsTorchModeSupported(AVCaptureTorchMode.On);
            float min = (float)device.MinAvailableVideoZoomFactor;
            float max = Math.Max(min, (float)Math.Min(device.MaxAvailableVideoZoomFactor, MaxZoomCap));
            Volatile.Write(ref _minZoom, min);
            Volatile.Write(ref _maxZoom, max);
            Volatile.Write(ref _zoom, Math.Clamp(Volatile.Read(ref _zoom), min, max));

            _observers =
            [
                AVCaptureSession.Notifications.ObserveWasInterrupted(session, OnInterrupted),
                AVCaptureSession.Notifications.ObserveInterruptionEnded(session, OnInterruptionEnded),
                AVCaptureSession.Notifications.ObserveRuntimeError(session, OnRuntimeError),
            ];

            if (!_paused) StartRunning();
            else _state = CameraSessionState.Paused;
        }
        catch (Exception ex)
        {
            Fail(CameraErrorKind.Failed, ex.Message, ex);
        }
    }

    /// <summary>The closest preset the device supports, falling back towards smaller ones. Session queue.</summary>
    private static NSString PresetFor(AVCaptureSession session, CameraResolution resolution)
    {
        NSString[] candidates = resolution switch
        {
            CameraResolution.Low => [AVCaptureSession.Preset640x480, AVCaptureSession.PresetMedium],
            CameraResolution.Medium => [AVCaptureSession.Preset1280x720, AVCaptureSession.Preset640x480],
            _ => [AVCaptureSession.Preset1920x1080, AVCaptureSession.Preset1280x720, AVCaptureSession.Preset640x480],
        };
        foreach (var preset in candidates)
        {
            if (session.CanSetSessionPreset(preset)) return preset;
        }
        return AVCaptureSession.PresetHigh;
    }

    /// <summary>Starts the frames and re-applies torch and zoom (stopping a session turns the torch off). Session queue.</summary>
    private void StartRunning()
    {
        if (_session is not { } session || _disposed) return;
        _awaitingFirstFrame = true;
        if (_state != CameraSessionState.Error) _state = CameraSessionState.Starting;
        session.StartRunning();
        ApplyZoom();
        ApplyTorch();
    }

    // ---- Frames -------------------------------------------------------------------------------

    /// <summary>One preview frame. Capture queue; must return quickly, or AVFoundation drops frames.</summary>
    private void OnFrame(CMSampleBuffer sampleBuffer)
    {
        if (_disposed || _paused) return;
        if (sampleBuffer.GetImageBuffer() is not CVPixelBuffer buffer) return;

        int width = (int)buffer.Width, height = (int)buffer.Height;
        Volatile.Write(ref _frameWidth, width);
        Volatile.Write(ref _frameHeight, height);
        var pts = sampleBuffer.PresentationTimeStamp;
        var timestamp = pts.IsInvalid ? TimeSpan.Zero : TimeSpan.FromSeconds(pts.Seconds);

        if (_pump.IsReady)
        {
            buffer.Lock(CVPixelBufferLock.ReadOnly);
            try
            {
                _pump.TryPost(buffer.GetBaseAddress(0), width, height, (int)buffer.GetBytesPerRowOfPlane(0),
                    0, IsMirrored, timestamp);
            }
            finally
            {
                buffer.Unlock(CVPixelBufferLock.ReadOnly);
            }
        }

        // The frame source owns the buffer from here (it holds its own retain on the pixel buffer).
        _frames.PushPixelBuffer(buffer, timestamp);

        if (_awaitingFirstFrame)
        {
            _awaitingFirstFrame = false;
            _state = CameraSessionState.Running;
            Event?.Invoke(new CameraSessionEvent.Started(width, height));
        }
        Event?.Invoke(CameraSessionEvent.FrameAvailable.Instance);
    }

    // ---- Orientation --------------------------------------------------------------------------

    /// <summary>Notices a change of interface orientation and rotates the connections. Main thread, throttled.</summary>
    private void TrackInterfaceOrientation()
    {
        if (_disposed || !NSThread.IsMain) return;
        long now = Environment.TickCount64;
        if (now - _orientationCheckedAt < 200) return;
        _orientationCheckedAt = now;
        if (CurrentInterfaceOrientation() is not { } orientation || orientation == _orientation) return;
        _orientation = orientation;
        _sessionQueue.DispatchAsync(() =>
        {
            if (_session is not { } session || _disposed) return;
            session.BeginConfiguration();
            ApplyOrientation();
            session.CommitConfiguration();
        });
    }

    /// <summary>The interface orientation of the foreground window scene. Main thread.</summary>
    private static AVCaptureVideoOrientation? CurrentInterfaceOrientation()
    {
        if (!NSThread.IsMain || !OperatingSystem.IsIOSVersionAtLeast(13)) return null;
        UIInterfaceOrientation? orientation = null;
        foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is not UIWindowScene windowScene) continue;
#pragma warning disable CA1422 // Obsolete on iOS 26 in favour of effectiveGeometry; still answers the same.
            orientation = windowScene.InterfaceOrientation;
#pragma warning restore CA1422
            if (windowScene.ActivationState == UISceneActivationState.ForegroundActive) break;
        }
        return orientation switch
        {
            UIInterfaceOrientation.Portrait => AVCaptureVideoOrientation.Portrait,
            UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
            UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
            UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
            _ => null,
        };
    }

    /// <summary>
    /// Rotates both connections to the interface orientation and turns mirroring off (the engine mirrors the
    /// front preview; photos are mirrored only on request). Session queue, inside a configuration block.
    /// <c>videoOrientation</c> is deprecated on iOS 17 in favour of <c>videoRotationAngle</c>, whose angles
    /// depend on how each camera is mounted; the orientation names the result instead, on every version.
    /// </summary>
    private void ApplyOrientation()
    {
        Align(_videoOutput?.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant()!));
        Align(_photoOutput?.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant()!));

        void Align(AVCaptureConnection? connection)
        {
            if (connection == null) return;
#pragma warning disable CA1422 // See above.
            if (connection.SupportsVideoOrientation) connection.VideoOrientation = _orientation;
#pragma warning restore CA1422
            if (connection.SupportsVideoMirroring)
            {
                connection.AutomaticallyAdjustsVideoMirroring = false;
                connection.VideoMirrored = false;
            }
        }
    }

    // ---- Controls -----------------------------------------------------------------------------

    public void SetTorch(bool on)
    {
        if (!_hasTorch || _disposed) return;
        _torchOn = on;
        _sessionQueue.DispatchAsync(ApplyTorch);
    }

    public void SetZoom(float zoom)
    {
        if (_disposed || !float.IsFinite(zoom)) return;
        Volatile.Write(ref _zoom, Math.Clamp(zoom, MinZoom, MaxZoom));
        _sessionQueue.DispatchAsync(ApplyZoom);
    }

    /// <summary>Session queue. The torch only lights while the session runs.</summary>
    private void ApplyTorch()
    {
        if (_device is not { } device || !_hasTorch || _session is not { Running: true }) return;
        var mode = _torchOn ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
        if (device.TorchMode == mode || (_torchOn && !device.TorchAvailable)) return;
        WithConfigurationLock(device, () => device.TorchMode = mode);
    }

    /// <summary>Session queue.</summary>
    private void ApplyZoom()
    {
        if (_device is not { } device) return;
        float zoom = Math.Clamp(Volatile.Read(ref _zoom), MinZoom, MaxZoom);
        if (Math.Abs(device.VideoZoomFactor - zoom) < 0.001) return;
        WithConfigurationLock(device, () => device.VideoZoomFactor = zoom);
    }

    private void WithConfigurationLock(AVCaptureDevice device, Action change)
    {
        if (!device.LockForConfiguration(out NSError? error))
        {
            _logger.LogWarning("Camera configuration lock failed: {Error}", error?.LocalizedDescription);
            return;
        }
        try
        {
            change();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Camera configuration failed");
        }
        finally
        {
            device.UnlockForConfiguration();
        }
    }

    public void Pause()
    {
        if (_disposed || _paused) return;
        _paused = true;
        if (_state is CameraSessionState.Running or CameraSessionState.Starting)
            _state = CameraSessionState.Paused;
        _sessionQueue.DispatchAsync(() =>
        {
            if (_session is { Running: true } session) session.StopRunning();
        });
        Event?.Invoke(new CameraSessionEvent.Stopped());
    }

    public void Resume()
    {
        if (_disposed || !_paused) return;
        _paused = false;
        if (_state == CameraSessionState.Paused) _state = CameraSessionState.Starting;
        // Not configured yet (still asking for permission): Configure starts it.
        _sessionQueue.DispatchAsync(StartRunning);
    }

    public void SetAnalyzer(ICameraFrameAnalyzer? analyzer) => _pump.Analyzer = analyzer;

    // ---- Stills -------------------------------------------------------------------------------

    public Task<CameraStill> CaptureStillAsync(CameraCaptureOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (_state != CameraSessionState.Running || _photoOutput is null)
            throw new InvalidOperationException("The camera is not running.");
        cancellationToken.ThrowIfCancellationRequested();

        var completion = new TaskCompletionSource<CameraStill>(TaskCreationOptions.RunContinuationsAsynchronously);
        // AVFoundation holds the delegate weakly: keep it alive until the photo is delivered.
        var photoDelegate = new PhotoDelegate(this, completion, IsMirrored);
        lock (_photoLock) _photoDelegates.Add(photoDelegate);
        if (cancellationToken.CanBeCanceled)
        {
            var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            completion.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        }

        _sessionQueue.DispatchAsync(() =>
        {
            try
            {
                if (_photoOutput is not { } output || _disposed || _session is not { Running: true })
                    throw new InvalidOperationException("The camera is not running.");
                var settings = AVCapturePhotoSettings.FromFormat(new NSDictionary<NSString, NSObject>(
                    AVVideo.CodecKey, new NSString("jpeg")));
                var flash = options.Flash switch
                {
                    CameraFlashMode.On => AVCaptureFlashMode.On,
                    CameraFlashMode.Auto => AVCaptureFlashMode.Auto,
                    _ => AVCaptureFlashMode.Off,
                };
                if (output.SupportedFlashModes.Contains(flash))
                    settings.FlashMode = flash;
                output.CapturePhoto(settings, photoDelegate);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
                ForgetPhotoDelegate(photoDelegate);
            }
        });
        return completion.Task;
    }

    private void ForgetPhotoDelegate(PhotoDelegate photoDelegate)
    {
        lock (_photoLock) _photoDelegates.Remove(photoDelegate);
    }

    // ---- Notifications ------------------------------------------------------------------------

    private void OnInterrupted(object? sender, NSNotificationEventArgs e)
    {
        if (_disposed || _paused) return;
        var code = (e.Notification.UserInfo?[AVCaptureSession.InterruptionReasonKey] as NSNumber)?.Int64Value;
        string reason = (AVCaptureSessionInterruptionReason?)code switch
        {
            AVCaptureSessionInterruptionReason.VideoDeviceInUseByAnotherClient => "Another app is using the camera.",
            AVCaptureSessionInterruptionReason.VideoDeviceNotAvailableWithMultipleForegroundApps => "The camera is unavailable while several apps share the screen.",
            AVCaptureSessionInterruptionReason.VideoDeviceNotAvailableDueToSystemPressure => "The camera was stopped to cool the device down.",
            _ => "The camera was interrupted.",
        };
        // AVFoundation resumes by itself when the interruption ends; the next frame reports Started again.
        _awaitingFirstFrame = true;
        Fail(CameraErrorKind.Unavailable, reason);
    }

    private void OnInterruptionEnded(object? sender, NSNotificationEventArgs e)
    {
        if (_disposed || _paused) return;
        _logger.LogInformation("Camera interruption ended");
    }

    private void OnRuntimeError(object? sender, AVCaptureSessionRuntimeErrorEventArgs e)
    {
        if (_disposed) return;
        var error = e.Error;
        _logger.LogWarning("Camera runtime error: {Error}", error?.LocalizedDescription);
        // The media server restarted: the session can be started again.
        if (error?.Code == (long)AVError.MediaServicesWereReset && !_paused)
        {
            _sessionQueue.DispatchAsync(StartRunning);
            return;
        }
        _awaitingFirstFrame = true;
        Fail(CameraErrorKind.Failed, error?.LocalizedDescription ?? "The camera stopped with an error.");
    }

    private void Fail(CameraErrorKind kind, string message, Exception? cause = null)
    {
        if (_disposed) return;
        _errorKind = kind;
        _errorMessage = message;
        _state = CameraSessionState.Error;
        if (cause != null) _logger.LogWarning(cause, "Camera error ({Kind}): {Message}", kind, message);
        else _logger.LogInformation("Camera error ({Kind}): {Message}", kind, message);
        Event?.Invoke(new CameraSessionEvent.Error(kind, message, cause));
    }

    // ---- Teardown -----------------------------------------------------------------------------

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pump.Analyzer = null;
        Event = null;
        PhotoDelegate[] pending;
        lock (_photoLock)
        {
            pending = _photoDelegates.ToArray();
            _photoDelegates.Clear();
        }
        foreach (var photoDelegate in pending)
            photoDelegate.Cancel();

        _sessionQueue.DispatchAsync(Teardown);
        // The texture cache belongs to the render thread, which is the caller (the engine releases
        // sessions while it lays out a frame on the main thread).
        _frames.Dispose();
    }

    /// <summary>Session queue.</summary>
    private void Teardown()
    {
        foreach (var observer in _observers) observer.Dispose();
        _observers = [];
        if (_session is { } session)
        {
            if (_device is { } device && _torchOn && session.Running)
                WithConfigurationLock(device, () => device.TorchMode = AVCaptureTorchMode.Off);
            if (session.Running) session.StopRunning();
            session.BeginConfiguration();
            if (_input != null) session.RemoveInput(_input);
            if (_videoOutput != null) session.RemoveOutput(_videoOutput);
            if (_photoOutput != null) session.RemoveOutput(_photoOutput);
            session.CommitConfiguration();
        }
        _videoOutput?.SetSampleBufferDelegate(null, null);
        _videoOutput?.Dispose();
        _photoOutput?.Dispose();
        _input?.Dispose();
        _frameDelegate?.Dispose();
        _session?.Dispose();
        _session = null;
        _videoOutput = null;
        _photoOutput = null;
        _input = null;
        _frameDelegate = null;
        _device = null;
    }

    // ---- Delegates ----------------------------------------------------------------------------

    private sealed class FrameDelegate(IosCameraSession owner) : AVCaptureVideoDataOutputSampleBufferDelegate
    {
        public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
        {
            try
            {
                owner.OnFrame(sampleBuffer);
            }
            catch (Exception ex)
            {
                owner._logger.LogWarning(ex, "Camera frame handling failed");
            }
            finally
            {
                // The capture pool holds only a few buffers: release the sample buffer now, not at the next GC.
                sampleBuffer.Dispose();
            }
        }
    }

    private sealed class PhotoDelegate(IosCameraSession owner, TaskCompletionSource<CameraStill> completion, bool mirrored)
        : AVCapturePhotoCaptureDelegate
    {
        public override void DidFinishProcessingPhoto(AVCapturePhotoOutput output, AVCapturePhoto photo, NSError? error)
        {
            try
            {
                if (error != null)
                {
                    completion.TrySetException(new InvalidOperationException(error.LocalizedDescription));
                    return;
                }
                using var data = photo.FileDataRepresentation;
                if (data == null || data.Length == 0)
                {
                    completion.TrySetException(new InvalidOperationException("The camera returned no photo data."));
                    return;
                }
                // The JPEG carries the orientation in its EXIF data; the core applies it.
                completion.TrySetResult(CameraStill.FromEncoded(data.ToArray(), 0, mirrored));
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }

        public override void DidFinishCapture(AVCapturePhotoOutput captureOutput, AVCaptureResolvedPhotoSettings resolvedSettings, NSError? error)
        {
            if (error != null)
                completion.TrySetException(new InvalidOperationException(error.LocalizedDescription));
            else
                completion.TrySetException(new InvalidOperationException("The camera finished without a photo."));
            owner.ForgetPhotoDelegate(this);
        }

        public void Cancel() => completion.TrySetException(new ObjectDisposedException(nameof(IosCameraSession)));
    }
}
