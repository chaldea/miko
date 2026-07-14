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
