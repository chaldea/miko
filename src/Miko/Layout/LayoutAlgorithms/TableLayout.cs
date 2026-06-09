using Miko.Common;
using Miko.Core.DomElements;
using Miko.Utils;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// 表格布局算法（支持 auto 和 fixed 两种模式）。
///
/// auto（默认，与浏览器一致）：
///   1. 测量每列的 min-content 和 max-content 宽度
///   2. 按列的内容意图分配可用宽度
///
/// fixed：
///   1. 列数和列宽由首行决定（或 colgroup/col 显式宽度）
///   2. 直接平均分配，性能更好
/// </summary>
public class TableLayout
{
    public void Layout(LayoutBox box, LayoutConstraints constraints, float x, float y)
    {
        var style = box.ComputedStyle;

        // 1. 计算 margin, border, padding
        float containerWidth = constraints.AvailableWidth ?? 0;
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

        // 2. 计算表格的可用宽度
        bool widthIsExplicit = !style.Width.IsAuto;
        float? explicitContentWidth = null;

        if (widthIsExplicit)
        {
            float w = style.Width.ToPixels(containerWidth, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
            {
                w -= box.BoxModel.Border.Horizontal + box.BoxModel.Padding.Horizontal;
                w = Math.Max(0, w);
            }
            explicitContentWidth = w;
        }

        float availableContentWidth;
        if (explicitContentWidth.HasValue)
        {
            availableContentWidth = explicitContentWidth.Value;
        }
        else if (constraints.IsInfiniteWidth || containerWidth <= 0)
        {
            availableContentWidth = float.PositiveInfinity;
        }
        else
        {
            availableContentWidth = containerWidth
                - box.BoxModel.Margin.Horizontal
                - box.BoxModel.Border.Horizontal
                - box.BoxModel.Padding.Horizontal;
            availableContentWidth = Math.Max(0, availableContentWidth);
        }

        // 3. 计算内容区域起点
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;

        // 4. 收集所有表格行（递归处理 thead/tbody/tfoot）
        var rows = CollectTableRows(box);

        // 表格分组容器（thead/tbody/tfoot）也是布局树中的盒子。
        // 它们不参与列宽计算，但需要给一个空盒子尺寸以避免渲染异常。
        ResetGroupBoxModels(box);

        // 5. 解析每行的单元格与跨列信息，并确定列数
        var tableData = new List<RowData>();
        int columnCount = 0;
        foreach (var row in rows)
        {
            var cells = row.Children
                .Where(c => c.Type == LayoutType.TableCell && !BlockLayout.IsOutOfFlow(c))
                .ToList();

            int rowColumns = 0;
            foreach (var cell in cells)
            {
                rowColumns += GetColSpan(cell);
            }
            columnCount = Math.Max(columnCount, rowColumns);
            tableData.Add(new RowData { Row = row, Cells = cells });
        }

        // 6. 计算列宽
        float[] columnWidths;
        if (columnCount == 0)
        {
            columnWidths = Array.Empty<float>();
        }
        else if (style.TableLayout == TableLayoutAlgorithm.Fixed)
        {
            columnWidths = ComputeFixedColumnWidths(tableData, columnCount, availableContentWidth);
        }
        else
        {
            columnWidths = ComputeAutoColumnWidths(tableData, columnCount, availableContentWidth, widthIsExplicit);
        }

        float tableContentWidth = 0;
        foreach (var w in columnWidths) tableContentWidth += w;

        // 7. 布局每一行的单元格
        float currentY = contentY;
        foreach (var rowData in tableData)
        {
            var row = rowData.Row;
            var cells = rowData.Cells;

            float rowX = contentX;
            float currentX = rowX;
            float rowHeight = 0;
            int columnIndex = 0;

            foreach (var cell in cells)
            {
                int colSpan = GetColSpan(cell);

                float cellWidth = 0;
                for (int i = 0; i < colSpan && columnIndex + i < columnCount; i++)
                {
                    cellWidth += columnWidths[columnIndex + i];
                }

                var cellConstraints = new LayoutConstraints(cellWidth, null);
                LayoutDispatcher.Dispatch(cell, cellConstraints, currentX, currentY);

                currentX += cellWidth;
                rowHeight = Math.Max(rowHeight, cell.BoxModel.MarginBox.Height);
                columnIndex += colSpan;
            }

            // 设置行盒尺寸（让行盒占据整个表格宽度，便于背景着色等场景）
            ApplyRowBoxModel(row, containerWidth, rowX, currentY, tableContentWidth, rowHeight);

            currentY += rowHeight;
        }

        // 8. 计算表格高度
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
        else
        {
            contentHeight = currentY - contentY;
        }

        float finalContentWidth = explicitContentWidth ?? tableContentWidth;
        if (!explicitContentWidth.HasValue && !float.IsPositiveInfinity(availableContentWidth))
        {
            // 当没有显式宽度但有有限的可用宽度，shrink-to-fit 内容宽度
            finalContentWidth = tableContentWidth;
        }

        box.BoxModel.Content = new RectF(contentX, contentY, finalContentWidth, contentHeight);

        // 9. 记录可滚动内容尺寸
        box.ScrollableContentWidth = tableContentWidth + box.BoxModel.Padding.Horizontal;
        box.ScrollableContentHeight = (currentY - contentY) + box.BoxModel.Padding.Vertical;
    }

    /// <summary>
    /// Fixed 算法：列宽由首行单元格的显式 width 决定（其他列等分剩余空间）。
    /// 简化实现：当首行所有单元格无显式宽度时，按列数等分可用宽度。
    /// </summary>
    private float[] ComputeFixedColumnWidths(List<RowData> tableData, int columnCount, float availableWidth)
    {
        var widths = new float[columnCount];
        if (tableData.Count == 0) return widths;

        // 默认宽度：当容器宽度有限时用容器宽度等分；无限时设一个合理基准。
        float fallbackTotal = float.IsPositiveInfinity(availableWidth) ? 800f : availableWidth;
        float defaultWidth = fallbackTotal / columnCount;

        for (int i = 0; i < columnCount; i++) widths[i] = defaultWidth;

        // 用首行单元格的显式宽度覆盖默认值
        var firstRow = tableData[0];
        int columnIndex = 0;
        foreach (var cell in firstRow.Cells)
        {
            int colSpan = GetColSpan(cell);
            var cellStyle = cell.ComputedStyle;
            if (!cellStyle.Width.IsAuto && columnIndex < columnCount)
            {
                float cw = cellStyle.Width.ToPixels(fallbackTotal, cellStyle.FontSize.Value);
                if (cellStyle.BoxSizing != BoxSizing.BorderBox)
                {
                    cw += cellStyle.BorderLeftWidth.ToPixels(fallbackTotal, cellStyle.FontSize.Value)
                        + cellStyle.BorderRightWidth.ToPixels(fallbackTotal, cellStyle.FontSize.Value)
                        + cellStyle.PaddingLeft.ToPixels(fallbackTotal, cellStyle.FontSize.Value)
                        + cellStyle.PaddingRight.ToPixels(fallbackTotal, cellStyle.FontSize.Value);
                }
                // 跨列单元格平均分配
                float perColumn = cw / colSpan;
                for (int i = 0; i < colSpan && columnIndex + i < columnCount; i++)
                    widths[columnIndex + i] = perColumn;
            }
            columnIndex += colSpan;
        }
        return widths;
    }

    /// <summary>
    /// Auto 算法（CSS table 自动布局）：
    ///   1. 计算每列的 min-content / max-content 宽度（由所有相关单元格的需求合成）
    ///   2. 在表格可用宽度内分配：
    ///      - 容量充足时按 max-content 分配，剩余空间按比例分配
    ///      - 容量不足时按 (max-content - min-content) 比例收缩到 min-content
    ///      - 无限宽度时直接按 max-content
    /// </summary>
    private float[] ComputeAutoColumnWidths(
        List<RowData> tableData, int columnCount, float availableWidth, bool widthIsExplicit)
    {
        var minWidths = new float[columnCount];
        var maxWidths = new float[columnCount];

        // 第一遍：单列单元格（colspan=1）确定基础列宽
        foreach (var rowData in tableData)
        {
            int columnIndex = 0;
            foreach (var cell in rowData.Cells)
            {
                int colSpan = GetColSpan(cell);
                if (colSpan == 1 && columnIndex < columnCount)
                {
                    var (cellMin, cellMax) = MeasureCellContentWidth(cell);
                    minWidths[columnIndex] = Math.Max(minWidths[columnIndex], cellMin);
                    maxWidths[columnIndex] = Math.Max(maxWidths[columnIndex], cellMax);
                }
                columnIndex += colSpan;
            }
        }

        // 第二遍：跨列单元格的需求若大于跨越列宽之和，按比例分摊到各列
        foreach (var rowData in tableData)
        {
            int columnIndex = 0;
            foreach (var cell in rowData.Cells)
            {
                int colSpan = GetColSpan(cell);
                if (colSpan > 1 && columnIndex < columnCount)
                {
                    var (cellMin, cellMax) = MeasureCellContentWidth(cell);
                    DistributeSpannedWidth(minWidths, columnIndex, colSpan, cellMin);
                    DistributeSpannedWidth(maxWidths, columnIndex, colSpan, cellMax);
                }
                columnIndex += colSpan;
            }
        }

        float totalMin = minWidths.Sum();
        float totalMax = maxWidths.Sum();

        // 分配可用宽度
        float[] finalWidths = new float[columnCount];

        if (float.IsPositiveInfinity(availableWidth))
        {
            // 无可用宽度约束：使用 max-content 宽度
            Array.Copy(maxWidths, finalWidths, columnCount);
            return finalWidths;
        }

        float target = availableWidth;
        if (!widthIsExplicit)
        {
            // 无显式宽度：shrink-to-fit，目标为 min(maxWidth-sum, available)
            target = Math.Min(totalMax, availableWidth);
        }

        if (target <= totalMin || totalMax <= 0)
        {
            // 容量已不足，使用 min-content
            Array.Copy(minWidths, finalWidths, columnCount);
        }
        else if (target >= totalMax)
        {
            // 容量超过 max-content：先给每列 max-content，多余空间按各列 preferred(max-content)
            // 权重比例分配。preferred 越大的列，分到的剩余空间越多：
            //   extra_i = remaining * (preferred_i / sum(preferred))
            //   width_i = preferred_i + extra_i
            float extra = target - totalMax;
            for (int i = 0; i < columnCount; i++)
            {
                float weight = totalMax > 0 ? maxWidths[i] / totalMax : 1f / columnCount;
                finalWidths[i] = maxWidths[i] + extra * weight;
            }
        }
        else
        {
            // min-content < target < max-content：按 (max - min) 比例插值
            float flexRange = totalMax - totalMin;
            float fill = target - totalMin;
            for (int i = 0; i < columnCount; i++)
            {
                float colRange = maxWidths[i] - minWidths[i];
                float share = flexRange > 0 ? colRange / flexRange : 1f / columnCount;
                finalWidths[i] = minWidths[i] + fill * share;
            }
        }

        return finalWidths;
    }

    /// <summary>
    /// 跨列单元格的最低需求若超过当前跨越列之和，按比例分摊到各列。
    /// </summary>
    private static void DistributeSpannedWidth(float[] widths, int start, int span, float demand)
    {
        float current = 0;
        for (int i = 0; i < span; i++) current += widths[start + i];

        if (demand <= current) return;

        float diff = demand - current;
        if (current > 0)
        {
            // 按当前比例分摊
            for (int i = 0; i < span; i++)
            {
                float share = widths[start + i] / current;
                widths[start + i] += diff * share;
            }
        }
        else
        {
            // 当前全为 0：均分
            float each = demand / span;
            for (int i = 0; i < span; i++) widths[start + i] = each;
        }
    }

    /// <summary>
    /// 估算单元格的 min-content 与 max-content 宽度。
    /// - min-content：单词级最长片段宽度 + padding/border（避免再撑大）
    /// - max-content：完整文本宽度 + padding/border（不换行）
    /// 若单元格设置了显式 width，则该宽度作为下限同时影响 min/max。
    /// </summary>
    private (float MinContent, float MaxContent) MeasureCellContentWidth(LayoutBox cell)
    {
        var style = cell.ComputedStyle;
        float fs = style.FontSize.Value;

        float horizontalChrome =
            style.BorderLeftWidth.ToPixels(0, fs)
            + style.BorderRightWidth.ToPixels(0, fs)
            + style.PaddingLeft.ToPixels(0, fs)
            + style.PaddingRight.ToPixels(0, fs);
        if (style.BoxSizing == BoxSizing.BorderBox)
        {
            // border-box 下宽度本身已包含 padding/border，min/max 内容部分单独累加更直观，
            // 这里把外延作为额外开销计入，保持与 content-box 一致的最终列宽语义。
        }

        string? text = cell.Element.TextContent;
        float minContent = 0;
        float maxContent = 0;

        if (!string.IsNullOrEmpty(text))
        {
            // max-content：完整文本宽度
            var (w, _) = TextMeasurer.MeasureText(text, style.FontFamily, fs, style.FontWeight);
            maxContent = w;

            // min-content：最长单词宽度（按空白拆分）
            float longestWord = 0;
            foreach (var part in text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var (pw, _) = TextMeasurer.MeasureText(part, style.FontFamily, fs, style.FontWeight);
                if (pw > longestWord) longestWord = pw;
            }
            minContent = longestWord;
        }

        // 子元素：取每个子元素 margin-box 的内容尺寸作为 max-content 的近似
        // （行内子元素之和、块级子元素的最大值）
        if (cell.Children.Count > 0)
        {
            float childrenMax = 0;
            foreach (var child in cell.Children)
            {
                if (BlockLayout.IsOutOfFlow(child)) continue;
                // 让子元素在无限宽度下布局以获得自然尺寸
                var childConstraints = new LayoutConstraints(null, null);
                LayoutDispatcher.Dispatch(child, childConstraints, 0, 0);
                childrenMax = Math.Max(childrenMax, child.BoxModel.MarginBox.Width);
            }
            maxContent = Math.Max(maxContent, childrenMax);
            minContent = Math.Max(minContent, childrenMax);
        }

        // 显式 width 视为下限：min/max 都不应小于显式宽度内容部分
        if (!style.Width.IsAuto)
        {
            float explicitW = style.Width.ToPixels(0, fs);
            if (style.BoxSizing == BoxSizing.BorderBox)
            {
                explicitW -= horizontalChrome;
                explicitW = Math.Max(0, explicitW);
            }
            minContent = Math.Max(minContent, explicitW);
            maxContent = Math.Max(maxContent, explicitW);
        }

        return (minContent + horizontalChrome, maxContent + horizontalChrome);
    }

    private void ApplyRowBoxModel(LayoutBox row, float containerWidth, float rowX, float rowY, float rowWidth, float rowHeight)
    {
        var rowStyle = row.ComputedStyle;
        float rowFs = rowStyle.FontSize.Value;

        row.BoxModel.Margin = new EdgeSizes(
            rowStyle.MarginTop.ToPixels(containerWidth, rowFs),
            rowStyle.MarginRight.ToPixels(containerWidth, rowFs),
            rowStyle.MarginBottom.ToPixels(containerWidth, rowFs),
            rowStyle.MarginLeft.ToPixels(containerWidth, rowFs)
        );

        row.BoxModel.Border = new EdgeSizes(
            rowStyle.BorderTopWidth.ToPixels(containerWidth, rowFs),
            rowStyle.BorderRightWidth.ToPixels(containerWidth, rowFs),
            rowStyle.BorderBottomWidth.ToPixels(containerWidth, rowFs),
            rowStyle.BorderLeftWidth.ToPixels(containerWidth, rowFs)
        );

        row.BoxModel.Padding = new EdgeSizes(
            rowStyle.PaddingTop.ToPixels(containerWidth, rowFs),
            rowStyle.PaddingRight.ToPixels(containerWidth, rowFs),
            rowStyle.PaddingBottom.ToPixels(containerWidth, rowFs),
            rowStyle.PaddingLeft.ToPixels(containerWidth, rowFs)
        );

        row.BoxModel.Content = new RectF(rowX, rowY, rowWidth, rowHeight);
    }

    /// <summary>
    /// 收集表格中的所有行（递归查找 TableRow，处理 thead/tbody/tfoot 嵌套）
    /// </summary>
    private List<LayoutBox> CollectTableRows(LayoutBox tableBox)
    {
        var rows = new List<LayoutBox>();
        foreach (var child in tableBox.Children)
        {
            if (child.Type == LayoutType.TableRow)
            {
                rows.Add(child);
            }
            else
            {
                rows.AddRange(CollectTableRows(child));
            }
        }
        return rows;
    }

    /// <summary>
    /// 重置 thead/tbody/tfoot 等分组容器的盒模型为零尺寸，
    /// 由 TableLayout 直接在表格坐标系下定位行；分组容器本身不占用额外空间。
    /// </summary>
    private void ResetGroupBoxModels(LayoutBox tableBox)
    {
        foreach (var child in tableBox.Children)
        {
            if (child.Type != LayoutType.TableRow && child.Type != LayoutType.TableCell)
            {
                child.BoxModel.Margin = new EdgeSizes(0);
                child.BoxModel.Border = new EdgeSizes(0);
                child.BoxModel.Padding = new EdgeSizes(0);
                child.BoxModel.Content = new RectF(0, 0, 0, 0);
                ResetGroupBoxModels(child);
            }
        }
    }

    private static int GetColSpan(LayoutBox cell)
    {
        var element = cell.Element;
        if (element is TdElement td) return Math.Max(1, td.ColSpan);
        if (element is ThElement th) return Math.Max(1, th.ColSpan);
        return 1;
    }

    /// <summary>
    /// 表格行数据
    /// </summary>
    private class RowData
    {
        public required LayoutBox Row { get; set; }
        public required List<LayoutBox> Cells { get; set; }
    }
}
