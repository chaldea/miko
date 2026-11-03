using System.Runtime.InteropServices;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Hardware.Display;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Microsoft.Extensions.Logging;
using Miko.Graphics;
using Miko.Platform.Camera;
using Miko.Platform.Video;
using AndroidImage = Android.Media.Image;
using Size = Android.Util.Size;

namespace Miko.Android.Camera;

/// <summary>
/// One Camera2 device: an OES preview stream (<see cref="AndroidCameraFrameSource"/>), a YUV stream for
/// analysis (in the repeating request only while an analyzer is installed) and a JPEG stream for stills.
/// <para>
/// Threads: every camera call runs on the session's own <c>miko-camera</c> looper, so the engine
/// (render thread) and the host's lifecycle callbacks (UI thread) only post to it. Analysis images
/// arrive on a second looper and are handed to a <see cref="CameraAnalysisPump"/>, which never blocks it.
/// Callbacks of a closed device carry an old generation and are ignored.
/// </para>
/// <para>
/// Lifecycle: start → (ask for the permission) → open → configure → repeating preview → first frame
/// = <see cref="CameraSessionState.Running"/>. <see cref="Pause"/> closes the device so other apps can
/// use it; <see cref="Resume"/> reopens it. The permission dialog is an Activity of its own, so the
/// host pauses (and the engine pauses this session) while it shows: the request survives that, and
/// the device opens on the resume that follows a grant.
/// </para>
/// </summary>
internal sealed class AndroidCameraSession : ICameraSession
{
    private const int CaptureTimeoutMs = 10_000;
    private const long MaxStillPixels = 12_600_000;

    private readonly CameraManager? _manager;
    private readonly Func<global::Android.App.Activity?> _activity;
    private readonly ILogger _logger;
    private readonly string? _cameraId;
    private readonly CameraFacing _facing;
    private readonly int _sensorOrientation;
    private readonly bool _hasFlash;
    private readonly bool _continuousAf;
    private readonly bool _zoomRatio;
    private readonly Rect? _activeArray;
    private readonly float _minZoom = 1, _maxZoom = 1;
    private readonly Size? _previewSize, _analysisSize, _stillSize;

    private readonly HandlerThread? _thread, _analysisThread;
    private readonly Handler? _handler, _analysisHandler;
    private readonly AndroidCameraFrameSource? _frames;
    private readonly ImageReader? _analysisReader, _stillReader;
    private readonly CameraAnalysisPump _pump = new();
    private readonly DisplayManager? _displays;
    private readonly DisplayListener? _displayListener;

    private readonly object _eventLock = new();
    private Action<CameraSessionEvent>? _event;
    private CameraSessionEvent? _unheardError;

    private readonly object _captureLock = new();
    private readonly List<TaskCompletionSource<CameraStill>> _captures = new();

    // Camera-thread state.
    private CameraDevice? _device;
    private CameraCaptureSession? _session;
    private int _generation;
    private bool _analysisInRequest;

    // Shared state.
    private volatile CameraSessionState _state = CameraSessionState.Starting;
    private volatile bool _paused, _disposed, _disposing, _permissionPending, _torch;
    private volatile int _displayRotation;
    private volatile float _zoom = 1;
    private CameraErrorKind? _errorKind;
    private string? _errorMessage;

