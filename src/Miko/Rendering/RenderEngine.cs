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
            // 下拉框关闭时，不渲染 OptionElement 子元素
            return;
        }

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

        // 检查是否有任何可见边框
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
                // 绘制密码圆点
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
                    // 绘制占位符
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
                // 绘制文本或占位符
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

        // 绘制select控件（不绘制背景和边框，因为RenderBackground和RenderBorder已经处理）
        _painter.DrawSelect(
            borderBox,
            selectElement.GetDisplayText(),
            selectElement.IsOpen,
            style.BorderTopColor,
            style.BackgroundColor,
            style.Color,
            Color.Gray,  // Arrow color
            style.FontSize.Value
        );

        // 如果下拉框展开，绘制选项列表
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

        // 构建选项列表
        var options = new List<(string text, bool isSelected, bool isDisabled, bool isGroupLabel)>();
        var allOptions = selectElement.GetAllOptions();
        int optionIndex = 0;

        foreach (var child in selectElement.Children)
        {
            if (child is OptGroupElement optGroup)
            {
                // 添加分组标签
                options.Add((optGroup.Label ?? string.Empty, false, false, true));

                // 添加分组中的选项
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

        // 计算下拉列表位置和大小
        float optionHeight = style.FontSize.Value + 8;
        float dropdownHeight = options.Count * optionHeight;
        var dropdownRect = new RectF(
            borderBox.Left,
            borderBox.Bottom,
            borderBox.Width,
            dropdownHeight
        );

        // 绘制下拉列表
        _painter.DrawSelectDropdown(
            dropdownRect,
            options,
            Color.White,           // Background
            style.BorderTopColor,  // Border
            style.Color,           // Text
            new Color(0, 120, 215),  // Selected background (blue)
            Color.White,           // Selected text
            Color.Gray,            // Disabled text
            Color.Gray,            // Group label color
            style.FontSize.Value
        );
    }
}
