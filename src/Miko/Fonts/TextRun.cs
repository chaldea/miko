using SkiaSharp;

namespace Miko.Fonts;

/// <summary>
/// Represents a segment of text with a specific font for rendering
/// </summary>
public class TextRun
{
    /// <summary>
    /// The text content of this run
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Starting index in the original string
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// Length of this run in characters
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// The typeface to use for rendering this run
    /// </summary>
    public SKTypeface Typeface { get; }

    public TextRun(string text, int startIndex, int length, SKTypeface typeface)
    {
        Text = text;
        StartIndex = startIndex;
        Length = length;
        Typeface = typeface;
    }
}
