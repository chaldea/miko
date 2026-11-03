using AVFoundation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Camera;

namespace Miko.iOS.Camera;

/// <summary>
/// iOS camera backend on AVFoundation (<c>AVCaptureSession</c>): preview frames are mapped to Metal
/// textures without copying, the same path as video (<see cref="Video.IosVideoFrameSource"/>), stills
/// come from <c>AVCapturePhotoOutput</c> as JPEG. A system framework: nothing is added to the app.
/// <para>
/// The app's Info.plist must carry <c>NSCameraUsageDescription</c>; without it iOS terminates the app
/// on the first camera access. The simulator has no camera: sessions there fail with
/// <see cref="CameraErrorKind.NoDevice"/>.
/// </para>
/// </summary>
public sealed class IosCameraBackend : ICameraBackend
{
    private readonly ILogger _logger;

    public IosCameraBackend(ILogger<IosCameraBackend>? logger = null)
    {
        _logger = logger ?? NullLogger<IosCameraBackend>.Instance;
    }

    public IReadOnlyList<CameraDeviceInfo> GetDevices()
    {
        try
        {
            return IosCameraDevices.Discover().Select(IosCameraDevices.Describe).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Camera discovery failed");
            return Array.Empty<CameraDeviceInfo>();
        }
    }

    public ICameraSession CreateSession(CameraSessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger.LogInformation("Creating iOS camera session ({Facing}, {DeviceId})", options.Facing, options.DeviceId);
        var session = new IosCameraSession(options, _logger);
        session.Start();
        return session;
    }
}

/// <summary>Device discovery shared by the backend and its sessions.</summary>
internal static class IosCameraDevices
{
    /// <summary>The wide-angle cameras (the ones every iPhone and iPad has) and, on iOS 17+, external cameras.</summary>
    public static AVCaptureDevice[] Discover()
    {
        var types = new List<AVCaptureDeviceType> { AVCaptureDeviceType.BuiltInWideAngleCamera };
        if (OperatingSystem.IsIOSVersionAtLeast(17))
            types.Add(AVCaptureDeviceType.External);
        using var discovery = AVCaptureDeviceDiscoverySession.Create(
            types.ToArray(), AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
        return discovery?.Devices ?? [];
    }

    public static CameraDeviceInfo Describe(AVCaptureDevice device)
        => new(device.UniqueID, device.LocalizedName, FacingOf(device));

    public static CameraFacing FacingOf(AVCaptureDevice device) => device.Position switch
    {
        AVCaptureDevicePosition.Front => CameraFacing.Front,
        AVCaptureDevicePosition.Back => CameraFacing.Back,
        _ => CameraFacing.External,
    };

    /// <summary>The device id if it is connected, else the first camera facing that way; null when there is none.</summary>
    public static AVCaptureDevice? Resolve(CameraSessionOptions options)
    {
        var devices = Discover();
        if (options.DeviceId is { } id)
            return devices.FirstOrDefault(d => d.UniqueID == id);
        return devices.FirstOrDefault(d => FacingOf(d) == options.Facing) ?? devices.FirstOrDefault();
    }
}
