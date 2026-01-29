using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using SkiaSharp;

namespace Miko.Rendering;

/// <summary>
/// 渲染引擎
/// </summary>
public class RenderEngine
{
    private SKCanvas? _canvas;
    private Painter? _painter;
    private List<RectF>? _dirtyRegions;

    /// <summary>
    /// 设置画布
    /// </summary>
    public void SetCanvas(SKCanvas canvas)
    {
        _canvas = canvas;
        _painter = new Painter(canvas);
    }

    /// <summary>
    /// 全量渲染
    /// </summary>
    public void Render(LayoutBox layoutRoot)
    {
        if (_canvas == null || _painter == null)
        {
            throw new InvalidOperationException("Canvas not set. Call SetCanvas first.");
        }

        _dirtyRegions = null;
        RenderBox(layoutRoot);
    }

    /// <summary>
    /// 脏区域渲染
    /// </summary>
    public void RenderDirty(LayoutBox layoutRoot, List<RectF> dirtyRegions)
    {
        if (_canvas == null || _painter == null)
        {
            throw new InvalidOperationException("Canvas not set. Call SetCanvas first.");
        }

        _dirtyRegions = dirtyRegions;

        // 对每个脏区域进行渲染
        foreach (var region in dirtyRegions)
        {
            _painter.Save();
            _painter.ClipRect(region);
            RenderBox(layoutRoot);
            _painter.Restore();
        }

        _dirtyRegions = null;
    }

    /// <summary>
    /// 渲染盒子
    /// </summary>
    private void RenderBox(LayoutBox box)
    {
        if (_painter == null) return;

        if (!ShouldRender(box)) return;

        // 1. 绘制背景
        RenderBackground(box);

        // 2. 绘制边框
        RenderBorder(box);

        // 3. 绘制内容
        RenderContent(box);

        // 4. 递归绘制子元素
        foreach (var child in box.Children)
        {
            RenderBox(child);
        }
    }

    /// <summary>
    /// 检查是否应该渲染
    /// </summary>
    private bool ShouldRender(LayoutBox box)
    {
        // 检查是否在脏区域内
        if (_dirtyRegions != null && _dirtyRegions.Count > 0)
        {
            return _dirtyRegions.Any(r => r.IntersectsWith(box.BoxModel.BorderBox));
        }

        return true;
    }

    /// <summary>
    /// 渲染背景
    /// </summary>
    private void RenderBackground(LayoutBox box)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        if (style.BackgroundColor.A > 0)
        {
            _painter.DrawBackground(
                box.BoxModel.BorderBox,
                style.BackgroundColor,
                style.BorderTopLeftRadius.Value,
                style.BorderTopRightRadius.Value,
                style.BorderBottomRightRadius.Value,
                style.BorderBottomLeftRadius.Value
            );
        }
    }

    /// <summary>
    /// 渲染边框
    /// </summary>
    private void RenderBorder(LayoutBox box)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        if (style.BorderStyle != BorderStyle.None && style.BorderWidth.Value > 0)
        {
            _painter.DrawBorder(
                box.BoxModel.BorderBox,
                style.BorderWidth.Value,
                style.BorderColor,
                style.BorderStyle,
                style.BorderTopLeftRadius.Value,
                style.BorderTopRightRadius.Value,
                style.BorderBottomRightRadius.Value,
                style.BorderBottomLeftRadius.Value
            );
        }
    }

    /// <summary>
    /// 渲染内容
    /// </summary>
    private void RenderContent(LayoutBox box)
    {
        if (_painter == null) return;

        var element = box.Element;

        // 渲染文本内容
        if (!string.IsNullOrEmpty(element.TextContent))
        {
            var style = box.ComputedStyle;
            _painter.DrawText(
                element.TextContent,
                box.BoxModel.Content,
                style.Color,
                style.FontFamily,
                style.FontSize.Value,
                style.FontWeight,
                style.TextAlign
            );
        }

        // 渲染图片
        if (element is ImageElement imageElement && imageElement.Bitmap != null)
        {
            _painter.DrawImage(imageElement.Bitmap, box.BoxModel.Content);
        }
    }
}
