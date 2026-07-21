using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// inline-flex 布局测试（ISSUE-097）：行内级外层（shrink-to-fit、参与行内行流）
/// + flex 容器内层（justify/align/wrap/grow/order 等与 flex 一致）。
/// </summary>
public class InlineFlexLayoutTests
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

    [Fact]
    public void InlineFlex_DisplayMapsToInlineFlexLayoutType()
    {
        var container = new DivElement { Class = "iflex" };

        var root = _layoutEngine.Layout(container,
            Sheets(("iflex", new Style { Display = Display.InlineFlex })), 800, 600);

        root.Type.ShouldBe(LayoutType.InlineFlex);
        root.ComputedStyle.Display.ShouldBe(Display.InlineFlex);
    }

    [Fact]
    public void InlineFlex_AutoWidth_ShrinksToFitChildren()
    {
        // 800px 块级父容器中的 inline-flex：宽度应由内容决定（2×100=200），而非填满 800。
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "it" });
        iflex.AddChild(new DivElement { Class = "it" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex }),
            ("it", new Style { Width = Length.Px(100), Height = Length.Px(50) })), 800, 600);

        var box = root.Children[0];
        box.Type.ShouldBe(LayoutType.InlineFlex);
        box.BoxModel.Content.Width.ShouldBe(200);
        box.BoxModel.Content.Height.ShouldBe(50);
        box.Children[0].BoxModel.Content.X.ShouldBe(0);
        box.Children[1].BoxModel.Content.X.ShouldBe(100);
    }

    [Fact]
    public void InlineFlex_FlowsInlineWithInlineSiblings()
    {
        // inline-flex 是行内级盒：与后续 inline-block 兄弟排在同一行。
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "it" });
        container.AddChild(iflex);
        container.AddChild(new DivElement { Class = "ib" });

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex }),
            ("it", new Style { Width = Length.Px(100), Height = Length.Px(50) }),
            ("ib", new Style { Display = Display.InlineBlock, Width = Length.Px(30), Height = Length.Px(20) })), 800, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[0].BoxModel.Content.Y.ShouldBe(0);
        // 兄弟紧跟 inline-flex 的 margin-box 右缘，且在同一行（Y 相同）。
        root.Children[1].BoxModel.Content.X.ShouldBe(100);
        root.Children[1].BoxModel.Content.Y.ShouldBe(0);
        // 行高取行内盒最大值。
        root.BoxModel.Content.Height.ShouldBe(50);
    }

    [Fact]
    public void InlineFlex_ExplicitWidth_UsesSpecifiedWidth()
    {
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "it" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex, Width = Length.Px(400) }),
            ("it", new Style { Width = Length.Px(100), Height = Length.Px(50) })), 800, 600);

        root.Children[0].BoxModel.Content.Width.ShouldBe(400);
    }

    [Fact]
    public void InlineFlex_PercentWidth_ResolvesAgainstContainingBlock()
    {
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "it" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex, Width = Length.Percent(50) }),
            ("it", new Style { Width = Length.Px(100), Height = Length.Px(50) })), 800, 600);

        root.Children[0].BoxModel.Content.Width.ShouldBe(400);
    }

    [Fact]
    public void InlineFlex_JustifyContent_DistributesWithinDefiniteWidth()
    {
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "it" });
        iflex.AddChild(new DivElement { Class = "it" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex, Width = Length.Px(400), JustifyContent = JustifyContent.Center }),
            ("it", new Style { Width = Length.Px(100), Height = Length.Px(50) })), 800, 600);

        var box = root.Children[0];
        box.Children[0].BoxModel.Content.X.ShouldBe(100);
        box.Children[1].BoxModel.Content.X.ShouldBe(200);
    }

    [Fact]
    public void InlineFlex_AlignItems_Center_OffsetsCrossAxis()
    {
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "it" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style
            {
                Display = Display.InlineFlex,
                Width = Length.Px(400),
                Height = Length.Px(100),
                AlignItems = AlignItems.Center
            }),
            ("it", new Style { Width = Length.Px(100), Height = Length.Px(50) })), 800, 600);

        root.Children[0].Children[0].BoxModel.Content.Y.ShouldBe(25);
    }

    [Fact]
    public void InlineFlex_ColumnDirection_ShrinkWrapsWidthAndHeight()
    {
        // 列方向 shrink-to-fit：宽 = 最宽子项（100），高 = 子项高度和（50+30=80）。
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "a" });
        iflex.AddChild(new DivElement { Class = "b" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex, FlexDirection = FlexDirection.Column }),
            ("a", new Style { Width = Length.Px(100), Height = Length.Px(50) }),
            ("b", new Style { Width = Length.Px(80), Height = Length.Px(30) })), 800, 600);

        var box = root.Children[0];
        box.BoxModel.Content.Width.ShouldBe(100);
        box.BoxModel.Content.Height.ShouldBe(80);
    }

    [Fact]
    public void Flex_ColumnDirection_AutoWidth_ShrinkWrapsToWidestChild()
    {
        // 回归（ISSUE-097 附带修复）：普通 flex 列容器作为 flex 行的 auto 宽度子项时，
        // 宽度应收缩包裹到最宽子项，而非塌缩为 0。
        var container = new DivElement { Class = "row" };
        var column = new DivElement { Class = "col" };
        column.AddChild(new DivElement { Class = "a" });
        column.AddChild(new DivElement { Class = "b" });
        container.AddChild(column);

        var root = _layoutEngine.Layout(container, Sheets(
            ("row", new Style { Display = Display.Flex }),
            ("col", new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column }),
            ("a", new Style { Width = Length.Px(100), Height = Length.Px(50) }),
            ("b", new Style { Width = Length.Px(80), Height = Length.Px(30) })), 800, 600);

        var box = root.Children[0];
        box.BoxModel.Content.Width.ShouldBe(100);
        box.BoxModel.Content.Height.ShouldBe(80);
    }

    [Fact]
    public void InlineFlex_WrapWithAutoWidth_StaysOnSingleLine()
    {
        // 主轴不确定（shrink-to-fit）时没有可对照的可用宽度：wrap 退化为单行。
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        for (int i = 0; i < 3; i++) iflex.AddChild(new DivElement { Class = "it" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex, FlexWrap = FlexWrap.Wrap }),
            ("it", new Style { Width = Length.Px(100), Height = Length.Px(50) })), 800, 600);

        var box = root.Children[0];
        box.BoxModel.Content.Width.ShouldBe(300);
        box.BoxModel.Content.Height.ShouldBe(50);
        foreach (var child in box.Children)
            child.BoxModel.Content.Y.ShouldBe(0);
    }

    [Fact]
    public void InlineFlex_WrapWithExplicitWidth_WrapsNormally()
    {
        // 显式宽度 250：3 个 100px 子项 → 每行 2 个，第三个换行。
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        for (int i = 0; i < 3; i++) iflex.AddChild(new DivElement { Class = "it" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex, Width = Length.Px(250), FlexWrap = FlexWrap.Wrap }),
            ("it", new Style { Width = Length.Px(100), Height = Length.Px(50) })), 800, 600);

        var box = root.Children[0];
        box.Children[0].BoxModel.Content.Y.ShouldBe(0);
        box.Children[1].BoxModel.Content.Y.ShouldBe(0);
        box.Children[2].BoxModel.Content.Y.ShouldBe(50);
        box.BoxModel.Content.Height.ShouldBe(100);
    }

    [Fact]
    public void InlineFlex_Gap_CountsInShrinkWrapWidth()
    {
        // 收缩宽度应包含主轴 gap：2×100 + 10 = 210。
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "it" });
        iflex.AddChild(new DivElement { Class = "it" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex, Gap = Length.Px(10) }),
            ("it", new Style { Width = Length.Px(100), Height = Length.Px(50) })), 800, 600);

        var box = root.Children[0];
        box.BoxModel.Content.Width.ShouldBe(210);
        box.Children[1].BoxModel.Content.X.ShouldBe(110);
    }

    [Fact]
    public void InlineFlex_MinMaxWidth_ClampsShrinkToFit()
    {
        var container = new DivElement();
        var withMin = new DivElement { Class = "min" };
        withMin.AddChild(new DivElement { Class = "it" });
        withMin.AddChild(new DivElement { Class = "it" });
        var withMax = new DivElement { Class = "max" };
        withMax.AddChild(new DivElement { Class = "it" });
        withMax.AddChild(new DivElement { Class = "it" });
        container.AddChild(withMin);
        container.AddChild(withMax);

        var root = _layoutEngine.Layout(container, Sheets(
            ("min", new Style { Display = Display.InlineFlex, MinWidth = Length.Px(250) }),
            ("max", new Style { Display = Display.InlineFlex, MaxWidth = Length.Px(150) }),
            ("it", new Style { Width = Length.Px(100), Height = Length.Px(50) })), 800, 600);

        root.Children[0].BoxModel.Content.Width.ShouldBe(250);
        root.Children[1].BoxModel.Content.Width.ShouldBe(150);
    }

    [Fact]
    public void InlineFlex_Order_ReordersItems()
    {
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "a" });
        iflex.AddChild(new DivElement { Class = "b" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex }),
            ("a", new Style { Width = Length.Px(100), Height = Length.Px(50), Order = 2 }),
            ("b", new Style { Width = Length.Px(50), Height = Length.Px(50), Order = 1 })), 800, 600);

        var box = root.Children[0];
        // order 1（b，宽 50）排在前，order 2（a，宽 100）紧随其后。
        box.Children[1].BoxModel.Content.X.ShouldBe(0);
        box.Children[0].BoxModel.Content.X.ShouldBe(50);
    }

    [Fact]
    public void InlineFlex_FlexGrow_DistributesWithinExplicitWidth()
    {
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "it" });
        iflex.AddChild(new DivElement { Class = "it" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex, Width = Length.Px(300) }),
            ("it", new Style { Width = Length.Px(50), Height = Length.Px(50), FlexGrow = 1 })), 800, 600);

        var box = root.Children[0];
        // 剩余 200 均分：各项 50 + 100 = 150。
        box.Children[0].BoxModel.Content.Width.ShouldBe(150);
        box.Children[1].BoxModel.Content.Width.ShouldBe(150);
        box.Children[1].BoxModel.Content.X.ShouldBe(150);
    }

    [Fact]
    public void InlineFlex_AutoHeight_EqualsMaxChildCrossSize()
    {
        var container = new DivElement();
        var iflex = new DivElement { Class = "iflex" };
        iflex.AddChild(new DivElement { Class = "a" });
        iflex.AddChild(new DivElement { Class = "b" });
        container.AddChild(iflex);

        var root = _layoutEngine.Layout(container, Sheets(
            ("iflex", new Style { Display = Display.InlineFlex }),
            ("a", new Style { Width = Length.Px(100), Height = Length.Px(50) }),
            ("b", new Style { Width = Length.Px(100), Height = Length.Px(80) })), 800, 600);

        root.Children[0].BoxModel.Content.Height.ShouldBe(80);
    }
}
