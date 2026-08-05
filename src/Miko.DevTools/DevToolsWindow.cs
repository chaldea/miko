using Microsoft.Extensions.Logging;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.DevTools.Panels;
using Miko.DevTools.Styles;
using Miko.Events;
using Miko.Layout;
using Miko.Rendering;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Miko.DevTools;

internal class DevToolsWindow
{
    private readonly DevToolsBridge _bridge;
    private readonly DevToolsOptions _options;
    private readonly MikoEngine _engine;
    private readonly EventDispatcher _eventDispatcher = new();

    private Thread? _thread;
    private IWindow? _window;
    private IInputContext? _inputContext;
    private GL? _gl;
    private GRContext? _grContext;
    private int _width;
    private int _height;
    private volatile bool _shouldClose;

    private Element? _lastRoot;
    private volatile bool _needsRebuild;
    private bool _scrollToConsoleBottom;

    private bool _isDragging;
    private MikoEngine.ScrollbarHitResult? _draggingScrollbar;

    private string _activeTab = "elements";
    private Element? _lastSelectedElement;
    private long _lastLogSequence = -1;
    private long _lastCollapseVersion = -1;
    private RectF _lastSelectedBorderBox;
    private LogLevel _consoleFilterLevel = LogLevel.Trace;

    // 样式表只构建一次并复用同一 List 引用：LayoutEngine 的缓存键按引用比较样式表列表
    // （见 LayoutEngine.IsLayoutCurrent），每次重建都造新表会额外永久击穿布局缓存。
    // 样式表按 ISSUE-096 的不可变约定对待——DevTools 从不改写规则内容，故复用是安全的。
    private readonly List<Styling.StyleSheet> _styleSheets = new() { DevToolsStyleSheet.Create() };

    // 重建时用于 Initialize 的临时 surface。按尺寸缓存，避免每次重建都新建一张
    // 全窗口大小的 SKSurface（非托管内存）。
    private SKSurface? _initSurface;
    private int _initSurfaceWidth;
    private int _initSurfaceHeight;

    // 兜底出帧计数：首帧与 resize 后必须连续画满整个缓冲链（双缓冲下一帧只填了一个
    // 后备缓冲，另一个仍是旧内容/未初始化，只画一帧会在下次交换时闪回旧画面）。
    // 用计数而非布尔值即为此。
    private int _pendingPresents = SwapChainDepth;

    /// <summary>兜底出帧的连续帧数，覆盖双缓冲（保守起见留一帧余量）。</summary>
    private const int SwapChainDepth = 3;

    public DevToolsWindow(DevToolsBridge bridge, DevToolsOptions options)
    {
        _bridge = bridge;
        _options = options;
        _engine = new MikoEngine();
        _width = options.Width;
        _height = options.Height;
    }

    public void Open()
    {
        _shouldClose = false;
        _thread = new Thread(RunWindow) { IsBackground = true, Name = "DevToolsWindow" };
        _thread.Start();
    }

    public void Close()
    {
        _shouldClose = true;
    }

    private void RunWindow()
    {
        var options = WindowOptions.Default with
        {
            Title = "Miko DevTools",
            Size = new Vector2D<int>(_width, _height),
            API = GraphicsAPI.Default,
            // 自行管理缓冲交换：空闲跳帧时我们**什么都不画**，若仍让 Silk 自动交换，
            // 就会把未绘制的后备缓冲呈现出来，与绘制过的缓冲交替 → 画面闪烁。
            // 只有真正画了一帧才交换（见 OnRender）。
            ShouldSwapAutomatically = false,
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Resize += OnResize;
        _window.Closing += OnClose;
        _window.Run();
        _window.Dispose();
    }

    private void OnLoad()
    {
        _gl = _window!.CreateOpenGL();

        var grInterface = GRGlInterface.Create(name =>
        {
            if (_window!.GLContext!.TryGetProcAddress(name, out var addr))
                return addr;
            return IntPtr.Zero;
        });

        _grContext = GRContext.CreateGl(grInterface);

        _inputContext = _window!.CreateInput();
        foreach (var mouse in _inputContext.Mice)
        {
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
            mouse.MouseMove += OnMouseMove;
            mouse.Scroll += OnMouseScroll;
        }

        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
        }

        _engine.Initialize(BuildUI(), _styleSheets, GetInitCanvas(), _width, _height);
    }

    /// <summary>
    /// <see cref="MikoEngine.Initialize"/> 需要一个 canvas，但重建时真正的帧 canvas 尚未创建。
    /// 复用一张按窗口尺寸缓存的离屏 surface，避免每次重建都分配非托管内存。
    /// </summary>
    private SKCanvas GetInitCanvas()
    {
        if (_initSurface == null || _initSurfaceWidth != _width || _initSurfaceHeight != _height)
        {
            _initSurface?.Dispose();
            _initSurface = SKSurface.Create(new SKImageInfo(Math.Max(1, _width), Math.Max(1, _height)));
            _initSurfaceWidth = _width;
            _initSurfaceHeight = _height;
        }
        return _initSurface.Canvas;
    }

