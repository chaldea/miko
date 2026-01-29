namespace Miko.Common;

/// <summary>
/// 外边距简写类型（类似CSS的margin简写）
/// </summary>
public struct Margin
{
    public Length Top { get; set; }
    public Length Right { get; set; }
    public Length Bottom { get; set; }
    public Length Left { get; set; }

    /// <summary>
    /// 四边相同的外边距
    /// </summary>
    public Margin(Length all)
    {
        Top = Right = Bottom = Left = all;
    }

    /// <summary>
    /// 垂直和水平外边距
    /// </summary>
    public Margin(Length vertical, Length horizontal)
    {
        Top = Bottom = vertical;
        Right = Left = horizontal;
    }

    /// <summary>
    /// 上、水平、下外边距
    /// </summary>
    public Margin(Length top, Length horizontal, Length bottom)
    {
        Top = top;
        Right = Left = horizontal;
        Bottom = bottom;
    }

    /// <summary>
    /// 四边分别指定外边距
    /// </summary>
    public Margin(Length top, Length right, Length bottom, Length left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    /// <summary>
    /// 创建四边相同的外边距
    /// </summary>
    public static Margin All(Length value) => new(value);

    /// <summary>
    /// 创建垂直和水平外边距
    /// </summary>
    public static Margin Symmetric(Length vertical, Length horizontal) => new(vertical, horizontal);

    /// <summary>
    /// 从像素值创建四边相同的外边距
    /// </summary>
    public static implicit operator Margin(float value) => new(Length.Px(value));

    /// <summary>
    /// 从Length创建四边相同的外边距
    /// </summary>
    public static implicit operator Margin(Length value) => new(value);
}
