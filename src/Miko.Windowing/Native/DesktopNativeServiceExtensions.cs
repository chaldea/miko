using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Native;
using Miko.Native.App;
using Miko.Native.Browser;
using Miko.Native.Clipboard;
using Miko.Native.Device;
using Miko.Native.Filesystem;
using Miko.Native.Keyboard;
using Miko.Native.Network;
using Miko.Native.ScreenReader;
using Miko.Platform;

namespace Miko.Windowing.Native;

/// <summary>
/// 桌面（Windows/Linux/macOS）Native 能力的 DI 注册扩展。
/// </summary>
public static class DesktopNativeServiceExtensions
{
    /// <summary>
    /// 注册桌面 Native 能力实现。
    /// <para>
    /// 内部先调用 <c>AddMikoNative()</c>（幂等）铺好全部 <c>Null*</c> 默认实现，再用
    /// <c>Replace</c> 覆盖桌面能真正支持的那些，因此调用方写不写 <c>AddMikoNative()</c>、
    /// 写在前还是写在后都不影响结果（ISSUE-129 契约）。
    /// </para>
    /// <para>
    /// 桌面**实现**：App（应用信息）、Browser（系统浏览器）、Clipboard（Windows）、
    /// Device、Filesystem、Keyboard、Network、ScreenReader（Windows 检测）。
    /// </para>
    /// <para>
    /// 桌面**不支持**（保留 <c>Null*</c>，调用即抛 <see cref="PlatformNotSupportedException"/>）：
    /// Camera、Geolocation、LocalNotification、PushNotification，以及仅移动端的
    /// Haptics、Motion、SplashScreen、StatusBar、Toast。
    /// </para>
    /// </summary>
    public static MikoAppBuilder UseDesktopNative(this MikoAppBuilder builder)
    {
        var services = builder.Services;

        services.AddMikoNative();

        services.ReplaceNative<IAppService, DesktopAppService>();
        services.ReplaceNative<IBrowserService, DesktopBrowserService>();
        services.ReplaceNative<IClipboardService, DesktopClipboardService>();
        services.ReplaceNative<IDeviceService, DesktopDeviceService>();
        services.ReplaceNative<IFilesystemService, DesktopFilesystemService>();
        services.ReplaceNative<IKeyboardService>(sp =>
            new DesktopKeyboardService(sp.GetRequiredService<IInputMethodService>()));
        services.ReplaceNative<INetworkService, DesktopNetworkService>();
        services.ReplaceNative<IScreenReaderService, DesktopScreenReaderService>();

        return builder;
    }
}
