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
/// </para>
///
/// <para>
/// <b>锁定与解锁（ISSUE-149）：</b>首次锁定时记下控制器原本允许的方向集合，全部解锁时恢复它并
/// 请求一次几何更新，让系统按设备当前的物理朝向重新选择方向。早先的实现在解锁时把允许的方向
/// 收窄为仅竖屏，退出全屏后 App 会一直锁在竖屏。
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

    // 首次锁定前控制器允许的方向集合，全部解锁时恢复。只在主线程读写。
    private UIInterfaceOrientationMask? _restoreMask;

    public IosOrientationService(INativeHostContext hostContext) => _hostContext = hostContext;

    public void Lock(ScreenOrientation? orientation = null)
    {
        lock (_sync)
        {
            _lockDepth++;
            // 只有最外层锁定生效：嵌套锁定中内层的方向被忽略，最外层解锁前方向保持不变。
            if (_lockDepth != 1) return;
        }

        RunOnMainThread(controller =>
        {
            _restoreMask ??= controller.GetSupportedInterfaceOrientations();

            if (orientation is { } target)
            {
                var mask = ToInterfaceMask(target);
                // 必须先收窄声明，再请求几何更新：系统在后者到来时会回头询问前者，
                // 顺序颠倒则该请求会被以"方向不受支持"为由忽略。
                controller.SetSupportedOrientations(mask);
                RequestGeometryUpdate(controller, mask);
            }
            else
            {
                // 不指定方向 = 锁定在当前方向：只收窄声明，不需要旋转。
                controller.SetSupportedOrientations(CurrentOrientationMask(controller));
            }
        });
    }

    public void Unlock()
    {
        lock (_sync)
        {
            if (_lockDepth == 0) return;
            _lockDepth--;
            if (_lockDepth != 0) return;
        }

        RunOnMainThread(controller =>
        {
            var mask = _restoreMask ?? UIInterfaceOrientationMask.All;
            _restoreMask = null;

            controller.SetSupportedOrientations(mask);
            // 放开声明只会让系统在下一次设备转动时才重新选择方向；主动请求一次几何更新，
            // 设备已经竖着拿时才会立刻转回竖屏，而不是停在刚才强制的横屏上。
            RequestGeometryUpdate(controller, mask);
        });
    }

    public Task SetAsync(ScreenOrientation orientation)
    {
        lock (_sync)
        {
            // 锁定期间忽略 Set（与平台约定一致）。
            if (_lockDepth > 0) return Task.CompletedTask;
        }

        // 只请求旋转、不收窄声明：转过去之后，设备再转动时仍按允许的方向自动旋转。
        RunOnMainThread(controller => RequestGeometryUpdate(controller, ToInterfaceMask(orientation)));
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

    private void RunOnMainThread(Action<MikoViewController> action)
    {
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            // 宿主可能尚未 Attach（控制器未加载）；方向是增强能力，静默跳过而非抛错。
            if ((_hostContext.Host as IosNativeHost)?.ViewController is MikoViewController controller)
                action(controller);
        });
    }

    private static void RequestGeometryUpdate(MikoViewController controller, UIInterfaceOrientationMask mask)
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(16)) return;

        var scene = controller.View?.Window?.WindowScene;
        if (scene is null) return;

        var preferences = new UIWindowSceneGeometryPreferencesIOS(mask);
        // 错误回调是必须的：系统仍可能拒绝（例如 iPad 多窗口），忽略即可——
        // 静默降级是本服务的契约。
        scene.RequestGeometryUpdate(preferences, _ => { });
    }

    /// <summary>当前界面方向对应的单一方向掩码；取不到时保持控制器现有的声明。</summary>
    private static UIInterfaceOrientationMask CurrentOrientationMask(MikoViewController controller)
    {
        var scene = controller.View?.Window?.WindowScene;
        var current = scene is null
            ? UIInterfaceOrientation.Unknown
            : OperatingSystem.IsIOSVersionAtLeast(16)
                ? scene.EffectiveGeometry.InterfaceOrientation
                : scene.InterfaceOrientation;

        return current switch
        {
            UIInterfaceOrientation.Portrait => UIInterfaceOrientationMask.Portrait,
            UIInterfaceOrientation.PortraitUpsideDown => UIInterfaceOrientationMask.PortraitUpsideDown,
            UIInterfaceOrientation.LandscapeLeft => UIInterfaceOrientationMask.LandscapeLeft,
            UIInterfaceOrientation.LandscapeRight => UIInterfaceOrientationMask.LandscapeRight,
            _ => controller.GetSupportedInterfaceOrientations(),
        };
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
