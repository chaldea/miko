using SkiaSharp;

namespace Miko.Text;

/// <summary>
/// One line of text after font fallback, bidi reordering and shaping (ISSUE-157): glyph runs in
/// visual order, left to right, ready to draw at a baseline. Immutable and cached by
/// <see cref="TextShaping"/>, so measuring and drawing the same text share one result.
/// </summary>
internal sealed class ShapedText
{
    public static readonly ShapedText Empty = new([], 0, 0, null, false);

    private SKTextBlob? _blob;

    public ShapedText(ShapedRun[] runs, float width, float height, SKTypeface? baselineTypeface, bool isRightToLeft)
    {
        Runs = runs;
        Width = width;
        Height = height;
        BaselineTypeface = baselineTypeface;
        IsRightToLeft = isRightToLeft;
    }

    /// <summary>Runs in visual order. Each has one typeface and one direction.</summary>
    public ShapedRun[] Runs { get; }

    /// <summary>Advance width, letter-spacing included.</summary>
    public float Width { get; }

    /// <summary>Tallest ascent + descent among the run fonts.</summary>
    public float Height { get; }

    /// <summary>Typeface of the logically first run; its metrics place the baseline, as on the simple path.</summary>
    public SKTypeface? BaselineTypeface { get; }

    /// <summary>Whether the paragraph direction the text was laid out in is right-to-left.</summary>
    public bool IsRightToLeft { get; }

    /// <summary>
    /// The glyphs as one text blob with positions relative to the line origin, built on first use.
    /// <paramref name="fontFactory"/> supplies the drawing font of a run (the painter's settings).
    /// </summary>
    public SKTextBlob? GetBlob(Func<SKTypeface, float, SKFont> fontFactory)
    {
        var blob = Volatile.Read(ref _blob);
        if (blob != null || Runs.Length == 0) return blob;

        using var builder = new SKTextBlobBuilder();
        bool any = false;
        foreach (var run in Runs)
        {
            if (run.Glyphs.Length == 0) continue;
            var positions = new SKPoint[run.Positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = new SKPoint(run.X + run.Positions[i].X, run.Positions[i].Y);
            }
            builder.AddPositionedRun(run.Glyphs, fontFactory(run.Typeface, run.FontSize), positions);
            any = true;
        }
        if (!any) return null;

        var built = builder.Build();
        // A concurrent first draw may have built one too; keep whichever landed first.
        var existing = Interlocked.CompareExchange(ref _blob, built, null);
        if (existing != null)
        {
            built?.Dispose();
            return existing;
        }
        return built;
    }

    /// <summary>
    /// Horizontal position of the caret before the UTF-16 offset <paramref name="index"/>, relative to
    /// the line origin. Inside a run the caret sits on the visual edge of the cluster; at the end of
    /// the text it sits on the line's end edge (right for LTR paragraphs, left for RTL).
    /// </summary>
    public float GetCaretX(int index)
    {
        foreach (var run in Runs)
        {
            if (index >= run.Start && index < run.Start + run.Length)
            {
                return run.X + run.GetCaretOffset(index);
            }
        }
        return IsRightToLeft ? 0 : Width;
    }
}

/// <summary>Glyphs of one typeface and one direction, in visual order.</summary>
internal sealed class ShapedRun
{
    public ShapedRun(SKTypeface typeface, float fontSize, ushort[] glyphs, SKPoint[] positions, float[] advances,
        int[] clusters, int start, int length, bool isRightToLeft, float width)
    {
        Typeface = typeface;
        FontSize = fontSize;
        Glyphs = glyphs;
        Positions = positions;
        Advances = advances;
        Clusters = clusters;
        Start = start;
        Length = length;
        IsRightToLeft = isRightToLeft;
        Width = width;
    }

    public SKTypeface Typeface { get; }
    public float FontSize { get; }
    public ushort[] Glyphs { get; }

    /// <summary>Glyph origins relative to the run's left edge and the baseline (mark offsets included).</summary>
    public SKPoint[] Positions { get; }

    /// <summary>Pen advance of each glyph, letter-spacing included.</summary>
    public float[] Advances { get; }

    /// <summary>UTF-16 offset (into the shaped text) of the cluster each glyph belongs to.</summary>
    public int[] Clusters { get; }

    /// <summary>Logical range of the run in the shaped text.</summary>
    public int Start { get; }
    public int Length { get; }

    public bool IsRightToLeft { get; }

    /// <summary>Left edge of the run in the line.</summary>
    public float X { get; internal set; }

    public float Width { get; }

    /// <summary>Caret before logical offset <paramref name="index"/> (within the run), from the run's left edge.</summary>
    public float GetCaretOffset(int index)
    {
        // LTR: the pen position of the first glyph (from the left) whose cluster is at or after the
        // index. RTL: logical order runs from the right edge, so the caret before a character is the
        // left edge of everything logically before it — the first glyph whose cluster precedes it.
        float pen = 0;
        for (int i = 0; i < Glyphs.Length; i++)
        {
            bool found = IsRightToLeft ? Clusters[i] < index : Clusters[i] >= index;
            if (found) return pen;
            pen += Advances[i];
        }
        return Width;
    }
}
