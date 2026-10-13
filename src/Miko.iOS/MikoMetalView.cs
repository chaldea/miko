using System.Diagnostics;
using CoreGraphics;
using Foundation;
using Metal;
using MetalKit;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using Miko.Rendering;
using SkiaSharp;
using UIKit;

namespace Miko.iOS;

/// <summary>
/// 承载 Miko 渲染引擎的 iOS Metal 视图。负责：
/// 引擎初始化、按屏幕缩放系数缩放的渲染、动画/热重载帧推进，以及触摸输入转发。
/// </summary>
/// <remarks>
/// <para>基于 Metal（<see cref="MTKView"/> + Skia 的 Metal 后端）。此前的 OpenGL ES 宿主在 iOS
/// 模拟器里只能跑在软件渲染器（<c>Apple Software Renderer</c>）上，帧率只有 1–6 FPS；Metal 在模拟器里
/// 直接使用宿主 Mac 的 GPU，在真机上也是 Apple 唯一仍在维护的图形 API（ISSUE-147）。</para>
///
/// <para>没有继承 SkiaSharp 的 <c>SKMetalView</c>：它在模拟器上强制 4× MSAA、为 Skia 用不到的深度/模板
/// 缓冲分配整屏纹理，而且不暴露命令队列——视频帧的原生纹理必须等本帧命令缓冲执行完才能释放
/// （见 <see cref="MetalFrameResources"/>）。因此设备、命令队列与 <see cref="GRContext"/> 都由本类持有。</para>
///
/// <para>按需出帧：视图保持暂停，只在 <see cref="UIView.SetNeedsDisplay"/> 之后的显示周期绘制一次
/// （由 <see cref="MikoViewController"/> 的 CADisplayLink 与触摸事件请求），静止页面不占用 GPU。</para>
/// </remarks>
public class MikoMetalView : MTKView, IMTKViewDelegate, IUIKeyInput
{
    private readonly MikoAppContext _context;
    private readonly MikoInteractionController _controller;
    private readonly IMTLCommandQueue _queue;
    private readonly GRContext _grContext;
    private readonly Action<SKCanvas> _paint;
    private readonly Stopwatch _frameTimer = new();
    private float _scale;
    private float _lastFrameTime;
    private bool _initialized;
    private readonly IosInputMethod _inputMethod;

    public MikoMetalView(MikoAppContext appContext, CGRect frame)
        : base(frame, MTLDevice.SystemDefault
            ?? throw new PlatformNotSupportedException("Metal is not available on this device."))
    {
        _context = appContext;
        _controller = appContext.Controller;
        _inputMethod = new IosInputMethod(this, _controller);
        _controller.AttachInputMethod(_inputMethod);
        // Touch scrolling needs inertia; the default friction already matches UIScrollView's
        // normal deceleration rate. Only fill in the engine default so an app that registered
        // its own IScrollBehavior wins.
        // 惯性外再裹一层橡皮筋（ISSUE-135）：样式默认 overscroll-effect:none 时它是纯透传，
        // 作者写一行 Elastic 即可在真机拿到边界拉伸回弹。
        if (_controller.ScrollBehavior is DefaultScrollBehavior)
            _controller.SetScrollBehavior(new ElasticScrollBehavior(new InertialScrollBehavior()));

        var device = Device!;
        _queue = device.CreateCommandQueue()
            ?? throw new InvalidOperationException("Failed to create a Metal command queue.");
        using var backend = new GRMtlBackendContext { Device = device, Queue = _queue };
        _grContext = GRContext.CreateMetal(backend)
            ?? throw new InvalidOperationException("Failed to create a Skia Metal context.");
        GpuResourceCache.Configure(_grContext);
        // 上下文随视图存活、不会重建，交给引擎一次即可（供视频帧源零拷贝包装解码纹理）。
        _controller.Engine.GraphicsContext = _grContext;

        // Skia 直接画进可绘制纹理：单采样（抗锯齿由 Skia 完成，模拟器与真机一致），不要深度/模板缓冲
        // （Skia 需要时自行分配）。FramebufferOnly=false 让 Skia 在需要读回目标的混合模式下仍可采样它。
        ColorPixelFormat = MTLPixelFormat.BGRA8Unorm;
        DepthStencilPixelFormat = MTLPixelFormat.Invalid;
        SampleCount = 1;
        FramebufferOnly = false;
        // 可绘制纹理按 Bounds × ContentScaleFactor 分配，显式对齐屏幕物理像素。
        ContentScaleFactor = UIScreen.MainScreen.Scale;
        Paused = true;
        EnableSetNeedsDisplay = true;
        Delegate = this;
        MultipleTouchEnabled = false;
        _paint = PaintFrame;
    }

    /// <summary>
    /// 安全区变化（刘海、Home 指示条、旋转）时回调。<see cref="UIView.SafeAreaInsets"/>
    /// 已是逻辑点，与渲染坐标一致，直接推给引擎作为安全区，使内容不被系统 UI 遮盖。
    /// </summary>
    public override void SafeAreaInsetsDidChange()
    {
        base.SafeAreaInsetsDidChange();
        var insets = SafeAreaInsets;
        _controller.SetSafeAreaInsets(
            (float)insets.Left, (float)insets.Top, (float)insets.Right, (float)insets.Bottom);
    }

    void IMTKViewDelegate.DrawableSizeWillChange(MTKView view, CGSize size)
    {
        // 视图暂停时尺寸变化（旋转、分屏）不会自动重绘，主动请求一帧，避免旧画面被拉伸停留。
        SetNeedsDisplay();
    }