    private void OnUpdate(double _)
    {
        if (_shouldClose)
        {
            _window?.Close();
        }
    }

    /// <summary>
    /// 每帧入口。稳态下**既不重建 DOM 也不产帧**（见 ISSUE-117）。
    /// <para>与 <c>SilkDesktopHost.RenderLoop</c> 的空闲跳帧同构，但判据不同：DevTools 引擎
    /// 不能用 <see cref="MikoEngine.HasPendingVisualWork"/>。原因是 <c>Element.MutationVersion</c>
    /// 是**进程级全局静态**，主窗口在另一线程持续变更自己的 DOM 就会不断递增它，于是
    /// DevTools 引擎的 <c>IsLayoutCurrent</c> 恒为 false，<c>HasPendingVisualWork</c> 也就恒为 true，
    /// 永远无法空闲。因此这里改用两段判据：</para>
    /// <list type="number">
    /// <item>DOM 是否需要重建 —— 由 <see cref="ConsumeRebuildRequest"/> 按输入指纹判断；</item>
    /// <item>引擎内部是否有待呈现工作 —— 用不含布局时效性检查的
    /// <see cref="MikoEngine.HasPendingRenderWork"/>（脏区域、动画、跨线程失效）。</item>
    /// </list>
    /// </summary>
    private void OnRender(double _)
    {
        if (_grContext == null || _gl == null) return;

        bool shouldRebuild = ConsumeRebuildRequest();
        if (shouldRebuild)
        {
            RebuildUI();
        }

        if (_scrollToConsoleBottom)
        {
            _scrollToConsoleBottom = false;
            ScrollConsoleToBottom();
        }

        // 内容有任何变化（重建或引擎内部脏区域/动画）都要刷满整个缓冲链，而不是只画一帧：
        // 引擎是增量绘制（只重绘脏区域），双缓冲下若只画一个后备缓冲，下次交换会露出
        // 另一个仍是旧内容的缓冲 → 画面在新旧之间闪烁。
        if (shouldRebuild || _engine.HasPendingRenderWork)
        {
            _pendingPresents = SwapChainDepth;
        }

        // 空闲跳帧：内容未变且兜底帧已画满时，本帧什么都不做。
        // 关键在于「不创建 GRBackendRenderTarget/SKSurface、不调用 Render、不交换缓冲」——
        // 稳态下这条路径零分配，GC 锯齿由此消失。
        if (_pendingPresents <= 0)
        {
            // Silk 的 Run() 循环会尽可能快地回调 OnRender，这里主动让出 CPU，
            // 否则空转会持续占满一个核心（并连带影响同进程的主窗口）。
            Thread.Sleep(Math.Max(1, 1000 / Math.Max(1, _options.TargetFramesPerSecond)));
            return;
        }

        _pendingPresents--;

        int fboId = _gl.GetInteger(GLEnum.FramebufferBinding);
        var fbInfo = new GRGlFramebufferInfo((uint)fboId, 0x8058);
        // 每帧新建的非托管 Skia 对象，必须随帧释放（否则原生内存随帧数线性增长，见 ISSUE-113）。
        using var target = new GRBackendRenderTarget(_width, _height, 0, 8, fbInfo);

        using var surface = SKSurface.Create(_grContext, target, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(36, 36, 36));
        _engine.Render(canvas);
        canvas.Flush();
        _grContext.Flush();

        // 我们接管了缓冲交换（ShouldSwapAutomatically = false）：只有真正画了一帧才呈现。
        _window?.GLContext?.SwapBuffers();
    }

