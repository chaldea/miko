using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform.Camera;

namespace Miko.Android.Camera;

/// <summary>DI registration of the Android camera backend.</summary>
public static class AndroidCameraServiceExtensions
{
    /// <summary>
    /// Registers the Camera2 backend for <c>&lt;camera&gt;</c> (zero-copy OES preview, YUV analysis,
    /// JPEG stills). The app's manifest must declare <c>android.permission.CAMERA</c>; the permission is
    /// requested when a camera first opens.
    /// </summary>
    public static MikoAppBuilder UseAndroidCamera(this MikoAppBuilder builder)
    {
        builder.Services.AddSingleton<ICameraBackend, AndroidCameraBackend>();
        return builder;
    }
}
