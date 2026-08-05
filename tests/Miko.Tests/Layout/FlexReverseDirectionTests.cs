using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Flex 反向主轴测试：flex-direction: row-reverse / column-reverse。
/// 反向 = 主轴镜像：项目在各行/列内顺序反转，且 justify-content 的 start/end 互换
/// （Center / SpaceBetween 等关于主轴中点对称的值保持不变）。
/// 回归：此前引擎把 RowReverse 当作 Row 处理（ion-checkbox labelPlacement="end" 无效）。
/// </summary>
public class FlexReverseDirectionTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>构造规则列表的便捷入口：类名 → 样式。</summary>
    private static List<StyleSheet> Sheets(params (string cls, Style style)[] rules)
        => new()
        {
            new()
            {
                Rules = rules.Select(r => new StyleRule
                {
                    Selector = new ClassSelector(r.cls),
                    Style = r.style
                }).ToList()
            }
        };

    private static DivElement BuildContainer(int children)
    {
        var container = new DivElement { Class = "container" };
        for (int i = 0; i < children; i++) container.AddChild(new DivElement { Class = "item" });
        return container;
    }

    private static readonly Style ItemStyle = new() { Width = Length.Px(100), Height = Length.Px(60) };

    [Fact]
    public void RowReverse_NoJustify_ItemsMirroredAndPackedToMainStart()
    {
        // CSS: row-reverse 的 main-start 在右缘；justify 初始值 normal(=flex-start) → 项目靠右排列，
        // 第一个 DOM 子元素在最右。500px 容器，3 个 100px 子项 → x: 400/300/200（DOM 顺序反转）。
        var root = _layoutEngine.Layout(BuildContainer(3), Sheets(
            ("container", new Style { Display = Display.Flex, FlexDirection = FlexDirection.RowReverse }),
            ("item", ItemStyle)), 500, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(400, 0.01f);
        root.Children[1].BoxModel.Content.X.ShouldBe(300, 0.01f);
        root.Children[2].BoxModel.Content.X.ShouldBe(200, 0.01f);
    }

    [Fact]
    public void Row_NoJustify_BaselineUnchanged()
    {
        // 对照组：row + normal → 靠左顺序排列。
        var root = _layoutEngine.Layout(BuildContainer(3), Sheets(
            ("container", new Style { Display = Display.Flex, FlexDirection = FlexDirection.Row }),
            ("item", ItemStyle)), 500, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(0, 0.01f);
        root.Children[1].BoxModel.Content.X.ShouldBe(100, 0.01f);
        root.Children[2].BoxModel.Content.X.ShouldBe(200, 0.01f);
    }

    [Fact]
    public void RowReverse_FlexEnd_PacksToLeft()
    {
        // row-reverse + justify-content: flex-end → main-end 在左缘 → 项目靠左排列（顺序仍反转）。
        var root = _layoutEngine.Layout(BuildContainer(3), Sheets(
            ("container", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.RowReverse,
                JustifyContent = JustifyContent.FlexEnd,
            }),
            ("item", ItemStyle)), 500, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(200, 0.01f);
        root.Children[1].BoxModel.Content.X.ShouldBe(100, 0.01f);
        root.Children[2].BoxModel.Content.X.ShouldBe(0, 0.01f);
    }

    [Fact]
    public void RowReverse_SpaceBetween_MirrorsItemOrder()
    {
        // space-between 关于主轴对称：首尾贴边，仅项目顺序反转。
        var root = _layoutEngine.Layout(BuildContainer(3), Sheets(
            ("container", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.RowReverse,
                JustifyContent = JustifyContent.SpaceBetween,
            }),
            ("item", ItemStyle)), 500, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(400, 0.01f);
        root.Children[1].BoxModel.Content.X.ShouldBe(200, 0.01f);
        root.Children[2].BoxModel.Content.X.ShouldBe(0, 0.01f);
    }

    [Fact]
    public void RowReverse_ShrinkWrappedInlineFlex_StillMirrorsOrder()
    {
        // 主轴尺寸不确定（inline-flex auto 宽度，shrink-to-fit）时无剩余空间，
        // 反转只改变视觉顺序：第一个 DOM 子元素排在最右。
        var wrapper = new DivElement();
        wrapper.AddChild(BuildContainer(2));

        var root = _layoutEngine.Layout(wrapper, Sheets(
            ("container", new Style { Display = Display.InlineFlex, FlexDirection = FlexDirection.RowReverse }),
            ("item", ItemStyle)), 500, 600);

        var box = root.Children[0];
        box.BoxModel.Content.Width.ShouldBe(200, 0.01f);
        box.Children[0].BoxModel.Content.X.ShouldBe(100, 0.01f);
        box.Children[1].BoxModel.Content.X.ShouldBe(0, 0.01f);
    }

    [Fact]
    public void RowReverse_Wrap_ReversesWithinEachLineOnly()
    {
        // 分行按文档序（前两个在第一行），仅行内主轴顺序反转；normal 翻转后靠右：
        // 行1 = [child1, child0]（child0 在右 x=300），行2 = [child2]（x=300）。
        var root = _layoutEngine.Layout(BuildContainer(3), Sheets(
            ("container", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.RowReverse,
                FlexWrap = FlexWrap.Wrap,
            }),
            ("item", new Style { Width = Length.Px(200), Height = Length.Px(60) })), 500, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(300, 0.01f);
        root.Children[0].BoxModel.Content.Y.ShouldBe(0, 0.01f);
        root.Children[1].BoxModel.Content.X.ShouldBe(100, 0.01f);
        root.Children[1].BoxModel.Content.Y.ShouldBe(0, 0.01f);
        root.Children[2].BoxModel.Content.X.ShouldBe(300, 0.01f);
        root.Children[2].BoxModel.Content.Y.ShouldBe(60, 0.01f);
    }

    [Fact]
    public void ColumnReverse_NoJustify_ItemsMirroredAndPackedToMainStart()
    {
        // column-reverse：main-start 在底缘。500px 高容器，3 个 60px 子项 → y: 440/380/320（DOM 顺序反转）。
        var root = _layoutEngine.Layout(BuildContainer(3), Sheets(
            ("container", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.ColumnReverse,
                Height = Length.Px(500),
            }),
            ("item", ItemStyle)), 500, 600);

        root.Children[0].BoxModel.Content.Y.ShouldBe(440, 0.01f);
        root.Children[1].BoxModel.Content.Y.ShouldBe(380, 0.01f);
        root.Children[2].BoxModel.Content.Y.ShouldBe(320, 0.01f);
    }

    [Fact]
    public void ColumnReverse_AutoHeight_StacksInReverseOrderFromTop()
    {
        // 高度 auto 时容器收缩包裹内容：内容盒高度由项目总高决定，main-start 镜像后
        // 等价于视觉顺序反转——第一个 DOM 子元素排在最下。
        var wrapper = new DivElement();
        wrapper.AddChild(BuildContainer(2));

        var root = _layoutEngine.Layout(wrapper, Sheets(
            ("container", new Style { Display = Display.Flex, FlexDirection = FlexDirection.ColumnReverse }),
            ("item", ItemStyle)), 500, 600);

        var col = root.Children[0];
        col.BoxModel.Content.Height.ShouldBe(120, 0.01f);
        col.Children[0].BoxModel.Content.Y.ShouldBe(60, 0.01f);
        col.Children[1].BoxModel.Content.Y.ShouldBe(0, 0.01f);
    }

    // ---- 绝对关键字 start / end（不随 reverse 翻转，ISSUE-116 问题4）----------
    //
    // CSS 有两组对齐关键字：flex 相对的 flex-start/flex-end 跟随主轴方向（row-reverse 时起点在
    // 右缘），书写方向相对的 start/end 则不受 flex-direction 影响（LTR 下 start 恒为左）。
    // Ionic 的表单控件用后者，故 labelPlacement="end"（row-reverse）叠加 justify="start" 时
    // 整体仍应靠左。

    [Fact]
    public void RowReverse_AbsoluteStart_PacksToLeft_NotFlipped()
    {
        // row-reverse + justify-content: start → 不翻转，项目靠左（顺序仍反转）。
        // 对比 RowReverse_NoJustify（flex-start 语义）靠右排列。
        var root = _layoutEngine.Layout(BuildContainer(3), Sheets(
            ("container", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.RowReverse,
                JustifyContent = JustifyContent.Start,
            }),
            ("item", ItemStyle)), 500, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(200, 0.01f);
        root.Children[1].BoxModel.Content.X.ShouldBe(100, 0.01f);
        root.Children[2].BoxModel.Content.X.ShouldBe(0, 0.01f);
    }

    [Fact]
    public void RowReverse_AbsoluteEnd_PacksToRight_NotFlipped()
    {
        // row-reverse + justify-content: end → 不翻转，项目靠右（顺序仍反转）。
        var root = _layoutEngine.Layout(BuildContainer(3), Sheets(
            ("container", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.RowReverse,
                JustifyContent = JustifyContent.End,
            }),
            ("item", ItemStyle)), 500, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(400, 0.01f);
        root.Children[1].BoxModel.Content.X.ShouldBe(300, 0.01f);
        root.Children[2].BoxModel.Content.X.ShouldBe(200, 0.01f);
    }

    [Fact]
    public void Row_AbsoluteStartAndEnd_MatchFlexStartAndFlexEnd()
    {
        // 非 reverse 方向上两组关键字等价（差异只在 reverse 时体现）。
        var start = _layoutEngine.Layout(BuildContainer(2), Sheets(
            ("container", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.Start,
            }),
            ("item", ItemStyle)), 500, 600);
        start.Children[0].BoxModel.Content.X.ShouldBe(0, 0.01f);
        start.Children[1].BoxModel.Content.X.ShouldBe(100, 0.01f);

        var end = _layoutEngine.Layout(BuildContainer(2), Sheets(
            ("container", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.End,
            }),
            ("item", ItemStyle)), 500, 600);
        end.Children[0].BoxModel.Content.X.ShouldBe(300, 0.01f);
        end.Children[1].BoxModel.Content.X.ShouldBe(400, 0.01f);
    }

    [Fact]
    public void ColumnReverse_AbsoluteStart_PacksToTop_NotFlipped()
    {
        // 列方向同理：column-reverse + start → 靠顶部（flex-start 语义会靠底部）。
        var root = _layoutEngine.Layout(BuildContainer(3), Sheets(
            ("container", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.ColumnReverse,
                Height = Length.Px(500),
                JustifyContent = JustifyContent.Start,
            }),
            ("item", ItemStyle)), 500, 600);

        root.Children[0].BoxModel.Content.Y.ShouldBe(120, 0.01f);
        root.Children[1].BoxModel.Content.Y.ShouldBe(60, 0.01f);
        root.Children[2].BoxModel.Content.Y.ShouldBe(0, 0.01f);
    }
}
