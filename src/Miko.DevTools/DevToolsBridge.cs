using Miko.Common;
using Miko.Core;
using Miko.DevTools.Logging;
using Miko.Graphics;
using Miko.Layout;
using Miko.Platform.Resources;
using Miko.Rendering;

namespace Miko.DevTools;

public class DevToolsBridge
{
    private readonly DevToolsOptions _options;

    // 打开/关闭来自多个线程：主窗口渲染线程（F12）、DevTools 渲染线程（Esc/F12）、
    // 事件循环线程（窗口被关闭后的收尾，见 OnWindowClosed）。
    private readonly object _windowGate = new();
    private DevToolsWindow? _devToolsWindow;

    public MikoEngine? MainEngine { get; private set; }

    /// <summary>
    /// 主引擎的渲染引擎（高亮覆盖层挂在它上面）。取自 <see cref="MikoEngine.RenderEngine"/>，
    /// 保证与主引擎实际使用的是同一个实例——多引擎下从 DI 另行解析不再有此保证（ISSUE-129）。
    /// </summary>
    public RenderEngine? MainRenderEngine => MainEngine?.RenderEngine;
    public LogBuffer LogBuffer { get; }
    public bool IsOpen { get; private set; }

    /// <summary>
    /// 应用注册的资源程序集提供器（<c>builder.AddResourceAssembly(...)</c>），
    /// 供 Resources 面板枚举嵌入资源清单。未解析到时面板显示空列表。
    /// </summary>
    public IResourceAssemblyProvider? ResourceAssemblies { get; private set; }

    /// <summary>
    /// 主引擎的图片加载器可观测面。默认的 <see cref="ResourceManager"/> 实现了
    /// <see cref="IResourceDiagnostics"/>；应用换用自定义加载器且未实现该接口时为 <c>null</c>，
    /// 此时面板只显示嵌入资源清单（那部分来自程序集元数据，与加载器无关）。
    /// </summary>
    public IResourceDiagnostics? ResourceDiagnostics => MainEngine?.ImageLoader as IResourceDiagnostics;

    private volatile Element? _selectedElement;
    public Element? SelectedElement
    {
        get => _selectedElement;
        set
        {
            if (_selectedElement != value)
            {
                _selectedElement = value;
                UpdateOverlay();
                MarkDevToolsDirty();
            }
        }
    }

    public DevToolsBridge(DevToolsOptions options)
    {
        _options = options;
        LogBuffer = new LogBuffer(options.MaxBufferedEntries);
    }

    /// <summary>
    /// 绑定主引擎。渲染引擎不再单独传入——从 <see cref="MikoEngine.RenderEngine"/> 取，
    /// 保证与主引擎实际使用的是同一个实例（ISSUE-129）。
    /// </summary>
    /// <param name="mainEngine">被检查的引擎。</param>
    /// <param name="resourceAssemblies">
    /// 应用注册的资源程序集提供器（Resources 面板用）。可为空——此时嵌入资源列表为空。
    /// </param>
    public void Initialize(MikoEngine mainEngine, IResourceAssemblyProvider? resourceAssemblies = null)
    {
        MainEngine = mainEngine;
        ResourceAssemblies = resourceAssemblies;
    }

    public void ToggleDevTools()
    {
        if (IsOpen)
            CloseDevTools();
        else
            OpenDevTools();
    }

    /// <summary>
    /// 打开 DevTools 窗口。窗口挂在桌面宿主的事件循环上创建（见 <see cref="Windowing.Common.SilkEventLoop"/>），
    /// 没有正在运行的桌面宿主时（移动端宿主、单元测试）什么都不做，<see cref="IsOpen"/> 保持 <c>false</c>。
    /// </summary>
    public void OpenDevTools()
    {
        lock (_windowGate)
        {
            if (IsOpen || MainEngine == null) return;
            var window = new DevToolsWindow(this, _options);
            if (!window.Open()) return;
            _devToolsWindow = window;
            IsOpen = true;
        }
    }

    public void CloseDevTools()
    {
        DevToolsWindow? window;
        lock (_windowGate)
        {
            if (!IsOpen) return;
            IsOpen = false;
            window = _devToolsWindow;
            _devToolsWindow = null;
        }
        SelectedElement = null;
        window?.Close();
    }

