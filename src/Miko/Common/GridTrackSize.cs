namespace Miko.Common;

/// <summary>
/// CSS Grid 轨道尺寸（grid-template-columns / grid-template-rows / grid-auto-rows 等的单个轨道）。
/// 三选一：固定长度（px/em/rem/% 等 <see cref="Length"/> 分量）、fr 分数（分配剩余空间）、
/// 或 auto（由该轨道内子项的内容尺寸决定）。
///
/// 支持写法：
/// <code>
/// GridTrackSize.Px(100)          // 100px 固定轨道
/// GridTrackSize.Percent(50)      // 50%（相对容器对应轴的内容尺寸）
/// GridTrackSize.Fr(1)            // 1fr：按比例分配剩余空间
/// GridTrackSize.Auto             // auto：内容尺寸
/// (GridTrackSize)100f            // 100px（float/int 隐式转换）
/// (GridTrackSize)Length.Em(2)    // 任意 Length
/// </code>
/// </summary>
public readonly struct GridTrackSize
{
    /// <summary>轨道类别判别。</summary>
    public enum TrackKind : byte { Fixed, Fraction, Auto }

    /// <summary>当前轨道为固定长度 / fr 分数 / auto 中的哪一种。</summary>
    public TrackKind Kind { get; }

    /// <summary>固定长度（仅 <see cref="Kind"/> 为 Fixed 时有意义）。</summary>
    public Length Fixed { get; }

    /// <summary>fr 分数（仅 <see cref="Kind"/> 为 Fraction 时有意义，&gt; 0）。</summary>
    public float Fraction { get; }

    private GridTrackSize(TrackKind kind, Length fixedLength, float fraction)
    {
        Kind = kind;
        Fixed = fixedLength;
        Fraction = fraction;
    }

    /// <summary>该轨道是否为 fr 分数轨道。</summary>
    public bool IsFraction => Kind == TrackKind.Fraction;

    /// <summary>该轨道是否为 auto（内容尺寸）轨道。</summary>
    public bool IsAuto => Kind == TrackKind.Auto;

    /// <summary>该轨道是否为固定长度轨道（含百分比）。</summary>
    public bool IsFixed => Kind == TrackKind.Fixed;

    /// <summary>固定像素轨道。</summary>
    public static GridTrackSize Px(float value) => new(TrackKind.Fixed, Length.Px(value), 0);

    /// <summary>百分比轨道（相对容器对应轴的内容尺寸；对不确定尺寸轴解析时退化为 auto）。</summary>
    public static GridTrackSize Percent(float value) => new(TrackKind.Fixed, Length.Percent(value), 0);

    /// <summary>fr 分数轨道：按比例分配固定/auto 轨道与 gap 之外的剩余空间。</summary>
    public static GridTrackSize Fr(float value) => new(TrackKind.Fraction, default, value);

    /// <summary>auto 轨道：由该轨道内子项的内容尺寸决定。</summary>
    public static GridTrackSize Auto => new(TrackKind.Auto, default, 0);

    /// <summary>由任意 <see cref="Length"/> 构造固定轨道（auto 长度视为 auto 轨道）。</summary>
    public static GridTrackSize FromLength(Length length)
        => length.IsAuto ? Auto : new GridTrackSize(TrackKind.Fixed, length, 0);

    /// <summary><c>100f</c> → 100px 固定轨道。</summary>
    public static implicit operator GridTrackSize(float value) => Px(value);

    /// <summary><c>100</c> → 100px 固定轨道。</summary>
    public static implicit operator GridTrackSize(int value) => Px(value);

    /// <summary>任意 <see cref="Length"/> → 固定轨道（auto → auto 轨道）。</summary>
    public static implicit operator GridTrackSize(Length length) => FromLength(length);

    public override string ToString() => Kind switch
    {
        TrackKind.Fraction => $"{Fraction}fr",
        TrackKind.Auto => "auto",
        _ => Fixed.ToString(),
    };
}
