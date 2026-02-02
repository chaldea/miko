using Miko.Common;
using SkiaSharp;

namespace Miko.Fonts;

/// <summary>
/// Resolves font fallback for text rendering, especially for mixed-script text
/// </summary>
public class FontFallbackResolver
{
    private readonly FontManager _fontManager;

    public FontFallbackResolver(FontManager fontManager)
    {
        _fontManager = fontManager;
    }

    /// <summary>
    /// Segment text into runs, each with an appropriate font
    /// </summary>
    public List<TextRun> ResolveTextRuns(string text, string fontFamily, FontWeight weight)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<TextRun>();
        }

        var fallbackChain = _fontManager.ParseFontFamilyChain(fontFamily);
        var runs = new List<TextRun>();

        int runStart = 0;
        SKTypeface? currentTypeface = null;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            SKTypeface? typeface = ResolveFont(c, fallbackChain, weight);

            // If typeface changed, start a new run
            if (currentTypeface != null && typeface != currentTypeface)
            {
                runs.Add(new TextRun(
                    text.Substring(runStart, i - runStart),
                    runStart,
                    i - runStart,
                    currentTypeface
                ));
                runStart = i;
            }

            currentTypeface = typeface;
        }

        // Add final run
        if (runStart < text.Length && currentTypeface != null)
        {
            runs.Add(new TextRun(
                text.Substring(runStart),
                runStart,
                text.Length - runStart,
                currentTypeface
            ));
        }

        return runs;
    }

    /// <summary>
    /// Get the best font for a specific character from the fallback chain
    /// </summary>
    public SKTypeface? ResolveFont(char character, List<string> fallbackChain, FontWeight weight)
    {
        // Try each font in the fallback chain
        foreach (var familyName in fallbackChain)
        {
            var typeface = _fontManager.GetTypeface(familyName, weight);
            if (typeface != null && FontManager.ContainsGlyph(typeface, character))
            {
                return typeface;
            }
        }

        // Try default fallback chain
        foreach (var familyName in _fontManager.DefaultFallbackChain)
        {
            var typeface = _fontManager.GetTypeface(familyName, weight);
            if (typeface != null && FontManager.ContainsGlyph(typeface, character))
            {
                return typeface;
            }
        }

        // Try system fallback based on script
        var script = GetCharacterScript(character);
        var systemFallback = GetSystemFallbackForScript(script, weight);
        if (systemFallback != null)
        {
            return systemFallback;
        }

        // Return first available typeface as last resort
        return _fontManager.GetTypeface(fallbackChain.FirstOrDefault() ?? "Arial", weight);
    }

    /// <summary>
    /// Detect the script/language of a character
    /// </summary>
    public static UnicodeScript GetCharacterScript(char character)
    {
        int codePoint = character;

        // CJK Unified Ideographs
        if (codePoint >= 0x4E00 && codePoint <= 0x9FFF) return UnicodeScript.CJK;

        // CJK Extension A
        if (codePoint >= 0x3400 && codePoint <= 0x4DBF) return UnicodeScript.CJK;

        // CJK Compatibility Ideographs
        if (codePoint >= 0xF900 && codePoint <= 0xFAFF) return UnicodeScript.CJK;

        // CJK Unified Ideographs Extension B (surrogate pairs needed for full range)
        // Simplified check for BMP portion
        if (codePoint >= 0x20000 && codePoint <= 0x2A6DF) return UnicodeScript.CJK;

        // Hiragana
        if (codePoint >= 0x3040 && codePoint <= 0x309F) return UnicodeScript.CJK;

        // Katakana
        if (codePoint >= 0x30A0 && codePoint <= 0x30FF) return UnicodeScript.CJK;

        // Hangul Syllables
        if (codePoint >= 0xAC00 && codePoint <= 0xD7AF) return UnicodeScript.CJK;

        // Hangul Jamo
        if (codePoint >= 0x1100 && codePoint <= 0x11FF) return UnicodeScript.CJK;

        // Bopomofo
        if (codePoint >= 0x3100 && codePoint <= 0x312F) return UnicodeScript.CJK;

        // CJK Symbols and Punctuation
        if (codePoint >= 0x3000 && codePoint <= 0x303F) return UnicodeScript.CJK;

        // Fullwidth ASCII variants
        if (codePoint >= 0xFF00 && codePoint <= 0xFFEF) return UnicodeScript.CJK;

        // Basic Latin
        if (codePoint >= 0x0000 && codePoint <= 0x007F) return UnicodeScript.Latin;

        // Latin-1 Supplement
        if (codePoint >= 0x0080 && codePoint <= 0x00FF) return UnicodeScript.Latin;

        // Latin Extended-A
        if (codePoint >= 0x0100 && codePoint <= 0x017F) return UnicodeScript.Latin;

        // Latin Extended-B
        if (codePoint >= 0x0180 && codePoint <= 0x024F) return UnicodeScript.Latin;

        // Latin Extended Additional
        if (codePoint >= 0x1E00 && codePoint <= 0x1EFF) return UnicodeScript.Latin;

        // Cyrillic
        if (codePoint >= 0x0400 && codePoint <= 0x04FF) return UnicodeScript.Cyrillic;

        // Cyrillic Supplement
        if (codePoint >= 0x0500 && codePoint <= 0x052F) return UnicodeScript.Cyrillic;

        // Arabic
        if (codePoint >= 0x0600 && codePoint <= 0x06FF) return UnicodeScript.Arabic;

        // Arabic Supplement
        if (codePoint >= 0x0750 && codePoint <= 0x077F) return UnicodeScript.Arabic;

        // Hebrew
        if (codePoint >= 0x0590 && codePoint <= 0x05FF) return UnicodeScript.Hebrew;

        // Thai
        if (codePoint >= 0x0E00 && codePoint <= 0x0E7F) return UnicodeScript.Thai;

        // General Punctuation, Symbols, etc.
        if (codePoint >= 0x2000 && codePoint <= 0x2BFF) return UnicodeScript.Symbol;

        // Private Use Area (often used for icon fonts)
        if (codePoint >= 0xE000 && codePoint <= 0xF8FF) return UnicodeScript.Symbol;

        return UnicodeScript.Unknown;
    }

    private SKTypeface? GetSystemFallbackForScript(UnicodeScript script, FontWeight weight)
    {
        var fallbackFonts = script switch
        {
            UnicodeScript.CJK => new[] { "Microsoft YaHei", "SimSun", "SimHei", "MS Gothic", "Malgun Gothic", "PingFang SC", "Hiragino Sans GB" },
            UnicodeScript.Cyrillic => new[] { "Arial", "Segoe UI", "Times New Roman" },
            UnicodeScript.Arabic => new[] { "Arial", "Segoe UI", "Tahoma" },
            UnicodeScript.Hebrew => new[] { "Arial", "Segoe UI", "Times New Roman" },
            UnicodeScript.Thai => new[] { "Tahoma", "Leelawadee", "Cordia New" },
            UnicodeScript.Symbol => new[] { "Segoe UI Symbol", "Arial Unicode MS", "Symbola" },
            _ => new[] { "Arial", "Segoe UI" }
        };

        foreach (var fontName in fallbackFonts)
        {
            var typeface = _fontManager.GetTypeface(fontName, weight);
            if (typeface != null)
            {
                return typeface;
            }
        }

        return null;
    }
}