    /// <summary>
    /// 事件循环线程：窗口已销毁（用户点了关闭按钮、<see cref="CloseDevTools"/>，或宿主退出）。
    /// 只在它仍是当前窗口时复位状态——快速按两次 F12 时，旧窗口的收尾会晚于新窗口的打开。
    /// </summary>
    internal void OnWindowClosed(DevToolsWindow window)
    {
        lock (_windowGate)
        {
            if (!ReferenceEquals(_devToolsWindow, window)) return;
            _devToolsWindow = null;
            IsOpen = false;
        }
        SelectedElement = null;
    }

    internal void MarkDevToolsDirty()
    {
        _devToolsWindow?.MarkDirty();
    }

    private void UpdateOverlay()
    {
        if (MainRenderEngine == null) return;

        var selected = _selectedElement;
        if (selected == null)
        {
            MainRenderEngine.OverlayCallback = null;
            // 请求主窗口重绘一帧，把高亮擦掉（理由同下）。
            MainEngine?.RequestRepaint();
            return;
        }

        MainRenderEngine.OverlayCallback = canvas =>
        {
            var element = _selectedElement;
            if (element == null || MainEngine == null) return;

            var root = MainEngine.GetCurrentLayout();
            if (root == null) return;

            // 计算祖先滚动容器累计的滚动偏移：渲染时每个滚动容器对其子元素
            // 应用了 Translate(-ScrollLeft, -ScrollTop)，因此高亮框也需扣除该偏移
            float scrollX = 0, scrollY = 0;
            var layoutBox = FindLayoutBox(root, element, ref scrollX, ref scrollY);
            if (layoutBox == null) return;

            DrawHighlight(canvas, layoutBox, scrollX, scrollY);
        };

        // 覆盖层的内容变了，但主窗口的 DOM 一个字节都没动，因此没有任何元素可以标脏。
        // 不显式请求重绘，主窗口就会在 HasPendingVisualWork == false 时整帧跳过
        // （ISSUE-096 的空闲跳帧），高亮永远画不出来。
        //
        // 变更版本号还是进程级全局静态时这里能侥幸工作：DevTools 窗口自身的 DOM 重建会不断
        // 递增全局计数，连带击穿主引擎的布局缓存，主窗口因此永不空闲。ISSUE-129 按引擎隔离
        // 之后这个巧合消失，必须显式请求（否则表现为「点 DOM 树，主窗口不高亮」）。
        MainEngine?.RequestRepaint();
    }

    private static LayoutBox? FindLayoutBox(LayoutBox root, Element element, ref float scrollX, ref float scrollY)
    {
        if (root.Element == element) return root;

        float childScrollX = scrollX + root.ScrollLeft;
        float childScrollY = scrollY + root.ScrollTop;

        foreach (var child in root.Children)
        {
            float sx = childScrollX, sy = childScrollY;
            var found = FindLayoutBox(child, element, ref sx, ref sy);
            if (found != null)
            {
                scrollX = sx;
                scrollY = sy;
                return found;
            }
        }
        return null;
    }

    // 盒模型四层的高亮色（Chrome DevTools 的配色），半透明叠在页面上。
    private static readonly Color MarginColor = new(246, 178, 107, 60);
    private static readonly Color BorderColor = new(253, 216, 53, 60);
    private static readonly Color PaddingColor = new(129, 199, 132, 60);
    private static readonly Color ContentColor = new(100, 150, 255, 60);
    private static readonly Color OutlineColor = new(66, 133, 244, 200);

    private static void DrawHighlight(IDrawingContext canvas, LayoutBox box, float scrollX, float scrollY)
    {
        var margin = box.BoxModel.MarginBox;
        var border = box.BoxModel.BorderBox;
        var padding = box.BoxModel.PaddingBox;
        var content = box.BoxModel.Content;

        canvas.FillRect(Offset(margin, scrollX, scrollY), MarginColor);
        canvas.FillRect(Offset(border, scrollX, scrollY), BorderColor);
        canvas.FillRect(Offset(padding, scrollX, scrollY), PaddingColor);
        canvas.FillRect(Offset(content, scrollX, scrollY), ContentColor);
        canvas.StrokeRect(Offset(border, scrollX, scrollY), OutlineColor, 1.5f);
    }

    private static RectF Offset(RectF rect, float scrollX, float scrollY)
        => new(rect.X - scrollX, rect.Y - scrollY, rect.Width, rect.Height);
}
