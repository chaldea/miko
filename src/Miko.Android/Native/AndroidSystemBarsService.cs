using Android.Views;
using Miko.Native;
using Miko.Platform;

namespace Miko.Android.Native;

/// <summary>
/// Android 的系统栏显隐（沉浸式全屏），供视频全屏等交互使用。
///
/// <para>
/// 隐藏期间状态栏与导航栏都不占屏，从屏幕边缘滑动可临时呼出（API 30+ 的
/// <c>BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE</c>，旧版本的 <c>IMMERSIVE_STICKY</c>）；窗口同时延伸进
/// 刘海 / 挖孔区（<c>LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES</c>）——否则 API 34 及以下横屏时
/// 挖孔那一侧会留一条系统画的黑边。targetSdk 35+ 的应用在 Android 15 上本就是 edge-to-edge，
/// 这一步对它们是空操作。
/// </para>
///
/// <para>
/// 系统栏隐藏后系统重新下发 inset：<see cref="MikoSurfaceView"/> 据此把安全区更新为只含刘海，
/// 页面用 <c>env(safe-area-inset-*)</c> 避开刘海即可。
/// </para>
///
/// <para>
/// 与其它组件面向的服务一样，<c>Activity</c> 在调用时才读取（宿主构造时 <c>Attach</c>），
/// 写操作切到 UI 线程，宿主缺失时静默跳过。全部解除时还原隐藏前的刘海模式与标志位。
/// </para>
/// </summary>
internal sealed class AndroidSystemBarsService : ISystemBarsService
{
    // API 26–29 的沉浸式标志位：隐藏两条栏、布局延伸到它们下面、滑动只临时呼出。
#pragma warning disable CA1422 // 这组标志在 API 30 起过时，只在更低版本上使用
    private const SystemUiFlags ImmersiveFlags = SystemUiFlags.ImmersiveSticky | SystemUiFlags.Fullscreen
        | SystemUiFlags.HideNavigation | SystemUiFlags.LayoutStable | SystemUiFlags.LayoutFullscreen
        | SystemUiFlags.LayoutHideNavigation;
#pragma warning restore CA1422

    private readonly INativeHostContext _hostContext;
    private readonly object _sync = new();
    private int _hideDepth;

    // 以下只在 UI 线程读写：隐藏前的刘海布局模式，以及（API 30 以下）由本服务新加上的标志位。
    // 只撤掉自己加的位：宿主每帧按背景亮度改写 Light* 位，整体还原旧值会把它们一起改回去。
    private LayoutInDisplayCutoutMode? _restoreCutoutMode;
    private SystemUiFlags _addedFlags;

    public AndroidSystemBarsService(INativeHostContext hostContext) => _hostContext = hostContext;

    public void Hide()
    {
        lock (_sync)
        {
            _hideDepth++;
            if (_hideDepth != 1) return;
        }

        RunOnUiThread(window => Apply(window, hidden: true));
    }

    public void Show()
    {
        lock (_sync)
        {
            if (_hideDepth == 0) return;
            _hideDepth--;
            if (_hideDepth != 0) return;
        }

        RunOnUiThread(window => Apply(window, hidden: false));
    }

    private void Apply(Window window, bool hidden)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(28) && window.Attributes is { } attributes)
        {
            if (hidden)
            {
                _restoreCutoutMode ??= attributes.LayoutInDisplayCutoutMode;
                attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
            else
            {
                attributes.LayoutInDisplayCutoutMode = _restoreCutoutMode ?? LayoutInDisplayCutoutMode.Default;
                _restoreCutoutMode = null;
            }
            // LayoutParams 是值的副本：写回 Window 才生效。
            window.Attributes = attributes;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var controller = window.InsetsController;
            if (controller is null) return;
            if (hidden)
            {
                controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                controller.Hide(WindowInsets.Type.SystemBars());
            }
            else
            {
                controller.Show(WindowInsets.Type.SystemBars());
            }
        }
        else
        {
#pragma warning disable CA1422
            var decor = window.DecorView;
            if (hidden)
            {
                _addedFlags = ImmersiveFlags & ~decor.SystemUiFlags;
                decor.SystemUiFlags |= ImmersiveFlags;
            }
            else
            {
                decor.SystemUiFlags &= ~_addedFlags;
                _addedFlags = 0;
            }
#pragma warning restore CA1422
        }
    }

    private void RunOnUiThread(Action<Window> action)
    {
        // 宿主可能尚未 Attach（视图未挂载）；沉浸式是增强能力，静默跳过而非抛错。
        var activity = (_hostContext.Host as AndroidNativeHost)?.Activity;
        if (activity is null) return;

        if (global::Android.OS.Looper.MyLooper() == global::Android.OS.Looper.MainLooper) Run();
        else activity.RunOnUiThread(Run);

        void Run()
        {
            if (activity.Window is { } window) action(window);
        }
    }
}
