namespace Miko.Layout;

/// <summary>
/// 布局约束
/// </summary>
public class LayoutConstraints
{
    /// <summary>
    /// 可用宽度
    /// </summary>
    public float? AvailableWidth { get; set; }

    /// <summary>
    /// 可用高度
    /// </summary>
    public float? AvailableHeight { get; set; }

    /// <summary>
    /// <see cref="AvailableHeight"/> 是否同时作为"填充指令"。
    /// 为 true 时，height:auto 且 overflow 非 visible 的盒子把 AvailableHeight 当作自身高度
    /// （布局期即定型，滚动度量与子孙百分比高度都基于它）——仅用于该盒子的最终高度本就被
    /// 外部强制为 AvailableHeight 的场景：flex 行向 stretch、flex 列向 grow/shrink 定型、
    /// grid area stretch、根视口。
    /// 为 false（默认）时 AvailableHeight 仅是后代百分比高度的解析基准，height:auto 恒由
    /// 内容决定（CSS 块级流语义；否则块流中定高父级的 overflow 子元素会被错误撑满父高，
    /// 见 ISSUE-105）。
    /// </summary>
    public bool FillAvailableHeight { get; set; }

    /// <summary>
    /// 是否为无限宽度
    /// </summary>
    public bool IsInfiniteWidth => !AvailableWidth.HasValue;

    /// <summary>
    /// 是否为无限高度
    /// </summary>
    public bool IsInfiniteHeight => !AvailableHeight.HasValue;

    public LayoutConstraints() { }

    public LayoutConstraints(float? width, float? height)
    {
        AvailableWidth = width;
        AvailableHeight = height;
    }

    public override string ToString() => $"Constraints(W: {AvailableWidth?.ToString() ?? "∞"}, H: {AvailableHeight?.ToString() ?? "∞"})";
}
