using Miko.Common;
using Miko.Utils;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// 块级布局算法
/// </summary>
public class BlockLayout
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

        // 2. 计算 width
        float contentWidth;
        if (!style.Width.IsAuto)
        {
            contentWidth = style.Width.ToPixels(containerWidth);
        }
        else if (constraints.IsInfiniteWidth || containerWidth <= 0)
        {
            // 当没有可用宽度约束时（如在 flex row 容器中），根据内容计算宽度
            if (box.Children.Count == 0 && !string.IsNullOrEmpty(box.Element.TextContent))
            {
                // 根据文本内容计算宽度
                var (textWidth, _) = TextMeasurer.MeasureText(
                    box.Element.TextContent,
                    style.FontFamily,
                    style.FontSize.Value,
                    style.FontWeight);
                contentWidth = textWidth;
            }
            else
            {
                // 没有内容时宽度为 0
                contentWidth = 0;
            }
        }
        else
        {
            // 块级元素默认占满可用宽度
            float availableWidth = containerWidth - box.BoxModel.Margin.Horizontal;
            contentWidth = availableWidth - box.BoxModel.Border.Horizontal - box.BoxModel.Padding.Horizontal;
        }

        // 应用 min/max 约束
        if (!style.MinWidth.IsAuto)
        {
            contentWidth = Math.Max(contentWidth, style.MinWidth.ToPixels(containerWidth));
        }
        if (!style.MaxWidth.IsAuto)
        {
            contentWidth = Math.Min(contentWidth, style.MaxWidth.ToPixels(containerWidth));
        }

        // 3. 计算内容区域的位置
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;

        // 4. 布局子元素
        // Block 子元素垂直堆叠，Inline/InlineBlock 子元素水平排列
        float currentY = contentY;
        float maxChildWidth = 0;

        int i = 0;
        while (i < box.Children.Count)
        {
            var child = box.Children[i];

            if (IsInlineOrInlineBlock(child))
            {
                // 收集连续的 Inline/InlineBlock 子元素并水平布局
                float lineX = contentX;
                float lineHeight = 0;

                while (i < box.Children.Count && IsInlineOrInlineBlock(box.Children[i]))
                {
                    var inlineChild = box.Children[i];
                    var childConstraints = new LayoutConstraints(null, null);
                    LayoutChild(inlineChild, childConstraints, lineX, currentY);

                    lineX = inlineChild.BoxModel.MarginBox.Right;
                    lineHeight = Math.Max(lineHeight, inlineChild.BoxModel.MarginBox.Height);
                    maxChildWidth = Math.Max(maxChildWidth, lineX - contentX);
                    i++;
                }

                // 移动到下一行
                currentY += lineHeight;
            }
            else
            {
                // Block 子元素垂直堆叠
                var childConstraints = new LayoutConstraints(contentWidth, null);
                LayoutChild(child, childConstraints, contentX, currentY);

                currentY = child.BoxModel.MarginBox.Bottom;
                maxChildWidth = Math.Max(maxChildWidth, child.BoxModel.MarginBox.Width);
                i++;
            }
        }

        // 5. 计算 height
        float contentHeight;
        if (!style.Height.IsAuto)
        {
            contentHeight = style.Height.ToPixels(constraints.AvailableHeight ?? 0);
        }
        else
        {
            // 高度由内容决定
            float childrenHeight = currentY - contentY;

            // 如果没有子元素但有文本内容，则根据文本计算高度
            if (box.Children.Count == 0 && !string.IsNullOrEmpty(box.Element.TextContent))
            {
                var (_, textHeight) = TextMeasurer.MeasureText(
                    box.Element.TextContent,
                    style.FontFamily,
                    style.FontSize.Value,
                    style.FontWeight);
                contentHeight = textHeight;
            }
            else
            {
                contentHeight = childrenHeight;
            }
        }

        // 应用 min/max 约束
        if (!style.MinHeight.IsAuto)
        {
            contentHeight = Math.Max(contentHeight, style.MinHeight.ToPixels(constraints.AvailableHeight ?? 0));
        }
        if (!style.MaxHeight.IsAuto)
        {
            contentHeight = Math.Min(contentHeight, style.MaxHeight.ToPixels(constraints.AvailableHeight ?? 0));
        }

        // 6. 设置内容区域
        box.BoxModel.Content = new RectF(contentX, contentY, contentWidth, contentHeight);
    }

    private void LayoutChild(LayoutBox child, LayoutConstraints constraints, float x, float y)
    {
        LayoutDispatcher.Dispatch(child, constraints, x, y);
    }

    private static bool IsInlineOrInlineBlock(LayoutBox child)
    {
        return child.Type == LayoutType.Inline || child.Type == LayoutType.InlineBlock;
    }
}
