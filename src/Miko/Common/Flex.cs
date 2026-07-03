namespace Miko.Common;

/// <summary>
/// CSS <c>flex</c> 简写值，聚合 <c>flex-grow</c> / <c>flex-shrink</c> / <c>flex-basis</c> 三个分量。
///
/// 支持多种写法：
/// <code>
/// new Flex(1, 1, Length.Px(0))     // grow shrink basis
/// new Flex(1, 1, Length.Px(150))
/// new Flex(1, 1, Length.Percent(0))
/// new Flex(1, 1, Length.Auto)
/// Flex.None                         // 0 0 auto
/// Flex.Auto                         // 1 1 auto
/// (Flex)1                           // 1 1 0%（由 float/int 隐式转换）
/// </code>
/// </summary>
public readonly struct Flex
{
    /// <summary>沿主轴分配剩余空间的增长系数（flex-grow）。</summary>
    public float Grow { get; }

    /// <summary>空间不足时的收缩系数（flex-shrink）。</summary>
    public float Shrink { get; }

    /// <summary>分配前的初始主轴尺寸（flex-basis）。</summary>
    public Length Basis { get; }

    public Flex(float grow, float shrink, Length basis)
    {
        Grow = grow;
        Shrink = shrink;
        Basis = basis;
    }

    /// <summary>
    /// <c>flex: none</c> → <c>0 0 auto</c>：不增长、不收缩，尺寸取内容/basis。
    /// </summary>
    public static Flex None => new Flex(0, 0, Length.Auto);

    /// <summary>
    /// <c>flex: auto</c> → <c>1 1 auto</c>：按内容尺寸出发，可增长可收缩。
    /// </summary>
    public static Flex Auto => new Flex(1, 1, Length.Auto);

    /// <summary>
    /// <c>flex: N</c> → <c>N 1 0%</c>：单数值简写，等比分配可用空间。
    /// 保持与旧的 <c>float</c> 写法兼容。
    /// </summary>
    public static implicit operator Flex(float value) => new Flex(value, 1, Length.Percent(0));

    /// <summary>
    /// <c>flex: N</c> → <c>N 1 0%</c>：整数简写。
    /// </summary>
    public static implicit operator Flex(int value) => new Flex(value, 1, Length.Percent(0));

    public override string ToString() => $"{Grow} {Shrink} {Basis}";
}
