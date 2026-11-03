namespace Miko.Platform.Camera;

/// <summary>
/// Camera backend: opens live camera sessions for <see cref="Core.DomElements.CameraElement"/>s.
/// Registered by the platform host (<c>UseSystemCamera()</c>, <c>UseAndroidCamera()</c>,
/// <c>UseIosCamera()</c>, the simulator); the engine creates and disposes the sessions, as it does
/// for <c>&lt;video&gt;</c>. The core default is <see cref="NullCameraBackend"/>, which opens nothing.
/// </summary>
public interface ICameraBackend
{
    /// <summary>The cameras that can be opened now. May be empty, and may change as devices come and go.</summary>
    IReadOnlyList<CameraDeviceInfo> GetDevices();

    /// <summary>
    /// Creates a session and starts it: the session asks for permission if it must, opens the device
    /// and reports <see cref="CameraSessionEvent.Started"/> with the first frame. Failures are reported
    /// as <see cref="CameraSessionEvent.Error"/> and <see cref="CameraSessionState.Error"/>, not thrown.
    /// Called on the UI thread.
    /// </summary>
    ICameraSession CreateSession(CameraSessionOptions options);
}
