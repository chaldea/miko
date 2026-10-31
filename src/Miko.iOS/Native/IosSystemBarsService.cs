using Miko.Native;
using Miko.Platform;

namespace Miko.iOS.Native;

/// <summary>
/// iOS 的系统栏显隐（沉浸式全屏），供视频全屏等交互使用。
///
/// <para>
/// iOS 的系统栏由视图控制器<b>声明</b>：隐藏期间宿主 <see cref="MikoViewController"/> 的
/// <c>PrefersStatusBarHidden</c> 为真、<c>PrefersHomeIndicatorAutoHidden</c> 为真——状态栏消失，
/// Home 指示条在一段时间无触摸后自动淡出（应用无权彻底隐藏它）。刘海 / 灵动岛的安全区不变，
/// 页面照常用 <c>env(safe-area-inset-*)</c> 避开。
/// </para>
///
/// <para>
/// 引用计数，在主线程修改控制器；宿主缺失时静默跳过——沉浸式是交互增强，组件不该因此崩。
/// </para>
/// </summary>
internal sealed class IosSystemBarsService : ISystemBarsService
{
    private readonly INativeHostContext _hostContext;
    private readonly object _sync = new();
    private int _hideDepth;

    public IosSystemBarsService(INativeHostContext hostContext) => _hostContext = hostContext;

    public void Hide()
    {
        lock (_sync)
        {
            _hideDepth++;
            if (_hideDepth != 1) return;
        }

        Apply(true);
    }

    public void Show()
    {
        lock (_sync)
        {
            if (_hideDepth == 0) return;
            _hideDepth--;
            if (_hideDepth != 0) return;
        }

        Apply(false);
    }

    private void Apply(bool hidden)
    {
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            if ((_hostContext.Host as IosNativeHost)?.ViewController is MikoViewController controller)
                controller.SetSystemBarsHidden(hidden);
        });
    }
}
