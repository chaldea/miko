namespace Miko.Common;

/// <summary>
/// Represents a single border side (top, right, bottom, or left)
/// </summary>
public struct BorderSide
{
    public Length Width { get; set; }
    public Color Color { get; set; }
    public BorderStyle Style { get; set; }

    /// <summary>
    /// Creates a border side with specified width, style, and color
    /// </summary>
    public BorderSide(Length width, BorderStyle style, Color color)
    {
        Width = width;
        Style = style;
        Color = color;
    }

    /// <summary>
    /// Creates a solid border side with specified width and color
    /// </summary>
    public BorderSide(Length width, Color color)
    {
        Width = width;
        Style = BorderStyle.Solid;
        Color = color;
    }

    /// <summary>
    /// Creates a solid black border side with specified width
    /// </summary>
    public BorderSide(Length width)
    {
        Width = width;
        Style = BorderStyle.Solid;
        Color = Common.Color.Black;
    }

    /// <summary>
    /// No border (0px, transparent)
    /// </summary>
    public static BorderSide None => new(Length.Px(0), BorderStyle.None, Common.Color.Transparent);

    /// <summary>
    /// Check if the border side is visible
    /// </summary>
    public bool IsVisible => Style != BorderStyle.None && Width.Value > 0 && Color.A > 0;
}
