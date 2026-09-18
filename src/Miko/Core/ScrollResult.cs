using Miko.Layout;

namespace Miko.Core;

/// <summary>
/// 一次 <see cref="MikoEngine.ScrollByDetailed"/> 的结果：滚动落在哪个容器上、消费了多少增量、
/// 还剩多少没被消费（ISSUE-135）。
///
/// <para><b>余量为什么重要</b>：手指推到边界的那一刻，增量往往只被消费了一部分——比如距顶端还有
/// 5px 却推了 30px，消费 5、余 25。橡皮筋要拉伸的正是这 25，用「滚动是否发生」这个布尔值
/// 表达不出来。完全到边界时消费为 0、余量即全部增量。</para>
///
/// <para><see cref="Box"/> 为 null 表示没找到可滚动容器（未初始化布局、点外部空白、
/// 或该方向上没有任何容器可滚），此时余量无意义、恒为 0。</para>
/// </summary>
/// <param name="Box">承载本次滚动的容器；没有则为 null。</param>
/// <param name="ConsumedX">水平方向实际滚动的距离。</param>
/// <param name="ConsumedY">垂直方向实际滚动的距离。</param>
/// <param name="RemainingX">水平方向未被消费的增量（越界部分）。</param>
/// <param name="RemainingY">垂直方向未被消费的增量（越界部分）。</param>
public readonly record struct ScrollResult(
    LayoutBox? Box,
    float ConsumedX,
    float ConsumedY,
    float RemainingX,
    float RemainingY)
{
    /// <summary>没有容器承接本次滚动。</summary>
    public static readonly ScrollResult None = default;

    /// <summary>
    /// 滚动位置是否真的变了。这是 <see cref="MikoEngine.ScrollBy"/> 的返回值，
    /// 阈值与它历史上的判定一致（0.01px），以免浮点噪声被当成滚动。
    /// </summary>
    public bool Scrolled => MathF.Abs(ConsumedX) > 0.01f || MathF.Abs(ConsumedY) > 0.01f;

    /// <summary>该轴上是否有越界余量（同样按 0.01px 阈值忽略浮点噪声）。</summary>
    public bool HasRemainingX => MathF.Abs(RemainingX) > 0.01f;

    /// <inheritdoc cref="HasRemainingX"/>
    public bool HasRemainingY => MathF.Abs(RemainingY) > 0.01f;
}
