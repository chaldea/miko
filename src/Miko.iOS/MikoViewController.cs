using CoreAnimation;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Native;
using Miko.iOS.Native;
using ObjCRuntime;
using UIKit;

namespace Miko.iOS;

/// <summary>
/// 承载 <see cref="MikoMetalView"/> 的视图控制器。通过 <see cref="CADisplayLink"/> 按需调度渲染，
/// 使动画与热重载得以推进，同时让静态页面在空闲时停止提交帧。
/// </summary>
public class MikoViewController : UIViewController
{
    private readonly MikoAppContext _context;
    private MikoMetalView? _metalView;
    private CADisplayLink? _displayLink;

    // 状态栏状态。iOS 的状态栏由视图控制器**声明**而非直接设置：系统会回头询问
    // PreferredStatusBarStyle / PrefersStatusBarHidden，因此 IStatusBarService 把请求
    // 记在这里，再调 SetNeedsStatusBarAppearanceUpdate 触发系统重新询问。
    private UIStatusBarStyle _statusBarStyle = UIStatusBarStyle.Default;
    private bool _statusBarHidden;
    private UIStatusBarAnimation _statusBarAnimation = UIStatusBarAnimation.None;

    // 沉浸式全屏（ISystemBarsService）：与 IStatusBarService 的显隐分开记，任一方要求隐藏状态栏就隐藏，
    // 全屏结束时不会把应用自己隐藏的状态栏又显示出来。
    private bool _systemBarsHidden;

    // 允许的屏幕方向。iOS 同样由视图控制器**声明**：IosOrientationService 请求旋转时，
    // 系统会回头询问本属性，若目标方向不在此集合内则请求被忽略（ISSUE-138）。
    // 默认全放开，与 iOS 应用的常规行为一致；锁定期间由 SetSupportedOrientations 收窄。
    private UIInterfaceOrientationMask _supportedOrientations = UIInterfaceOrientationMask.All;

    public MikoViewController(MikoAppContext context)
    {
        _context = context;
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        _metalView = new MikoMetalView(_context, View!.Bounds)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
        };
        View!.AddSubview(_metalView);

        // 把视图控制器交给 Native 能力层。服务容器在 MikoAppBuilder.Build() 时就已构建，
        // 那时控制器还不存在，因此 iOS Native 服务只能在这里拿到宿主（延迟注入）。
        _context.Services.GetService<INativeHostContext>()?.Attach(new IosNativeHost(this));

