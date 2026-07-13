namespace Miko.Core.DomElements;

/// <summary>
/// 强制换行元素 (br)。
///
/// <c>br</c> 是行内级的空元素：它不产生任何可见盒，但会强制结束当前行盒，
/// 使其后的行内内容排到新的一行。布局阶段由
/// <see cref="Layout.LayoutAlgorithms.BlockLayout"/> 识别并作为“行分隔标记”处理
/// （见 <see cref="Layout.LayoutAlgorithms.BlockLayout.IsForcedLineBreak"/>）。
/// </summary>
public class BrElement : Element
{
    public override string TagName => "br";
}

/// <summary>
/// 主题分隔线元素 (hr)。
///
/// <c>hr</c> 是块级元素，浏览器默认渲染为一条水平分隔线。Miko 通过 UA 默认样式
/// （<see cref="Styling.StyleResolver"/>）为其设置一条上边框来绘制该线条，内容高度为 0，
/// 因此无需在渲染层做特殊处理，直接复用既有的边框绘制路径。
/// </summary>
public class HrElement : Element
{
    public override string TagName => "hr";
}
