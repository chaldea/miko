using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miko.Hosting;
using Miko.Native;
using Miko.Native.Camera;
using Miko.Native.Device;
using Miko.Native.Geolocation;
using Miko.Native.Haptics;
using Miko.Native.LocalNotifications;
using Miko.Native.Motion;
using Miko.Native.PushNotifications;
using Miko.Native.ScreenReader;
using Miko.Native.SplashScreen;
using Miko.Native.StatusBar;
using Miko.Native.Toast;
using Miko.Windowing.Native;

namespace Miko.Simulator.Native;

/// <summary>
/// 模拟器 Native 能力的 DI 注册扩展。
/// </summary>
public static class SimulatorNativeServiceExtensions
{
    /// <summary>
    /// 注册模拟器的 Native 能力实现。
    /// <para>
    /// 分两类：
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <b>沿用桌面真实实现</b>——App、Browser、Clipboard、Filesystem、Keyboard、Network。
    /// 模拟器本来就跑在桌面上，这些能力在宿主机上是真的，仿真反而不如真实。
    /// </item>
    /// <item>
    /// <b>模拟器仿真实现</b>——Device（映射当前设备预设）、Camera、Geolocation、Haptics、
    /// LocalNotification、Motion、PushNotification、ScreenReader、SplashScreen、StatusBar、Toast。
    /// 这些在桌面上没有系统栈，仿真的目的是让调用链在桌面上可跑通、可观察，
    /// 而不是让开发者一调用就撞 <see cref="PlatformNotSupportedException"/>。
    /// </item>
    /// </list>
    /// <para>
    /// 内部先调 <c>UseDesktopNative()</c>（它自己会调 <c>AddMikoNative()</c>），再用 <c>Replace</c>
    /// 覆盖需要仿真的部分，因此与 <c>AddMikoNative()</c> 的调用顺序无关。
    /// </para>
    /// </summary>
    public static MikoAppBuilder UseSimulatorNative(this MikoAppBuilder builder)
    {
        var services = builder.Services;

        // 先铺桌面实现（含 AddMikoNative 的全部 Null 默认），再覆盖需要仿真的能力。
        builder.UseDesktopNative();

        services.ReplaceNative<IDeviceService>(sp => new SimulatorDeviceService(
            sp.GetRequiredService<INativeHostContext>(),
            sp.GetService<ILogger<SimulatorDeviceService>>()));

        services.ReplaceNative<ICameraService>(sp => new SimulatorCameraService(
            sp.GetRequiredService<INativeHostContext>(),
            sp.GetService<ILogger<SimulatorCameraService>>()));

        services.ReplaceNative<IGeolocationService>(sp => new SimulatorGeolocationService(
            sp.GetRequiredService<INativeHostContext>(),
            sp.GetService<ILogger<SimulatorGeolocationService>>()));

        services.ReplaceNative<IHapticsService>(sp => new SimulatorHapticsService(
            sp.GetRequiredService<INativeHostContext>(),
            sp.GetService<ILogger<SimulatorHapticsService>>()));

        services.ReplaceNative<ILocalNotificationService>(sp => new SimulatorLocalNotificationService(
            sp.GetRequiredService<INativeHostContext>(),
            sp.GetService<ILogger<SimulatorLocalNotificationService>>()));

        services.ReplaceNative<IMotionService, SimulatorMotionService>();

        services.ReplaceNative<IPushNotificationService>(sp => new SimulatorPushNotificationService(
            sp.GetRequiredService<INativeHostContext>(),
            sp.GetService<ILogger<SimulatorPushNotificationService>>()));

        services.ReplaceNative<IScreenReaderService>(sp => new SimulatorScreenReaderService(
            sp.GetRequiredService<INativeHostContext>(),
            sp.GetService<ILogger<SimulatorScreenReaderService>>()));

        services.ReplaceNative<ISplashScreenService>(sp => new SimulatorSplashScreenService(
            sp.GetRequiredService<INativeHostContext>(),
            sp.GetService<ILogger<SimulatorSplashScreenService>>()));

        services.ReplaceNative<IStatusBarService>(sp => new SimulatorStatusBarService(
            sp.GetRequiredService<INativeHostContext>(),
            sp.GetService<ILogger<SimulatorStatusBarService>>()));

        services.ReplaceNative<IToastService>(sp => new SimulatorToastService(
            sp.GetRequiredService<INativeHostContext>(),
            sp.GetService<ILogger<SimulatorToastService>>()));

        return builder;
    }
}
