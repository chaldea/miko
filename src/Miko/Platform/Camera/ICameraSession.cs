using Miko.Platform.Video;

namespace Miko.Platform.Camera;

/// <summary>
/// One open camera, bound to one <see cref="Core.DomElements.CameraElement"/>. The engine creates it
/// when the element enters the tree and disposes it when the element leaves, is deactivated or asks
/// for another camera.
/// <para>
/// Control methods are called on the UI thread. Frames are produced on the backend's own threads and
/// announced through <see cref="Event"/>; the render thread takes the latest one from
/// <see cref="FrameSource"/>, as for video (zero-copy textures where the platform allows it).
/// </para>
/// </summary>
public interface ICameraSession : IDisposable
{
    CameraSessionState State { get; }

    /// <summary>Why the session failed; null unless <see cref="State"/> is <see cref="CameraSessionState.Error"/>.</summary>
    CameraErrorKind? ErrorKind { get; }

    string? ErrorMessage { get; }

    /// <summary>The camera that was opened; null before it is known.</summary>
    CameraDeviceInfo? Device { get; }

    /// <summary>Raw width of the frames (sensor orientation), 0 before the first frame.</summary>
    int FrameWidth { get; }

    /// <summary>Raw height of the frames, 0 before the first frame.</summary>
    int FrameHeight { get; }

    /// <summary>
    /// Clockwise rotation (0, 90, 180 or 270) that turns a raw frame upright on the current display.
    /// Read every frame: it follows the display orientation.
    /// </summary>
    int Rotation { get; }

    /// <summary>Whether the preview is shown mirrored (after <see cref="Rotation"/>): true for front cameras.</summary>
    bool IsMirrored { get; }

    /// <summary>Preview frames for the render thread. The frames are owned by the source.</summary>
    IVideoFrameSource FrameSource { get; }

    /// <summary>Whether the camera has a torch (a continuous flash light).</summary>
    bool HasTorch { get; }

    bool IsTorchOn { get; }

    /// <summary>Turns the torch on or off; ignored without a torch or before the session runs.</summary>
    void SetTorch(bool on);

    /// <summary>Smallest zoom factor (1 when zoom is not supported).</summary>
    float MinZoom { get; }

    /// <summary>Largest zoom factor (1 when zoom is not supported).</summary>
    float MaxZoom { get; }

    float Zoom { get; }

    /// <summary>Sets the zoom factor, clamped to [<see cref="MinZoom"/>, <see cref="MaxZoom"/>].</summary>
    void SetZoom(float zoom);

    /// <summary>Stops the frames and releases the device for other apps; <see cref="Resume"/> reopens it.</summary>
    void Pause();

    void Resume();

    /// <summary>
    /// Installs the analyzer that receives luminance frames, or removes it with null. Backends produce
    /// analysis frames only while an analyzer is installed, and deliver them through a
    /// <see cref="CameraAnalysisPump"/>, which drops frames while the analyzer is busy.
    /// </summary>
    void SetAnalyzer(ICameraFrameAnalyzer? analyzer);

    /// <summary>
    /// Captures a still. Fails with <see cref="InvalidOperationException"/> when the session is not
    /// running. The still may be rotated or carry EXIF orientation; <see cref="Core.DomElements.CameraElement.CapturePhotoAsync"/>
    /// turns it into an upright photo.
    /// </summary>
    Task<CameraStill> CaptureStillAsync(CameraCaptureOptions options, CancellationToken cancellationToken = default);

    /// <summary>Session events. May be raised on backend threads.</summary>
    event Action<CameraSessionEvent>? Event;
}
