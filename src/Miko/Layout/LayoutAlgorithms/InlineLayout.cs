using Miko.Common;
using Miko.Utils;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// 行内布局算法
/// </summary>
public class InlineLayout
{
    public void Layout(LayoutBox box, LayoutConstraints constraints, float x, float y)
    {
        var style = box.ComputedStyle;

        // 1. 计算 margin, border, padding
        float containerWidth = constraints.AvailableWidth ?? 0;

        box.BoxModel.Margin = new EdgeSizes(
            style.MarginTop.ToPixels(containerWidth),
            style.MarginRight.ToPixels(containerWidth),
            style.MarginBottom.ToPixels(containerWidth),
            style.MarginLeft.ToPixels(containerWidth)
        );

        box.BoxModel.Border = new EdgeSizes(
            style.BorderTopWidth.ToPixels(containerWidth),
            style.BorderRightWidth.ToPixels(containerWidth),
            style.BorderBottomWidth.ToPixels(containerWidth),
            style.BorderLeftWidth.ToPixels(containerWidth)
        );

        box.BoxModel.Padding = new EdgeSizes(
            style.PaddingTop.ToPixels(containerWidth),
            style.PaddingRight.ToPixels(containerWidth),
            style.PaddingBottom.ToPixels(containerWidth),
            style.PaddingLeft.ToPixels(containerWidth)
        );

        // 2. 创建行盒子（line box）并水平排列行内元素
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;

        float currentX = contentX;
        float lineHeight = 0;
        float maxWidth = 0;

        // 简化实现：单行布局，不处理换行
        foreach (var child in box.Children)
        {
            var childConstraints = new LayoutConstraints(null, null);
            LayoutChild(child, childConstraints, currentX, contentY);

            currentX = child.BoxModel.MarginBox.Right;
            lineHeight = Math.Max(lineHeight, child.BoxModel.MarginBox.Height);
            maxWidth = currentX - contentX;
        }

        // 3. 计算内容区域
        float contentWidth;
        float contentHeight;

        if (!style.Width.IsAuto)
        {
            contentWidth = style.Width.ToPixels(containerWidth);
        }
        else if (box.Children.Count == 0 && !string.IsNullOrEmpty(box.Element.TextContent))
        {
            // 如果没有子元素但有文本内容，则根据文本计算宽度
            var (textWidth, _) = TextMeasurer.MeasureText(
                box.Element.TextContent,
                style.FontFamily,
                style.FontSize.Value,
                style.FontWeight);
            contentWidth = textWidth;
        }
        else
        {
            contentWidth = maxWidth;
        }

        if (!style.Height.IsAuto)
        {
            contentHeight = style.Height.ToPixels(constraints.AvailableHeight ?? 0);
        }
        else if (box.Children.Count == 0 && !string.IsNullOrEmpty(box.Element.TextContent))
        {
            // 如果没有子元素但有文本内容，则根据文本计算高度
            var (_, textHeight) = TextMeasurer.MeasureText(
                box.Element.TextContent,
                style.FontFamily,
                style.FontSize.Value,
                style.FontWeight);
            contentHeight = textHeight;
        }
        else
        {
            contentHeight = lineHeight;
        }

        box.BoxModel.Content = new RectF(contentX, contentY, contentWidth, contentHeight);
    }

    private void LayoutChild(LayoutBox child, LayoutConstraints constraints, float x, float y)
    {
        LayoutDispatcher.Dispatch(child, constraints, x, y);
    }
}
