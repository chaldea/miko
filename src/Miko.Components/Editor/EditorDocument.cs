using System.Globalization;

namespace Miko.Components.Editor;

/// <summary>An immutable text snapshot. Lines and UTF-16 offsets are zero-based. Line slices exclude their terminators.</summary>
public sealed class EditorDocument
{
    private readonly List<int> _starts = [0];
    public string Text { get; }
    public int LineCount => _starts.Count;
    public string NewLine { get; } = "\n";

    public EditorDocument(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        int offset = 0;
        while (offset < text.Length)
        {
            int next = text.AsSpan(offset).IndexOfAny('\r', '\n');
            if (next < 0) break;
            int start = offset + next;
            offset = start + 1;
            if (text[start] == '\r' && offset < text.Length && text[offset] == '\n') offset++;
            if (_starts.Count == 1) NewLine = text[start..offset];
            _starts.Add(offset);
        }
    }

    public int GetLineStart(int line) => _starts[line];

    public ReadOnlyMemory<char> GetLine(int line)
    {
        int start = _starts[line];
        int end = line + 1 < LineCount ? _starts[line + 1] : Text.Length;
        while (end > start && Text[end - 1] is '\n' or '\r') end--;
        return Text.AsMemory(start, end - start);
    }

    public int GetLineAt(int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, Text.Length);
        int found = _starts.BinarySearch(offset);
        return found >= 0 ? found : ~found - 1;
    }

    internal int Snap(int offset)
    {
        offset = Math.Clamp(offset, 0, Text.Length);
        if (offset == 0 || offset == Text.Length) return offset;
        if (Text[offset] == '\n' && Text[offset - 1] == '\r') return offset - 1;
        if (char.IsAscii(Text[offset]) && char.IsAscii(Text[offset - 1])) return offset;
        int position = _starts[GetLineAt(offset)];
        while (position < offset)
        {
            int next = Next(position);
            if (next > offset) break;
            position = next;
        }
        return position;
    }

    internal int Next(int offset) => offset == Text.Length ? offset :
        offset + StringInfo.GetNextTextElementLength(Text.AsSpan(offset));
}
