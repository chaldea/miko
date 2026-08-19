using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Animation;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Layout;
using Miko.Platform.Video;
using Miko.Rendering;
using Miko.Routing;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Core;

public class MikoEngine
{
    private readonly LayoutEngine _layoutEngine;
    private readonly RenderEngine _renderEngine;
    private readonly DirtyRegionManager _dirtyManager;
    private readonly EventDispatcher _eventDispatcher;
    private readonly AnimationManager _animationManager;
    private readonly Platform.MikoDispatcher _dispatcher;
    private List<StyleSheet> _styleSheets = new();
    private ILogger _logger = NullLogger.Instance;

    public MikoEngine(
        LayoutEngine layoutEngine,
        RenderEngine renderEngine,
        DirtyRegionManager dirtyManager,
        EventDispatcher eventDispatcher,
        AnimationManager animationManager,
        Platform.MikoDispatcher dispatcher,
        ILogger<MikoEngine>? logger = null)
    {
        _layoutEngine = layoutEngine;
        _renderEngine = renderEngine;
        _dirtyManager = dirtyManager;
        _eventDispatcher = eventDispatcher;
        _animationManager = animationManager;
        _dispatcher = dispatcher;
        if (logger != null) _logger = logger;
    }

    public MikoEngine() : this(new(), new(), new(), new(), new(), new()) { }

    public void SetLogger(ILogger logger)
    {
        _logger = logger;
        _animationManager.SetLogger(logger);
    }

    private Element? _root;
    private LayoutBox? _currentLayout;
    private float _viewportWidth;
    private float _viewportHeight;
    private SafeAreaInsets _safeArea;

    // 页面转场状态（ISSUE-108）：转场期间旧页面树（leaving 层）被保留，与新页面树
    // （entering 层，即 _root/_currentLayout）作为两个叠放图层共同绘制；
    // 转场结束或被新导航打断时 leaving 层被丢弃。
    private Element? _navLeavingRoot;
    private LayoutBox? _navLeavingLayout;
    private NavigationTransitionContext? _navContext;
    private NavigationTransition? _navTransition;
    private float _navElapsed;

    /// <summary>
    /// 视频后端。由平台宿主在初始化时从 DI 注入（未注册视频后端时为 null，
    /// <c>&lt;video&gt;</c> 元素将只显示背景/poster）。
    /// </summary>
    public IVideoBackend? VideoBackend { get; set; }

    /// <summary>
    /// 图片资源加载器。由平台宿主在初始化时从 DI 注入（默认注入内置 <c>ResourceManager</c>）。
    /// 为 null 时 <c>&lt;img&gt;</c> 不会自动加载，需应用层自行填充 <c>Bitmap</c>。
    /// </summary>
    public Platform.Resources.IImageLoader? ImageLoader { get; set; }

    /// <summary>
    /// 当前 GPU 上下文。GPU 宿主（桌面 OpenGL / 移动 GLES/Metal）每帧设置，
    /// 供视频帧源把解码 GPU 资源零拷贝包装为图像；转发给底层渲染引擎。
    /// </summary>
    public GRContext? GraphicsContext
    {
        get => _renderEngine.GraphicsContext;
        set => _renderEngine.GraphicsContext = value;
    }

    /// <summary>
    /// 语法高亮器（<c>&lt;code language="..."&gt;</c>）。默认内置实现；
    /// 由 DI 容器注册（MikoAppBuilder.CreateDefault），应用可重新注册
    /// <see cref="Highlight.ISyntaxHighlighter"/> 覆盖，初始化时解析并转发给渲染引擎。
    /// </summary>
    public Highlight.ISyntaxHighlighter SyntaxHighlighter
    {
        get => _renderEngine.SyntaxHighlighter;
        set => _renderEngine.SyntaxHighlighter = value;
    }

    // 已激活的视频会话，按元素身份索引；用于跨重建复用与移除时回收。
    private readonly Dictionary<VideoElement, IVideoSession> _videoSessions = new();

    // 已发起加载的图片元素，按元素身份索引；避免每帧重复请求。值为加载任务。
    private readonly Dictionary<ImageElement, Task> _imageLoads = new();

    // 已发起占位图加载的元素，避免重复请求。
    private readonly HashSet<ImageElement> _placeholderLoads = new();

    // 外部线程（视频解码线程等）投递的失效请求，在帧开始时于主线程排空。
    private readonly List<Element> _pendingInvalidations = new();
    private readonly object _pendingInvalidationsLock = new();

    // 按路由路径保存的滚动快照（ISSUE-118）：跨「页面被完全销毁再重建」的返回恢复。
    private readonly ScrollSnapshotStore _scrollSnapshots = new();

    /// <summary>
    /// 以新根元素初始化（导航重建）引擎。
    /// <para><paramref name="transition"/> 描述本次导航（方向 + 起止路径 + 可选转场效果）：
    /// 其 <see cref="NavigationTransitionInfo.Transition"/> 非空且存在旧页面时，旧页面树被保留为
    /// leaving 图层，与新页面共同绘制直到转场完成（ISSUE-108）；否则瞬时切换。无论是否有转场效果，
    /// 方向与路径都用于维护按路径的滚动快照，使返回上一页时能恢复其滚动位置（ISSUE-118）。
    /// 为 null 表示非导航重建（如热重载），不触碰快照。</para>
    /// </summary>
    public void Initialize(Element root, List<StyleSheet> styleSheets, SKCanvas canvas, float viewportWidth, float viewportHeight, NavigationTransitionInfo? transition = null)
    {
        // Capture old layout for scroll position restoration (ISSUE-092)
        var oldLayout = _currentLayout;

        // 导航离开当前页：为来源路径拍下滚动快照，供之后返回该页时回放（ISSUE-118）。
        // 必须在 _currentLayout 被新树替换之前完成——偏移就存放在即将被丢弃的旧布局树上。
        CaptureScrollSnapshot(transition, oldLayout);

        // 新一轮导航打断进行中的转场：直接丢弃 leaving 层
        // （其视频会话由下方 SyncVideoSessions 随旧树移除而回收）。
        CancelNavigationTransition();

        // 请求转场且存在旧页面时，保留旧页面树作为 leaving 层参与后续绘制。
        // 首帧导航（无旧页面/无旧布局）或零时长转场退化为普通瞬时切换。
        bool startTransition = transition?.Transition != null
            && transition.Transition.Duration > 0
            && _root != null
            && _currentLayout != null;
        if (startTransition)
        {
            _navLeavingRoot = _root;
            _navLeavingLayout = _currentLayout;
        }

        // Transfer old LayoutBox references to new elements for transition detection
        if (_root != null)
        {
            MapElementIdentityRecursive(_root, root);
        }

        _root = root;
        _styleSheets = styleSheets;
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;

        _animationManager.Clear();
        EnsureParentReferences(root);
        _renderEngine.SetCanvas(canvas);

        _logger.LogInformation("Engine initialized with viewport {Width}x{Height}", viewportWidth, viewportHeight);

        // 对齐动画条目并补写当前进度值（ISSUE-127）。这里 _animationManager 刚被 Clear，
        // 通常无事可做；保留调用是为了让三条渲染路径的顺序保持一致。
        ReconcileAnimationTargets(root);

        // Capture old styles from transferred LayoutBoxes (before layout replaces them)
        var oldStyles = CaptureTransitionableStyles(root);

        _currentLayout = _layoutEngine.Layout(root, _styleSheets, viewportWidth, viewportHeight, _safeArea);

        // Restore scroll positions from old layout (ISSUE-092)
        RestoreScrollState(oldLayout, _currentLayout, IsCrossPageNavigation(transition));
        // 返回上一页时回放该页离开时的滚动快照（ISSUE-118）。放在 ISSUE-092 的恢复之后：
        // 跨页面返回以快照为准，同树内重渲染没有快照条目、仍由上面那步处理，两者不冲突。
        RestoreScrollSnapshot(transition, _currentLayout);

        if (oldStyles.Elements.Count > 0 || oldStyles.PseudoElements.Count > 0)
        {
            bool transitionsTriggered = DetectAndTriggerTransitions(root, oldStyles);
            if (transitionsTriggered)
            {
                _currentLayout = _layoutEngine.Layout(root, _styleSheets, viewportWidth, viewportHeight, _safeArea);
                // Restore scroll state again after re-layout
                RestoreScrollState(oldLayout, _currentLayout, IsCrossPageNavigation(transition));
                RestoreScrollSnapshot(transition, _currentLayout);
            }
        }

        // 同步视频会话（创建新元素的会话、回收已移除元素的会话）。
        SyncVideoSessions(root);
        // 同步图片源（为新 <img> 发起异步加载、解码占位图）。
        SyncImageSources(root);

        if (startTransition)
        {
            // startTransition 为真已蕴含 transition 与其 Transition 均非空。
            _navTransition = transition!.Transition!;
            _navElapsed = 0f;
            _navContext = new NavigationTransitionContext(
                _navLeavingRoot!, root, transition.Direction,
                transition.FromPath, transition.ToPath, viewportWidth, viewportHeight);
            _navTransition.OnStart(_navContext);
            // 首帧即以 progress=0 的图层状态绘制，避免新页面在初始位置闪烁一帧。
            _navTransition.Apply(_navContext, EaseProgress(_navTransition, 0f));
            _logger.LogDebug("Navigation transition started: {From} -> {To} ({Direction}), effect={Effect}, duration={Duration}s",
                transition.FromPath, transition.ToPath, transition.Direction,
                _navTransition.GetType().Name, _navTransition.Duration);
            RenderTransitionFrame();
        }
        else
        {
            if (transition?.Transition != null)
            {
                _logger.LogDebug("Navigation transition skipped (first navigation or non-positive duration): {From} -> {To} ({Direction})",
                    transition.FromPath, transition.ToPath, transition.Direction);
            }
            _renderEngine.Render(_currentLayout);
        }

        // 本次返回导航的快照已回放完毕（可能因 transition 重新布局而回放了两次），消费掉它。
        ConsumeScrollSnapshot(transition);

        ScanAndStartAnimations(root);
    }

    private static void MapElementIdentityRecursive(Element oldElement, Element newElement)
    {
        // 仅在新旧元素表示"同一个"元素时才迁移 LayoutBox。
        // 否则旧元素的 LayoutBox（携带其 transition 定义与计算样式）会被错误地挂到
        // 结构位置相同但语义不同的新元素上，导致旧页面的 transition 在新页面上被触发
        // （见 ISSUE-043：切换页面时 .btn 的背景色动画在 /form 页面上播放）。
        if (!IsSameElementIdentity(oldElement, newElement)) return;

        if (oldElement.LayoutBox != null)
        {
            newElement.LayoutBox = oldElement.LayoutBox;
        }

        int count = Math.Min(oldElement.Children.Count, newElement.Children.Count);
        for (int i = 0; i < count; i++)
        {
            MapElementIdentityRecursive(oldElement.Children[i], newElement.Children[i]);
        }
    }

    /// <summary>
    /// 判断两个元素是否表示"同一个"元素，用于跨重建（导航/重新渲染）的身份保持。
    /// 以标签名为身份依据：
    /// - 切换页面时结构位置相同但标签不同的元素（如 button.btn ↔ input.form-control）不会被误配，
    ///   避免旧页面的 transition 状态被错误迁移（见 ISSUE-043）。
    /// - 同一元素仅 class 变化（如 div "panel" ↔ "panel open"）仍视为同一元素，
    ///   从而保留 Razor 重新渲染时基于状态变化触发 transition 的能力。
    /// </summary>
    private static bool IsSameElementIdentity(Element a, Element b)
    {
        return a.TagName == b.TagName;
    }

    // ---------------------------------------------------------------------
    // 页面转场（ISSUE-108）
    // ---------------------------------------------------------------------

    /// <summary>
    /// 是否有进行中的页面转场。转场期间旧页面树仍作为 leaving 图层参与绘制，
    /// 命中测试则始终作用于 entering 页面树（旧页面不可交互）。
    /// </summary>
    public bool IsNavigationTransitionActive => _navContext != null;

