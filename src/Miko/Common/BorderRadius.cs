namespace Miko.Common;

/// <summary>
/// 圆角简写类型（类似CSS的border-radius简写）
/// </summary>
public struct BorderRadius
{
    public Length TopLeft { get; set; }
    public Length TopRight { get; set; }
    public Length BottomRight { get; set; }
    public Length BottomLeft { get; set; }

    /// <summary>
    /// 四角相同的圆角
    /// </summary>
    public BorderRadius(Length all)
    {
        TopLeft = TopRight = BottomRight = BottomLeft = all;
    }

    /// <summary>
    /// 左上/右下 和 右上/左下 圆角
    /// </summary>
    public BorderRadius(Length topLeftBottomRight, Length topRightBottomLeft)
    {
        TopLeft = BottomRight = topLeftBottomRight;
        TopRight = BottomLeft = topRightBottomLeft;
    }

    /// <summary>
    /// 左上、右上/左下、右下 圆角
    /// </summary>
    public BorderRadius(Length topLeft, Length topRightBottomLeft, Length bottomRight)
    {
        TopLeft = topLeft;
        TopRight = BottomLeft = topRightBottomLeft;
        BottomRight = bottomRight;
    }

    /// <summary>
    /// 四角分别指定圆角
    /// </summary>
    public BorderRadius(Length topLeft, Length topRight, Length bottomRight, Length bottomLeft)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    /// <summary>
    /// 创建四角相同的圆角
    /// </summary>
    public static BorderRadius All(Length value) => new(value);

    /// <summary>
    /// 创建圆形（需要配合正方形元素使用）
    /// </summary>
    public static BorderRadius Circle => new(Length.Percent(50));

    /// <summary>
    /// 无圆角
    /// </summary>
    public static BorderRadius None => new(Length.Px(0));

    /// <summary>
    /// 从像素值创建四角相同的圆角
    /// </summary>
    public static implicit operator BorderRadius(float value) => new(Length.Px(value));

    /// <summary>
    /// 从Length创建四角相同的圆角
    /// </summary>
    public static implicit operator BorderRadius(Length value) => new(value);
}
