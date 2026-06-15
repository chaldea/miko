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

        // 接收系统窗口 inset 以便计算安全区（edge-to-edge 下系统栏会覆盖内容）。
        SetFitsSystemWindows(false);
    }

    /// <summary>
    /// 系统窗口 inset 变化（首次 attach、旋转、系统栏显隐）时回调。读取状态栏/导航栏
    /// 的 inset（物理像素），换算为逻辑像素后推给引擎作为安全区，使内容不被系统 UI 遮盖。
    /// </summary>
    public override WindowInsets? OnApplyWindowInsets(WindowInsets? insets)
    {
        if (insets != null)
        {
            int left, top, right, bottom;

            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.R)
            {
                // API 30+：用类型化 inset（状态栏 + 导航栏 + 刘海）。
                var bars = insets.GetInsets(
                    WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
                left = bars.Left;
                top = bars.Top;
                right = bars.Right;
                bottom = bars.Bottom;
            }
            else
            {
                // API 21–29：回退到已废弃的 system-window inset。
#pragma warning disable CA1422 // 旧 API 在新平台标记过时，此处为向后兼容有意调用
                left = insets.SystemWindowInsetLeft;
                top = insets.SystemWindowInsetTop;
                right = insets.SystemWindowInsetRight;
                bottom = insets.SystemWindowInsetBottom;
#pragma warning restore CA1422
            }

            _controller.SetSafeAreaInsets(left / _density, top / _density, right / _density, bottom / _density);
        }

        return base.OnApplyWindowInsets(insets);
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

        float currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        float deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        // RenderFrame holds the input/render lock so touch-driven DOM mutations on the
        // UI thread can't race the layout walk on this GL thread.
        _controller.RenderFrame(canvas, logicalWidth, logicalHeight, deltaTime, c =>
        {
            // 用根背景色填充整个 surface，使安全区内的系统栏带与内容背景一致（而非白边）。
            var rootBg = _controller.Engine.GetRootBackgroundColor();
            c.Clear(rootBg?.ToSKColor() ?? SKColors.White);
            c.Save();
            c.Scale(_density);
            _controller.Engine.Render(c);
            c.Restore();
        });
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
