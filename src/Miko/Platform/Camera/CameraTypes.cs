namespace Miko.Platform.Camera;

/// <summary>Which way a camera faces.</summary>
public enum CameraFacing
{
    /// <summary>The main camera on the back of a phone or tablet (the default).</summary>
    Back,

    /// <summary>The selfie camera; its preview is mirrored.</summary>
    Front,

    /// <summary>A camera that is not part of the device, such as a USB webcam.</summary>
    External,
}

/// <summary>Preview resolution preset. Backends pick the closest size the device supports.</summary>
public enum CameraResolution
{
    /// <summary>About 640×480: cheap, enough for barcode scanning on slow devices.</summary>
    Low,

    /// <summary>About 1280×720.</summary>
    Medium,

    /// <summary>About 1920×1080 (the default).</summary>
    High,
}

/// <summary>How the preview fills the element's content box.</summary>
public enum CameraFit
{
    /// <summary>Fills the box and crops what overflows, like <c>object-fit: cover</c> (the default).</summary>
    Cover,

    /// <summary>Shows the whole frame, letterboxed, like <c>object-fit: contain</c>.</summary>
    Contain,
}

/// <summary>Flash used for a still capture.</summary>
public enum CameraFlashMode
{
    Off,
    On,
    Auto,
}

/// <summary>Encoding of a captured photo.</summary>
public enum CameraPhotoFormat
{
    Jpeg,
    Png,
}

/// <summary>A camera the backend can open.</summary>
/// <param name="Id">Backend-specific identifier, stable while the device is connected.</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Facing">Which way it faces.</param>
public sealed record CameraDeviceInfo(string Id, string Name, CameraFacing Facing);

/// <summary>What a session opens. An explicit <see cref="DeviceId"/> wins over <see cref="Facing"/>.</summary>
public sealed record CameraSessionOptions
{
    public CameraFacing Facing { get; init; } = CameraFacing.Back;

    public string? DeviceId { get; init; }

    public CameraResolution Resolution { get; init; } = CameraResolution.High;
}

/// <summary>Options of a still capture.</summary>
public sealed record CameraCaptureOptions
{
    public CameraPhotoFormat Format { get; init; } = CameraPhotoFormat.Jpeg;

    /// <summary>JPEG quality, 1–100.</summary>
    public int Quality { get; init; } = 90;

    public CameraFlashMode Flash { get; init; } = CameraFlashMode.Off;

    /// <summary>
    /// Keep the mirror image of the preview in photos from the front camera. Off by default, as in the
    /// system cameras: the photo shows the scene as others see it.
    /// </summary>
    public bool MirrorFrontCamera { get; init; }

    /// <summary>Longest side of the photo in pixels; larger photos are scaled down. Null keeps the sensor size.</summary>
    public int? MaxDimension { get; init; }
}

/// <summary>Session state machine.</summary>
public enum CameraSessionState
{
    /// <summary>Not started.</summary>
    Idle,

    /// <summary>Asking for permission, opening the device or waiting for the first frame.</summary>
    Starting,

    /// <summary>Delivering frames.</summary>
    Running,

    /// <summary>Stopped by <see cref="ICameraSession.Pause"/> (the app went to the background); the device is released.</summary>
    Paused,

    /// <summary>Failed; see <see cref="ICameraSession.ErrorKind"/>.</summary>
    Error,
}

/// <summary>Why a session failed.</summary>
public enum CameraErrorKind
{
    /// <summary>The user or the system denied camera access.</summary>
    PermissionDenied,

    /// <summary>No camera matches the requested facing or device id.</summary>
    NoDevice,

    /// <summary>The camera exists but another app or session holds it, or the system disabled it.</summary>
    Unavailable,

    /// <summary>Any other failure.</summary>
    Failed,
}

/// <summary>Session events. Backends may raise them on any thread; the engine marshals them to the UI thread.</summary>
public abstract record CameraSessionEvent
{
    /// <summary>The first frame arrived: the session is running with frames of this raw size.</summary>
    public sealed record Started(int Width, int Height) : CameraSessionEvent;

    /// <summary>A new preview frame is available. Raised for every frame; use <see cref="Instance"/>.</summary>
    public sealed record FrameAvailable : CameraSessionEvent
    {
        public static FrameAvailable Instance { get; } = new();
    }

    /// <summary>The session stopped delivering frames (paused or disposed).</summary>
    public sealed record Stopped : CameraSessionEvent;

    public sealed record Error(CameraErrorKind Kind, string Message, Exception? Cause = null) : CameraSessionEvent;
}
