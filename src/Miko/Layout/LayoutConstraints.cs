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
    /// 已由外部定型的内容宽度（目前由 flex 行方向主轴尺寸解析设置，见 ISSUE-106）。
    /// flex 项目的最终主轴尺寸由 flex 算法经 flex-basis（含百分比相对容器主轴的解析）、
    /// min/max 主轴夹取与 grow/shrink 分配后确定；若再让盒子自身按 width / min-width /
    /// max-width 对"已解析尺寸"求值，百分比长度会被二次应用（width:50% 的项目实际得到
    /// 50%×50%），后代的百分比尺寸也随之基于错误基准。
    /// 设置该值后，盒子直接以其为内容宽度，跳过自身 width 解析与该轴 min/max 夹取
    /// （二者均已在 flex 算法中相对正确的包含块完成）。
    /// </summary>
    public float? ResolvedContentWidth { get; set; }

    /// <summary>
    /// 已由外部定型的内容高度（flex 列方向主轴尺寸解析设置；语义同
    /// <see cref="ResolvedContentWidth"/>，见 ISSUE-106）。
    /// </summary>
    public float? ResolvedContentHeight { get; set; }

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
