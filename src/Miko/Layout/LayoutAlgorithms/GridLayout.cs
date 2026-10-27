using Miko.Common;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// Grid 布局算法（见 ISSUE-097；ISSUE-159 补齐 repeat() / minmax() / 命名区域与命名线）。
///
/// 支持：
/// - 轨道模板 <c>grid-template-columns/rows</c>：固定长度（px/em/rem/%）、fr、auto、min-content、
///   max-content、<c>minmax(min, max)</c>、<c>fit-content(limit)</c>、
///   <c>repeat(n | auto-fill | auto-fit, …)</c> 与线名 <c>[name]</c>（见 <see cref="GridTrackSize"/>）；
///   隐式轨道由 <c>grid-auto-rows/columns</c> 定尺寸；
/// - <c>grid-template-areas</c>：命名区域决定显式网格的行列数下限，并隐式命名 <c>name-start</c> /
///   <c>name-end</c> 边线（见 <see cref="GridTemplateAreas"/>）；
/// - 子项放置 <c>grid-row/column-start/end</c>（<see cref="GridLine"/>）：线编号（负数从末尾数）、
///   第 n 条同名线、区域名、<c>span n</c>、<c>span name</c>，按 CSS §8.3.1 处理冲突；越界或负向的
///   放置在显式网格两侧产生隐式轨道；其余子项按 CSS §8.5 行优先 sparse 自动放置，<c>order</c> 参与排序；
/// - 轨道尺寸按 CSS §11 的轨道尺寸算法（<see cref="TrackSizer"/>）：初始化 base size / growth limit →
///   解析内在尺寸（单轨子项、按跨度递增处理的跨轨子项、跨弹性轨道的子项）→ 最大化轨道 → 扩展弹性
///   轨道（可用空间确定 / 不确定两种求法；容器被 min/max 夹取后按确定空间重算）→ 拉伸 auto 轨道
///   （justify/align-content 为默认时）。<c>1fr</c> 即 <c>minmax(auto, 1fr)</c>：不会小于其中子项的
///   最小内容尺寸（与浏览器一致；要允许收缩到 0 用 <c>minmax(0, 1fr)</c>）；auto-fit 放置后仍为空的
///   重复轨道折叠为 0，两侧 gap 一并折叠；
/// - 轨道分布：<c>justify-content</c> 分布列轨道、<c>align-content</c> 分布行轨道；
/// - 子项对齐：inline 轴经 justify-items / justify-self（ISSUE-101），块轴经 align-items / align-self；
///   两轴 stretch 仅当对应尺寸 auto 且非文本节点（默认 normal→stretch，填满 grid area），
///   start/center/end 下按内容尺寸（shrink-to-fit）再在 area 内对齐；auto margin 优先吸收
///   area 剩余空间（实现 margin:auto 居中）。
///
/// 已知简化：
/// - 无 <c>grid-auto-flow: column / dense</c>、subgrid、baseline 对齐（按起点）；
///   <c>grid-auto-rows/columns</c> 只取单个轨道尺寸（不支持轨道列表循环）；
/// - min-content 宽度以极窄的可用宽度（<see cref="MinContentProbeWidth"/>）预排子树、取其内容外缘测得：
///   文本在每个换行机会处换行，固定宽度的后代保持其宽度；
/// - 行方向的 min-content 与 max-content 贡献都取子项在其列区域宽度下的布局高度；
/// - 跨轨子项的空间分配实现了「先到上限、再越过上限」两段，但不区分 CSS 中按子项累计的
///   item-incurred increase 细节之外的边角（如 infinitely growable 标记一律视为可无限增长）；
/// - 子项最多被派发四次（列 max-content、按需的列 min-content、行测量、正式布局），与 flex 的
///   flex-basis 测量、table 的单元格测量同一先例；只落在固定尺寸轨道里的子项不做内容测量；
/// - 文本节点作为普通 grid 项参与放置与测量，但不拉伸（与 flex 一致）；其宽度约束取 area 宽度
///   （允许在轨道内换行）。
/// </summary>
public class GridLayout
{
    /// <summary>
    /// 测量 min-content 宽度用的可用宽度。0 在各布局算法里表示「宽度不确定」（按 max-content 收缩包裹），
    /// 所以取一个极小的正数：文本在每个换行机会处换行，自动宽度的块收到最窄，溢出部分即最小内容宽度。
    /// </summary>
    internal const float MinContentProbeWidth = 0.01f;

    /// <summary>auto-fill / auto-fit 的重复次数上限，防止极小的轨道在大容器里展开出海量轨道。</summary>
    private const int MaxAutoRepeat = 10000;

    /// <summary>已放置的 grid 子项（0 起始轨道索引 + 跨度）与本次布局内的尺寸贡献缓存。</summary>
    private sealed class GridItemInfo
    {
        public required LayoutBox Child { get; init; }
        public int ColumnStart { get; set; }
        public int ColumnSpan { get; set; }
        public int RowStart { get; set; }
        public int RowSpan { get; set; }

        // 列方向的 max-content / min-content 外尺寸（margin box），按需测量；NaN = 尚未测量。
        public float MaxContentWidth = float.NaN;
        public float MinContentWidth = float.NaN;

        // 列轨道定型后子项所在区域的宽度，以及在该宽度下的外高度（行方向贡献）；NaN = 尚未测量。
        public float AreaWidth;
        public float Height = float.NaN;

        public int Start(bool isColumn) => isColumn ? ColumnStart : RowStart;
        public int Span(bool isColumn) => isColumn ? ColumnSpan : RowSpan;
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
        // 外部已定型宽度（作为 flex 项时主轴尺寸已解析，见 ISSUE-106）：直接使用，
        // 跳过自身 width 解析与该轴 min/max 夹取，避免百分比长度二次求值。
        bool widthIsForced = constraints.ResolvedContentWidth.HasValue;

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

        // min/max-width 的内容盒像素值（未设置、或百分比针对不确定包含块时为 null）。
        float? minWidthPx = null, maxWidthPx = null;
        if (!widthIsForced && !style.MinWidth.IsAuto && !(style.MinWidth.HasPercentComponent && widthCbIndefinite))
        {
            float minW = style.MinWidth.ToPixels(constraints.AvailableWidth ?? 0, fs);
            minWidthPx = isBorderBox ? Math.Max(0, minW - horizontalExtra) : minW;
        }
        if (!widthIsForced && !style.MaxWidth.IsAuto && !(style.MaxWidth.HasPercentComponent && widthCbIndefinite))
        {
            float max = style.MaxWidth.ToPixels(constraints.AvailableWidth ?? 0, fs);
            maxWidthPx = isBorderBox ? Math.Max(0, max - horizontalExtra) : max;
        }

        // auto 宽度时提前用 min-width 抬升（收缩包裹的下限；不改变 widthIsIndefinite——
        // min-width 不使宽度对 fr/百分比轨道解析变为确定，与 ISSUE-078 的理念一致）。
        // 宽度由外部定型（flex 项主轴）时跳过 min/max-width（夹取已在 flex 完成，见 ISSUE-106）。
        if (widthIsAuto && minWidthPx is { } earlyMin)
            contentWidth = Math.Max(contentWidth, earlyMin);

        // 提前应用 max-width：轨道尺寸（fr、百分比轨道）需以夹取后的宽度为分母（同 ISSUE-081）。
        if (maxWidthPx is { } earlyMax)
            contentWidth = Math.Min(contentWidth, earlyMax);

        // 3. 计算容器高度（显式 / overflow+可用高度约束 / auto→行轨道求和）。
        float containerHeight = constraints.AvailableHeight ?? 0;
        float contentHeight;
        bool heightPercentAgainstIndefinite = style.Height.HasPercentComponent && !constraints.AvailableHeight.HasValue;
        bool heightIsAuto = style.Height.IsAuto || heightPercentAgainstIndefinite;
        // 外部已定型高度（作为列向 flex 项时主轴尺寸已解析，见 ISSUE-106）。
        bool heightIsForced = constraints.ResolvedContentHeight.HasValue;

