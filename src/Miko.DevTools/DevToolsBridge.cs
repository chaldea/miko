using Miko.Common;
using Miko.Core;
using Miko.DevTools.Logging;
using Miko.Layout;
using Miko.Platform.Resources;
using Miko.Rendering;
using SkiaSharp;

namespace Miko.DevTools;

public class DevToolsBridge
{
    private readonly DevToolsOptions _options;
    private DevToolsWindow? _devToolsWindow;

    public MikoEngine? MainEngine { get; private set; }

    /// <summary>
    /// 主引擎的渲染引擎（高亮覆盖层挂在它上面）。取自 <see cref="MikoEngine.RenderEngine"/>，
    /// 保证与主引擎实际使用的是同一个实例——多引擎下从 DI 另行解析不再有此保证（ISSUE-129）。
    /// </summary>
    public RenderEngine? MainRenderEngine => MainEngine?.RenderEngine;
    public LogBuffer LogBuffer { get; }
    public bool IsOpen { get; internal set; }

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

    public void OpenDevTools()
    {
        if (IsOpen || MainEngine == null) return;
        IsOpen = true;
        _devToolsWindow = new DevToolsWindow(this, _options);
        _devToolsWindow.Open();
    }

    public void CloseDevTools()
    {
        if (!IsOpen) return;
        IsOpen = false;
        SelectedElement = null;
        _devToolsWindow?.Close();
        _devToolsWindow = null;
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

    private static void DrawHighlight(SKCanvas canvas, LayoutBox box, float scrollX, float scrollY)
    {
        var margin = box.BoxModel.MarginBox;
        var border = box.BoxModel.BorderBox;
        var padding = box.BoxModel.PaddingBox;
        var content = box.BoxModel.Content;

        using var marginPaint = new SKPaint { Color = new SKColor(246, 178, 107, 60), Style = SKPaintStyle.Fill };
        canvas.DrawRect(margin.X - scrollX, margin.Y - scrollY, margin.Width, margin.Height, marginPaint);

        using var borderPaint = new SKPaint { Color = new SKColor(253, 216, 53, 60), Style = SKPaintStyle.Fill };
        canvas.DrawRect(border.X - scrollX, border.Y - scrollY, border.Width, border.Height, borderPaint);

        using var paddingPaint = new SKPaint { Color = new SKColor(129, 199, 132, 60), Style = SKPaintStyle.Fill };
        canvas.DrawRect(padding.X - scrollX, padding.Y - scrollY, padding.Width, padding.Height, paddingPaint);

        using var contentPaint = new SKPaint { Color = new SKColor(100, 150, 255, 60), Style = SKPaintStyle.Fill };
        canvas.DrawRect(content.X - scrollX, content.Y - scrollY, content.Width, content.Height, contentPaint);

        using var outlinePaint = new SKPaint
        {
            Color = new SKColor(66, 133, 244, 200),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true
        };
        canvas.DrawRect(border.X - scrollX, border.Y - scrollY, border.Width, border.Height, outlinePaint);
    }
}
