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
        // 元素自身字体大小（px），用于解析长度中的 em 分量。
        float fs = style.FontSize.Value;

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

        // 2. 创建行盒子（line box）并水平排列行内元素
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;

        // 当元素同时拥有 TextContent 和子元素时，文本作为匿名 inline 盒
        // 排在子元素之前，子元素从文本宽度之后开始排列（与 BlockLayout 保持一致）。
        float ownTextWidth = 0;
        float ownTextHeight = 0;
        bool hasOwnText = !string.IsNullOrEmpty(box.Element.TextContent);

        if (hasOwnText)
        {
            // 行高优先使用显式 line-height（如 1.5 × 字体大小），否则取字体自然度量。
            float resolvedLineHeight = BlockLayout.ResolveLineHeight(style);

            // 如果元素有明确的宽度约束，使用多行测量（支持自动换行）
            if (constraints.AvailableWidth.HasValue && constraints.AvailableWidth.Value > 0)
            {
                // 减去 padding 和 border 得到可用内容宽度
                float availableContentWidth = constraints.AvailableWidth.Value
                    - box.BoxModel.Padding.Horizontal
                    - box.BoxModel.Border.Horizontal;

                var (wrappedWidth, wrappedHeight) = TextMeasurer.MeasureTextWithWrap(
                    box.Element.TextContent,
                    style.FontFamily,
                    style.FontSize.Value,
                    style.FontWeight,
                    availableContentWidth,
                    resolvedLineHeight,
                    style.WhiteSpace);

                ownTextWidth = wrappedWidth;
                ownTextHeight = wrappedHeight;
            }
            else
            {
                // 没有宽度约束，使用单行测量
                var (textWidth, _) = TextMeasurer.MeasureText(
                    box.Element.TextContent,
                    style.FontFamily,
                    style.FontSize.Value,
                    style.FontWeight);
                ownTextWidth = textWidth;
                ownTextHeight = resolvedLineHeight;
            }
        }

        float currentX = contentX + ownTextWidth;
        float lineHeight = ownTextHeight;
        float maxWidth = ownTextWidth;

        // 简化实现：单行布局，不处理换行
        foreach (var child in box.Children)
        {
            // 脱离文档流的子元素仍布局以获得尺寸，但不推进行光标、不计入内容宽度。
            // 最终位置由 LayoutEngine 的定位阶段修正。
            if (BlockLayout.IsOutOfFlow(child))
            {
                var outOfFlowConstraints = new LayoutConstraints(null, null);
                LayoutChild(child, outOfFlowConstraints, currentX, contentY);
                continue;
            }

            var childConstraints = new LayoutConstraints(null, null);
            LayoutChild(child, childConstraints, currentX, contentY);

            currentX = child.BoxModel.MarginBox.Right;
            lineHeight = Math.Max(lineHeight, child.BoxModel.MarginBox.Height);
            maxWidth = currentX - contentX;
        }

        // 3. 计算内容区域
        float contentWidth;
        float contentHeight;

        // replaced 元素（video）的内禀尺寸，用于 inline replaced 的盒尺寸（与 <img> 行为对齐）。
        var (intrinsicW, intrinsicH) = BlockLayout.GetReplacedIntrinsicSize(box.Element);
        bool isReplaced = intrinsicW > 0 && intrinsicH > 0;

        // 百分比针对"不确定尺寸"的包含块解析时按 auto 处理（CSS 规范）：
        // inline/inline-block 的子元素以 null 约束布局（无确定包含块尺寸），此时
        // width/height:100% 会解析为 0 而塌缩。若父尺寸不确定，退化为内容尺寸（见 ISSUE-077）。
        bool widthPercentAgainstIndefinite = style.Width.HasPercentComponent && !constraints.AvailableWidth.HasValue;
        bool heightPercentAgainstIndefinite = style.Height.HasPercentComponent && !constraints.AvailableHeight.HasValue;
        bool widthIsAuto = style.Width.IsAuto || widthPercentAgainstIndefinite;
        bool heightIsAuto = style.Height.IsAuto || heightPercentAgainstIndefinite;

        if (!widthIsAuto)
        {
            contentWidth = style.Width.ToPixels(containerWidth, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
            {
                contentWidth -= box.BoxModel.Border.Horizontal + box.BoxModel.Padding.Horizontal;
                contentWidth = Math.Max(0, contentWidth);
            }
        }
        else if (isReplaced)
        {
            // width auto：height 指定时按纵横比反推宽，否则用内禀宽。
            if (heightIsAuto)
            {
                contentWidth = intrinsicW;
            }
            else
            {
                float h = style.Height.ToPixels(constraints.AvailableHeight ?? 0, fs);
                if (style.BoxSizing == BoxSizing.BorderBox)
                    h = Math.Max(0, h - box.BoxModel.Border.Vertical - box.BoxModel.Padding.Vertical);
                contentWidth = h * intrinsicW / intrinsicH;
            }
        }
        else if (box.Children.Count == 0 && hasOwnText)
        {
            // 只有文本内容、没有子元素：宽度即文本宽度
            contentWidth = ownTextWidth;
        }
        else
        {
            // 有子元素（可能同时有文本）：maxWidth 已包含文本宽度作为起始偏移
            contentWidth = maxWidth;
        }

        if (!heightIsAuto)
        {
            contentHeight = style.Height.ToPixels(constraints.AvailableHeight ?? 0, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
            {
                contentHeight -= box.BoxModel.Border.Vertical + box.BoxModel.Padding.Vertical;
                contentHeight = Math.Max(0, contentHeight);
            }
        }
        else if (isReplaced)
        {
            // height auto：width 指定时按纵横比反推高，否则用内禀高。
            contentHeight = widthIsAuto
                ? intrinsicH
                : contentWidth * intrinsicH / intrinsicW;
        }
        else if (BlockLayout.GetTextFormControlContentHeight(box) is float formControlHeight)
        {
            // 单行文本表单控件（input、select）：以一行文本（行高/字体度量）撑起内容高度。
            // select 的 option 子元素由下拉层叠加渲染，不计入闭合态高度，因此即便存在子元素
            // 也优先采用单行高度，避免被 option 撑高或塌缩为 0（参见 ISSUE-040）。
            contentHeight = formControlHeight;
        }
        else if (box.Children.Count == 0 && hasOwnText)
        {
            // 只有文本内容、没有子元素：高度即文本高度
            contentHeight = ownTextHeight;
        }
        else
        {
            // 有子元素时，lineHeight 已取文本高度与子元素高度的较大值
            contentHeight = lineHeight;
        }

        box.BoxModel.Content = new RectF(contentX, contentY, contentWidth, contentHeight);
    }

    private void LayoutChild(LayoutBox child, LayoutConstraints constraints, float x, float y)
    {
        LayoutDispatcher.Dispatch(child, constraints, x, y);
    }
}
