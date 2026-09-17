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
using Miko.Platform;

namespace Miko.iOS.Native;

/// <summary>
/// iOS Native 能力的 DI 注册扩展。
/// </summary>
public static class IosNativeServiceExtensions
{
    /// <summary>
    /// 注册 iOS Native 能力实现。
    /// <para>
    /// 内部先调 <c>AddMikoNative()</c>（幂等）铺好全部 <c>Null*</c> 默认实现，再用 <c>Replace</c>
    /// 覆盖为 iOS 实现，因此与 <c>AddMikoNative()</c> 的调用顺序无关（ISSUE-129 契约）。
    /// </para>
    /// <para>
    /// 服务需要 <c>UIViewController</c> 才能 present 系统界面，而服务容器在
    /// <c>MikoAppBuilder.Build()</c> 时就已构建、那时控制器还不存在。因此
    /// <see cref="MikoViewController"/> 在 <c>ViewDidLoad</c> 时把 <see cref="IosNativeHost"/>
    /// <c>Attach</c> 到 <see cref="INativeHostContext"/>，服务在**调用时**才读取它。
    /// </para>
    /// <para>
    /// 相机、定位、通知等能力还要求应用在 Info.plist 中声明对应的用途说明
    /// （<c>NSCameraUsageDescription</c>、<c>NSLocationWhenInUseUsageDescription</c> 等），
    /// 这属于应用侧的平台配置职责（<c>issues/feat-platform.md</c> 要求第 3 条）。
    /// </para>
    /// </summary>
    public static MikoAppBuilder UseIosNative(this MikoAppBuilder builder)
    {
        var services = builder.Services;

        services.AddMikoNative();

        services.ReplaceNative<IAppService, IosAppService>();
        services.ReplaceNative<IBrowserService, IosBrowserService>();
        services.ReplaceNative<ICameraService, IosCameraService>();
        services.ReplaceNative<IClipboardService, IosClipboardService>();
        services.ReplaceNative<IDeviceService, IosDeviceService>();
        services.ReplaceNative<IFilesystemService, IosFilesystemService>();
        services.ReplaceNative<IGeolocationService, IosGeolocationService>();
        services.ReplaceNative<IHapticsService, IosHapticsService>();
        services.ReplaceNative<IKeyboardService>(sp =>
            new IosKeyboardService(sp.GetRequiredService<IInputMethodService>()));
        services.ReplaceNative<ILocalNotificationService, IosLocalNotificationService>();
        services.ReplaceNative<IMotionService, IosMotionService>();
        services.ReplaceNative<INetworkService, IosNetworkService>();
        services.ReplaceNative<IPushNotificationService, IosPushNotificationService>();
        services.ReplaceNative<IScreenReaderService, IosScreenReaderService>();
        services.ReplaceNative<ISplashScreenService, IosSplashScreenService>();
        services.ReplaceNative<IStatusBarService, IosStatusBarService>();
        services.ReplaceNative<IToastService, IosToastService>();

        return builder;
    }
}
