using Miko.Platform.Camera;

namespace Miko.Core.DomElements;

/// <summary>
/// Live camera preview (<c>&lt;camera&gt;</c>, an engine extension). Like <see cref="VideoElement"/>,
/// Miko only composites the latest frame into the box; the platform backend
/// (<see cref="ICameraBackend"/>) opens the device. The engine owns the session: it opens one when an
/// active element enters the tree, disposes it when the element leaves, is deactivated or asks for
/// another camera, and pauses it while the app is in the background.
/// <para>
/// A replaced element: it takes part in layout, clipping, opacity and transforms, and other elements
/// can be laid over it — the base of camera UIs such as a barcode scanner or a photo camera.
/// </para>
/// </summary>
public class CameraElement : Element
{
    public override string TagName => "camera";

    private CameraFacing _facing = CameraFacing.Back;
    private string? _deviceId;
    private CameraResolution _resolution = CameraResolution.High;
    private bool _active = true;
    private ICameraFrameAnalyzer? _analyzer;

    /// <summary>Which camera to open. Changing it opens the other camera.</summary>
    public CameraFacing Facing
    {
        get => _facing;
        set
        {
            if (_facing == value) return;
            _facing = value;
            BumpContentVersion();
        }
    }

    /// <summary>A specific camera (see <see cref="ICameraBackend.GetDevices"/>); wins over <see cref="Facing"/>.</summary>
    public string? DeviceId
    {
        get => _deviceId;
        set
        {
            if (_deviceId == value) return;
            _deviceId = value;
            BumpContentVersion();
        }
    }

    public CameraResolution Resolution
    {
        get => _resolution;
        set
        {
            if (_resolution == value) return;
            _resolution = value;
            BumpContentVersion();
        }
    }

    /// <summary>Whether the camera runs. False releases the device; the element then shows its background.</summary>
    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value) return;
            _active = value;
            BumpContentVersion();
        }
    }

    /// <summary>Torch (continuous flash light). The engine applies it to the running session each frame.</summary>
    public bool Torch { get; set; }

    /// <summary>Zoom factor, clamped to the session's range; applied each frame.</summary>
    public float Zoom { get; set; } = 1;

    /// <summary>How the preview fills the content box. Paint only.</summary>
    public CameraFit Fit { get; set; } = CameraFit.Cover;

    /// <summary>Mirrors the preview; null follows the camera (front cameras are mirrored). Paint only.</summary>
    public bool? Mirror { get; set; }

    /// <summary>
    /// Receives luminance frames (barcode scanning and the like) on a thread-pool thread. Carried over to
    /// every session the element opens.
    /// </summary>
    public ICameraFrameAnalyzer? Analyzer
    {
        get => _analyzer;
        set
        {
            if (ReferenceEquals(_analyzer, value)) return;
            _analyzer = value;
            Session?.SetAnalyzer(value);
        }
    }

    /// <summary>The open session, set and disposed by the engine; null while none is open.</summary>
    public ICameraSession? Session { get; internal set; }

    /// <summary>Raised on the UI thread when the engine opens or releases a session.</summary>
    public event Action<ICameraSession?>? SessionChanged;

    /// <summary>Session events (start, stop, errors — not frames), on the UI thread.</summary>
    public event Action<CameraSessionEvent>? SessionEvent;

    /// <summary>
    /// Opens the camera again on the next frame — after an error, for example once the user has granted
    /// the permission in the system settings.
    /// </summary>
    public void Restart()
    {
        RestartRequested = true;
        BumpContentVersion();
    }

    /// <summary>
    /// Takes a photo with the open session and returns it upright and encoded. Call on the UI thread;
    /// fails with <see cref="InvalidOperationException"/> when no session is running.
    /// </summary>
    public async Task<CameraPhoto> CapturePhotoAsync(CameraCaptureOptions? options = null, CancellationToken cancellationToken = default)
    {
        var session = Session;
        if (session is not { State: CameraSessionState.Running })
            throw new InvalidOperationException("The camera is not running.");
        options ??= new CameraCaptureOptions();
        var still = await session.CaptureStillAsync(options, cancellationToken).ConfigureAwait(false);
        return await Task.Run(() => CameraPhotoEncoder.Encode(still, options), cancellationToken).ConfigureAwait(false);
    }

    // ---- Engine state ---------------------------------------------------------------------------

    /// <summary>What the current (or last attempted) session was opened with; null when none was.</summary>
    internal CameraSessionOptions? SessionOptions { get; set; }

    internal bool RestartRequested { get; set; }

    internal Action<CameraSessionEvent>? SessionHandler { get; set; }

    internal bool Matches(CameraSessionOptions options)
        => options.Facing == _facing && options.DeviceId == _deviceId && options.Resolution == _resolution;

    internal CameraSessionOptions CreateOptions() => new() { Facing = _facing, DeviceId = _deviceId, Resolution = _resolution };

    internal void NotifySessionChanged() => SessionChanged?.Invoke(Session);

    internal void NotifySessionEvent(CameraSessionEvent value) => SessionEvent?.Invoke(value);

    /// <summary>Intrinsic size (upright frame size), set when the session starts; null falls back to 300×150.</summary>
    internal int? IntrinsicWidth { get; set; }

    internal int? IntrinsicHeight { get; set; }
}
