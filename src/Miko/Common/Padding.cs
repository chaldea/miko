namespace Miko.Common;

/// <summary>
/// 内边距简写类型（类似CSS的padding简写）
/// </summary>
public struct Padding
{
    public Length Top { get; set; }
    public Length Right { get; set; }
    public Length Bottom { get; set; }
    public Length Left { get; set; }

    /// <summary>
    /// 四边相同的内边距
    /// </summary>
    public Padding(Length all)
    {
        Top = Right = Bottom = Left = all;
    }

    /// <summary>
    /// 垂直和水平内边距
    /// </summary>
    public Padding(Length vertical, Length horizontal)
    {
        Top = Bottom = vertical;
        Right = Left = horizontal;
    }

    /// <summary>
    /// 上、水平、下内边距
    /// </summary>
    public Padding(Length top, Length horizontal, Length bottom)
    {
        Top = top;
        Right = Left = horizontal;
        Bottom = bottom;
    }

    /// <summary>
    /// 四边分别指定内边距
    /// </summary>
    public Padding(Length top, Length right, Length bottom, Length left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    /// <summary>
    /// 创建四边相同的内边距
    /// </summary>
    public static Padding All(Length value) => new(value);

    /// <summary>
    /// 创建垂直和水平内边距
    /// </summary>
    public static Padding Symmetric(Length vertical, Length horizontal) => new(vertical, horizontal);

    /// <summary>
    /// 从像素值创建四边相同的内边距
    /// </summary>
    public static implicit operator Padding(float value) => new(Length.Px(value));

    /// <summary>
    /// 从Length创建四边相同的内边距
    /// </summary>
    public static implicit operator Padding(Length value) => new(value);
}
