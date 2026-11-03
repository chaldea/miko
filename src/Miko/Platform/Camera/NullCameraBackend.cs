namespace Miko.Platform.Camera;

/// <summary>
/// The core's camera backend when no platform backend is registered, like
/// <see cref="Video.NullVideoBackend"/>: it lists no cameras and the engine opens no sessions, so a
/// <see cref="Core.DomElements.CameraElement"/> shows only its background.
/// </summary>
public sealed class NullCameraBackend : ICameraBackend
{
    public IReadOnlyList<CameraDeviceInfo> GetDevices() => Array.Empty<CameraDeviceInfo>();

    /// <summary>The engine never calls this (it checks for the null backend first); a direct call fails.</summary>
    public ICameraSession CreateSession(CameraSessionOptions options)
        => throw new NotSupportedException(
            "No camera backend is registered. Register a platform backend (UseSystemCamera() on the desktop, " +
            "UseAndroidCamera(), UseIosCamera(), or the simulator) to enable the camera.");
}