    /// <summary>
    /// 推进页面转场时钟（每帧调用一次，<paramref name="deltaTime"/> 单位秒）。
    /// 到达转场时长后以 progress=1 应用终态并回收 leaving 层；无进行中的转场时为 no-op。
    /// </summary>
    public void AdvanceNavigationTransition(float deltaTime)
    {
        if (_navContext == null || _navTransition == null) return;

        _navElapsed += deltaTime;
        float linear = _navTransition.Duration <= 0 ? 1f : Math.Clamp(_navElapsed / _navTransition.Duration, 0f, 1f);
        _logger.LogTrace("Navigation transition frame: dt={Delta:F4}s elapsed={Elapsed:F3}s progress={Progress:F3} ({From} -> {To})",
            deltaTime, _navElapsed, linear, _navContext.FromPath, _navContext.ToPath);
        _navTransition.Apply(_navContext, EaseProgress(_navTransition, linear));

        if (linear < 1f) return;

        // 自然完成：回收 leaving 层，全量重绘一次去除旧图层的屏幕残影，
        // 并同步视频会话（leaving 树的 <video> 随树移除而回收）。
        var transition = _navTransition;
        var context = _navContext;
        ClearNavigationTransitionState();
        _logger.LogDebug("Navigation transition completed: {From} -> {To} ({Direction})",
            context.FromPath, context.ToPath, context.Direction);
        transition.OnEnd(context);
        if (_root != null)
        {
            SyncVideoSessions(_root);
            InvalidateElement(_root);
        }
    }

    /// <summary>丢弃转场状态（不触发 OnEnd；新导航打断或视口/安全区变化时调用）。</summary>
    private void CancelNavigationTransition()
    {
        if (_navContext != null)
        {
            _logger.LogDebug("Navigation transition canceled: {From} -> {To} ({Direction})",
                _navContext.FromPath, _navContext.ToPath, _navContext.Direction);
        }
        ClearNavigationTransitionState();
    }

    private void ClearNavigationTransitionState()
    {
        _navLeavingRoot = null;
        _navLeavingLayout = null;
        _navContext = null;
        _navTransition = null;
        _navElapsed = 0f;
    }

    private static float EaseProgress(NavigationTransition transition, float linear)
        => EasingFunctions.Evaluate(transition.TimingFunction, linear, transition.CubicBezier);

    /// <summary>
    /// 绘制一帧转场画面：leaving/entering 两棵布局树按上下文中的叠放次序、
    /// 偏移与不透明度作为两个图层依次绘制，最后统一绘制一次覆盖层。
    /// </summary>
    private void RenderTransitionFrame()
    {
        if (_navContext == null || _currentLayout == null) return;

        var ctx = _navContext;
        if (ctx.EnteringBelow)
        {
            _renderEngine.RenderLayer(_currentLayout, ctx.EnteringOffsetX, ctx.EnteringOffsetY, ctx.EnteringOpacity);
            if (_navLeavingLayout != null)
                _renderEngine.RenderLayer(_navLeavingLayout, ctx.LeavingOffsetX, ctx.LeavingOffsetY, ctx.LeavingOpacity);
        }
        else
        {
            if (_navLeavingLayout != null)
                _renderEngine.RenderLayer(_navLeavingLayout, ctx.LeavingOffsetX, ctx.LeavingOffsetY, ctx.LeavingOpacity);
            _renderEngine.RenderLayer(_currentLayout, ctx.EnteringOffsetX, ctx.EnteringOffsetY, ctx.EnteringOpacity);
        }
        _renderEngine.RenderOverlay();
    }

    /// <summary>绘制当前帧：转场期间绘制双层转场画面，否则常规全量渲染。</summary>
    private void RenderCurrentFrame()
    {
        if (_navContext != null)
            RenderTransitionFrame();
        else
            _renderEngine.Render(_currentLayout!);
    }

    public void Update(SKCanvas canvas)
    {
        if (_root == null) throw new InvalidOperationException("Engine not initialized. Call Initialize first.");

        // 排空跨线程失效请求（视频解码线程投递的新帧/加载完成）。
        _dispatcher.Drain();
        DrainPendingInvalidations();
        SyncVideoSessions(_root);
        SyncImageSources(_root);

        _renderEngine.SetCanvas(canvas);

        // 页面转场期间（ISSUE-108）：每帧无条件整体重绘两个图层（转场动画连续改变
        // 图层偏移/透明度，脏区域模型不适用）；进入页 DOM 变化仍照常触发重排。
        if (_navContext != null)
        {
            if (_dirtyManager.HasDirtyRegions())
            {
                var oldLayout = _currentLayout;
                _currentLayout = _layoutEngine.Layout(_root, _styleSheets, _viewportWidth, _viewportHeight, _safeArea);
                RestoreScrollState(oldLayout, _currentLayout);
            }
            RenderTransitionFrame();
            _dirtyManager.Clear();
            return;
        }

        if (_dirtyManager.HasDirtyRegions())
        {
            var dirtyRegions = _dirtyManager.GetDirtyRegions();
            var oldLayout = _currentLayout;
            _currentLayout = _layoutEngine.Layout(_root, _styleSheets, _viewportWidth, _viewportHeight, _safeArea);
            RestoreScrollState(oldLayout, _currentLayout);

            // 脏区域过多时，增量渲染会退化为多次全树遍历，成本超过一次全量渲染，
            // 此时回退到全量渲染（见基准报告 §2 拐点）。
            if (dirtyRegions.Count > _renderEngine.MaxIncrementalDirtyRegions)
            {
                _logger.LogDebug("Full update fallback, {Count} dirty regions exceed threshold {Threshold}",
                    dirtyRegions.Count, _renderEngine.MaxIncrementalDirtyRegions);
                _renderEngine.Render(_currentLayout);
            }
            else
            {
                _logger.LogDebug("Incremental update, {Count} dirty regions", dirtyRegions.Count);
                _renderEngine.RenderDirty(_currentLayout, dirtyRegions);
            }
        }
    }

    public void Render(SKCanvas canvas)
    {
        if (_root == null) throw new InvalidOperationException("Engine not initialized. Call Initialize first.");

        // 排空跨线程失效请求（如视频解码线程投递的新帧/加载完成）。
        _dispatcher.Drain();
        DrainPendingInvalidations();

        _renderEngine.SetCanvas(canvas);

        // 把动画/过渡条目对齐到重渲染后的在场元素、回收停掉的条目，并把当前进度值补写回
        // 行内样式（ISSUE-127）。必须早于 IsLayoutCurrent：这些写入会改动行内样式（即布局输入），
        // 先判定就会用上一帧的结论走快速路径，把改动漏到下一帧。
        ReconcileAnimationTargets(_root);

        // 快速路径（ISSUE-096）：布局输入（DOM/样式/视口/安全区）自上次布局后未变，
        // 直接复用现有布局树。此时不可能有新的 transition 触发（transition 由样式变化引起，
        // 而任何样式变化都会递增变更版本号），故一并跳过 transition 检测的整树扫描。
        if (_currentLayout != null
            && _layoutEngine.IsLayoutCurrent(_root, _styleSheets, _viewportWidth, _viewportHeight, _safeArea))
        {
            // 同步视频会话（DOM 可能在 Razor 重渲染中增删 <video>）。
            SyncVideoSessions(_root);
            // 同步图片源（DOM 可能在 Razor 重渲染中增删 <img>）。
            SyncImageSources(_root);
            // DOM 可能在 Razor 重渲染中新增带动画的元素（如 IonLoading 打开时才渲染 IonSpinner），
            // 扫描并启动新元素上的 Style.Animations（已启动的动画不会重复启动）。
            ScanAndStartAnimations(_root);

            RenderCurrentFrame();
            _dirtyManager.Clear();
            return;
        }

        var oldStyles = CaptureTransitionableStyles(_root);
        var oldLayout = _currentLayout;
        _currentLayout = _layoutEngine.Layout(_root, _styleSheets, _viewportWidth, _viewportHeight, _safeArea);

        bool transitionsTriggered = DetectAndTriggerTransitions(_root, oldStyles);
        if (transitionsTriggered)
        {
            // 重新布局，使用 transition 起始值（已写入 inline style）
            _currentLayout = _layoutEngine.Layout(_root, _styleSheets, _viewportWidth, _viewportHeight, _safeArea);
        }

        // 同步视频会话（DOM 可能在 Razor 重渲染中增删 <video>）。
        SyncVideoSessions(_root);
        // 同步图片源（DOM 可能在 Razor 重渲染中增删 <img>）。
        SyncImageSources(_root);
        // DOM 可能在 Razor 重渲染中新增带动画的元素（如 IonLoading 打开时才渲染 IonSpinner），
        // 扫描并启动新元素上的 Style.Animations（已启动的动画不会重复启动）。
        ScanAndStartAnimations(_root);

        RestoreScrollState(oldLayout, _currentLayout);
        RenderCurrentFrame();
        _dirtyManager.Clear();
    }

    /// <summary>
    /// 标记元素为脏（需要重绘）
    /// </summary>
    public void InvalidateElement(Element element)
    {
        _dirtyManager.MarkDirty(element);
    }

    /// <summary>
    /// 线程安全的失效入口。供外部线程（视频解码线程、异步资源加载等）调用：
    /// 仅把元素入队，真正的标脏在下一帧主循环（<see cref="DrainPendingInvalidations"/>）执行，
    /// 避免与布局/渲染对 DOM 的遍历并发。
    /// </summary>
    public void PostInvalidate(Element element)
    {
        lock (_pendingInvalidationsLock)
        {
            _pendingInvalidations.Add(element);
        }
    }

    /// <summary>是否有待处理的跨线程失效请求（平台宿主据此决定是否需要再绘制一帧）。</summary>
    public bool HasPendingInvalidations
    {
        get { lock (_pendingInvalidationsLock) return _pendingInvalidations.Count > 0; }
    }

    /// <summary>
    /// 引擎是否有未呈现的视觉工作（平台宿主据此决定是渲染新帧还是空闲等待，见 ISSUE-096）。
    /// 为 false 时表示屏幕上已是最新内容：无脏区域、无待排队的回调、无运行中的动画、
    /// 无播放中的视频，且布局输入（DOM/样式/视口/安全区）自上次渲染后未变。
    /// 宿主的输入事件（指针、键盘、滚动）会直接修改元素状态/滚动偏移并标脏，
    /// 因此无需额外计入。
    /// </summary>
    public bool HasPendingVisualWork
    {
        get
        {
            if (HasPendingRenderWork) return true;
            // 布局输入已变（DOM/样式/视口/安全区）→ 需要重排重绘
            if (!_layoutEngine.IsLayoutCurrent(_root!, _styleSheets, _viewportWidth, _viewportHeight, _safeArea)) return true;
            return false;
        }
    }

    /// <summary>
    /// 与 <see cref="HasPendingVisualWork"/> 相同，但**不含**「布局输入是否变化」这一项。
    /// <para>专供**同进程内的次级引擎**（如 DevTools 的独立窗口）判断空闲：
    /// <c>Element.MutationVersion</c> 是进程级全局静态，任何引擎的 DOM 变更都会递增它，
    /// 因此次级引擎的 <c>IsLayoutCurrent</c> 会被其他窗口的活动持续击穿而恒为 false，
    /// 使 <see cref="HasPendingVisualWork"/> 恒为 true、永远无法空闲（见 ISSUE-117）。
    /// 这类宿主需自行判断其 DOM 是否真的需要重建，再用本属性捕获引擎内部的工作
    /// （脏区域、动画、跨线程失效等）。</para>
    /// <para>常规的单引擎宿主应使用 <see cref="HasPendingVisualWork"/>。</para>
    /// </summary>
    public bool HasPendingRenderWork
    {
        get
        {
            if (_root == null || _currentLayout == null) return true;   // 首帧尚未渲染
            if (_dispatcher.HasPendingActions) return true;             // 排队回调可能修改 DOM
            if (HasPendingInvalidations) return true;                   // 跨线程失效（视频帧、图片加载）
            if (_dirtyManager.HasDirtyRegions()) return true;           // 已标脏未绘制
            if (_animationManager.HasActiveAnimations) return true;     // 动画/过渡逐帧推进
            if (_navContext != null) return true;                       // 页面转场逐帧推进（ISSUE-108）
            // 加载中/播放中的视频会持续投递新帧
            foreach (var session in _videoSessions.Values)
            {
                if (session.State is VideoSessionState.Loading or VideoSessionState.Playing) return true;
            }
            return false;
        }
    }

    private void DrainPendingInvalidations()
    {
        List<Element>? batch = null;
        lock (_pendingInvalidationsLock)
        {
            if (_pendingInvalidations.Count == 0) return;
            batch = new List<Element>(_pendingInvalidations);
            _pendingInvalidations.Clear();
        }
        foreach (var element in batch)
            _dirtyManager.MarkDirty(element);
    }

