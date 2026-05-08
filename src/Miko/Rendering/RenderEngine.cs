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
        // 特殊处理：SelectElement 在未展开时不渲染子元素（与浏览器行为一致）
        if (box.Element is SelectElement selectElement && !selectElement.IsOpen)
        {
            return;
        }

        // 处理 overflow 裁剪和滚动
        bool hasOverflow = box.ComputedStyle.OverflowX != Overflow.Visible ||
                           box.ComputedStyle.OverflowY != Overflow.Visible;

        if (hasOverflow && box.Children.Count > 0)
        {
            RenderChildrenWithOverflow(box);
        }
        else
        {
            foreach (var child in box.Children)
            {
                RenderBox(child);
            }
        }

        // 5. 绘制滚动条（在裁剪区域之外）
        RenderScrollbars(box);
    }

    /// <summary>
    /// 带溢出裁剪的子元素渲染
    /// </summary>
    private void RenderChildrenWithOverflow(LayoutBox box)
    {
        if (_painter == null) return;

        var paddingBox = box.BoxModel.PaddingBox;
        float clipWidth = paddingBox.Width;
        float clipHeight = paddingBox.Height;

        // Classic 模式下，滚动条占用 padding box 空间
        if (box.HasVerticalScrollbar)
        {
            clipWidth -= LayoutBox.ScrollbarThickness;
        }
        if (box.HasHorizontalScrollbar)
        {
            clipHeight -= LayoutBox.ScrollbarThickness;
        }

        var clipRect = new RectF(paddingBox.X, paddingBox.Y, clipWidth, clipHeight);

        _painter.Save();
        _painter.ClipRect(clipRect);
        _painter.Translate(-box.ScrollLeft, -box.ScrollTop);

        foreach (var child in box.Children)
        {
            RenderBox(child);
        }

        _painter.Restore();
    }

    /// <summary>
    /// 渲染滚动条
    /// </summary>
    private void RenderScrollbars(LayoutBox box)
    {
        if (_painter == null) return;

        var paddingBox = box.BoxModel.PaddingBox;
        bool hasVScrollbar = box.HasVerticalScrollbar;
        bool hasHScrollbar = box.HasHorizontalScrollbar;

        if (hasVScrollbar)
        {
            float trackX = paddingBox.Right - LayoutBox.ScrollbarThickness;
            float trackHeight = paddingBox.Height - (hasHScrollbar ? LayoutBox.ScrollbarThickness : 0);
            var trackRect = new RectF(trackX, paddingBox.Y, LayoutBox.ScrollbarThickness, trackHeight);

            _painter.DrawVerticalScrollbar(
                trackRect,
                box.ScrollTop,
                box.ScrollableContentHeight,
                trackHeight);
        }

        if (hasHScrollbar)
        {
            float trackY = paddingBox.Bottom - LayoutBox.ScrollbarThickness;
            float trackWidth = paddingBox.Width - (hasVScrollbar ? LayoutBox.ScrollbarThickness : 0);
            var trackRect = new RectF(paddingBox.X, trackY, trackWidth, LayoutBox.ScrollbarThickness);

            _painter.DrawHorizontalScrollbar(
                trackRect,
                box.ScrollLeft,
                box.ScrollableContentWidth,
                trackWidth);
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

        bool hasVisibleBorder =
            style.ComputedBorderTop.IsVisible ||
            style.ComputedBorderRight.IsVisible ||
            style.ComputedBorderBottom.IsVisible ||
            style.ComputedBorderLeft.IsVisible;

        if (!hasVisibleBorder) return;

        _painter.DrawBorderSides(
            box.BoxModel.BorderBox,
            style.ComputedBorderTop,
            style.ComputedBorderRight,
            style.ComputedBorderBottom,
            style.ComputedBorderLeft,
            style.BorderTopLeftRadius.Value,
            style.BorderTopRightRadius.Value,
            style.BorderBottomRightRadius.Value,
            style.BorderBottomLeftRadius.Value
        );
    }

    /// <summary>
    /// 渲染内容
    /// </summary>
    private void RenderContent(LayoutBox box)
    {
        if (_painter == null) return;

        var element = box.Element;

        // 渲染输入框
        if (element is InputElement inputElement)
        {
            RenderInputElement(box, inputElement);
            return;
        }

        // 渲染下拉选择框
        if (element is SelectElement selectElement)
        {
            RenderSelectElement(box, selectElement);
            return;
        }

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

    /// <summary>
    /// 渲染输入框元素
    /// </summary>
    private void RenderInputElement(LayoutBox box, InputElement inputElement)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var contentRect = box.BoxModel.Content;

        switch (inputElement.Type)
        {
            case InputType.Checkbox:
                _painter.DrawCheckbox(
                    contentRect,
                    inputElement.Checked,
                    style.BorderTopColor,
                    style.Color,
                    style.BackgroundColor
                );
                break;

            case InputType.Radio:
                _painter.DrawRadio(
                    contentRect,
                    inputElement.Checked,
                    style.BorderTopColor,
                    style.Color,
                    style.BackgroundColor
                );
                break;

            case InputType.Range:
                _painter.DrawRange(
                    contentRect,
                    inputElement.NumericValue,
                    inputElement.Min,
                    inputElement.Max,
                    Color.LightGray,
                    style.Color,
                    style.Color
                );
                break;

            case InputType.Password:
                if (!string.IsNullOrEmpty(inputElement.Value))
                {
                    _painter.DrawPasswordText(
                        inputElement.Value.Length,
                        contentRect,
                        style.Color,
                        style.FontSize.Value
                    );
                }
                else if (!string.IsNullOrEmpty(inputElement.Placeholder))
                {
                    _painter.DrawText(
                        inputElement.Placeholder,
                        contentRect,
                        Color.Gray,
                        style.FontFamily,
                        style.FontSize.Value,
                        style.FontWeight,
                        TextAlign.Left
                    );
                }
                break;

            case InputType.Text:
            default:
                if (!string.IsNullOrEmpty(inputElement.Value))
                {
                    _painter.DrawText(
                        inputElement.Value,
                        contentRect,
                        style.Color,
                        style.FontFamily,
                        style.FontSize.Value,
                        style.FontWeight,
                        TextAlign.Left
                    );
                }
                else if (!string.IsNullOrEmpty(inputElement.Placeholder))
                {
                    _painter.DrawText(
                        inputElement.Placeholder,
                        contentRect,
                        Color.Gray,
                        style.FontFamily,
                        style.FontSize.Value,
                        style.FontWeight,
                        TextAlign.Left
                    );
                }
                break;
        }
    }

    /// <summary>
    /// 渲染下拉选择框元素
    /// </summary>
    private void RenderSelectElement(LayoutBox box, SelectElement selectElement)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var borderBox = box.BoxModel.BorderBox;

        _painter.DrawSelect(
            borderBox,
            selectElement.GetDisplayText(),
            selectElement.IsOpen,
            style.BorderTopColor,
            style.BackgroundColor,
            style.Color,
            Color.Gray,
            style.FontSize.Value
        );

        if (selectElement.IsOpen)
        {
            RenderSelectDropdown(box, selectElement);
        }
    }

    /// <summary>
    /// 渲染下拉选项列表
    /// </summary>
    private void RenderSelectDropdown(LayoutBox box, SelectElement selectElement)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var borderBox = box.BoxModel.BorderBox;

        var options = new List<(string text, bool isSelected, bool isDisabled, bool isGroupLabel)>();
        var allOptions = selectElement.GetAllOptions();
        int optionIndex = 0;

        foreach (var child in selectElement.Children)
        {
            if (child is OptGroupElement optGroup)
            {
                options.Add((optGroup.Label ?? string.Empty, false, false, true));

                foreach (var groupChild in optGroup.Children)
                {
                    if (groupChild is OptionElement option)
                    {
                        bool isSelected = optionIndex == selectElement.SelectedIndex || option.Selected;
                        bool isDisabled = option.IsDisabled;
                        options.Add((option.TextContent ?? option.Value ?? string.Empty, isSelected, isDisabled, false));
                        optionIndex++;
                    }
                }
            }
            else if (child is OptionElement option)
            {
                bool isSelected = optionIndex == selectElement.SelectedIndex || option.Selected;
                bool isDisabled = option.IsDisabled;
                options.Add((option.TextContent ?? option.Value ?? string.Empty, isSelected, isDisabled, false));
                optionIndex++;
            }
        }

        float optionHeight = style.FontSize.Value + 8;
        float dropdownHeight = options.Count * optionHeight;
        var dropdownRect = new RectF(
            borderBox.Left,
            borderBox.Bottom,
            borderBox.Width,
            dropdownHeight
        );

        _painter.DrawSelectDropdown(
            dropdownRect,
            options,
            Color.White,
            style.BorderTopColor,
            style.Color,
            new Color(0, 120, 215),
            Color.White,
            Color.Gray,
            Color.Gray,
            style.FontSize.Value
        );
    }
}
