using Miko.Common;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// Grid 布局算法（见 ISSUE-097）。
///
/// 支持子集（对齐组件移植的常见用法）：
/// - 显式轨道模板 <c>grid-template-columns/rows</c>：固定长度（px/em/rem/%）、fr 分数、auto（内容尺寸）；
/// - 隐式轨道由 <c>grid-auto-rows/columns</c> 提供尺寸（默认 auto）；
/// - 子项放置：<c>grid-column/row-start/end</c>（1 起始；0=auto；负结束线相对显式轨道数解析）与
///   行优先 sparse 自动放置（auto-flow row）；自动放置子项的跨度超过显式列数时按规范加宽网格；
/// - 轨道分布：<c>justify-content</c> 分布列轨道、<c>align-content</c> 分布行轨道
///   （容器内容尺寸大于网格总尺寸时生效）。默认模式（FlexStart，充当 CSS normal）与
///   <c>align-content: stretch</c> 按 CSS §12.8 拉伸 auto 轨道填满剩余空间——无模板的
///   单列 grid 因此填满容器宽度；其余显式模式（Center/FlexEnd/Space*）做轨道偏移分布；
/// - 子项对齐：宽度 auto → 拉伸填满 grid area（CSS 默认 stretch，无 justify-items 时 inline 轴仅此与
///   起点对齐两种）；块轴经 align-items / align-self（stretch 仅当高度 auto）；auto margin 吸收
///   area 剩余空间（实现 margin:auto 居中）。
///
/// 已知简化：
/// - 无 repeat() / minmax() / 命名线 / template-areas / auto-flow column / dense 紧凑放置；
/// - 跨多轨道的子项不参与 auto 轨道的内容尺寸计算（其内容可能溢出）；
/// - auto 轨道测量采用 measure-then-layout：子项最多被 dispatch 三次（列内容测量 → 行内容测量 →
///   正式布局），与 flex 的 flex-basis 测量、table 的单元格测量同一先例；
/// - 文本节点作为普通 grid 项参与放置与测量，但不拉伸（与 flex 一致）；其宽度约束取 area 宽度
///   （允许在轨道内换行）。
/// </summary>
public class GridLayout
{
    /// <summary>已放置的 grid 子项（0 起始轨道索引 + 跨度）。</summary>
    private sealed class GridItemInfo
    {
        public required LayoutBox Child { get; init; }
        public int ColumnStart { get; set; }
        public int ColumnSpan { get; set; }
        public int RowStart { get; set; }
        public int RowSpan { get; set; }
    }

    public void Layout(LayoutBox box, LayoutConstraints constraints, float x, float y)
    {
        var style = box.ComputedStyle;
        float containerWidth = constraints.AvailableWidth ?? 0;
        // 容器自身字体大小（px），用于解析容器长度中的 em 分量。
        float fs = style.FontSize.Value;

        // 1. 计算 margin, border, padding（与 FlexLayout 前导一致）
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

        // 2. 计算容器宽度（块级外层：显式 / 不确定→收缩包裹 / 填满可用，与 FlexLayout 对称）。
        float contentWidth;
        bool widthIsIndefinite = false;
        // 百分比宽度针对不确定包含块时退化为 auto（与 ISSUE-077 一致）。
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
            // 无可用宽度约束（如 flex 行内项目）：轨道尺寸确定后按列轨道总宽收缩包裹。
            widthIsIndefinite = true;
            contentWidth = 0;
        }
        else
        {
            float availableWidth = containerWidth - box.BoxModel.Margin.Horizontal;
            contentWidth = availableWidth - box.BoxModel.Border.Horizontal - box.BoxModel.Padding.Horizontal;
        }

        // 百分比 min/max-width 针对不确定宽度的包含块退化为"无约束"（见 ISSUE-094）。
        bool widthCbIndefinite = constraints.IsInfiniteWidth || (constraints.AvailableWidth ?? 0) <= 0;
        bool isBorderBox = style.BoxSizing == BoxSizing.BorderBox;
        float horizontalExtra = box.BoxModel.Border.Horizontal + box.BoxModel.Padding.Horizontal;

        // auto 宽度时提前用 min-width 抬升（收缩包裹的下限；不改变 widthIsIndefinite——
        // min-width 不使宽度对 fr/百分比轨道解析变为确定，与 ISSUE-078 的理念一致）。
        if (widthIsAuto && !style.MinWidth.IsAuto && !(style.MinWidth.HasPercentComponent && widthCbIndefinite))
        {
            float minW = style.MinWidth.ToPixels(constraints.AvailableWidth ?? 0, fs);
            if (isBorderBox) minW = Math.Max(0, minW - horizontalExtra);
            contentWidth = Math.Max(contentWidth, minW);
        }

