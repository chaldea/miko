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
        // 元素自身字体大小（px），用于解析长度中的 em 分量。
        float fs = style.FontSize.Value;

        bool marginLeftAuto = style.MarginLeft.IsAuto;
        bool marginRightAuto = style.MarginRight.IsAuto;

        box.BoxModel.Margin = new EdgeSizes(
            style.MarginTop.ToPixels(containerWidth, fs),
            style.MarginRight.ToPixels(containerWidth, fs),
            style.MarginBottom.ToPixels(containerWidth, fs),
            style.MarginLeft.ToPixels(containerWidth, fs)
        );

        box.BoxModel.Border = new EdgeSizes(
            style.BorderTopWidth.ToPixels(containerWidth, fs),
            style.BorderRightWidth.ToPixels(containerWidth, fs),
            style.BorderBottomWidth.ToPixels(containerWidth, fs),
            style.BorderLeftWidth.ToPixels(containerWidth, fs)
        );

        box.BoxModel.Padding = new EdgeSizes(
            style.PaddingTop.ToPixels(containerWidth, fs),
            style.PaddingRight.ToPixels(containerWidth, fs),
            style.PaddingBottom.ToPixels(containerWidth, fs),
            style.PaddingLeft.ToPixels(containerWidth, fs)
        );

        // 2. 计算 width
        float contentWidth;
        if (!style.Width.IsAuto)
        {
            contentWidth = style.Width.ToPixels(containerWidth, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
            {
                contentWidth -= box.BoxModel.Border.Horizontal + box.BoxModel.Padding.Horizontal;
                contentWidth = Math.Max(0, contentWidth);
            }
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
        // border-box 下，min/max-width 约束的是 border-box 宽度，需先扣除水平 padding+border
        // 再与内容宽度比较（与上面 Width 的 border-box 处理保持一致）。
        bool isBorderBoxW = style.BoxSizing == BoxSizing.BorderBox;
        float horizontalExtra = box.BoxModel.Border.Horizontal + box.BoxModel.Padding.Horizontal;
        if (!style.MinWidth.IsAuto)
        {
            float min = style.MinWidth.ToPixels(containerWidth, fs);
            if (isBorderBoxW) min = Math.Max(0, min - horizontalExtra);
            contentWidth = Math.Max(contentWidth, min);
        }
        if (!style.MaxWidth.IsAuto)
        {
            float max = style.MaxWidth.ToPixels(containerWidth, fs);
            if (isBorderBoxW) max = Math.Max(0, max - horizontalExtra);
            contentWidth = Math.Min(contentWidth, max);
        }

        // 解析 margin auto：当元素有明确宽度时，auto margin 占据剩余空间
        if ((marginLeftAuto || marginRightAuto) && !style.Width.IsAuto && containerWidth > 0)
        {
            float usedWidth = contentWidth + box.BoxModel.Border.Horizontal + box.BoxModel.Padding.Horizontal;
            float remainingSpace = Math.Max(0, containerWidth - usedWidth
                - (marginLeftAuto ? 0 : box.BoxModel.Margin.Left)
                - (marginRightAuto ? 0 : box.BoxModel.Margin.Right));

            float newMarginLeft = box.BoxModel.Margin.Left;
            float newMarginRight = box.BoxModel.Margin.Right;

            if (marginLeftAuto && marginRightAuto)
            {
                newMarginLeft = remainingSpace / 2f;
                newMarginRight = remainingSpace / 2f;
            }
            else if (marginLeftAuto)
            {
                newMarginLeft = remainingSpace;
            }
            else
            {
                newMarginRight = remainingSpace;
            }

            box.BoxModel.Margin = new EdgeSizes(
                box.BoxModel.Margin.Top,
                newMarginRight,
                box.BoxModel.Margin.Bottom,
                newMarginLeft
            );
        }

        // 判断是否需要为滚动条预留空间（Classic 模式占用布局）
        bool needsVerticalScrollbar = style.OverflowY == Overflow.Scroll;
        float scrollbarReservedWidth = needsVerticalScrollbar ? LayoutBox.ScrollbarThickness : 0;
        float childAvailableWidth = contentWidth - scrollbarReservedWidth;

        // 3. 计算内容区域的位置
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;

        // 计算确定性高度，用于子元素百分比高度解析
        float? childAvailableHeight = null;
        if (!style.Height.IsAuto)
        {
            float h = style.Height.ToPixels(constraints.AvailableHeight ?? 0, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
                h -= box.BoxModel.Border.Vertical + box.BoxModel.Padding.Vertical;
            childAvailableHeight = Math.Max(0, h);
        }
        else if (constraints.AvailableHeight.HasValue &&
                 (style.OverflowY == Overflow.Auto || style.OverflowY == Overflow.Scroll || style.OverflowY == Overflow.Hidden))
        {
            float h = constraints.AvailableHeight.Value
                - box.BoxModel.Margin.Vertical
                - box.BoxModel.Border.Vertical
                - box.BoxModel.Padding.Vertical;
            childAvailableHeight = Math.Max(0, h);
        }

        // 4. 布局子元素
        // Block 子元素垂直堆叠，Inline/InlineBlock 子元素水平排列
        float currentY = contentY;
        float maxChildWidth = 0;

        // 当元素同时拥有 TextContent 和子元素时：
        // - 如果第一个子元素是 block，文本作为独立行盒占据顶部，block 子元素从下方开始
        // - 如果第一个子元素是 inline/inline-block，文本作为匿名 inline 盒与 inline 子元素同行排列
        float ownTextWidth = 0;
        float ownTextHeight = 0;
        bool hasOwnText = box.Children.Count > 0 && !string.IsNullOrEmpty(box.Element.TextContent);
        bool firstChildIsBlock = hasOwnText && !IsInlineOrInlineBlock(box.Children[0]);

        if (hasOwnText)
        {
            var (textWidth, textHeight) = TextMeasurer.MeasureText(
                box.Element.TextContent,
                style.FontFamily,
                style.FontSize.Value,
                style.FontWeight);
            ownTextWidth = textWidth;
            ownTextHeight = textHeight;

            // 仅当第一个子元素是 block 时，文本独占一行，currentY 下移
            if (firstChildIsBlock)
            {
                currentY += ownTextHeight;
                maxChildWidth = Math.Max(maxChildWidth, ownTextWidth);
            }
        }

        int i = 0;
        while (i < box.Children.Count)
        {
            var child = box.Children[i];

            // 脱离文档流的子元素（absolute/fixed）不参与常规流：
            // 仍进行布局以获得尺寸，但不推进流光标、不计入父元素内容尺寸。
            // 最终位置由 LayoutEngine 的定位阶段修正。
            if (IsOutOfFlow(child))
            {
                var childConstraints = new LayoutConstraints(childAvailableWidth, childAvailableHeight);
                LayoutChild(child, childConstraints, contentX, currentY);
                i++;
                continue;
            }

            if (IsInlineOrInlineBlock(child))
            {
                // 收集连续的 Inline/InlineBlock 子元素并水平布局
                // 如果这是第一组 inline 子元素且父元素有文本内容，文本宽度作为起始 X
                float lineX = contentX;
                if (i == 0 && hasOwnText && !firstChildIsBlock)
                {
                    lineX += ownTextWidth;
                }

                float lineHeight = hasOwnText && i == 0 && !firstChildIsBlock ? ownTextHeight : 0;

                while (i < box.Children.Count && IsInlineOrInlineBlock(box.Children[i])
                       && !IsOutOfFlow(box.Children[i]))
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
                var childConstraints = new LayoutConstraints(childAvailableWidth, childAvailableHeight);
                LayoutChild(child, childConstraints, contentX, currentY);

                currentY = child.BoxModel.MarginBox.Bottom;
                maxChildWidth = Math.Max(maxChildWidth, child.BoxModel.MarginBox.Width);
                i++;
            }
        }

        // 记录子元素实际占用的总高度
        float childrenTotalHeight = currentY - contentY;

        // 5. 计算 height
        float contentHeight;
        if (!style.Height.IsAuto)
        {
            contentHeight = style.Height.ToPixels(constraints.AvailableHeight ?? 0, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
            {
                contentHeight -= box.BoxModel.Border.Vertical + box.BoxModel.Padding.Vertical;
                contentHeight = Math.Max(0, contentHeight);
            }
        }
        else if (constraints.AvailableHeight.HasValue &&
                 (style.OverflowY == Overflow.Auto || style.OverflowY == Overflow.Scroll || style.OverflowY == Overflow.Hidden))
        {
            // 当有可用高度约束（如来自 flex 容器）且设置了 overflow 时，使用约束高度
            contentHeight = constraints.AvailableHeight.Value
                - box.BoxModel.Margin.Vertical
                - box.BoxModel.Border.Vertical
                - box.BoxModel.Padding.Vertical;
            contentHeight = Math.Max(0, contentHeight);
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
            // 文本类表单控件（input）无内容时，以一行文本（行高/字体度量）撑起内容高度，
            // 而非塌缩为 0（参见 ISSUE-040）。
            else if (box.Children.Count == 0 && GetTextFormControlContentHeight(box) is float formControlHeight)
            {
                contentHeight = formControlHeight;
            }
            else
            {
                contentHeight = childrenHeight;
            }
        }

        // 应用 min/max 约束
        // border-box 下，min/max-height 约束的是 border-box 高度，需先扣除垂直 padding+border
        // 再与内容高度比较（与上面 Height 的 border-box 处理保持一致，参见 ISSUE-040 后续）。
        bool isBorderBoxH = style.BoxSizing == BoxSizing.BorderBox;
        float verticalExtra = box.BoxModel.Border.Vertical + box.BoxModel.Padding.Vertical;
        if (!style.MinHeight.IsAuto)
        {
            float min = style.MinHeight.ToPixels(constraints.AvailableHeight ?? 0, fs);
            if (isBorderBoxH) min = Math.Max(0, min - verticalExtra);
            contentHeight = Math.Max(contentHeight, min);
        }
        if (!style.MaxHeight.IsAuto)
        {
            float max = style.MaxHeight.ToPixels(constraints.AvailableHeight ?? 0, fs);
            if (isBorderBoxH) max = Math.Max(0, max - verticalExtra);
            contentHeight = Math.Min(contentHeight, max);
        }

        // 6. 设置内容区域
        box.BoxModel.Content = new RectF(contentX, contentY, contentWidth, contentHeight);

        // 7. 记录可滚动内容尺寸
        // 滚动区域（scrollHeight/scrollWidth）应包含盒子的内边距：
        // CSS 中可滚动区域是内容延伸范围加上 padding box 的内边距，
        // 否则滚动到底部时无法看到内容底部的 bottom padding（以及内容的下边缘）。
        box.ScrollableContentWidth = maxChildWidth + box.BoxModel.Padding.Horizontal;
        box.ScrollableContentHeight = childrenTotalHeight + box.BoxModel.Padding.Vertical;

        // 8. 如果 overflow-y 是 auto 且内容溢出，需要为滚动条预留空间并重新布局
        if (style.OverflowY == Overflow.Auto && !needsVerticalScrollbar &&
            childrenTotalHeight > contentHeight && contentHeight > 0)
        {
            scrollbarReservedWidth = LayoutBox.ScrollbarThickness;
            childAvailableWidth = contentWidth - scrollbarReservedWidth;
            RelayoutChildren(box, childAvailableWidth, contentX, contentY);
        }
    }

    private void RelayoutChildren(LayoutBox box, float childAvailableWidth, float contentX, float contentY)
    {
        var style = box.ComputedStyle;
        float currentY = contentY;
        float maxChildWidth = 0;

        // 与主布局逻辑保持一致地处理 TextContent 与子元素共存的情况
        float ownTextWidth = 0;
        float ownTextHeight = 0;
        bool hasOwnText = box.Children.Count > 0 && !string.IsNullOrEmpty(box.Element.TextContent);
        bool firstChildIsBlock = hasOwnText && !IsInlineOrInlineBlock(box.Children[0]);

        if (hasOwnText)
        {
            var (textWidth, textHeight) = TextMeasurer.MeasureText(
                box.Element.TextContent,
                style.FontFamily,
                style.FontSize.Value,
                style.FontWeight);
            ownTextWidth = textWidth;
            ownTextHeight = textHeight;

            if (firstChildIsBlock)
            {
                currentY += ownTextHeight;
                maxChildWidth = Math.Max(maxChildWidth, ownTextWidth);
            }
        }

        int i = 0;
        while (i < box.Children.Count)
        {
            var child = box.Children[i];

            // 脱离文档流的子元素不参与常规流（见主布局逻辑说明）
            if (IsOutOfFlow(child))
            {
                var childConstraints = new LayoutConstraints(childAvailableWidth, null);
                LayoutChild(child, childConstraints, contentX, currentY);
                i++;
                continue;
            }

            if (IsInlineOrInlineBlock(child))
            {
                float lineX = contentX;
                if (i == 0 && hasOwnText && !firstChildIsBlock)
                {
                    lineX += ownTextWidth;
                }

                float lineHeight = hasOwnText && i == 0 && !firstChildIsBlock ? ownTextHeight : 0;

                while (i < box.Children.Count && IsInlineOrInlineBlock(box.Children[i])
                       && !IsOutOfFlow(box.Children[i]))
                {
                    var inlineChild = box.Children[i];
                    var childConstraints = new LayoutConstraints(null, null);
                    LayoutChild(inlineChild, childConstraints, lineX, currentY);

                    lineX = inlineChild.BoxModel.MarginBox.Right;
                    lineHeight = Math.Max(lineHeight, inlineChild.BoxModel.MarginBox.Height);
                    maxChildWidth = Math.Max(maxChildWidth, lineX - contentX);
                    i++;
                }
                currentY += lineHeight;
            }
            else
            {
                var childConstraints = new LayoutConstraints(childAvailableWidth, null);
                LayoutChild(child, childConstraints, contentX, currentY);

                currentY = child.BoxModel.MarginBox.Bottom;
                maxChildWidth = Math.Max(maxChildWidth, child.BoxModel.MarginBox.Width);
                i++;
            }
        }

        box.ScrollableContentWidth = maxChildWidth + box.BoxModel.Padding.Horizontal;
        box.ScrollableContentHeight = (currentY - contentY) + box.BoxModel.Padding.Vertical;
    }

    private void LayoutChild(LayoutBox child, LayoutConstraints constraints, float x, float y)
    {
        LayoutDispatcher.Dispatch(child, constraints, x, y);
    }

    private static bool IsInlineOrInlineBlock(LayoutBox child)
    {
        return child.Type == LayoutType.Inline || child.Type == LayoutType.InlineBlock;
    }

    /// <summary>
    /// 子元素是否脱离常规文档流（absolute / fixed 定位）。
    /// 脱离文档流的元素不占据常规流空间，也不计入父元素的内容尺寸。
    /// </summary>
    internal static bool IsOutOfFlow(LayoutBox child)
    {
        var position = child.ComputedStyle.Position;
        return position == Common.Position.Absolute || position == Common.Position.Fixed;
    }

    /// <summary>
    /// 文本类表单控件（如 input[text/password]）在没有子元素、自动高度时，
    /// 其内容高度应由一行文本占据：优先取显式行高（line-height），否则取字体度量高度。
    /// 返回 null 表示该盒子不适用此规则（应回退到常规的内容高度计算）。
    /// </summary>
    internal static float? GetTextFormControlContentHeight(LayoutBox box)
    {
        if (box.Element is not Miko.Core.DomElements.InputElement)
            return null;

        var style = box.ComputedStyle;

        // 显式设置了行高（>0 表示非 normal），直接作为单行内容高度。
        // line-height 可能是 rem/em，按元素自身字体大小解析其中的 em 分量。
        if (!style.LineHeight.IsAuto && style.LineHeight.Value > 0)
            return style.LineHeight.ToPixels(0, style.FontSize.Value);

        // 否则按字体度量得到一行文本的自然高度
        return TextMeasurer.MeasureTextHeight(
            style.FontFamily,
            style.FontSize.Value,
            style.FontWeight);
    }
}
