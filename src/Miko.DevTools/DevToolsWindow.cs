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
    private int _lastLogBufferCount;
    private LogLevel _consoleFilterLevel = LogLevel.Trace;

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
            API = GraphicsAPI.Default
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

        var root = BuildUI();
        var styleSheets = new List<Styling.StyleSheet> { DevToolsStyleSheet.Create() };
        using var tempSurface = SKSurface.Create(new SKImageInfo(_width, _height));
        _engine.Initialize(root, styleSheets, tempSurface.Canvas, _width, _height);
    }

    private void OnUpdate(double _)
    {
        if (_shouldClose)
        {
            _window?.Close();
        }
    }

    private void OnRender(double _)
    {
        if (_grContext == null || _gl == null) return;

        bool shouldRebuild = _needsRebuild;
        _needsRebuild = false;

        var currentRoot = _bridge.MainEngine?.GetRoot();
        if (currentRoot != _lastRoot)
        {
            _lastRoot = currentRoot;
            shouldRebuild = true;
        }

        if (_bridge.SelectedElement != _lastSelectedElement)
        {
            _lastSelectedElement = _bridge.SelectedElement;
            shouldRebuild = true;
        }

        var currentLogCount = _bridge.LogBuffer.Count;
        if (_activeTab == "console" && currentLogCount != _lastLogBufferCount)
        {
            _lastLogBufferCount = currentLogCount;
            shouldRebuild = true;
            _scrollToConsoleBottom = true;
        }

        if (shouldRebuild)
        {
            RebuildUI();
        }

        if (_scrollToConsoleBottom)
        {
            _scrollToConsoleBottom = false;
            ScrollConsoleToBottom();
        }

        int fboId = _gl.GetInteger(GLEnum.FramebufferBinding);
        var fbInfo = new GRGlFramebufferInfo((uint)fboId, 0x8058);
        var target = new GRBackendRenderTarget(_width, _height, 0, 8, fbInfo);

        using var surface = SKSurface.Create(_grContext, target, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(36, 36, 36));
        _engine.Render(canvas);
        canvas.Flush();
        _grContext.Flush();
    }

    private void OnResize(Vector2D<int> size)
    {
        _width = size.X;
        _height = size.Y;
        _gl?.Viewport(size);
        _engine.SetViewportSize(size.X, size.Y);
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

        var root = BuildUI();
        var styleSheets = new List<Styling.StyleSheet> { DevToolsStyleSheet.Create() };
        using var tempSurface = SKSurface.Create(new SKImageInfo(_width, _height));
        _engine.Initialize(root, styleSheets, tempSurface.Canvas, _width, _height);

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