        // 提前应用 max-width：轨道尺寸（fr、百分比轨道）需以夹取后的宽度为分母（同 ISSUE-081）。
        if (!style.MaxWidth.IsAuto && !(style.MaxWidth.HasPercentComponent && widthCbIndefinite))
        {
            float max = style.MaxWidth.ToPixels(constraints.AvailableWidth ?? 0, fs);
            if (isBorderBox) max = Math.Max(0, max - horizontalExtra);
            contentWidth = Math.Min(contentWidth, max);
        }

        // 3. 计算容器高度（显式 / overflow+可用高度约束 / auto→行轨道求和）。
        float containerHeight = constraints.AvailableHeight ?? 0;
        float contentHeight;
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
            contentHeight = 0; // 行轨道尺寸确定后求和
        }

        // 高度是否确定——决定 fr/百分比行轨道能否相对容器解析；auto（即便被 min-height 抬升）不算确定。
        bool heightIsDefinite = !heightIsAuto
            || (constraints.AvailableHeight.HasValue &&
                (style.OverflowY == Overflow.Auto || style.OverflowY == Overflow.Scroll || style.OverflowY == Overflow.Hidden));

        // 4. 解析 gap：columnGap 相对容器宽度、rowGap 相对容器高度（各自轴的内容尺寸）。
        float columnGap = (style.ColumnGap.IsAuto ? style.Gap : style.ColumnGap).ToPixels(contentWidth, fs);
        float rowGap = (style.RowGap.IsAuto ? style.Gap : style.RowGap).ToPixels(contentHeight, fs);

        // 5. 收集在流子项（含文本节点），按 order 稳定排序。
        var items = CollectGridItems(box);

        // 6. 放置子项（显式定位 + 行优先 sparse 自动放置），得出最终轨道数。
        int explicitCols = style.GridTemplateColumns?.Count ?? 0;
        int explicitRows = style.GridTemplateRows?.Count ?? 0;
        var placed = PlaceItems(items, explicitCols, explicitRows, out int colCount, out int rowCount);

        // 7. 列轨道尺寸。宽度不确定（收缩包裹）时：fr 按内容尺寸、百分比轨道退化为 auto。
        var colSizes = ComputeTrackSizes(
            style.GridTemplateColumns, style.GridAutoColumns, colCount,
            axisIsDefinite: !widthIsIndefinite, availableSize: contentWidth, gap: columnGap,
            placed, isColumn: true, fs, crossSizes: null, crossGap: 0,
            out var colAutoTracks);

        // 8. 宽度收缩包裹：列轨道总宽（含 gap）；min-width 抬升值作为下限保留。
        if (widthIsIndefinite)
        {
            contentWidth = Math.Max(contentWidth, SumTrackSizes(colSizes, columnGap));
        }

        // 8b. justify-content 为默认（FlexStart 充当 CSS normal）且宽度确定：
        // 拉伸 auto 列轨道填满剩余空间（CSS §12.8 Stretch auto Tracks——
        // 无模板单列 auto 轨道据此填满容器宽度）。须在行轨道测量之前完成，
        // 使行测量使用拉伸后的最终列宽（文本/拉伸子项在正确宽度下换行）。
        if (!widthIsIndefinite && style.JustifyContent == JustifyContent.FlexStart)
        {
            StretchAutoTracks(colSizes, colAutoTracks, contentWidth - SumTrackSizes(colSizes, columnGap));
        }

        // 8. 宽度收缩包裹：列轨道总宽（含 gap）；min-width 抬升值作为下限保留。
        if (widthIsIndefinite)
        {
            contentWidth = Math.Max(contentWidth, SumTrackSizes(colSizes, columnGap));
        }

        // 9. 解析容器自身的水平 auto margin（相对其包含块居中，与 FlexLayout 2d 对称）。
        // 宽度不确定（收缩包裹）时 auto margin 无剩余空间可分，保持 0。
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

        // 10. 内容区原点
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;

        // 11. 行轨道尺寸（列轨道已知，auto 行按子项在 area 宽度下的内容高度测量）。
        var rowSizes = ComputeTrackSizes(
            style.GridTemplateRows, style.GridAutoRows, rowCount,
            axisIsDefinite: heightIsDefinite, availableSize: contentHeight, gap: rowGap,
            placed, isColumn: false, fs, crossSizes: colSizes, crossGap: columnGap,
            out var rowAutoTracks);

        // 12. auto 高度：行轨道总高（含 gap）。
        if (heightIsAuto)
        {
            contentHeight = SumTrackSizes(rowSizes, rowGap);
        }

        // 13. 尾部 min/max 约束（与 FlexLayout 步骤 6/7 对称；百分比 min/max-width 对不确定包含块退化）。
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
        if (!style.MinWidth.IsAuto && !(style.MinWidth.HasPercentComponent && widthCbIndefinite))
        {
            float min = style.MinWidth.ToPixels(constraints.AvailableWidth ?? 0, fs);
            if (isBorderBox) min = Math.Max(0, min - horizontalExtra);
            contentWidth = Math.Max(contentWidth, min);
        }
        if (!style.MaxWidth.IsAuto && !(style.MaxWidth.HasPercentComponent && widthCbIndefinite))
        {
            float max = style.MaxWidth.ToPixels(constraints.AvailableWidth ?? 0, fs);
            if (isBorderBox) max = Math.Max(0, max - horizontalExtra);
            contentWidth = Math.Min(contentWidth, max);
        }

        // 13b. align-content 为默认（FlexStart 充当 CSS normal）或 Stretch：
        // 拉伸 auto 行轨道填满剩余空间（与 8b 同一规则；须在 min/max 夹取之后，
        // 使 min-height 撑出的剩余空间也能被 auto 行吸收）。
        if (style.AlignContent is AlignContent.FlexStart or AlignContent.Stretch)
        {
            StretchAutoTracks(rowSizes, rowAutoTracks, contentHeight - SumTrackSizes(rowSizes, rowGap));
        }

        // 14. 轨道位置：justify-content / align-content 的非默认模式在容器内容尺寸
        // 超出网格总尺寸时分布轨道（free ≤ 0 时不调整，保持自起点排布）。
        var colPos = ComputeColumnTrackPositions(colSizes, contentX, columnGap, contentWidth, style.JustifyContent);
        var rowPos = ComputeRowTrackPositions(rowSizes, contentY, rowGap, contentHeight, style.AlignContent);

        // 15. 脱离文档流的子元素（absolute/fixed）不是 grid 项目：不占格，单独布局以获得尺寸，
        // 最终位置由 LayoutEngine 的定位阶段修正（与 FlexLayout 的处理一致）。
        float? outOfFlowWidth = contentWidth > 0 ? contentWidth : (float?)null;
        float? outOfFlowHeight = contentHeight > 0 ? contentHeight : (float?)null;
        foreach (var child in box.Children)
        {
            if (BlockLayout.IsOutOfFlow(child))
            {
                LayoutDispatcher.Dispatch(child, new LayoutConstraints(outOfFlowWidth, outOfFlowHeight), contentX, contentY);
            }
        }

        // 16. 布局各 grid 子项到其 area。
        foreach (var item in placed)
        {
            LayoutItem(item, colPos, colSizes, rowPos, rowSizes, style.AlignItems);
        }

        // 17. 内容区与可滚动内容尺寸（滚动区域包含内边距，与 Block/Flex 一致）。
        box.BoxModel.Content = new RectF(contentX, contentY, contentWidth, contentHeight);
        box.ScrollableContentWidth = SumTrackSizes(colSizes, columnGap) + box.BoxModel.Padding.Horizontal;
        box.ScrollableContentHeight = SumTrackSizes(rowSizes, rowGap) + box.BoxModel.Padding.Vertical;
    }

    /// <summary>
    /// 收集参与 grid 布局的子项（排除脱离文档流者），并按 CSS <c>order</c> 稳定排序
    /// （与 FlexLayout.CollectFlexItems 同一模式）。
    /// </summary>
    private static List<LayoutBox> CollectGridItems(LayoutBox box)
    {
        var items = new List<LayoutBox>();
        foreach (var child in box.Children)
            if (!BlockLayout.IsOutOfFlow(child)) items.Add(child);

        bool anyOrder = false;
        foreach (var item in items)
            if (item.ComputedStyle.Order != 0) { anyOrder = true; break; }

        if (!anyOrder) return items;

        return items
            .Select((c, i) => (c, i))
            .OrderBy(t => t.c.ComputedStyle.Order)
            .ThenBy(t => t.i)
            .Select(t => t.c)
            .ToList();
    }

    /// <summary>
    /// 放置全部子项：显式定位优先，其余按行优先 sparse 自动放置。
    /// 轨道数随放置扩展：隐式行按需增加；隐式列由显式越界放置或超宽自动流跨度产生。
    /// </summary>
    /// <param name="explicitCols">显式列轨道数（grid-template-columns 长度，0 表示无模板）。</param>
    /// <param name="explicitRows">显式行轨道数。</param>
    /// <param name="colCount">最终列轨道数（≥ max(1, explicitCols)）。</param>
    /// <param name="rowCount">最终行轨道数（≥ explicitRows）。</param>
    private static List<GridItemInfo> PlaceItems(
        List<LayoutBox> items, int explicitCols, int explicitRows,
        out int colCount, out int rowCount)
    {
        var placed = new List<GridItemInfo>(items.Count);
        // 占用表：行索引 → 已占列集合，按需增长（隐式行）。
        var occupancy = new List<HashSet<int>>();

        // 解析每个子项两条轴的放置线。
        var parsed = new List<GridItemInfo>(items.Count);
        // 自动流列数：无显式模板时单列；自动放置子项的列跨度超过时按规范加宽（span 不钳制）。
        int flowColCount = Math.Max(1, explicitCols);
        foreach (var child in items)
        {
            var cs = child.ComputedStyle;
            ResolveAxisPlacement(cs.GridColumnStart, cs.GridColumnEnd, explicitCols,
                out int colStart, out int colSpan, out bool colAuto);
            ResolveAxisPlacement(cs.GridRowStart, cs.GridRowEnd, explicitRows,
                out int rowStart, out int rowSpan, out bool rowAuto);

            parsed.Add(new GridItemInfo
            {
                Child = child,
                ColumnStart = colStart,
                ColumnSpan = colSpan,
                RowStart = rowStart,
                RowSpan = rowSpan,
            });

            if (colAuto) flowColCount = Math.Max(flowColCount, colSpan);
        }

        int maxColEnd = flowColCount;
        int maxRowEnd = explicitRows;

        // 第一遍：行、列均显式的子项直接放置。
        var rest = new List<(GridItemInfo info, bool colAuto, bool rowAuto)>(parsed.Count);
        for (int i = 0; i < parsed.Count; i++)
        {
            var child = items[i];
            var cs = child.ComputedStyle;
            bool colAuto = cs.GridColumnStart == 0;
            bool rowAuto = cs.GridRowStart == 0;
            var info = parsed[i];

            if (!colAuto && !rowAuto)
            {
                MarkOccupied(occupancy, info.RowStart, info.ColumnStart, info.RowSpan, info.ColumnSpan);
                maxColEnd = Math.Max(maxColEnd, info.ColumnStart + info.ColumnSpan);
                maxRowEnd = Math.Max(maxRowEnd, info.RowStart + info.RowSpan);
                placed.Add(info);
            }
            else
            {
                rest.Add((info, colAuto, rowAuto));
            }
        }

        // 第二遍：按顺序放置其余子项。
        int cursorRow = 0, cursorCol = 0;
        foreach (var (info, colAuto, rowAuto) in rest)
        {
            if (!colAuto && rowAuto)
            {
                // 列显式、行自动：从第 0 行起扫描首个放得下该跨度的行。
                int r = 0;
                while (!AreaFree(occupancy, r, info.ColumnStart, info.RowSpan, info.ColumnSpan)) r++;
                info.RowStart = r;
            }
            else if (colAuto && !rowAuto)
            {
                // 行显式、列自动：该行内从列 0 起找首个空位；找不到则退化到列 0（可能重叠，
                // 属罕见退化情形——规范应产生隐式列，此处从简）。
                int span = Math.Min(info.ColumnSpan, flowColCount);
                info.ColumnSpan = span;
                int c = 0;
                while (c + span <= flowColCount && !AreaFree(occupancy, info.RowStart, c, info.RowSpan, span)) c++;
                if (c + span > flowColCount) c = 0;
                info.ColumnStart = c;
            }
            else
            {
                // 全自动：行优先游标（sparse——游标不回退）。
                while (true)
                {
                    if (cursorCol + info.ColumnSpan > flowColCount)
                    {
                        cursorRow++;
                        cursorCol = 0;
                        continue;
                    }
                    if (AreaFree(occupancy, cursorRow, cursorCol, info.RowSpan, info.ColumnSpan)) break;
                    cursorCol++;
                }
                info.RowStart = cursorRow;
                info.ColumnStart = cursorCol;
                cursorCol += info.ColumnSpan;
            }

            MarkOccupied(occupancy, info.RowStart, info.ColumnStart, info.RowSpan, info.ColumnSpan);
            maxColEnd = Math.Max(maxColEnd, info.ColumnStart + info.ColumnSpan);
            maxRowEnd = Math.Max(maxRowEnd, info.RowStart + info.RowSpan);
            placed.Add(info);
        }

        colCount = maxColEnd;
        rowCount = Math.Max(maxRowEnd, placed.Count == 0 ? explicitRows : 0);
        return placed;
    }

    /// <summary>
    /// 解析一条轴（列/行）的放置线：1 起始的网格线；0 = 自动放置；负结束线相对显式轨道数解析
    /// （-1 = 最后一条显式线）。输出 0 起始的轨道索引与跨度。
    /// 起始线缺省而结束线确定时，按规范视为自动放置 + 跨度提示（end-1；负值按显式轨道数折算）。
    /// </summary>
    private static void ResolveAxisPlacement(int start, int end, int explicitTrackCount,
        out int startIndex, out int span, out bool isAuto)
    {
        if (start > 0)
        {
            isAuto = false;
            startIndex = start - 1;
            if (end != 0)
            {
                // 结束线（1 起始）：负值相对显式轨道数折算（-1 → explicitTrackCount+1）。
                int endLine = end < 0 ? explicitTrackCount + 2 + end : end;
                span = Math.Max(1, endLine - 1 - startIndex);
            }
            else
            {
                span = 1;
            }
        }
        else
        {
            isAuto = true;
            startIndex = 0;
            if (end > 1)
            {
                span = end - 1;
            }
            else if (end < 0)
            {
                span = Math.Max(1, explicitTrackCount + 1 + end);
            }
            else
            {
                span = 1;
            }
        }
    }

    private static bool AreaFree(List<HashSet<int>> occupancy, int row, int col, int rowSpan, int colSpan)
    {
        for (int r = row; r < row + rowSpan; r++)
        {
            if (r >= occupancy.Count) break;
            var occupiedCols = occupancy[r];
            for (int c = col; c < col + colSpan; c++)
                if (occupiedCols.Contains(c)) return false;
        }
        return true;
    }

    private static void MarkOccupied(List<HashSet<int>> occupancy, int row, int col, int rowSpan, int colSpan)
    {
        while (occupancy.Count < row + rowSpan)
            occupancy.Add(new HashSet<int>());

        for (int r = row; r < row + rowSpan; r++)
            for (int c = col; c < col + colSpan; c++)
                occupancy[r].Add(c);
    }

    /// <summary>
    /// 计算一条轴（列/行）上全部轨道的尺寸：
    /// 固定长度经 ToPixels（百分比针对不确定轴退化为内容尺寸）；auto 取该轨道内 span-1 子项的
    /// 最大内容尺寸；fr 在确定轴上分配扣除固定/auto 轨道与 gap 后的剩余空间，在不确定轴上按内容尺寸。
    /// </summary>
    /// <param name="crossSizes">行轨道测量时的列轨道尺寸（用于子项 area 宽度）；列测量时传 null。</param>
    /// <param name="autoTracks">
    /// 输出：哪些轨道是按内容定尺寸的（auto、百分比退化、不确定轴上的 fr）——
    /// 即 CSS §12.8 中可被 align/justify-content 的 normal/stretch 拉伸填满剩余空间的轨道。
    /// </param>
    private float[] ComputeTrackSizes(
        IReadOnlyList<GridTrackSize>? template, GridTrackSize implicitTrackSize, int trackCount,
        bool axisIsDefinite, float availableSize, float gap,
        List<GridItemInfo> items, bool isColumn, float fs,
        float[]? crossSizes, float crossGap,
        out bool[] autoTracks)
    {
        var sizes = new float[trackCount];
        autoTracks = new bool[trackCount];
        if (trackCount == 0) return sizes;

        List<(int index, float fraction)>? frTracks = null;
        float frSum = 0;

        for (int i = 0; i < trackCount; i++)
        {
            var track = i < (template?.Count ?? 0) ? template![i] : implicitTrackSize;

            if (track.IsFraction)
            {
                if (axisIsDefinite)
                {
                    (frTracks ??= new()).Add((i, track.Fraction));
                    frSum += track.Fraction;
                }
                else
                {
                    // 不确定轴上 fr 轨道按内容尺寸（规范：indefinite 轴的弹性轨道取 max-content）。
                    sizes[i] = MeasureAutoTrack(i, items, isColumn, crossSizes, crossGap);
                    autoTracks[i] = true;
                }
            }
            else if (track.IsFixed && !(track.Fixed.HasPercentComponent && !axisIsDefinite))
            {
                sizes[i] = Math.Max(0, track.Fixed.ToPixels(availableSize, fs));
            }
            else
            {
                // auto，或百分比针对不确定轴退化为内容尺寸（与 ISSUE-077 的退化哲学一致）。
                sizes[i] = MeasureAutoTrack(i, items, isColumn, crossSizes, crossGap);
                autoTracks[i] = true;
            }
        }

        // fr 分配：剩余空间 = 可用尺寸 - 固定/auto 轨道 - gap，按比例分配（不足时为 0）。
        if (frTracks != null)
        {
            float used = gap * Math.Max(0, trackCount - 1);
            foreach (var s in sizes) used += s;
            float leftover = Math.Max(0, availableSize - used);
            foreach (var (index, fraction) in frTracks)
                sizes[index] = leftover * fraction / frSum;
        }

        return sizes;
    }

    /// <summary>
    /// 把剩余空间均分进各自内容定尺寸的（auto）轨道（CSS §12.8：justify/align-content 为
    /// normal 或 stretch 时拉伸 auto 轨道填满容器）。无剩余空间或无 auto 轨道时为空操作。
    /// </summary>
    private static void StretchAutoTracks(float[] sizes, bool[] autoTracks, float free)
    {
        if (free <= 0.01f) return;
        int autoCount = 0;
        foreach (var a in autoTracks) if (a) autoCount++;
        if (autoCount == 0) return;

        float grow = free / autoCount;
        for (int i = 0; i < sizes.Length; i++)
            if (autoTracks[i]) sizes[i] += grow;
    }

    /// <summary>
    /// 测量 auto（或退化为内容尺寸）轨道：取该轨道内 span-1 子项的最大内容尺寸。
    /// 跨轨道子项不参与（见类注释的简化说明）。
    /// </summary>
    private float MeasureAutoTrack(int trackIndex, List<GridItemInfo> items, bool isColumn,
        float[]? colSizes, float columnGap)
    {
        float max = 0;
        foreach (var item in items)
        {
            if (isColumn)
            {
                if (item.ColumnSpan != 1 || item.ColumnStart != trackIndex) continue;
                // 列内容测量：无约束布局取自然宽度（max-content）。
                LayoutDispatcher.Dispatch(item.Child, new LayoutConstraints(null, null), 0, 0);
                max = Math.Max(max, item.Child.BoxModel.MarginBox.Width);
            }
            else
            {
                if (item.RowSpan != 1 || item.RowStart != trackIndex) continue;
                // 行内容测量：子项先按其 area 宽度布局（拉伸宽度的子项/文本在该宽度下换行），
                // 再取 MarginBox 高度。
                float areaW = ComputeAreaSpan(item.ColumnStart, item.ColumnSpan, colSizes!, columnGap);
                float widthConstraint = ComputeWidthConstraint(areaW);
                LayoutDispatcher.Dispatch(item.Child, new LayoutConstraints(widthConstraint, null), 0, 0);
                max = Math.Max(max, item.Child.BoxModel.MarginBox.Height);
            }
        }
        return max;
    }

    /// <summary>计算跨 span 个轨道的 area 尺寸（含内部 gap）。</summary>
    private static float ComputeAreaSpan(int start, int span, float[] sizes, float gap)
    {
        float total = gap * (span - 1);
        for (int i = start; i < start + span; i++) total += sizes[i];
        return total;
    }

    /// <summary>
    /// 子项在 area 内的宽度约束：取 area 全宽（margin 由子项自身布局扣除）——
    /// 宽度 auto 的块级子项据此填满 margin-box、显式/百分比宽度子项以 area 为基准解析、
    /// 文本子项以此宽度换行。
    /// </summary>
    private static float ComputeWidthConstraint(float areaW) => Math.Max(0, areaW);

    private static float SumTrackSizes(float[] sizes, float gap)
    {
        float total = gap * Math.Max(0, sizes.Length - 1);
        foreach (var s in sizes) total += s;
        return total;
    }

    /// <summary>
    /// 计算列轨道位置：justify-content 在确定的内容宽度内分布轨道
    /// （free ≤ 0 时不调整，保持自起点排布）。
    /// </summary>
    private static float[] ComputeColumnTrackPositions(
        float[] sizes, float start, float gap, float containerSize, JustifyContent justifyContent)
    {
        int n = sizes.Length;
        var positions = new float[n];
        if (n == 0) return positions;

        float free = containerSize - SumTrackSizes(sizes, gap);
        float offset = 0, extra = 0;
        if (free > 0.01f)
        {
            switch (justifyContent)
            {
                case JustifyContent.FlexEnd: offset = free; break;
                case JustifyContent.Center: offset = free / 2f; break;
                case JustifyContent.SpaceBetween:
                    if (n > 1) extra = free / (n - 1);
                    break;
                case JustifyContent.SpaceAround:
                    extra = free / n;
                    offset = extra / 2f;
                    break;
                case JustifyContent.SpaceEvenly:
                    extra = free / (n + 1);
                    offset = extra;
                    break;
            }
        }

        positions[0] = start + offset;
        for (int i = 1; i < n; i++)
            positions[i] = positions[i - 1] + sizes[i - 1] + gap + extra;
        return positions;
    }

    /// <summary>
    /// 计算行轨道位置：align-content 的非默认模式在内容高度内分布轨道。
    /// （normal/stretch 的 auto 轨道拉伸已在主流程步骤 13b 处理，此处不再重复。）
    /// </summary>
    private static float[] ComputeRowTrackPositions(
        float[] sizes, float start, float gap, float containerSize, AlignContent alignContent)
    {
        int n = sizes.Length;
        var positions = new float[n];
        if (n == 0) return positions;

        float free = containerSize - SumTrackSizes(sizes, gap);
        float offset = 0, extra = 0;
        if (free > 0.01f)
        {
            switch (alignContent)
            {
                case AlignContent.FlexEnd: offset = free; break;
                case AlignContent.Center: offset = free / 2f; break;
                case AlignContent.SpaceBetween:
                    if (n > 1) extra = free / (n - 1);
                    break;
                case AlignContent.SpaceAround:
                    extra = free / n;
                    offset = extra / 2f;
                    break;
            }
        }

        positions[0] = start + offset;
        for (int i = 1; i < n; i++)
            positions[i] = positions[i - 1] + sizes[i - 1] + gap + extra;
        return positions;
    }

    /// <summary>
    /// 布局一个 grid 子项到其 area：宽度 auto 拉伸填满（文本除外），高度按有效 align
    /// （align-self → align-items）stretch/对齐；auto margin 吸收 area 剩余空间。
    /// </summary>
    private void LayoutItem(
        GridItemInfo item,
        float[] colPos, float[] colSizes, float[] rowPos, float[] rowSizes,
        AlignItems containerAlignItems)
    {
        var child = item.Child;
        var childStyle = child.ComputedStyle;
        float childFs = childStyle.FontSize.Value;

        int colEnd = item.ColumnStart + item.ColumnSpan - 1;
        int rowEnd = item.RowStart + item.RowSpan - 1;
        float areaX = colPos[item.ColumnStart];
        float areaW = colPos[colEnd] + colSizes[colEnd] - areaX;
        float areaY = rowPos[item.RowStart];
        float areaH = rowPos[rowEnd] + rowSizes[rowEnd] - areaY;

        bool isText = child.Type == LayoutType.Text;

        // margin：百分比统一相对 area 宽度解析（CSS 中 margin 百分比恒相对包含块 inline 尺寸）。
        bool mlAuto = childStyle.MarginLeft.IsAuto;
        bool mrAuto = childStyle.MarginRight.IsAuto;
        bool mtAuto = childStyle.MarginTop.IsAuto;
        bool mbAuto = childStyle.MarginBottom.IsAuto;
        float ml = mlAuto ? 0 : childStyle.MarginLeft.ToPixels(areaW, childFs);
        float mr = mrAuto ? 0 : childStyle.MarginRight.ToPixels(areaW, childFs);
        float mt = mtAuto ? 0 : childStyle.MarginTop.ToPixels(areaW, childFs);
        float mb = mbAuto ? 0 : childStyle.MarginBottom.ToPixels(areaW, childFs);

        bool widthIsAuto = childStyle.Width.IsAuto;
        bool heightIsAuto = childStyle.Height.IsAuto;
        // 无 justify-items/justify-self：宽度 auto 的子项拉伸填满 area（CSS 默认 stretch），文本节点除外。
        bool stretchWidth = widthIsAuto && !isText;
        var effectiveAlign = ResolveItemAlign(childStyle.AlignSelf, containerAlignItems);
        bool stretchHeight = effectiveAlign == AlignItems.Stretch && heightIsAuto && !isText;

        // stretch 轴上的 auto margin 无剩余空间可分，归零（CSS：stretch 优先于 auto margin）。
        if (stretchWidth) { mlAuto = false; mrAuto = false; }
        if (stretchHeight) { mtAuto = false; mbAuto = false; }

        // 高度约束：stretch 或显式高度（含百分比，相对 area 解析）时传入 area 全高
        // （margin 由子项自身布局扣除）；auto 高度保持内容决定。
        float widthConstraint = Math.Max(0, areaW);
        float? heightConstraint = (stretchHeight || !heightIsAuto) ? Math.Max(0, areaH) : (float?)null;

        // dispatch 以 (x, y) 为子项 margin-box 原点：area 原点即 margin-box 原点
        // （start 对齐；margin 占据 area 边缘与 border-box 之间的空间）。
        LayoutDispatcher.Dispatch(child, new LayoutConstraints(widthConstraint, heightConstraint), areaX, areaY);

        // 拉伸：强制内容盒填满 area（扣除已解析的 border/padding，使 margin-box == area）。
        // 与 flex 的 align-items:stretch 同一做法：不改写子树，仅定本子盒尺寸。
        if (stretchWidth)
        {
            float cw = Math.Max(0, areaW - ml - mr
                - child.BoxModel.Border.Horizontal - child.BoxModel.Padding.Horizontal);
            var c = child.BoxModel.Content;
            child.BoxModel.Content = new RectF(c.X, c.Y, cw, c.Height);
        }
        if (stretchHeight)
        {
            float ch = Math.Max(0, areaH - mt - mb
                - child.BoxModel.Border.Vertical - child.BoxModel.Padding.Vertical);
            var c = child.BoxModel.Content;
            child.BoxModel.Content = new RectF(c.X, c.Y, c.Width, ch);
        }

        // auto margin 吸收 area 内的剩余空间（margin:auto 居中）。
        // 在子项已解析的 margin 值上累加剩余份额，而非覆盖：子项自身布局可能已吸收
        // 全部剩余空间（如 Block/Flex 子项对确定宽度约束解析了水平 auto margin，
        // 此时 MarginBox 已填满 area，free 为 0，份额为 0，保留子项的解析结果）。
        if (mlAuto || mrAuto)
        {
            float free = Math.Max(0, areaW - child.BoxModel.MarginBox.Width);
            float share = free / ((mlAuto ? 1 : 0) + (mrAuto ? 1 : 0));
            if (mlAuto) ml = child.BoxModel.Margin.Left + share;
            if (mrAuto) mr = child.BoxModel.Margin.Right + share;
            child.BoxModel.Margin = new EdgeSizes(child.BoxModel.Margin.Top, mr, child.BoxModel.Margin.Bottom, ml);
        }
        if (mtAuto || mbAuto)
        {
            float free = Math.Max(0, areaH - child.BoxModel.MarginBox.Height);
            float share = free / ((mtAuto ? 1 : 0) + (mbAuto ? 1 : 0));
            if (mtAuto) mt = child.BoxModel.Margin.Top + share;
            if (mbAuto) mb = child.BoxModel.Margin.Bottom + share;
            child.BoxModel.Margin = new EdgeSizes(mt, child.BoxModel.Margin.Right, mb, child.BoxModel.Margin.Left);
        }

        // 水平位置：起点对齐（stretch 已填满；auto margin 已计入 child 的 Margin，
        // 使 border-box 偏移到 areaX + ml——margin-box 左缘保持 areaX）。
        float dx = areaX - child.BoxModel.MarginBox.Left;
        if (Math.Abs(dx) > 0.01f) OffsetSubtree(child, dx, 0);

        // 垂直位置：有效 align（存在竖直 auto margin 时对齐已被 margin 吸收，margin-box 顶缘
        // 保持 areaY，border-box 经 mt 偏移居中）。
        float targetMarginBoxTop;
        if (mtAuto || mbAuto)
        {
            targetMarginBoxTop = areaY;
        }
        else
        {
            float marginBoxH = child.BoxModel.MarginBox.Height;
            targetMarginBoxTop = effectiveAlign switch
            {
                AlignItems.FlexEnd => areaY + areaH - marginBoxH,
                AlignItems.Center => areaY + (areaH - marginBoxH) / 2f,
                // FlexStart / Stretch / Baseline（baseline 简化为起点对齐）。
                _ => areaY,
            };
        }
        float dy = targetMarginBoxTop - child.BoxModel.MarginBox.Top;
        if (Math.Abs(dy) > 0.01f) OffsetSubtree(child, 0, dy);
    }

    /// <summary>
    /// 将 <see cref="AlignSelf"/> 映射到等效的 <see cref="AlignItems"/>；
    /// <c>Auto</c> 时回退到容器的 align-items（与 FlexLayout 同一语义）。
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

    /// <summary>
    /// 递归平移一个盒子及其全部子孙的内容区（对齐/分布产生的位移，与 FlexLayout 同一实现）。
    /// </summary>
    private static void OffsetSubtree(LayoutBox box, float dx, float dy)
    {
        var content = box.BoxModel.Content;
        box.BoxModel.Content = new RectF(content.X + dx, content.Y + dy, content.Width, content.Height);
        foreach (var child in box.Children)
            OffsetSubtree(child, dx, dy);
    }
}
