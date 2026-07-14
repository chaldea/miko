namespace Miko.Common;

/// <summary>
/// 轮廓简写类型（类似 CSS 的 <c>outline</c> 简写）。轮廓绘制在边框盒之外，
/// 不占据布局空间；复用 <see cref="BorderStyle"/> 表示线型。
/// <para>注意：<c>outline-offset</c> 不属于 <c>outline</c> 简写（CSS 中需单独设置），
/// 故此结构不含 Offset；偏移通过独立的 <c>OutlineOffset</c> 样式属性设置。</para>
/// </summary>
public struct Outline
{
    public Length Width { get; set; }
    public Color Color { get; set; }
    public BorderStyle Style { get; set; }

    /// <summary>
    /// 创建轮廓。
    /// </summary>
    public Outline(Length width, BorderStyle style, Color color)
    {
        Width = width;
        Style = style;
        Color = color;
    }

    /// <summary>
    /// 创建指定宽度和颜色的实线轮廓。
    /// </summary>
    public Outline(Length width, Color color)
    {
        Width = width;
        Style = BorderStyle.Solid;
        Color = color;
    }

    /// <summary>
    /// 创建实线轮廓。
    /// </summary>
    public static Outline Solid(Length width, Color color) => new(width, BorderStyle.Solid, color);

    /// <summary>
    /// 无轮廓。
    /// </summary>
    public static Outline None => new(Length.Px(0), BorderStyle.None, Common.Color.Transparent);
}
