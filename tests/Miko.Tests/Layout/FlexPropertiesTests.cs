using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-088：Flex 布局补充属性测试 —— order、align-self、align-content、flex-wrap: wrap-reverse。
/// </summary>
public class FlexPropertiesTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>构建一个 flex 行容器，含若干带指定 order 的等宽子元素。</summary>
    private static DivElement BuildRow(params Style[] childStyles)
    {
        var container = new DivElement
        {
            Style = new Style { Display = Display.Flex, Width = Length.Px(600), Height = Length.Px(100) }
        };
        foreach (var s in childStyles)
        {
            s.Width ??= Length.Px(100);
            s.Height ??= Length.Px(50);
            container.AddChild(new DivElement { Style = s });
        }
        return container;
    }

    [Fact]
    public void Order_ReordersFlexItems_AlongMainAxis()
    {
        // 三个子元素，order 分别为 2 / 0 / 1 → 视觉顺序应为 child1(0), child2(1), child0(2)。
        var container = BuildRow(
            new Style { Order = 2 },   // child0
            new Style { Order = 0 },   // child1
            new Style { Order = 1 });  // child2

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        // 主轴 X 位置反映 order 排序：order=0 的 child1 在最左。
        root.Children[1].BoxModel.Content.X.ShouldBe(0);    // order 0
        root.Children[2].BoxModel.Content.X.ShouldBe(100);  // order 1
        root.Children[0].BoxModel.Content.X.ShouldBe(200);  // order 2
    }

    [Fact]
    public void Order_Default_PreservesDomOrder()
    {
        // 无 order（全 0）时保持 DOM 顺序。
        var container = BuildRow(new Style(), new Style(), new Style());

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[1].BoxModel.Content.X.ShouldBe(100);
        root.Children[2].BoxModel.Content.X.ShouldBe(200);
    }

    [Fact]
    public void AlignSelf_Center_OverridesContainerAlignItems()
    {
        // 容器 align-items: flex-start，某子项 align-self: center → 该子项在交叉轴居中。
        var container = new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                AlignItems = AlignItems.FlexStart,
                Width = Length.Px(400),
                Height = Length.Px(200)
            }
        };
        // 普通子项（贴顶）。
        container.AddChild(new DivElement { Style = new Style { Width = Length.Px(50), Height = Length.Px(40) } });
        // align-self: center 的子项。
        container.AddChild(new DivElement
        {
            Style = new Style { Width = Length.Px(50), Height = Length.Px(40), AlignSelf = AlignSelf.Center }
        });

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        // 普通子项贴顶（Y=0）。
        root.Children[0].BoxModel.Content.Y.ShouldBe(0);
        // align-self: center 的子项在 200 高度内垂直居中：(200-40)/2 = 80。
        root.Children[1].BoxModel.Content.Y.ShouldBe(80);
    }

    [Fact]
    public void AlignSelf_FlexEnd_AlignsItemToCrossEnd()
    {
        var container = new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                AlignItems = AlignItems.FlexStart,
                Width = Length.Px(400),
                Height = Length.Px(200)
            }
        };
        container.AddChild(new DivElement
        {
            Style = new Style { Width = Length.Px(50), Height = Length.Px(40), AlignSelf = AlignSelf.FlexEnd }
        });

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        // flex-end：贴交叉轴末端 → Y = 200 - 40 = 160。
        root.Children[0].BoxModel.Content.Y.ShouldBe(160);
    }

    [Fact]
    public void WrapReverse_ReversesLineOrderOnCrossAxis()
    {
        // 容器 250px 宽、200px 高，3 个 100px 子项，wrap-reverse。
        // 每行 2 个：line0 = [0,1]，line1 = [2]。wrap-reverse 使 line1 排在交叉轴起点（Y=0）。
        // align-content 显式 FlexStart：隔离本用例意图（行序反转），排除默认 stretch 的行增大。
        var container = new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                FlexWrap = FlexWrap.WrapReverse,
                AlignContent = AlignContent.FlexStart,
                Width = Length.Px(250),
                Height = Length.Px(200)
            }
        };
        for (int i = 0; i < 3; i++)
            container.AddChild(new DivElement { Style = new Style { Width = Length.Px(100), Height = Length.Px(50) } });

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        // child2 单独成行，wrap-reverse 后排在第一行（Y=0）。
        root.Children[2].BoxModel.Content.Y.ShouldBe(0);
        // child0/child1 在第二行（Y=50）。
        root.Children[0].BoxModel.Content.Y.ShouldBe(50);
        root.Children[1].BoxModel.Content.Y.ShouldBe(50);
    }

    [Fact]
    public void WrapReverse_StillWraps_LikeWrap()
    {
        // wrap-reverse 与 wrap 一样会换行（区别仅交叉轴方向）。
        var container = new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                FlexWrap = FlexWrap.WrapReverse,
                Width = Length.Px(250),
                Height = Length.Px(200)
            }
        };
        for (int i = 0; i < 3; i++)
            container.AddChild(new DivElement { Style = new Style { Width = Length.Px(100), Height = Length.Px(50) } });

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        // 3 项分两行：说明确实换行了（若不换行，child2 会与 child0/1 同 Y）。
        var uniqueYs = new HashSet<float>
        {
            root.Children[0].BoxModel.Content.Y,
            root.Children[1].BoxModel.Content.Y,
            root.Children[2].BoxModel.Content.Y
        };
        uniqueYs.Count.ShouldBe(2);
    }

    [Fact]
    public void AlignContent_Center_CentersLinesInCrossAxis()
    {
        // 容器 250px 宽、300px 高，3 个 100×50 子项，wrap + align-content: center。
        // 两行各 50px 高，总 100px；剩余 200px 在上下均分 → 首行 Y = 100。
        var container = new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                AlignContent = AlignContent.Center,
                Width = Length.Px(250),
                Height = Length.Px(300)
            }
        };
        for (int i = 0; i < 3; i++)
            container.AddChild(new DivElement { Style = new Style { Width = Length.Px(100), Height = Length.Px(50) } });

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        // 第一行（child0/1）Y = (300 - 100) / 2 = 100。
        root.Children[0].BoxModel.Content.Y.ShouldBe(100);
        root.Children[1].BoxModel.Content.Y.ShouldBe(100);
        // 第二行（child2）Y = 100 + 50 = 150。
        root.Children[2].BoxModel.Content.Y.ShouldBe(150);
    }

    [Fact]
    public void AlignContent_FlexEnd_PushesLinesToCrossEnd()
    {
        var container = new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                AlignContent = AlignContent.FlexEnd,
                Width = Length.Px(250),
                Height = Length.Px(300)
            }
        };
        for (int i = 0; i < 3; i++)
            container.AddChild(new DivElement { Style = new Style { Width = Length.Px(100), Height = Length.Px(50) } });

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        // 两行总高 100，flex-end 下推 200 → 首行 Y = 200，次行 Y = 250。
        root.Children[0].BoxModel.Content.Y.ShouldBe(200);
        root.Children[2].BoxModel.Content.Y.ShouldBe(250);
    }

    [Fact]
    public void AlignContent_SpaceBetween_DistributesLines()
    {
        var container = new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                AlignContent = AlignContent.SpaceBetween,
                Width = Length.Px(250),
                Height = Length.Px(300)
            }
        };
        for (int i = 0; i < 3; i++)
            container.AddChild(new DivElement { Style = new Style { Width = Length.Px(100), Height = Length.Px(50) } });

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        // space-between：首行贴顶（Y=0），末行贴底（Y = 300 - 50 = 250）。
        root.Children[0].BoxModel.Content.Y.ShouldBe(0);
        root.Children[2].BoxModel.Content.Y.ShouldBe(250);
    }
}
