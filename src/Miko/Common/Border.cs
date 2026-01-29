namespace Miko.Common;

/// <summary>
/// 边框简写类型（类似CSS的border简写）
/// </summary>
public struct Border
{
    public Length Width { get; set; }
    public Color Color { get; set; }
    public BorderStyle Style { get; set; }

    /// <summary>
    /// 创建边框
    /// </summary>
    public Border(Length width, BorderStyle style, Color color)
    {
        Width = width;
        Style = style;
        Color = color;
    }

    /// <summary>
    /// 创建指定宽度和颜色的实线边框
    /// </summary>
    public Border(Length width, Color color)
    {
        Width = width;
        Style = BorderStyle.Solid;
        Color = color;
    }

    /// <summary>
    /// 创建指定宽度的黑色实线边框
    /// </summary>
    public Border(Length width)
    {
        Width = width;
        Style = BorderStyle.Solid;
        Color = Common.Color.Black;
    }

    /// <summary>
    /// 创建实线边框
    /// </summary>
    public static Border Solid(Length width, Color color) => new(width, BorderStyle.Solid, color);

    /// <summary>
    /// 创建虚线边框
    /// </summary>
    public static Border Dashed(Length width, Color color) => new(width, BorderStyle.Dashed, color);

    /// <summary>
    /// 创建点线边框
    /// </summary>
    public static Border Dotted(Length width, Color color) => new(width, BorderStyle.Dotted, color);

    /// <summary>
    /// 无边框
    /// </summary>
    public static Border None => new(Length.Px(0), BorderStyle.None, Common.Color.Transparent);
}