    /// <summary>
    /// 判断本帧是否需要重建镜像 DOM，并就地记录新的指纹。
    /// 只比较**真实的构建输入**：主树引用、选中元素、活动标签、日志序号、折叠状态版本，
    /// 以及选中元素的盒模型几何（Style 面板显示的是主引擎实时布局值，主窗口重排后会变）。
    /// </summary>
    private bool ConsumeRebuildRequest()
    {
        bool shouldRebuild = _needsRebuild;
        _needsRebuild = false;

        var currentRoot = _bridge.MainEngine?.GetRoot();
        if (!ReferenceEquals(currentRoot, _lastRoot))
        {
            _lastRoot = currentRoot;
            shouldRebuild = true;
        }

        var selected = _bridge.SelectedElement;
        if (!ReferenceEquals(selected, _lastSelectedElement))
        {
            _lastSelectedElement = selected;
            shouldRebuild = true;
        }

        // 选中元素的几何：只查这一个盒子，不遍历全树。
        var selectedBorderBox = GetSelectedBorderBox(selected);
        if (!SameRect(selectedBorderBox, _lastSelectedBorderBox))
        {
            _lastSelectedBorderBox = selectedBorderBox;
            shouldRebuild = true;
        }

        long collapseVersion = DomTreeBuilder.CollapseVersion;
        if (collapseVersion != _lastCollapseVersion)
        {
            _lastCollapseVersion = collapseVersion;
            shouldRebuild = true;
        }

        // 日志用单调序号而非条目数：缓冲有界，裁剪后条目数可能不变却已有新内容。
        long logSequence = _bridge.LogBuffer.Sequence;
        if (logSequence != _lastLogSequence)
        {
            _lastLogSequence = logSequence;
            if (_activeTab == "console")
            {
                shouldRebuild = true;
                _scrollToConsoleBottom = true;
            }
        }

        return shouldRebuild;
    }

    /// <summary>
    /// 选中元素在主窗口中的边框盒。用于察觉「元素没换、但主窗口重排导致数值变化」
    /// 的情况——Style 面板读取的正是主引擎的实时布局（见 StyleInspector）。
    /// </summary>
    private RectF GetSelectedBorderBox(Element? selected)
    {
        if (selected == null) return default;
        var layout = _bridge.MainEngine?.GetCurrentLayout();
        if (layout == null) return default;
        var box = FindLayoutBoxByElement(layout, selected);
        return box?.BoxModel.BorderBox ?? default;
    }

    /// <summary>逐分量比较两个矩形（RectF 未定义相等运算符）。</summary>
    private static bool SameRect(RectF a, RectF b) =>
        Math.Abs(a.X - b.X) < 0.01f
        && Math.Abs(a.Y - b.Y) < 0.01f
        && Math.Abs(a.Width - b.Width) < 0.01f
        && Math.Abs(a.Height - b.Height) < 0.01f;

    private static LayoutBox? FindLayoutBoxByElement(LayoutBox box, Element element)
    {
        if (ReferenceEquals(box.Element, element)) return box;
        foreach (var child in box.Children)
        {
            var found = FindLayoutBoxByElement(child, element);
            if (found != null) return found;
        }
        return null;
    }

    private void OnResize(Vector2D<int> size)
    {
        _width = size.X;
        _height = size.Y;
        _gl?.Viewport(size);
        _engine.SetViewportSize(size.X, size.Y);
        // 尺寸变化后帧缓冲内容失效，必须强制连续呈现若干帧刷满缓冲链。
        _pendingPresents = SwapChainDepth;
    }

    private void OnMouseDown(IMouse mouse, SilkMouseButton button)
    {
        if (button != SilkMouseButton.Left) return;

        var scrollbarHit = _engine.HitTestScrollbar(mouse.Position.X, mouse.Position.Y);
        if (scrollbarHit != null)
        {
            _draggingScrollbar = scrollbarHit;
            _isDragging = true;
            if (scrollbarHit.HitType == MikoEngine.ScrollbarHitType.VerticalThumb ||
                scrollbarHit.HitType == MikoEngine.ScrollbarHitType.HorizontalThumb)
            {
                return;
            }
        }

        var target = _engine.HitTest(mouse.Position.X, mouse.Position.Y);
        if (target == null) return;

        var args = new MouseEventArgs
        {
            Target = target,
            X = mouse.Position.X,
            Y = mouse.Position.Y,
            Button = Events.MouseButton.Left,
            Bubbles = true
        };
        _eventDispatcher.Dispatch(target, EventTypes.Click, args);
    }

