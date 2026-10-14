using Microsoft.Extensions.DependencyInjection;
using Miko.Native;
using Miko.Platform;

namespace Miko.iOS.Native;

/// <summary>
/// iOS 的屏幕方向 / 息屏控制（ISSUE-138）。
///
/// <para>
/// 息屏走 <c>UIApplication.IdleTimerDisabled</c>——这是 UIKit 提供的能力，直接可靠。
/// 方向则请求所连 scene 重算几何（iOS 16+ 的 <c>RequestGeometryUpdate</c>）：这要求宿主
/// <c>MikoViewController</c> 同步放开对应的 <c>GetSupportedInterfaceOrientations</c>，
/// 否则系统会以 <see cref="UIWindowSceneGeometryPreferencesIOS"/> 不允许该方向为由拒绝。
/// 模拟器上该请求通常被忽略（模拟器不强制旋转），**真机才可见效**。
/// </para>
///
/// <para>
/// 与其它组件面向的服务一致：宿主缺失或系统拒绝时静默降级，不抛异常——方向锁定是交互增强，
/// 视频组件不该因为旋转失败就崩。
/// </para>
/// </summary>
internal sealed class IosOrientationService : IOrientationService
{
    private readonly INativeHostContext _hostContext;
    private readonly object _sync = new();

    private int _lockDepth;
    private int _awakeDepth;
    private ScreenOrientation? _lockedOrientation;

    public IosOrientationService(INativeHostContext hostContext) => _hostContext = hostContext;

    public void Lock(ScreenOrientation? orientation = null)
    {
        ScreenOrientation? requested;
        lock (_sync)
        {
            _lockDepth++;
            // 首次锁定才记录方向：嵌套锁定中内层传 null 表示"保持外层方向"。
            if (_lockDepth == 1) _lockedOrientation = orientation;
            requested = _lockedOrientation;
        }

        if (requested is { } target) ApplyOrientation(target);
    }

    public void Unlock()
    {
        lock (_sync)
        {
            if (_lockDepth == 0) return;
            _lockDepth--;
            if (_lockDepth == 0) _lockedOrientation = null;
            else return;
        }

        // 全部解锁：回到竖屏（与 iOS 应用默认布局一致）。
        ApplyOrientation(ScreenOrientation.Portrait);
    }

    public Task SetAsync(ScreenOrientation orientation)
    {
        lock (_sync)
        {
            // 锁定期间忽略 Set（与平台约定一致）。
            if (_lockDepth > 0) return Task.CompletedTask;
        }

        ApplyOrientation(orientation);
        return Task.CompletedTask;
    }

    public void KeepAwake()
    {
        lock (_sync)
        {
            _awakeDepth++;
            if (_awakeDepth != 1) return;
        }

        UIApplication.SharedApplication.InvokeOnMainThread(
            () => UIApplication.SharedApplication.IdleTimerDisabled = true);
    }

    public void AllowSleep()
    {
        lock (_sync)
        {
            if (_awakeDepth == 0) return;
            _awakeDepth--;
            if (_awakeDepth != 0) return;
        }

        UIApplication.SharedApplication.InvokeOnMainThread(
            () => UIApplication.SharedApplication.IdleTimerDisabled = false);
    }

    private void ApplyOrientation(ScreenOrientation orientation)
    {
        var mask = ToInterfaceMask(orientation);

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            var controller = (_hostContext.Host as IosNativeHost)?.ViewController as MikoViewController;
            if (controller is null) return;

            // 必须先放开声明，再请求几何更新：系统在后者到来时会回头询问前者，
            // 顺序颠倒则该请求会被以"方向不受支持"为由忽略。
            controller.SetSupportedOrientations(mask);

            if (!OperatingSystem.IsIOSVersionAtLeast(16)) return;

            var scene = controller.View?.Window?.WindowScene;
            if (scene is null) return;

            var preferences = new UIWindowSceneGeometryPreferencesIOS(mask);
            // 错误回调是必须的：系统仍可能拒绝（例如 iPad 多窗口），忽略即可——
            // 静默降级是本服务的契约。
            scene.RequestGeometryUpdate(preferences, _ => { });
        });
    }

    private static UIInterfaceOrientationMask ToInterfaceMask(ScreenOrientation orientation) => orientation switch
    {
        ScreenOrientation.Portrait => UIInterfaceOrientationMask.Portrait,
        ScreenOrientation.PortraitPrimary => UIInterfaceOrientationMask.Portrait,
        ScreenOrientation.PortraitSecondary => UIInterfaceOrientationMask.PortraitUpsideDown,
        ScreenOrientation.Landscape => UIInterfaceOrientationMask.Landscape,
        ScreenOrientation.LandscapePrimary => UIInterfaceOrientationMask.LandscapeLeft,
        ScreenOrientation.LandscapeSecondary => UIInterfaceOrientationMask.LandscapeRight,
        _ => UIInterfaceOrientationMask.Portrait,
    };
}
