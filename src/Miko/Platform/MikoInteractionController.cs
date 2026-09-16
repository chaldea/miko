using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform.Resources;
using Miko.Platform.Video;
using Miko.Routing;
using SkiaSharp;

namespace Miko.Platform;

/// <summary>
/// 平台无关的交互控制器。封装了所有与具体窗口/输入框架无关的交互逻辑：
/// 命中测试、焦点管理、点击/选择/下拉、滚动条与滑块拖拽、文本输入编辑、
/// 事件分发、光标解析、路由重建与热重载标记。
/// <para>
/// 各平台实现层（桌面 Silk.NET、Android、iOS）只负责拥有窗口/GL/原生输入，
/// 并将归一化后的指针/键盘事件转发到本控制器。
/// </para>
/// </summary>
public sealed class MikoInteractionController
{
    private readonly MikoAppOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly MikoEngine _engine;
    private readonly HotReloadService _hotReloadService;
    private readonly ILogger<MikoInteractionController> _logger;
    private readonly EventDispatcher _eventDispatcher;
    private readonly MikoSynchronizationContext _syncContext;
    private readonly MikoDispatcher _dispatcher;
    private IScrollBehavior _scrollBehavior;
    private IScrollGestureBehavior? _scrollGestureBehavior;

    private Router? _router;
    private NavigationManager? _navigationManager;
    private RouteView? _routeView;

    private System.Numerics.Vector2? _mouseDownPosition;
    private System.Numerics.Vector2? _lastPointerPosition;
    private Element? _pointerDownTarget;
    private RectF? _pointerDownBounds;
    private PointerType _activePointerType = PointerType.Mouse;
    private int _activePointerId = 1;
    private bool _pointerMoved;
    private bool _longPressFired;
    private CancellationTokenSource? _longPressCancellation;
    private Cursor _currentCursor = Cursor.Default;

    private const float GestureThreshold = 8f;
    private static readonly TimeSpan LongPressDelay = TimeSpan.FromMilliseconds(500);

    // volatile because hot reload (UpdateApplication) sets this from a background thread,
    // while the render loop reads it on the render thread. Without volatile the render
    // thread may never observe the change due to CPU caching.
    private volatile bool _needsRebuild;

    // 最近一次导航的事件参数（含方向与可选的页面转场效果，ISSUE-108）。
    // 先于 volatile 的 _needsRebuild 写入，渲染线程读到 _needsRebuild=true 时必然能读到它。
    private NavigationEventArgs? _pendingNavigation;

    // 焦点元素。读取一律经 FocusedElement（下方）而非直接读字段：组件重渲染会用全新实例
    // 替换整棵子树，此处缓存的引用可能已脱离树（ISSUE-121）。
    private Element? _focusedElement;
    private InputElement? _draggingRange;
    private bool _isDragging;
    private MikoEngine.ScrollbarHitResult? _draggingScrollbar;

    // 当前悬停链：指针命中元素及其全部祖先（持有 ElementState.Hover）。
    // CSS 中 :hover 匹配指针下的元素及其所有祖先，故以链为单位维护。
    private readonly List<Element> _hoveredElements = new();

    // UpdateHover 的复用缓冲：指针移动是高频事件，链构建不得逐次分配（ISSUE-104 问题1）。
    private readonly List<Element> _hoverChainBuffer = new();

    // Serializes input handling against the render frame. On hosts that render on a
    // dedicated thread (Android GLThread, iOS CADisplayLink) the render thread walks the
    // DOM during layout (LayoutEngine.ComputeStyles enumerates Element.Children) while
    // input events on the UI thread mutate the DOM synchronously (a click runs the
    // component handler, which calls StateHasChanged and rewrites Children). Without this
    // lock the two race and throw "Collection was modified; enumeration operation may not
    // execute". The desktop (Silk) host pumps input and render on one thread, so it never
    // hit this — but the lock is harmless there (uncontended, re-entrant).
    private readonly object _sync = new();
    private IInputMethod? _inputMethod;
    private InputMethodState? _publishedInputMethodState;
    private string _compositionText = string.Empty;

    public MikoInteractionController(
        IOptions<MikoAppOptions> options,
        IServiceProvider serviceProvider,
        MikoEngine engine,
        EventDispatcher eventDispatcher,
        MikoDispatcher dispatcher,
        HotReloadService hotReloadService,
        ILogger<MikoInteractionController> logger,
        IInputMethodService? inputMethod = null,
        IScrollBehavior? scrollBehavior = null)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _engine = engine;
        _eventDispatcher = eventDispatcher;
        _dispatcher = dispatcher;
        _scrollBehavior = scrollBehavior ?? new DefaultScrollBehavior();
        _scrollGestureBehavior = _scrollBehavior as IScrollGestureBehavior;
        _hotReloadService = hotReloadService;
        _logger = logger;
        _syncContext = new MikoSynchronizationContext(dispatcher);
        if (inputMethod != null)
            AttachInputMethod(inputMethod);

        // 注：视频后端、图片加载器、语法高亮器的注入已上移到 AddMikoEngine 的引擎工厂
        // （见 Hosting/EngineExtensions.cs）。此前放在这里，导致不经 App 宿主创建的引擎
        // （DevTools、模拟器面板、测试）永远拿不到这些可选服务（ISSUE-129）。

        if (_options.RouteAssemblies != null || _options.RouteConfigurator != null)
        {
            _router = new Router();
            if (_options.RouteAssemblies != null)
                _router.ScanAssemblies(_options.RouteAssemblies);
            _options.RouteConfigurator?.Invoke(_router);
            _navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
            _routeView = new RouteView(_router, _navigationManager, _options.DefaultLayout, _serviceProvider);
            _navigationManager.LocationChanged += args =>
            {
                _pendingNavigation = args;
                _needsRebuild = true;
                _logger.LogDebug("Navigation: {From} -> {To} ({Direction}), transition={Transition}",
                    args.FromPath, args.ToPath, args.Direction, args.Transition?.GetType().Name ?? "none");
            };
        }

