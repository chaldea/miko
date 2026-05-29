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

            var layoutBox = FindLayoutBox(MainEngine.GetCurrentLayout(), element);
            if (layoutBox == null) return;

            DrawHighlight(canvas, layoutBox);
        };
    }

    private static LayoutBox? FindLayoutBox(LayoutBox? root, Element element)
    {
        if (root == null) return null;
        if (root.Element == element) return root;
        foreach (var child in root.Children)
        {
            var found = FindLayoutBox(child, element);
            if (found != null) return found;
        }
        return null;
    }

    private static void DrawHighlight(SKCanvas canvas, LayoutBox box)
    {
        var margin = box.BoxModel.MarginBox;
        var border = box.BoxModel.BorderBox;
        var padding = box.BoxModel.PaddingBox;
        var content = box.BoxModel.Content;

        using var marginPaint = new SKPaint { Color = new SKColor(246, 178, 107, 60), Style = SKPaintStyle.Fill };
        canvas.DrawRect(margin.X, margin.Y, margin.Width, margin.Height, marginPaint);

        using var borderPaint = new SKPaint { Color = new SKColor(253, 216, 53, 60), Style = SKPaintStyle.Fill };
        canvas.DrawRect(border.X, border.Y, border.Width, border.Height, borderPaint);

        using var paddingPaint = new SKPaint { Color = new SKColor(129, 199, 132, 60), Style = SKPaintStyle.Fill };
        canvas.DrawRect(padding.X, padding.Y, padding.Width, padding.Height, paddingPaint);

        using var contentPaint = new SKPaint { Color = new SKColor(100, 150, 255, 60), Style = SKPaintStyle.Fill };
        canvas.DrawRect(content.X, content.Y, content.Width, content.Height, contentPaint);

        using var outlinePaint = new SKPaint
        {
            Color = new SKColor(66, 133, 244, 200),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true
        };
        canvas.DrawRect(border.X, border.Y, border.Width, border.Height, outlinePaint);
    }
}
