using CoreAnimation;
using Foundation;
using Miko.Hosting;
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

        _displayLink = CADisplayLink.Create(OnFrame);
        _displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Default);
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