        // Set up hot reload if enabled
        if (_options.EnableHotReload)
        {
            _logger.LogInformation("[HotReload] Hot reload enabled, registering rebuild callback");
            _hotReloadService.OnReload(() =>
            {
                _logger.LogInformation("[HotReload] Rebuild callback invoked - setting _needsRebuild flag");
                _needsRebuild = true;
            });
        }
    }

    /// <summary>底层引擎，平台实现层用其进行渲染。</summary>
    public MikoEngine Engine => _engine;

    /// <summary>Installs a host-specific scroll implementation.</summary>
    public void SetScrollBehavior(IScrollBehavior? behavior)
    {
        lock (_sync)
        {
            _scrollGestureBehavior?.PointerCancel();
            _scrollBehavior = behavior ?? new DefaultScrollBehavior();
            _scrollGestureBehavior = _scrollBehavior as IScrollGestureBehavior;
        }
    }

    public IScrollBehavior ScrollBehavior => _scrollBehavior;

    /// <summary>当前光标解析结果发生变化时触发，平台实现层据此应用原生光标。</summary>
    public event Action<Cursor>? CursorChanged;

    /// <summary>Text currently being composed by the native IME (not yet committed).</summary>
    public string CompositionText
    {
        get { lock (_sync) return _compositionText; }
    }

    /// <summary>
    /// Connects a platform text endpoint. Hosts should call this once after creating their
    /// native view; replacing an endpoint safely detaches the previous one.
    /// </summary>
    public void AttachInputMethod(IInputMethod? inputMethod)
    {
        lock (_sync)
        {
            DetachInputMethodCore();
            _inputMethod = inputMethod;
            _publishedInputMethodState = null;
            if (_inputMethod != null)
            {
                _inputMethod.TextCommitted += OnInputMethodTextCommitted;
                _inputMethod.CompositionStarted += OnInputMethodCompositionStarted;
                _inputMethod.CompositionUpdated += OnInputMethodCompositionUpdated;
                _inputMethod.CompositionEnded += OnInputMethodCompositionEnded;
            }
            PublishInputMethodState();
        }
    }

    /// <summary>Currently attached native input endpoint, if the host provides one.</summary>
    public IInputMethod? InputMethod => _inputMethod;

    /// <summary>
    /// Asks the platform to present its soft keyboard for the focused editor.
    /// <para>Callers use this for gestures that <i>mean</i> "give me the keyboard" — tapping an input.
    /// It bypasses the state deduplication in <see cref="PublishInputMethodState"/>, because tapping
    /// an already-focused field produces an identical state yet must still re-show a keyboard the
    /// user dismissed via the IME's own hide button.</para>
    /// </summary>
    public void RequestInputMethodShow()
    {
        lock (_sync)
        {
            PublishInputMethodState();
            if (FocusedElement is ITextEditable { IsEditable: true })
                _inputMethod?.ShowKeyboard();
        }
    }

    /// <summary>Compatibility alias for hosts that configure the endpoint after construction.</summary>
    public void SetInputMethod(IInputMethod? inputMethod) => AttachInputMethod(inputMethod);

    private void DetachInputMethodCore()
    {
        if (_inputMethod == null) return;
        _inputMethod.SetState(null);
        _inputMethod.TextCommitted -= OnInputMethodTextCommitted;
        _inputMethod.CompositionStarted -= OnInputMethodCompositionStarted;
        _inputMethod.CompositionUpdated -= OnInputMethodCompositionUpdated;
        _inputMethod.CompositionEnded -= OnInputMethodCompositionEnded;
        _inputMethod = null;
    }

    private void OnInputMethodTextCommitted(string text) => OnTextInput(text);
    private void OnInputMethodCompositionStarted() => OnTextCompositionStart();
    private void OnInputMethodCompositionUpdated(string text) => OnTextCompositionUpdate(text);
    private void OnInputMethodCompositionEnded(string? text) => OnTextCompositionEnd(text);

    /// <summary>热重载/路由变更后是否需要重建 DOM 树。</summary>
    public bool NeedsRebuild => _needsRebuild;

    /// <summary>
    /// 是否有需要在下一帧呈现的视觉工作（DOM 重建请求，或引擎侧存在脏区域/动画/视频/
    /// 排队的回调/布局输入变化）。平台宿主可在其为 false 时跳过帧生产（不交换缓冲）
    /// 并短暂休眠——稳态下做到零分配、零 GPU 提交的空闲（ISSUE-096）。
    /// 输入事件不经此判断：宿主每轮循环都应先排空输入队列，输入处理会把工作产生出来。
    /// </summary>
    public bool HasPendingWork
    {
        get
        {
            lock (_sync)
            {
                return _needsRebuild || _dispatcher.HasPendingActions || _engine.HasPendingVisualWork ||
                    (_scrollGestureBehavior?.HasPendingWork ?? false);
            }
        }
    }

    /// <summary>
    /// 请求在下一帧重建整个 DOM 树（在锁保护下由 <see cref="RenderFrame"/> 执行）。
    /// 供平台宿主在影响整棵树的外部状态变化后调用——例如模拟器切换设备/平台后，
    /// 让 Ionic 组件以新的 mode 重新渲染。
    /// </summary>
    public void RequestRebuild() => _needsRebuild = true;

    // ---------------------------------------------------------------------
    // 生命周期
    // ---------------------------------------------------------------------

    /// <summary>构建根元素（路由优先，否则使用根组件工厂）。</summary>
    public Element BuildRoot()
    {
        using (ComponentServiceScope.Push(_serviceProvider))
        {
            if (_routeView != null && _navigationManager != null)
                return _routeView.Render(_navigationManager.CurrentPath);

            return _options.RootComponentFactory?.Invoke()
                ?? throw new InvalidOperationException("No root component or router configured.");
        }
    }

    /// <summary>初始化引擎并构建首帧 DOM 树。</summary>
    public void Initialize(SKCanvas canvas, float width, float height)
    {
        // Install the sync context before BuildRoot so that OnInitializedAsync
        // continuations are captured and posted back to the dispatcher (render thread).
        var prevCtx = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_syncContext);
        Element root;
        try { root = BuildRoot(); }
        finally { SynchronizationContext.SetSynchronizationContext(prevCtx); }
        _engine.Initialize(root, _options.StyleSheets, canvas, width, height);
    }

    /// <summary>执行所有 PostInit 钩子（如 DevTools 桥接初始化）。</summary>
    public void RunPostInitHooks()
    {
        foreach (var hook in _options.PostInitHooks)
            hook(_serviceProvider);
    }

    /// <summary>当 <see cref="NeedsRebuild"/> 为真时重建 DOM 树。</summary>
    public void Rebuild(SKCanvas canvas, float width, float height)
    {
        _logger.LogInformation("[HotReload] Render loop detected _needsRebuild flag, rebuilding DOM tree");
        _needsRebuild = false;
        var navigation = _pendingNavigation;
        _pendingNavigation = null;
        // Install the sync context so OnInitializedAsync continuations post to the dispatcher.
        var prevCtx = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_syncContext);
        Element root;
        try { root = BuildRoot(); }
        finally { SynchronizationContext.SetSynchronizationContext(prevCtx); }
        if (navigation != null)
        {
            // 路由导航：把方向/路径（以及可选的转场效果）一并交给引擎。
            // - 有转场效果时保留旧页面树作为 leaving 图层与新页面共同绘制（ISSUE-108）。
            // - 无转场效果时仍需传入：引擎据方向与路径维护按路径的滚动快照，
            //   使返回上一页时能恢复其滚动位置（ISSUE-118）。
            _engine.Initialize(root, _options.StyleSheets, canvas, width, height,
                new NavigationTransitionInfo(navigation.Transition, navigation.Direction, navigation.FromPath, navigation.ToPath));
        }
        else
        {
            // 热重载等非导航重建：不涉及历史栈，不触碰滚动快照。
            _engine.Initialize(root, _options.StyleSheets, canvas, width, height);
        }
        _logger.LogInformation("[HotReload] DOM rebuilt and initialized, next frame will render new content");
    }

    /// <summary>
    /// 帧间隔上限（秒）。超过该间隔说明期间没有出帧——稳态空闲（ISSUE-096）下宿主跳帧休眠，
    /// 或严重卡顿。动画时间只在出帧期间流动：此类"恢复帧"按 <see cref="NominalFrameDelta"/>
    /// 推进，而非按包含空闲时长的真实间隔——否则该帧刚启动的动画/页面转场会被一步跳过
    /// 大半（ISSUE-108 转场"不生效/一闪而过"）。
    /// </summary>
    private const float MaxFrameDelta = 0.1f;

    /// <summary>恢复帧采用的标称帧步长（1/60 秒）。</summary>
    private const float NominalFrameDelta = 1f / 60f;

    /// <summary>推进动画与页面转场（每帧调用）。</summary>
    public void Update(float deltaTime)
    {
        var dt = deltaTime > MaxFrameDelta ? NominalFrameDelta : deltaTime;
        _engine.AnimationManager.Update(dt);
        _engine.AdvanceNavigationTransition(dt);
        _scrollGestureBehavior?.Update(_engine, dt);
    }

    /// <summary>
    /// 在输入/渲染锁的保护下执行一帧：先按需重建 DOM 树，再推进动画，最后调用
    /// <paramref name="render"/> 绘制。平台宿主应通过本方法渲染，而非直接调用
    /// <see cref="Rebuild"/>/<see cref="Update"/>/<c>Engine.Render</c>，以确保输入处理
    /// 引发的 DOM 变更不会与布局/渲染对 DOM 的遍历并发执行。
    /// </summary>
    public void RenderFrame(SKCanvas canvas, float width, float height, float deltaTime, Action<SKCanvas> render)
    {
        lock (_sync)
        {
            if (_needsRebuild)
                Rebuild(canvas, width, height);

            Update(deltaTime);
            render(canvas);
            // Layout may have moved after a rebuild or scroll. Keep the native candidate
            // window anchored to the current input rectangle instead of (0, 0).
            PublishInputMethodState();
        }
    }

    /// <summary>视口尺寸变化。</summary>
    public void SetViewportSize(float width, float height)
    {
        lock (_sync)
        {
            _engine.SetViewportSize(width, height);
            PublishInputMethodState();
        }
    }

    /// <summary>
    /// 安全区边距变化（逻辑像素）。平台宿主从系统状态栏/导航栏 inset 换算为逻辑像素后调用，
    /// 使根内容内缩到安全区内，避免被系统 UI 遮盖。
    /// </summary>
    public void SetSafeAreaInsets(float left, float top, float right, float bottom)
    {
        lock (_sync)
        {
            _engine.SetSafeAreaInsets(new SafeAreaInsets(left, top, right, bottom));
        }
    }

    // ---------------------------------------------------------------------
    // 指针输入（坐标为已根据像素密度换算后的逻辑坐标）
    // ---------------------------------------------------------------------

    public void OnPointerDown(
        float x,
        float y,
        MouseButton button,
        PointerType pointerType = PointerType.Mouse,
        int pointerId = 1)
    {
        if (button != MouseButton.Left) return;
        lock (_sync)
        {
            _mouseDownPosition = new System.Numerics.Vector2(x, y);
            _lastPointerPosition = _mouseDownPosition;
            _pointerDownTarget = null;
            _pointerDownBounds = null;
            _activePointerType = pointerType;
            _activePointerId = pointerId;
            _pointerMoved = false;
            _longPressFired = false;
            CancelLongPress();
            _scrollGestureBehavior?.PointerDown(x, y);

            // Select popups are painted above content, including range inputs and scrollbars.
            var dropdownHit = HitTestSelectDropdown(x, y);
            var scrollbarHit = dropdownHit == null && pointerType == PointerType.Mouse ? _engine.HitTestScrollbar(x, y) : null;
            if (scrollbarHit != null)
            {
                _logger.LogTrace("Scrollbar hit: type={HitType}, element={Tag}#{Id}, thumbOffset={Offset}, pos=({X}, {Y})",
                    scrollbarHit.HitType, scrollbarHit.Box.Element.TagName, scrollbarHit.Box.Element.Id ?? "",
                    scrollbarHit.ThumbOffset, x, y);
                _draggingScrollbar = scrollbarHit;
                _isDragging = true;
                if (scrollbarHit.HitType == MikoEngine.ScrollbarHitType.VerticalThumb ||
                    scrollbarHit.HitType == MikoEngine.ScrollbarHitType.HorizontalThumb)
                {
                    _mouseDownPosition = null; // suppress click handling
                }
                return;
            }

            var target = dropdownHit?.select ?? _engine.HitTest(x, y);
            if (target != null)
            {
                _pointerDownTarget = target;
                _pointerDownBounds = GetRenderedBorderBox(target);
                DispatchPointerPair(target, EventTypes.PointerDown, EventTypes.MouseDown,
                    x, y, button, isButtonPressed: true, _pointerDownBounds);
                ScheduleLongPress();
            }

            if (target is InputElement { Type: InputType.Range } rangeInput)
            {
                _isDragging = true;
                _draggingRange = rangeInput;
                UpdateRangeValue(rangeInput, x);
            }
        }
    }

    public void OnPointerUp(
        float x,
        float y,
        MouseButton button,
        PointerType pointerType = PointerType.Mouse,
        int pointerId = 1)
    {
        if (button != MouseButton.Left) return;
        lock (_sync)
        {
            if (_pointerDownTarget != null && pointerId != _activePointerId) return;
            CancelLongPress();
            _scrollGestureBehavior?.PointerUp(x, y);
            if (_isDragging)
            {
                _isDragging = false;

                // Handle scrollbar track click (not thumb drag)
                if (_draggingScrollbar != null)
                {
                    var hit = _draggingScrollbar;
                    _draggingScrollbar = null;
                    if (hit.HitType == MikoEngine.ScrollbarHitType.VerticalTrack ||
                        hit.HitType == MikoEngine.ScrollbarHitType.HorizontalTrack)
                    {
                        _logger.LogTrace("Scrollbar track click: type={HitType}, element={Tag}#{Id}, pos=({X}, {Y})",
                            hit.HitType, hit.Box.Element.TagName, hit.Box.Element.Id ?? "", x, y);
                        _engine.ScrollTrackClick(hit.Box, hit.HitType, x, y);
                    }
                }

                _draggingRange = null;
                _mouseDownPosition = null;
                _lastPointerPosition = null;
                DispatchPointerUp(x, y, button);
                return;
            }

            if (_mouseDownPosition == null) return;

            var position = _mouseDownPosition.Value;
            _mouseDownPosition = null;

            DispatchPointerUp(x, y, button);

            if (_pointerMoved || _longPressFired)
            {
                _lastPointerPosition = null;
                return;
            }

            var target = _engine.HitTest(position.X, position.Y);
            if (target == null)
            {
                SetFocusCore(null);
                _lastPointerPosition = null;
                return;
            }

            HandleClick(target, position.X, position.Y);
            _lastPointerPosition = null;
        }
    }

    public void OnPointerMove(float x, float y)
    {
        lock (_sync)
        {
            if (_isDragging && _draggingScrollbar != null)
            {
                var hit = _draggingScrollbar;
                if (hit.HitType == MikoEngine.ScrollbarHitType.VerticalThumb)
                {
                    _logger.LogTrace("Scrollbar thumb drag: vertical, element={Tag}#{Id}, mouseY={Y}",
                        hit.Box.Element.TagName, hit.Box.Element.Id ?? "", y);
                    _engine.DragVerticalThumb(hit.Box, y, hit.ThumbOffset);
                }
                else if (hit.HitType == MikoEngine.ScrollbarHitType.HorizontalThumb)
                {
                    _logger.LogTrace("Scrollbar thumb drag: horizontal, element={Tag}#{Id}, mouseX={X}",
                        hit.Box.Element.TagName, hit.Box.Element.Id ?? "", x);
                    _engine.DragHorizontalThumb(hit.Box, x, hit.ThumbOffset);
                }
                return;
            }

            if (_isDragging && _draggingRange != null)
            {
                UpdateRangeValue(_draggingRange, x);
                DispatchCapturedPointerPair(EventTypes.PointerMove, EventTypes.MouseMove,
                    x, y, MouseButton.Left, isButtonPressed: true);
                return;
            }

            if (_pointerDownTarget != null && _mouseDownPosition is { } down)
            {
                var current = new System.Numerics.Vector2(x, y);
                var previous = _lastPointerPosition ?? down;
                if (!_pointerMoved && System.Numerics.Vector2.Distance(down, current) >= GestureThreshold)
                {
                    _pointerMoved = true;
                    CancelLongPress();
                }

                var pointerArgs = DispatchCapturedPointerPair(EventTypes.PointerMove, EventTypes.MouseMove,
                    x, y, MouseButton.Left, isButtonPressed: true);

                if (_activePointerType != PointerType.Mouse && _pointerMoved
                    && pointerArgs?.DefaultPrevented != true)
                    _scrollBehavior.ScrollBy(_engine, down.X, down.Y,
                        previous.X - current.X, previous.Y - current.Y);

                _lastPointerPosition = current;
                return;
            }

            // 拖拽路径保持既有悬停不变（浏览器在拖拽期间也不更新 :hover）。
            var target = _engine.HitTest(x, y);
            if (target != null)
                DispatchPointerPair(target, EventTypes.PointerMove, EventTypes.MouseMove,
                    x, y, MouseButton.Left, isButtonPressed: false, GetRenderedBorderBox(target),
                    PointerType.Mouse, pointerId: 1);
            UpdateHover(target);
            UpdateCursor(target);
        }
    }

    /// <summary>Cancels the active pointer without producing a click.</summary>
    public void OnPointerCancel(float x, float y, int pointerId = 1)
    {
        lock (_sync)
        {
            if (pointerId != _activePointerId) return;
            // The gesture behavior was told PointerDown unconditionally, so it must always hear
            // the cancel — even on paths that took over the press and left _pointerDownTarget
            // null (a scrollbar grab, or a press that hit nothing). Skipping it strands the
            // behavior mid-drag, and it would then sample unrelated deltas as drag velocity.
            _scrollGestureBehavior?.PointerCancel();

            if (_pointerDownTarget == null) return;
            CancelLongPress();
            DispatchCapturedPointerEvent(EventTypes.PointerCancel, x, y, MouseButton.Left,
                isButtonPressed: false);
            ResetPointerState();
        }
    }

    /// <summary>
    /// 维护 :hover 悬停状态：命中元素及其祖先链设置 <see cref="ElementState.Hover"/>；
    /// 之前悬停链上不再悬停的元素清除该状态（CSS：:hover 匹配指针下的元素及其全部祖先）。
    ///
    /// 性能（ISSUE-104 问题1）：状态置/清是否标脏按"悬停相关性"门控——元素与所有
    /// 样式表的 :hover 规则都无关时，其 Hover 状态不可能影响任何规则匹配，仅作标志位
    /// 静默跟踪（不递增 MutationVersion），避免指针扫过普通元素就触发全量重排造成
    /// 内存锯齿。相关元素（如命中 .btn:hover 的 .btn）仍走完整标脏，下一帧样式解析
    /// 即按 :hover 重新级联（见 ISSUE-099 问题5）。链构建复用缓冲，指针移动热路径
    /// 零分配。
    /// </summary>
    private void UpdateHover(Element? target)
    {
        // 新悬停链：命中元素及其全部祖先（复用缓冲）。
        var chain = _hoverChainBuffer;
        chain.Clear();
        for (var current = target; current != null; current = current.Parent)
            chain.Add(current);

        foreach (var element in _hoveredElements)
        {
            if (!chain.Contains(element))
                element.ClearState(ElementState.Hover, _engine.IsHoverRelevant(element));
        }

        foreach (var element in chain)
        {
            if (!_hoveredElements.Contains(element))
                element.SetState(ElementState.Hover, _engine.IsHoverRelevant(element));
        }

        _hoveredElements.Clear();
        _hoveredElements.AddRange(chain);
        // The buffer is only scratch storage. Keeping the copied chain here would give every
        // hovered element a second long-lived root while captured moves skip UpdateHover.
        chain.Clear();
    }

    /// <summary>
    /// Moves cached hover references to their live replacements after component re-rendering.
    /// Captured pointer moves intentionally do not recompute :hover from hit testing, but the
    /// cached references must still advance or their SupersededBy chains retain every old tree.
    /// </summary>
    private void ReanchorHoveredElements()
    {
        for (int i = 0; i < _hoveredElements.Count; i++)
            _hoveredElements[i] = _hoveredElements[i].ResolveSuperseded();
    }

    /// <summary>
    /// 滚动。<paramref name="deltaX"/>/<paramref name="deltaY"/> 为像素增量
    /// （平台实现层负责将滚轮单位换算为像素）。
    /// </summary>
    public void OnScroll(float x, float y, float deltaX, float deltaY)
    {
        lock (_sync)
        {
            _logger.LogTrace("OnScroll: pos=({PosX}, {PosY}), delta=({DeltaX}, {DeltaY})", x, y, deltaX, deltaY);
            _scrollBehavior.ScrollBy(_engine, x, y, deltaX, deltaY);
        }
    }

    /// <summary>
    /// 解析指针下元素的 CSS 光标并在变化时通过 <see cref="CursorChanged"/> 通知平台层。
    /// </summary>
    private void UpdateCursor(Element? target)
    {
        var cursor = ResolveCursor(target);

        if (cursor == _currentCursor) return;
        _currentCursor = cursor;
        CursorChanged?.Invoke(cursor);
    }

    /// <summary>
    /// 从悬停元素向上查找首个具有显式（非默认）光标的元素，近似 CSS 光标继承。
    /// 回退到 <see cref="Cursor.Default"/>。
    /// </summary>
    private static Cursor ResolveCursor(Element? element)
    {
        for (var current = element; current != null; current = current.Parent)
        {
            var computed = current.LayoutBox?.ComputedStyle;
            if (computed != null && computed.Cursor != Cursor.Default)
                return computed.Cursor;
        }
        return Cursor.Default;
    }

    private void HandleClick(Element target, float x, float y)
    {
        // Check if click is on an open select's dropdown area first
        var dropdownHit = HitTestSelectDropdown(x, y);
        if (dropdownHit.HasValue)
        {
            var (selectElement, optionIndex) = dropdownHit.Value;
            HandleOptionClick(selectElement, optionIndex);
            return;
        }

        DispatchPointerEvent(target, EventTypes.Click, x, y, MouseButton.Left,
            isButtonPressed: false, GetRenderedBorderBox(target), _activePointerType, _activePointerId);

        // 分发过程中的处理器（如 IonInput 的 @onclick）很可能已经重渲染了这棵子树，把 target
        // 换成了一个新实例。后续的焦点/光标处理必须落在<b>在场</b>的那个实例上——否则焦点写在
        // 已脱离树的元素上，渲染看不到它，光标便不会出现（ISSUE-121）。
        target = target.ResolveSuperseded();

        if (target is InputElement inputElement)
        {
            HandleInputClick(inputElement);
        }
        else if (target is TextAreaElement textAreaElement)
        {
            CloseAllSelects();
            textAreaElement.MoveCursorToEnd();
            SetFocusCore(textAreaElement);
            RequestInputMethodShow();
        }
        else if (target is SelectElement selectElement2)
        {
            HandleSelectClick(selectElement2);
        }
        else
        {
            CloseAllSelects();
            SetFocusCore(null);
        }
    }

    private void HandleInputClick(InputElement inputElement)
    {
        CloseAllSelects();

        switch (inputElement.Type)
        {
            // 文本类输入（含 search，Ionic 的 ion-searchbar 用的就是它；含 number，走数字软键盘）
            // 点击即聚焦并把光标移到末尾。此清单必须与 InputElement.IsEditable 保持同步。
            case InputType.Text:
            case InputType.Password:
            case InputType.Search:
            case InputType.Number:
                inputElement.MoveCursorToEnd();
                SetFocusCore(inputElement);
                RequestInputMethodShow();
                break;
            case InputType.Checkbox:
                inputElement.Checked = !inputElement.Checked;
                _engine.InvalidateElement(inputElement);
                DispatchChange(inputElement);
                break;
            case InputType.Radio:
                inputElement.Checked = true;
                _engine.InvalidateElement(inputElement);
                DispatchChange(inputElement);
                break;
        }
    }

    private void HandleSelectClick(SelectElement selectElement)
    {
        SetFocusCore(selectElement);
        selectElement.Toggle();
    }

    private void HandleOptionClick(SelectElement selectElement, int optionIndex)
    {
        if (optionIndex >= 0)
        {
            bool changed = selectElement.SelectOption(optionIndex);
            if (changed)
            {
                DispatchChange(selectElement);
            }
        }
        else
        {
            selectElement.Close();
        }
    }

    private (SelectElement select, int optionIndex)? HitTestSelectDropdown(float x, float y)
    {
        var root = _engine.GetRoot();
        if (root == null) return null;
        return HitTestSelectDropdownRecursive(root, x, y);
    }

    private (SelectElement select, int optionIndex)? HitTestSelectDropdownRecursive(Element element, float x, float y)
    {
        if (element is SelectElement { IsOpen: true } sel)
        {
            var layoutBox = FindLayoutBoxForElement(sel);
            if (layoutBox != null)
            {
                var scrollOffset = GetAccumulatedScrollOffset(sel);
                var borderBox = layoutBox.BoxModel.BorderBox;
                float left = borderBox.Left - scrollOffset.x;
                float right = borderBox.Right - scrollOffset.x;
                float bottom = borderBox.Bottom - scrollOffset.y;

                var options = sel.GetAllOptions();
                float fontSize = layoutBox.ComputedStyle.FontSize.Value;
                float optionHeight = fontSize + 8;
                float dropdownTop = bottom;
                float dropdownBottom = dropdownTop + options.Count * optionHeight;

                if (x >= left && x <= right &&
                    y >= dropdownTop && y <= dropdownBottom)
                {
                    int index = (int)((y - dropdownTop) / optionHeight);
                    index = Math.Clamp(index, 0, options.Count - 1);
                    return (sel, index);
                }
            }
        }

        foreach (var child in element.Children)
        {
            var result = HitTestSelectDropdownRecursive(child, x, y);
            if (result != null) return result;
        }
        return null;
    }

    private void CloseAllSelects()
    {
        var root = _engine.GetRoot();
        if (root == null) return;
        CloseSelectsRecursive(root);
    }

    private static void CloseSelectsRecursive(Element element)
    {
        if (element is SelectElement { IsOpen: true } sel)
        {
            sel.Close();
        }
        foreach (var child in element.Children)
        {
            CloseSelectsRecursive(child);
        }
    }

    /// <summary>设置当前焦点元素，触发 blur/focus 事件。</summary>
    public void SetFocus(Element? newFocus)
    {
        lock (_sync)
        {
            SetFocusCore(newFocus);
        }
    }

    /// <summary>
    /// Wraps event dispatcher invocation with the MikoSynchronizationContext so async
    /// event handlers resume on the render thread.
    /// </summary>
    private void DispatchWithSyncContext<T>(Element target, string eventType, T args) where T : MikoEventArgs
    {
        var prevContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_syncContext);
        try
        {
            _eventDispatcher.Dispatch(target, eventType, args);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevContext);
        }
    }

    /// <summary>
    /// 当前焦点元素，经 <c>Element.SupersededBy</c> 转发链解析到仍在树中的实例。
    /// <para>事件处理器（<c>@oninput</c>、<c>@onclick</c>）触发的组件重渲染会用全新实例替换
    /// 整棵子树，而这里缓存的是引用。不解析就会把后续键盘输入送给一个已脱离树的元素：
    /// 值和光标都写在看不见的实例上（ISSUE-121）。</para>
    /// </summary>
    private Element? FocusedElement
    {
        get
        {
            if (_focusedElement == null) return null;
            _focusedElement = _focusedElement.ResolveSuperseded();
            return _focusedElement;
        }
    }

    private void SetFocusCore(Element? newFocus)
    {
        var oldFocus = FocusedElement;
        if (oldFocus == newFocus) return;

        if (oldFocus != null)
        {
            _compositionText = string.Empty;
            oldFocus.ClearState(ElementState.Focus);
            var blurArgs = new FocusEventArgs
            {
                Target = oldFocus,
                RelatedTarget = newFocus,
                Bubbles = false
            };
            DispatchWithSyncContext(oldFocus, EventTypes.Blur, blurArgs);

            if (oldFocus is SelectElement sel)
                sel.HandleBlur();
        }

        // A blur callback can rebuild the component subtree. Resolve the requested target after
        // dispatch so focus is applied to the replacement that is still in the live DOM, matching
        // the click path's post-dispatch resolution above.
        newFocus = newFocus?.ResolveSuperseded();
        _focusedElement = newFocus;

        if (newFocus != null)
        {
            newFocus.SetState(ElementState.Focus);
            var focusArgs = new FocusEventArgs
            {
                Target = newFocus,
                RelatedTarget = oldFocus,
                Bubbles = false
            };
            DispatchWithSyncContext(newFocus, EventTypes.Focus, focusArgs);
        }

        PublishInputMethodState();
    }

    // ---------------------------------------------------------------------
    // 键盘 / 文本输入
    // ---------------------------------------------------------------------

    /// <summary>键按下。返回 true 表示该按键已被全局处理器消费。</summary>
    public bool OnKeyDown(MikoKey key, MikoKeyModifiers mods)
    {
        lock (_sync)
        {
            foreach (var handler in _options.GlobalKeyDownHandlers)
            {
                if (handler(key)) return true;
            }

            if (FocusedElement is not ITextEditable editable || !editable.IsEditable) return false;
            var editableElement = (Element)editable;

            var keyName = key.ToString();
            bool ctrl = mods.HasFlag(MikoKeyModifiers.Control);

            var keyArgs = new KeyboardEventArgs
            {
                Target = editableElement,
                Key = keyName,
                CtrlKey = ctrl,
                ShiftKey = mods.HasFlag(MikoKeyModifiers.Shift),
                AltKey = mods.HasFlag(MikoKeyModifiers.Alt),
                Bubbles = true
            };
            DispatchWithSyncContext(editableElement, EventTypes.KeyDown, keyArgs);

            if (!keyArgs.DefaultPrevented)
                ProcessKeyAction(key);
            return keyArgs.DefaultPrevented;
        }
    }

    /// <summary>键释放（当前无状态需要清理，保留以备扩展与对称性）。</summary>
    public void OnKeyUp(MikoKey key, MikoKeyModifiers mods)
    {
    }

    /// <summary>该按键在按住时是否应触发重复（用于平台层的按键重复定时）。</summary>
    public static bool IsRepeatableKey(MikoKey key) => key is
        MikoKey.Backspace or MikoKey.Delete or MikoKey.Left or MikoKey.Right or MikoKey.Home or MikoKey.End;

    /// <summary>由平台层在按键重复时调用，重新执行编辑动作。</summary>
    public void RepeatKey(MikoKey key)
    {
        lock (_sync)
        {
            ProcessKeyAction(key);
        }
    }

    private void ProcessKeyAction(MikoKey key)
    {
        if (FocusedElement is not ITextEditable editable || !editable.IsEditable) return;
        var element = (Element)editable;

        switch (key)
        {
            case MikoKey.Backspace:
                if (editable.Backspace())
                    DispatchInputEvent(element, editable);
                break;
            case MikoKey.Delete:
                if (editable.Delete())
                    DispatchInputEvent(element, editable);
                break;
            case MikoKey.Left:
                if (editable.CursorPosition > 0)
                {
                    editable.CursorPosition--;
                    _engine.InvalidateElement(element);
                }
                break;
            case MikoKey.Right:
                if (editable.CursorPosition < (editable.Value ?? string.Empty).Length)
                {
                    editable.CursorPosition++;
                    _engine.InvalidateElement(element);
                }
                break;
            case MikoKey.Home:
                editable.CursorPosition = 0;
                _engine.InvalidateElement(element);
                break;
            case MikoKey.End:
                editable.MoveCursorToEnd();
                _engine.InvalidateElement(element);
                break;
            case MikoKey.Enter:
                // 多行控件（textarea）回车插入换行；单行控件不处理（可用于表单提交，由上层处理）。
                if (editable.IsMultiline)
                {
                    editable.InsertText("\n");
                    DispatchInputEvent(element, editable);
                }
                break;
        }
    }

    /// <summary>文本输入（已组合的字符）。控制字符会被忽略。</summary>
    public void OnTextInput(string text)
    {
        lock (_sync)
        {
            if (FocusedElement is not ITextEditable editable || !editable.IsEditable) return;
            var element = (Element)editable;

            foreach (var character in text)
            {
                // Native IMEs may commit a newline directly (not as a key event) for a
                // multiline editor; preserve it while still filtering other controls.
                if (char.IsControl(character) && !(character is '\r' or '\n' && editable.IsMultiline))
                    continue;
                editable.InsertText(character.ToString());
            }
            DispatchInputEvent(element, editable);
            PublishInputMethodState();
        }
    }

    /// <summary>Begins a native IME composition without changing the committed value.</summary>
    public void OnTextCompositionStart()
    {
        lock (_sync)
        {
            if (FocusedElement is not ITextEditable { IsEditable: true }) return;
            _compositionText = string.Empty;
            _engine.InvalidateElement((Element)FocusedElement!);
        }
    }

    /// <summary>Updates the transient composition string supplied by a platform IME.</summary>
    public void OnTextCompositionUpdate(string text)
    {
        lock (_sync)
        {
            if (FocusedElement is not ITextEditable { IsEditable: true }) return;
            _compositionText = text ?? string.Empty;
            _engine.InvalidateElement((Element)FocusedElement!);
        }
    }

    /// <summary>Ends composition and optionally commits the final text.</summary>
    public void OnTextCompositionEnd(string? committedText = null)
    {
        lock (_sync)
        {
            _compositionText = string.Empty;
            if (!string.IsNullOrEmpty(committedText))
                OnTextInput(committedText);
            else
                PublishInputMethodState();
        }
    }

    /// <summary>Updates the insertion point reported by a native text connection.</summary>
    public void SetTextSelection(int start, int end)
    {
        lock (_sync)
        {
            if (FocusedElement is not ITextEditable editable || !editable.IsEditable) return;
            var length = (editable.Value ?? string.Empty).Length;
            editable.CursorPosition = Math.Clamp(Math.Min(start, end), 0, length);
            _engine.InvalidateElement((Element)editable);
            PublishInputMethodState();
        }
    }

    private void PublishInputMethodState()
    {
        if (_inputMethod == null) return;
        var focused = FocusedElement;
        if (focused is not ITextEditable editable || !editable.IsEditable)
        {
            if (_publishedInputMethodState != null)
            {
                _publishedInputMethodState = null;
                _inputMethod.SetState(null);
            }
            return;
        }

        var rect = GetInputMethodCursorRect(focused, editable);
        var inputType = focused switch
        {
            TextAreaElement => InputMethodType.Multiline,
            InputElement { Type: InputType.Password } => InputMethodType.Password,
            InputElement { Type: InputType.Number } => InputMethodType.Number,
            _ => InputMethodType.Text,
        };
        var state = new InputMethodState(
            editable.Value ?? string.Empty,
            Math.Clamp(editable.CursorPosition, 0, (editable.Value ?? string.Empty).Length),
            editable.IsMultiline,
            inputType,
            rect);
        if (Equals(_publishedInputMethodState, state)) return;
        _publishedInputMethodState = state;
        _inputMethod.SetState(state);
    }

    private RectF GetInputMethodCursorRect(Element element, ITextEditable editable)
    {
        var box = FindLayoutBoxForElement(element);
        if (box == null) return GetRenderedBorderBox(element) ?? default;

        var content = box.BoxModel.Content;
        var style = box.ComputedStyle;
        var ancestorScroll = GetAccumulatedScrollOffset(element);
        if (element is InputElement input)
        {
            var text = input.Value ?? string.Empty;
            var pos = Math.Clamp(input.CursorPosition, 0, text.Length);
            var renderedText = input.Type == InputType.Password
                ? new string('\u25cf', text.Length)
                : text;
            var caretWidth = Utils.TextMeasurer.MeasureTextWidth(
                renderedText.Substring(0, pos), style.FontFamily, style.FontSize.Value, style.FontWeight);
            var totalWidth = Utils.TextMeasurer.MeasureTextWidth(
                renderedText, style.FontFamily, style.FontSize.Value, style.FontWeight);
            var scroll = caretWidth > content.Width ? caretWidth - content.Width : 0;
            var alignmentOffset = scroll > 0 ? 0 : style.TextAlign switch
            {
                TextAlign.Right => Math.Max(0, content.Width - totalWidth),
                TextAlign.Center => Math.Max(0, (content.Width - totalWidth) / 2),
                _ => 0,
            };
            return new RectF(
                content.Left - ancestorScroll.x + alignmentOffset + caretWidth - scroll,
                content.Top - ancestorScroll.y,
                1,
                content.Height);
        }

        if (element is TextAreaElement textArea)
        {
            var text = textArea.Value ?? string.Empty;
            var pos = Math.Clamp(textArea.CursorPosition, 0, text.Length);
            var before = Utils.TextWrapper.ProcessText(text.Substring(0, pos), WhiteSpace.PreWrap);
            var lines = Utils.TextWrapper.WrapText(
                before,
                style.FontFamily,
                style.FontSize.Value,
                style.FontWeight,
                content.Width,
                WhiteSpace.PreWrap,
                breakLongWords: true);
            var lineIndex = lines.Count > 0 ? lines.Count - 1 : 0;
            var currentLine = lines.Count > 0 ? lines[^1] : string.Empty;
            var caretWidth = Utils.TextMeasurer.MeasureTextWidth(
                currentLine, style.FontFamily, style.FontSize.Value, style.FontWeight);
            var lineHeight = Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(style);
            return new RectF(
                content.Left - ancestorScroll.x + caretWidth,
                content.Top - ancestorScroll.y + lineIndex * lineHeight,
                1,
                lineHeight);
        }

        return new RectF(
            content.Left - ancestorScroll.x,
            content.Top - ancestorScroll.y,
            1,
            content.Height);
    }

    private void DispatchInputEvent(Element element, ITextEditable editable)
    {
        // 文本内容已变更：标脏区域以调度重绘（同 ISSUE-104，IsDirty 标志无读取方）。
        _engine.InvalidateElement(element);

        var inputArgs = new InputEventArgs
        {
            Target = element,
            Data = editable.Value ?? string.Empty,
            Bubbles = true
        };
        DispatchWithSyncContext(element, EventTypes.Input, inputArgs);
    }

    private void DispatchChange(Element element)
    {
        var changeArgs = new ChangeEventArgs
        {
            Target = element,
            Bubbles = true
        };
        DispatchWithSyncContext(element, EventTypes.Change, changeArgs);
    }

    // ---------------------------------------------------------------------
    // 辅助：滑块、滚动偏移、布局盒查找
    // ---------------------------------------------------------------------

    private void UpdateRangeValue(InputElement rangeInput, float mouseX)
    {
        var layoutBox = FindLayoutBoxForElement(rangeInput);
        if (layoutBox == null) return;

        var scrollOffset = GetAccumulatedScrollOffset(rangeInput);
        var contentRect = layoutBox.BoxModel.Content;
        float adjustedLeft = contentRect.Left - scrollOffset.x;
        float adjustedRight = contentRect.Right - scrollOffset.x;
        float height = contentRect.Height;

        float thumbRadius = Math.Min(height / 2 - 2, 8);
        float trackLeft = adjustedLeft + thumbRadius;
        float trackRight = adjustedRight - thumbRadius;
        float trackWidth = trackRight - trackLeft;

        if (trackWidth <= 0) return;

        float percentage = Math.Clamp((mouseX - trackLeft) / trackWidth, 0f, 1f);
        float newValue = rangeInput.Min + (rangeInput.Max - rangeInput.Min) * percentage;

        if (Math.Abs(rangeInput.NumericValue - newValue) > 0.01f)
        {
            rangeInput.NumericValue = newValue;
            // 必须经引擎标脏区域：IsDirty 是无读取方的内部标志（见 Element.IsDirty），
            // 仅靠它宿主渲染循环的稳态空闲检测（ISSUE-096）不会产生新帧，
            // 滑块要等到 :hover 等其他状态变化碰巧触发重绘才移动（ISSUE-104）。
            _engine.InvalidateElement(rangeInput);
            DispatchChange(rangeInput);
        }
    }

    private (float x, float y) GetAccumulatedScrollOffset(Element element)
    {
        float scrollX = 0, scrollY = 0;
        var current = element.Parent;
        while (current != null)
        {
            var box = FindLayoutBoxForElement(current);
            if (box != null)
            {
                scrollX += box.ScrollLeft;
                scrollY += box.ScrollTop;
            }
            current = current.Parent;
        }
        return (scrollX, scrollY);
    }

    private LayoutBox? FindLayoutBoxForElement(Element element)
    {
        var layout = _engine.GetCurrentLayout();
        if (layout == null) return null;
        return FindLayoutBoxRecursive(layout, element);
    }

    private RectF? GetRenderedBorderBox(Element element)
    {
        var box = FindLayoutBoxForElement(element);
        if (box == null) return null;

        var bounds = box.BoxModel.BorderBox;
        var scrollOffset = GetAccumulatedScrollOffset(element);
        return new RectF(bounds.X - scrollOffset.x, bounds.Y - scrollOffset.y,
            bounds.Width, bounds.Height);
    }

    private void DispatchPointerUp(float x, float y, MouseButton button)
    {
        DispatchCapturedPointerPair(EventTypes.PointerUp, EventTypes.MouseUp,
            x, y, button, isButtonPressed: false);
        _pointerDownTarget = null;
        _pointerDownBounds = null;
    }

    /// <summary>
    /// 取回捕获目标当前在场的实例，并把字段本身前推到该实例。
    ///
    /// <para>前推是为了让字段只握住最新一代：拖动期间每个 mousemove 都可能触发重渲染，字段若一直
    /// 停在第 0 代，就会经 <c>SupersededBy</c> 前向链把中间各代一并留住。
    /// <c>ResolveSuperseded</c> 的路径压缩只改写链头那一格，无法让字段自己松手。</para>
    /// </summary>
    private Element? ResolveAndReanchorPointerDownTarget()
    {
        ReanchorHoveredElements();
        if (_pointerDownTarget == null) return null;
        var resolved = _pointerDownTarget.ResolveSuperseded();
        // 重新锚定后，被跳过的各代不再从本字段可达，可以正常回收。
        _pointerDownTarget = resolved;
        return resolved;
    }

    private PointerEventArgs? DispatchCapturedPointerPair(
        string pointerEventType,
        string legacyMouseEventType,
        float x,
        float y,
        MouseButton button,
        bool isButtonPressed)
    {
        var target = ResolveAndReanchorPointerDownTarget();
        if (target == null) return null;

        return DispatchPointerPair(target, pointerEventType, legacyMouseEventType,
            x, y, button, isButtonPressed, _pointerDownBounds);
    }

    private void DispatchCapturedPointerEvent(
        string eventType,
        float x,
        float y,
        MouseButton button,
        bool isButtonPressed)
    {
        var target = ResolveAndReanchorPointerDownTarget();
        if (target == null) return;

        DispatchPointerEvent(target, eventType, x, y, button, isButtonPressed, _pointerDownBounds,
            _activePointerType, _activePointerId);
    }

    private PointerEventArgs DispatchPointerPair(
        Element target,
        string pointerEventType,
        string legacyMouseEventType,
        float x,
        float y,
        MouseButton button,
        bool isButtonPressed,
        RectF? bounds,
        PointerType? pointerType = null,
        int? pointerId = null)
    {
        var pointerArgs = DispatchPointerEvent(target, pointerEventType, x, y, button, isButtonPressed, bounds,
            pointerType ?? _activePointerType, pointerId ?? _activePointerId);
        DispatchPointerEvent(target.ResolveSuperseded(), legacyMouseEventType, x, y, button,
            isButtonPressed, bounds, pointerType ?? _activePointerType, pointerId ?? _activePointerId);
        return pointerArgs;
    }

    private PointerEventArgs DispatchPointerEvent(
        Element target,
        string eventType,
        float x,
        float y,
        MouseButton button,
        bool isButtonPressed,
        RectF? bounds,
        PointerType pointerType = PointerType.Mouse,
        int pointerId = 1)
    {
        var rect = bounds ?? GetRenderedBorderBox(target) ?? default;
        var args = new PointerEventArgs
        {
            Target = target,
            X = x,
            Y = y,
            OffsetX = x - rect.Left,
            OffsetY = y - rect.Top,
            TargetWidth = rect.Width,
            TargetHeight = rect.Height,
            ViewportWidth = _engine.ViewportWidth,
            ViewportHeight = _engine.ViewportHeight,
            IsButtonPressed = isButtonPressed,
            Button = button,
            PointerType = pointerType,
            PointerId = pointerId,
            Bubbles = true,
        };

        DispatchWithSyncContext(target, eventType, args);
        return args;
    }

    private void ScheduleLongPress()
    {
        if (_pointerDownTarget == null) return;

        var cancellation = new CancellationTokenSource();
        _longPressCancellation = cancellation;
        _ = Task.Delay(LongPressDelay, cancellation.Token).ContinueWith(
            task =>
            {
                if (task.IsCanceled) return;
                _dispatcher.Post(() =>
                {
                    lock (_sync)
                    {
                        if (cancellation.IsCancellationRequested || _pointerMoved ||
                            _pointerDownTarget == null)
                            return;

                        _longPressFired = true;
                        DispatchCapturedPointerEvent(EventTypes.LongPress,
                            _mouseDownPosition?.X ?? 0,
                            _mouseDownPosition?.Y ?? 0,
                            MouseButton.Left,
                            isButtonPressed: true);
                    }
                });
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void CancelLongPress()
    {
        _longPressCancellation?.Cancel();
        _longPressCancellation = null;
    }

    private void ResetPointerState()
    {
        _mouseDownPosition = null;
        _lastPointerPosition = null;
        _pointerDownTarget = null;
        _pointerDownBounds = null;
        _pointerMoved = false;
        _longPressFired = false;
        _isDragging = false;
        _draggingRange = null;
        _draggingScrollbar = null;
    }

    private static LayoutBox? FindLayoutBoxRecursive(LayoutBox box, Element element)
    {
        if (box.Element == element) return box;
        foreach (var child in box.Children)
        {
            var found = FindLayoutBoxRecursive(child, element);
            if (found != null) return found;
        }
        return null;
    }
}