    private void OnMouseUp(IMouse mouse, SilkMouseButton button)
    {
        if (button != SilkMouseButton.Left) return;

        if (_isDragging && _draggingScrollbar != null)
        {
            var hit = _draggingScrollbar;
            _draggingScrollbar = null;
            _isDragging = false;
            if (hit.HitType == MikoEngine.ScrollbarHitType.VerticalTrack ||
                hit.HitType == MikoEngine.ScrollbarHitType.HorizontalTrack)
            {
                _engine.ScrollTrackClick(hit.Box, hit.HitType, mouse.Position.X, mouse.Position.Y);
            }
            return;
        }

        _isDragging = false;
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
    {
        if (!_isDragging || _draggingScrollbar == null) return;

        var hit = _draggingScrollbar;
        if (hit.HitType == MikoEngine.ScrollbarHitType.VerticalThumb)
            _engine.DragVerticalThumb(hit.Box, position.Y, hit.ThumbOffset);
        else if (hit.HitType == MikoEngine.ScrollbarHitType.HorizontalThumb)
            _engine.DragHorizontalThumb(hit.Box, position.X, hit.ThumbOffset);
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
    {
        float deltaY = scrollWheel.Y * -40f;
        float deltaX = scrollWheel.X * -40f;
        _engine.ScrollBy(mouse.Position.X, mouse.Position.Y, deltaX, deltaY);
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.Escape || key == Key.F12)
        {
            _bridge.CloseDevTools();
        }
    }

    private void OnClose()
    {
        _bridge.IsOpen = false;
        _bridge.SelectedElement = null;
        _inputContext?.Dispose();
        _initSurface?.Dispose();
        _initSurface = null;
        _grContext?.Dispose();
        _gl?.Dispose();
    }

    internal void MarkDirty()
    {
        _needsRebuild = true;
    }

    private void ScrollConsoleToBottom()
    {
        var layout = _engine.GetCurrentLayout();
        if (layout == null) return;

        var outputBox = FindLayoutBoxByClass(layout, "console-output");
        if (outputBox == null || !outputBox.HasVerticalScrollbar) return;

        float maxScroll = outputBox.ScrollableContentHeight - outputBox.BoxModel.PaddingBox.Height;
        if (maxScroll > 0)
        {
            outputBox.ScrollTop = maxScroll;
            // 直接改写 ScrollTop 绕过了引擎的失效入口，需显式标脏才会重绘（见 ISSUE-104）。
            _engine.InvalidateElement(outputBox.Element);
        }
    }

    private static LayoutBox? FindLayoutBoxByClass(LayoutBox box, string className)
    {
        if (box.Element.HasClass(className)) return box;
        foreach (var child in box.Children)
        {
            var found = FindLayoutBoxByClass(child, className);
            if (found != null) return found;
        }
        return null;
    }

    private void RebuildUI()
    {
        // 窗口被最小化
        if (_width == 0 || _height == 0) return;

        // 保存当前滚动位置
        float domTreeScrollTop = 0;
        float stylePanelScrollTop = 0;
        var currentLayout = _engine.GetCurrentLayout();
        if (currentLayout != null)
        {
            var domTreeBox = FindLayoutBoxByClass(currentLayout, "dom-tree-panel");
            if (domTreeBox != null) domTreeScrollTop = domTreeBox.ScrollTop;

            var stylePanelBox = FindLayoutBoxByClass(currentLayout, "style-panel");
            if (stylePanelBox != null) stylePanelScrollTop = stylePanelBox.ScrollTop;
        }

        _engine.Initialize(BuildUI(), _styleSheets, GetInitCanvas(), _width, _height);

        // 恢复滚动位置
        var newLayout = _engine.GetCurrentLayout();
        if (newLayout != null)
        {
            var newDomTreeBox = FindLayoutBoxByClass(newLayout, "dom-tree-panel");
            if (newDomTreeBox != null) newDomTreeBox.ScrollTop = domTreeScrollTop;

            var newStylePanelBox = FindLayoutBoxByClass(newLayout, "style-panel");
            if (newStylePanelBox != null) newStylePanelBox.ScrollTop = stylePanelScrollTop;
        }
    }

    private Element BuildUI()
    {
        var root = new DivElement { Id = "devtools-root", Class = "devtools-root" };

        var toolbar = BuildToolbar();
        root.AddChild(toolbar);

        var content = new DivElement { Id = "devtools-content", Class = "devtools-content" };

        var elementsPanel = ElementsPanel.Build(_bridge, _activeTab == "elements");
        var consolePanel = ConsolePanel.Build(_bridge, _consoleFilterLevel, _activeTab == "console", level =>
        {
            _consoleFilterLevel = level;
            _needsRebuild = true;
        });

        content.AddChild(elementsPanel);
        content.AddChild(consolePanel);

        root.AddChild(content);
        return root;
    }

    private DivElement BuildToolbar()
    {
        var toolbar = new DivElement { Class = "devtools-toolbar" };

        var elementsTabBtn = new DivElement
        {
            Class = _activeTab == "elements" ? "devtools-tab devtools-tab-active" : "devtools-tab",
            TextContent = "Elements"
        };
        elementsTabBtn.OnClick = _ =>
        {
            if (_activeTab != "elements")
            {
                _activeTab = "elements";
                _needsRebuild = true;
            }
        };

        var consoleTabBtn = new DivElement
        {
            Class = _activeTab == "console" ? "devtools-tab devtools-tab-active" : "devtools-tab",
            TextContent = "Console"
        };
        consoleTabBtn.OnClick = _ =>
        {
            if (_activeTab != "console")
            {
                _activeTab = "console";
                _needsRebuild = true;
            }
        };

        toolbar.AddChild(elementsTabBtn);
        toolbar.AddChild(consoleTabBtn);
        return toolbar;
    }
}