        _displayLink = CADisplayLink.Create(OnFrame);
        _displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Default);

        // 应用生命周期（Release-1.0.0 A1）。iOS 禁止后台提交 GPU 命令：进入后台先停掉
        // CADisplayLink 并让引擎暂停视频，回到前台再恢复；内存告警时清掉 Skia 的 GPU 缓存。
        // UIApplication 的这几个通知在 Scene 生命周期下照样发出（最后一个场景进入后台时）。
        _lifecycleObservers =
        [
            UIApplication.Notifications.ObserveDidEnterBackground((_, _) => OnDidEnterBackground()),
            UIApplication.Notifications.ObserveWillEnterForeground((_, _) => OnWillEnterForeground()),
            UIApplication.Notifications.ObserveDidReceiveMemoryWarning((_, _) => OnMemoryWarning()),
            // 软键盘（Release-1.0.0 A2）：frame 变化覆盖弹出、收起、切换输入法与悬浮键盘。
            UIKeyboard.Notifications.ObserveWillChangeFrame((_, e) => OnKeyboardFrameChange(e.FrameEnd)),
        ];
    }

    /// <summary>
    /// 键盘盖住视图底部的高度（逻辑点）推给引擎，布局排到键盘上方。frame 是屏幕坐标，先换算到本视图；
    /// 收起、悬浮（iPad 浮动键盘）或外接键盘时与视图不相交，高度为 0。
    /// </summary>
    private void OnKeyboardFrameChange(CoreGraphics.CGRect keyboardFrameInScreen)
    {
        var view = View;
        if (view is null) return;
        var frame = view.ConvertRectFromCoordinateSpace(keyboardFrameInScreen, UIScreen.MainScreen.CoordinateSpace);
        var overlap = frame.IntersectsWith(view.Bounds) && frame.Width >= view.Bounds.Width
            ? (float)Math.Max(0, view.Bounds.Bottom - frame.Top)
            : 0f;
        _context.Controller.SetKeyboardInset(overlap);
        _metalView?.SetNeedsDisplay();
    }

    private NSObject[] _lifecycleObservers = [];

    private void OnDidEnterBackground()
    {
        if (_displayLink is not null) _displayLink.Paused = true;
        _context.Controller.Suspend();
    }

    private void OnWillEnterForeground()
    {
        _context.Controller.Resume();
        if (_displayLink is not null) _displayLink.Paused = false;
        _metalView?.SetNeedsDisplay();
    }

    private void OnMemoryWarning()
    {
        // 通知在主线程发出，Metal 视图也在主线程绘制，可以直接清理它的 GPU 上下文。
        _metalView?.TrimGpuResources();
    }

    /// <inheritdoc />
    public override UIStatusBarStyle PreferredStatusBarStyle() => _statusBarStyle;

    /// <inheritdoc />
    public override bool PrefersStatusBarHidden() => _statusBarHidden || _systemBarsHidden;

    /// <inheritdoc />
    public override bool PrefersHomeIndicatorAutoHidden => _systemBarsHidden;

    /// <inheritdoc />
    public override UIStatusBarAnimation PreferredStatusBarUpdateAnimation => _statusBarAnimation;

    /// <inheritdoc />
    public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations() => _supportedOrientations;

    /// <inheritdoc />
    public override bool ShouldAutorotate() => true;

    /// <summary>
    /// 由 <c>IosOrientationService</c> 调用以收窄/放开允许的屏幕方向。必须在主线程调用。
    /// 收窄后需 <c>SetNeedsUpdateOfSupportedInterfaceOrientations</c>，否则系统不会重新询问。
    /// </summary>
    internal void SetSupportedOrientations(UIInterfaceOrientationMask mask)
    {
        _supportedOrientations = mask;
        SetNeedsUpdateOfSupportedInterfaceOrientations();
    }

    /// <summary>由 <c>IStatusBarService</c> 调用以更改状态栏文字样式。必须在主线程调用。</summary>
    internal void SetStatusBarStyle(UIStatusBarStyle style)
    {
        _statusBarStyle = style;
        SetNeedsStatusBarAppearanceUpdate();
    }

    /// <summary>由 <c>IStatusBarService</c> 调用以显隐状态栏。必须在主线程调用。</summary>
    internal void SetStatusBarHidden(bool hidden, UIStatusBarAnimation animation)
    {
        _statusBarHidden = hidden;
        _statusBarAnimation = animation;

        // 动画由 PreferredStatusBarUpdateAnimation 驱动，UIKit 在收到本次更新请求时读取它；
        // 不需要（也不能）自己包一层 UIView.Animate——SetNeedsStatusBarAppearanceUpdate 只是
        // 打个标记，包在动画块里没有任何属性可插值，Slide 反而会变成瞬间切换。
        SetNeedsStatusBarAppearanceUpdate();
    }

    /// <summary>
    /// 由 <c>IosSystemBarsService</c> 调用以进入/退出沉浸式全屏：隐藏状态栏，Home 指示条在无触摸时自动隐藏
    /// （iOS 不允许应用彻底隐藏它）。必须在主线程调用。
    /// </summary>
    internal void SetSystemBarsHidden(bool hidden)
    {
        _systemBarsHidden = hidden;
        SetNeedsStatusBarAppearanceUpdate();
        SetNeedsUpdateOfHomeIndicatorAutoHidden();
    }

    private void OnFrame()
    {
        if (_context.Controller.HasPendingWork)
            _metalView?.SetNeedsDisplay();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var observer in _lifecycleObservers)
                observer.Dispose();
            _lifecycleObservers = [];
            _displayLink?.Invalidate();
            _displayLink?.Dispose();
            _displayLink = null;
        }
        base.Dispose(disposing);
    }
}
