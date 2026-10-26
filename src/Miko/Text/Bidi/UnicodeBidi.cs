using static Miko.Text.Bidi.BidiClass;

namespace Miko.Text.Bidi;

/// <summary>
/// The bidirectional algorithm over UTF-16 text (ISSUE-157). Levels are returned per code unit; the
/// low half of a surrogate pair has the level of its high half.
/// </summary>
internal static class UnicodeBidi
{
    /// <summary>
    /// Whether the text contains anything that can give a character a non-zero level in a
    /// left-to-right paragraph: right-to-left letters, Arabic digits, or explicit directional
    /// controls. When it does not and the paragraph is left-to-right, every level is 0 and the
    /// algorithm can be skipped.
    /// </summary>
    public static bool MayNeedReordering(ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            // Nothing below the Hebrew block is R, AL, AN or an explicit control.
            if (c < 0x0590) continue;

            int codePoint = c;
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(c, text[i + 1]);
                i++;
            }

            if (UnicodeBidiData.GetBidiClass(codePoint)
                is R or AL or AN or LRE or RLE or LRO or RLO or PDF or LRI or RLI or FSI or PDI)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The direction of the first strong character (rules P2–P3), or <c>null</c> when there is none.</summary>
    public static bool? IsFirstStrongRightToLeft(ReadOnlySpan<char> text)
    {
        int isolateDepth = 0;
        for (int i = 0; i < text.Length; i++)
        {
            int codePoint = text[i];
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                i++;
            }

            switch (UnicodeBidiData.GetBidiClass(codePoint))
            {
                case L when isolateDepth == 0: return false;
                case R or AL when isolateDepth == 0: return true;
                case LRI or RLI or FSI: isolateDepth++; break;
                case PDI when isolateDepth > 0: isolateDepth--; break;
                case B: return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Resolved embedding levels of <paramref name="text"/> as one paragraph.
    /// </summary>
    /// <param name="paragraphLevel">0 (LTR), 1 (RTL), or -1 to take it from the first strong character.</param>
    /// <param name="resetTrailingWhitespace">
    /// Apply the line-end part of rule L1, treating the whole text as one line. Callers that break the
    /// text into lines themselves pass <c>false</c> and handle line-end whitespace per line.
    /// </param>
    public static byte[] GetLevels(ReadOnlySpan<char> text, int paragraphLevel, bool resetTrailingWhitespace,
        out byte resolvedParagraphLevel)
    {
        int count = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) i++;
            count++;
        }

        var codePoints = new int[count];
        var types = new BidiClass[count];
        for (int i = 0, n = 0; i < text.Length; i++, n++)
        {
            int codePoint = text[i];
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                i++;
            }
            codePoints[n] = codePoint;
            types[n] = UnicodeBidiData.GetBidiClass(codePoint);
        }

        var paragraph = new BidiParagraph(types, codePoints, paragraphLevel);
        resolvedParagraphLevel = paragraph.ParagraphLevel;
        var levels = paragraph.GetLevels(resetTrailingWhitespace ? [count] : []);
        if (count == text.Length) return levels;

        var expanded = new byte[text.Length];
        for (int i = 0, n = 0; i < text.Length; i++, n++)
        {
            expanded[i] = levels[n];
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                expanded[++i] = levels[n];
            }
        }
        return expanded;
    }
}
