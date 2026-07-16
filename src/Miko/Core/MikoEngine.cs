using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Animation;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Layout;
using Miko.Platform.Video;
using Miko.Rendering;
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

    // 已激活的视频会话，按元素身份索引；用于跨重建复用与移除时回收。
    private readonly Dictionary<VideoElement, IVideoSession> _videoSessions = new();

    // 已发起加载的图片元素，按元素身份索引；避免每帧重复请求。值为加载任务。
    private readonly Dictionary<ImageElement, Task> _imageLoads = new();

    // 已发起占位图加载的元素，避免重复请求。
    private readonly HashSet<ImageElement> _placeholderLoads = new();

    // 外部线程（视频解码线程等）投递的失效请求，在帧开始时于主线程排空。
    private readonly List<Element> _pendingInvalidations = new();
    private readonly object _pendingInvalidationsLock = new();

    public void Initialize(Element root, List<StyleSheet> styleSheets, SKCanvas canvas, float viewportWidth, float viewportHeight)
    {
        // Capture old layout for scroll position restoration (ISSUE-092)
        var oldLayout = _currentLayout;

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

        // Capture old styles from transferred LayoutBoxes (before layout replaces them)
        var oldStyles = CaptureTransitionableStyles(root);

        _currentLayout = _layoutEngine.Layout(root, _styleSheets, viewportWidth, viewportHeight, _safeArea);

        // Restore scroll positions from old layout (ISSUE-092)
        RestoreScrollState(oldLayout, _currentLayout);

        if (oldStyles.Elements.Count > 0 || oldStyles.PseudoElements.Count > 0)
        {
            bool transitionsTriggered = DetectAndTriggerTransitions(root, oldStyles);
            if (transitionsTriggered)
            {
                _currentLayout = _layoutEngine.Layout(root, _styleSheets, viewportWidth, viewportHeight, _safeArea);
                // Restore scroll state again after re-layout
                RestoreScrollState(oldLayout, _currentLayout);
            }
        }

        // 同步视频会话（创建新元素的会话、回收已移除元素的会话）。
        SyncVideoSessions(root);
        // 同步图片源（为新 <img> 发起异步加载、解码占位图）。
        SyncImageSources(root);

        _renderEngine.Render(_currentLayout);
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

    public void Update(SKCanvas canvas)
    {
        if (_root == null) throw new InvalidOperationException("Engine not initialized. Call Initialize first.");

        // 排空跨线程失效请求（视频解码线程投递的新帧/加载完成）。
        _dispatcher.Drain();
        DrainPendingInvalidations();
        SyncVideoSessions(_root);
        SyncImageSources(_root);

        _renderEngine.SetCanvas(canvas);

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

        RestoreScrollState(oldLayout, _currentLayout);
        _renderEngine.Render(_currentLayout);
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

        if (!_animationManager.HasActiveAnimations && !_dirtyManager.HasDirtyRegions())
        {
            _logger.LogTrace("Tick: no active animations or dirty regions, skipping");
            return;
        }

        _logger.LogTrace("Tick: deltaTime={DeltaTime}s, activeAnimations={HasAnim}, dirtyRegions={HasDirty}",
            deltaTime, _animationManager.HasActiveAnimations, _dirtyManager.HasDirtyRegions());
        _animationManager.Update(deltaTime);
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

        // 安全区变化需要完整重新布局（与视口变化同理）
        if (_root != null)
        {
            InvalidateElement(_root);
        }
    }

    /// <summary>
    /// 添加样式表
    /// </summary>
    public void AddStyleSheet(StyleSheet styleSheet)
    {
        _styleSheets.Add(styleSheet);

        // 样式变化需要重新布局
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
        return HitTestBox(_currentLayout, x, y);
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

        if (x < adjustedLeft || x > adjustedRight || y < adjustedTop || y > adjustedBottom)
            return null;

        float childScrollOffsetX = scrollOffsetX + box.ScrollLeft;
        float childScrollOffsetY = scrollOffsetY + box.ScrollTop;

        for (int i = box.Children.Count - 1; i >= 0; i--)
        {
            var child = box.Children[i];
            var childRect = child.BoxModel.BorderBox;
            float childScreenTop = childRect.Top - childScrollOffsetY;
            float childScreenBottom = childRect.Bottom - childScrollOffsetY;
            float childScreenLeft = childRect.Left - childScrollOffsetX;
            float childScreenRight = childRect.Right - childScrollOffsetX;

            bool isClipped = (box.ScrollTop > 0 || box.ScrollLeft > 0 ||
                              box.ComputedStyle.OverflowY != Overflow.Visible ||
                              box.ComputedStyle.OverflowX != Overflow.Visible) &&
                             (childScreenBottom < adjustedTop || childScreenTop > adjustedBottom ||
                              childScreenRight < adjustedLeft || childScreenLeft > adjustedRight);

            if (isClipped) continue;

            var hit = HitTestBox(child, x, y, childScrollOffsetX, childScrollOffsetY);
            if (hit != null) return hit;
        }

        // pointer-events:none makes this element transparent to hits — the tap passes
        // through to whatever is behind it (descendants were already tested above and can
        // still be hit if they reset pointer-events to auto).
        if (box.ComputedStyle.PointerEvents == PointerEvents.None)
            return null;

        return box.Element;
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
        if (layoutBox != null && layoutBox.ComputedStyle.Transitions.Count > 0)
        {
            elements[element] = (CaptureSnapshot(layoutBox.ComputedStyle), layoutBox.ComputedStyle.Transitions);
        }

        if (layoutBox != null)
        {
            foreach (var child in layoutBox.Children)
            {
                if (child.Element is PseudoElement pseudo && child.ComputedStyle.Transitions.Count > 0)
                {
                    pseudos[(element, pseudo.Type)] = (CaptureSnapshot(child.ComputedStyle), child.ComputedStyle.Transitions);
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

    private void ScanAndStartAnimations(Element element)
    {
        var animations = element.Style?.Animations.RefValueOrNull();
        if (animations != null)
        {
            foreach (var animation in animations)
            {
                _logger.LogDebug("ScanAndStartAnimations: found animation \"{Name}\" on <{Tag} id=\"{Id}\">",
                    animation.Name, element.TagName, element.Id ?? "");
                _animationManager.StartAnimation(element, animation);
            }
        }

        foreach (var child in element.Children)
        {
            ScanAndStartAnimations(child);
        }
    }

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
    /// 重新渲染仍会被视为「同一内容」而正确恢复。</para>
    /// </summary>
    private static void RestoreScrollState(LayoutBox? oldRoot, LayoutBox? newRoot)
    {
        if (oldRoot == null || newRoot == null) return;
        // 根节点必须同标签才对齐（否则整棵树语义不同，无从恢复）。
        if (!IsSameElementIdentity(oldRoot.Element, newRoot.Element)) return;
        RestoreScrollStateRecursive(oldRoot, newRoot);
    }

    private static void RestoreScrollStateRecursive(LayoutBox oldBox, LayoutBox newBox)
    {
        // 仅当该容器有非零滚动偏移时才需要恢复；此时再验证其承载的内容（整棵子树）结构一致
        // ——结构一致，旧偏移才仍然有效。结构不同意味着「同一槽位、不同内容」（如路由切换后的
        // .main-content），旧偏移不再对应任何内容，恢复它会把新内容顶出可视区。
        // 将结构比较（O(子树大小)）延迟到确有偏移需要恢复时，避免对整棵树逐节点做深度比较。
        if ((oldBox.ScrollTop != 0f || oldBox.ScrollLeft != 0f) && HasEquivalentStructure(oldBox, newBox))
        {
            newBox.ScrollTop = oldBox.ScrollTop;
            newBox.ScrollLeft = oldBox.ScrollLeft;
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
                RestoreScrollStateRecursive(oldChild, newChild);
            }
        }
    }

    /// <summary>
    /// 判断两棵布局子树是否<b>结构等价</b>：根标签相同、子节点数量相同，且每个对应位置的子树
    /// 递归结构等价。只比较标签名与树形，忽略文本、属性、样式等叶子值——因此仅内容文本变化的
    /// 重新渲染仍算等价（滚动应恢复），而整页替换（子树形状不同）不算等价（滚动应重置）。
    /// </summary>
    private static bool HasEquivalentStructure(LayoutBox a, LayoutBox b)
    {
        if (!IsSameElementIdentity(a.Element, b.Element)) return false;
        if (a.Children.Count != b.Children.Count) return false;
        for (int i = 0; i < a.Children.Count; i++)
        {
            if (!HasEquivalentStructure(a.Children[i], b.Children[i])) return false;
        }
        return true;
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
