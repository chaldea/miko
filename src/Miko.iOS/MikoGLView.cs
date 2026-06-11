using System.Diagnostics;
using CoreGraphics;
using Foundation;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using SkiaSharp;
using SkiaSharp.Views.iOS;
using UIKit;

namespace Miko.iOS;

/// <summary>
/// 承载 Miko 渲染引擎的 iOS GL 视图。负责：
/// 引擎初始化、按屏幕缩放系数缩放的渲染、动画/热重载帧推进，以及触摸输入转发。
/// </summary>
/// <remarks>
/// 当前基于 OpenGL ES（<see cref="SKGLView"/>）。Apple 已弃用 OpenGL ES，未来可迁移到
/// 基于 Metal 的 SKMetalView；此处沿用 GL 以与桌面/Android 的 GL 后端保持一致，功能仍可用。
/// </remarks>
#pragma warning disable CA1422 // SKGLView 在 iOS 12+ 标记为过时（建议 Metal），此处有意沿用 GL 后端
public class MikoGLView : SKGLView
{
    private readonly MikoAppContext _context;
    private readonly MikoInteractionController _controller;
    private readonly nfloat _scale;
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;
    private bool _initialized;

    public MikoGLView(MikoAppContext appContext, CGRect frame) : base(frame)
    {
        _context = appContext;
        _controller = appContext.Controller;
        _scale = UIScreen.MainScreen.Scale;
        MultipleTouchEnabled = false;
        PaintSurface += OnPaintSurface;
    }

    private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        // 背景缓冲区为物理像素，逻辑尺寸 = 物理像素 / 缩放系数。
        float logicalWidth = e.BackendRenderTarget.Width / (float)_scale;
        float logicalHeight = e.BackendRenderTarget.Height / (float)_scale;

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

        // RenderFrame holds the input/render lock so touch-driven DOM mutations on the
        // UI thread can't race the layout walk on the render thread.
        _controller.RenderFrame(canvas, logicalWidth, logicalHeight, deltaTime, c =>
        {
            c.Clear(SKColors.White);
            c.Save();
            c.Scale((float)_scale);
            _controller.Engine.Render(c);
            c.Restore();
        });
    }

    // UITouch.LocationInView 返回的是逻辑点（point），与渲染所用的逻辑坐标一致，无需再除以缩放系数。

    public override void TouchesBegan(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
            _controller.OnPointerDown(x, y, MouseButton.Left);
    }

    public override void TouchesMoved(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
            _controller.OnPointerMove(x, y);
    }

    public override void TouchesEnded(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
            _controller.OnPointerUp(x, y, MouseButton.Left);
    }

    public override void TouchesCancelled(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
            _controller.OnPointerUp(x, y, MouseButton.Left);
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
}
#pragma warning restore CA1422
