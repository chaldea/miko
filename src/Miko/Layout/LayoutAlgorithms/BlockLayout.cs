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

        // replaced 元素（video）的内禀尺寸：用于 width/height 为 auto 时回退，
        // 并支持只给一边时按内禀纵横比补全另一边（与 <img> 行为对齐）。
        var (intrinsicW, intrinsicH) = GetReplacedIntrinsicSize(box.Element);
        bool isReplaced = intrinsicW > 0 && intrinsicH > 0;

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
        else if (isReplaced)
        {
            // width auto 的 replaced 元素：
            // - height 也 auto 时用内禀宽；
            // - height 指定时按纵横比反推宽（保持 video 不变形）。
            if (style.Height.IsAuto)
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
        else if (constraints.IsInfiniteWidth || containerWidth <= 0)
        {
            // 当没有可用宽度约束时（如在 flex row 容器中），根据内容计算宽度（shrink-to-fit）。
            // textarea 有由 cols 决定的固有宽度；否则以 null 约束预布局在流子元素（含文本节点），
            // 取其行内排列后的最右边界作为内容宽度。
            contentWidth = GetTextFormControlContentWidth(box) ?? MeasureInlineChildrenWidth(box);
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

        // 解析 margin auto：当元素宽度确定且小于容器时，auto margin 占据剩余空间。
        // 宽度确定的两种情形：
        //   1. 显式设置了 width；
        //   2. width 为 auto，但被 max-width 夹取到比“填满容器”更窄（此时 contentWidth
        //      已在上面的 min/max 处理中定型，剩余空间应由 auto margin 吸收，实现居中）。
        // 注意：width auto 且未被 max-width 约束时会填满容器，剩余空间为 0，auto margin 无效果，
        // 因此下面的 remainingSpace 计算天然覆盖这种情况，无需额外分支。
        bool hasDefiniteWidth = !style.Width.IsAuto || !style.MaxWidth.IsAuto;
        if ((marginLeftAuto || marginRightAuto) && hasDefiniteWidth && containerWidth > 0)
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
        // Block 子元素垂直堆叠，Inline/InlineBlock/文本节点 子元素水平排列。
        // 文本节点（TextNode）作为普通行内子盒参与，交错顺序由子节点列表天然表达（见 ISSUE-086），
        // 不再需要把父元素的 TextContent 作为「排在子元素之前的匿名盒」特殊处理。
        float currentY = contentY;
        float maxChildWidth = 0;

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
                // 收集连续的 Inline/InlineBlock/文本节点 子元素并水平布局到同一行。
                float lineX = contentX;
                float lineHeight = 0;

                while (i < box.Children.Count && IsInlineOrInlineBlock(box.Children[i])
                       && !IsOutOfFlow(box.Children[i]))
                {
                    var inlineChild = box.Children[i];

                    // 遇到强制换行标记（br）：结束当前行盒，其后的行内内容排到新的一行。
                    // br 自身不占宽度，但会贡献至少一行的高度（保证空 br 也能换行）。
                    if (IsForcedLineBreak(inlineChild))
                    {
                        var brConstraints = new LayoutConstraints(0, null);
                        LayoutChild(inlineChild, brConstraints, lineX, currentY);
                        lineHeight = Math.Max(lineHeight, ResolveLineHeight(box.ComputedStyle));
                        i++;
                        break;
                    }

                    // 传入父内容宽度作为可用宽度，使 inline-block 子元素的百分比宽度
                    // 能相对包含块解析（auto 宽度仍由内容决定，不受影响）。
                    var childConstraints = new LayoutConstraints(childAvailableWidth, childAvailableHeight);
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
        else if (isReplaced)
        {
            // height auto 的 replaced 元素：按内禀纵横比从最终内容宽度反推高。
            // contentWidth 已应用 min/max-width 夹取，因此 max-width 约束会等比缩放
            // 高度（auto 宽未被夹取时 contentWidth==intrinsicW，结果即内禀高）。见 ISSUE-083。
            contentHeight = contentWidth * intrinsicH / intrinsicW;
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

            // 单行文本表单控件（input、select）：以一行文本（行高/字体度量）撑起内容高度。
            // select 的 option 子元素由下拉层叠加渲染，不计入闭合态高度，因此即便存在子元素
            // 也优先采用单行高度，避免被 option 撑高或塌缩为 0（参见 ISSUE-040）。
            if (GetTextFormControlContentHeight(box) is float formControlHeight)
            {
                contentHeight = formControlHeight;
            }
            else
            {
                // 高度由内容决定。文本节点作为行内子盒已计入 childrenHeight（含换行后的多行高度），
                // 因此无需再对父元素的 TextContent 做单独测量（见 ISSUE-086）。
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

        // 文本节点作为普通行内子盒参与，无需 ownText 特例（见 ISSUE-086）。
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
                float lineHeight = 0;

                while (i < box.Children.Count && IsInlineOrInlineBlock(box.Children[i])
                       && !IsOutOfFlow(box.Children[i]))
                {
                    var inlineChild = box.Children[i];

                    // 强制换行标记（br）：结束当前行盒（见主布局逻辑说明）。
                    if (IsForcedLineBreak(inlineChild))
                    {
                        LayoutChild(inlineChild, new LayoutConstraints(0, null), lineX, currentY);
                        lineHeight = Math.Max(lineHeight, ResolveLineHeight(box.ComputedStyle));
                        i++;
                        break;
                    }

                    // 传入父内容宽度，使 inline-block 子元素的百分比宽度能相对包含块解析。
                    var childConstraints = new LayoutConstraints(childAvailableWidth, null);
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

    /// <summary>
    /// 计算 shrink-to-fit 内容宽度（无宽度约束时，如 flex row 项）：以 null 约束预布局在流子元素
    /// （含文本节点），行内子盒累加宽度、块级子盒取最大宽度，返回内容最大宽度。子盒随后会在确定
    /// 宽度后重排，故此处仅用于测量。
    /// </summary>
    private float MeasureInlineChildrenWidth(LayoutBox box)
    {
        float maxWidth = 0;
        float lineWidth = 0;
        foreach (var child in box.Children)
        {
            if (IsOutOfFlow(child)) continue;

            LayoutChild(child, new LayoutConstraints(null, null), 0, 0);
            float w = child.BoxModel.MarginBox.Width;

            if (IsInlineOrInlineBlock(child))
            {
                lineWidth += w;
                maxWidth = Math.Max(maxWidth, lineWidth);
            }
            else
            {
                lineWidth = 0;
                maxWidth = Math.Max(maxWidth, w);
            }
        }
        return maxWidth;
    }

    internal static bool IsInlineOrInlineBlock(LayoutBox child)
    {
        // 文本节点（TextNode）是行内级盒，与 inline/inline-block 一起参与水平行内流。
        return child.Type == LayoutType.Inline
            || child.Type == LayoutType.InlineBlock
            || child.Type == LayoutType.Text;
    }

    /// <summary>
    /// 该盒子是否为强制换行标记（<c>&lt;br&gt;</c>）。br 是行内级空元素，本身不产生可见盒，
    /// 但在行内流中会结束当前行盒，使其后的行内内容排到新的一行。
    /// </summary>
    internal static bool IsForcedLineBreak(LayoutBox child)
        => child.Element is Miko.Core.DomElements.BrElement;

    /// <summary>
    /// replaced 元素（video / img）的内禀尺寸（CSS 像素）。
    /// 媒体已加载时返回真实尺寸；video 未知时回退到 HTML 默认 300×150。
    /// img 未加载完成时返回 (0, 0)（无内禀尺寸，需 CSS 显式定尺寸以避免加载前后的重排抖动）。
    /// 非 replaced 元素返回 (0, 0)。
    /// </summary>
    internal static (float width, float height) GetReplacedIntrinsicSize(Core.Element element)
    {
        if (element is Core.DomElements.VideoElement video)
        {
            float w = video.IntrinsicWidth ?? 300;
            float h = video.IntrinsicHeight ?? 150;
            return (w, h);
        }
        if (element is Core.DomElements.ImageElement image)
        {
            return (image.IntrinsicWidth ?? 0, image.IntrinsicHeight ?? 0);
        }
        return (0, 0);
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
    /// 是否为“文本表单控件”：其盒子高度应由文本（行高/字体度量）撑起，且其子元素不在常规流中
    /// 参与盒子高度（如 select 的 option 由下拉层叠加渲染，不计入闭合态的高度；textarea 的文本
    /// 由元素自身渲染，不作为子节点参与布局）。
    /// 目前包括 input（文本类）、select 与 textarea。
    /// </summary>
    internal static bool IsTextFormControl(LayoutBox box)
        => box.Element is Miko.Core.DomElements.InputElement
        || box.Element is Miko.Core.DomElements.SelectElement
        || box.Element is Miko.Core.DomElements.TextAreaElement;

    /// <summary>
    /// 文本表单控件在自动高度时的内容高度：
    /// - input[text/password]、select：由一行文本占据（一行高度）；
    /// - textarea：由其 <see cref="Core.DomElements.TextAreaElement.Rows"/> 行文本占据（多行高度）。
    /// 优先取显式行高（line-height），否则取字体度量高度。
    /// 返回 null 表示该盒子不适用此规则（应回退到常规的内容高度计算）。
    /// </summary>
    internal static float? GetTextFormControlContentHeight(LayoutBox box)
    {
        if (!IsTextFormControl(box))
            return null;

        float lineHeight = ResolveLineHeight(box.ComputedStyle);

        // textarea 高度由可见行数（rows）撑起。
        if (box.Element is Miko.Core.DomElements.TextAreaElement textArea)
            return lineHeight * Math.Max(1, textArea.Rows);

        return lineHeight;
    }

    /// <summary>
    /// textarea 在自动宽度时的内容宽度：由其 <see cref="Core.DomElements.TextAreaElement.Cols"/>
    /// 列数乘以字体平均字符宽度得到（对齐浏览器 <c>&lt;textarea cols&gt;</c> 的固有宽度语义）。
    /// 返回 null 表示该盒子不适用此规则（应回退到常规的内容宽度计算）。
    /// </summary>
    /// <remarks>
    /// 浏览器以字体的“平均字符宽度”（约等于数字 '0' 的字宽）× cols 作为 textarea 的固有内容宽度。
    /// Miko 无字体 avgCharWidth 度量接口，这里以字符 '0' 的实测宽度近似平均字宽，与浏览器观感一致。
    /// </remarks>
    internal static float? GetTextFormControlContentWidth(LayoutBox box)
    {
        if (box.Element is not Miko.Core.DomElements.TextAreaElement textArea)
            return null;

        var style = box.ComputedStyle;
        float avgCharWidth = TextMeasurer.MeasureTextWidth(
            "0", style.FontFamily, style.FontSize.Value, style.FontWeight);

        return avgCharWidth * Math.Max(1, textArea.Cols);
    }

    /// <summary>
    /// 解析元素一行文本的有效高度（line box 高度）：
    /// - 若显式设置了 line-height（非 auto 且 &gt;0），按元素自身字体大小解析其中的 em / number 分量
    ///   后返回（如 <c>line-height: 1.5</c> 在 16px 字号下返回 24px）；
    /// - 否则回退到字体度量得到的自然行高（ascent + descent）。
    /// 用于布局阶段确定带文本元素自身的内容行高（参见 ISSUE-041）。
    /// </summary>
    internal static float ResolveLineHeight(Styling.ComputedStyle style)
    {
        // 显式 line-height（>0 表示非 normal），按元素自身字体大小解析。
        if (!style.LineHeight.IsAuto && style.LineHeight.Value > 0)
            return style.LineHeight.ToPixels(0, style.FontSize.Value);

        // 默认按字体度量得到一行文本的自然高度
        return TextMeasurer.MeasureTextHeight(
            style.FontFamily,
            style.FontSize.Value,
            style.FontWeight);
    }
}