    void IMTKViewDelegate.Draw(MTKView view)
    {
        // 可绘制纹理全部在 GPU 上排队时，CurrentDrawable 会等待其中一块空出来（GPU 背压）。
        var drawable = CurrentDrawable;
        var texture = drawable?.Texture;
        if (drawable == null || texture == null) return;

        int width = (int)texture.Width;
        int height = (int)texture.Height;
        if (width <= 0 || height <= 0) return;

        // CAMetalLayer 每帧轮换可绘制纹理，包装对象随帧新建、随帧释放。
        using var target = new GRBackendRenderTarget(width, height, new GRMtlTextureInfo(texture));
        using var surface = SKSurface.Create(_grContext, target, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888);
        if (surface == null) return;

        RenderFrame(surface.Canvas, width, height);

        // 先提交 Skia 本帧的命令，再在同一队列上用一个命令缓冲呈现。
        _grContext.Flush();
        var commandBuffer = _queue.CommandBuffer();
        if (commandBuffer == null) return;
        using (commandBuffer)
        {
            // 本帧登记的视频纹理在呈现缓冲完成（即 Skia 本帧命令已执行完）后释放。
            var retained = MetalFrameResources.TakePending();
            if (retained != null)
            {
                commandBuffer.AddCompletedHandler(_ =>
                {
                    foreach (var resource in retained)
                        resource.Dispose();
                });
            }

            commandBuffer.PresentDrawable(drawable);
            commandBuffer.Commit();
        }
    }

    private void RenderFrame(SKCanvas canvas, int pixelWidth, int pixelHeight)
    {
        // 可绘制纹理为物理像素，逻辑尺寸 = 物理像素 / 缩放系数。
        _scale = (float)ContentScaleFactor;
        float logicalWidth = pixelWidth / _scale;
        float logicalHeight = pixelHeight / _scale;

        if (!_initialized)
        {
            _context.RegisterFonts();
            _controller.Initialize(canvas, logicalWidth, logicalHeight);
            _controller.RunPostInitHooks();
            _frameTimer.Start();
            _initialized = true;
        }

        float currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        float deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        // RenderFrame holds the input/render lock so touch-driven DOM mutations can't race
        // the layout walk.
        _controller.RenderFrame(canvas, logicalWidth, logicalHeight, deltaTime, _paint);
    }

    private void PaintFrame(SKCanvas canvas)
    {
        // 用根背景色填充整个 surface，使安全区内的系统栏带与内容背景一致（而非白边）。
        var rootBg = _controller.Engine.GetRootBackgroundColor();
        canvas.Clear(rootBg?.ToSKColor() ?? SKColors.White);
        canvas.Save();
        canvas.Scale(_scale);
        _controller.Engine.Render(canvas);
        canvas.Restore();
    }

    // UITouch.LocationInView 返回的是逻辑点（point），与渲染所用的逻辑坐标一致，无需再除以缩放系数。

    public override void TouchesBegan(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
        {
            _controller.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);
            SetNeedsDisplay();
        }
    }

    public override void TouchesMoved(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
        {
            _controller.OnPointerMove(x, y);
            if (_controller.HasPendingWork) SetNeedsDisplay();
        }
    }

    public override void TouchesEnded(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
        {
            _controller.OnPointerUp(x, y, MouseButton.Left, PointerType.Touch);
            SetNeedsDisplay();
        }
    }

    public override void TouchesCancelled(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
        {
            _controller.OnPointerCancel(x, y);
            SetNeedsDisplay();
        }
    }

    private bool TryGetPoint(NSSet touches, out float x, out float y)
    {
        x = 0;
        y = 0;
        if (touches.AnyObject is not UITouch touch) return false;
        var location = touch.LocationInView(this);
        x = (float)location.X;
        y = (float)location.Y;
        return true;
    }

    public override bool CanBecomeFirstResponder => true;

    public bool HasText => !string.IsNullOrEmpty(_inputMethod.Text);

    public void InsertText(string text) => _inputMethod.Commit(text);

    public void DeleteBackward()
        => _controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 视频帧源持有本上下文上的 GPU 图像，须先于上下文释放（与桌面宿主关闭时的顺序一致）。
            _controller.Engine.DisposeVideoSessions();
            _controller.Engine.GraphicsContext = null;
            GpuResourceCache.PurgeAllResources(_grContext);
            _grContext.Dispose();
            _queue.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed class IosInputMethod : InputMethodBase
    {
        private readonly MikoMetalView _view;
        private readonly MikoInteractionController _controller;
        private InputMethodState? _state;

        public IosInputMethod(MikoMetalView view, MikoInteractionController controller)
        {
            _view = view;
            _controller = controller;
        }

        public string Text => _state?.Text ?? string.Empty;

        public override void SetState(InputMethodState? state)
        {
            _state = state;
            UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
            {
                if (state == null)
                    _view.ResignFirstResponder();
                else
                    _view.BecomeFirstResponder();
            });
        }

        /// <summary>
        /// iOS 上"成为第一响应者"即等于唤起键盘。用户下拉收起键盘后会自动放弃第一响应者身份，
        /// 因此这里无条件重新申请即可再次弹出。
        /// </summary>
        public override void ShowKeyboard()
        {
            UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
            {
                if (_state == null) return;
                _view.BecomeFirstResponder();
            });
        }

        public void Commit(string text) => CommitText(text);
        public void Begin() => StartComposition();
        public void Update(string text) => UpdateComposition(text);
        public void End(string? text = null) => EndComposition(text);
    }
}
