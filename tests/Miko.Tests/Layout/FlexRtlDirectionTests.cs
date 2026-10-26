using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Flex layout under <c>direction: rtl</c> (ISSUE-157): a row's main-start is on the right, the
/// absolute <c>start</c>/<c>end</c> keywords follow the direction, and a column's cross-start is on
/// the right.
/// </summary>
public class FlexRtlDirectionTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static List<StyleSheet> Sheets(Style container) =>
    [
        new()
        {
            Rules =
            [
                new StyleRule { Selector = new ClassSelector("container"), Style = container },
                new StyleRule { Selector = new ClassSelector("item"), Style = new Style { Width = Length.Px(100), Height = Length.Px(60) } },
            ]
        }
    ];

    private LayoutBox Layout(Style container, int children = 3)
    {
        var root = new DivElement { Class = "container" };
        for (int i = 0; i < children; i++) root.AddChild(new DivElement { Class = "item" });
        return _layoutEngine.Layout(root, Sheets(container), 500, 600);
    }

    private static Style Row(FlexDirection direction = FlexDirection.Row, JustifyContent justify = JustifyContent.Normal) => new()
    {
        Display = Display.Flex,
        FlexDirection = direction,
        JustifyContent = justify,
        Direction = Direction.Rtl,
    };

    [Fact]
    public void Should_Start_A_Rtl_Row_At_The_Right()
    {
        var root = Layout(Row());

        root.Children.Select(c => c.BoxModel.Content.X).ShouldBe(new float[] { 400, 300, 200 });
    }

    [Fact]
    public void Should_Start_A_Rtl_Row_Reverse_At_The_Left()
    {
        var root = Layout(Row(FlexDirection.RowReverse));

        root.Children.Select(c => c.BoxModel.Content.X).ShouldBe(new float[] { 0, 100, 200 });
    }

    [Theory]
    [InlineData(JustifyContent.FlexEnd, new float[] { 200, 100, 0 })]
    [InlineData(JustifyContent.Start, new float[] { 400, 300, 200 })]
    [InlineData(JustifyContent.End, new float[] { 200, 100, 0 })]
    [InlineData(JustifyContent.Center, new float[] { 300, 200, 100 })]
    [InlineData(JustifyContent.SpaceBetween, new float[] { 400, 200, 0 })]
    public void Should_Justify_A_Rtl_Row(JustifyContent justify, float[] expected)
    {
        var root = Layout(Row(justify: justify));

        root.Children.Select(c => c.BoxModel.Content.X).ShouldBe(expected);
    }

    [Theory]
    // The last item is leftmost. Its auto margin takes the free space on its physical side:
    // margin-right (towards the previous item) pushes it to the left edge, margin-left keeps it
    // next to the previous item — as in the browser, margins are not mirrored by direction.
    [InlineData(true, 0)]
    [InlineData(false, 200)]
    public void Should_Keep_Physical_Auto_Margins_In_A_Rtl_Row(bool marginRightAuto, float expectedX)
    {
        var root = new DivElement { Class = "container" };
        root.AddChild(new DivElement { Class = "item" });
        root.AddChild(new DivElement { Class = "item" });
        root.AddChild(new DivElement
        {
            Class = "item",
            Style = marginRightAuto ? new Style { MarginRight = Length.Auto } : new Style { MarginLeft = Length.Auto },
        });

        var layout = _layoutEngine.Layout(root, Sheets(Row()), 500, 600);

        layout.Children[0].BoxModel.Content.X.ShouldBe(400, 0.01f);
        layout.Children[1].BoxModel.Content.X.ShouldBe(300, 0.01f);
        layout.Children[2].BoxModel.Content.X.ShouldBe(expectedX, 0.01f);
    }

    [Theory]
    [InlineData(AlignItems.FlexStart, 400)]
    [InlineData(AlignItems.FlexEnd, 0)]
    [InlineData(AlignItems.Center, 200)]
    public void Should_Align_A_Rtl_Column_From_The_Right(AlignItems align, float expectedX)
    {
        var root = Layout(new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            AlignItems = align,
            Direction = Direction.Rtl,
        }, children: 1);

        root.Children[0].BoxModel.Content.X.ShouldBe(expectedX, 0.01f);
    }

    [Fact]
    public void Should_Leave_Ltr_Rows_Unchanged()
    {
        var root = Layout(new Style { Display = Display.Flex });

        root.Children.Select(c => c.BoxModel.Content.X).ShouldBe(new float[] { 0, 100, 200 });
    }
}
