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
/// 承载 <see cref="MikoGLView"/> 的视图控制器。通过 <see cref="CADisplayLink"/> 按需调度渲染，
/// 使动画与热重载得以推进，同时让静态页面在空闲时停止提交帧。
/// </summary>
public class MikoViewController : UIViewController
{
    private readonly MikoAppContext _context;
    private MikoGLView? _glView;
    private CADisplayLink? _displayLink;

    // 状态栏状态。iOS 的状态栏由视图控制器**声明**而非直接设置：系统会回头询问
    // PreferredStatusBarStyle / PrefersStatusBarHidden，因此 IStatusBarService 把请求
    // 记在这里，再调 SetNeedsStatusBarAppearanceUpdate 触发系统重新询问。
    private UIStatusBarStyle _statusBarStyle = UIStatusBarStyle.Default;
    private bool _statusBarHidden;
    private UIStatusBarAnimation _statusBarAnimation = UIStatusBarAnimation.None;

    public MikoViewController(MikoAppContext context)
    {
        _context = context;
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        _glView = new MikoGLView(_context, View!.Bounds)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
        };
        View!.AddSubview(_glView);

        // 把视图控制器交给 Native 能力层。服务容器在 MikoAppBuilder.Build() 时就已构建，
        // 那时控制器还不存在，因此 iOS Native 服务只能在这里拿到宿主（延迟注入）。
        _context.Services.GetService<INativeHostContext>()?.Attach(new IosNativeHost(this));

        _displayLink = CADisplayLink.Create(OnFrame);
        _displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Default);
    }

    /// <inheritdoc />
    public override UIStatusBarStyle PreferredStatusBarStyle() => _statusBarStyle;

    /// <inheritdoc />
    public override bool PrefersStatusBarHidden() => _statusBarHidden;

    /// <inheritdoc />
    public override UIStatusBarAnimation PreferredStatusBarUpdateAnimation => _statusBarAnimation;

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

    private void OnFrame()
    {
        if (_context.Controller.HasPendingWork)
            _glView?.SetNeedsDisplay();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _displayLink?.Invalidate();
            _displayLink?.Dispose();
            _displayLink = null;
        }
        base.Dispose(disposing);
    }
}
