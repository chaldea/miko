using Miko.Common;
using SkiaSharp;

namespace Miko.Fonts;

/// <summary>
/// Represents a registered font with its metadata and typeface
/// </summary>
public class RegisteredFont : IDisposable
{
    public string FamilyName { get; }
    public FontFormat Format { get; }
    public SKTypeface Typeface { get; }
    public FontWeight Weight { get; }
    public SKFontStyleSlant Slant { get; }

    private readonly byte[] _fontData;
    private bool _disposed;

    public RegisteredFont(string familyName, byte[] fontData, FontFormat format)
    {
        FamilyName = familyName;
        Format = format;
        _fontData = fontData;

        // Create typeface from font data
        using var stream = new MemoryStream(fontData);
        Typeface = SKTypeface.FromStream(stream)
            ?? throw new InvalidOperationException($"Failed to create typeface from font data for '{familyName}'");

        Weight = ConvertToFontWeight(Typeface.FontWeight);
        Slant = Typeface.FontSlant;
    }

    /// <summary>
    /// Check if this font contains a glyph for the specified character
    /// </summary>
    public bool ContainsGlyph(char character)
    {
        using var font = new SKFont(Typeface);
        ushort glyphId = font.GetGlyph(character);
        return glyphId != 0;
    }

    /// <summary>
    /// Check if this font contains a glyph for the specified Unicode codepoint
    /// </summary>
    public bool ContainsGlyph(int codepoint)
    {
        using var font = new SKFont(Typeface);
        ushort glyphId = font.GetGlyph(codepoint);
        return glyphId != 0;
    }

    private static FontWeight ConvertToFontWeight(int skiaWeight)
    {
        return skiaWeight switch
        {
            <= 350 => FontWeight.Lighter,
            <= 450 => FontWeight.Normal,
            <= 650 => FontWeight.Bold,
            _ => FontWeight.Bolder
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Typeface.Dispose();
            _disposed = true;
        }
    }
}