    public AndroidCameraSession(
        CameraManager manager, string cameraId, CameraCharacteristics characteristics, CameraDeviceInfo device,
        CameraResolution resolution, Func<global::Android.App.Activity?> activity, ILogger logger)
    {
        _manager = manager;
        _activity = activity;
        _logger = logger;
        _cameraId = cameraId;
        Device = device;
        _facing = device.Facing;
        _sensorOrientation = GetInt(characteristics, CameraCharacteristics.SensorOrientation) ?? 0;
        _hasFlash = GetBool(characteristics, CameraCharacteristics.FlashInfoAvailable) ?? false;
        var afModes = GetIntArray(characteristics, CameraCharacteristics.ControlAfAvailableModes);
        _continuousAf = afModes?.Contains((int)ControlAFMode.ContinuousPicture) == true;

        if (OperatingSystem.IsAndroidVersionAtLeast(30)
            && characteristics.Get(CameraCharacteristics.ControlZoomRatioRange) is global::Android.Util.Range range
            && range.Lower is Java.Lang.Float lower && range.Upper is Java.Lang.Float upper)
        {
            _zoomRatio = true;
            _minZoom = lower.FloatValue();
            _maxZoom = upper.FloatValue();
        }
        else
        {
            _activeArray = characteristics.Get(CameraCharacteristics.SensorInfoActiveArraySize) as Rect;
            _maxZoom = Math.Max(1, GetFloat(characteristics, CameraCharacteristics.ScalerAvailableMaxDigitalZoom) ?? 1);
        }

        var map = characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap) as StreamConfigurationMap
            ?? throw new InvalidOperationException("The camera reports no stream configurations.");
        var (targetWidth, targetHeight) = resolution switch
        {
            CameraResolution.Low => (640, 480),
            CameraResolution.Medium => (1280, 720),
            _ => (1920, 1080),
        };
        _previewSize = ChooseSize(map.GetOutputSizes(Java.Lang.Class.FromType(typeof(SurfaceTexture))), targetWidth, targetHeight, null)
            ?? throw new InvalidOperationException("The camera has no preview sizes.");
        float aspect = _previewSize.Width / (float)_previewSize.Height;
        _analysisSize = ChooseSize(map.GetOutputSizes((int)ImageFormatType.Yuv420888),
            resolution == CameraResolution.Low ? 640 : 1280, resolution == CameraResolution.Low ? 480 : 720, aspect)
            ?? throw new InvalidOperationException("The camera has no YUV sizes.");
        _stillSize = ChooseStillSize(map.GetOutputSizes((int)ImageFormatType.Jpeg), aspect)
            ?? throw new InvalidOperationException("The camera has no JPEG sizes.");

        _thread = new HandlerThread("miko-camera");
        _thread.Start();
        _handler = new Handler(_thread.Looper!);
        _analysisThread = new HandlerThread("miko-camera-analysis");
        _analysisThread.Start();
        _analysisHandler = new Handler(_analysisThread.Looper!);

        _frames = new AndroidCameraFrameSource(_previewSize.Width, _previewSize.Height, _handler, () => Guard(OnPreviewFrame));
        _analysisReader = ImageReader.NewInstance(_analysisSize.Width, _analysisSize.Height, ImageFormatType.Yuv420888, 2);
        _analysisReader.SetOnImageAvailableListener(new ImageListener(OnAnalysisImage), _analysisHandler);
        _stillReader = ImageReader.NewInstance(_stillSize.Width, _stillSize.Height, ImageFormatType.Jpeg, 2);
        _stillReader.SetOnImageAvailableListener(new ImageListener(OnStillImage), _handler);
        _pump.AnalyzerFailed += ex => _logger.LogWarning(ex, "Camera frame analyzer failed");

