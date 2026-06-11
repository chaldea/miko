using System.Diagnostics;
using Android.Content;
using Android.Util;
using Android.Views;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using SkiaSharp;
using SkiaSharp.Views.Android;

namespace Miko.Android;

/// <summary>
/// 承载 Miko 渲染引擎的 Android GL 视图。负责：
/// 引擎初始化、按像素密度缩放的渲染、动画/热重载帧推进，以及触摸输入转发。
/// </summary>
public class MikoSurfaceView : SKGLSurfaceView
{
    private readonly MikoAppContext _context;
    private readonly MikoInteractionController _controller;
    private readonly float _density;
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;
    private bool _initialized;

    public MikoSurfaceView(Context context, MikoAppContext appContext) : base(context)
    {
        _context = appContext;
        _controller = appContext.Controller;
        _density = context.Resources?.DisplayMetrics?.Density ?? 1f;
        Log.Info("MikoSurfaceView",
            $"Screen density: {_density}, Physical size: {context.Resources?.DisplayMetrics?.WidthPixels}×{context.Resources?.DisplayMetrics?.HeightPixels}");

        // 连续渲染，使动画与热重载得以推进。
        RenderMode = global::Android.Opengl.Rendermode.Continuously;
    }

    protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float logicalWidth = e.BackendRenderTarget.Width / _density;
        float logicalHeight = e.BackendRenderTarget.Height / _density;

        if (!_initialized)
        {
            _context.RegisterFonts();
            _controller.Initialize(canvas, logicalWidth, logicalHeight);
            _controller.RunPostInitHooks();
            _frameTimer.Start();
            _initialized = true;
        }

        if (_controller.NeedsRebuild)
        {
            _controller.Rebuild(canvas, logicalWidth, logicalHeight);
        }

        float currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        float deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;
        _controller.Update(deltaTime);

        canvas.Clear(SKColors.White);
        canvas.Save();
        canvas.Scale(_density);
        _controller.Engine.Render(canvas);
        canvas.Restore();
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e == null) return base.OnTouchEvent(e);

        // 原生坐标为物理像素，按密度换算为逻辑坐标。
        float x = e.GetX() / _density;
        float y = e.GetY() / _density;

        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                _controller.OnPointerDown(x, y, MouseButton.Left);
                break;
            case MotionEventActions.Move:
                _controller.OnPointerMove(x, y);
                break;
            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                _controller.OnPointerUp(x, y, MouseButton.Left);
                break;
            default:
                return base.OnTouchEvent(e);
        }

        return true;
    }
}
