using System.Collections.Concurrent;
using HarfBuzzSharp;
using Miko.Common;
using Direction = Miko.Common.Direction;
using Miko.Fonts;
using Miko.Text.Bidi;
using SkiaSharp;

namespace Miko.Text;

/// <summary>
/// Lays out one line of text for measuring and drawing (ISSUE-157): font fallback, bidi levels,
/// itemization by font / level / script, shaping, and visual reordering.
/// <para>
/// Text that is <see cref="TextComplexity.IsSimple(ReadOnlySpan{char})">simple</see> and laid out
/// left-to-right never comes here: <see cref="Utils.TextMeasurer"/> and the painter keep their
/// existing glyph-for-character path for it (<see cref="NeedsShaping"/>). Within shaped text, the
/// simple left-to-right items still use nominal glyphs and advances, so a Latin word measures the
/// same inside an Arabic sentence as on its own.
/// </para>
/// <para>
/// Results are cached process-wide like the other text caches (content-addressed, so sharing is
/// correct), and dropped with them when the font registry changes.
/// </para>
/// </summary>
internal static class TextShaping
{
    private readonly record struct ShapeKey(
        string Text, string FontFamily, float FontSize, FontWeight FontWeight, Direction? Direction, float LetterSpacing);

    private readonly record struct FontKey(SKTypeface Typeface, float FontSize);

    private static readonly ConcurrentDictionary<ShapeKey, ShapedText> Cache = new();
    private static readonly ConcurrentDictionary<FontKey, SKFont> MeasureFonts = new();
    private const int MaxCacheEntries = 4096;
    private const int MaxMeasureFonts = 256;

    // The paint argument does not affect advances; one shared instance avoids an allocation per run.
    private static readonly SKPaint MeasurePaint = new();

    /// <summary>
    /// Whether the text needs this pipeline: anything but simple text in a left-to-right paragraph.
    /// </summary>
    public static bool NeedsShaping(ReadOnlySpan<char> text, Direction direction = Direction.Ltr)
        => direction == Direction.Rtl || !TextComplexity.IsSimple(text);

    /// <summary>Drops the shaping caches and the HarfBuzz faces (font registry changed).</summary>
    public static void ClearCache()
    {
        Cache.Clear();
        MeasureFonts.Clear();
        HarfBuzzShaper.Clear();
    }

    /// <param name="direction">Paragraph direction; <c>null</c> takes it from the first strong character.</param>
    public static ShapedText Shape(string text, string fontFamily, float fontSize, FontWeight fontWeight,
        Direction? direction, float letterSpacing = 0)
    {
        if (string.IsNullOrEmpty(text)) return ShapedText.Empty;

        var key = new ShapeKey(text, fontFamily, fontSize, fontWeight, direction, letterSpacing);
        if (Cache.TryGetValue(key, out var cached)) return cached;

        var shaped = ShapeUncached(text, fontFamily, fontSize, fontWeight, direction, letterSpacing);
        if (Cache.Count >= MaxCacheEntries) Cache.Clear();
        Cache[key] = shaped;
        return shaped;
    }

    private static ShapedText ShapeUncached(string text, string fontFamily, float fontSize, FontWeight fontWeight,
        Direction? direction, float letterSpacing)
    {
        var fontRuns = new FontFallbackResolver(FontManager.Instance).ResolveTextRuns(text, fontFamily, fontWeight);
        if (fontRuns.Count == 0) return ShapedText.Empty;

        // Bidi levels. Without right-to-left content an LTR paragraph is all level 0.
        int requested = direction switch { Direction.Rtl => 1, Direction.Ltr => 0, _ => -1 };
        byte paragraphLevel;
        byte[]? levels = null;
        if (requested == 1 || UnicodeBidi.MayNeedReordering(text))
        {
            levels = UnicodeBidi.GetLevels(text, requested, resetTrailingWhitespace: true, out paragraphLevel);
        }
        else
        {
            paragraphLevel = 0;
        }

        var runs = new List<ShapedRun>();
        var runLevels = new List<byte>();
        float height = 0;
        foreach (var fontRun in fontRuns)
        {
            var font = GetMeasureFont(fontRun.Typeface, fontSize);
            var metrics = font.Metrics;
            height = Math.Max(height, metrics.Descent - metrics.Ascent);

            int end = fontRun.StartIndex + fontRun.Length;
            for (int start = fontRun.StartIndex; start < end;)
            {
                byte level = levels?[start] ?? 0;
                int limit = start + 1;
                while (limit < end && (levels?[limit] ?? 0) == level) limit++;

                foreach (var (itemStart, itemLength, script) in SplitByScript(text, start, limit))
                {
                    runs.Add(ShapeItem(text, itemStart, itemLength, fontRun.Typeface, font, fontSize, level, script, letterSpacing));
                    runLevels.Add(level);
                }
                start = limit;
            }
        }

        // L2 over items: each item has a single level, so reordering items is reordering characters.
        var ordered = new ShapedRun[runs.Count];
        var order = BidiParagraph.ComputeReordering(runLevels.ToArray());
        float x = 0;
        for (int i = 0; i < order.Length; i++)
        {
            var run = runs[order[i]];
            run.X = x;
            x += run.Width;
            ordered[i] = run;
        }

        return new ShapedText(ordered, x, height, fontRuns[0].Typeface, (paragraphLevel & 1) != 0);
    }

