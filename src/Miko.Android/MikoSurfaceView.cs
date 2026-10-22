using System.Diagnostics;
using Android.Content;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Java.Lang;
using Microsoft.Extensions.DependencyInjection;
using Miko.Android.Native;
using Miko.Common;
using Miko.Events;
using Miko.Hosting;
using Miko.Native;
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
    // UI 线程写（OnConfigurationChanged），GL 线程与触摸回调读。
    private volatile float _density;
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;
    private bool _initialized;
    private readonly AndroidInputMethod _inputMethod;

    // 用于在每帧根据根背景色亮度调整状态栏/导航栏图标外观（深/浅）。
    // 持有 Activity 引用以便 Post 到 UI 线程修改 Window；上一次应用的“浅色背景”判定
    // 用于去抖，避免每帧都触发跨线程调用。
    // 完整限定 Activity 类型——本文件 using 了 System.Diagnostics（Stopwatch），与
    // System.Diagnostics.Activity 同名冲突。
    private readonly global::Android.App.Activity? _activity;
    private bool? _lastLightAppearance;

    // 预测性返回（API 33+）：组件的返回处理链，以及按需向系统注册的回调（见 HandleBackPressed）。
    private readonly BackButtonRegistry? _backRegistry;
    private BackInvokedCallback? _backCallback;
    private bool _backCallbackRegistered;

    // 渲染线程的动作队列。本视图按需出帧（WhenDirty），而队列只在帧里排空：空闲时投递进来的动作
    // （异步加载的续延、计时器回调）若不主动要一帧，就会一直等到下一次触摸。
    private readonly MikoDispatcher? _dispatcher;

    /// <summary>
    /// 本视图提供给 Native 能力层的 Android 宿主。相机、相册等能力要
    /// <c>startActivityForResult</c>，宿主 Activity 必须把 <c>OnActivityResult</c> 转发给
    /// <see cref="AndroidNativeHost.ActivityResults"/>（见 <see cref="MikoAndroidApp.HandleActivityResult"/>）。
    /// </summary>
    public AndroidNativeHost NativeHost { get; }

    public MikoSurfaceView(Context context, MikoAppContext appContext) : base(context)
    {
        _context = appContext;
        _controller = appContext.Controller;
        _inputMethod = new AndroidInputMethod(this, _controller);
        _controller.AttachInputMethod(_inputMethod);
        // Android scrolling is finger-driven, so it needs inertia the desktop wheel path does not.
        // Only fill in the engine default — an app that registered its own IScrollBehavior wins.
        // The friction is larger than the iOS default: Android's fling comes to rest noticeably
        // sooner than UIScrollView's long glide. The value is a hand-tuned approximation of that
        // feel, not a constant derived from Android's Scroller.
        // 惯性外再裹一层橡皮筋（ISSUE-135）：样式默认 overscroll-effect:none 时它是纯透传，
        // 作者写一行 Elastic 即可在真机拿到边界拉伸回弹。
        if (_controller.ScrollBehavior is DefaultScrollBehavior)
            _controller.SetScrollBehavior(new ElasticScrollBehavior(new InertialScrollBehavior(friction: 4.0f)));
        _activity = context as global::Android.App.Activity;

        // 把 Android 宿主交给 Native 能力层。服务容器在 MikoAppBuilder.Build() 时就已构建，
        // 那时 Activity 还不存在，因此 Native 服务只能在这里拿到宿主（延迟注入）。
        NativeHost = new AndroidNativeHost(context, _activity);

        _backRegistry = appContext.Services.GetService<IPlatformService>()?.Back;
        _dispatcher = appContext.Services.GetService<MikoDispatcher>();

        var hostContext = appContext.Services.GetService<INativeHostContext>();

        // 旋转会销毁并重建 Activity，于是这里换上新宿主。挂在旧 relay 上的 startActivityForResult
        // 再也等不到结果（结果会送到新 Activity），必须作废掉，否则调用方永远挂起。
        (hostContext?.Host as AndroidNativeHost)?.ActivityResults.CancelAll();

        hostContext?.Attach(NativeHost);

        _density = context.Resources?.DisplayMetrics?.Density ?? 1f;
        Log.Info("MikoSurfaceView",
            $"Screen density: {_density}, Physical size: {context.Resources?.DisplayMetrics?.WidthPixels}×{context.Resources?.DisplayMetrics?.HeightPixels}");

        // Render only when the engine has visual work. The frame callback requests the next
        // render while an animation, invalidation, or queued callback is active.
        RenderMode = global::Android.Opengl.Rendermode.WhenDirty;

        // 接收系统窗口 inset 以便计算安全区（edge-to-edge 下系统栏会覆盖内容）。
        SetFitsSystemWindows(false);
        Focusable = true;
        FocusableInTouchMode = true;
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

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
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
                // API 26–29：回退到已废弃的 system-window inset。
#pragma warning disable CA1422 // 旧 API 在新平台标记过时，此处为向后兼容有意调用
                left = insets.SystemWindowInsetLeft;
                top = insets.SystemWindowInsetTop;
                right = insets.SystemWindowInsetRight;
                bottom = insets.SystemWindowInsetBottom;
#pragma warning restore CA1422
            }

            _controller.SetSafeAreaInsets(left / _density, top / _density, right / _density, bottom / _density);
            // WhenDirty 渲染：inset 变化不一定伴随 surface 尺寸变化（改密度、系统栏显隐），
            // 不主动请求一帧的话，新的安全区要等下一次触摸才画出来。
            RequestRender();
        }

        return base.OnApplyWindowInsets(insets);
    }

    protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float logicalWidth = e.BackendRenderTarget.Width / _density;
        float logicalHeight = e.BackendRenderTarget.Height / _density;

        // 把 GPU 上下文交给引擎，供视频帧源零拷贝包装解码纹理（对齐桌面宿主）。
        // SKGLSurfaceView 每帧提供 surface，其 Context 在上下文重建后可能变化，故每帧同步。
        _controller.Engine.GraphicsContext = e.Surface.Context as GRContext;

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

        if (_controller.HasPendingWork)
            RequestRender();
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

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
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
            else
            {
                // API 26–29（最低支持 API 26）：通过 DecorView 的 SystemUiFlags 标志切换。
                // 这组标志在 API 30 起标记过时，此分支只在更低版本上执行。
#pragma warning disable CA1422
                var decor = window.DecorView;
                var flags = decor.SystemUiFlags;

                if (useDarkIcons) flags |= SystemUiFlags.LightStatusBar | SystemUiFlags.LightNavigationBar;
                else flags &= ~(SystemUiFlags.LightStatusBar | SystemUiFlags.LightNavigationBar);

                decor.SystemUiFlags = flags;
#pragma warning restore CA1422
            }
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
                _controller.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch, e.GetPointerId(0));
                break;
            case MotionEventActions.Move:
                _controller.OnPointerMove(x, y);
                break;
            case MotionEventActions.Up:
                _controller.OnPointerUp(x, y, MouseButton.Left, PointerType.Touch, e.GetPointerId(0));
                break;
            case MotionEventActions.Cancel:
                _controller.OnPointerCancel(x, y, e.GetPointerId(0));
                break;
            default:
                return base.OnTouchEvent(e);
        }

        // Input can create dirty regions or queued component work while the view is idle.
        RequestRender();

        return true;
    }

    /// <summary>
    /// 系统返回键（ISSUE-138），返回是否被某个组件（覆盖层/菜单/返回按钮）消费。
    /// <para>
    /// Android 有两条返回路径，本视图都接：
    /// </para>
    /// <list type="bullet">
    /// <item><b>预测性返回</b>（API 33+ 且应用启用了 <c>OnBackInvokedCallback</c>——targetSdk 36 的
    /// 应用在 Android 16 上默认启用）：系统<b>不再</b>调用 <c>OnBackPressed</c>、也不派发
    /// <c>KEYCODE_BACK</c>。视图自己向 <c>OnBackInvokedDispatcher</c> 注册回调，且只在确有组件
    /// 注册了返回处理时才注册——否则系统的默认返回与"返回桌面"预测动画保持原样。</item>
    /// <item><b>传统路径</b>（其余情况）：宿主 Activity 重写 <c>OnBackPressed</c> 并转发：
    /// <code>
    /// public override void OnBackPressed()
    /// {
    ///     if (_view.HandleBackPressed()) return;   // 某个组件消费了本次返回
    ///     base.OnBackPressed();                    // 无人消费 → 交还系统
    /// }
    /// </code></item>
    /// </list>
    /// <para>系统只会走其中一条，因此两者不会重复分发。</para>
    /// </summary>
    public bool HandleBackPressed()
    {
        // 经控制器（持锁）分发：本回调在 UI 线程，组件在 GL 线程构建，处理者会改写组件状态。
        var consumed = _controller.OnBackButton();
        if (consumed) RequestRender();
        return consumed;
    }

    /// <summary>
    /// Activity 声明了 <c>ConfigurationChanges</c>（含 Density）时，系统不再重建 Activity，
    /// 而是回调这里。改「显示大小」或拖到另一块屏幕都会改变密度：必须刷新换算系数，
    /// 并让系统重新下发 inset，否则逻辑尺寸、触摸坐标和安全区都按旧密度换算。
    /// </summary>
    protected override void OnConfigurationChanged(global::Android.Content.Res.Configuration? newConfig)
    {
        base.OnConfigurationChanged(newConfig);

        var density = newConfig is { DensityDpi: > 0 }
            ? newConfig.DensityDpi / (float)DisplayMetricsDensity.Default
            : Resources?.DisplayMetrics?.Density ?? _density;
        if (density != _density)
        {
            _density = density;
            RequestApplyInsets();
        }

        RequestRender();
    }

    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        // RequestRender 可从任意线程调用，投递动作的正是各种后台线程。
        if (_dispatcher is not null) _dispatcher.Posted += RequestRender;
        if (_backRegistry is null || !OperatingSystem.IsAndroidVersionAtLeast(33)) return;
        _backRegistry.Changed += OnBackHandlersChanged;
        SyncBackCallback();
    }

    protected override void OnDetachedFromWindow()
    {
        if (_dispatcher is not null) _dispatcher.Posted -= RequestRender;
        if (_backRegistry is not null && OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            _backRegistry.Changed -= OnBackHandlersChanged;
            SyncBackCallback();
        }
        base.OnDetachedFromWindow();
    }

    // 处理者在 GL 线程上随组件构建注册/注销；系统回调的注册必须回到 UI 线程。
    private void OnBackHandlersChanged() => _activity?.RunOnUiThread(SyncBackCallback);

    /// <summary>让系统回调的注册状态与"是否有组件要处理返回"保持一致。只在 UI 线程调用。</summary>
    private void SyncBackCallback()
    {
        if (_activity is null || _backRegistry is null || !OperatingSystem.IsAndroidVersionAtLeast(33)) return;

        var wanted = IsAttachedToWindow && _backRegistry.Count > 0;
        if (wanted == _backCallbackRegistered) return;

        _backCallback ??= new BackInvokedCallback(this);
        var dispatcher = _activity.OnBackInvokedDispatcher;
        if (wanted)
            dispatcher.RegisterOnBackInvokedCallback(global::Android.Window.IOnBackInvokedDispatcher.PriorityDefault, _backCallback);
        else
            dispatcher.UnregisterOnBackInvokedCallback(_backCallback);
        _backCallbackRegistered = wanted;
    }

    /// <summary>预测性返回路径的系统回调。</summary>
    private sealed class BackInvokedCallback(MikoSurfaceView view) : Java.Lang.Object, global::Android.Window.IOnBackInvokedCallback
    {
        public void OnBackInvoked()
        {
            if (view.HandleBackPressed()) return;

            // 注册了处理者、但它们这次都放弃了（例如返回按钮的 DefaultHref 正是当前页）。
            // 本回调已经截下了这次返回，系统不会再执行默认行为，只能显式执行它。
#pragma warning disable CA1422 // OnBackPressed 在 API 33+ 标记过时；这里正是要它的默认实现
            view._activity?.OnBackPressed();
#pragma warning restore CA1422
        }
    }

    public override bool OnCheckIsTextEditor() => true;

    public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
        => _inputMethod.CreateConnection(outAttrs);

    private sealed class AndroidInputMethod : InputMethodBase
    {
        private readonly MikoSurfaceView _view;
        private readonly MikoInteractionController _controller;
        private InputMethodState? _state;
        private string _composingText = string.Empty;

        // 上一次告知系统的编辑器"形态"与选区。形态变化必须 RestartInput（EditorInfo 需重建），
        // 选区变化只需 UpdateSelection——后者不打断正在进行的组合会话。
        private (bool multiline, InputMethodType type)? _editorShape;
        private (int start, int end) _reportedSelection = (-1, -1);

        public AndroidInputMethod(MikoSurfaceView view, MikoInteractionController controller)
        {
            _view = view;
            _controller = controller;
        }

        /// <summary>当前文本客户端快照，供 <see cref="Connection"/> 回读文档内容。</summary>
        public InputMethodState? State => _state;

        public override void SetState(InputMethodState? state)
        {
            _state = state;

            _view.Post(() =>
            {
                var manager = _view.Context?.GetSystemService(global::Android.Content.Context.InputMethodService)
                    as InputMethodManager;

                if (state == null)
                {
                    _editorShape = null;
                    _reportedSelection = (-1, -1);
                    if (_view.WindowToken != null)
                        manager?.HideSoftInputFromWindow(_view.WindowToken, HideSoftInputFlags.None);
                    _view.ClearFocus();
                    return;
                }

                _view.RequestFocus();

                // 编辑器形态变了（单行↔多行、文本↔密码↔数字）才重建 InputConnection。
                // 无条件 RestartInput 会丢弃当前 InputConnection，把正在进行的拼音组合
                // 连同候选框一起清掉——而 SetState 是每帧发布的，于是候选框刚出现就消失。
                var shape = (state.IsMultiline, state.InputType);
                if (_editorShape != shape)
                {
                    _editorShape = shape;
                    _reportedSelection = (-1, -1);
                    manager?.RestartInput(_view);
                }

                // 选区变化用轻量的 UpdateSelection 通知输入法，它据此维护自己的组合状态。
                var selection = (state.SelectionStart, state.SelectionEnd);
                if (_reportedSelection != selection)
                {
                    _reportedSelection = selection;
                    manager?.UpdateSelection(_view, selection.Item1, selection.Item2, -1, -1);
                }
            });
        }

        /// <summary>
        /// 显式弹出软键盘。不缓存"键盘是否已显示"——用户可以用输入法自带的隐藏键收起键盘，
        /// 而系统不会回调通知我们，任何缓存都会与真实可见性脱节，导致再次点击输入框时
        /// 我们误以为键盘还开着而不去唤起它。
        /// </summary>
        public override void ShowKeyboard()
        {
            _view.Post(() =>
            {
                if (_state == null) return;
                _view.RequestFocus();
                var manager = _view.Context?.GetSystemService(global::Android.Content.Context.InputMethodService)
                    as InputMethodManager;
                manager?.ShowSoftInput(_view, ShowFlags.Implicit);
            });
        }

        public IInputConnection CreateConnection(EditorInfo? info)
        {
            var state = _state;
            if (info != null)
            {
                var type = InputTypes.ClassText;
                if (state?.InputType == InputMethodType.Number)
                {
                    // 数字键盘：ClassNumber 是独立的类，不能与 ClassText 的 flag 混用。
                    type = InputTypes.ClassNumber | InputTypes.NumberFlagSigned | InputTypes.NumberFlagDecimal;
                }
                else
                {
                    if (state?.IsMultiline == true) type |= InputTypes.TextFlagMultiLine;
                    if (state?.InputType == InputMethodType.Password)
                        type |= InputTypes.TextVariationPassword;
                }
                info.InputType = type;
                info.ImeOptions = state?.IsMultiline == true
                    ? (ImeFlags)ImeAction.None
                    : (ImeFlags)ImeAction.Done;

                // 让输入法一开始就知道光标落点。缺少这个，它会认为文档为空、光标在 0，
                // 于是永远不会发出 DeleteSurroundingText（退格因此毫无反应）。
                info.InitialSelStart = state?.SelectionStart ?? 0;
                info.InitialSelEnd = state?.SelectionEnd ?? 0;
            }
            return new Connection(this);
        }

        public void Commit(string text)
        {
            End(text);
            RequestFrame();
        }

        public void Begin()
        {
            StartComposition();
            RequestFrame();
        }

        public void Update(string text)
        {
            _composingText = text ?? string.Empty;
            UpdateComposition(_composingText);
            RequestFrame();
        }

        public void End(string? text = null)
        {
            var committed = text ?? _composingText;
            _composingText = string.Empty;
            EndComposition(string.IsNullOrEmpty(committed) ? null : committed);
        }

        /// <summary>
        /// 视图工作在 <see cref="global::Android.Opengl.Rendermode.WhenDirty"/> 下，GL 线程只在被
        /// 显式请求时才绘制一帧。触摸走 <see cref="OnTouchEvent"/>，那里已经请求了；但输入法的
        /// 提交/组合/按键是通过 InputConnection 直接送到 UI 线程的，不经过触摸路径——不在此处
        /// 请求，引擎虽已标脏却永远等不到那一帧，文字进了 DOM 却始终不显示。
        /// </summary>
        private void RequestFrame() => _view.RequestRender();

        /// <summary>
        /// 桥接 Android 输入法与 Miko 的文本客户端。
        ///
        /// <para>关键点：<see cref="BaseInputConnection"/> 默认从它<b>自己内部</b>那份 Editable
        /// 读写文本，而 Miko 的文本存活在 DOM 元素里，两者毫无关联。若不重写下面这组回读方法，
        /// 输入法看到的永远是一篇空文档，它据此认为"光标前没有字符"而不发退格、
        /// 不给候选词上下文；若不重写 <see cref="SendKeyEvent"/>，数字键盘与硬件键盘的按键
        /// 会被父类吞进那份无用的 Editable，永远到不了引擎。</para>
        /// </summary>
        private sealed class Connection : BaseInputConnection
        {
            private readonly AndroidInputMethod _owner;

            public Connection(AndroidInputMethod owner) : base(owner._view, true) => _owner = owner;

            private string Text => _owner.State?.Text ?? string.Empty;

            private int Cursor
            {
                get
                {
                    var text = Text;
                    return System.Math.Clamp(_owner.State?.CursorPosition ?? 0, 0, text.Length);
                }
            }

            public override ICharSequence? GetTextBeforeCursorFormatted(int length, GetTextFlags flags)
            {
                if (length <= 0) return new Java.Lang.String(string.Empty);
                var cursor = Cursor;
                var start = System.Math.Max(0, cursor - length);
                return new Java.Lang.String(Text.Substring(start, cursor - start));
            }

            public override ICharSequence? GetTextAfterCursorFormatted(int length, GetTextFlags flags)
            {
                if (length <= 0) return new Java.Lang.String(string.Empty);
                var text = Text;
                var cursor = Cursor;
                var count = System.Math.Min(length, text.Length - cursor);
                return new Java.Lang.String(text.Substring(cursor, count));
            }

            // 光标为折叠状态（Miko 目前不支持选区），没有被选中的文本。
            public override ICharSequence? GetSelectedTextFormatted(GetTextFlags flags) => null;

            public override ExtractedText? GetExtractedText(ExtractedTextRequest? request, GetTextFlags flags)
            {
                var text = Text;
                var cursor = Cursor;
                return new ExtractedText
                {
                    Text = new Java.Lang.String(text),
                    StartOffset = 0,
                    PartialStartOffset = -1,
                    PartialEndOffset = -1,
                    SelectionStart = cursor,
                    SelectionEnd = cursor,
                };
            }

            public override CapitalizationMode GetCursorCapsMode(CapitalizationMode reqModes)
                => (CapitalizationMode)TextUtils.GetCapsMode(new Java.Lang.String(Text), Cursor, (CapitalizationMode)reqModes);

            public override bool CommitText(ICharSequence? text, int newCursorPosition)
            {
                _owner.Commit(text?.ToString() ?? string.Empty);
                return true;
            }

            public override bool SetComposingText(ICharSequence? text, int newCursorPosition)
            {
                _owner.Begin();
                _owner.Update(text?.ToString() ?? string.Empty);
                return true;
            }

            public override bool FinishComposingText()
            {
                _owner.End();
                _owner.RequestFrame();
                return true;
            }

            public override bool DeleteSurroundingText(int beforeLength, int afterLength)
            {
                // 长度是 UTF-16 码元数；控制器按字素簇删除，不会把 emoji 删掉半个（ISSUE-154）。
                _owner._controller.DeleteSurroundingText(beforeLength, afterLength);
                _owner.RequestFrame();
                return true;
            }

            /// <summary>
            /// 软键盘的退格/回车/方向键，以及数字键盘的全部数字，走的都是 KeyEvent 而非
            /// CommitText。必须自己翻译并转发给引擎——绝不能回落到 base，父类只会写进
            /// 它内部那份与 DOM 无关的 Editable。
            /// </summary>
            public override bool SendKeyEvent(KeyEvent? e)
            {
                if (e == null || e.Action != KeyEventActions.Down) return true;

                var key = e.KeyCode switch
                {
                    Keycode.Del => (MikoKey?)MikoKey.Backspace,
                    Keycode.ForwardDel => MikoKey.Delete,
                    Keycode.Enter or Keycode.NumpadEnter => MikoKey.Enter,
                    Keycode.DpadLeft => MikoKey.Left,
                    Keycode.DpadRight => MikoKey.Right,
                    Keycode.MoveHome => MikoKey.Home,
                    Keycode.MoveEnd => MikoKey.End,
                    _ => null,
                };

                if (key.HasValue)
                {
                    _owner._controller.OnKeyDown(key.Value, MikoKeyModifiers.None);
                }
                else
                {
                    // UnicodeChar 是完整码位（可能大于 0xFFFF），按码位转成字符串——强转 char 会截断
                    // 增补平面字符。带 COMBINING_ACCENT 标志的是死键，不产生文本。
                    var unicode = e.UnicodeChar;
                    if (unicode <= 0 || (unicode & KeyCharacterMap.CombiningAccent) != 0) return true;
                    if (unicode > 0x10FFFF || unicode is >= 0xD800 and <= 0xDFFF) return true;
                    var text = char.ConvertFromUtf32(unicode);
                    if (text.Length == 1 && char.IsControl(text[0])) return true;
                    _owner._controller.OnTextInput(text);
                }

                _owner.RequestFrame();
                return true;
            }

            public override bool SetSelection(int start, int end)
            {
                _owner._controller.SetTextSelection(start, end);
                _owner.RequestFrame();
                return true;
            }
        }
    }
}