    public AnimationManager AnimationManager => _animationManager;

    public void RegisterAnimation(KeyframeAnimation animation)
    {
        _logger.LogDebug("MikoEngine.RegisterAnimation: \"{Name}\"", animation.Name);
        _animationManager.RegisterAnimation(animation);
    }

    public void StartAnimation(Element element, string animationName)
    {
        _logger.LogDebug("MikoEngine.StartAnimation: \"{Name}\" on <{Tag} id=\"{Id}\">",
            animationName, element.TagName, element.Id ?? "");
        _animationManager.StartAnimation(element, animationName);
    }

    public void StartAnimation(Element element, KeyframeAnimation animation)
    {
        _logger.LogDebug("MikoEngine.StartAnimation: \"{Name}\" on <{Tag} id=\"{Id}\">",
            animation.Name, element.TagName, element.Id ?? "");
        _animationManager.StartAnimation(element, animation);
    }

    public void StopAnimation(Element element, string? animationName = null)
    {
        _logger.LogDebug("MikoEngine.StopAnimation: \"{Name}\" on <{Tag} id=\"{Id}\">",
            animationName ?? "(all)", element.TagName, element.Id ?? "");
        _animationManager.StopAnimation(element, animationName);
    }

    public void Tick(float deltaTime, SKCanvas canvas)
    {
        if (_root == null) throw new InvalidOperationException("Engine not initialized. Call Initialize first.");

        if (!_animationManager.HasActiveAnimations && !_dirtyManager.HasDirtyRegions() && _navContext == null)
        {
            _logger.LogTrace("Tick: no active animations or dirty regions, skipping");
            return;
        }

        _logger.LogTrace("Tick: deltaTime={DeltaTime}s, activeAnimations={HasAnim}, dirtyRegions={HasDirty}",
            deltaTime, _animationManager.HasActiveAnimations, _dirtyManager.HasDirtyRegions());
        _animationManager.Update(deltaTime);
        AdvanceNavigationTransition(deltaTime);
        Render(canvas);
    }

    /// <summary>
    /// 设置视口大小
    /// </summary>
    public void SetViewportSize(float width, float height)
    {
        if (Math.Abs(_viewportWidth - width) > 0.01f || Math.Abs(_viewportHeight - height) > 0.01f)
        {
            _viewportWidth = width;
            _viewportHeight = height;

            // 视口变化使 leaving 层的旧布局失效，直接结束转场（ISSUE-108）。
            CancelNavigationTransition();

            // 视口变化需要完整重新布局
            if (_root != null)
            {
                InvalidateElement(_root);
            }
        }
    }

    /// <summary>当前安全区边距（逻辑像素）。</summary>
    public SafeAreaInsets SafeAreaInsets => _safeArea;

    /// <summary>
    /// 设置安全区边距（逻辑像素）。由平台宿主从系统状态栏/导航栏获取后传入。
    /// 值发生变化时触发完整重新布局，使根元素内缩到安全区内。
    /// </summary>
    public void SetSafeAreaInsets(SafeAreaInsets insets)
    {
        if (_safeArea == insets) return;

        _safeArea = insets;

        // 安全区变化使 leaving 层的旧布局失效，直接结束转场（ISSUE-108）。
        CancelNavigationTransition();

        // 安全区变化需要完整重新布局（与视口变化同理）
        if (_root != null)
        {
            InvalidateElement(_root);
        }
    }

    /// <summary>
    /// 使缓存的布局结果失效，强制下一帧完整重排（ISSUE-096）。
    /// 常规变更（DOM 结构、文本、class、行内样式替换、元素状态、视口、安全区、
    /// <see cref="AddStyleSheet"/>）都会被自动检测，无需调用本方法。
    /// 仅在引擎无法察觉的变更后调用——主要是**就地改写了已添加样式表的规则内容**
    /// （样式表对象图按不可变约定对待；替换样式表列表本身会被自动检测），
    /// 或运行时注册新字体导致文本度量变化。
    /// </summary>
    public void InvalidateLayoutCache() => _layoutEngine.InvalidateCache();

    /// <summary>
    /// 添加样式表
    /// </summary>
    public void AddStyleSheet(StyleSheet styleSheet)
    {
        _styleSheets.Add(styleSheet);

        // 样式变化需要重新布局
        Element.BumpMutationVersion();
        if (_root != null)
        {
            InvalidateElement(_root);
        }
    }

    /// <summary>
    /// 获取当前布局树
    /// </summary>
    public LayoutBox? GetCurrentLayout() => _currentLayout;

    /// <summary>
    /// 获取根元素
    /// </summary>
    public Element? GetRoot() => _root;

    /// <summary>
    /// 根元素的已解析背景色。平台宿主用它填充整个 surface（含安全区系统栏带），
    /// 使状态栏/导航栏后方的颜色与内容背景一致。根背景透明时返回 null。
    /// </summary>
    public Color? GetRootBackgroundColor()
    {
        var bg = _currentLayout?.ComputedStyle.BackgroundColor;
        if (bg == null || bg.Value.A == 0) return null;
        return bg;
    }

    /// <summary>
    /// 在指定坐标处进行命中测试，返回最深层的元素
    /// </summary>
    public Element? HitTest(float x, float y)
    {
        if (_currentLayout == null) return null;

        // position: fixed 的盒子由渲染器的顶层 pass（RenderEngine.FlushFixed）最后绘制，因此在
        // 命中测试里也必须最先被测——否则会「看得见但点不到」：正常递归会在裁剪型祖先处被
        // `!insideSelf && clipsChildren` 剪枝掉，而 fixed 覆盖层恰恰总是溢出到祖先之外。
        // 见 issues/ion-select.md 问题 4（ion-item 里的 select 覆盖层）。
        var fixedBoxes = CollectFixedBoxes(_currentLayout);
        if (fixedBoxes != null)
        {
            // 绘制是 z-index 升序（后画者在上），命中则反向：从最上层往下测。
            for (int i = fixedBoxes.Count - 1; i >= 0; i--)
            {
                var hit = HitTestBox(fixedBoxes[i], x, y);
                if (hit != null) return hit;
            }
        }

        return HitTestBox(_currentLayout, x, y);
    }

    /// <summary>
    /// 收集整棵树里所有 <c>position: fixed</c> 的盒子，按 z-index 升序（与
    /// <see cref="Rendering.RenderEngine.FlushFixed"/> 的绘制顺序一致）返回；没有则返回 null。
    /// <para>
    /// 不下探进 fixed 盒自身的子树：那里面的后代由该 fixed 盒的递归命中测试自行处理；若其中还
    /// 嵌着更深的 fixed 盒，它们同样在那次递归里被测到。
    /// </para>
    /// </summary>
    private static List<LayoutBox>? CollectFixedBoxes(LayoutBox root)
    {
        List<LayoutBox>? found = null;
        Collect(root, ref found);
        // 绝大多数树里一个 fixed 都没有；此时不分配任何东西就返回（HitTest 每次鼠标移动都会
        // 走这条路来跟踪 :hover，见 MikoInteractionController.OnMouseMove）。
        if (found == null) return null;
        if (found.Count == 1) return found;

        // 收集顺序为文档序，List.Sort 不稳定，故按 (z-index, 文档序) 双键排序保持同值的文档序。
        var order = new Dictionary<LayoutBox, int>(found.Count);
        for (int i = 0; i < found.Count; i++) order[found[i]] = i;
        found.Sort((a, b) =>
        {
            int byZ = a.ComputedStyle.ZIndex.CompareTo(b.ComputedStyle.ZIndex);
            return byZ != 0 ? byZ : order[a].CompareTo(order[b]);
        });
        return found;

        static void Collect(LayoutBox box, ref List<LayoutBox>? found)
        {
            foreach (var child in box.Children)
            {
                if (child.ComputedStyle.Position == Common.Position.Fixed)
                {
                    (found ??= new List<LayoutBox>()).Add(child);
                    continue;
                }

                Collect(child, ref found);
            }
        }
    }

    /// <summary>
    /// 元素的 Hover 状态是否可能影响任一样式表的规则匹配（见 StyleSheet.IsHoverRelevant）。
    /// 交互层据此决定悬停状态变化是否需要标脏触发样式重算（ISSUE-104 问题1）：
    /// 为 false 时悬停仅作为元素标志位跟踪，不产生任何重排/重绘工作。
    /// </summary>
    internal bool IsHoverRelevant(Element element)
    {
        for (int i = 0; i < _styleSheets.Count; i++)
        {
            if (_styleSheets[i].IsHoverRelevant(element)) return true;
        }
        return false;
    }

    private Element? HitTestBox(LayoutBox box, float x, float y, float scrollOffsetX = 0, float scrollOffsetY = 0)
    {
        // 文本节点（TextNode）对命中透明：点击文本时命中目标应解析为其包含元素（如 button），
        // 而非匿名文本节点本身，否则会破坏事件处理与冒泡（见 ISSUE-086）。
        if (box.Element is TextNode)
            return null;

        var rect = box.BoxModel.BorderBox;
        float adjustedLeft = rect.Left - scrollOffsetX;
        float adjustedRight = rect.Right - scrollOffsetX;
        float adjustedTop = rect.Top - scrollOffsetY;
        float adjustedBottom = rect.Bottom - scrollOffsetY;

        bool insideSelf = x >= adjustedLeft && x <= adjustedRight && y >= adjustedTop && y <= adjustedBottom;

        // 跨行的非替换 inline 盒由逐行片段组成（ISSUE-126）：其边框盒并集覆盖了首行行尾与
        // 末行行首之外的空白区域，按并集命中会让 <span> 吃掉这些空白处的点击。
        // 命中判定改为「落在任一片段内」，与逐片段绘制的可视范围一致。
        if (insideSelf && box.InlineFragments is { Count: > 0 } fragments)
        {
            insideSelf = false;
            foreach (var frag in fragments)
            {
                if (x >= frag.Left - scrollOffsetX && x <= frag.Right - scrollOffsetX
                    && y >= frag.Top - scrollOffsetY && y <= frag.Bottom - scrollOffsetY)
                {
                    insideSelf = true;
                    break;
                }
            }
        }

        // overflow:visible 的盒子不裁剪后代：溢出到盒外的子孙（绝对定位、负外边距等）在 CSS 中
        // 依然可命中，因此点在盒外时不能就此返回——仍需下探子树，只是本盒自身不能作为命中目标。
        // 反之，裁剪型盒子（overflow 非 visible，或已滚动）之外的一切都不可命中，可立即剪枝。
        // 缺了这条区分，比自身内容小的容器会吞掉子孙的点击：ion-fab 的 fit-content 宿主只有
        // 主按钮那么高，展开后的 ion-fab-list 整体落在宿主之外，列表按钮点不到（issues/ion-fab.md）。
        bool clipsChildren = box.ScrollTop > 0 || box.ScrollLeft > 0
            || box.ComputedStyle.OverflowY != Overflow.Visible
            || box.ComputedStyle.OverflowX != Overflow.Visible;

        if (!insideSelf && clipsChildren)
            return null;

        float childScrollOffsetX = scrollOffsetX + box.ScrollLeft;
        float childScrollOffsetY = scrollOffsetY + box.ScrollTop;

        // HitTest must respect z-index order like rendering does. Collect positioned descendants with
        // z-index (穿透 non-stacking-context ancestors like CollectZOrderedDescendants does), test them
        // in descending z-index order (highest first), then test remaining children in reverse DOM order.
        // This ensures the fab (z-index:1000) is tested before the header (z-index:10) even though they
        // are uncle/nephew, not siblings (issues/ion-fab.md problem 3, issues/ion-menu.md problem 1).
        var zOrdered = CollectZOrderedDescendantsForHitTest(box);
        if (zOrdered != null)
        {
            // Test in descending z-index order (highest first)
            for (int i = zOrdered.Count - 1; i >= 0; i--)
            {
                var descendant = zOrdered[i];
                var hit = HitTestBox(descendant, x, y, childScrollOffsetX, childScrollOffsetY);
                if (hit != null) return hit;
            }
        }

        // Test remaining children (non-positioned or positioned without z-index, or deferred by clipping)
        // in reverse DOM order
        var deferred = zOrdered?.ToHashSet();
        for (int i = box.Children.Count - 1; i >= 0; i--)
        {
            var child = box.Children[i];
            // Skip children already tested above
            if (deferred?.Contains(child) == true)
                continue;

            // fixed 后代已在 HitTest 入口的顶层 pass 中测过（且不受本盒裁剪影响），跳过。
            if (child.ComputedStyle.Position == Common.Position.Fixed)
                continue;

            var childRect = child.BoxModel.BorderBox;
            float childScreenTop = childRect.Top - childScrollOffsetY;
            float childScreenBottom = childRect.Bottom - childScrollOffsetY;
            float childScreenLeft = childRect.Left - childScrollOffsetX;
            float childScreenRight = childRect.Right - childScrollOffsetX;

            bool isClipped = clipsChildren &&
                             (childScreenBottom < adjustedTop || childScreenTop > adjustedBottom ||
                              childScreenRight < adjustedLeft || childScreenLeft > adjustedRight);

            if (isClipped) continue;

            var hit = HitTestBox(child, x, y, childScrollOffsetX, childScrollOffsetY);
            if (hit != null) return hit;
        }

        // 点落在本盒之外（只可能来自上面的 overflow:visible 下探）：本盒不是命中目标，
        // 但其溢出的子孙已在上面测过。
        if (!insideSelf)
            return null;

        // pointer-events:none makes this element transparent to hits — the tap passes
        // through to whatever is behind it (descendants were already tested above and can
        // still be hit if they reset pointer-events to auto).
        if (box.ComputedStyle.PointerEvents == PointerEvents.None)
            return null;

        return box.Element;
    }

