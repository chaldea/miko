using System.Globalization;
using Miko.Common;
using Miko.Graphics;

namespace Miko.Components.Editor;

/// <summary>Maps UTF-16 offsets to visual columns. ASCII uses a monospace run, tabs advance to
/// a stop, and fallback glyphs (including emoji and combining sequences) occupy whole cells.</summary>
internal sealed class EditorColumns(IGraphicsFactory graphics)
{
    internal static readonly TextStyle Font = new("Menlo, Consolas, DejaVu Sans Mono, monospace", 15, Direction: Direction.Ltr);
    public float Width { get; } = graphics.CreateText("0", Font).Width;

    public (int Offset, int Column) Locate(ReadOnlySpan<char> line, int maxOffset, int maxColumn)
    {
        int offset = 0, column = 0;
        while (offset < line.Length && offset < maxOffset && column < maxColumn)
        {
            var (length, cells) = Run(line[offset..], column, Math.Min(maxOffset - offset, maxColumn - column));
            if (length > maxOffset - offset || cells > maxColumn - column) break;
            offset += length;
            column += cells;
        }
        return (offset, column);
    }

    public (int Length, int Cells) Run(ReadOnlySpan<char> remaining, int column, int limit)
    {
        if (remaining[0] == '\t') return (1, 4 - column % 4);
        // Inspect one extra code unit: the last ASCII letter may start a combining sequence.
        int count = Math.Min(limit, remaining.Length);
        int scan = count < remaining.Length ? count + 1 : count;
        int special = remaining[..scan].IndexOfAnyExceptInRange(' ', '~');
        if (special < 0) return (count, count);
        int ascii = char.IsAscii(remaining[special]) ? special : special - 1;
        if (ascii > 0) return (ascii, ascii);
        int length = StringInfo.GetNextTextElementLength(remaining);
        float advance = graphics.CreateText(remaining[..length].ToString(), Font).Width;
        return (length, Math.Max(1, (int)MathF.Round(advance / Width)));
    }
}
