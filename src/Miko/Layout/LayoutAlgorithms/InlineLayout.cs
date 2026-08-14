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

        // 2. 创建行盒子（line box）并排列行内子盒
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;

        // 文本节点作为普通行内子盒参与，交错顺序由子节点列表表达（见 ISSUE-086），
        // 不再需要把父元素的 TextContent 作为「排在子元素之前的匿名盒」特殊处理。

        // 行内流的换行宽度：当行内元素自身有宽度约束时，行内内容应在剩余宽度内换行
        // （见 ISSUE-079）。auto 宽度且无约束时传 null，整段流排为单行（shrink-to-fit）。
        float? textWrapWidth = null;
        if (constraints.AvailableWidth.HasValue && constraints.AvailableWidth.Value > 0)
        {
            textWrapWidth = constraints.AvailableWidth.Value
                - box.BoxModel.Padding.Horizontal
                - box.BoxModel.Border.Horizontal;
            if (textWrapWidth < 0) textWrapWidth = 0;
        }

        // 脱离文档流的子元素仍布局以获得尺寸，但不参与行内流。
        // 最终位置由 LayoutEngine 的定位阶段修正。
        foreach (var child in box.Children)
        {
            if (BlockLayout.IsOutOfFlow(child))
            {
                LayoutChild(child, new LayoutConstraints(null, null), contentX, contentY);
            }
        }

        // 行内格式化上下文统一断行（ISSUE-110）：文本片段与行内元素盒共享行盒、
        // 按序排列，超宽时在断行单元边界换行（首个排版遍）。
        // 原子盒宽度（百分比解析基准）保持 null：本遍是 shrink-to-fit 的内容测量遍，
        // 传入可用宽度会让 width:100% 的子盒按满宽求值，auto 宽父盒随之被撑满
        // （见 ISSUE-077 / ISSUE-081，InlineBlockMinSizeTests 覆盖）。
        // 嵌套 inline 盒的换行不依赖这个基准——它们在 IFC 中被透明化展开、
        // 直接用本行内流的 wrapWidth 断行（ISSUE-126）。
        var run = LayoutContents(
            box, contentX, contentY,
            textWrapWidth,
            null, null);
        float lineHeight = run.TotalHeight;
        float maxWidth = run.MaxLineWidth;

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
        // 外部已定型宽度/高度（作为 flex 项时主轴尺寸已解析，见 ISSUE-106）：直接使用，
        // 跳过自身 width/height 解析与该轴 min/max 夹取，避免百分比长度二次求值。
        bool widthIsForced = constraints.ResolvedContentWidth.HasValue;
        bool heightIsForced = constraints.ResolvedContentHeight.HasValue;

        if (widthIsForced)
        {
            contentWidth = constraints.ResolvedContentWidth!.Value;
        }
        else if (!widthIsAuto)
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
        else
        {
            // auto 宽度：textarea 由 cols 列数撑起固有宽度；否则为行内子盒（含文本节点）排列后的最大宽度。
            contentWidth = BlockLayout.GetTextFormControlContentWidth(box) ?? maxWidth;
        }

        // 先应用 min/max-width 约束，得到确定的内容宽度，再据此重排子元素与计算高度。
        // border-box 下 min/max-width 约束的是 border-box 宽度，需先扣除水平 padding+border
        // 再与内容宽度比较（与 Width 的 border-box 处理保持一致，参见 BlockLayout）。
        bool isBorderBox = style.BoxSizing == BoxSizing.BorderBox;
        float horizontalExtra = box.BoxModel.Border.Horizontal + box.BoxModel.Padding.Horizontal;
        float verticalExtra = box.BoxModel.Border.Vertical + box.BoxModel.Padding.Vertical;

        // 百分比 min/max-width 针对不确定宽度的包含块退化为"无约束"，而非折算为 0 把内容夹取归零
        // （见 ISSUE-094；与 widthPercentAgainstIndefinite 的百分比宽度退化一致）。
        bool widthCbIndefinite = !constraints.AvailableWidth.HasValue || constraints.AvailableWidth.Value <= 0;
        bool minWidthEffective = !widthIsForced && !style.MinWidth.IsAuto && !(style.MinWidth.HasPercentComponent && widthCbIndefinite);
        bool maxWidthEffective = !widthIsForced && !style.MaxWidth.IsAuto && !(style.MaxWidth.HasPercentComponent && widthCbIndefinite);

        if (minWidthEffective)
        {
            float min = style.MinWidth.ToPixels(containerWidth, fs);
            if (isBorderBox) min = Math.Max(0, min - horizontalExtra);
            contentWidth = Math.Max(contentWidth, min);
        }
        if (maxWidthEffective)
        {
            float max = style.MaxWidth.ToPixels(containerWidth, fs);
            if (isBorderBox) max = Math.Max(0, max - horizontalExtra);
            contentWidth = Math.Min(contentWidth, max);
        }

        // 宽度确定后（显式宽度，或 auto 宽度被 min/max-width 夹取为定值），以该确定宽度
        // 重排行内流，使行内内容在自身内容宽度内换行、子元素的 width:100% 能解析到
        // 父内容宽度（浏览器行为）。
        // 高度传 null 保持不确定——min-height 撑起的高度不使百分比高度可解析（见 ISSUE-078）。
        // 注意：auto 宽度且无 min/max-width 时不重排，保持 shrink-to-fit 的内容尺寸（见 ISSUE-077）。
        // 针对不确定包含块退化的百分比 min/max-width 不计入"确定宽度"（见 ISSUE-094）。
        bool widthIsDefinite = widthIsForced || !widthIsAuto || minWidthEffective || maxWidthEffective;
        if (widthIsDefinite && contentWidth > 0 && box.Children.Count > 0)
        {
            var rerun = LayoutContents(
                box, contentX, contentY,
                contentWidth,
                contentWidth, null);
            lineHeight = rerun.TotalHeight;
        }

        if (heightIsForced)
        {
            // 外部已定型高度（列向 flex 项主轴尺寸，见 ISSUE-106）。
            contentHeight = constraints.ResolvedContentHeight!.Value;
        }
        else if (!heightIsAuto)
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
            // height auto：按内禀纵横比从最终内容宽度反推高。contentWidth 已应用
            // min/max-width 夹取，因此 max-width 约束会等比缩放高度而非保持内禀高
            // （auto 宽未被夹取时 contentWidth==intrinsicW，结果即内禀高）。见 ISSUE-083。
            contentHeight = contentWidth * intrinsicH / intrinsicW;
        }
        else if (BlockLayout.GetTextFormControlContentHeight(box) is float formControlHeight)
        {
            // 单行文本表单控件（input、select）：以一行文本（行高/字体度量）撑起内容高度。
            // select 的 option 子元素由下拉层叠加渲染，不计入闭合态高度，因此即便存在子元素
            // 也优先采用单行高度，避免被 option 撑高或塌缩为 0（参见 ISSUE-040）。
            contentHeight = formControlHeight;
        }
        else
        {
            // auto 高度：内容高度即行内子盒（含文本节点）行高的最大值。
            contentHeight = lineHeight;
        }

        // 应用 min/max-height 约束（min/max-width 已在重排子元素前处理）。
        // 高度由外部定型时跳过（夹取已在 flex 主轴解析中完成，见 ISSUE-106）。
        if (!heightIsForced && !style.MinHeight.IsAuto)
        {
            float min = style.MinHeight.ToPixels(constraints.AvailableHeight ?? 0, fs);
            if (isBorderBox) min = Math.Max(0, min - verticalExtra);
            contentHeight = Math.Max(contentHeight, min);
        }
        if (!heightIsForced && !style.MaxHeight.IsAuto)
        {
            float max = style.MaxHeight.ToPixels(constraints.AvailableHeight ?? 0, fs);
            if (isBorderBox) max = Math.Max(0, max - verticalExtra);
            contentHeight = Math.Min(contentHeight, max);
        }

        // 高度确定后（外部定型，或自身显式高度），以该确定高度重排行内流，使子元素的
        // height:100% 能解析到父内容高度（浏览器行为）。此前重排只传了确定宽度、高度传 null
        // （因为那时 contentHeight 还未算出），导致 inline-block 内的百分比高度永远退化为
        // auto —— 同样的确定基准 BlockLayout 早已通过 childAvailableHeight 下传。
        // 与 BlockLayout/FlexLayout 一致：min-height 抬升出来的 auto 高度不算确定基准
        // （见 ISSUE-078），故只在 heightIsForced 或显式高度时下传。
        bool heightIsDefinite = heightIsForced || !heightIsAuto;
        if (heightIsDefinite && contentHeight > 0 && box.Children.Count > 0
            && HasPercentHeightChild(box))
        {
            LayoutContents(
                box, contentX, contentY,
                widthIsDefinite && contentWidth > 0 ? contentWidth : textWrapWidth,
                widthIsDefinite && contentWidth > 0 ? contentWidth : textWrapWidth, contentHeight);
        }

        // 强制换行元素（br）作为独立盒（如 flex 项）布局时，应生成一个行盒：宽 0、高一行行高。
        // 在常规块级流中 br 由 BlockLayout 的 IsForcedLineBreak 特殊路径处理、不读此盒高；
        // 而作为 flex 项时它是普通行内子盒，需占据一行的交叉/主轴尺寸，否则在列方向塌缩为 0
        // 导致 br 不产生可见换行（见 ISSUE-093 问题2；对齐浏览器：br flex 项生成一个空行盒）。
        if (BlockLayout.IsForcedLineBreak(box))
        {
            contentWidth = 0;
            contentHeight = BlockLayout.ResolveLineHeight(style);
        }

        box.BoxModel.Content = new RectF(contentX, contentY, contentWidth, contentHeight);
    }

    /// <summary>
    /// 布局行内盒的内容：连续的行内级子盒交给行内格式化上下文统一断行，块级子盒独占整行
    /// （block-in-inline，ISSUE-126）。与 <see cref="BlockLayout"/> 主循环同构。
    ///
    /// 浏览器对 block-in-inline 会把 inline 盒拆成「块级前段 / 块级子元素 / 块级后段」三个
    /// 匿名块；这里不做匿名块拆分，只让块级子盒占据独立的纵向区间、其前后行内内容各成一段
    /// 行内流——最终的视觉排布与浏览器一致，而 inline 盒自身仍是单一盒（其片段矩形覆盖各行内段）。
    /// </summary>
    /// <returns>内容总高度与最宽行盒宽度。</returns>
    private InlineFormattingContext.RunResult LayoutContents(
        LayoutBox box,
        float contentX,
        float contentY,
        float? wrapWidth,
        float? atomicWidth,
        float? atomicHeight)
    {
        var style = box.ComputedStyle;
        float lineHeight = BlockLayout.ResolveLineHeight(style);
        bool allowWrap = TextWrapper.ShouldWrap(style.WhiteSpace);

        float currentY = contentY;
        float maxWidth = 0;

        int i = 0;
        while (i < box.Children.Count)
        {
            var child = box.Children[i];

            // 脱离文档流的子盒不参与常规流（已在调用方单独布局）。
            if (BlockLayout.IsOutOfFlow(child))
            {
                i++;
                continue;
            }

            if (BlockLayout.IsInlineOrInlineBlock(child))
            {
                int runStart = i;
                while (i < box.Children.Count && BlockLayout.IsInlineOrInlineBlock(box.Children[i])
                       && !BlockLayout.IsOutOfFlow(box.Children[i]))
                {
                    i++;
                }

                var run = InlineFormattingContext.Layout(
                    box.Children, runStart, i,
                    contentX, currentY,
                    wrapWidth,
                    atomicWidth, atomicHeight,
                    style.TextAlign,
                    lineHeight,
                    allowWrap);

                currentY += run.TotalHeight;
                maxWidth = Math.Max(maxWidth, run.MaxLineWidth);
            }
            else
            {
                // 块级子盒：独占整行，垂直堆叠。
                LayoutDispatcher.Dispatch(child, new LayoutConstraints(atomicWidth, atomicHeight), contentX, currentY);
                currentY = child.BoxModel.MarginBox.Bottom;
                maxWidth = Math.Max(maxWidth, child.BoxModel.MarginBox.Width);
                i++;
            }
        }

        return new InlineFormattingContext.RunResult
        {
            TotalHeight = currentY - contentY,
            MaxLineWidth = maxWidth,
        };
    }

    private void LayoutChild(LayoutBox child, LayoutConstraints constraints, float x, float y)
    {
        LayoutDispatcher.Dispatch(child, constraints, x, y);
    }

    /// <summary>
    /// 是否存在依赖父高度的在流子元素（height / min-height / max-height 含百分比分量）。
    /// 仅在此时才值得为解析百分比高度多跑一遍行内重排。
    /// </summary>
    private static bool HasPercentHeightChild(LayoutBox box)
    {
        foreach (var child in box.Children)
        {
            if (BlockLayout.IsOutOfFlow(child)) continue;
            var s = child.ComputedStyle;
            if (s.Height.HasPercentComponent
                || s.MinHeight.HasPercentComponent
                || s.MaxHeight.HasPercentComponent)
            {
                return true;
            }
        }
        return false;
    }
}
