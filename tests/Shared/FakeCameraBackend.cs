using Miko.Graphics;
using Miko.Platform.Camera;
using Miko.Platform.Video;

namespace Miko.Testing.Camera;

/// <summary>
/// A camera backend for tests: sessions start only when the test says so, frames are pixel buffers the
/// test supplies, analysis frames go through a real <see cref="CameraAnalysisPump"/>. Public API only,
/// so the component tests can use it as well as the engine's white-box tests.
/// </summary>
public sealed class FakeCameraBackend : ICameraBackend
{
    public List<FakeCameraSession> Sessions { get; } = new();

    public List<CameraDeviceInfo> Devices { get; } =
    [
        new("back-0", "Back Camera", CameraFacing.Back),
        new("front-1", "Front Camera", CameraFacing.Front),
    ];

    /// <summary>Thrown by <see cref="CreateSession"/> when set.</summary>
    public Exception? CreateFailure { get; set; }

    /// <summary>Start every new session right away with a frame of this size.</summary>
    public (int Width, int Height)? AutoStart { get; set; }

    public int Rotation { get; set; }

    public IReadOnlyList<CameraDeviceInfo> GetDevices() => Devices;

    public ICameraSession CreateSession(CameraSessionOptions options)
    {
        if (CreateFailure is { } failure) throw failure;
        var device = options.DeviceId is { } id
            ? Devices.First(d => d.Id == id)
            : Devices.FirstOrDefault(d => d.Facing == options.Facing) ?? Devices[0];
        var session = new FakeCameraSession(options, device) { Rotation = Rotation };
        Sessions.Add(session);
        if (AutoStart is { } size) session.Start(size.Width, size.Height);
        return session;
    }
}

public sealed class FakeCameraSession : ICameraSession
{
    private readonly FakeFrameSource _frames = new();

    public FakeCameraSession(CameraSessionOptions options, CameraDeviceInfo device)
    {
        Options = options;
        Device = device;
        IsMirrored = device.Facing == CameraFacing.Front;
    }

    public CameraSessionOptions Options { get; }
    public CameraAnalysisPump Pump { get; } = new();
    public bool Disposed { get; private set; }
    public int PauseCount { get; private set; }
    public int ResumeCount { get; private set; }
    public List<bool> TorchCalls { get; } = new();
    public List<float> ZoomCalls { get; } = new();
    public List<CameraCaptureOptions> Captures { get; } = new();

    /// <summary>What <see cref="CaptureStillAsync"/> returns; a 4×2 red pixel still by default.</summary>
    public Func<CameraCaptureOptions, CameraStill>? StillFactory { get; set; }

    public CameraSessionState State { get; set; } = CameraSessionState.Starting;
    public CameraErrorKind? ErrorKind { get; private set; }
    public string? ErrorMessage { get; private set; }
    public CameraDeviceInfo? Device { get; }
    public int FrameWidth { get; private set; }
    public int FrameHeight { get; private set; }
    public int Rotation { get; set; }
    public bool IsMirrored { get; set; }
    public IVideoFrameSource FrameSource => _frames;
    public bool HasTorch { get; set; } = true;
    public bool IsTorchOn { get; private set; }
    public float MinZoom { get; set; } = 1;
    public float MaxZoom { get; set; } = 4;
    public float Zoom { get; private set; } = 1;
    public ICameraFrameAnalyzer? Analyzer => Pump.Analyzer;
    public event Action<CameraSessionEvent>? Event;

    /// <summary>Delivers the first frame (solid grey) and reports the session as running.</summary>
    public void Start(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        Array.Fill(pixels, (byte)128);
        Start(width, height, pixels);
    }

    public void Start(int width, int height, byte[] rgba)
    {
        FrameWidth = width;
        FrameHeight = height;
        State = CameraSessionState.Running;
        PushFrame(width, height, rgba, raiseEvent: false);
        Event?.Invoke(new CameraSessionEvent.Started(width, height));
    }

    /// <summary>Pushes an RGBA preview frame.</summary>
    public void PushFrame(int width, int height, byte[] rgba, bool raiseEvent = true)
    {
        _frames.PushFrame(VideoFrameBuffer.FromCpuPlanes(width, height, VideoPixelFormat.Rgba8888, TimeSpan.Zero, [rgba], [width * 4]));
        if (raiseEvent) Event?.Invoke(CameraSessionEvent.FrameAvailable.Instance);
    }

    public void Fail(CameraErrorKind kind, string message)
    {
        State = CameraSessionState.Error;
        ErrorKind = kind;
        ErrorMessage = message;
        Event?.Invoke(new CameraSessionEvent.Error(kind, message));
    }

    public void SetTorch(bool on)
    {
        TorchCalls.Add(on);
        IsTorchOn = on;
    }

    public void SetZoom(float zoom)
    {
        Zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        ZoomCalls.Add(Zoom);
    }

    public void Pause()
    {
        PauseCount++;
        State = CameraSessionState.Paused;
    }

    public void Resume()
    {
        ResumeCount++;
        State = FrameWidth > 0 ? CameraSessionState.Running : CameraSessionState.Starting;
    }

    public void SetAnalyzer(ICameraFrameAnalyzer? analyzer) => Pump.Analyzer = analyzer;

    public Task<CameraStill> CaptureStillAsync(CameraCaptureOptions options, CancellationToken cancellationToken = default)
    {
        if (State != CameraSessionState.Running) throw new InvalidOperationException("The camera is not running.");
        Captures.Add(options);
        if (StillFactory is { } factory) return Task.FromResult(factory(options));
        var pixels = new byte[4 * 2 * 4];
        for (int i = 0; i < pixels.Length; i += 4) { pixels[i] = 255; pixels[i + 3] = 255; }
        return Task.FromResult(CameraStill.FromPixels(4, 2, ImagePixelFormat.Rgba8888, pixels, 16, Rotation, IsMirrored));
    }

    public void Dispose()
    {
        Disposed = true;
        _frames.Dispose();
    }

    private sealed class FakeFrameSource : VideoFrameSourceBase;
}
