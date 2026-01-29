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

    public override string ToString() => $"LayoutBox({Element.TagName}, Type: {Type})";
}
