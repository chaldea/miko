using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using SkiaSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace Miko.Text;

/// <summary>
/// Shapes text with HarfBuzz (ISSUE-157): contextual forms, ligatures, mark positioning and
/// reordering for complex scripts, mirroring of brackets in right-to-left runs.
/// <para>
/// One HarfBuzz face per <see cref="SKTypeface"/>, built from the typeface's tables on demand
/// (<see cref="SKTypeface.TryGetTableData(uint, out byte[])"/>) instead of from the whole font file:
/// shaping reads cmap / GSUB / GPOS / hmtx and never the outlines, which keeps a fallback to a large
/// system font (Apple Color Emoji is ~180 MB) cheap. Positions are in font units (scale = units per
/// em), so a shaping result can be scaled to any font size.
/// </para>
/// </summary>
internal sealed class HarfBuzzShaper : IDisposable
{
    /// <summary>
    /// Shapers by typeface reference. Typefaces live in <see cref="Fonts.FontManager"/>'s cache (or
    /// Skia's own for system fallbacks), so references are stable; the cap only guards against a
    /// source of fresh typeface instances.
    /// </summary>
    private static readonly ConcurrentDictionary<SKTypeface, HarfBuzzShaper> Shapers = new(ReferenceEqualityComparer.Instance);

    // Serialises creation and clearing, so a shaper is never built twice (the loser of a race would
    // hold native memory until finalisation). Always taken before a shaper's own lock, never after.
    private static readonly object CacheLock = new();

    private const int MaxShapers = 64;

    [ThreadStatic] private static Buffer? t_buffer;

    private readonly SKTypeface _typeface;
    private readonly Face _face;
    private readonly HarfBuzzSharp.Font _font;
    private bool _disposed;

    private HarfBuzzShaper(SKTypeface typeface)
    {
        _typeface = typeface;
        UnitsPerEm = Math.Max(1, typeface.UnitsPerEm);
        _face = new Face(LoadTable) { UnitsPerEm = UnitsPerEm, GlyphCount = typeface.GlyphCount };
        _font = new HarfBuzzSharp.Font(_face);
        _font.SetFunctionsOpenType();
        _font.SetScale(UnitsPerEm, UnitsPerEm);
    }

    public int UnitsPerEm { get; }

    /// <summary>
    /// Shapes <c>text[start..start+length]</c>, using the rest of <paramref name="text"/> as context so
    /// that joining scripts connect across run boundaries. Glyphs come out in visual order (left to
    /// right); clusters are UTF-16 offsets into <paramref name="text"/>; advances and offsets are in
    /// font units with y pointing down.
    /// </summary>
    public static ShapingResult Shape(SKTypeface typeface, string text, int start, int length, bool rightToLeft, Script script)
    {
        while (true)
        {
            if (!Shapers.TryGetValue(typeface, out var shaper))
            {
                lock (CacheLock)
                {
                    if (!Shapers.TryGetValue(typeface, out shaper))
                    {
                        if (Shapers.Count >= MaxShapers) ClearCore();
                        shaper = new HarfBuzzShaper(typeface);
                        Shapers[typeface] = shaper;
                    }
                }
            }

            lock (shaper)
            {
                // Lost a race with Clear(): fetch the replacement.
                if (shaper._disposed) continue;
                return shaper.ShapeCore(text, start, length, rightToLeft, script);
            }
        }
    }

    /// <summary>
    /// Drops every shaper. Called with the other font caches when the font registry changes, since
    /// registering or unregistering a family can dispose typefaces the shapers read from.
    /// </summary>
    public static void Clear()
    {
        lock (CacheLock)
        {
            ClearCore();
        }
    }

    private static void ClearCore()
    {
        foreach (var typeface in Shapers.Keys)
        {
            if (Shapers.TryRemove(typeface, out var shaper))
            {
                // Waits for a shape in progress on another thread before freeing native memory.
                lock (shaper)
                {
                    shaper.Dispose();
                }
            }
        }
    }

    private ShapingResult ShapeCore(string text, int start, int length, bool rightToLeft, Script script)
    {
        var buffer = t_buffer ??= new Buffer();
        buffer.ClearContents();
        buffer.AddUtf16(text.AsSpan(), start, length);
        buffer.Direction = rightToLeft ? Direction.RightToLeft : Direction.LeftToRight;
        buffer.Script = script;
        // The language stays unset: Language.Default reads the process locale, which would make
        // the output machine-dependent, and HarfBuzz falls back to each script's default system.
        var flags = BufferFlags.Default;
        if (start == 0) flags |= BufferFlags.BeginningOfText;
        if (start + length == text.Length) flags |= BufferFlags.EndOfText;
        buffer.Flags = flags;

        _font.Shape(buffer);

        var infos = buffer.GetGlyphInfoSpan();
        var positions = buffer.GetGlyphPositionSpan();
        var result = new ShapingResult(infos.Length, UnitsPerEm);
        for (int i = 0; i < infos.Length; i++)
        {
            result.Glyphs[i] = (ushort)infos[i].Codepoint;
            result.Clusters[i] = (int)infos[i].Cluster;
            result.Advances[i] = positions[i].XAdvance;
            result.OffsetsX[i] = positions[i].XOffset;
            result.OffsetsY[i] = -positions[i].YOffset;
        }
        return result;
    }

    /// <summary>
    /// HarfBuzz's table callback. Each table is copied once into native memory that HarfBuzz owns and
    /// frees through the release delegate; the face caches the blob for its lifetime.
    /// </summary>
    private Blob? LoadTable(Face face, Tag tag)
    {
        if (_typeface.Handle == IntPtr.Zero || !_typeface.TryGetTableData(tag, out var data) || data is not { Length: > 0 })
        {
            return null;
        }

        var memory = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, memory, data.Length);
        return new Blob(memory, data.Length, MemoryMode.ReadOnly, () => Marshal.FreeHGlobal(memory));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _font.Dispose();
        _face.Dispose();
    }
}

/// <summary>Glyphs of one shaped run in visual order; metrics in font units (see <see cref="HarfBuzzShaper"/>).</summary>
internal sealed class ShapingResult
{
    public ShapingResult(int count, int unitsPerEm)
    {
        UnitsPerEm = unitsPerEm;
        Glyphs = new ushort[count];
        Clusters = new int[count];
        Advances = new int[count];
        OffsetsX = new int[count];
        OffsetsY = new int[count];
    }

    public int UnitsPerEm { get; }
    public ushort[] Glyphs { get; }
    public int[] Clusters { get; }
    public int[] Advances { get; }
    public int[] OffsetsX { get; }
    public int[] OffsetsY { get; }
}