    /// <summary>
    /// Collect positioned descendants with z-index within this stacking context, sorted by z-index
    /// (ascending). Mirrors the rendering engine's CollectZOrderedDescendants but for hit testing.
    /// Penetrates non-stacking-context ancestors to find all positioned descendants with z-index.
    /// </summary>
    private static List<LayoutBox>? CollectZOrderedDescendantsForHitTest(LayoutBox root)
    {
        List<LayoutBox>? found = null;
        Collect(root, ref found);
        if (found == null || found.Count == 0) return null;

        // Stable sort by z-index (ascending, so higher z-index comes later in the list)
        return found.OrderBy(b => b.ComputedStyle.ZIndex).ToList();

        static void Collect(LayoutBox box, ref List<LayoutBox>? found)
        {
            foreach (var child in box.Children)
            {
                // fixed 后代由 HitTest 入口的顶层 pass 处理，不能再从这里提取（与
                // RenderEngine.CollectZOrderedDescendants 对称）。
                if (child.ComputedStyle.Position == Common.Position.Fixed) continue;

                if (IsZOrderedPositioned(child))
                {
                    (found ??= new List<LayoutBox>()).Add(child);
                    // This descendant will be tested as a whole unit; don't recurse into it
                    continue;
                }

                // Stacking contexts contain their own descendants, don't penetrate
                if (EstablishesStackingContext(child)) continue;

                // Clipping ancestors: descendants must stay in place for clipping, don't extract
                if (Clips(child)) continue;

                Collect(child, ref found);
            }
        }

        static bool IsZOrderedPositioned(LayoutBox box)
            => box.ComputedStyle.Position != Common.Position.Static && box.ComputedStyle.HasZIndex;

        static bool Clips(LayoutBox box)
            => box.ComputedStyle.OverflowX != Overflow.Visible
            || box.ComputedStyle.OverflowY != Overflow.Visible;

        static bool EstablishesStackingContext(LayoutBox box)
        {
            // Positioned elements with z-index establish a stacking context
            if (box.ComputedStyle.Position != Common.Position.Static && box.ComputedStyle.HasZIndex)
                return true;

            // Opacity < 1 establishes a stacking context
            if (box.ComputedStyle.Opacity < 1.0f)
                return true;

            // Transform establishes a stacking context
            if (box.ComputedStyle.Transform.Functions.Count > 0)
                return true;

            return false;
        }
    }

    private record struct StyleSnapshot(
        float MaxWidth, float MaxHeight,
        float Width, float Height,
        float PaddingTop, float PaddingRight, float PaddingBottom, float PaddingLeft,
        float MarginTop, float MarginRight, float MarginBottom, float MarginLeft,
        float Top, float Right, float Bottom, float Left,
        float Opacity, float FontSize, float BorderWidth,
        float BorderTopLeftRadius, float BorderTopRightRadius, float BorderBottomRightRadius, float BorderBottomLeftRadius,
        Color BackgroundColor, Color Color, Color BorderColor,
        Transform Transform);

    private record TransitionCapture(
        Dictionary<Element, (StyleSnapshot snapshot, List<Transition> transitions)> Elements,
        Dictionary<(Element parent, PseudoElementType type), (StyleSnapshot snapshot, List<Transition> transitions)> PseudoElements);

    private TransitionCapture CaptureTransitionableStyles(Element root)
    {
        var elements = new Dictionary<Element, (StyleSnapshot, List<Transition>)>();
        var pseudos = new Dictionary<(Element, PseudoElementType), (StyleSnapshot, List<Transition>)>();
        CaptureRecursive(root, elements, pseudos);
        return new TransitionCapture(elements, pseudos);
    }

    private static void CaptureRecursive(Element element,
        Dictionary<Element, (StyleSnapshot, List<Transition>)> elements,
        Dictionary<(Element, PseudoElementType), (StyleSnapshot, List<Transition>)> pseudos)
    {
        var layoutBox = element.LayoutBox;
        // 热路径用 TransitionsOrNull 判空，避免为无过渡元素触发懒分配（ISSUE-096）。
        if (layoutBox != null && layoutBox.ComputedStyle.TransitionsOrNull is { Count: > 0 } transitions)
        {
            elements[element] = (CaptureSnapshot(layoutBox.ComputedStyle), transitions);
        }

        if (layoutBox != null)
        {
            foreach (var child in layoutBox.Children)
            {
                if (child.Element is PseudoElement pseudo && child.ComputedStyle.TransitionsOrNull is { Count: > 0 } pseudoTransitions)
                {
                    pseudos[(element, pseudo.Type)] = (CaptureSnapshot(child.ComputedStyle), pseudoTransitions);
                }
            }
        }

        foreach (var child in element.Children)
            CaptureRecursive(child, elements, pseudos);
    }

    private static StyleSnapshot CaptureSnapshot(ComputedStyle cs)
    {
        return new StyleSnapshot(
            cs.MaxWidth.IsAuto ? float.MaxValue : cs.MaxWidth.Value,
            cs.MaxHeight.IsAuto ? float.MaxValue : cs.MaxHeight.Value,
            cs.Width.IsAuto ? float.NaN : cs.Width.Value,
            cs.Height.IsAuto ? float.NaN : cs.Height.Value,
            cs.PaddingTop.Value, cs.PaddingRight.Value, cs.PaddingBottom.Value, cs.PaddingLeft.Value,
            cs.MarginTop.Value, cs.MarginRight.Value, cs.MarginBottom.Value, cs.MarginLeft.Value,
            cs.Top.IsAuto ? float.NaN : cs.Top.Value,
            cs.Right.IsAuto ? float.NaN : cs.Right.Value,
            cs.Bottom.IsAuto ? float.NaN : cs.Bottom.Value,
            cs.Left.IsAuto ? float.NaN : cs.Left.Value,
            cs.Opacity, cs.FontSize.Value, cs.BorderTopWidth.Value,
            cs.BorderTopLeftRadius.Value, cs.BorderTopRightRadius.Value,
            cs.BorderBottomRightRadius.Value, cs.BorderBottomLeftRadius.Value,
            cs.BackgroundColor, cs.Color, cs.BorderTopColor,
            cs.Transform);
    }

    private bool DetectAndTriggerTransitions(Element root, TransitionCapture oldStyles)
    {
        if (oldStyles.Elements.Count == 0 && oldStyles.PseudoElements.Count == 0) return false;
        int before = _animationManager.ActiveTransitionCount;
        DetectTransitionsRecursive(root, oldStyles);
        return _animationManager.ActiveTransitionCount > before;
    }

    private void DetectTransitionsRecursive(Element element, TransitionCapture oldStyles)
    {
        if (oldStyles.Elements.TryGetValue(element, out var old) && element.LayoutBox != null)
        {
            var newSnapshot = CaptureSnapshot(element.LayoutBox.ComputedStyle);
            foreach (var transition in old.transitions)
            {
                TriggerPropertyTransitions(element, transition, old.snapshot, newSnapshot);
            }
        }

        if (element.LayoutBox != null)
        {
            foreach (var child in element.LayoutBox.Children)
            {
                if (child.Element is PseudoElement pseudo &&
                    oldStyles.PseudoElements.TryGetValue((element, pseudo.Type), out var oldPseudo))
                {
                    var newSnapshot = CaptureSnapshot(child.ComputedStyle);
                    foreach (var transition in oldPseudo.transitions)
                    {
                        TriggerPseudoElementTransitions(element, pseudo.Type, transition, oldPseudo.snapshot, newSnapshot);
                    }
                }
            }
        }

        foreach (var child in element.Children)
            DetectTransitionsRecursive(child, oldStyles);
    }

    private void TriggerPropertyTransitions(Element element, Transition transition, StyleSnapshot oldSnap, StyleSnapshot newSnap)
    {
        var prop = transition.Property;

        if (prop == "all" || prop == nameof(Style.MaxHeight))
            TryTrackFloat(element, nameof(Style.MaxHeight), oldSnap.MaxHeight, newSnap.MaxHeight, transition);
        if (prop == "all" || prop == nameof(Style.MaxWidth))
            TryTrackFloat(element, nameof(Style.MaxWidth), oldSnap.MaxWidth, newSnap.MaxWidth, transition);
        if (prop == "all" || prop == nameof(Style.Width))
            TryTrackFloat(element, nameof(Style.Width), oldSnap.Width, newSnap.Width, transition);
        if (prop == "all" || prop == nameof(Style.Height))
            TryTrackFloat(element, nameof(Style.Height), oldSnap.Height, newSnap.Height, transition);
        if (prop == "all" || prop == nameof(Style.Opacity))
            TryTrackFloat(element, nameof(Style.Opacity), oldSnap.Opacity, newSnap.Opacity, transition);
        if (prop == "all" || prop == nameof(Style.FontSize))
            TryTrackFloat(element, nameof(Style.FontSize), oldSnap.FontSize, newSnap.FontSize, transition);
        if (prop == "all" || prop == nameof(Style.BorderWidth))
            TryTrackFloat(element, nameof(Style.BorderWidth), oldSnap.BorderWidth, newSnap.BorderWidth, transition);

        if (prop == "all" || prop == nameof(Style.PaddingTop) || prop == nameof(Style.Padding))
            TryTrackFloat(element, nameof(Style.PaddingTop), oldSnap.PaddingTop, newSnap.PaddingTop, transition);
        if (prop == "all" || prop == nameof(Style.PaddingRight) || prop == nameof(Style.Padding))
            TryTrackFloat(element, nameof(Style.PaddingRight), oldSnap.PaddingRight, newSnap.PaddingRight, transition);
        if (prop == "all" || prop == nameof(Style.PaddingBottom) || prop == nameof(Style.Padding))
            TryTrackFloat(element, nameof(Style.PaddingBottom), oldSnap.PaddingBottom, newSnap.PaddingBottom, transition);
        if (prop == "all" || prop == nameof(Style.PaddingLeft) || prop == nameof(Style.Padding))
            TryTrackFloat(element, nameof(Style.PaddingLeft), oldSnap.PaddingLeft, newSnap.PaddingLeft, transition);

        if (prop == "all" || prop == nameof(Style.MarginTop) || prop == nameof(Style.Margin))
            TryTrackFloat(element, nameof(Style.MarginTop), oldSnap.MarginTop, newSnap.MarginTop, transition);
        if (prop == "all" || prop == nameof(Style.MarginRight) || prop == nameof(Style.Margin))
            TryTrackFloat(element, nameof(Style.MarginRight), oldSnap.MarginRight, newSnap.MarginRight, transition);
        if (prop == "all" || prop == nameof(Style.MarginBottom) || prop == nameof(Style.Margin))
            TryTrackFloat(element, nameof(Style.MarginBottom), oldSnap.MarginBottom, newSnap.MarginBottom, transition);
        if (prop == "all" || prop == nameof(Style.MarginLeft) || prop == nameof(Style.Margin))
            TryTrackFloat(element, nameof(Style.MarginLeft), oldSnap.MarginLeft, newSnap.MarginLeft, transition);

        // Inset properties (used e.g. to slide an absolutely-positioned drawer on/off-screen).
        if (prop == "all" || prop == nameof(Style.Top))
            TryTrackFloat(element, nameof(Style.Top), oldSnap.Top, newSnap.Top, transition);
        if (prop == "all" || prop == nameof(Style.Right))
            TryTrackFloat(element, nameof(Style.Right), oldSnap.Right, newSnap.Right, transition);
        if (prop == "all" || prop == nameof(Style.Bottom))
            TryTrackFloat(element, nameof(Style.Bottom), oldSnap.Bottom, newSnap.Bottom, transition);
        if (prop == "all" || prop == nameof(Style.Left))
            TryTrackFloat(element, nameof(Style.Left), oldSnap.Left, newSnap.Left, transition);

        if (prop == "all" || prop == nameof(Style.BorderTopLeftRadius))
            TryTrackFloat(element, nameof(Style.BorderTopLeftRadius), oldSnap.BorderTopLeftRadius, newSnap.BorderTopLeftRadius, transition);
        if (prop == "all" || prop == nameof(Style.BorderTopRightRadius))
            TryTrackFloat(element, nameof(Style.BorderTopRightRadius), oldSnap.BorderTopRightRadius, newSnap.BorderTopRightRadius, transition);
        if (prop == "all" || prop == nameof(Style.BorderBottomRightRadius))
            TryTrackFloat(element, nameof(Style.BorderBottomRightRadius), oldSnap.BorderBottomRightRadius, newSnap.BorderBottomRightRadius, transition);
        if (prop == "all" || prop == nameof(Style.BorderBottomLeftRadius))
            TryTrackFloat(element, nameof(Style.BorderBottomLeftRadius), oldSnap.BorderBottomLeftRadius, newSnap.BorderBottomLeftRadius, transition);

        if (prop == "all" || prop == nameof(Style.BackgroundColor))
            TryTrackColor(element, nameof(Style.BackgroundColor), oldSnap.BackgroundColor, newSnap.BackgroundColor, transition);
        if (prop == "all" || prop == nameof(Style.Color))
            TryTrackColor(element, nameof(Style.Color), oldSnap.Color, newSnap.Color, transition);
        if (prop == "all" || prop == nameof(Style.BorderColor))
            TryTrackColor(element, nameof(Style.BorderColor), oldSnap.BorderColor, newSnap.BorderColor, transition);

        if (prop == "all" || prop == nameof(Style.Transform))
            TryTrackTransform(element, oldSnap.Transform, newSnap.Transform, transition);
    }