        if (heightIsForced)
        {
            contentHeight = constraints.ResolvedContentHeight!.Value;
        }
        else if (!heightIsAuto)
        {
            contentHeight = style.Height.ToPixels(containerHeight, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
            {
                contentHeight -= box.BoxModel.Border.Vertical + box.BoxModel.Padding.Vertical;
                contentHeight = Math.Max(0, contentHeight);
            }
        }
        else if (constraints.AvailableHeight.HasValue && constraints.FillAvailableHeight &&
                 (style.OverflowY == Overflow.Auto || style.OverflowY == Overflow.Scroll || style.OverflowY == Overflow.Hidden))
        {
            // 仅当 AvailableHeight 是填充指令（grid area stretch / 列向定型 / 根视口）时使用
            // 约束高度；块流中父级定高不填充 height:auto 的盒子（见 ISSUE-105）。
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
        // overflow+可用高度约束仅在填充指令（FillAvailableHeight）下视为确定（见 ISSUE-105）。
        bool heightIsDefinite = !heightIsAuto || heightIsForced
            || (constraints.AvailableHeight.HasValue && constraints.FillAvailableHeight &&
                (style.OverflowY == Overflow.Auto || style.OverflowY == Overflow.Scroll || style.OverflowY == Overflow.Hidden));

        bool isBorderBoxH = style.BoxSizing == BoxSizing.BorderBox;
        float verticalExtra = box.BoxModel.Border.Vertical + box.BoxModel.Padding.Vertical;
        float? minHeightPx = null, maxHeightPx = null;
        if (!heightIsForced && !style.MinHeight.IsAuto)
        {
            float min = style.MinHeight.ToPixels(constraints.AvailableHeight ?? 0, fs);
            minHeightPx = isBorderBoxH ? Math.Max(0, min - verticalExtra) : min;
        }
        if (!heightIsForced && !style.MaxHeight.IsAuto)
        {
            float max = style.MaxHeight.ToPixels(constraints.AvailableHeight ?? 0, fs);
            maxHeightPx = isBorderBoxH ? Math.Max(0, max - verticalExtra) : max;
        }

        // 4. 解析 gap：columnGap 相对容器宽度、rowGap 相对容器高度（各自轴的内容尺寸）。
        float columnGap = (style.ColumnGap.IsAuto ? style.Gap : style.ColumnGap).ToPixels(contentWidth, fs);
        float rowGap = (style.RowGap.IsAuto ? style.Gap : style.RowGap).ToPixels(contentHeight, fs);

        // 5. 收集在流子项（含文本节点），按 order 稳定排序。
        var items = CollectGridItems(box);

        // 6. 显式网格：展开 repeat()（auto-fill/auto-fit 的次数取决于该轴的容器尺寸）、收集线名，
        // 再按 grid-template-areas 扩大并添加区域的隐式边线名。
        var colAxis = ExplicitAxis.Build(style.GridTemplateColumns, style.GridTemplateAreas, isColumn: true,
            style.GridAutoColumns,
            AutoRepeatSpace(!widthIsIndefinite, contentWidth, minWidthPx, maxWidthPx), columnGap, fs);
        var rowAxis = ExplicitAxis.Build(style.GridTemplateRows, style.GridTemplateAreas, isColumn: false,
            style.GridAutoRows,
            AutoRepeatSpace(heightIsDefinite, contentHeight, minHeightPx, maxHeightPx), rowGap, fs);

        // 7. 放置子项（显式定位 + 行优先 sparse 自动放置），得出最终轨道数与显式网格前的隐式轨道数。
        var placed = PlaceItems(items, colAxis, rowAxis,
            out int colCount, out int rowCount, out int colOffset, out int rowOffset);

        // 8. 列轨道尺寸。宽度不确定（收缩包裹）时：百分比轨道退化为 auto，fr 按内容求 fr 尺寸。
        var colSizer = new TrackSizer(
            BuildTrackFunctions(colAxis, colOffset, colCount, style.GridAutoColumns, !widthIsIndefinite, contentWidth, fs),
            CollapsedTracks(colAxis, colOffset, colCount, placed, isColumn: true),
            columnGap, isColumn: true);
        colSizer.Size(placed, widthIsIndefinite ? null : contentWidth);

        // 9. 宽度收缩包裹：列轨道总宽（含 gap），再按 min/max-width 夹取。夹取改变了网格宽度且有弹性
        // 轨道时，按夹取后的确定宽度重新扩展弹性轨道（CSS §11.7）。
        if (widthIsIndefinite)
        {
            contentWidth = Math.Max(contentWidth, colSizer.Total);
            if (minWidthPx is { } minW) contentWidth = Math.Max(contentWidth, minW);
            if (maxWidthPx is { } maxW) contentWidth = Math.Min(contentWidth, maxW);
            if (colSizer.HasFlexible && Math.Abs(contentWidth - colSizer.Total) > 0.01f)
                colSizer.RedoFlexible(contentWidth);
        }

        // 9b. justify-content 为默认（Normal 为 CSS 初始值；FlexStart 与其行为一致）：拉伸 auto 列轨道
        // 填满剩余空间（CSS §11.8 Stretch auto Tracks——无模板单列 auto 轨道据此填满容器宽度）。
        // 须在行轨道测量之前完成，使行测量使用拉伸后的最终列宽（文本/拉伸子项在正确宽度下换行）。
        if (style.JustifyContent is JustifyContent.Normal or JustifyContent.FlexStart)
            colSizer.StretchAutoTracks(contentWidth - colSizer.Total);

        // 10. 解析容器自身的水平 auto margin（相对其包含块居中，与 FlexLayout 2d 对称）。
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

        // 11. 内容区原点与列轨道位置：justify-content 的非默认模式在容器内容宽度超出网格总宽时分布轨道。
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;
        var colSizes = colSizer.Sizes;
        var colPos = colSizer.Positions(contentX, contentWidth, ColumnDistribution(style.JustifyContent));

        // 12. 行轨道尺寸：列轨道已定型，行方向贡献取子项在其列区域宽度下的布局高度。
        foreach (var item in placed)
        {
            int colEnd = item.ColumnStart + item.ColumnSpan - 1;
            item.AreaWidth = colPos[colEnd] + colSizes[colEnd] - colPos[item.ColumnStart];
        }
        var rowSizer = new TrackSizer(
            BuildTrackFunctions(rowAxis, rowOffset, rowCount, style.GridAutoRows, heightIsDefinite, contentHeight, fs),
            CollapsedTracks(rowAxis, rowOffset, rowCount, placed, isColumn: false),
            rowGap, isColumn: false);
        rowSizer.Size(placed, heightIsDefinite ? contentHeight : null);

        // 13. auto 高度：行轨道总高（含 gap）。
        // 外部定型高度（列向 flex 项主轴尺寸）不被行轨道求和覆盖（见 ISSUE-106）。
        if (!heightIsForced && heightIsAuto)
        {
            contentHeight = rowSizer.Total;
        }

        // 14. 尾部 min/max 约束（与 FlexLayout 步骤 6/7 对称；百分比 min/max-width 对不确定包含块退化）。
        // 由外部定型的轴跳过对应夹取（已在 flex 主轴解析中完成，见 ISSUE-106）。
        if (minHeightPx is { } minH) contentHeight = Math.Max(contentHeight, minH);
        if (maxHeightPx is { } maxH) contentHeight = Math.Min(contentHeight, maxH);
        if (minWidthPx is { } minWidth) contentWidth = Math.Max(contentWidth, minWidth);
        if (maxWidthPx is { } maxWidth) contentWidth = Math.Min(contentWidth, maxWidth);

        // 14b. 高度不确定、被 min/max-height 夹取后与行轨道总高不同：按夹取后的确定高度重新扩展
        // 弹性行轨道（CSS §11.7，与步骤 9 对称）。
        if (!heightIsDefinite && rowSizer.HasFlexible && Math.Abs(contentHeight - rowSizer.Total) > 0.01f)
            rowSizer.RedoFlexible(contentHeight);

        // 14c. align-content 为默认（Normal 为 CSS 初始值，行为同 Stretch；FlexStart 与其一致）
        // 或显式 Stretch：拉伸 auto 行轨道填满剩余空间（与 9b 同一规则；须在 min/max 夹取之后，
        // 使 min-height 撑出的剩余空间也能被 auto 行吸收）。
        if (style.AlignContent is AlignContent.Normal or AlignContent.FlexStart or AlignContent.Stretch)
            rowSizer.StretchAutoTracks(contentHeight - rowSizer.Total);

        // 15. 行轨道位置：align-content 的非默认模式在容器内容高度超出网格总高时分布轨道。
        var rowSizes = rowSizer.Sizes;
        var rowPos = rowSizer.Positions(contentY, contentHeight, RowDistribution(style.AlignContent));

        // 16. 脱离文档流的子元素（absolute/fixed）不是 grid 项目：不占格，单独布局以获得尺寸，
        // 最终位置由 LayoutEngine 的定位阶段修正（与 FlexLayout 的处理一致）。
        // width:auto 时传入无限宽约束触发 shrink-to-fit（与 BlockLayout 的脱离流处理一致）。
        float? outOfFlowHeight = contentHeight > 0 ? contentHeight : (float?)null;
        foreach (var child in box.Children)
        {
            if (BlockLayout.IsOutOfFlow(child))
            {
                float? outOfFlowWidth = child.ComputedStyle.Width.IsAuto
                    ? null
                    : contentWidth > 0 ? contentWidth : (float?)null;
                LayoutDispatcher.Dispatch(child, new LayoutConstraints(outOfFlowWidth, outOfFlowHeight), contentX, contentY);
            }
        }

        // 17. 布局各 grid 子项到其 area。
        foreach (var item in placed)
        {
            LayoutItem(item, colPos, colSizes, rowPos, rowSizes, style.AlignItems, style.JustifyItems);
        }

        // 18. 内容区与可滚动内容尺寸（滚动区域包含内边距，与 Block/Flex 一致）。
        box.BoxModel.Content = new RectF(contentX, contentY, contentWidth, contentHeight);
        box.ScrollableContentWidth = colSizer.Total + box.BoxModel.Padding.Horizontal;
        box.ScrollableContentHeight = rowSizer.Total + box.BoxModel.Padding.Vertical;
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

    // ---------------------------------------------------------------------
    // 显式网格：repeat() 展开、线名、区域
    // ---------------------------------------------------------------------

    /// <summary>
    /// auto-fill / auto-fit 计算重复次数所用的空间：容器该轴尺寸确定时取之（放下尽可能多的次数）；
    /// 否则取 max 尺寸（同样取放得下的最多次数），再否则取 min 尺寸（取填满它的最少次数）；都没有为 null（1 次）。
    /// </summary>
    private static (float Size, bool IsMinimum)? AutoRepeatSpace(bool definite, float size, float? min, float? max)
    {
        if (definite) return (size, false);
        if (max is { } m) return (m, false);
        if (min is { } n) return (n, true);
        return null;
    }

    /// <summary>
    /// 一条轴的显式网格：<c>repeat()</c> 展开后的轨道尺寸、每条显式线的名字（<see cref="LineNames"/>
    /// 比轨道多一项，第 i 项是第 i 条线、即 1 起始编号 i+1 的线），以及来自 <c>repeat(auto-fit, …)</c>
    /// 的轨道标记（放置后仍为空者折叠）。
    /// </summary>
    private sealed class ExplicitAxis
    {
        public List<GridTrackSize> Tracks { get; } = new();
        public List<List<string>?> LineNames { get; } = new() { null };
        public List<bool>? AutoFit { get; private set; }

        public int Count => Tracks.Count;

        public static ExplicitAxis Build(
            List<GridTrackSize>? template, GridTemplateAreas? areas, bool isColumn, GridTrackSize autoTrack,
            (float Size, bool IsMinimum)? repeatSpace, float gap, float fs)
        {
            var axis = new ExplicitAxis();
            if (template != null)
            {
                int autoRepeats = 1;
                foreach (var entry in template)
                {
                    if (entry.AutoRepeat != null)
                    {
                        autoRepeats = ComputeAutoRepeatCount(template, repeatSpace, gap, fs);
                        break;
                    }
                }

                // CSS 只允许一个自动重复；多出的按一次展开（宽容处理，不使整个列表失效）。
                bool autoRepeatSeen = false;
                foreach (var entry in template)
                {
                    if (entry.IsLineNames)
                    {
                        axis.AddNames(entry.Names);
                    }
                    else if (entry.IsRepeat)
                    {
                        int count = entry.RepeatCount;
                        bool autoFit = false;
                        if (entry.AutoRepeat is { } mode)
                        {
                            count = autoRepeatSeen ? 1 : autoRepeats;
                            autoFit = !autoRepeatSeen && mode == GridAutoRepeat.AutoFit;
                            autoRepeatSeen = true;
                        }
                        for (int r = 0; r < count; r++)
                        {
                            foreach (var t in entry.RepeatTracks)
                            {
                                if (t.IsLineNames) axis.AddNames(t.Names);
                                else axis.AddTrack(t, autoFit);
                            }
                        }
                    }
                    else
                    {
                        axis.AddTrack(entry, autoFit: false);
                    }
                }
            }

            if (areas != null)
            {
                // 区域定义的行列数多于模板轨道时，多出的仍是显式轨道，按 grid-auto-* 定尺寸。
                int areaTracks = isColumn ? areas.ColumnCount : areas.RowCount;
                while (axis.Count < areaTracks) axis.AddTrack(autoTrack, autoFit: false);

                foreach (var name in areas.AreaNames)
                {
                    areas.TryGetArea(name, out int rowStart, out int colStart, out int rowEnd, out int colEnd);
                    axis.AddName(isColumn ? colStart : rowStart, name + "-start");
                    axis.AddName(isColumn ? colEnd : rowEnd, name + "-end");
                }
            }

            return axis;
        }

        private void AddTrack(GridTrackSize track, bool autoFit)
        {
            if (autoFit)
            {
                AutoFit ??= Enumerable.Repeat(false, Tracks.Count).ToList();
            }
            Tracks.Add(track);
            LineNames.Add(null);
            AutoFit?.Add(autoFit);
        }

        private void AddNames(IReadOnlyList<string> names)
        {
            foreach (var name in names) AddName(LineNames.Count - 1, name);
        }

        private void AddName(int line, string name)
        {
            var names = LineNames[line] ??= new List<string>(1);
            if (!names.Contains(name)) names.Add(name);
        }

        /// <summary>显式线 <paramref name="index"/>（0 起始）是否带名字 <paramref name="name"/>。</summary>
        public bool HasName(int index, string name) => LineNames[index]?.Contains(name) == true;

        /// <summary>显式线 <paramref name="index"/> 是否带名字 <c>{name}{suffix}</c>（不拼接字符串）。</summary>
        public bool HasName(int index, string name, string suffix)
        {
            var names = LineNames[index];
            if (names == null) return false;
            foreach (var n in names)
            {
                if (n.Length == name.Length + suffix.Length
                    && n.StartsWith(name, StringComparison.Ordinal)
                    && n.EndsWith(suffix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public bool IsAutoFit(int track) => AutoFit != null && AutoFit[track];
    }

    /// <summary>
    /// auto-fill / auto-fit 的重复次数：被重复的轨道各取其固定上限（没有则取固定下限），连同 gap 计入，
    /// 求放得下的最多次数（<paramref name="space"/> 为下限时求填满它的最少次数），至少 1 次。
    /// </summary>
    private static int ComputeAutoRepeatCount(List<GridTrackSize> template, (float Size, bool IsMinimum)? space, float gap, float fs)
    {
        if (space is not { } s) return 1;

        float otherSum = 0, repeatSum = 0;
        int otherCount = 0, repeatCount = 0;
        bool autoRepeatSeen = false;
        foreach (var entry in template)
        {
            if (entry.IsLineNames) continue;
            if (!entry.IsRepeat)
            {
                otherSum += DefiniteTrackSize(entry, s.Size, fs);
                otherCount++;
                continue;
            }

            bool isTheAutoRepeat = entry.AutoRepeat != null && !autoRepeatSeen;
            int times = entry.AutoRepeat != null ? 1 : entry.RepeatCount;
            if (entry.AutoRepeat != null) autoRepeatSeen = true;
            foreach (var t in entry.RepeatTracks)
            {
                if (t.IsLineNames) continue;
                float size = DefiniteTrackSize(t, s.Size, fs);
                if (isTheAutoRepeat)
                {
                    repeatSum += size;
                    repeatCount++;
                }
                else
                {
                    otherSum += size * times;
                    otherCount += times;
                }
            }
        }

        // 总尺寸(n) = otherSum + n·repeatSum + gap·(otherCount + n·repeatCount − 1)。
        float unit = repeatSum + gap * repeatCount;
        if (unit <= 0.01f) return 1;
        float room = s.Size - otherSum - gap * (otherCount - 1);
        int n = s.IsMinimum
            ? (int)MathF.Ceiling(room / unit - 0.001f)
            : (int)MathF.Floor(room / unit + 0.001f);
        return Math.Clamp(n, 1, MaxAutoRepeat);
    }

    /// <summary>计算自动重复次数时一条轨道的确定尺寸：固定上限，否则固定下限（二者都有时上限不低于下限），否则 0。</summary>
    private static float DefiniteTrackSize(GridTrackSize track, float basis, float fs)
    {
        if (track.IsFixed) return Math.Max(0, track.Fixed.ToPixels(basis, fs));
        if (!track.IsMinMax) return 0;
        float min = track.Min.IsFixed ? Math.Max(0, track.Min.Fixed.ToPixels(basis, fs)) : 0;
        return track.Max.IsFixed ? Math.Max(min, track.Max.Fixed.ToPixels(basis, fs)) : min;
    }

    // ---------------------------------------------------------------------
    // 放置
    // ---------------------------------------------------------------------

    /// <summary>一条轴上解析后的放置：确定时为起始线（1 起始，可 ≤ 0 或越过显式网格）与跨度，否则只有跨度。</summary>
    private readonly record struct AxisPlacement(bool IsDefinite, int StartLine, int Span);

    /// <summary>
    /// 放置全部子项（CSS §8.5，行优先 sparse）：
    /// ① 两轴都确定的直接放置；② 行确定、列自动的按行就地找位（同一行内不回退）；
    /// ③ 确定隐式网格的列数（显式列、确定列位置的子项、自动子项的最大列跨度）；
    /// ④ 其余按自动放置游标：列确定者在该列向下找行，全自动者行内向右找、放不下换行。
    /// 线编号先在「显式网格从 1 起」的坐标中解析（可能 ≤ 0 或越过显式网格），再整体平移到 0 起始的轨道
    /// 索引：<paramref name="colOffset"/> / <paramref name="rowOffset"/> 是显式网格之前的隐式轨道数。
    /// </summary>
    private static List<GridItemInfo> PlaceItems(
        List<LayoutBox> items, ExplicitAxis colAxis, ExplicitAxis rowAxis,
        out int colCount, out int rowCount, out int colOffset, out int rowOffset)
    {
        var colPlacements = new AxisPlacement[items.Count];
        var rowPlacements = new AxisPlacement[items.Count];
        int minColLine = 1, minRowLine = 1;
        for (int i = 0; i < items.Count; i++)
        {
            var cs = items[i].ComputedStyle;
            colPlacements[i] = ResolveAxis(cs.GridColumnStart, cs.GridColumnEnd, colAxis);
            rowPlacements[i] = ResolveAxis(cs.GridRowStart, cs.GridRowEnd, rowAxis);
            if (colPlacements[i].IsDefinite) minColLine = Math.Min(minColLine, colPlacements[i].StartLine);
            if (rowPlacements[i].IsDefinite) minRowLine = Math.Min(minRowLine, rowPlacements[i].StartLine);
        }

        // 线 L（1 起始）→ 轨道索引 L − minLine：负向放置在显式网格之前产生的隐式轨道从索引 0 起。
        colOffset = 1 - minColLine;
        rowOffset = 1 - minRowLine;

        var infos = new GridItemInfo[items.Count];
        // 占用表：行索引 → 已占列集合，按需增长（隐式行）。
        var occupancy = new List<HashSet<int>>();
        int maxColEnd = colOffset + colAxis.Count;
        int maxRowEnd = rowOffset + rowAxis.Count;

        for (int i = 0; i < items.Count; i++)
        {
            infos[i] = new GridItemInfo
            {
                Child = items[i],
                ColumnStart = colPlacements[i].IsDefinite ? colPlacements[i].StartLine - minColLine : 0,
                ColumnSpan = colPlacements[i].Span,
                RowStart = rowPlacements[i].IsDefinite ? rowPlacements[i].StartLine - minRowLine : 0,
                RowSpan = rowPlacements[i].Span,
            };
        }

        void Place(GridItemInfo info)
        {
            MarkOccupied(occupancy, info.RowStart, info.ColumnStart, info.RowSpan, info.ColumnSpan);
            maxColEnd = Math.Max(maxColEnd, info.ColumnStart + info.ColumnSpan);
            maxRowEnd = Math.Max(maxRowEnd, info.RowStart + info.RowSpan);
        }

        // ① 两轴都确定。
        for (int i = 0; i < items.Count; i++)
        {
            if (colPlacements[i].IsDefinite && rowPlacements[i].IsDefinite) Place(infos[i]);
        }

        // ② 行确定、列自动：在该行内找最早的空位，且不早于本步骤先前放进同一行的子项（sparse）。
        Dictionary<int, int>? rowCursors = null;
        for (int i = 0; i < items.Count; i++)
        {
            if (colPlacements[i].IsDefinite || !rowPlacements[i].IsDefinite) continue;
            var info = infos[i];
            rowCursors ??= new Dictionary<int, int>();
            int c = rowCursors.TryGetValue(info.RowStart, out var cursor) ? cursor : 0;
            while (!AreaFree(occupancy, info.RowStart, c, info.RowSpan, info.ColumnSpan)) c++;
            info.ColumnStart = c;
            rowCursors[info.RowStart] = c + info.ColumnSpan;
            Place(info);
        }

        // ③ 隐式网格的列数：已放置者与「列确定、行自动」者的列终点，以及自动列子项的最大跨度。
        colCount = maxColEnd;
        for (int i = 0; i < items.Count; i++)
        {
            if (colPlacements[i].IsDefinite)
                colCount = Math.Max(colCount, infos[i].ColumnStart + infos[i].ColumnSpan);
            else
                colCount = Math.Max(colCount, infos[i].ColumnSpan);
        }

        // ④ 自动放置游标（sparse：游标只前进不回退）。
        int cursorRow = 0, cursorCol = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (rowPlacements[i].IsDefinite) continue;
            var info = infos[i];

            if (colPlacements[i].IsDefinite)
            {
                // 列确定：游标移到该列，若在原游标左侧则先换行；再向下找不重叠的行。
                if (info.ColumnStart < cursorCol) cursorRow++;
                cursorCol = info.ColumnStart;
                while (!AreaFree(occupancy, cursorRow, cursorCol, info.RowSpan, info.ColumnSpan)) cursorRow++;
            }
            else
            {
                // 全自动：行内向右找空位，放不下（越过列数）就换到下一行行首。
                while (true)
                {
                    if (cursorCol + info.ColumnSpan > colCount)
                    {
                        cursorRow++;
                        cursorCol = 0;
                        continue;
                    }
                    if (AreaFree(occupancy, cursorRow, cursorCol, info.RowSpan, info.ColumnSpan)) break;
                    cursorCol++;
                }
                info.ColumnStart = cursorCol;
            }

            info.RowStart = cursorRow;
            Place(info);
            if (!colPlacements[i].IsDefinite) cursorCol += info.ColumnSpan;
        }

        colCount = Math.Max(colCount, maxColEnd);
        rowCount = maxRowEnd;

        // 每个子项都已放置；按 order 修正后的文档序返回。
        return new List<GridItemInfo>(infos);
    }

    /// <summary>
    /// 解析一条轴的放置（CSS §8.3 / §8.3.1）：两侧都是 span 时去掉结束侧；两侧线相同时结束线后移一条；
    /// 起始线大于结束线时交换；只有一侧确定时另一侧按跨度（默认 1）推出；两侧都不确定时自动放置——
    /// 按名跨度此时按 1 处理。
    /// </summary>
    private static AxisPlacement ResolveAxis(GridLine start, GridLine end, ExplicitAxis axis)
    {
        if (start.IsSpan && end.IsSpan) end = GridLine.Auto;

        bool startDefinite = !start.IsAuto && !start.IsSpan;
        bool endDefinite = !end.IsAuto && !end.IsSpan;

        if (startDefinite && endDefinite)
        {
            int s = ResolveLine(start, axis, isStart: true);
            int e = ResolveLine(end, axis, isStart: false);
            if (s > e) (s, e) = (e, s);
            if (s == e) e = s + 1;
            return new AxisPlacement(true, s, e - s);
        }

        if (startDefinite)
        {
            int s = ResolveLine(start, axis, isStart: true);
            int span = !end.IsSpan ? 1
                : end.Name == null ? end.Number
                : SpanToNamedLine(axis, s, end.Name, end.Number, forward: true) - s;
            return new AxisPlacement(true, s, span);
        }

        if (endDefinite)
        {
            int e = ResolveLine(end, axis, isStart: false);
            int span = !start.IsSpan ? 1
                : start.Name == null ? start.Number
                : e - SpanToNamedLine(axis, e, start.Name, start.Number, forward: false);
            return new AxisPlacement(true, e - span, span);
        }

        var spanLine = start.IsSpan ? start : end;
        int autoSpan = spanLine.IsSpan && spanLine.Name == null ? spanLine.Number : 1;
        return new AxisPlacement(false, 0, autoSpan);
    }

    /// <summary>
    /// 把一条确定的线解析为 1 起始的线编号（显式网格的线为 1..Count+1，之外为隐式线）。
    /// 只写名字时先按区域边线（<c>name-start</c> / <c>name-end</c>）找，再找第一条同名线。
    /// </summary>
    private static int ResolveLine(GridLine line, ExplicitAxis axis, bool isStart)
    {
        int lineCount = axis.Count + 1;
        if (line.Name == null)
            return line.Number > 0 ? line.Number : lineCount + 1 + line.Number;

        if (line.IsBareName)
        {
            string suffix = isStart ? "-start" : "-end";
            for (int i = 0; i < lineCount; i++)
            {
                if (axis.HasName(i, line.Name, suffix)) return i + 1;
            }
            return NthNamedLine(axis, line.Name, 1);
        }

        return NthNamedLine(axis, line.Name, line.Number);
    }

    /// <summary>
    /// 第 <paramref name="n"/> 条名为 <paramref name="name"/> 的线（负数从末尾倒数）。显式网格里不够数时，
    /// 显式网格之外（按 n 的方向）的隐式线都视为带有该名字。
    /// </summary>
    private static int NthNamedLine(ExplicitAxis axis, string name, int n)
    {
        int lineCount = axis.Count + 1;
        int found = 0;
        if (n > 0)
        {
            for (int i = 0; i < lineCount; i++)
            {
                if (axis.HasName(i, name) && ++found == n) return i + 1;
            }
            return lineCount + (n - found);
        }

        for (int i = lineCount - 1; i >= 0; i--)
        {
            if (axis.HasName(i, name) && ++found == -n) return i + 1;
        }
        return 1 - (-n - found);
    }

    /// <summary>
    /// <c>span name n</c>：从线 <paramref name="from"/> 出发沿 <paramref name="forward"/> 方向找第
    /// <paramref name="n"/> 条名为 <paramref name="name"/> 的线；不够数时，该方向上显式网格之外的隐式线
    /// 都视为带有该名字。
    /// </summary>
    private static int SpanToNamedLine(ExplicitAxis axis, int from, string name, int n, bool forward)
    {
        int lineCount = axis.Count + 1;
        int found = 0;
        if (forward)
        {
            for (int line = Math.Max(from + 1, 1); line <= lineCount; line++)
            {
                if (axis.HasName(line - 1, name) && ++found == n) return line;
            }
            return Math.Max(from, lineCount) + (n - found);
        }

        for (int line = Math.Min(from - 1, lineCount); line >= 1; line--)
        {
            if (axis.HasName(line - 1, name) && ++found == n) return line;
        }
        return Math.Min(from, 1) - (n - found);
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

    // ---------------------------------------------------------------------
    // 轨道尺寸函数
    // ---------------------------------------------------------------------

    /// <summary>轨道尺寸函数一端的类别（CSS min / max track sizing function）。</summary>
    private enum Breadth : byte { Fixed, Auto, MinContent, MaxContent, Flex, FitContent }

    /// <summary>一条轨道解析后的尺寸函数：下限与上限各一个 <see cref="Breadth"/>。</summary>
    private readonly record struct TrackFunction(Breadth MinKind, float MinValue, Breadth MaxKind, float MaxValue)
    {
        // MinValue：Fixed 时为像素。MaxValue：Fixed / FitContent 时为像素，Flex 时为 fr 系数。
        public bool IsFlexible => MaxKind == Breadth.Flex;
        public bool HasIntrinsicMin => MinKind is Breadth.Auto or Breadth.MinContent or Breadth.MaxContent;
        public bool HasIntrinsicMax => MaxKind is Breadth.Auto or Breadth.MinContent or Breadth.MaxContent or Breadth.FitContent;

        public static TrackFunction Auto => new(Breadth.Auto, 0, Breadth.Auto, 0);
    }

    /// <summary>
    /// 最终网格中每条轨道的尺寸函数：显式网格之内取展开后的模板轨道，之外（前后隐式轨道）取 grid-auto-*。
    /// 百分比相对 <paramref name="available"/> 解析；该轴尺寸不确定时百分比按 auto 处理（与 ISSUE-077 一致）。
    /// </summary>
    private static TrackFunction[] BuildTrackFunctions(
        ExplicitAxis axis, int offset, int count, GridTrackSize autoTrack, bool definite, float available, float fs)
    {
        var fns = new TrackFunction[count];
        for (int i = 0; i < count; i++)
        {
            int explicitIndex = i - offset;
            var track = explicitIndex >= 0 && explicitIndex < axis.Count ? axis.Tracks[explicitIndex] : autoTrack;
            fns[i] = Resolve(track, definite, available, fs);
        }
        return fns;
    }

    private static TrackFunction Resolve(GridTrackSize track, bool definite, float available, float fs)
    {
        switch (track.Kind)
        {
            case GridTrackSize.TrackKind.Fixed:
            {
                if (track.Fixed.HasPercentComponent && !definite) return TrackFunction.Auto;
                float px = Math.Max(0, track.Fixed.ToPixels(available, fs));
                return new TrackFunction(Breadth.Fixed, px, Breadth.Fixed, px);
            }
            case GridTrackSize.TrackKind.Fraction:
                return new TrackFunction(Breadth.Auto, 0, Breadth.Flex, Math.Max(0, track.Fraction));
            case GridTrackSize.TrackKind.MinContent:
                return new TrackFunction(Breadth.MinContent, 0, Breadth.MinContent, 0);
            case GridTrackSize.TrackKind.MaxContent:
                return new TrackFunction(Breadth.MaxContent, 0, Breadth.MaxContent, 0);
            case GridTrackSize.TrackKind.FitContent:
                // fit-content(百分比) 在不确定轴上无从解析上限，按 max-content（即 auto 的上限）处理。
                return track.Fixed.HasPercentComponent && !definite
                    ? TrackFunction.Auto
                    : new TrackFunction(Breadth.Auto, 0, Breadth.FitContent, Math.Max(0, track.Fixed.ToPixels(available, fs)));
            case GridTrackSize.TrackKind.MinMax:
            {
                var (minKind, minValue) = ResolveBreadth(track.Min, definite, available, fs);
                var (maxKind, maxValue) = ResolveBreadth(track.Max, definite, available, fs);
                return new TrackFunction(minKind, minValue, maxKind, maxValue);
            }
            default:
                // auto，以及误用在 grid-auto-* 里的 repeat() / 线名。
                return TrackFunction.Auto;
        }
    }

    private static (Breadth Kind, float Value) ResolveBreadth(GridTrackSize breadth, bool definite, float available, float fs)
        => breadth.Kind switch
        {
            GridTrackSize.TrackKind.Fixed when breadth.Fixed.HasPercentComponent && !definite => (Breadth.Auto, 0),
            GridTrackSize.TrackKind.Fixed => (Breadth.Fixed, Math.Max(0, breadth.Fixed.ToPixels(available, fs))),
            GridTrackSize.TrackKind.Fraction => (Breadth.Flex, Math.Max(0, breadth.Fraction)),
            GridTrackSize.TrackKind.MinContent => (Breadth.MinContent, 0),
            GridTrackSize.TrackKind.MaxContent => (Breadth.MaxContent, 0),
            _ => (Breadth.Auto, 0),
        };

    /// <summary>auto-fit 展开出的重复轨道中放置后仍无子项者（折叠为 0，两侧 gap 一并折叠）。</summary>
    private static bool[] CollapsedTracks(ExplicitAxis axis, int offset, int count, List<GridItemInfo> items, bool isColumn)
    {
        var collapsed = new bool[count];
        if (axis.AutoFit == null) return collapsed;

        for (int i = 0; i < count; i++)
        {
            int explicitIndex = i - offset;
            collapsed[i] = explicitIndex >= 0 && explicitIndex < axis.Count && axis.IsAutoFit(explicitIndex);
        }
        foreach (var item in items)
        {
            for (int t = item.Start(isColumn); t < item.Start(isColumn) + item.Span(isColumn); t++)
                collapsed[t] = false;
        }
        return collapsed;
    }

    // ---------------------------------------------------------------------
    // 轨道尺寸算法（CSS Grid §11）
    // ---------------------------------------------------------------------

    /// <summary>轨道分布模式（justify-content / align-content 映射到轨道上）。</summary>
    private enum Distribution : byte { Start, End, Center, SpaceBetween, SpaceAround, SpaceEvenly }

    private static Distribution ColumnDistribution(JustifyContent justify) => justify switch
    {
        // grid 无 reverse 方向，故绝对关键字 Start/End 与 FlexStart/FlexEnd 行为完全一致。
        JustifyContent.FlexEnd or JustifyContent.End => Distribution.End,
        JustifyContent.Center => Distribution.Center,
        JustifyContent.SpaceBetween => Distribution.SpaceBetween,
        JustifyContent.SpaceAround => Distribution.SpaceAround,
        JustifyContent.SpaceEvenly => Distribution.SpaceEvenly,
        // Normal/FlexStart/Start：自起点排布（auto 轨道拉伸已在步骤 9b 处理）。
        _ => Distribution.Start,
    };

    private static Distribution RowDistribution(AlignContent align) => align switch
    {
        AlignContent.FlexEnd => Distribution.End,
        AlignContent.Center => Distribution.Center,
        AlignContent.SpaceBetween => Distribution.SpaceBetween,
        AlignContent.SpaceAround => Distribution.SpaceAround,
        // Normal/FlexStart/Stretch：自起点排布（auto 轨道拉伸已在步骤 14c 处理）。
        _ => Distribution.Start,
    };

    /// <summary>
    /// 一条轴的轨道尺寸（CSS Grid §11.3–§11.8）。<see cref="Size"/> 跑完 §11.4–§11.7，容器定型后由调用方
    /// 视需要调用 <see cref="RedoFlexible"/>（容器被 min/max 夹取）与 <see cref="StretchAutoTracks"/>（§11.8）。
    /// </summary>
    private sealed class TrackSizer
    {
        private enum Step : byte { IntrinsicMinimums, ContentMinimums, MaxContentMinimums, IntrinsicMaximums, MaxContentMaximums }

        // 逐轨道临时数组在此长度以内用栈分配；auto-fill 可展开出上千条轨道，超出时改用堆。
        private const int StackLimit = 128;

        private readonly TrackFunction[] _fns;
        private readonly bool[] _collapsed;
        private readonly float _gap;
        private readonly bool _isColumn;
        private readonly float[] _base;
        private readonly float[] _growth;
        private bool _indefinite;
        private List<GridItemInfo> _items = new();

        // 弹性轨道扩展前的 base size（容器被 min/max 夹取后按确定空间重新扩展时还原用）。
        private float[]? _preFlexBase;

        public TrackSizer(TrackFunction[] fns, bool[] collapsed, float gap, bool isColumn)
        {
            _fns = fns;
            _collapsed = collapsed;
            _gap = gap;
            _isColumn = isColumn;
            _base = new float[fns.Length];
            _growth = new float[fns.Length];
        }

        /// <summary>各轨道的最终尺寸（即 base size）。</summary>
        public float[] Sizes => _base;

        public bool HasFlexible
        {
            get
            {
                for (int i = 0; i < _fns.Length; i++)
                    if (_fns[i].IsFlexible && !_collapsed[i]) return true;
                return false;
            }
        }

        /// <summary>网格在该轴的总尺寸：未折叠轨道之和，加上它们之间的 gap。</summary>
        public float Total => SumRange(0, _fns.Length, _base);

        /// <param name="available">该轴的确定可用空间；null 表示不确定（按 max-content 定尺寸）。</param>
        public void Size(List<GridItemInfo> items, float? available)
        {
            _items = items;
            _indefinite = available == null;

            // §11.4 初始化：固定下限为 base size，固定上限为 growth limit，其余 base 为 0、growth 为无穷；
            // growth 不小于 base。折叠的轨道恒为 0。
            for (int i = 0; i < _fns.Length; i++)
            {
                if (_collapsed[i])
                {
                    _base[i] = _growth[i] = 0;
                    continue;
                }
                var fn = _fns[i];
                _base[i] = fn.MinKind == Breadth.Fixed ? fn.MinValue : 0;
                _growth[i] = fn.MaxKind == Breadth.Fixed ? fn.MaxValue : float.PositiveInfinity;
                if (_growth[i] < _base[i]) _growth[i] = _base[i];
            }

            if (items.Count > 0) ResolveIntrinsicSizes();

            // §11.5 末尾：仍为无穷的 growth limit 取 base size。
            for (int i = 0; i < _fns.Length; i++)
            {
                if (float.IsPositiveInfinity(_growth[i])) _growth[i] = _base[i];
            }

            MaximizeTracks(available);

            if (HasFlexible)
            {
                _preFlexBase = (float[])_base.Clone();
                ExpandFlexibleTracks(available);
            }
        }

        /// <summary>容器尺寸被 min/max 夹取后，以 <paramref name="space"/> 为确定空间重新扩展弹性轨道。</summary>
        public void RedoFlexible(float space)
        {
            if (_preFlexBase == null) return;
            Array.Copy(_preFlexBase, _base, _base.Length);
            ExpandFlexibleTracks(space);
        }

        /// <summary>§11.8：把正的剩余空间均分给上限为 auto 的轨道。</summary>
        public void StretchAutoTracks(float free)
        {
            if (free <= 0.01f) return;
            int count = 0;
            for (int i = 0; i < _fns.Length; i++)
                if (_fns[i].MaxKind == Breadth.Auto && !_collapsed[i]) count++;
            if (count == 0) return;

            float grow = free / count;
            for (int i = 0; i < _fns.Length; i++)
                if (_fns[i].MaxKind == Breadth.Auto && !_collapsed[i]) _base[i] += grow;
        }

        /// <summary>
        /// 各轨道起点：自 <paramref name="start"/> 依次排布，未折叠轨道之间隔 gap；网格总尺寸小于
        /// <paramref name="containerSize"/> 时按 <paramref name="distribution"/> 整体偏移或在轨道间加距
        /// （free ≤ 0 时不调整，保持自起点排布）。折叠的轨道位于前一条轨道的末端。
        /// </summary>
        public float[] Positions(float start, float containerSize, Distribution distribution)
        {
            int n = _fns.Length;
            var positions = new float[n];
            if (n == 0) return positions;

            int visible = 0;
            for (int i = 0; i < n; i++) if (!_collapsed[i]) visible++;

            float free = containerSize - Total;
            float offset = 0, extra = 0;
            if (free > 0.01f && visible > 0)
            {
                switch (distribution)
                {
                    case Distribution.End: offset = free; break;
                    case Distribution.Center: offset = free / 2f; break;
                    case Distribution.SpaceBetween:
                        if (visible > 1) extra = free / (visible - 1);
                        break;
                    case Distribution.SpaceAround:
                        extra = free / visible;
                        offset = extra / 2f;
                        break;
                    case Distribution.SpaceEvenly:
                        extra = free / (visible + 1);
                        offset = extra;
                        break;
                }
            }

            float pos = start + offset;
            bool first = true;
            for (int i = 0; i < n; i++)
            {
                if (_collapsed[i])
                {
                    positions[i] = pos;
                    continue;
                }
                if (!first) pos += _gap + extra;
                positions[i] = pos;
                pos += _base[i];
                first = false;
            }
            return positions;
        }

        // ---- §11.5 解析内在轨道尺寸 ----

        private void ResolveIntrinsicSizes()
        {
            List<GridItemInfo>? spanning = null;
            List<GridItemInfo>? crossingFlexible = null;

            // §11.5.2 单轨子项（不在弹性轨道里）：按轨道两端的类别取对应的尺寸贡献。
            foreach (var item in _items)
            {
                int start = item.Start(_isColumn), span = item.Span(_isColumn);
                if (CrossesFlexible(start, span))
                {
                    (crossingFlexible ??= new()).Add(item);
                    continue;
                }
                if (span > 1)
                {
                    (spanning ??= new()).Add(item);
                    continue;
                }

                var fn = _fns[start];
                if (!fn.HasIntrinsicMin && !fn.HasIntrinsicMax) continue;

                if (fn.MinKind == Breadth.MinContent)
                    _base[start] = Math.Max(_base[start], MinContent(item));
                else if (fn.MinKind == Breadth.MaxContent)
                    _base[start] = Math.Max(_base[start], MaxContent(item));
                else if (fn.MinKind == Breadth.Auto)
                    _base[start] = Math.Max(_base[start], Minimum(item, start, 1));

                float limit = fn.MaxKind switch
                {
                    Breadth.MinContent => MinContent(item),
                    Breadth.MaxContent or Breadth.Auto => MaxContent(item),
                    // fit-content(limit)：max-content 贡献，但不超过参数。
                    Breadth.FitContent => Math.Min(MaxContent(item), fn.MaxValue),
                    _ => float.NaN,
                };
                if (!float.IsNaN(limit))
                    _growth[start] = float.IsPositiveInfinity(_growth[start]) ? limit : Math.Max(_growth[start], limit);
                if (_growth[start] < _base[start]) _growth[start] = _base[start];
            }

            // §11.5.3 跨多条（非弹性）轨道的子项：按跨度从小到大分组，依次满足各类尺寸贡献。
            if (spanning != null)
            {
                spanning.Sort((a, b) => a.Span(_isColumn).CompareTo(b.Span(_isColumn)));
                int groupStart = 0;
                while (groupStart < spanning.Count)
                {
                    int span = spanning[groupStart].Span(_isColumn);
                    int groupEnd = groupStart;
                    while (groupEnd < spanning.Count && spanning[groupEnd].Span(_isColumn) == span) groupEnd++;

                    for (var step = Step.IntrinsicMinimums; step <= Step.MaxContentMaximums; step++)
                        DistributeForGroup(spanning, groupStart, groupEnd, step);

                    groupStart = groupEnd;
                }
            }

            // §11.5.4 跨弹性轨道的子项（不论跨度，一并处理）：最小贡献只分给下限为内在尺寸的弹性轨道，
            // 按 fr 系数的比例分配。
            if (crossingFlexible != null)
                DistributeToFlexibleTracks(crossingFlexible);
        }

        private bool CrossesFlexible(int start, int span)
        {
            for (int t = start; t < start + span; t++)
                if (_fns[t].IsFlexible) return true;
            return false;
        }

        private static bool Affects(Step step, TrackFunction fn, bool indefinite) => step switch
        {
            Step.IntrinsicMinimums => fn.HasIntrinsicMin,
            Step.ContentMinimums => fn.MinKind is Breadth.MinContent or Breadth.MaxContent,
            // 在 max-content 约束下（轴不确定），auto 下限的轨道也按 max-content 贡献增长。
            Step.MaxContentMinimums => fn.MinKind == Breadth.MaxContent || (indefinite && fn.MinKind == Breadth.Auto),
            Step.IntrinsicMaximums => fn.HasIntrinsicMax,
            _ => fn.MaxKind is Breadth.Auto or Breadth.MaxContent or Breadth.FitContent,
        };

        /// <summary>所有受影响的轨道都到了上限、仍有剩余空间时，继续分给哪些轨道（CSS §11.5「distribute space beyond limits」）。</summary>
        private static bool GrowsBeyondLimit(Step step, TrackFunction fn) => step switch
        {
            Step.IntrinsicMinimums or Step.ContentMinimums => fn.HasIntrinsicMax,
            Step.MaxContentMinimums => fn.MaxKind is Breadth.Auto or Breadth.MaxContent,
            _ => fn.MaxKind != Breadth.FitContent,
        };

        private float Contribution(Step step, GridItemInfo item, int start, int span) => step switch
        {
            Step.IntrinsicMinimums => Minimum(item, start, span),
            Step.ContentMinimums or Step.IntrinsicMaximums => MinContent(item),
            _ => MaxContent(item),
        };

        private void DistributeForGroup(List<GridItemInfo> items, int from, int to, Step step)
        {
            bool toGrowth = step >= Step.IntrinsicMaximums;
            float[]? planned = null;

            for (int k = from; k < to; k++)
            {
                var item = items[k];
                int start = item.Start(_isColumn), span = item.Span(_isColumn);

                int affected = 0;
                for (int t = start; t < start + span; t++)
                    if (!_collapsed[t] && Affects(step, _fns[t], _indefinite)) affected++;
                if (affected == 0) continue;

                // 已覆盖的尺寸：跨越的全部轨道（growth limit 为无穷时按 base size 计）加内部 gap。
                float covered = toGrowth ? SumRange(start, span, _growth, fallback: _base) : SumRange(start, span, _base);
                float extra = Contribution(step, item, start, span) - covered;
                if (extra <= 0.01f) continue;

                planned ??= new float[_fns.Length];
                DistributeExtra(start, span, extra, step, toGrowth, planned);
            }

            if (planned == null) return;
            for (int t = 0; t < _fns.Length; t++)
            {
                if (planned[t] <= 0) continue;
                if (toGrowth)
                {
                    _growth[t] = (float.IsPositiveInfinity(_growth[t]) ? _base[t] : _growth[t]) + planned[t];
                }
                else
                {
                    _base[t] += planned[t];
                    if (_growth[t] < _base[t]) _growth[t] = _base[t];
                }
            }
        }

        /// <summary>
        /// 把 <paramref name="extra"/> 均分给 [start, start+span) 中受 <paramref name="step"/> 影响的轨道：
        /// 先到各自上限（base size 以 growth limit / fit-content 参数为限，growth limit 视为可无限增长，
        /// fit-content 轨道除外），仍有剩余再按 <see cref="GrowsBeyondLimit"/> 越限分配；
        /// 每条轨道的计划增量取各子项所需的最大值。
        /// </summary>
        private void DistributeExtra(int start, int span, float extra, Step step, bool toGrowth, float[] planned)
        {
            Span<float> increase = span <= StackLimit ? stackalloc float[span] : new float[span];
            Span<bool> frozen = span <= StackLimit ? stackalloc bool[span] : new bool[span];
            float remaining = extra;

            // 先到上限。
            while (remaining > 0.01f)
            {
                int unfrozen = 0;
                for (int k = 0; k < span; k++)
                {
                    int t = start + k;
                    if (frozen[k] || _collapsed[t] || !Affects(step, _fns[t], _indefinite)) continue;
                    if (Current(t, toGrowth) + increase[k] >= Limit(t, toGrowth) - 0.01f) { frozen[k] = true; continue; }
                    unfrozen++;
                }
                if (unfrozen == 0) break;

                float share = remaining / unfrozen;
                for (int k = 0; k < span; k++)
                {
                    int t = start + k;
                    if (frozen[k] || _collapsed[t] || !Affects(step, _fns[t], _indefinite)) continue;
                    float add = Math.Min(share, Limit(t, toGrowth) - Current(t, toGrowth) - increase[k]);
                    increase[k] += add;
                    remaining -= add;
                }
            }

            // 再越过上限。
            if (remaining > 0.01f)
            {
                int beyond = 0;
                for (int k = 0; k < span; k++)
                {
                    int t = start + k;
                    if (!_collapsed[t] && Affects(step, _fns[t], _indefinite) && GrowsBeyondLimit(step, _fns[t])) beyond++;
                }
                bool all = beyond == 0;
                if (all)
                {
                    for (int k = 0; k < span; k++)
                        if (!_collapsed[start + k] && Affects(step, _fns[start + k], _indefinite)) beyond++;
                }

                float share = remaining / beyond;
                for (int k = 0; k < span; k++)
                {
                    int t = start + k;
                    if (_collapsed[t] || !Affects(step, _fns[t], _indefinite)) continue;
                    if (all || GrowsBeyondLimit(step, _fns[t])) increase[k] += share;
                }
            }

            for (int k = 0; k < span; k++)
                planned[start + k] = Math.Max(planned[start + k], increase[k]);
        }

        private float Current(int t, bool toGrowth)
            => toGrowth && !float.IsPositiveInfinity(_growth[t]) ? _growth[t] : _base[t];

        private float Limit(int t, bool toGrowth)
        {
            var fn = _fns[t];
            if (toGrowth)
                return fn.MaxKind == Breadth.FitContent ? Math.Max(fn.MaxValue, _base[t]) : float.PositiveInfinity;

            float limit = _growth[t];
            if (fn.MaxKind == Breadth.FitContent) limit = Math.Min(limit, Math.Max(fn.MaxValue, _base[t]));
            return limit;
        }

        private void DistributeToFlexibleTracks(List<GridItemInfo> items)
        {
            float[]? planned = null;
            foreach (var item in items)
            {
                int start = item.Start(_isColumn), span = item.Span(_isColumn);

                float flexSum = 0;
                int affected = 0;
                for (int t = start; t < start + span; t++)
                {
                    if (_collapsed[t] || !_fns[t].IsFlexible || !_fns[t].HasIntrinsicMin) continue;
                    flexSum += _fns[t].MaxValue;
                    affected++;
                }
                if (affected == 0) continue;

                float extra = Minimum(item, start, span) - SumRange(start, span, _base);
                if (extra <= 0.01f) continue;

                planned ??= new float[_fns.Length];
                for (int t = start; t < start + span; t++)
                {
                    if (_collapsed[t] || !_fns[t].IsFlexible || !_fns[t].HasIntrinsicMin) continue;
                    float share = flexSum > 0 ? extra * _fns[t].MaxValue / flexSum : extra / affected;
                    planned[t] = Math.Max(planned[t], share);
                }
            }

            if (planned == null) return;
            for (int t = 0; t < _fns.Length; t++)
            {
                if (planned[t] <= 0) continue;
                _base[t] += planned[t];
                if (_growth[t] < _base[t]) _growth[t] = _base[t];
            }
        }

        // ---- §11.6 最大化轨道 ----

        private void MaximizeTracks(float? available)
        {
            if (available is not { } space)
            {
                // max-content 约束下剩余空间视为无穷：每条轨道都长到 growth limit。
                for (int i = 0; i < _fns.Length; i++) _base[i] = Math.Max(_base[i], _growth[i]);
                return;
            }

            float free = space - Total;
            while (free > 0.01f)
            {
                int unfrozen = 0;
                for (int i = 0; i < _fns.Length; i++)
                    if (!_collapsed[i] && _base[i] < _growth[i] - 0.01f) unfrozen++;
                if (unfrozen == 0) break;

                float share = free / unfrozen;
                for (int i = 0; i < _fns.Length; i++)
                {
                    if (_collapsed[i] || _base[i] >= _growth[i] - 0.01f) continue;
                    float add = Math.Min(share, _growth[i] - _base[i]);
                    _base[i] += add;
                    free -= add;
                }
            }
        }

        // ---- §11.7 扩展弹性轨道 ----

        private void ExpandFlexibleTracks(float? available)
        {
            float fr;
            if (available is { } space)
            {
                fr = FindFrSize(0, _fns.Length, space);
            }
            else
            {
                // 可用空间不确定：fr 取每条弹性轨道的 base/系数（系数 ≤ 1 时取 base）与每个跨弹性轨道子项
                // 「以其 max-content 贡献为待填空间」求得的 fr 中的最大者。
                fr = 0;
                for (int i = 0; i < _fns.Length; i++)
                {
                    if (!_fns[i].IsFlexible || _collapsed[i]) continue;
                    float factor = _fns[i].MaxValue;
                    fr = Math.Max(fr, factor > 1 ? _base[i] / factor : _base[i]);
                }
                foreach (var item in _items)
                {
                    int start = item.Start(_isColumn), span = item.Span(_isColumn);
                    if (CrossesFlexible(start, span))
                        fr = Math.Max(fr, FindFrSize(start, span, MaxContent(item)));
                }
            }

            for (int i = 0; i < _fns.Length; i++)
            {
                if (!_fns[i].IsFlexible || _collapsed[i]) continue;
                float size = fr * _fns[i].MaxValue;
                if (size > _base[i]) _base[i] = size;
            }
        }

        /// <summary>
        /// CSS §11.7.1 Find the Size of an fr：在 [start, start+count) 中把 <paramref name="space"/> 扣去非弹性
        /// 轨道与 gap 后按 fr 系数分（系数和小于 1 时按 1 计）；某条弹性轨道分到的不足其 base size 时
        /// 视其为非弹性、重算。
        /// </summary>
        private float FindFrSize(int start, int count, float space)
        {
            Span<bool> inflexible = count <= StackLimit ? stackalloc bool[count] : new bool[count];
            float leftover = space - Gaps(start, count);
            while (true)
            {
                float baseSum = 0, flexSum = 0;
                for (int k = 0; k < count; k++)
                {
                    int t = start + k;
                    if (_collapsed[t]) continue;
                    if (_fns[t].IsFlexible && !inflexible[k]) flexSum += _fns[t].MaxValue;
                    else baseSum += _base[t];
                }
                if (flexSum <= 0) return 0;

                float hypothetical = (leftover - baseSum) / Math.Max(1f, flexSum);
                bool restart = false;
                for (int k = 0; k < count; k++)
                {
                    int t = start + k;
                    if (_collapsed[t] || !_fns[t].IsFlexible || inflexible[k]) continue;
                    if (hypothetical * _fns[t].MaxValue < _base[t])
                    {
                        inflexible[k] = true;
                        restart = true;
                    }
                }
                if (!restart) return Math.Max(0, hypothetical);
            }
        }

        // ---- 尺寸贡献 ----

        /// <summary>max-content 贡献（外尺寸）。列：无约束布局的自然宽度；行：在列区域宽度下的高度。</summary>
        private float MaxContent(GridItemInfo item)
        {
            if (!_isColumn) return Height(item);
            if (float.IsNaN(item.MaxContentWidth))
            {
                LayoutDispatcher.Dispatch(item.Child, new LayoutConstraints(null, null), 0, 0);
                item.MaxContentWidth = item.Child.BoxModel.MarginBox.Width;
            }
            return item.MaxContentWidth;
        }

        /// <summary>min-content 贡献（外尺寸）。列：固定宽度的子项即其宽度，否则按探测宽度预排后取内容外缘。</summary>
        private float MinContent(GridItemInfo item)
        {
            if (!_isColumn) return Height(item);
            if (float.IsNaN(item.MinContentWidth))
            {
                var child = item.Child;
                item.MinContentWidth = child.Type != LayoutType.Text && IsFixedLength(child.ComputedStyle.Width)
                    ? MaxContent(item)
                    : MeasureMinContentWidth(child);
            }
            return item.MinContentWidth;
        }

        private static float Height(GridItemInfo item)
        {
            if (float.IsNaN(item.Height))
            {
                LayoutDispatcher.Dispatch(item.Child, new LayoutConstraints(ComputeWidthConstraint(item.AreaWidth), null), 0, 0);
                item.Height = item.Child.BoxModel.MarginBox.Height;
            }
            return item.Height;
        }

        /// <summary>
        /// 最小贡献（auto 下限的轨道所用，CSS §6.6 automatic minimum size）：显式 min-width/height 为准；
        /// 否则滚动容器为 0；否则取基于内容的最小尺寸（显式尺寸，没有则 min-content 贡献）——跨越的
        /// 轨道上限都固定时不超过它们之和（含内部 gap）。
        /// </summary>
        private float Minimum(GridItemInfo item, int start, int span)
        {
            var child = item.Child;
            if (child.Type == LayoutType.Text) return MinContent(item);

            var cs = child.ComputedStyle;
            // Miko 没有 min-width/height: auto，计算值默认为 0：只有正的固定值算作显式下限
            // （因此 min-width: 0 关不掉自动最小尺寸——要让 fr 轨道收缩到 0，写 minmax(0, 1fr)）。
            var minSize = _isColumn ? cs.MinWidth : cs.MinHeight;
            if (IsFixedLength(minSize) && minSize.ToPixels(0, cs.FontSize.Value) > 0) return OuterSize(item, minSize);

            var overflow = _isColumn ? cs.OverflowX : cs.OverflowY;
            if (overflow != Overflow.Visible) return OuterSize(item, Length.Px(0));

            // 基于内容的最小尺寸：有显式尺寸时取之（specified size suggestion），否则取 min-content。
            float contentBased = IsFixedLength(_isColumn ? cs.Width : cs.Height) ? MaxContent(item) : MinContent(item);
            float fixedMaxSum = Gaps(start, span);
            for (int t = start; t < start + span; t++)
            {
                if (_fns[t].MaxKind != Breadth.Fixed) return contentBased;
                fixedMaxSum += _fns[t].MaxValue;
            }
            return Math.Min(contentBased, fixedMaxSum);
        }

        /// <summary>子项内容尺寸为 <paramref name="size"/> 时在该轴的外尺寸（加上 padding、border 与 margin）。</summary>
        private float OuterSize(GridItemInfo item, Length size)
        {
            // 先测一次，使 BoxModel 里的 margin/border/padding 就位。
            MaxContent(item);
            var bm = item.Child.BoxModel;
            var cs = item.Child.ComputedStyle;
            float px = size.ToPixels(0, cs.FontSize.Value);
            float margin = _isColumn ? bm.Margin.Horizontal : bm.Margin.Vertical;
            float chrome = _isColumn ? bm.Border.Horizontal + bm.Padding.Horizontal : bm.Border.Vertical + bm.Padding.Vertical;
            return (cs.BoxSizing == BoxSizing.BorderBox ? Math.Max(px, chrome) : px + chrome) + margin;
        }

        private static bool IsFixedLength(Length length) => !length.IsAuto && !length.HasPercentComponent;

        // ---- 求和 ----

        /// <summary>[start, start+count) 中未折叠轨道之间的 gap 总和。</summary>
        private float Gaps(int start, int count)
        {
            int visible = 0;
            for (int t = start; t < start + count; t++) if (!_collapsed[t]) visible++;
            return _gap * Math.Max(0, visible - 1);
        }

        /// <summary>
        /// [start, start+count) 中未折叠轨道的 <paramref name="sizes"/> 之和加内部 gap；
        /// 值为无穷时改取 <paramref name="fallback"/>（growth limit 无穷时按 base size 计）。
        /// </summary>
        private float SumRange(int start, int count, float[] sizes, float[]? fallback = null)
        {
            float total = Gaps(start, count);
            for (int t = start; t < start + count; t++)
            {
                if (_collapsed[t]) continue;
                float s = sizes[t];
                total += float.IsPositiveInfinity(s) && fallback != null ? fallback[t] : s;
            }
            return total;
        }
    }

    /// <summary>
    /// 测量子项的 min-content 外宽度：以 <see cref="MinContentProbeWidth"/> 预排（文本在每个换行机会处换行、
    /// 自动宽度的块收到最窄），再取其自身 margin box 与在流后代内容外缘中的较大者。
    /// 显式宽度的后代按其宽度计、不再下探（溢出它的内容不计入 min-content）。
    /// </summary>
    private static float MeasureMinContentWidth(LayoutBox child)
    {
        LayoutDispatcher.Dispatch(child, new LayoutConstraints(MinContentProbeWidth, null), 0, 0);
        var bm = child.BoxModel;
        if (child.Type == LayoutType.Text) return bm.MarginBox.Width;

        float contentRight = Math.Max(bm.Content.Right, DescendantExtentRight(child));
        float outer = contentRight + bm.Padding.Right + bm.Border.Right + bm.Margin.Right - bm.MarginBox.Left;
        return Math.Max(bm.MarginBox.Width, outer);

        static float DescendantExtentRight(LayoutBox box)
        {
            float right = float.NegativeInfinity;
            foreach (var c in box.Children)
            {
                if (BlockLayout.IsOutOfFlow(c)) continue;
                var cbm = c.BoxModel;
                right = Math.Max(right, cbm.MarginBox.Right);
                if (c.Children.Count > 0 && c.ComputedStyle.Width.IsAuto)
                {
                    float inner = DescendantExtentRight(c);
                    if (!float.IsNegativeInfinity(inner))
                        right = Math.Max(right, inner + cbm.Padding.Right + cbm.Border.Right + cbm.Margin.Right);
                }
            }
            return right;
        }
    }

    /// <summary>
    /// 子项在 area 内的宽度约束：取 area 全宽（margin 由子项自身布局扣除）——
    /// 宽度 auto 的块级子项据此填满 margin-box、显式/百分比宽度子项以 area 为基准解析、
    /// 文本子项以此宽度换行。
    /// </summary>
    private static float ComputeWidthConstraint(float areaW) => Math.Max(0, areaW);

    /// <summary>
    /// 布局一个 grid 子项到其 area：inline（水平）轴按有效 justify（justify-self → justify-items）
    /// stretch/对齐，block（垂直）轴按有效 align（align-self → align-items）stretch/对齐；
    /// 两轴的 stretch 仅当对应尺寸为 auto 且非文本节点，其余情形按内容尺寸后再在 area 内对齐；
    /// auto margin 优先吸收 area 剩余空间（margin:auto 居中）。
    /// </summary>
    private void LayoutItem(
        GridItemInfo item,
        float[] colPos, float[] colSizes, float[] rowPos, float[] rowSizes,
        AlignItems containerAlignItems, JustifyItems containerJustifyItems)
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
        // 有效 justify/align：justify-self/align-self → justify-items/align-items（容器 Normal 时映射为 stretch）。
        var effectiveJustify = ResolveItemJustify(childStyle.JustifySelf, containerJustifyItems);
        var effectiveAlign = ResolveItemAlign(childStyle.AlignSelf, containerAlignItems);
        // inline 轴：justify 为 stretch（含 normal）且宽度 auto 时拉伸填满 area（文本节点除外）；
        // 其余 justify（start/center/end）下宽度 auto 的子项按内容尺寸（shrink-to-fit）再在 area 内对齐。
        bool stretchWidth = effectiveJustify == JustifyItems.Stretch && widthIsAuto && !isText;
        bool stretchHeight = effectiveAlign == AlignItems.Stretch && heightIsAuto && !isText;

        // 有首选纵横比（aspect-ratio，ISSUE-150）的子项在 normal 对齐下不拉伸，而按块级盒的规则定尺寸
        // （CSS Grid 2 §6.2）：宽度填满 area、高度由比值从宽度推导；高度已指定时宽度由比值推导。
        // 显式 stretch 仍填满 area（可能扭曲比值，与规范一致）。
        if (AspectRatioSizing.Resolve(child).HasValue)
        {
            bool alignIsNormal = childStyle.AlignSelf == AlignSelf.Auto && containerAlignItems == AlignItems.Normal;
            bool justifyIsNormal = childStyle.JustifySelf == JustifySelf.Auto && containerJustifyItems == JustifyItems.Normal;
            if (alignIsNormal) stretchHeight = false;
            if (justifyIsNormal && !heightIsAuto) stretchWidth = false;
        }

        // stretch 轴上的 auto margin 无剩余空间可分，归零（CSS：stretch 优先于 auto margin）。
        if (stretchWidth) { mlAuto = false; mrAuto = false; }
        if (stretchHeight) { mtAuto = false; mbAuto = false; }

        // 宽度约束：stretch 或显式宽度（含百分比，相对 area 解析）时传入 area 全宽；auto 宽度且
        // 非 stretch 对齐时传 null 让子项收缩到内容尺寸（与 block 的 shrink-to-fit 一致），以便对齐生效。
        // 文本节点始终以 area 全宽为约束（在该宽度内换行），不参与拉伸。
        bool widthDefinite = stretchWidth || !widthIsAuto || isText;
        float? widthConstraint = widthDefinite ? Math.Max(0, areaW) : (float?)null;
        // 高度约束：stretch 或显式高度（含百分比，相对 area 解析）时传入 area 全高
        // （margin 由子项自身布局扣除）；auto 高度保持内容决定。
        float? heightConstraint = (stretchHeight || !heightIsAuto) ? Math.Max(0, areaH) : (float?)null;

        // dispatch 以 (x, y) 为子项 margin-box 原点：area 原点即 margin-box 原点
        // （start 对齐；margin 占据 area 边缘与 border-box 之间的空间）。
        // stretchHeight 时 AvailableHeight 是填充指令：height:auto + overflow 的子项在布局期
        // 即以 area 高度定型（见 ISSUE-105）；其余情形仅作百分比解析基准。
        LayoutDispatcher.Dispatch(child, new LayoutConstraints(widthConstraint, heightConstraint) { FillAvailableHeight = stretchHeight }, areaX, areaY);

        // 拉伸：强制内容盒填满 area（扣除已解析的 border/padding，使 margin-box == area）。
        float stretchedW = stretchWidth
            ? Math.Max(0, areaW - ml - mr
                - child.BoxModel.Border.Horizontal - child.BoxModel.Padding.Horizontal)
            : child.BoxModel.Content.Width;
        float stretchedH = stretchHeight
            ? Math.Max(0, areaH - mt - mb
                - child.BoxModel.Border.Vertical - child.BoxModel.Padding.Vertical)
            : child.BoxModel.Content.Height;

        if (stretchWidth || stretchHeight)
        {
            var c = child.BoxModel.Content;
            child.BoxModel.Content = new RectF(c.X, c.Y, stretchedW, stretchedH);

            // 拉伸尺寸必须对子项自身的布局生效：子树已按拉伸前的内容尺寸布局完成，
            // 其 align-items / justify-content / 百分比尺寸都是相对旧尺寸求值的
            // （与 flex 的 align-items:stretch 同一问题，见 ISSUE-122）。
            // 只在尺寸确有变化且有子树可排时重排，避免无谓的子树重算。
            bool changed = (stretchWidth && Math.Abs(stretchedW - c.Width) > 0.01f)
                || (stretchHeight && Math.Abs(stretchedH - c.Height) > 0.01f);
            if (changed && child.Children.Count > 0)
            {
                var marginBox = child.BoxModel.MarginBox;
                LayoutDispatcher.Dispatch(child, new LayoutConstraints(stretchedW, stretchedH)
                {
                    FillAvailableHeight = true,
                    ResolvedContentWidth = stretchedW,
                    ResolvedContentHeight = stretchedH,
                }, marginBox.X, marginBox.Y);

                // 重排后回写：两轴内容尺寸恒为上面算出的拉伸/自然尺寸，抵消子布局中
                // 内容驱动的尺寸重算与 min/max 夹取带来的偏差。
                var after = child.BoxModel.Content;
                child.BoxModel.Content = new RectF(after.X, after.Y, stretchedW, stretchedH);
            }
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

        // 水平位置：有效 justify（存在水平 auto margin 时对齐已被 margin 吸收，margin-box 左缘
        // 保持 areaX，border-box 经 ml 偏移居中；stretch 已填满，居中/末端偏移为 0）。
        float targetMarginBoxLeft;
        if (mlAuto || mrAuto)
        {
            targetMarginBoxLeft = areaX;
        }
        else
        {
            float marginBoxW = child.BoxModel.MarginBox.Width;
            targetMarginBoxLeft = effectiveJustify switch
            {
                JustifyItems.FlexEnd => areaX + areaW - marginBoxW,
                JustifyItems.Center => areaX + (areaW - marginBoxW) / 2f,
                // FlexStart / Stretch / Normal：起点对齐。
                _ => areaX,
            };
        }
        float dx = targetMarginBoxLeft - child.BoxModel.MarginBox.Left;
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
    /// <c>Auto</c> 时回退到容器的 align-items；容器为 <c>Normal</c>（CSS 初始值）时表现为
    /// stretch（与 FlexLayout 同一语义）。
    /// </summary>
    private static AlignItems ResolveItemAlign(AlignSelf alignSelf, AlignItems containerAlign)
        => alignSelf switch
        {
            AlignSelf.FlexStart => AlignItems.FlexStart,
            AlignSelf.FlexEnd => AlignItems.FlexEnd,
            AlignSelf.Center => AlignItems.Center,
            AlignSelf.Stretch => AlignItems.Stretch,
            AlignSelf.Baseline => AlignItems.Baseline,
            _ => containerAlign == AlignItems.Normal ? AlignItems.Stretch : containerAlign, // Auto
        };

    /// <summary>
    /// 将 <see cref="JustifySelf"/> 映射到等效的 <see cref="JustifyItems"/>；
    /// <c>Auto</c> 时回退到容器的 justify-items；容器为 <c>Normal</c>（CSS 初始值）时表现为
    /// stretch（与 <see cref="ResolveItemAlign"/> 同一语义，作用于 inline 轴）。
    /// </summary>
    private static JustifyItems ResolveItemJustify(JustifySelf justifySelf, JustifyItems containerJustify)
        => justifySelf switch
        {
            JustifySelf.FlexStart => JustifyItems.FlexStart,
            JustifySelf.FlexEnd => JustifyItems.FlexEnd,
            JustifySelf.Center => JustifyItems.Center,
            JustifySelf.Stretch => JustifyItems.Stretch,
            _ => containerJustify == JustifyItems.Normal ? JustifyItems.Stretch : containerJustify, // Auto
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
