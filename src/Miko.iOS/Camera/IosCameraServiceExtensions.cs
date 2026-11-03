using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform.Camera;

namespace Miko.iOS.Camera;

/// <summary>DI registration of the iOS camera backend.</summary>
public static class IosCameraServiceExtensions
{
    /// <summary>
    /// Registers the AVFoundation camera backend (<c>AVCaptureSession</c>, zero-copy Metal preview,
    /// <c>AVCapturePhotoOutput</c> stills). The app's Info.plist must declare <c>NSCameraUsageDescription</c>.
    /// </summary>
    public static MikoAppBuilder UseIosCamera(this MikoAppBuilder builder)
    {
        builder.Services.AddSingleton<ICameraBackend, IosCameraBackend>();
        return builder;
    }
}
