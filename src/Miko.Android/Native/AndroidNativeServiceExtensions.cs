using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Native;
using Miko.Native.App;
using Miko.Native.Browser;
using Miko.Native.Camera;
using Miko.Native.Clipboard;
using Miko.Native.Device;
using Miko.Native.Filesystem;
using Miko.Native.Geolocation;
using Miko.Native.Haptics;
using Miko.Native.Keyboard;
using Miko.Native.LocalNotifications;
using Miko.Native.Motion;
using Miko.Native.Network;
using Miko.Native.PushNotifications;
using Miko.Native.ScreenReader;
using Miko.Native.SplashScreen;
using Miko.Native.StatusBar;
using Miko.Native.Toast;

namespace Miko.Android.Native;

/// <summary>
/// Android Native 能力的 DI 注册扩展。
/// </summary>
public static class AndroidNativeServiceExtensions
{
    /// <summary>
    /// 注册 Android Native 能力实现。
    /// <para>
    /// 内部先调 <c>AddMikoNative()</c>（幂等）铺好全部 <c>Null*</c> 默认实现，再用 <c>Replace</c>
    /// 覆盖为 Android 实现，因此与 <c>AddMikoNative()</c> 的调用顺序无关（ISSUE-129 契约）。
    /// </para>
    /// <para>
    /// 服务需要 <c>Activity</c>/<c>Context</c>，而服务容器在 <c>MikoAppBuilder.Build()</c> 时就已构建、
    /// 那时 Activity 还不存在。因此宿主 <see cref="MikoSurfaceView"/> 在构造时把
    /// <see cref="AndroidNativeHost"/> <c>Attach</c> 到 <see cref="INativeHostContext"/>，
    /// 服务在**调用时**才读取它。
    /// </para>
    /// <para>
    /// 需要 <c>startActivityForResult</c> 的能力（相机、相册、图片编辑）还要求宿主 Activity
    /// 把 <c>OnActivityResult</c> 转发给 <see cref="AndroidActivityResultRelay.Deliver"/>，
    /// 见 <see cref="MikoAndroidApp.HandleActivityResult"/>。
    /// </para>
    /// </summary>
    public static MikoAppBuilder UseAndroidNative(this MikoAppBuilder builder)
    {
        var services = builder.Services;

        services.AddMikoNative();

        services.ReplaceNative<IAppService, AndroidAppService>();
        services.ReplaceNative<IBrowserService, AndroidBrowserService>();
        services.ReplaceNative<ICameraService, AndroidCameraService>();
        services.ReplaceNative<IClipboardService, AndroidClipboardService>();
        services.ReplaceNative<IDeviceService, AndroidDeviceService>();
        services.ReplaceNative<IFilesystemService, AndroidFilesystemService>();
        services.ReplaceNative<IGeolocationService, AndroidGeolocationService>();
        services.ReplaceNative<IHapticsService, AndroidHapticsService>();
        services.ReplaceNative<IKeyboardService, AndroidKeyboardService>();
        services.ReplaceNative<ILocalNotificationService, AndroidLocalNotificationService>();
        services.ReplaceNative<IMotionService, AndroidMotionService>();
        services.ReplaceNative<INetworkService, AndroidNetworkService>();
        services.ReplaceNative<IPushNotificationService, AndroidPushNotificationService>();
        services.ReplaceNative<IScreenReaderService, AndroidScreenReaderService>();
        services.ReplaceNative<ISplashScreenService, AndroidSplashScreenService>();
        services.ReplaceNative<IStatusBarService, AndroidStatusBarService>();
        services.ReplaceNative<IToastService, AndroidToastService>();

        return builder;
    }
}