        _displays = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.DisplayService) as DisplayManager;
        if (_displays != null)
        {
            _displayListener = new DisplayListener(this);
            _displays.RegisterDisplayListener(_displayListener, _handler);
        }
        ReadDisplayRotation();

        _logger.LogInformation("Camera {Id} ({Facing}): preview {Preview}, analysis {Analysis}, still {Still}, sensor {Sensor}°",
            cameraId, _facing, _previewSize, _analysisSize, _stillSize, _sensorOrientation);
        Post(Begin);
    }

    /// <summary>A session that failed before it could open anything (no camera, no service).</summary>
    public AndroidCameraSession(CameraErrorKind kind, string message, CameraDeviceInfo? device, ILogger logger)
    {
        _activity = static () => null;
        _logger = logger;
        Device = device;
        _facing = device?.Facing ?? CameraFacing.Back;
        Fail(kind, message, null);
    }

    public CameraSessionState State => _state;
    public CameraErrorKind? ErrorKind => _errorKind;
    public string? ErrorMessage => _errorMessage;
    public CameraDeviceInfo? Device { get; }
    public int FrameWidth => _previewSize?.Width ?? 0;
    public int FrameHeight => _previewSize?.Height ?? 0;

    // The texture is the raw buffer (see AndroidCameraFrameSource): turned upright by the sensor
    // orientation against the current display rotation, mirrored for front cameras — what the
    // SurfaceTexture transform would have done for a TextureView.
    public int Rotation => CameraOrientation.PreviewRotation(_sensorOrientation, _displayRotation, _facing);
    public bool IsMirrored => _facing == CameraFacing.Front;
    public IVideoFrameSource FrameSource => (IVideoFrameSource?)_frames ?? EmptyFrameSource.Instance;
    public bool HasTorch => _hasFlash;
    public bool IsTorchOn => _torch;
    public float MinZoom => _minZoom;
    public float MaxZoom => _maxZoom;
    public float Zoom => _zoom;

    /// <summary>Errors raised before anyone listened are replayed to the first subscriber.</summary>
    public event Action<CameraSessionEvent>? Event
    {
        add
        {
            CameraSessionEvent? replay;
            lock (_eventLock)
            {
                _event += value;
                replay = _unheardError;
                _unheardError = null;
            }
            if (replay != null) value?.Invoke(replay);
        }
        remove
        {
            lock (_eventLock) _event -= value;
        }
    }

    public void SetTorch(bool on)
    {
        if (!_hasFlash || _torch == on) return;
        _torch = on;
        Post(UpdateRepeatingRequest);
    }

    public void SetZoom(float zoom)
    {
        if (!float.IsFinite(zoom)) return;
        zoom = Math.Clamp(zoom, _minZoom, _maxZoom);
        if (Math.Abs(zoom - _zoom) < 0.0001f) return;
        _zoom = zoom;
        Post(UpdateRepeatingRequest);
    }

    public void Pause()
    {
        if (_disposed || _paused) return;
        _paused = true;
        if (_state is CameraSessionState.Running or CameraSessionState.Starting)
        {
            _state = CameraSessionState.Paused;
            Post(() =>
            {
                CloseDevice("The camera was paused.");
                Raise(new CameraSessionEvent.Stopped());
            });
        }
    }

    public void Resume()
    {
        if (_disposed || !_paused) return;
        _paused = false;
        if (_state != CameraSessionState.Paused) return;
        _state = CameraSessionState.Starting;
        Post(Open);
    }

    public void SetAnalyzer(ICameraFrameAnalyzer? analyzer)
    {
        _pump.Analyzer = analyzer;
        Post(() =>
        {
            if (_analysisInRequest != (analyzer != null)) UpdateRepeatingRequest();
        });
    }

    public Task<CameraStill> CaptureStillAsync(CameraCaptureOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (_state != CameraSessionState.Running || _handler == null)
            throw new InvalidOperationException("The camera is not running.");
        cancellationToken.ThrowIfCancellationRequested();

        var pending = new TaskCompletionSource<CameraStill>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (cancellationToken.CanBeCanceled)
        {
            var registration = cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
            pending.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        }
        // A still that never arrives (driver hang) must not leave the caller waiting forever.
        _handler.PostDelayed(() => pending.TrySetException(new TimeoutException("The camera did not deliver the photo.")), CaptureTimeoutMs);
        Post(() => SubmitCapture(pending, options));
        return pending.Task;
    }

    // ---- Camera thread ---------------------------------------------------------------------------

    private void Begin()
    {
        if (_disposed) return;
        if (AndroidCameraPermission.IsGranted())
        {
            Open();
            return;
        }

        var activity = _activity();
        if (activity == null)
        {
            Fail(CameraErrorKind.PermissionDenied,
                "Camera permission is not granted, and there is no Activity to ask for it.", null);
            return;
        }

        _permissionPending = true;
        _logger.LogInformation("Requesting the camera permission");
        _ = RequestPermissionAsync(activity);
    }

    private async Task RequestPermissionAsync(global::Android.App.Activity activity)
    {
        bool granted = await AndroidCameraPermission.RequestAsync(activity, () => _paused, () => _disposed).ConfigureAwait(false);
        Post(() =>
        {
            _permissionPending = false;
            if (_disposed) return;
            if (!granted)
            {
                Fail(CameraErrorKind.PermissionDenied, "Camera permission was denied.", null);
                return;
            }
            // Paused (the dialog's own pause, or the user left): Resume() opens the device.
            if (!_paused) Open();
        });
    }

    private void Open()
    {
        if (_disposed || _paused || _permissionPending || _device != null || _manager == null || _cameraId == null) return;
        if (_state == CameraSessionState.Error) return;
        int generation = ++_generation;
        _state = CameraSessionState.Starting;
        try
        {
            _manager.OpenCamera(_cameraId, new DeviceCallback(this, generation), _handler);
        }
        catch (CameraAccessException ex)
        {
            Fail(MapAccessError((int)ex.Reason), ex.Message ?? "The camera cannot be opened.", ex);
        }
        catch (Java.Lang.SecurityException ex)
        {
            Fail(CameraErrorKind.PermissionDenied, "Camera permission is not granted.", ex);
        }
        catch (Exception ex)
        {
            Fail(CameraErrorKind.Failed, ex.Message, ex);
        }
    }

    private void OnOpened(int generation, CameraDevice device)
    {
        if (generation != _generation || _disposed || _paused)
        {
            device.Close();
            return;
        }
        _device = device;
        try
        {
            CreateCaptureSession(device);
        }
        catch (Exception ex)
        {
            CloseDevice(null);
            Fail(ex is CameraAccessException access ? MapAccessError((int)access.Reason) : CameraErrorKind.Failed, ex.Message, ex);
        }
    }

    private void OnDeviceLost(int generation, CameraDevice device, int? error)
    {
        if (generation != _generation || !ReferenceEquals(device, _device))
        {
            device.Close();
            return;
        }
        CloseDevice(null);
        if (_disposed || _paused) return;
        if (error is { } code)
        {
            // CameraDevice.StateCallback: 1 in use, 2 max cameras in use, 3 disabled, 4 device, 5 service.
            var kind = code is 1 or 2 or 3 ? CameraErrorKind.Unavailable : CameraErrorKind.Failed;
            Fail(kind, $"The camera reported error {code}.", null);
        }
        else
        {
            Fail(CameraErrorKind.Unavailable, "The camera was disconnected or taken by another app.", null);
        }
    }

    private void CreateCaptureSession(CameraDevice device)
    {
        var surfaces = new List<Surface> { _frames!.Surface, _analysisReader!.Surface!, _stillReader!.Surface! };
        var callback = new SessionCallback(this, device);
        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            var outputs = surfaces.Select(s => new OutputConfiguration(s)).ToList();
            device.CreateCaptureSession(new SessionConfiguration(
                (int)SessionType.Regular, outputs, new HandlerExecutor(_handler!), callback));
        }
        else
        {
#pragma warning disable CA1422 // The SessionConfiguration overload only exists from API 28.
            device.CreateCaptureSession(surfaces, callback, _handler);
#pragma warning restore CA1422
        }
    }

    private void OnConfigured(CameraDevice device, CameraCaptureSession session)
    {
        if (!ReferenceEquals(device, _device) || _disposed || _paused)
        {
            session.Close();
            return;
        }
        _session = session;
        UpdateRepeatingRequest();
    }

    private void OnConfigureFailed(CameraDevice device)
    {
        if (!ReferenceEquals(device, _device)) return;
        CloseDevice(null);
        Fail(CameraErrorKind.Failed, "The camera streams could not be configured.", null);
    }

    /// <summary>(Re)issues the preview request with the current torch, zoom and analysis state.</summary>
    private void UpdateRepeatingRequest()
    {
        if (_session == null || _device == null || _disposed) return;
        try
        {
            using var builder = _device.CreateCaptureRequest(CameraTemplate.Preview);
            builder.AddTarget(_frames!.Surface);
            bool analyze = _pump.Analyzer != null;
            if (analyze) builder.AddTarget(_analysisReader!.Surface!);
            ApplyCommon(builder, null);
            _session.SetRepeatingRequest(builder.Build(), null, _handler);
            _analysisInRequest = analyze;
        }
        catch (Exception ex) when (ex is CameraAccessException or Java.Lang.IllegalStateException)
        {
            // The session was closed under us (pause, disconnect): its callbacks report the reason.
            _logger.LogDebug(ex, "Camera repeating request not issued");
        }
    }

    private void ApplyCommon(CaptureRequest.Builder builder, CameraFlashMode? stillFlash)
    {
        builder.Set(CaptureRequest.ControlMode!, Java.Lang.Integer.ValueOf((int)ControlMode.Auto));
        if (_continuousAf)
            builder.Set(CaptureRequest.ControlAfMode!, Java.Lang.Integer.ValueOf((int)ControlAFMode.ContinuousPicture));

        var aeMode = ControlAEMode.On;
        var flash = FlashMode.Off;
        if (_hasFlash)
        {
            if (stillFlash == CameraFlashMode.On) aeMode = ControlAEMode.OnAlwaysFlash;
            else if (stillFlash == CameraFlashMode.Auto) aeMode = ControlAEMode.OnAutoFlash;
            // The torch needs plain auto-exposure; a flash still overrides it for that one frame.
            if (_torch && aeMode == ControlAEMode.On) flash = FlashMode.Torch;
        }
        builder.Set(CaptureRequest.ControlAeMode!, Java.Lang.Integer.ValueOf((int)aeMode));
        builder.Set(CaptureRequest.FlashMode!, Java.Lang.Integer.ValueOf((int)flash));

        float zoom = _zoom;
        if (_zoomRatio && OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            builder.Set(CaptureRequest.ControlZoomRatio!, Java.Lang.Float.ValueOf(zoom));
        }
        else if (_activeArray is { } active && _maxZoom > 1)
        {
            int width = (int)(active.Width() / zoom), height = (int)(active.Height() / zoom);
            int left = (active.Width() - width) / 2, top = (active.Height() - height) / 2;
            builder.Set(CaptureRequest.ScalerCropRegion!, new Rect(left, top, left + width, top + height));
        }
    }

    private void SubmitCapture(TaskCompletionSource<CameraStill> pending, CameraCaptureOptions options)
    {
        if (pending.Task.IsCompleted) return;
        if (_session == null || _device == null || _paused || _disposed)
        {
            pending.TrySetException(new InvalidOperationException("The camera is not running."));
            return;
        }
        try
        {
            using var builder = _device.CreateCaptureRequest(CameraTemplate.StillCapture);
            builder.AddTarget(_stillReader!.Surface!);
            builder.AddTarget(_frames!.Surface);
            ApplyCommon(builder, options.Flash);
            // Same arithmetic as the preview: what is upright on screen is upright in the photo. Devices
            // either rotate the pixels or write the EXIF orientation; CameraPhotoEncoder handles both.
            builder.Set(CaptureRequest.JpegOrientation!, Java.Lang.Integer.ValueOf(Rotation));
            builder.Set(CaptureRequest.JpegQuality!, Java.Lang.Byte.ValueOf((sbyte)Math.Clamp(options.Quality, 1, 100)));
            lock (_captureLock) _captures.Add(pending);
            _session.Capture(builder.Build(), new StillCallback(this, pending), _handler);
        }
        catch (Exception ex)
        {
            lock (_captureLock) _captures.Remove(pending);
            pending.TrySetException(new InvalidOperationException("The photo could not be taken: " + ex.Message, ex));
        }
    }

    private void OnStillFailed(TaskCompletionSource<CameraStill> pending)
    {
        lock (_captureLock) _captures.Remove(pending);
        pending.TrySetException(new InvalidOperationException("The camera failed to take the photo."));
    }

    private void OnStillImage(ImageReader reader)
    {
        AndroidImage? image = null;
        try
        {
            image = reader.AcquireNextImage();
            if (image == null) return;
            byte[] data = CopyPlane(image);
            TaskCompletionSource<CameraStill>? pending = null;
            lock (_captureLock)
            {
                // One image per submitted capture, in order; a cancelled caller's image is dropped.
                if (_captures.Count > 0)
                {
                    pending = _captures[0];
                    _captures.RemoveAt(0);
                }
            }
            pending?.TrySetResult(CameraStill.FromEncoded(data, rotation: 0, isMirrored: IsMirrored));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Camera still could not be read");
        }
        finally
        {
            image?.Close();
            image?.Dispose();
        }
    }

    private static byte[] CopyPlane(AndroidImage image)
    {
        var planes = image.GetPlanes() ?? throw new InvalidOperationException("The photo has no data.");
        try
        {
            using var buffer = planes[0].Buffer ?? throw new InvalidOperationException("The photo has no data.");
            int length = buffer.Remaining();
            var data = new byte[length];
            Marshal.Copy(JNIEnv.GetDirectBufferAddress(buffer.Handle) + buffer.Position(), data, 0, length);
            return data;
        }
        finally
        {
            foreach (var plane in planes) plane.Dispose();
        }
    }

    private void OnPreviewFrame()
    {
        if (_disposed) return;
        if (_state == CameraSessionState.Starting && _session != null && !_paused)
        {
            _state = CameraSessionState.Running;
            Raise(new CameraSessionEvent.Started(FrameWidth, FrameHeight));
        }
        Raise(CameraSessionEvent.FrameAvailable.Instance);
    }

    /// <summary>Closes the capture session and the device (camera thread). Pending stills fail.</summary>
    private void CloseDevice(string? reason)
    {
        _generation++;
        try { _session?.Close(); } catch (Exception) { }
        _session?.Dispose();
        _session = null;
        try { _device?.Close(); } catch (Exception) { }
        _device?.Dispose();
        _device = null;
        _analysisInRequest = false;

        List<TaskCompletionSource<CameraStill>> pending;
        lock (_captureLock)
        {
            pending = [.. _captures];
            _captures.Clear();
        }
        foreach (var capture in pending)
            capture.TrySetException(new InvalidOperationException(reason ?? "The camera was closed."));
    }

    // ---- Analysis thread -------------------------------------------------------------------------

    private void OnAnalysisImage(ImageReader reader)
    {
        AndroidImage? image = null;
        try
        {
            // Always taken and closed, analyzed or not: an unread image holds one of the two buffers.
            image = reader.AcquireLatestImage();
            if (image == null || !_pump.IsReady || _paused || _disposed) return;
            var planes = image.GetPlanes();
            if (planes == null || planes.Length == 0) return;
            try
            {
                var luma = planes[0];
                using var buffer = luma.Buffer;
                if (buffer == null) return;
                _pump.TryPost(JNIEnv.GetDirectBufferAddress(buffer.Handle), image.Width, image.Height, luma.RowStride,
                    Rotation, IsMirrored, TimeSpan.FromTicks(image.Timestamp / 100));
            }
            finally
            {
                foreach (var plane in planes) plane.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Camera analysis frame dropped");
        }
        finally
        {
            image?.Close();
            image?.Dispose();
        }
    }

    // ---- Shared ----------------------------------------------------------------------------------

    private void ReadDisplayRotation()
    {
        var rotation = _displays?.GetDisplay(Display.DefaultDisplay)?.Rotation ?? SurfaceOrientation.Rotation0;
        _displayRotation = rotation switch
        {
            SurfaceOrientation.Rotation90 => 90,
            SurfaceOrientation.Rotation180 => 180,
            SurfaceOrientation.Rotation270 => 270,
            _ => 0,
        };
    }

    private void Post(Action action)
    {
        if (_disposed || _handler == null) return;
        _handler.Post(() => Guard(action));
    }

    /// <summary>
    /// Runs a looper callback. An exception escaping into the Java looper would take the process
    /// down; here it fails the session instead.
    /// </summary>
    private void Guard(Action action) => Guard(action, skipWhenDisposed: true);

    /// <param name="skipWhenDisposed">
    /// False for device callbacks: a device that opens after Dispose still has to be closed.
    /// </param>
    private void Guard(Action action, bool skipWhenDisposed)
    {
        if (skipWhenDisposed && _disposed) return;
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Fail(CameraErrorKind.Failed, ex.Message, ex);
        }
    }

    private void Fail(CameraErrorKind kind, string message, Exception? cause)
    {
        if (_disposed) return;
        _errorKind = kind;
        _errorMessage = message;
        _state = CameraSessionState.Error;
        _logger.LogWarning(cause, "Camera error ({Kind}): {Message}", kind, message);
        Raise(new CameraSessionEvent.Error(kind, message, cause));
    }

    private void Raise(CameraSessionEvent value)
    {
        Action<CameraSessionEvent>? handlers;
        lock (_eventLock)
        {
            handlers = _event;
            if (handlers == null && value is CameraSessionEvent.Error) _unheardError = value;
        }
        try
        {
            handlers?.Invoke(value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Camera event handler failed");
        }
    }

    private static CameraErrorKind MapAccessError(int reason)
        // CameraAccessException: 1 disabled, 2 disconnected, 3 error, 4 in use, 5 max cameras in use.
        => reason is 1 or 2 or 4 or 5 ? CameraErrorKind.Unavailable : CameraErrorKind.Failed;

    public void Dispose()
    {
        if (_disposed || _disposing) return;
        _disposing = true;
        var handler = _handler;
        if (handler != null)
        {
            // Close on the camera thread (it owns the device) and wait briefly, so the preview surface
            // is not released under a running stream.
            using var closed = new ManualResetEventSlim();
            handler.Post(() =>
            {
                try
                {
                    bool open = _device != null;
                    CloseDevice("The camera was closed.");
                    if (_displayListener != null) _displays?.UnregisterDisplayListener(_displayListener);
                    // Camera2 reports onClosed on this looper after Close() returns; quitting before it
                    // arrives logs "sending message to a Handler on a dead thread". OnDeviceClosed quits;
                    // the delayed call covers a device that never reports back.
                    if (open) handler.PostDelayed(QuitThreads, 2000);
                    else QuitThreads();
                }
                finally
                {
                    closed.Set();
                }
            });
            if (Looper.MyLooper() != handler.Looper) closed.Wait(TimeSpan.FromSeconds(1));
        }
        else
        {
            QuitThreads();
        }
        _disposed = true;
        _pump.Analyzer = null;
        lock (_eventLock) _event = null;

        _frames?.Dispose();
        try { _analysisReader?.Close(); } catch (Exception) { }
        try { _stillReader?.Close(); } catch (Exception) { }
    }

    private int _threadsQuit;

    private void QuitThreads()
    {
        if (Interlocked.Exchange(ref _threadsQuit, 1) != 0) return;
        _thread?.QuitSafely();
        _analysisThread?.QuitSafely();
    }

    /// <summary>A device finished closing (camera thread).</summary>
    private void OnDeviceClosed()
    {
        if (_disposing) QuitThreads();
    }

    // ---- Selection helpers -----------------------------------------------------------------------

    /// <summary>
    /// The largest size within the target area, preferring the target's aspect ratio (or
    /// <paramref name="aspect"/>); the smallest size when none fits.
    /// </summary>
    internal static Size? ChooseSize(Size[]? sizes, int targetWidth, int targetHeight, float? aspect)
    {
        if (sizes == null || sizes.Length == 0) return null;
        float wanted = aspect ?? targetWidth / (float)targetHeight;
        long area = (long)targetWidth * targetHeight;
        bool Matches(Size s) => Math.Abs(s.Width / (float)s.Height - wanted) < 0.03f;
        var fitting = sizes.Where(s => (long)s.Width * s.Height <= area && s.Width >= s.Height).ToList();
        return fitting.Where(Matches).MaxBy(s => (long)s.Width * s.Height)
            ?? fitting.MaxBy(s => (long)s.Width * s.Height)
            ?? sizes.MinBy(s => (long)s.Width * s.Height);
    }

    /// <summary>The largest JPEG size up to ~12 MP, preferring the preview's aspect ratio (so the photo frames what was shown).</summary>
    internal static Size? ChooseStillSize(Size[]? sizes, float aspect)
    {
        if (sizes == null || sizes.Length == 0) return null;
        var capped = sizes.Where(s => (long)s.Width * s.Height <= MaxStillPixels).ToList();
        return capped.Where(s => Math.Abs(s.Width / (float)s.Height - aspect) < 0.03f).MaxBy(s => (long)s.Width * s.Height)
            ?? capped.MaxBy(s => (long)s.Width * s.Height)
            ?? sizes.MinBy(s => (long)s.Width * s.Height);
    }

    internal static int? GetInt(CameraCharacteristics characteristics, CameraCharacteristics.Key? key)
        => key != null && characteristics.Get(key) is Java.Lang.Integer value ? value.IntValue() : null;

    private static float? GetFloat(CameraCharacteristics characteristics, CameraCharacteristics.Key? key)
        => key != null && characteristics.Get(key) is Java.Lang.Float value ? value.FloatValue() : null;

    private static bool? GetBool(CameraCharacteristics characteristics, CameraCharacteristics.Key? key)
        => key != null && characteristics.Get(key) is Java.Lang.Boolean value ? value.BooleanValue() : null;

    private static int[]? GetIntArray(CameraCharacteristics characteristics, CameraCharacteristics.Key? key)
    {
        if (key == null || characteristics.Get(key) is not { } value) return null;
        return JNIEnv.GetArray<int>(value.Handle);
    }

    // ---- Callbacks -------------------------------------------------------------------------------

    private sealed class DeviceCallback(AndroidCameraSession owner, int generation) : CameraDevice.StateCallback
    {
        public override void OnOpened(CameraDevice camera) => owner.Guard(() => owner.OnOpened(generation, camera), skipWhenDisposed: false);
        public override void OnDisconnected(CameraDevice camera) => owner.Guard(() => owner.OnDeviceLost(generation, camera, null), skipWhenDisposed: false);
        public override void OnError(CameraDevice camera, CameraError error) => owner.Guard(() => owner.OnDeviceLost(generation, camera, (int)error), skipWhenDisposed: false);
        public override void OnClosed(CameraDevice camera) => owner.OnDeviceClosed();
    }

    private sealed class SessionCallback(AndroidCameraSession owner, CameraDevice device) : CameraCaptureSession.StateCallback
    {
        public override void OnConfigured(CameraCaptureSession session) => owner.Guard(() => owner.OnConfigured(device, session), skipWhenDisposed: false);
        public override void OnConfigureFailed(CameraCaptureSession session) => owner.Guard(() => owner.OnConfigureFailed(device));
    }

    private sealed class StillCallback(AndroidCameraSession owner, TaskCompletionSource<CameraStill> pending) : CameraCaptureSession.CaptureCallback
    {
        public override void OnCaptureFailed(CameraCaptureSession session, CaptureRequest request, CaptureFailure failure)
            => owner.OnStillFailed(pending);
    }

    private sealed class ImageListener(Action<ImageReader> handler) : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        public void OnImageAvailable(ImageReader? reader)
        {
            if (reader != null) handler(reader);
        }
    }

    private sealed class DisplayListener(AndroidCameraSession owner) : Java.Lang.Object, DisplayManager.IDisplayListener
    {
        public void OnDisplayAdded(int displayId) { }
        public void OnDisplayRemoved(int displayId) { }
        public void OnDisplayChanged(int displayId)
        {
            if (displayId == Display.DefaultDisplay) owner.ReadDisplayRotation();
        }
    }

    private sealed class HandlerExecutor(Handler handler) : Java.Lang.Object, Java.Util.Concurrent.IExecutor
    {
        public void Execute(Java.Lang.IRunnable? command)
        {
            if (command != null) handler.Post(command);
        }
    }

    private sealed class EmptyFrameSource : IVideoFrameSource
    {
        public static EmptyFrameSource Instance { get; } = new();
        public Miko.Graphics.Image? AcquireCurrentFrame(GraphicsDevice device) => null;
        public void ReleaseCurrentFrame() { }
    }
}
