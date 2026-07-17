using Miko.Common;
using Miko.Utils;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// Flexbox 布局算法
/// </summary>
public class FlexLayout
{
    public void Layout(LayoutBox box, LayoutConstraints constraints, float x, float y)
    {
        var style = box.ComputedStyle;

        // 1. 计算 margin, border, padding
        float containerWidth = constraints.AvailableWidth ?? 0;
        // 容器自身字体大小（px），用于解析容器长度中的 em 分量。
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

        // 2. 计算容器宽度
        float contentWidth;
        // 宽度未定（auto 且无可用宽度约束）：先按 0 占位，待子元素布局后再收缩包裹。
        bool widthIsIndefinite = false;
        // 百分比宽度针对不确定包含块时退化为 auto（见 ISSUE-077 Flex 循环依赖）。
        bool widthPercentAgainstIndefinite = style.Width.HasPercentComponent && (constraints.IsInfiniteWidth || containerWidth <= 0);
        bool widthIsAuto = style.Width.IsAuto || widthPercentAgainstIndefinite;

        if (!widthIsAuto)
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
            widthIsIndefinite = true;
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
                // 没有子元素时宽度为 0，有子元素时稍后根据子元素计算
                contentWidth = 0;
            }
        }
        else
        {
            float availableWidth = containerWidth - box.BoxModel.Margin.Horizontal;
            contentWidth = availableWidth - box.BoxModel.Border.Horizontal - box.BoxModel.Padding.Horizontal;
        }

        // 计算容器高度
        float containerHeight = constraints.AvailableHeight ?? 0;
        float contentHeight;
        // 百分比高度针对不确定包含块时退化为 auto（见 ISSUE-077 Flex 循环依赖）。
        bool heightPercentAgainstIndefinite = style.Height.HasPercentComponent && !constraints.AvailableHeight.HasValue;
        bool heightIsAuto = style.Height.IsAuto || heightPercentAgainstIndefinite;

        if (!heightIsAuto)
        {
            contentHeight = style.Height.ToPixels(containerHeight, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
            {
                contentHeight -= box.BoxModel.Border.Vertical + box.BoxModel.Padding.Vertical;
                contentHeight = Math.Max(0, contentHeight);
            }
        }
        else if (constraints.AvailableHeight.HasValue &&
                 (style.OverflowY == Overflow.Auto || style.OverflowY == Overflow.Scroll || style.OverflowY == Overflow.Hidden))
        {
            contentHeight = constraints.AvailableHeight.Value
                - box.BoxModel.Margin.Vertical
                - box.BoxModel.Border.Vertical
                - box.BoxModel.Padding.Vertical;
            contentHeight = Math.Max(0, contentHeight);
        }
        else
        {
            contentHeight = 0; // 稍后根据子元素计算
        }

        // 高度是否为"确定尺寸"——用于决定是否把该高度作为确定包含块传给子元素，
        // 以解析子元素的百分比高度（height:100%）。
        // 仅显式 height（或百分比针对确定包含块）、以及 overflow+可用高度约束视为确定；
        // auto 高度（即便被 min-height 抬升）不算确定，子元素 height:100% 应退化为内容尺寸
        // （见 ISSUE-078：min-height 撑出的高度不应让百分比子元素按该高度解析）。
        bool heightIsDefinite = !heightIsAuto
            || (constraints.AvailableHeight.HasValue &&
                (style.OverflowY == Overflow.Auto || style.OverflowY == Overflow.Scroll || style.OverflowY == Overflow.Hidden));

        // 宽度是否为"确定尺寸"（与 heightIsDefinite 对称）：显式宽度、或 auto 宽度针对确定
        // 包含块（contentWidth 取自容器）时为确定；auto 宽度针对不确定容器（即便被 min-width 抬升）
        // 不算确定，子元素 width:100% 应退化为内容尺寸（见 ISSUE-078）。
        bool widthIsDefinite = !widthIsIndefinite;

        // 3. 确定主轴和交叉轴方向
        bool isRow = style.FlexDirection == FlexDirection.Row ||
                     style.FlexDirection == FlexDirection.RowReverse;

        // 2b. 当尺寸为 auto 时，提前用 min-height / min-width 抬升内容区尺寸，再交给行/列布局。
        // 这样当容器（如 ion-segment-button）靠 min-height 撑出高度、但内容更小时，主轴上的
        // justify-content 能在该 min 尺寸内居中子元素（否则主轴尺寸仍为 0，对齐被跳过，子元素贴边）。
        // 交叉轴上的 align-items 同理受 min-width / min-height 影响。
        // 注意：min 只在 auto 维度上抬升占位尺寸，不影响显式尺寸（显式尺寸已是确定值）。
        if (heightIsAuto && !style.MinHeight.IsAuto)
        {
            float minH = style.MinHeight.ToPixels(constraints.AvailableHeight ?? 0, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
                minH = Math.Max(0, minH - box.BoxModel.Border.Vertical - box.BoxModel.Padding.Vertical);
            contentHeight = Math.Max(contentHeight, minH);
        }
        if (widthIsAuto && !style.MinWidth.IsAuto)
        {
            float minW = style.MinWidth.ToPixels(constraints.AvailableWidth ?? 0, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
                minW = Math.Max(0, minW - box.BoxModel.Border.Horizontal - box.BoxModel.Padding.Horizontal);
            contentWidth = Math.Max(contentWidth, minW);
            // 注意：min-width 只抬升占位尺寸（供 justify-content/wrap 使用），
            // 不改变 widthIsIndefinite——auto 宽度即便被 min-width 抬升，对子元素
            // 百分比主轴尺寸解析仍应视为不确定（见 ISSUE-078）。
        }

        // 2c. 提前应用 max-width / max-height 约束，确保子元素布局时使用正确的容器尺寸。
        // 如果 max-width / max-height 在子元素布局之后才应用，子元素的百分比尺寸会相对于
        // 未约束的容器宽度计算，导致子元素超出容器（见 ISSUE-081 IonCard 在 Flex 容器中宽度问题）。
        bool isBorderBoxW_Early = style.BoxSizing == BoxSizing.BorderBox;
        float horizontalExtra_Early = box.BoxModel.Border.Horizontal + box.BoxModel.Padding.Horizontal;
        if (!style.MaxWidth.IsAuto)
        {
            float max = style.MaxWidth.ToPixels(constraints.AvailableWidth ?? 0, fs);
            if (isBorderBoxW_Early) max = Math.Max(0, max - horizontalExtra_Early);
            contentWidth = Math.Min(contentWidth, max);
        }

        bool isBorderBoxH_Early = style.BoxSizing == BoxSizing.BorderBox;
        float verticalExtra_Early = box.BoxModel.Border.Vertical + box.BoxModel.Padding.Vertical;
        if (!style.MaxHeight.IsAuto)
        {
            float max = style.MaxHeight.ToPixels(constraints.AvailableHeight ?? 0, fs);
            if (isBorderBoxH_Early) max = Math.Max(0, max - verticalExtra_Early);
            contentHeight = Math.Min(contentHeight, max);
        }

        // 2d. 解析 flex 容器自身的水平 auto margin（相对其包含块居中）。
        // 这与 BlockLayout 的处理对称：当容器宽度确定且小于包含块时，auto margin 吸收剩余空间。
        // 此处 contentWidth 已在上面经 max-width 夹取定型，且尚未用于计算 contentX（见下方第 4 步），
        // 因此就地更新 margin 即可让内容原点正确偏移，无需事后平移子树。
        // 注意区别：第 610 行附近的 auto margin 逻辑针对 flex 子项在主轴上的分布，与容器自身无关。
        bool marginLeftAuto = style.MarginLeft.IsAuto;
        bool marginRightAuto = style.MarginRight.IsAuto;
        bool hasDefiniteWidth = !widthIsAuto || !style.MaxWidth.IsAuto;
        if ((marginLeftAuto || marginRightAuto) && hasDefiniteWidth
            && !widthIsIndefinite && containerWidth > 0)
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

        // 4. 布局子元素
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;

        float maxCrossSize = 0;

        // 文本节点（TextNode）作为普通 flex 项参与主轴/交叉轴的对齐与分布（见 ISSUE-086），
        // 不再需要把父元素的 TextContent 作为匿名 flex 项特殊处理（原 TextContentOffset 逻辑已移除）。

        // 脱离文档流的子元素（absolute/fixed）不是 flex 项目：
        // 单独布局以获得尺寸，不参与主轴/交叉轴的排列与尺寸计算。
        // 最终位置由 LayoutEngine 的定位阶段修正。
        foreach (var child in box.Children)
        {
            if (BlockLayout.IsOutOfFlow(child))
            {
                float? childAvailableHeight = contentHeight > 0 ? contentHeight : null;
                var childConstraints = new LayoutConstraints(
                    contentWidth > 0 ? contentWidth : (float?)null, childAvailableHeight);
                LayoutDispatcher.Dispatch(child, childConstraints, contentX, contentY);
            }
        }

        if (isRow)
        {
            LayoutRowDirection(box, contentX, contentY, contentWidth, contentHeight, ref maxCrossSize, widthIsIndefinite, heightIsDefinite);
        }
        else
        {
            LayoutColumnDirection(box, contentX, contentY, contentWidth, contentHeight, ref maxCrossSize, heightIsDefinite, widthIsDefinite);
        }

        // 4b. 行布局且宽度未定：收缩包裹到子元素主轴总宽度（对称于下面 auto 高度的处理）。
        // 例如一个 auto 宽度的 flex 行容器作为另一个 flex 行的项目（ion-buttons 内含按钮），
        // 需以子元素的自然宽度作为自身宽度，否则会塌缩为 0。
        if (isRow && widthIsIndefinite && widthIsAuto)
        {
            // 文本节点已作为 flex 子项计入 MarginBox 宽度，无需再单独测量文本（见 ISSUE-086）。
            float totalChildMainWidth = 0;
            foreach (var child in box.Children)
            {
                if (BlockLayout.IsOutOfFlow(child)) continue;
                totalChildMainWidth += child.BoxModel.MarginBox.Width;
            }

            contentWidth = totalChildMainWidth;
        }

        // 5. 计算最终容器高度
        if (heightIsAuto)
        {
            if (isRow)
            {
                contentHeight = maxCrossSize;
            }
            else
            {
                // 列布局：计算所有子元素的总高度（文本节点作为 flex 子项已计入，见 ISSUE-086）。
                float totalChildHeight = 0;
                foreach (var child in box.Children)
                {
                    if (BlockLayout.IsOutOfFlow(child)) continue;
                    totalChildHeight += child.BoxModel.MarginBox.Height;
                }

                contentHeight = totalChildHeight;
            }
        }

        // 6. 应用 min-height/max-height 约束
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

        // 7. 应用 min-width/max-width 约束
        bool isBorderBoxW = style.BoxSizing == BoxSizing.BorderBox;
        float horizontalExtra = box.BoxModel.Border.Horizontal + box.BoxModel.Padding.Horizontal;
        if (!style.MinWidth.IsAuto)
        {
            float min = style.MinWidth.ToPixels(constraints.AvailableWidth ?? 0, fs);
            if (isBorderBoxW) min = Math.Max(0, min - horizontalExtra);
            contentWidth = Math.Max(contentWidth, min);
        }
        if (!style.MaxWidth.IsAuto)
        {
            float max = style.MaxWidth.ToPixels(constraints.AvailableWidth ?? 0, fs);
            if (isBorderBoxW) max = Math.Max(0, max - horizontalExtra);
            contentWidth = Math.Min(contentWidth, max);
        }

        box.BoxModel.Content = new RectF(contentX, contentY, contentWidth, contentHeight);

        // 记录可滚动内容尺寸
        // 滚动区域包含盒子的内边距（见 BlockLayout 中的说明）
        float padH = box.BoxModel.Padding.Horizontal;
        float padV = box.BoxModel.Padding.Vertical;
        if (isRow)
        {
            float totalChildWidth = 0;
            foreach (var child in box.Children)
            {
                if (BlockLayout.IsOutOfFlow(child)) continue;
                totalChildWidth += child.BoxModel.MarginBox.Width;
            }
            box.ScrollableContentWidth = totalChildWidth + padH;
            box.ScrollableContentHeight = maxCrossSize + padV;
        }
        else
        {
            float totalChildHeight = 0;
            foreach (var child in box.Children)
            {
                if (BlockLayout.IsOutOfFlow(child)) continue;
                totalChildHeight += child.BoxModel.MarginBox.Height;
            }
            box.ScrollableContentWidth = maxCrossSize + padH;
            box.ScrollableContentHeight = totalChildHeight + padV;
        }
    }

    private void LayoutRowDirection(LayoutBox box, float contentX, float contentY,
        float contentWidth, float contentHeight, ref float maxCrossSize, bool widthIsIndefinite = false,
        bool crossHeightIsDefinite = true)
    {
        var style = box.ComputedStyle;
        float fs = style.FontSize.Value;

        // 解析 gap：columnGap 用于行内项目间距（主轴），rowGap 用于行间距（交叉轴）。
        float columnGap = (style.ColumnGap.IsAuto ? style.Gap : style.ColumnGap).ToPixels(contentWidth, fs);
        float rowGap = (style.RowGap.IsAuto ? style.Gap : style.RowGap).ToPixels(contentWidth, fs);

        // 收集所有非绝对定位子元素（含文本节点，见 ISSUE-086），并按 order 稳定排序。
        var allChildren = CollectFlexItems(box);

        // 无 flex 项目：提前返回。
        if (allChildren.Count == 0)
        {
            return;
        }

        // 如果不换行，所有子元素在一行；否则按宽度 + gap 分行。
        // wrap-reverse 与 wrap 一样分行，仅交叉轴方向相反（在下方反转行序）。
        var lines = style.FlexWrap != FlexWrap.Nowrap
            ? PartitionIntoLines(allChildren, contentWidth, columnGap, true, widthIsIndefinite)
            : new List<List<LayoutBox>> { allChildren };

        // wrap-reverse：交叉轴上行序反转（第一行排在交叉轴末端）。
        if (style.FlexWrap == FlexWrap.WrapReverse) lines.Reverse();

        // 逐行布局，记录每行起始 Y 与实际交叉尺寸，供 align-content 事后调整。
        var lineStarts = new List<float>(lines.Count);
        var lineCrossSizes = new List<float>(lines.Count);
        float currentY = contentY;
        foreach (var line in lines)
        {
            float lineCrossSize = 0;
            lineStarts.Add(currentY);
            LayoutFlexLine(box, line, contentX, currentY, contentWidth, contentHeight, columnGap, true, widthIsIndefinite,
                style.JustifyContent, style.AlignItems, ref lineCrossSize, crossHeightIsDefinite);
            lineCrossSizes.Add(lineCrossSize);
            maxCrossSize = Math.Max(maxCrossSize, lineCrossSize);
            currentY += lineCrossSize + rowGap;
        }

        // 移除最后一行后多余的 rowGap。
        if (lines.Count > 0) currentY -= rowGap;

        // align-content：多行且交叉轴（高度）尺寸确定时，在剩余空间内重新分布各行（沿 Y 平移子树）。
        ApplyAlignContent(lines, lineStarts, lineCrossSizes, style.AlignContent, contentHeight,
            currentY - contentY, crossHeightIsDefinite, isRow: true);

        // 容器交叉轴尺寸由所有行的总高决定（未指定高度时）。
        // 注：justify-content / align-items 已在每行内 (LayoutFlexLine) 应用。
        maxCrossSize = currentY - contentY;
    }

    /// <summary>
    /// 收集参与 flex 布局的子项（排除脱离文档流者），并按 CSS <c>order</c> 稳定排序：
    /// order 值小的在前，相同 order 保持 DOM 顺序。
    /// </summary>
    private static List<LayoutBox> CollectFlexItems(LayoutBox box)
    {
        var items = new List<LayoutBox>();
        foreach (var child in box.Children)
            if (!BlockLayout.IsOutOfFlow(child)) items.Add(child);

        // 是否存在非零 order：无则跳过排序，保持原有顺序与性能。
        bool anyOrder = false;
        foreach (var item in items)
            if (item.ComputedStyle.Order != 0) { anyOrder = true; break; }

        if (!anyOrder) return items;

        // 稳定排序（保持相同 order 的 DOM 顺序）。
        return items
            .Select((c, i) => (c, i))
            .OrderBy(t => t.c.ComputedStyle.Order)
            .ThenBy(t => t.i)
            .Select(t => t.c)
            .ToList();
    }

    /// <summary>
    /// 应用 CSS <c>align-content</c>：当存在多行/多列且交叉轴尺寸确定、有剩余空间时，
    /// 在交叉轴方向重新分布各行/列（通过平移每行/列的子树实现）。
    /// 单行/列、交叉轴尺寸不确定、或 FlexStart（默认起点排布）时不做任何调整。
    /// </summary>
    /// <param name="lines">已布局的行（行方向）或列（列方向）。</param>
    /// <param name="lineStarts">各行/列布局时的起始交叉坐标（Y 或 X）。</param>
    /// <param name="lineCrossSizes">各行/列的实际交叉尺寸。</param>
    /// <param name="crossContentSize">容器交叉轴内容尺寸（行方向为 contentHeight，列方向为 contentWidth）。</param>
    /// <param name="usedCrossSize">已用交叉尺寸（含行/列间 gap）。</param>
    /// <param name="crossIsDefinite">交叉轴尺寸是否确定（否则无剩余空间可分配）。</param>
    /// <param name="isRow">行方向（沿 Y 平移）为 true，列方向（沿 X 平移）为 false。</param>
    private static void ApplyAlignContent(
        List<List<LayoutBox>> lines, List<float> lineStarts, List<float> lineCrossSizes,
        AlignContent alignContent, float crossContentSize, float usedCrossSize,
        bool crossIsDefinite, bool isRow)
    {
        // 仅在交叉轴尺寸确定、多行、且有正剩余空间时才分布。
        // 内容溢出（free ≤ 0）时不调整，各行保持自起点排布，避免负偏移导致意外上溢。
        if (!crossIsDefinite || lines.Count <= 1) return;
        float free = crossContentSize - usedCrossSize;
        if (free <= 0.01f) return;

        int n = lines.Count;
        float offset = 0;      // 首行/列相对其当前位置的额外偏移。
        float spacing = 0;     // 行/列之间额外增加的间距。

        switch (alignContent)
        {
            case AlignContent.FlexStart:
                return; // 默认排布，无需调整。
            case AlignContent.FlexEnd:
                offset = free;
                break;
            case AlignContent.Center:
                offset = free / 2f;
                break;
            case AlignContent.SpaceBetween:
                spacing = n > 1 ? free / (n - 1) : 0;
                break;
            case AlignContent.SpaceAround:
                spacing = free / n;
                offset = spacing / 2f;
                break;
            case AlignContent.Stretch:
                // 简化实现：stretch 等分剩余空间为行间距（不改变行内子项交叉尺寸），
                // 使各行均匀铺满交叉轴。
                spacing = free / n;
                offset = spacing / 2f;
                break;
        }

        for (int i = 0; i < n; i++)
        {
            // 目标起点 = 原布局起点 + 首偏移 + 前面各行/列累计的额外间距。
            float target = lineStarts[i] + offset + spacing * i;
            float delta = target - lineStarts[i];
            if (Math.Abs(delta) < 0.01f) continue;
            foreach (var child in lines[i])
            {
                if (isRow) OffsetSubtree(child, 0, delta);
                else OffsetSubtree(child, delta, 0);
            }
        }
    }

    private void LayoutColumnDirection(LayoutBox box, float contentX, float contentY,
        float contentWidth, float contentHeight, ref float maxCrossSize, bool heightIsDefinite,
        bool crossWidthIsDefinite = true)
    {
        var style = box.ComputedStyle;
        float fs = style.FontSize.Value;

        // 解析 gap：rowGap 用于列内项目间距（主轴），columnGap 用于列间距（交叉轴）。
        float rowGap = (style.RowGap.IsAuto ? style.Gap : style.RowGap).ToPixels(contentHeight, fs);
        float columnGap = (style.ColumnGap.IsAuto ? style.Gap : style.ColumnGap).ToPixels(contentHeight, fs);

        // 收集所有非绝对定位子元素（含文本节点，见 ISSUE-086），并按 order 稳定排序。
        var allChildren = CollectFlexItems(box);

        // 无 flex 项目：提前返回。
        if (allChildren.Count == 0)
        {
            return;
        }

        // 列方向主轴为高度；高度未指定（auto，未被 overflow 约束为确定）时为 indefinite，
        // 不参与 shrink 且不分列。注意 min-height 抬升 contentHeight 不使其变为确定（见 ISSUE-078）。
        bool heightIsIndefinite = !heightIsDefinite;

        // 如果不换行或高度未定，所有子元素在一列；否则按高度 + gap 分列。
        // wrap-reverse 与 wrap 一样分列，仅交叉轴方向相反（在下方反转列序）。
        var lines = (style.FlexWrap != FlexWrap.Nowrap && !heightIsIndefinite)
            ? PartitionIntoLines(allChildren, contentHeight, rowGap, false, heightIsIndefinite)
            : new List<List<LayoutBox>> { allChildren };

        // wrap-reverse：交叉轴上列序反转（第一列排在交叉轴末端）。
        if (style.FlexWrap == FlexWrap.WrapReverse) lines.Reverse();

        // 逐列布局，记录每列起始 X 与实际交叉尺寸，供 align-content 事后调整。
        var lineStarts = new List<float>(lines.Count);
        var lineCrossSizes = new List<float>(lines.Count);
        float currentX = contentX;
        foreach (var line in lines)
        {
            float lineCrossSize = 0;
            lineStarts.Add(currentX);
            LayoutFlexLine(box, line, currentX, contentY, contentHeight, contentWidth, rowGap, false, heightIsIndefinite,
                style.JustifyContent, style.AlignItems, ref lineCrossSize, crossWidthIsDefinite);
            lineCrossSizes.Add(lineCrossSize);
            maxCrossSize = Math.Max(maxCrossSize, lineCrossSize);
            currentX += lineCrossSize + columnGap;
        }

        // 移除最后一列后多余的 columnGap。
        if (lines.Count > 0) currentX -= columnGap;

        // align-content：多列且交叉轴（宽度）尺寸确定时，在剩余空间内重新分布各列（沿 X 平移子树）。
        ApplyAlignContent(lines, lineStarts, lineCrossSizes, style.AlignContent, contentWidth,
            currentX - contentX, crossWidthIsDefinite, isRow: false);

        // 容器交叉轴尺寸由所有列的总宽决定（未指定宽度时）。
        // 注：justify-content / align-items 已在每列内 (LayoutFlexLine) 应用。
        maxCrossSize = currentX - contentX;
    }
    /// <summary>
    /// 递归平移一个盒子及其全部子孙的内容区。用于 flex 对齐/分布产生的位移，
    /// 确保子元素的子树跟随移动。
    /// </summary>
    private static void OffsetSubtree(LayoutBox box, float dx, float dy)
    {
        var content = box.BoxModel.Content;
        box.BoxModel.Content = new RectF(content.X + dx, content.Y + dy, content.Width, content.Height);
        foreach (var child in box.Children)
            OffsetSubtree(child, dx, dy);
    }

    /// <summary>
    /// Flex 子元素信息
    /// </summary>
    private class FlexChildInfo
    {
        public required LayoutBox Child { get; set; }
        public float FlexBasis { get; set; }
        public float FlexGrow { get; set; }
        public float FlexShrink { get; set; }
        public float FinalSize { get; set; }
        public bool UsedAutoSize { get; set; }
    }

    /// <summary>
    /// 将子元素按 FlexWrap 分行/列。主轴方向 isRow=true → 按宽度分行，false → 按高度分列。
    /// </summary>
    private List<List<LayoutBox>> PartitionIntoLines(List<LayoutBox> children, float availableMainSize,
        float gap, bool isRow, bool mainSizeIsIndefinite)
    {
        var lines = new List<List<LayoutBox>>();
        var currentLine = new List<LayoutBox>();
        float currentLineSize = 0;

        foreach (var child in children)
        {
            // 计算该子元素的主轴 flex-basis（初步尺寸，未经 grow/shrink）。
            float childBasis = ComputeFlexBasis(child, availableMainSize, isRow, mainSizeIsIndefinite);

            // 如果当前行非空且加上此子会超出容器宽度，则换行。
            bool wouldOverflow = currentLine.Count > 0 &&
                (currentLineSize + gap + childBasis > availableMainSize + 0.01f);

            if (wouldOverflow)
            {
                lines.Add(currentLine);
                currentLine = new List<LayoutBox>();
                currentLineSize = 0;
            }

            currentLine.Add(child);
            currentLineSize += childBasis;
            if (currentLine.Count > 1) currentLineSize += gap; // gap 从第二个元素起计。
        }

        if (currentLine.Count > 0) lines.Add(currentLine);
        return lines;
    }

    private float ComputeFlexBasis(LayoutBox child, float containerMainSize, bool isRow, bool mainSizeIsIndefinite)
        => ComputeFlexBasis(child, containerMainSize, isRow, mainSizeIsIndefinite, out _);

    /// <summary>
    /// 计算子元素的 flex-basis（主轴方向初步尺寸），包含 margin/border/padding (outer size)。
    /// <paramref name="usedAutoSize"/> 表示是否使用了内容自然尺寸（无显式 width/height/flex-basis）。
    /// </summary>
    private float ComputeFlexBasis(LayoutBox child, float containerMainSize, bool isRow, bool mainSizeIsIndefinite, out bool usedAutoSize)
    {
        var style = child.ComputedStyle;
        float fs = style.FontSize.Value;

        float basisContentSize;
        usedAutoSize = false;

        if (!style.FlexBasis.IsAuto)
        {
            // flex-basis 为百分比且容器主轴不确定时退化为 auto（见 ISSUE-077 Flex 循环依赖）。
            if (style.FlexBasis.HasPercentComponent && mainSizeIsIndefinite)
            {
                // 退化为内容尺寸
                var childConstraints = new LayoutConstraints(null, null);
                LayoutDispatcher.Dispatch(child, childConstraints, 0, 0);
                basisContentSize = isRow ? child.BoxModel.Content.Width : child.BoxModel.Content.Height;
                usedAutoSize = true;
            }
            else
            {
                basisContentSize = style.FlexBasis.ToPixels(containerMainSize, fs);
            }
        }
        else
        {
            var explicitSize = isRow ? style.Width : style.Height;
            // 显式尺寸为百分比且容器主轴不确定时退化为 auto（见 ISSUE-077 Flex 循环依赖）。
            if (!explicitSize.IsAuto && !(explicitSize.HasPercentComponent && mainSizeIsIndefinite))
            {
                basisContentSize = explicitSize.ToPixels(containerMainSize, fs);
            }
            else
            {
                // 使用内容自然尺寸。
                var childConstraints = new LayoutConstraints(null, null);
                LayoutDispatcher.Dispatch(child, childConstraints, 0, 0);
                basisContentSize = isRow ? child.BoxModel.Content.Width : child.BoxModel.Content.Height;
                usedAutoSize = true;
            }
        }

        // 转换为 outer size (margin box)。
        float outerExtra;
        if (isRow)
        {
            float ml = style.MarginLeft.ToPixels(containerMainSize, fs);
            float mr = style.MarginRight.ToPixels(containerMainSize, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
            {
                outerExtra = ml + mr;
            }
            else
            {
                outerExtra = ml + mr
                    + style.BorderLeftWidth.ToPixels(containerMainSize, fs)
                    + style.BorderRightWidth.ToPixels(containerMainSize, fs)
                    + style.PaddingLeft.ToPixels(containerMainSize, fs)
                    + style.PaddingRight.ToPixels(containerMainSize, fs);
            }
        }
        else
        {
            float mt = style.MarginTop.ToPixels(containerMainSize, fs);
            float mb = style.MarginBottom.ToPixels(containerMainSize, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
            {
                outerExtra = mt + mb;
            }
            else
            {
                outerExtra = mt + mb
                    + style.BorderTopWidth.ToPixels(containerMainSize, fs)
                    + style.BorderBottomWidth.ToPixels(containerMainSize, fs)
                    + style.PaddingTop.ToPixels(containerMainSize, fs)
                    + style.PaddingBottom.ToPixels(containerMainSize, fs);
            }
        }

        return basisContentSize + outerExtra;
    }

    /// <summary>
    /// 布局一行 flex 子元素（行方向）或一列（列方向），应用 flex-grow/shrink + gap。
    /// </summary>
    private void LayoutFlexLine(LayoutBox box, List<LayoutBox> lineChildren, float lineX, float lineY,
        float lineMainSize, float lineCrossSize, float gap, bool isRow, bool mainSizeIsIndefinite,
        JustifyContent justifyContent, AlignItems alignItems, ref float resultCrossSize,
        bool crossSizeIsDefinite = true)
    {
        // 第一遍：计算 flex-basis。文本节点（TextNode）作为普通 flex 项参与（见 ISSUE-086）。
        var childInfos = new List<FlexChildInfo>();
        float totalFlexBasisSize = 0;
        float totalFlexGrow = 0;
        float totalFlexShrinkWeighted = 0;

        foreach (var child in lineChildren)
        {
            var childStyle = child.ComputedStyle;
            float flexBasis = ComputeFlexBasis(child, lineMainSize, isRow, mainSizeIsIndefinite, out bool usedAutoSize);

            var info = new FlexChildInfo
            {
                Child = child,
                FlexBasis = flexBasis,
                FlexGrow = childStyle.FlexGrow,
                FlexShrink = childStyle.FlexShrink,
                FinalSize = flexBasis,
                UsedAutoSize = usedAutoSize
            };

            childInfos.Add(info);
            totalFlexBasisSize += flexBasis;
            totalFlexGrow += childStyle.FlexGrow;
            totalFlexShrinkWeighted += childStyle.FlexShrink * flexBasis;
        }

        // gap 占用主轴空间：n 个项目间有 n-1 个 gap。
        int totalItems = lineChildren.Count;
        float totalGapSize = Math.Max(0, totalItems - 1) * gap;
        float freeSpace = lineMainSize - totalFlexBasisSize - totalGapSize;

        // 检查 auto margin。
        int autoMarginCount = 0;
        foreach (var info in childInfos)
        {
            var childStyle = info.Child.ComputedStyle;
            if (isRow)
            {
                if (childStyle.MarginLeft.IsAuto) autoMarginCount++;
                if (childStyle.MarginRight.IsAuto) autoMarginCount++;
            }
            else
            {
                if (childStyle.MarginTop.IsAuto) autoMarginCount++;
                if (childStyle.MarginBottom.IsAuto) autoMarginCount++;
            }
        }

        bool hasAutoMargins = autoMarginCount > 0 && freeSpace > 0;
        float autoMarginSize = hasAutoMargins ? Math.Max(0, freeSpace) / autoMarginCount : 0;

        // flex-grow / flex-shrink（仅当没有 auto margin 时）。
        bool anyAdjustment = false;
        if (!hasAutoMargins)
        {
            if (freeSpace > 0 && totalFlexGrow > 0)
            {
                anyAdjustment = true;
                foreach (var info in childInfos)
                {
                    float growRatio = info.FlexGrow / totalFlexGrow;
                    info.FinalSize = info.FlexBasis + freeSpace * growRatio;
                }
            }
            else if (freeSpace < 0 && totalFlexShrinkWeighted > 0 && !mainSizeIsIndefinite)
            {
                anyAdjustment = true;
                float shrinkAmount = -freeSpace;
                foreach (var info in childInfos)
                {
                    float shrinkRatio = (info.FlexShrink * info.FlexBasis) / totalFlexShrinkWeighted;
                    info.FinalSize = info.FlexBasis - shrinkAmount * shrinkRatio;
                    info.FinalSize = Math.Max(0, info.FinalSize);
                }
            }
        }

        // 第二遍：用最终尺寸布局子元素并放置（含 gap）。
        float lineStart = isRow ? lineX : lineY;
        float lineCross = isRow ? lineY : lineX;
        float currentMain = lineStart;
        float maxCross = 0;

        foreach (var info in childInfos)
        {
            var child = info.Child;
            var childStyle = child.ComputedStyle;
            float childFs = childStyle.FontSize.Value;

            // 交叉轴是否 stretch：仅当有效 align（align-self 覆盖 align-items）为 Stretch
            // 且交叉轴尺寸为 auto 时，子元素才填满该行的交叉尺寸。文本节点不可拉伸。
            // 非 stretch 且交叉轴 auto 的子元素应 shrink-to-fit（内容尺寸），而非撑满交叉轴
            // （见 ISSUE-093 问题1：align-items:center 的 auto 宽度列项应收缩包裹）。
            AlignItems effectiveAlign = ResolveItemAlign(childStyle.AlignSelf, alignItems);
            bool crossSizeIsAuto = isRow ? childStyle.Height.IsAuto : childStyle.Width.IsAuto;
            bool crossStretches = effectiveAlign == AlignItems.Stretch
                && crossSizeIsAuto && child.Type != LayoutType.Text;

            // 处理 auto margin。
            float extraMarginBefore = 0, extraMarginAfter = 0;
            if (hasAutoMargins)
            {
                if (isRow)
                {
                    if (childStyle.MarginLeft.IsAuto) extraMarginBefore = autoMarginSize;
                    if (childStyle.MarginRight.IsAuto) extraMarginAfter = autoMarginSize;
                }
                else
                {
                    if (childStyle.MarginTop.IsAuto) extraMarginBefore = autoMarginSize;
                    if (childStyle.MarginBottom.IsAuto) extraMarginAfter = autoMarginSize;
                }
                currentMain += extraMarginBefore;
            }

            bool needsResize = anyAdjustment && Math.Abs(info.FinalSize - info.FlexBasis) > 0.01f;

            if (isRow)
            {
                float childMarginLeft = childStyle.MarginLeft.IsAuto ? 0 : childStyle.MarginLeft.ToPixels(lineMainSize, childFs);
                float childMarginRight = childStyle.MarginRight.IsAuto ? 0 : childStyle.MarginRight.ToPixels(lineMainSize, childFs);
                // 交叉轴（高）可用尺寸：仅当容器高度为确定尺寸时才作为确定包含块传给子元素，
                // 以解析子元素 height:100%。容器高度为 auto（即便被 min-height 抬升到 lineCrossSize>0）
                // 时传 null，使子元素百分比高度退化为内容尺寸（见 ISSUE-078）。
                // 交叉轴为 auto 且非 stretch 时也传 null，让子元素按内容 shrink-to-fit（见 ISSUE-093 问题1）。
                float? childAvailableHeight = crossSizeIsDefinite && lineCrossSize > 0
                    && (crossStretches || !crossSizeIsAuto) ? lineCrossSize : null;

                // dispatch 把 margin-box 原点放到 currentMain；置子前不再修正坐标。
                if (needsResize || !info.UsedAutoSize)
                {
                    // 显式尺寸或需 grow/shrink：用计算出的 content width 重新布局并强制宽度。
                    float childBorderLeftWidth = childStyle.BorderLeftWidth.ToPixels(lineMainSize, childFs);
                    float childBorderRightWidth = childStyle.BorderRightWidth.ToPixels(lineMainSize, childFs);
                    float childPaddingLeft = childStyle.PaddingLeft.ToPixels(lineMainSize, childFs);
                    float childPaddingRight = childStyle.PaddingRight.ToPixels(lineMainSize, childFs);

                    float childContentWidth = info.FinalSize - childMarginLeft - childMarginRight
                        - childBorderLeftWidth - childBorderRightWidth - childPaddingLeft - childPaddingRight;
                    childContentWidth = Math.Max(0, childContentWidth);

                    var childConstraints = new LayoutConstraints(childContentWidth, childAvailableHeight);
                    LayoutDispatcher.Dispatch(child, childConstraints, currentMain, lineCross);
                    child.BoxModel.Content = new RectF(
                        child.BoxModel.Content.X, child.BoxModel.Content.Y,
                        childContentWidth, child.BoxModel.Content.Height);
                }
                else
                {
                    // 自然尺寸（含文本）：不强制宽度，让子元素按内容布局。
                    var childConstraints = new LayoutConstraints(null, childAvailableHeight);
                    LayoutDispatcher.Dispatch(child, childConstraints, currentMain, lineCross);
                }

                // 子元素 margin-box 已放在 currentMain，下一项从其右缘开始（含 auto margin + gap）。
                currentMain = child.BoxModel.MarginBox.Right + extraMarginAfter + gap;
                maxCross = Math.Max(maxCross, child.BoxModel.MarginBox.Height);
            }
            else
            {
                float childMarginTop = childStyle.MarginTop.IsAuto ? 0 : childStyle.MarginTop.ToPixels(lineMainSize, childFs);
                float childMarginBottom = childStyle.MarginBottom.IsAuto ? 0 : childStyle.MarginBottom.ToPixels(lineMainSize, childFs);
                // 交叉轴（宽）可用尺寸：仅当容器宽度为确定尺寸时才作为确定包含块传给子元素，
                // 以解析子元素 width:100%。容器宽度为 auto（即便被 min-width 抬升到 lineCrossSize>0）
                // 时传 null，使子元素百分比宽度退化为内容尺寸（见 ISSUE-078）。
                // 交叉轴为 auto 且非 stretch 时也传 null，让子元素按内容 shrink-to-fit（见 ISSUE-093 问题1）。
                float? childAvailableWidth = crossSizeIsDefinite && lineCrossSize > 0
                    && (crossStretches || !crossSizeIsAuto) ? lineCrossSize : null;

                if (needsResize || !info.UsedAutoSize)
                {
                    float childBorderTopWidth = childStyle.BorderTopWidth.ToPixels(lineMainSize, childFs);
                    float childBorderBottomWidth = childStyle.BorderBottomWidth.ToPixels(lineMainSize, childFs);
                    float childPaddingTop = childStyle.PaddingTop.ToPixels(lineMainSize, childFs);
                    float childPaddingBottom = childStyle.PaddingBottom.ToPixels(lineMainSize, childFs);

                    float childContentHeight = info.FinalSize - childMarginTop - childMarginBottom
                        - childBorderTopWidth - childBorderBottomWidth - childPaddingTop - childPaddingBottom;
                    childContentHeight = Math.Max(0, childContentHeight);

                    var childConstraints = new LayoutConstraints(childAvailableWidth, childContentHeight);
                    LayoutDispatcher.Dispatch(child, childConstraints, lineCross, currentMain);
                    child.BoxModel.Content = new RectF(
                        child.BoxModel.Content.X, child.BoxModel.Content.Y,
                        child.BoxModel.Content.Width, childContentHeight);
                }
                else
                {
                    var childConstraints = new LayoutConstraints(childAvailableWidth, null);
                    LayoutDispatcher.Dispatch(child, childConstraints, lineCross, currentMain);
                }

                currentMain = child.BoxModel.MarginBox.Bottom + extraMarginAfter + gap;
                maxCross = Math.Max(maxCross, child.BoxModel.MarginBox.Width);
            }
        }

        // 主轴对齐 (justify-content)：仅当本行无 flex-grow 时应用（grow 已占满主轴）。
        // gap 已计入 totalMainUsed，使对齐基于含间距的实际占用。
        if (totalFlexGrow == 0 && lineMainSize > 0)
        {
            float totalMainUsed = currentMain - lineStart - gap; // 去掉末尾多余 gap
            ApplyLineJustifyContent(childInfos, lineStart, lineMainSize, totalMainUsed, isRow, justifyContent, totalItems);
        }

        // 交叉轴对齐 (align-items)。
        float crossExtent = lineCrossSize > 0 ? Math.Max(lineCrossSize, maxCross) : maxCross;
        ApplyLineAlignItems(childInfos, lineCross, crossExtent, isRow, alignItems);

        resultCrossSize = maxCross;
    }

    /// <summary>对一行/列的子元素应用 justify-content（主轴对齐）。</summary>
    private void ApplyLineJustifyContent(List<FlexChildInfo> line, float start, float containerSize, float contentSize, bool isRow, JustifyContent justifyContent, int totalItems)
    {
        if (line.Count == 0) return;

        float spacing = 0, offset = 0;
        switch (justifyContent)
        {
            case Common.JustifyContent.FlexEnd: offset = containerSize - contentSize; break;
            case Common.JustifyContent.Center: offset = (containerSize - contentSize) / 2; break;
            case Common.JustifyContent.SpaceBetween:
                if (totalItems > 1) spacing = (containerSize - contentSize) / (totalItems - 1);
                break;
            case Common.JustifyContent.SpaceAround:
                spacing = (containerSize - contentSize) / totalItems; offset = spacing / 2; break;
            case Common.JustifyContent.SpaceEvenly:
                spacing = (containerSize - contentSize) / (totalItems + 1); offset = spacing; break;
        }

        for (int i = 0; i < line.Count; i++)
        {
            float itemOffset = offset + spacing * i;
            if (Math.Abs(itemOffset) < 0.01f) continue;
            if (isRow) OffsetSubtree(line[i].Child, itemOffset, 0);
            else OffsetSubtree(line[i].Child, 0, itemOffset);
        }
    }

    /// <summary>
    /// 将 <see cref="AlignSelf"/> 映射到等效的 <see cref="AlignItems"/>；
    /// <c>Auto</c> 时回退到容器的 align-items。
    /// </summary>
    private static AlignItems ResolveItemAlign(AlignSelf alignSelf, AlignItems containerAlign)
        => alignSelf switch
        {
            AlignSelf.FlexStart => AlignItems.FlexStart,
            AlignSelf.FlexEnd => AlignItems.FlexEnd,
            AlignSelf.Center => AlignItems.Center,
            AlignSelf.Stretch => AlignItems.Stretch,
            AlignSelf.Baseline => AlignItems.Baseline,
            _ => containerAlign, // Auto
        };

    /// <summary>对一行/列的子元素应用 align-items（交叉轴对齐），并允许每个子项用 align-self 覆盖。</summary>
    private void ApplyLineAlignItems(List<FlexChildInfo> line, float crossStart, float crossSize, bool isRow, AlignItems alignItems)
    {
        foreach (var info in line)
        {
            var child = info.Child;
            // 每个子项可通过 align-self 覆盖容器的 align-items。
            AlignItems effectiveAlign = ResolveItemAlign(child.ComputedStyle.AlignSelf, alignItems);
            float childCrossSize = isRow ? child.BoxModel.MarginBox.Height : child.BoxModel.MarginBox.Width;
            float offset = 0;

            switch (effectiveAlign)
            {
                case Common.AlignItems.FlexEnd: offset = crossSize - childCrossSize; break;
                case Common.AlignItems.Center: offset = (crossSize - childCrossSize) / 2; break;
                case Common.AlignItems.Stretch:
                    // 文本节点不可拉伸（其尺寸由文本决定），锚定起点。
                    // 强制换行元素（br）不产生可见盒，交叉轴保持 0 尺寸不拉伸（见 ISSUE-093 问题2）。
                    if (child.Type == LayoutType.Text || BlockLayout.IsForcedLineBreak(child)) break;
                    if (isRow)
                    {
                        if (child.ComputedStyle.Height.IsAuto)
                        {
                            float target = Math.Max(0, crossSize - child.BoxModel.Margin.Vertical
                                - child.BoxModel.Border.Vertical - child.BoxModel.Padding.Vertical);
                            child.BoxModel.Content = new RectF(child.BoxModel.Content.X, child.BoxModel.Content.Y,
                                child.BoxModel.Content.Width, target);
                        }
                    }
                    else
                    {
                        if (child.ComputedStyle.Width.IsAuto)
                        {
                            float target = Math.Max(0, crossSize - child.BoxModel.Margin.Horizontal
                                - child.BoxModel.Border.Horizontal - child.BoxModel.Padding.Horizontal);
                            child.BoxModel.Content = new RectF(child.BoxModel.Content.X, child.BoxModel.Content.Y,
                                target, child.BoxModel.Content.Height);
                        }
                    }
                    break;
            }

            if (Math.Abs(offset) > 0.01f)
            {
                if (isRow) OffsetSubtree(child, 0, offset);
                else OffsetSubtree(child, offset, 0);
            }
        }
    }
}
