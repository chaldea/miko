using System.Diagnostics;
using Android.Content;
using Android.Util;
using Android.Views;
using Miko.Common;
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

    // 用于在每帧根据根背景色亮度调整状态栏/导航栏图标外观（深/浅）。
    // 持有 Activity 引用以便 Post 到 UI 线程修改 Window；上一次应用的“浅色背景”判定
    // 用于去抖，避免每帧都触发跨线程调用。
    // 完整限定 Activity 类型——本文件 using 了 System.Diagnostics（Stopwatch），与
    // System.Diagnostics.Activity 同名冲突。
    private readonly global::Android.App.Activity? _activity;
    private bool? _lastLightAppearance;

    public MikoSurfaceView(Context context, MikoAppContext appContext) : base(context)
    {
        _context = appContext;
        _controller = appContext.Controller;
        _activity = context as global::Android.App.Activity;
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

            // 根据根背景色亮度切换系统栏图标颜色。浅色背景下用深色图标（避免“白底白字”
            // 看不见状态栏内容，见 ISSUE-056），深色背景下用浅色图标。无背景时回退到浅色
            // 背景规则（与 surface 清屏的白色保持一致）。
            SyncSystemBarAppearance(rootBg ?? Color.White);
        });
    }

    /// <summary>
    /// 根据给定背景色亮度切换状态栏 / 导航栏图标外观（深/浅）。
    /// 该调用必须在 UI 线程上执行（修改 Window/View 系统 UI 标志），因此从 GL 线程通过
    /// <see cref="global::Android.App.Activity.RunOnUiThread(Action)"/> 转发；并按上一次
    /// 应用的判定去抖，避免每帧都跨线程调度。
    /// </summary>
    private void SyncSystemBarAppearance(Color background)
    {
        if (_activity == null) return;

        // 透明背景时把它视作浅色（与 GL clear 的白色回退一致）。
        bool useDarkIcons = background.A == 0 || IsLight(background);
        if (_lastLightAppearance == useDarkIcons) return;
        _lastLightAppearance = useDarkIcons;

        _activity.RunOnUiThread(() =>
        {
            var window = _activity.Window;
            if (window == null) return;

            // 各分支均已通过 Build.VERSION.SdkInt 守卫；分析器无法跨匿名方法跟踪平台版本，
            // 故在此处显式抑制 CA1416/CA1422（与本文件 OnApplyWindowInsets 中相同模式）。
#pragma warning disable CA1416, CA1422
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.R)
            {
                // API 30+：通过 WindowInsetsController 切换系统栏外观。
                // LightStatusBars / LightNavigationBars = 浅色背景下使用深色图标。
                var controller = window.InsetsController;
                if (controller == null) return;

                var mask = WindowInsetsControllerAppearance.LightStatusBars
                         | WindowInsetsControllerAppearance.LightNavigationBars;
                var appearance = useDarkIcons ? mask : WindowInsetsControllerAppearance.None;
                controller.SetSystemBarsAppearance((int)appearance, (int)mask);
            }
            else if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.M)
            {
                // API 23–29：通过 DecorView 的 SystemUiFlags 标志切换。
                // Android O+ (API 26) 起还支持 LightNavigationBar；早于 O 时仅切换状态栏。
                var decor = window.DecorView;
                var flags = decor.SystemUiFlags;

                if (useDarkIcons) flags |= SystemUiFlags.LightStatusBar;
                else flags &= ~SystemUiFlags.LightStatusBar;

                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                {
                    if (useDarkIcons) flags |= SystemUiFlags.LightNavigationBar;
                    else flags &= ~SystemUiFlags.LightNavigationBar;
                }

                decor.SystemUiFlags = flags;
            }
#pragma warning restore CA1416, CA1422
        });
    }

    /// <summary>
    /// 用 sRGB 相对亮度公式判定颜色是否“浅”——结果用于决定状态栏图标颜色。
    /// 阈值 0.5 与 Material 设计指南一致；alpha 不参与（系统栏的内容色仅取决于其后方
    /// surface 的实际着色，alpha 已在调用前被处理）。
    /// </summary>
    private static bool IsLight(Color c)
    {
        // 0.299*R + 0.587*G + 0.114*B 是 ITU-R BT.601 的快速近似（避免 sRGB 解码），
        // 用于浅/深判定足够稳健。
        float luminance = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;
        return luminance > 0.5f;
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
