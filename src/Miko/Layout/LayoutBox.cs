using Miko.Common;
using Miko.Core;
using Miko.Styling;

namespace Miko.Layout;

/// <summary>
/// 布局盒子
/// </summary>
public class LayoutBox
{
    public Element Element { get; set; } = null!;
    public ComputedStyle ComputedStyle { get; set; } = null!;

    // 盒子维度
    public BoxModel BoxModel { get; set; } = new();

    // 子盒子
    public List<LayoutBox> Children { get; set; } = new();

    // 布局类型
    public LayoutType Type { get; set; }

    // 滚动状态
    public float ScrollTop { get; set; }
    public float ScrollLeft { get; set; }

    // 内容实际尺寸（可能超出 Content 区域）
    public float ScrollableContentWidth { get; set; }
    public float ScrollableContentHeight { get; set; }

    // ---- 行内盒的逐行片段（ISSUE-126）----
    // 非替换 inline 盒在行内格式化上下文中是「透明」的：其内容参与父级行内流、可跨多行断开，
    // 因此它自身的可视几何不是单个矩形，而是每条行盒上一段矩形（浏览器的 inline box fragment）。
    // 元素存**绝对坐标的 border box** 矩形（不像 TextNode.LayoutFragments 那样相对内容盒原点——
    // 跨行 inline 盒没有单一内容盒可作基准）；BoxModel.Content 仍是全部片段的并集，
    // 供 ScrollableContent* 度量、绝对定位包含块等既有消费者使用。
    // 为 null 表示未经行内断行（inline-block / flex 项 / 块级盒等），按 BorderBox 单矩形绘制。
    internal List<RectF>? InlineFragments;

    // 是否需要显示滚动条
    // ScrollableContent* 表示包含内边距的滚动区域，因此与 padding box 的视口尺寸比较。
    public bool HasVerticalScrollbar => ComputedStyle.OverflowY == Overflow.Scroll ||
        (ComputedStyle.OverflowY == Overflow.Auto && ScrollableContentHeight > BoxModel.PaddingBox.Height + 0.01f);
    public bool HasHorizontalScrollbar => ComputedStyle.OverflowX == Overflow.Scroll ||
        (ComputedStyle.OverflowX == Overflow.Auto && ScrollableContentWidth > BoxModel.PaddingBox.Width + 0.01f);

    // Classic 滚动条宽度（占用布局空间）
    public const float ScrollbarThickness = 12f;

    public override string ToString() => $"LayoutBox({Element.TagName}, Type: {Type})";
}