    /// <summary>
    /// Splits [start, limit) where the script changes (UAX #24 Script, from HarfBuzz's tables), so each
    /// item reaches the right shaper. Common and Inherited characters (spaces, punctuation, marks)
    /// stay with the preceding script, or with the following one at the start.
    /// </summary>
    private static IEnumerable<(int Start, int Length, Script Script)> SplitByScript(string text, int start, int limit)
    {
        var unicode = UnicodeFunctions.Default;
        int itemStart = start;
        var itemScript = Script.Invalid;
        for (int i = start; i < limit; i++)
        {
            int codePoint = text[i];
            int width = 1;
            if (char.IsHighSurrogate(text[i]) && i + 1 < limit && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                width = 2;
            }

            var script = unicode.GetScript(codePoint);
            if (script != Script.Common && script != Script.Inherited && script != Script.Unknown)
            {
                if (itemScript == Script.Invalid)
                {
                    itemScript = script;
                }
                else if (script != itemScript)
                {
                    yield return (itemStart, i - itemStart, itemScript);
                    itemStart = i;
                    itemScript = script;
                }
            }
            i += width - 1;
        }
        yield return (itemStart, limit - itemStart, itemScript == Script.Invalid ? Script.Common : itemScript);
    }

    private static ShapedRun ShapeItem(string text, int start, int length, SKTypeface typeface, SKFont font,
        float fontSize, byte level, Script script, float letterSpacing)
    {
        bool rtl = (level & 1) != 0;
        if (!rtl && TextComplexity.IsSimple(text.AsSpan(start, length)))
        {
            return LayoutSimple(text, start, length, typeface, font, fontSize, letterSpacing);
        }

        var shaped = HarfBuzzShaper.Shape(typeface, text, start, length, rtl, script);
        int count = shaped.Glyphs.Length;
        float scale = fontSize / shaped.UnitsPerEm;
        var positions = new SKPoint[count];
        var advances = new float[count];
        float pen = 0;
        for (int i = 0; i < count; i++)
        {
            float advance = shaped.Advances[i] * scale;
            // letter-spacing goes after each cluster, once per UTF-16 unit it covers (as the simple
            // path adds it after each character), on the cluster's last glyph in visual order.
            if (letterSpacing != 0 && (i + 1 == count || shaped.Clusters[i + 1] != shaped.Clusters[i]))
            {
                advance += letterSpacing * ClusterLength(shaped.Clusters, shaped.Clusters[i], start + length);
            }
            positions[i] = new SKPoint(pen + shaped.OffsetsX[i] * scale, shaped.OffsetsY[i] * scale);
            advances[i] = advance;
            pen += advance;
        }
        return new ShapedRun(typeface, fontSize, shaped.Glyphs, positions, advances, shaped.Clusters,
            start, length, rtl, pen);
    }

    /// <summary>UTF-16 units from a cluster to the next cluster boundary after it (or the item end).</summary>
    private static int ClusterLength(int[] clusters, int cluster, int itemEnd)
    {
        int next = itemEnd;
        foreach (int c in clusters)
        {
            if (c > cluster && c < next) next = c;
        }
        return next - cluster;
    }

    /// <summary>Nominal glyphs and advances, as the simple path measures them.</summary>
    private static ShapedRun LayoutSimple(string text, int start, int length, SKTypeface typeface, SKFont font,
        float fontSize, float letterSpacing)
    {
        var span = text.AsSpan(start, length);
        var glyphs = font.GetGlyphs(span);
        var widths = font.GetGlyphWidths(glyphs, MeasurePaint);
        var positions = new SKPoint[glyphs.Length];
        var advances = new float[glyphs.Length];
        var clusters = new int[glyphs.Length];
        float pen = 0;
        for (int i = 0; i < glyphs.Length; i++)
        {
            positions[i] = new SKPoint(pen, 0);
            advances[i] = widths[i] + letterSpacing;
            clusters[i] = start + i;
            pen += advances[i];
        }
        return new ShapedRun(typeface, fontSize, glyphs, positions, advances, clusters, start, length, false, pen);
    }

    /// <summary>Measuring font, configured like <see cref="Utils.TextMeasurer"/>'s (Skia defaults).</summary>
    private static SKFont GetMeasureFont(SKTypeface typeface, float fontSize)
    {
        var key = new FontKey(typeface, fontSize);
        if (MeasureFonts.TryGetValue(key, out var font)) return font;
        // Superseded instances are left to the finalizer: another thread may still be measuring with them.
        if (MeasureFonts.Count >= MaxMeasureFonts) MeasureFonts.Clear();
        font = new SKFont(typeface, fontSize);
        MeasureFonts[key] = font;
        return font;
    }
}
