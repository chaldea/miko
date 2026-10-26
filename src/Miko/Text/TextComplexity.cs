using System.Globalization;

namespace Miko.Text;

/// <summary>
/// Decides whether text can be drawn glyph-for-character, or needs the shaper (ISSUE-157).
/// <para>
/// "Simple" text maps each UTF-16 code unit to its nominal glyph and advance, which is exactly how
/// Miko measured and drew all text before HarfBuzz was wired in. Latin, Greek, Cyrillic, CJK and
/// Hangul syllables (without combining marks) are simple, so the text most UIs render keeps its
/// existing metrics and fast path. Everything else goes through HarfBuzz: scripts with contextual
/// forms or reordering (Arabic, Hebrew, Indic, Thai, …), combining marks, joiners and variation
/// selectors, and anything outside the BMP (emoji sequences). Over-classifying only costs speed;
/// under-classifying would draw broken text, so unknown ranges count as complex.
/// </para>
/// </summary>
internal static class TextComplexity
{
    private static readonly ulong[] SimpleBmp = BuildSimpleBmp();

    /// <summary>Whether every character of <paramref name="text"/> is simple.</summary>
    public static bool IsSimple(ReadOnlySpan<char> text)
    {
        foreach (char c in text)
        {
            // Below the combining diacritics block everything is simple.
            if (c < 0x0300) continue;
            if ((SimpleBmp[c >> 6] & (1UL << (c & 63))) == 0) return false;
        }
        return true;
    }

    public static bool IsSimple(char c) => c < 0x0300 || (SimpleBmp[c >> 6] & (1UL << (c & 63))) != 0;

    private static ulong[] BuildSimpleBmp()
    {
        var bits = new ulong[0x10000 / 64];
        // Blocks whose characters render with their nominal glyph. Combining marks and format
        // characters inside them are cleared again below.
        (int Start, int End)[] simpleRanges =
        [
            (0x0000, 0x036F), // Latin, IPA, spacing modifiers (combining diacritics cleared below)
            (0x0370, 0x03FF), // Greek and Coptic
            (0x0400, 0x052F), // Cyrillic and Cyrillic Supplement
            (0x0530, 0x058F), // Armenian
            (0x10A0, 0x10FF), // Georgian
            (0x13A0, 0x13FF), // Cherokee
            (0x1C80, 0x1C8F), // Cyrillic Extended-C
            (0x1C90, 0x1CBF), // Georgian Extended
            (0x1D00, 0x1DBF), // Phonetic extensions
            (0x1E00, 0x1FFF), // Latin Extended Additional, Greek Extended
            (0x2000, 0x20CF), // General punctuation, super/subscripts, currency
            (0x2100, 0x2BFF), // Letterlike, number forms, arrows, math, technical, shapes, symbols, dingbats
            (0x2C00, 0x2C5F), // Glagolitic
            (0x2C60, 0x2C7F), // Latin Extended-C
            (0x2C80, 0x2CFF), // Coptic
            (0x2D00, 0x2D2F), // Georgian Supplement
            (0x2E00, 0x2E7F), // Supplemental punctuation
            (0x2E80, 0x2FDF), // CJK radicals, Kangxi radicals
            (0x2FF0, 0x2FFF), // Ideographic description characters
            (0x3000, 0x303F), // CJK symbols and punctuation
            (0x3040, 0x30FF), // Hiragana, Katakana
            (0x3100, 0x312F), // Bopomofo
            (0x3130, 0x318F), // Hangul compatibility jamo
            (0x3190, 0x31FF), // Kanbun, Bopomofo extended, CJK strokes, Katakana extensions
            (0x3200, 0x4DBF), // Enclosed CJK, compatibility, CJK Extension A
            (0x4DC0, 0x4DFF), // Yijing hexagrams
            (0x4E00, 0x9FFF), // CJK unified ideographs
            (0xA000, 0xA4CF), // Yi
            (0xA640, 0xA69F), // Cyrillic Extended-B
            (0xA700, 0xA7FF), // Modifier tone letters, Latin Extended-D
            (0xAB30, 0xAB6F), // Latin Extended-E
            (0xAC00, 0xD7A3), // Hangul syllables
            (0xE000, 0xF8FF), // Private use (icon fonts)
            (0xF900, 0xFAFF), // CJK compatibility ideographs
            (0xFB00, 0xFB06), // Latin ligatures
            (0xFE10, 0xFE1F), // Vertical forms
            (0xFE30, 0xFE4F), // CJK compatibility forms
            (0xFE50, 0xFE6F), // Small form variants
            (0xFF00, 0xFFEF), // Halfwidth and fullwidth forms
        ];

        foreach (var (start, end) in simpleRanges)
        {
            for (int c = start; c <= end; c++)
            {
                bits[c >> 6] |= 1UL << (c & 63);
            }
        }

        // Marks attach to the preceding glyph and format characters (ZWJ, ZWNJ, bidi controls,
        // variation selectors' neighbours …) change shaping: neither may be drawn on its own.
        for (int c = 0x0300; c < 0x10000; c++)
        {
            if ((bits[c >> 6] & (1UL << (c & 63))) == 0) continue;
            var category = CharUnicodeInfo.GetUnicodeCategory((char)c);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.EnclosingMark or UnicodeCategory.Format
                or UnicodeCategory.Surrogate or UnicodeCategory.OtherNotAssigned)
            {
                bits[c >> 6] &= ~(1UL << (c & 63));
            }
        }
        return bits;
    }
}
