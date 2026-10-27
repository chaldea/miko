namespace Miko.Common;

/// <summary>
/// <c>grid-template-areas</c> 的值（ISSUE-159）：用一组字符串给网格单元命名，每个字符串是一行，
/// 行内以空白分隔各列的单元名，<c>.</c>（或一串点）表示不属于任何区域的空单元。
///
/// <code>
/// GridTemplateAreas = new GridTemplateAreas(
///     "header header",
///     "nav    main",
///     "footer footer"),
/// // 子项：GridArea = "main"
/// </code>
///
/// <para>同名单元必须连成一个矩形。区域决定显式网格的行列数下限（模板轨道不够时，多出的行列按
/// <c>grid-auto-rows/columns</c> 定尺寸），并为每个区域隐式命名四条边线
/// <c>name-start</c> / <c>name-end</c>（行、列各一对），子项可经 <see cref="GridLine.Named(string)"/>
/// 或 <c>GridArea</c> 简写放进区域。</para>
///
/// <para>实例不可变，可在多个样式与线程间共享。</para>
/// </summary>
public sealed class GridTemplateAreas
{
    private readonly string[] _rows;
    private readonly Dictionary<string, (int RowStart, int ColumnStart, int RowEnd, int ColumnEnd)> _areas;

    /// <param name="rows">每行一个字符串。</param>
    /// <exception cref="ArgumentException">
    /// 没有行、某行为空、各行列数不一致、单元名含非法字符，或同名单元不构成矩形。
    /// </exception>
    public GridTemplateAreas(params string[] rows)
    {
        if (rows == null || rows.Length == 0)
            throw new ArgumentException("grid-template-areas needs at least one row.", nameof(rows));

        var cells = new string?[rows.Length][];
        for (int r = 0; r < rows.Length; r++)
        {
            var tokens = (rows[r] ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                throw new ArgumentException($"grid-template-areas row {r + 1} is empty.", nameof(rows));
            if (r > 0 && tokens.Length != cells[0].Length)
                throw new ArgumentException(
                    $"grid-template-areas rows must have the same number of columns: row {r + 1} has {tokens.Length}, row 1 has {cells[0].Length}.",
                    nameof(rows));

            cells[r] = new string?[tokens.Length];
            for (int c = 0; c < tokens.Length; c++)
                cells[r][c] = ParseCell(tokens[c], r);
        }

        RowCount = rows.Length;
        ColumnCount = cells[0].Length;
        _rows = rows.Select(row => string.Join(' ', row.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))).ToArray();
        _areas = new Dictionary<string, (int, int, int, int)>(StringComparer.Ordinal);

        // 每个名字的外接矩形；随后核对矩形内全是这个名字（同名单元不连成矩形时，单元数会对不上）。
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int r = 0; r < RowCount; r++)
        {
            for (int c = 0; c < ColumnCount; c++)
            {
                var name = cells[r][c];
                if (name == null) continue;
                counts[name] = counts.TryGetValue(name, out var n) ? n + 1 : 1;
                _areas[name] = _areas.TryGetValue(name, out var a)
                    ? (Math.Min(a.RowStart, r), Math.Min(a.ColumnStart, c), Math.Max(a.RowEnd, r + 1), Math.Max(a.ColumnEnd, c + 1))
                    : (r, c, r + 1, c + 1);
            }
        }
        foreach (var (name, a) in _areas)
        {
            if ((a.RowEnd - a.RowStart) * (a.ColumnEnd - a.ColumnStart) != counts[name])
                throw new ArgumentException($"grid-template-areas: the cells named \"{name}\" do not form a rectangle.", nameof(rows));
        }
    }

    /// <summary>行数。</summary>
    public int RowCount { get; }

    /// <summary>列数。</summary>
    public int ColumnCount { get; }

    /// <summary>各行（单元间以单个空格规范化）。</summary>
    public IReadOnlyList<string> Rows => _rows;

    /// <summary>全部区域名。</summary>
    public IEnumerable<string> AreaNames => _areas.Keys;

    /// <summary>
    /// 取区域 <paramref name="name"/> 的范围：0 起始的起始行/列与（不含的）结束行/列，
    /// 即区域的四条边线在显式网格中的线索引（0 = 第一条线）。
    /// </summary>
    public bool TryGetArea(string name, out int rowStart, out int columnStart, out int rowEnd, out int columnEnd)
    {
        if (_areas.TryGetValue(name, out var a))
        {
            (rowStart, columnStart, rowEnd, columnEnd) = a;
            return true;
        }
        rowStart = columnStart = rowEnd = columnEnd = 0;
        return false;
    }

    /// <summary>CSS 写法：<c>"header header" "nav main"</c>。</summary>
    public override string ToString() => string.Join(' ', _rows.Select(r => $"\"{r}\""));

    private static string? ParseCell(string token, int row)
    {
        bool allDots = true;
        foreach (var ch in token)
        {
            if (ch != '.') { allDots = false; break; }
        }
        if (allDots) return null;

        foreach (var ch in token)
        {
            // CSS 的 name code point：字母、数字、- 与 _，以及全部非 ASCII 字符。
            if (!(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch > 0x7F))
                throw new ArgumentException(
                    $"grid-template-areas row {row + 1}: \"{token}\" is not a valid area name.", "rows");
        }
        return token;
    }
}