    private void TriggerPseudoElementTransitions(Element parent, PseudoElementType pseudoType, Transition transition, StyleSnapshot oldSnap, StyleSnapshot newSnap)
    {
        var prop = transition.Property;

        if (prop == "all" || prop == nameof(Style.Transform))
        {
            if (!TransformEquals(oldSnap.Transform, newSnap.Transform))
            {
                string key = $"::pseudo({pseudoType}).Transform";
                if (!_animationManager.HasActiveTransition(parent, key))
                {
                    _animationManager.TrackTransformChangeWithApplier(
                        parent, key, oldSnap.Transform, newSnap.Transform, transition,
                        (e, t) =>
                        {
                            e.PseudoElementStyles ??= new();
                            if (!e.PseudoElementStyles.TryGetValue(pseudoType, out var s))
                            {
                                s = new Style();
                                e.PseudoElementStyles[pseudoType] = s;
                            }
                            s.Transform = t;
                        });
                }
            }
        }

        if (prop == "all" || prop == nameof(Style.Opacity))
        {
            TryTrackPseudoFloat(parent, pseudoType, nameof(Style.Opacity), oldSnap.Opacity, newSnap.Opacity, transition,
                (s, v) => s.Opacity = v);
        }

        if (prop == "all" || prop == nameof(Style.BackgroundColor))
        {
            TryTrackPseudoColor(parent, pseudoType, nameof(Style.BackgroundColor), oldSnap.BackgroundColor, newSnap.BackgroundColor, transition,
                (s, c) => s.BackgroundColor = c);
        }

        if (prop == "all" || prop == nameof(Style.Color))
        {
            TryTrackPseudoColor(parent, pseudoType, nameof(Style.Color), oldSnap.Color, newSnap.Color, transition,
                (s, c) => s.Color = c);
        }
    }

    private void TryTrackPseudoFloat(Element parent, PseudoElementType pseudoType, string property, float oldValue, float newValue, Transition transition, Action<Style, float> setter)
    {
        if (float.IsNaN(oldValue) || float.IsNaN(newValue)) return;
        if (MathF.Abs(oldValue - newValue) < 1e-6f) return;

        string key = $"::pseudo({pseudoType}).{property}";
        if (_animationManager.HasActiveTransition(parent, key)) return;

        _animationManager.TrackPropertyChangeWithApplier(parent, key, oldValue, newValue, transition,
            (e, v) =>
            {
                e.PseudoElementStyles ??= new();
                if (!e.PseudoElementStyles.TryGetValue(pseudoType, out var s))
                {
                    s = new Style();
                    e.PseudoElementStyles[pseudoType] = s;
                }
                setter(s, v);
            });
    }

    private void TryTrackPseudoColor(Element parent, PseudoElementType pseudoType, string property, Color oldColor, Color newColor, Transition transition, Action<Style, Color> setter)
    {
        if (oldColor.R == newColor.R && oldColor.G == newColor.G &&
            oldColor.B == newColor.B && oldColor.A == newColor.A) return;

        string key = $"::pseudo({pseudoType}).{property}";
        if (_animationManager.HasActiveTransition(parent, key)) return;

        _animationManager.TrackColorChangeWithApplier(parent, key, oldColor, newColor, transition,
            (e, c) =>
            {
                e.PseudoElementStyles ??= new();
                if (!e.PseudoElementStyles.TryGetValue(pseudoType, out var s))
                {
                    s = new Style();
                    e.PseudoElementStyles[pseudoType] = s;
                }
                setter(s, c);
            });
    }

    private void TryTrackTransform(Element element, Transform oldTransform, Transform newTransform, Transition transition)
    {
        if (_animationManager.HasActiveTransition(element, nameof(Style.Transform))) return;
        _animationManager.TrackTransformChange(element, oldTransform, newTransform, transition);
    }

    private void TryTrackColor(Element element, string property, Color oldColor, Color newColor, Transition transition)
    {
        if (_animationManager.HasActiveTransition(element, property)) return;
        _animationManager.TrackColorChange(element, property, oldColor, newColor, transition);
    }

    private void TryTrackFloat(Element element, string property, float oldValue, float newValue, Transition transition)
    {
        if (float.IsNaN(oldValue) || float.IsNaN(newValue)) return;
        if (oldValue == float.MaxValue && newValue == float.MaxValue) return;
        if (_animationManager.HasActiveTransition(element, property)) return;
        _animationManager.TrackPropertyChange(element, property, oldValue, newValue, transition);
    }

