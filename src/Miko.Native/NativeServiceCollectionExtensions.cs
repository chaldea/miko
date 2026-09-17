using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miko.Hosting;
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

namespace Miko.Native;

/// <summary>
/// Native 能力接口的 DI 注册扩展。
/// <para>
/// 应用侧只需注册一次：<c>builder.Services.AddMikoNative();</c>，随后即可在页面中
/// <c>[Inject] public ICameraService CameraService { get; set; } = default!;</c>。
/// </para>
/// </summary>
public static class NativeServiceCollectionExtensions
{
    /// <summary>
    /// 注册全部 Native 能力接口的默认（<c>Null*</c>）实现与 <see cref="INativeHostContext"/>。
    /// <para>
    /// 全部使用 <c>TryAdd</c>（ISSUE-129 契约）：既可重复调用，也**绝不覆盖**调用方已有的注册——
    /// 平台包的 <c>UseDesktopNative()</c> / <c>UseAndroidNative()</c> / <c>UseIosNative()</c> /
    /// <c>UseSimulatorNative()</c> 无论在本方法之前还是之后调用，其真实实现都胜出
    /// （平台侧用 <c>Replace</c> 而非 <c>Add</c>）。
    /// </para>
    /// <para>
    /// 默认实现不会让注入失败，但任何调用都抛 <see cref="PlatformNotSupportedException"/>，
    /// 而不是静默返回假数据。
    /// </para>
    /// </summary>
    public static IServiceCollection AddMikoNative(this IServiceCollection services)
    {
        // 平台宿主（Activity / UIViewController / IWindow）的延迟载体。宿主在自身构造完成时
        // 调用 Attach 回填——服务容器在那之前就已经构建好了。
        services.TryAddSingleton<INativeHostContext, NativeHostContext>();

        services.TryAddSingleton<IAppService, NullAppService>();
        services.TryAddSingleton<IBrowserService, NullBrowserService>();
        services.TryAddSingleton<ICameraService, NullCameraService>();
        services.TryAddSingleton<IClipboardService, NullClipboardService>();
        services.TryAddSingleton<IDeviceService, NullDeviceService>();
        services.TryAddSingleton<IFilesystemService, NullFilesystemService>();
        services.TryAddSingleton<IGeolocationService, NullGeolocationService>();
        services.TryAddSingleton<IHapticsService, NullHapticsService>();
        services.TryAddSingleton<IKeyboardService, NullKeyboardService>();
        services.TryAddSingleton<ILocalNotificationService, NullLocalNotificationService>();
        services.TryAddSingleton<IMotionService, NullMotionService>();
        services.TryAddSingleton<INetworkService, NullNetworkService>();
        services.TryAddSingleton<IPushNotificationService, NullPushNotificationService>();
        services.TryAddSingleton<IScreenReaderService, NullScreenReaderService>();
        services.TryAddSingleton<ISplashScreenService, NullSplashScreenService>();
        services.TryAddSingleton<IStatusBarService, NullStatusBarService>();
        services.TryAddSingleton<IToastService, NullToastService>();

        return services;
    }

    /// <summary>
    /// <see cref="AddMikoNative(IServiceCollection)"/> 的 <see cref="MikoAppBuilder"/> 重载，
    /// 便于链式配置。
    /// </summary>
    public static MikoAppBuilder AddMikoNative(this MikoAppBuilder builder)
    {
        builder.Services.AddMikoNative();
        return builder;
    }
}
