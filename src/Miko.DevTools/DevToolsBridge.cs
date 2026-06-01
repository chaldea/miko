using System.Collections.Concurrent;
using Miko.Common;
using Miko.Core;
using Miko.DevTools.Logging;
using Miko.Layout;
using Miko.Rendering;
using SkiaSharp;

namespace Miko.DevTools;

public class DevToolsBridge
{
    private readonly DevToolsOptions _options;
    private DevToolsWindow? _devToolsWindow;

    public MikoEngine? MainEngine { get; private set; }
    public RenderEngine? MainRenderEngine { get; private set; }
    public ConcurrentQueue<LogEntry> LogBuffer { get; } = new();
    public bool IsOpen { get; internal set; }

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
    }

    public void Initialize(MikoEngine mainEngine, RenderEngine mainRenderEngine)
    {
        MainEngine = mainEngine;
        MainRenderEngine = mainRenderEngine;
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