    private static bool TransformEquals(Transform a, Transform b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.Functions.Count != b.Functions.Count) return false;
        for (int i = 0; i < a.Functions.Count; i++)
        {
            if (!a.Functions[i].Equals(b.Functions[i])) return false;
        }
        return true;
    }

    private static void EnsureParentReferences(Element element)
    {
        foreach (var child in element.Children)
        {
            child.SetParent(element);
            EnsureParentReferences(child);
        }
    }

    // ---------------------------------------------------------------------
    // 视频会话生命周期
    // ---------------------------------------------------------------------

    /// <summary>
    /// 同步视频会话与当前 DOM 树：为新出现的 <see cref="VideoElement"/> 创建会话并接管，
    /// 为已消失的元素回收会话。在每次布局后调用，使会话随元素增删而增删。
    /// </summary>
    private void SyncVideoSessions(Element root)
    {
        if (VideoBackend == null) return;

        // 收集当前树中所有 VideoElement
        var present = new HashSet<VideoElement>();
        CollectVideoElements(root, present);
        // 页面转场期间 leaving 树仍在绘制，其视频会话需保持存活（ISSUE-108）。
        if (_navLeavingRoot != null)
            CollectVideoElements(_navLeavingRoot, present);

        // 1. 回收已不在树中的会话
        if (_videoSessions.Count > 0)
        {
            var removed = new List<VideoElement>();
            foreach (var (element, session) in _videoSessions)
            {
                if (!present.Contains(element))
                {
                    session.Dispose();
                    element.Session = null;
                    removed.Add(element);
                }
            }
            foreach (var element in removed)
                _videoSessions.Remove(element);
        }

        // 2. 为新元素创建会话
        foreach (var video in present)
        {
            EnsurePosterDecoded(video);

            if (video.Session != null) continue;
            if (video.Source.IsEmpty) continue;

            CreateVideoSession(video);
        }
    }

    /// <summary>
    /// 惰性解码本地 poster 文件（首帧前占位）。仅处理可读的本地文件路径；
    /// 远程 URL 的获取交由应用层设置 <see cref="VideoElement.PosterBitmap"/>。失败时静默忽略。
    /// </summary>
    private static void EnsurePosterDecoded(VideoElement video)
    {
        if (video.PosterBitmap != null) return;
        if (string.IsNullOrEmpty(video.Poster)) return;
        if (!File.Exists(video.Poster)) return;

        try
        {
            using var stream = File.OpenRead(video.Poster);
            video.PosterBitmap = SKBitmap.Decode(stream);
        }
        catch
        {
            // poster 解码失败不应影响播放，回退到背景色。
        }
    }

    private void CreateVideoSession(VideoElement video)
    {
        try
        {
            var session = VideoBackend!.CreateSession(
                new VideoSourceDescriptor(video.Source.ToUri()),
                new VideoSessionOptions(
                    AutoPlay: video.AutoPlay,
                    Muted: video.Muted,
                    Loop: video.Loop));

            video.Session = session;
            _videoSessions[video] = session;

            // 事件可能在解码线程触发，统一通过 PostInvalidate 把重绘转交主循环。
            session.Event += evt => OnVideoSessionEvent(video, evt);

            _logger.LogDebug("Video session created for <video src=\"{Src}\">", video.Source.Raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create video session for <video src=\"{Src}\">", video.Source.Raw);
        }
    }

    private void OnVideoSessionEvent(VideoElement video, VideoSessionEvent evt)
    {
        switch (evt)
        {
            case VideoSessionEvent.Loaded loaded:
                // 写入内禀尺寸并触发重排（auto 尺寸的 video 将按真实纵横比布局）。
                video.IntrinsicWidth = loaded.Width;
                video.IntrinsicHeight = loaded.Height;
                // 内禀尺寸是布局输入：递增版本号使下一帧重排（区别于新帧到达的纯绘制失效）。
                Element.BumpMutationVersion();
                PostInvalidate(video);
                break;

            case VideoSessionEvent.FrameAvailable:
                // 新帧到达：标脏该元素，下一帧合成最新帧。
                PostInvalidate(video);
                break;

            case VideoSessionEvent.Ended:
                PostInvalidate(video);
                break;

            case VideoSessionEvent.Error error:
                _logger.LogError(error.Cause, "Video error on <video src=\"{Src}\">: {Message}",
                    video.Source.Raw, error.Message);
                break;
        }
    }

    private static void CollectVideoElements(Element element, HashSet<VideoElement> sink)
    {
        if (element is VideoElement video)
            sink.Add(video);
        foreach (var child in element.Children)
            CollectVideoElements(child, sink);
    }

    /// <summary>释放所有视频会话。平台宿主关闭时调用。</summary>
    public void DisposeVideoSessions()
    {
        foreach (var session in _videoSessions.Values)
            session.Dispose();
        _videoSessions.Clear();
    }

    // ---------------------------------------------------------------------
    // 图片资源加载生命周期
    // ---------------------------------------------------------------------

    /// <summary>
    /// 同步图片源与当前 DOM 树：为带 <c>src</c>、尚未解码且未在加载中的 <see cref="ImageElement"/>
    /// 发起异步加载，并惰性解码占位图。镜像 <see cref="SyncVideoSessions"/>，在每次布局后调用。
    /// </summary>
    private void SyncImageSources(Element root)
    {
        if (ImageLoader == null) return;

        var present = new HashSet<ImageElement>();
        CollectImageElements(root, present);

        foreach (var img in present)
        {
            EnsurePlaceholderDecoded(img);

            if (img.Bitmap != null) continue;
            if (img.Source.IsEmpty) continue;
            if (_imageLoads.ContainsKey(img)) continue;

            BeginImageLoad(img);
        }

        // 回收已不在树中的元素的加载记录，避免字典无界增长。
        if (_imageLoads.Count > 0)
        {
            var stale = new List<ImageElement>();
            foreach (var img in _imageLoads.Keys)
                if (!present.Contains(img)) stale.Add(img);
            foreach (var img in stale)
                _imageLoads.Remove(img);
        }
        if (_placeholderLoads.Count > 0)
            _placeholderLoads.RemoveWhere(img => !present.Contains(img));
    }

    /// <summary>
    /// 惰性解码占位图（首张真实图前显示）。通过统一资源管理器异步加载，支持本地文件与 res://。
    /// 失败时静默忽略，回退到背景色。
    /// </summary>
    private void EnsurePlaceholderDecoded(ImageElement img)
    {
        if (img.PlaceholderBitmap != null) return;
        if (string.IsNullOrEmpty(img.Placeholder)) return;
        if (ImageLoader == null) return;

        // 标记已请求：用 _imageLoads 之外的小技巧——占位图加载也可能在途，避免重复请求。
        // 这里用 PlaceholderBitmap 的赋值作为完成标记，进行中状态由下方任务捕获保证幂等。
        if (_placeholderLoads.Contains(img)) return;
        _placeholderLoads.Add(img);

        MediaSource placeholder = img.Placeholder; // 隐式解析协议
        ImageLoader.LoadAsync(placeholder).ContinueWith(t =>
        {
            if (t.Status == TaskStatus.RanToCompletion && t.Result != null)
            {
                img.PlaceholderBitmap = t.Result;
                PostInvalidate(img);
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// 发起一次图片异步加载。加载在后台线程进行，完成后写入位图与内禀尺寸，
    /// 再通过 <see cref="PostInvalidate"/> 把重排/重绘转交主循环。镜像视频 <c>Loaded</c> 事件。
    /// </summary>
    private void BeginImageLoad(ImageElement img)
    {
        // ExecuteSynchronously：加载已完成时内联执行（仅做字段赋值，开销极小），
        // 否则在后台线程完成。无论哪种情形都通过 PostInvalidate 把失效安全转交主循环。
        var task = ImageLoader!.LoadAsync(img.Source).ContinueWith(t =>
        {
            var bmp = t.Status == TaskStatus.RanToCompletion ? t.Result : null;
            if (bmp != null)
            {
                img.Bitmap = bmp;
                img.IntrinsicWidth = bmp.Width;
                img.IntrinsicHeight = bmp.Height;
                // 内禀尺寸是布局输入（auto 尺寸的 img 按真实尺寸布局）：递增版本号触发重排。
                Element.BumpMutationVersion();
            }
            // 即使失败也投递失效：让占位图/背景在下一帧稳定呈现。
            PostInvalidate(img);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        _imageLoads[img] = task;
    }

    private static void CollectImageElements(Element element, HashSet<ImageElement> sink)
    {
        if (element is ImageElement img)
            sink.Add(img);
        foreach (var child in element.Children)
            CollectImageElements(child, sink);
    }

    /// <summary>
    /// 扫描整树，启动尚未运行的关键帧动画，并刷新已在运行者的定义。
    ///
    /// <para>调用于布局<b>之后</b>，与 <see cref="ReconcileAnimationTargets"/> 配对——后者在布局
    /// 之前完成迁移、回收与补写。这里能安全地按引用比对，正是因为迁移已经发生过了（ISSUE-127）。</para>
    ///
    /// <para>本帧新启动的动画其首帧值由下一次 <c>Update</c> 写入，与旧行为一致。</para>
    /// </summary>
    private void ScanAndStartAnimations(Element root)
    {
        foreach (var (element, names) in CollectDeclaredAnimations(root))
        {
            if (names.Count == 0) continue;

            var animations = element.Style?.Animations.RefValueOrNull();
            if (animations == null) continue;

            foreach (var animation in animations)
            {
                _logger.LogDebug("ScanAndStartAnimations: found animation \"{Name}\" on <{Tag} id=\"{Id}\">",
                    animation.Name, element.TagName, element.Id ?? "");
                // 已在运行者不重启，只刷新定义；未运行者从 0 起播。
                _animationManager.StartAnimationIfNotRunning(element, animation);
            }
        }
    }

    /// <summary>
    /// 在布局<b>之前</b>把动画/过渡条目对齐到重渲染后的在场元素实例、回收不该再播放的条目，
    /// 并把当前进度值补写回行内样式（ISSUE-127）。
    ///
    /// <para>三件事都必须早于布局与过渡检测：</para>
    /// <list type="bullet">
    /// <item>迁移早于 <see cref="DetectAndTriggerTransitions"/>——过渡按元素引用去重，条目还挂在
    ///   旧实例上时，替换元素上检出的同一属性过渡会另起一条，同属性并存两条相互覆写。</item>
    /// <item>回收早于布局——停掉的动画要把自己写进行内样式的值一并抹掉，好让元素自己声明的值
    ///   重新生效。晚于布局则本帧仍按动画最后一帧的值排版，元素停在半路。</item>
    /// <item>补写早于布局——否则替换后的首帧按刚声明的原始样式布局，画出动画起始态或直接跳到
    ///   过渡终点，每次组件回调都闪一下。</item>
    /// </list>
    ///
    /// <para>回收发生在<b>本帧的启动之前</b>（启动在布局后的 <see cref="ScanAndStartAnimations"/>），
    /// 因此判据用的是<b>当前 DOM 的声明</b>而非上一帧的：新增元素本就还没有条目，不会被误伤。</para>
    /// </summary>
    private void ReconcileAnimationTargets(Element root)
    {
        _animationManager.MigrateSupersededTargets();
        _animationManager.PruneDetachedTargets(CollectDeclaredAnimations(root));
        _animationManager.ReapplyCurrentValues();
    }

    /// <summary>
    /// 收集树中每个元素及其 <c>Style.Animations</c> 声明的动画名。<c>Element</c> 未重写
    /// <c>Equals</c>/<c>GetHashCode</c>，字典天然按引用做键——正是这里要的身份语义。
    /// </summary>
    private static Dictionary<Element, HashSet<string>> CollectDeclaredAnimations(Element root)
    {
        var sink = new Dictionary<Element, HashSet<string>>();
        CollectDeclaredAnimations(root, sink);
        return sink;
    }

    private static void CollectDeclaredAnimations(Element element, Dictionary<Element, HashSet<string>> sink)
    {
        // 绝大多数元素没有动画：共用一个空集合，避免每帧为每个元素各分配一个 HashSet
        // （整树扫描每帧都跑，包括 ISSUE-096 的快速路径）。
        var animations = element.Style?.Animations.RefValueOrNull();
        if (animations == null || animations.Count == 0)
        {
            sink[element] = s_noDeclaredAnimations;
        }
        else
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var animation in animations)
                names.Add(animation.Name);
            sink[element] = names;
        }

        foreach (var child in element.Children)
            CollectDeclaredAnimations(child, sink);
    }

    /// <summary>无动画声明的元素共用的空集合。只读使用，绝不写入。</summary>
    private static readonly HashSet<string> s_noDeclaredAnimations = new(StringComparer.Ordinal);

    /// <summary>
    /// 将旧布局树的滚动状态恢复到新布局树。
    ///
    /// <para>滚动偏移属于「被滚动的内容」，而非「容器所在的槽位」。因此本方法在旧树与新树间
    /// 按位置逐层对齐地行走，仅当某个可滚动容器的<b>整棵子树结构一致</b>时才恢复其滚动偏移
    /// （见 ISSUE-092 问题2）：</para>
    /// <list type="bullet">
    /// <item>侧栏重新渲染（菜单项结构不变）→ 子树结构一致 → 恢复，菜单停留原位。</item>
    /// <item>路由切换使 <c>.main-content</c> 内容从 button 页换成 accordion 页 → 子树结构不同
    ///   → 不恢复，新内容从顶部开始（否则短内容会被旧的滚动偏移顶出可视区）。</item>
    /// </list>
    /// <para>结构签名只比较标签名与嵌套形状，忽略文本与属性等叶子值，因此仅有文本变化的
    /// 重新渲染仍会被视为「同一内容」而正确恢复。结构比较基于 <b>DOM 子树</b>（而非布局子树），
    /// 使 <c>display:none</c> 的展开/折叠（如 IonAccordion 面板）不被误判为内容替换而重置滚动。</para>
    /// <para>判定的<b>宽严</b>取决于本次重建是不是跨页面导航（ISSUE-120）：同页重建放宽到子序列
    /// 等价以容纳「追加」（无限滚动），跨页面导航则要求严格等价——那里结构相似纯属巧合，
    /// 路由路径才是权威的身份信号。</para>
    /// </summary>
    /// <summary>
    /// 导航离开当前页时，为来源路径拍下滚动快照（ISSUE-118）。必须在旧布局树被替换之前调用。
    /// <para><see cref="NavigationDirection.Root"/> 会清空历史栈（见 <see cref="NavigationManager"/>），
    /// 栈上所有页面都不再可返回，故连同来源页一起丢弃全部快照。</para>
    /// </summary>
    private void CaptureScrollSnapshot(NavigationTransitionInfo? navigation, LayoutBox? oldLayout)
    {
        if (navigation == null) return;

        if (navigation.Direction == NavigationDirection.Root)
        {
            _scrollSnapshots.Clear();
            return;
        }

        _scrollSnapshots.Capture(navigation.FromPath, oldLayout);
    }

    /// <summary>
    /// 返回（出栈）到某页时，把该页离开时的滚动快照回放到新布局树上（ISSUE-118）。
    /// <para>只在 <see cref="NavigationDirection.Back"/> 回放：<see cref="NavigationDirection.Forward"/>
    /// 压栈进入的是一次新的页面访问，按浏览器语义从顶部开始。</para>
    /// </summary>
    private void RestoreScrollSnapshot(NavigationTransitionInfo? navigation, LayoutBox? newLayout)
    {
        if (navigation is not { Direction: NavigationDirection.Back }) return;

        int restored = _scrollSnapshots.Apply(navigation.ToPath, newLayout);
        if (restored > 0)
        {
            _logger.LogDebug("Restored scroll snapshot for {Path}: {Count} scrollable box(es)",
                navigation.ToPath, restored);
        }
    }

    /// <summary>
    /// 收尾一次返回导航：消费掉已回放的快照（该历史条目已出栈）。在同一次
    /// <see cref="Initialize"/> 内的多次回放（transition 触发重新布局）之后调用一次。
    /// </summary>
    private void ConsumeScrollSnapshot(NavigationTransitionInfo? navigation)
    {
        if (navigation is not { Direction: NavigationDirection.Back }) return;
        _scrollSnapshots.Forget(navigation.ToPath);
    }

    /// <summary>
    /// 本次重建是否为<b>跨页面</b>的路由导航（ISSUE-120）。
    /// <para>「同一路径重建」（<see cref="ComponentBase.StateHasChanged"/>、热重载、无限滚动加载更多）
    /// 与「切换到另一个页面」在<b>结构上</b>可能无法区分——文档站的各个页面都以
    /// <c>h1 → p → h2 → …</c> 这类通用标签开头，旧页的子树往往能在新页里按序找到对应项。
    /// 但两者在<b>语义上</b>截然不同，而路由信息恰好提供了结构判断给不出的权威身份信号：
    /// 目标路径与来源路径不同，就意味着被路由的槽位里装的是另一批内容。</para>
    /// </summary>
    private static bool IsCrossPageNavigation(NavigationTransitionInfo? navigation)
        => navigation != null && !string.Equals(navigation.FromPath, navigation.ToPath, StringComparison.Ordinal);

    private static void RestoreScrollState(LayoutBox? oldRoot, LayoutBox? newRoot, bool crossPageNavigation = false)
    {
        if (oldRoot == null || newRoot == null) return;
        // 布局树被缓存复用时新旧为同一对象，滚动偏移本就保留在盒子上，无需恢复（ISSUE-096）。
        if (ReferenceEquals(oldRoot, newRoot)) return;
        // 根节点必须同标签才对齐（否则整棵树语义不同，无从恢复）。
        if (!IsSameElementIdentity(oldRoot.Element, newRoot.Element)) return;
        RestoreScrollStateRecursive(oldRoot, newRoot, crossPageNavigation);
    }

    private static void RestoreScrollStateRecursive(LayoutBox oldBox, LayoutBox newBox, bool crossPageNavigation)
    {
        // 仅当该容器有非零滚动偏移时才需要恢复；此时再验证其承载的内容（整棵子树）结构一致
        // ——结构一致，旧偏移才仍然有效。结构不同意味着「同一槽位、不同内容」（如路由切换后的
        // .main-content），旧偏移不再对应任何内容，恢复它会把新内容顶出可视区。
        // 将结构比较（O(子树大小)）延迟到确有偏移需要恢复时，避免对整棵树逐节点做深度比较。
        //
        // 注意：这里比较的是 DOM 子树（Element）而非布局子树（LayoutBox）。display:none 的元素
        // 会被布局树过滤掉，因此若按布局树比较，展开/折叠 IonAccordion 面板（切换其内容盒的
        // display）会被误判为「内容替换」，从而重置外层可滚动容器的滚动条（见 ion-accordion 问题1）。
        // DOM 子树在 display 切换下保持稳定（内容元素始终在树中，仅计算 display 变化），既能在
        // 折叠/展开时正确恢复滚动，又能在真正的路由内容替换（DOM 子树形状不同）时正确重置。
        // 跨页面导航时改用<b>严格</b>结构等价（子节点数量逐层相同），而不是同页重建时的
        // 子序列等价：子序列判定是为了容纳「追加」（无限滚动加载更多），那只在同一页内才成立。
        // 跨页面时槽位里装的是另一批内容，而通用标签让不同页面也能通过子序列判定
        // （文档站各页都以 h1 → p → h2 → … 开头），于是上一页的偏移被继承到新页面上——
        // 正是 ISSUE-120 的现场。侧栏这类跨导航<b>原样在场</b>的容器仍满足严格等价，偏移照常保留。
        if ((oldBox.ScrollTop != 0f || oldBox.ScrollLeft != 0f) &&
            (crossPageNavigation
                ? IsIdenticalPresentedContent(oldBox.Element, newBox.Element)
                : IsSamePresentedContent(oldBox.Element, newBox.Element)))
        {
            // 新内容可能比旧的短（列表被裁剪），按新的可滚动范围夹取，避免越界。
            newBox.ScrollTop = ClampScrollOffset(
                oldBox.ScrollTop, newBox.ScrollableContentHeight, newBox.BoxModel.PaddingBox.Height);
            newBox.ScrollLeft = ClampScrollOffset(
                oldBox.ScrollLeft, newBox.ScrollableContentWidth, newBox.BoxModel.PaddingBox.Width);
        }

        // 按位置逐一对齐子节点并向下递归；仅在标签相同（同一元素身份）时才配对，
        // 避免把兄弟节点串位（如 .sidebar 与 .main-content 都是 <div>）。
        int count = Math.Min(oldBox.Children.Count, newBox.Children.Count);
        for (int i = 0; i < count; i++)
        {
            var oldChild = oldBox.Children[i];
            var newChild = newBox.Children[i];
            if (IsSameElementIdentity(oldChild.Element, newChild.Element))
            {
                RestoreScrollStateRecursive(oldChild, newChild, crossPageNavigation);
            }
        }
    }

    /// <summary>
    /// 严格结构等价：标签相同、子节点数量相同，且逐个子树递归严格等价（ISSUE-120）。
    /// <para>只比较标签名与嵌套形状，忽略文本与属性，因此侧栏这类「跨导航原样重建」的容器
    /// 仍判定为同一批内容而保留滚动偏移；而被路由替换掉的页面内容（块数、嵌套形状都不同）
    /// 判定不成立，新页面从顶部开始。</para>
    /// <para>与 <see cref="IsSamePresentedContent"/> 的分工：后者放宽到子序列以容纳同页内的
    /// 「追加」，前者用于跨页面导航——那里「内容变没变」不能靠结构猜，宁可严格。</para>
    /// </summary>
    private static bool IsIdenticalPresentedContent(Element oldElement, Element newElement)
    {
        if (!IsSameElementIdentity(oldElement, newElement)) return false;
        if (oldElement.Children.Count != newElement.Children.Count) return false;

        for (int i = 0; i < oldElement.Children.Count; i++)
        {
            if (!IsIdenticalPresentedContent(oldElement.Children[i], newElement.Children[i])) return false;
        }
        return true;
    }

    /// <summary>
    /// 判断可滚动容器<b>承载的内容是否仍是同一批</b>——旧偏移是否依然指向同一处内容。
    /// 只比较标签名与树形，忽略文本、属性、样式等叶子值，因此仅内容文本变化的重新渲染仍算
    /// 同一批（滚动应保留），而整页替换不算（滚动应重置）。
    /// <para>刻意比较 <b>DOM 树</b>而非布局树：<c>display:none</c> 的元素会从布局树中被过滤，
    /// 故折叠一个 IonAccordion 面板会改变外层可滚动容器的布局子树形状，若按布局树比较将被
    /// 误判为「内容替换」而重置滚动条。DOM 树在 display 切换下保持不变（内容元素始终存在），
    /// 因此展开/折叠面板时能正确保留滚动，同时真正的路由内容替换仍被正确识别。</para>
    /// <para>早期实现要求<b>严格</b>结构等价（子节点数量逐层相同），这会把「追加内容」误判为
    /// 「内容被替换」：无限滚动加载出新数据后行数变了，滚动条因而被重置回顶部
    /// （ion-infinite-scroll 问题 2）。追加的语义是「原有内容仍在原位，后面多了一截」，旧偏移
    /// 依然有效，必须保留。</para>
    /// <para>因此这里放宽为<b>子序列等价</b>：标签相同，且较短一侧的每个子树都能在较长一侧
    /// 里<b>按原有顺序</b>找到对应项。判定<b>逐层递归</b>——增删可能发生在任意深度：真实的
    /// Ionic 结构是 <c>.inner-scroll &gt; ion-list &gt; ion-item*</c>，滚动容器自己的子节点
    /// 始终是「列表 + ion-infinite-scroll 哨兵」两个，行数变化发生在<b>下一层</b>的
    /// <c>ion-list</c> 里。若只在容器这一层放宽、内部退回严格等价，就会在 <c>ion-list</c>
    /// 的子节点数上判负——正是 ion-infinite-scroll 问题 2 的现场。</para>
    /// <para>整页替换时旧内容在新树里找不到按序对应项，判定不成立，「切换内容要回到顶部」的
    /// 既有语义得以保留。</para>
    /// </summary>
    private static bool IsSamePresentedContent(Element oldElement, Element newElement)
    {
        if (!IsSameElementIdentity(oldElement, newElement)) return false;

        // 较长的一侧是"全集"，较短的一侧必须是它的有序子序列。
        var (subset, superset) = oldElement.Children.Count <= newElement.Children.Count
            ? (oldElement.Children, newElement.Children)
            : (newElement.Children, oldElement.Children);

        int j = 0;
        for (int i = 0; i < subset.Count; i++)
        {
            // 在剩余的 superset 中向后寻找与 subset[i] 对应的项；递归同样按子序列判定，
            // 使「更深层」的增删（ion-list 里加行）同样被认作同一批内容。
            while (j < superset.Count && !IsSamePresentedContent(subset[i], superset[j])) j++;
            if (j == superset.Count) return false;
            j++;
        }
        return true;
    }

    /// <summary>
    /// 与 <see cref="ScrollBy"/> 同款的夹取：可滚动内容尺寸与 padding box 视口尺寸之差即为上限。
    /// </summary>
    private static float ClampScrollOffset(float saved, float scrollableContentSize, float viewportSize)
    {
        if (saved <= 0f) return 0f;
        float max = Math.Max(0f, scrollableContentSize - viewportSize);
        return Math.Clamp(saved, 0f, max);
    }

    /// <summary>
    /// 处理滚动事件，更新滚动位置
    /// </summary>
    public bool ScrollBy(float x, float y, float deltaX, float deltaY)
    {
        if (_currentLayout == null)
        {
            _logger.LogTrace("ScrollBy: no layout available");
            return false;
        }

        var targetElement = HitTest(x, y);
        if (targetElement == null)
        {
            _logger.LogTrace("ScrollBy: no element at ({X}, {Y})", x, y);
            return false;
        }

        _logger.LogTrace("ScrollBy: hit element <{Tag} id=\"{Id}\" class=\"{Class}\"> at ({X}, {Y}), delta=({DeltaX}, {DeltaY})",
            targetElement.TagName, targetElement.Id, targetElement.Class, x, y, deltaX, deltaY);

        var scrollableBox = FindScrollableBox(targetElement, deltaX, deltaY);
        if (scrollableBox == null)
        {
            _logger.LogTrace("ScrollBy: no scrollable ancestor found for <{Tag} id=\"{Id}\" class=\"{Class}\">",
                targetElement.TagName, targetElement.Id, targetElement.Class);
            return false;
        }

        _logger.LogTrace("ScrollBy: found scrollable <{Tag} id=\"{Id}\" class=\"{Class}\">, overflowY={OverflowY}, scrollableHeight={ScrollableH}, paddingBoxHeight={PaddingH}",
            scrollableBox.Element.TagName, scrollableBox.Element.Id, scrollableBox.Element.Class,
            scrollableBox.ComputedStyle.OverflowY, scrollableBox.ScrollableContentHeight, scrollableBox.BoxModel.PaddingBox.Height);

        float oldScrollLeft = scrollableBox.ScrollLeft;
        float oldScrollTop = scrollableBox.ScrollTop;

        // 更新垂直滚动
        if (Math.Abs(deltaY) > 0.01f && scrollableBox.HasVerticalScrollbar)
        {
            float maxScrollTop = scrollableBox.ScrollableContentHeight - scrollableBox.BoxModel.PaddingBox.Height
                + (scrollableBox.HasHorizontalScrollbar ? LayoutBox.ScrollbarThickness : 0);
            maxScrollTop = Math.Max(0, maxScrollTop);
            scrollableBox.ScrollTop = Math.Clamp(scrollableBox.ScrollTop + deltaY, 0, maxScrollTop);
            _logger.LogTrace("ScrollBy: vertical scroll {Old} -> {New} (max={Max})", oldScrollTop, scrollableBox.ScrollTop, maxScrollTop);
        }

        // 更新水平滚动
        if (Math.Abs(deltaX) > 0.01f && scrollableBox.HasHorizontalScrollbar)
        {
            float maxScrollLeft = scrollableBox.ScrollableContentWidth - scrollableBox.BoxModel.PaddingBox.Width
                + (scrollableBox.HasVerticalScrollbar ? LayoutBox.ScrollbarThickness : 0);
            maxScrollLeft = Math.Max(0, maxScrollLeft);
            scrollableBox.ScrollLeft = Math.Clamp(scrollableBox.ScrollLeft + deltaX, 0, maxScrollLeft);
            _logger.LogTrace("ScrollBy: horizontal scroll {Old} -> {New} (max={Max})", oldScrollLeft, scrollableBox.ScrollLeft, maxScrollLeft);
        }

        bool scrolled = Math.Abs(scrollableBox.ScrollLeft - oldScrollLeft) > 0.01f ||
                        Math.Abs(scrollableBox.ScrollTop - oldScrollTop) > 0.01f;

        if (scrolled)
        {
            // 分发滚动事件
            var scrollArgs = new ScrollEventArgs
            {
                Target = scrollableBox.Element,
                DeltaX = scrollableBox.ScrollLeft - oldScrollLeft,
                DeltaY = scrollableBox.ScrollTop - oldScrollTop,
                ScrollLeft = scrollableBox.ScrollLeft,
                ScrollTop = scrollableBox.ScrollTop,
                // 滚动几何量，供监听者按 DOM 语义判断位置（如 ion-infinite-scroll 的阈值计算）。
                ScrollWidth = scrollableBox.ScrollableContentWidth,
                ScrollHeight = scrollableBox.ScrollableContentHeight,
                ClientWidth = scrollableBox.BoxModel.PaddingBox.Width,
                ClientHeight = scrollableBox.BoxModel.PaddingBox.Height,
                Bubbles = true
            };

            // Scroll events may have async handlers; wrap with SynchronizationContext.
            // (The dispatcher is already drained at the start of this frame, so any
            // continuations will run next frame.)
            var prevContext = SynchronizationContext.Current;
            var syncContext = new Platform.MikoSynchronizationContext(_dispatcher);
            SynchronizationContext.SetSynchronizationContext(syncContext);
            try
            {
                _eventDispatcher.Dispatch(scrollableBox.Element, EventTypes.Scroll, scrollArgs);

                // 目标+冒泡只覆盖祖先链，但关心滚动的组件通常位于滚动容器*内部*
                // （ion-infinite-scroll 就是 ion-content .inner-scroll 的后代）。DOM 里这类
                // 组件会直接在滚动元素上加监听器；Miko 的组件拿不到祖先引用，因此这里额外
                // 向下通知一次。嵌套的独立滚动容器整棵剪掉：外层滚动不应触发内层的监听器。
                _eventDispatcher.DispatchToDescendants(
                    scrollableBox.Element, EventTypes.Scroll, scrollArgs, IsNestedScrollContainer);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevContext);
            }

            InvalidateElement(scrollableBox.Element);
            _logger.LogTrace("ScrollBy: scrolled, new position=({ScrollLeft}, {ScrollTop})", scrollableBox.ScrollLeft, scrollableBox.ScrollTop);
        }
        else
        {
            _logger.LogTrace("ScrollBy: no actual scroll change (already at boundary)");
        }

        return scrolled;
    }

    /// <summary>
    /// 元素自身是否是一个独立的滚动容器（任一轴为 auto/scroll）。
    /// 向下派发滚动事件时用来剪枝：内层滚动容器有自己的滚动位置，外层的滚动量对它
    /// 的后代没有意义。
    /// </summary>
    private static bool IsNestedScrollContainer(Element element)
    {
        var style = element.LayoutBox?.ComputedStyle;
        if (style == null) return false;

        return style.OverflowY is Overflow.Auto or Overflow.Scroll
            || style.OverflowX is Overflow.Auto or Overflow.Scroll;
    }

    /// <summary>
    /// 从目标元素向上查找最近的可滚动容器
    /// </summary>
    private LayoutBox? FindScrollableBox(Element target, float deltaX, float deltaY)
    {
        if (_currentLayout == null) return null;

        var current = target;
        while (current != null)
        {
            var box = FindLayoutBoxForElement(_currentLayout, current);
            if (box != null)
            {
                bool canScrollY = Math.Abs(deltaY) > 0.01f &&
                    (box.ComputedStyle.OverflowY == Overflow.Auto || box.ComputedStyle.OverflowY == Overflow.Scroll) &&
                    box.ScrollableContentHeight > box.BoxModel.PaddingBox.Height;

                bool canScrollX = Math.Abs(deltaX) > 0.01f &&
                    (box.ComputedStyle.OverflowX == Overflow.Auto || box.ComputedStyle.OverflowX == Overflow.Scroll) &&
                    box.ScrollableContentWidth > box.BoxModel.PaddingBox.Width;

                if (canScrollY || canScrollX)
                    return box;
            }
            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// 在布局树中查找对应元素的 LayoutBox
    /// </summary>
    private static LayoutBox? FindLayoutBoxForElement(LayoutBox box, Element element)
    {
        if (box.Element == element) return box;

        foreach (var child in box.Children)
        {
            var found = FindLayoutBoxForElement(child, element);
            if (found != null) return found;
        }

        return null;
    }

    public enum ScrollbarHitType { None, VerticalThumb, VerticalTrack, HorizontalThumb, HorizontalTrack }

    public record ScrollbarHitResult(LayoutBox Box, ScrollbarHitType HitType, float ThumbOffset);

    /// <summary>
    /// 检测鼠标是否点击在某个滚动条上
    /// </summary>
    public ScrollbarHitResult? HitTestScrollbar(float x, float y)
    {
        if (_currentLayout == null) return null;
        return HitTestScrollbarBox(_currentLayout, x, y, 0, 0);
    }

    private static ScrollbarHitResult? HitTestScrollbarBox(LayoutBox box, float x, float y, float scrollOffsetX, float scrollOffsetY)
    {
        var borderBox = box.BoxModel.BorderBox;
        float adjustedLeft = borderBox.Left - scrollOffsetX;
        float adjustedRight = borderBox.Right - scrollOffsetX;
        float adjustedTop = borderBox.Top - scrollOffsetY;
        float adjustedBottom = borderBox.Bottom - scrollOffsetY;

        if (x < adjustedLeft || x > adjustedRight || y < adjustedTop || y > adjustedBottom)
            return null;

        var paddingBox = box.BoxModel.PaddingBox;
        float pLeft = paddingBox.X - scrollOffsetX;
        float pTop = paddingBox.Y - scrollOffsetY;
        float pRight = pLeft + paddingBox.Width;
        float pBottom = pTop + paddingBox.Height;

        if (box.HasVerticalScrollbar)
        {
            float trackX = pRight - LayoutBox.ScrollbarThickness;
            float trackHeight = paddingBox.Height - (box.HasHorizontalScrollbar ? LayoutBox.ScrollbarThickness : 0);

            if (x >= trackX && x <= pRight && y >= pTop && y <= pTop + trackHeight)
            {
                var (thumbTop, thumbHeight) = GetVerticalThumbGeometry(box, trackHeight);
                float screenThumbTop = thumbTop - scrollOffsetY;
                float screenThumbBottom = screenThumbTop + thumbHeight;
                if (y >= screenThumbTop && y <= screenThumbBottom)
                    return new ScrollbarHitResult(box, ScrollbarHitType.VerticalThumb, y - screenThumbTop);
                return new ScrollbarHitResult(box, ScrollbarHitType.VerticalTrack, 0);
            }
        }

        if (box.HasHorizontalScrollbar)
        {
            float trackY = pBottom - LayoutBox.ScrollbarThickness;
            float trackWidth = paddingBox.Width - (box.HasVerticalScrollbar ? LayoutBox.ScrollbarThickness : 0);

            if (x >= pLeft && x <= pLeft + trackWidth && y >= trackY && y <= pBottom)
            {
                var (thumbLeft, thumbWidth) = GetHorizontalThumbGeometry(box, trackWidth);
                float screenThumbLeft = thumbLeft - scrollOffsetX;
                float screenThumbRight = screenThumbLeft + thumbWidth;
                if (x >= screenThumbLeft && x <= screenThumbRight)
                    return new ScrollbarHitResult(box, ScrollbarHitType.HorizontalThumb, x - screenThumbLeft);
                return new ScrollbarHitResult(box, ScrollbarHitType.HorizontalTrack, 0);
            }
        }

        float childScrollOffsetX = scrollOffsetX + box.ScrollLeft;
        float childScrollOffsetY = scrollOffsetY + box.ScrollTop;

        foreach (var child in box.Children)
        {
            var hit = HitTestScrollbarBox(child, x, y, childScrollOffsetX, childScrollOffsetY);
            if (hit != null) return hit;
        }

        return null;
    }

    private static (float thumbTop, float thumbHeight) GetVerticalThumbGeometry(LayoutBox box, float trackHeight)
    {
        float viewportH = box.BoxModel.PaddingBox.Height;
        float contentH = box.ScrollableContentHeight;
        float thumbHeight = Math.Max(trackHeight * (viewportH / contentH), 20f);
        float scrollableTrack = trackHeight - thumbHeight;
        float maxScroll = contentH - viewportH;
        float thumbTop = box.BoxModel.PaddingBox.Y + (maxScroll > 0 ? (box.ScrollTop / maxScroll) * scrollableTrack : 0);
        return (thumbTop, thumbHeight);
    }

    private static (float thumbLeft, float thumbWidth) GetHorizontalThumbGeometry(LayoutBox box, float trackWidth)
    {
        float viewportW = box.BoxModel.PaddingBox.Width;
        float contentW = box.ScrollableContentWidth;
        float thumbWidth = Math.Max(trackWidth * (viewportW / contentW), 20f);
        float scrollableTrack = trackWidth - thumbWidth;
        float maxScroll = contentW - viewportW;
        float thumbLeft = box.BoxModel.PaddingBox.X + (maxScroll > 0 ? (box.ScrollLeft / maxScroll) * scrollableTrack : 0);
        return (thumbLeft, thumbWidth);
    }

    /// <summary>
    /// 拖拽垂直滑块到指定鼠标Y位置
    /// </summary>
    public bool DragVerticalThumb(LayoutBox box, float mouseY, float thumbOffset)
    {
        var current = FindLayoutBoxForElement(_currentLayout!, box.Element);
        if (current == null) return false;

        float trackHeight = current.BoxModel.PaddingBox.Height - (current.HasHorizontalScrollbar ? LayoutBox.ScrollbarThickness : 0);
        var (_, thumbHeight) = GetVerticalThumbGeometry(current, trackHeight);
        float scrollableTrack = trackHeight - thumbHeight;
        if (scrollableTrack <= 0) return false;

        float thumbTop = mouseY - thumbOffset - current.BoxModel.PaddingBox.Y;
        float ratio = Math.Clamp(thumbTop / scrollableTrack, 0f, 1f);

        float viewportH = current.BoxModel.PaddingBox.Height;
        float maxScroll = Math.Max(0, current.ScrollableContentHeight - viewportH);
        float newScrollTop = ratio * maxScroll;

        if (Math.Abs(current.ScrollTop - newScrollTop) < 0.01f) return false;
        current.ScrollTop = newScrollTop;
        InvalidateElement(current.Element);
        return true;
    }

    public bool DragHorizontalThumb(LayoutBox box, float mouseX, float thumbOffset)
    {
        var current = FindLayoutBoxForElement(_currentLayout!, box.Element);
        if (current == null) return false;

        float trackWidth = current.BoxModel.PaddingBox.Width - (current.HasVerticalScrollbar ? LayoutBox.ScrollbarThickness : 0);
        var (_, thumbWidth) = GetHorizontalThumbGeometry(current, trackWidth);
        float scrollableTrack = trackWidth - thumbWidth;
        if (scrollableTrack <= 0) return false;

        float thumbLeft = mouseX - thumbOffset - current.BoxModel.PaddingBox.X;
        float ratio = Math.Clamp(thumbLeft / scrollableTrack, 0f, 1f);

        float viewportW = current.BoxModel.PaddingBox.Width;
        float maxScroll = Math.Max(0, current.ScrollableContentWidth - viewportW);
        float newScrollLeft = ratio * maxScroll;

        if (Math.Abs(current.ScrollLeft - newScrollLeft) < 0.01f) return false;
        current.ScrollLeft = newScrollLeft;
        InvalidateElement(current.Element);
        return true;
    }

    /// <summary>
    /// 点击滑轨，按页滚动（≈ clientSize * 0.875）
    /// </summary>
    public bool ScrollTrackClick(LayoutBox box, ScrollbarHitType hitType, float mouseX, float mouseY)
    {
        var current = FindLayoutBoxForElement(_currentLayout!, box.Element);
        if (current == null) return false;

        if (hitType == ScrollbarHitType.VerticalTrack)
        {
            float viewportH = current.BoxModel.PaddingBox.Height;
            float pageSize = viewportH * 0.875f;
            float trackHeight = viewportH - (current.HasHorizontalScrollbar ? LayoutBox.ScrollbarThickness : 0);
            var (thumbTop, _) = GetVerticalThumbGeometry(current, trackHeight);
            float delta = mouseY < thumbTop ? -pageSize : pageSize;

            float maxScroll = Math.Max(0, current.ScrollableContentHeight - viewportH);
            float newScrollTop = Math.Clamp(current.ScrollTop + delta, 0, maxScroll);
            if (Math.Abs(current.ScrollTop - newScrollTop) < 0.01f) return false;
            current.ScrollTop = newScrollTop;
            InvalidateElement(current.Element);
            return true;
        }

        if (hitType == ScrollbarHitType.HorizontalTrack)
        {
            float viewportW = current.BoxModel.PaddingBox.Width;
            float pageSize = viewportW * 0.875f;
            float trackWidth = viewportW - (current.HasVerticalScrollbar ? LayoutBox.ScrollbarThickness : 0);
            var (thumbLeft, _) = GetHorizontalThumbGeometry(current, trackWidth);
            float delta = mouseX < thumbLeft ? -pageSize : pageSize;

            float maxScroll = Math.Max(0, current.ScrollableContentWidth - viewportW);
            float newScrollLeft = Math.Clamp(current.ScrollLeft + delta, 0, maxScroll);
            if (Math.Abs(current.ScrollLeft - newScrollLeft) < 0.01f) return false;
            current.ScrollLeft = newScrollLeft;
            InvalidateElement(current.Element);
            return true;
        }

        return false;
    }
}
