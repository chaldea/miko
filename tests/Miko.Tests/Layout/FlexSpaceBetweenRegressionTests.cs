using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-085 问题3：修复后的 flex 布局破坏了 JustifyContent.SpaceBetween。
/// Test2 被挤到 container 外面，不在容器范围内。
/// </summary>
public class FlexSpaceBetweenRegressionTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void SpaceBetween_WithTwoChildren_ShouldStayWithinContainer()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
            },
            [".root"] = new()
            {
                Width = Length.Px(600),
                Height = Length.Px(600)
            },
            [".container"] = new()
            {
                Width = Length.Px(200),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.SpaceBetween,
                PaddingTop = Length.Px(4),
                PaddingBottom = Length.Px(4),
            }
        });

        var child1 = new DivElement { TextContent = "Test1" };
        var child2 = new DivElement { TextContent = "Test2" };
        var container = new DivElement { Class = "container", Children = { child1, child2 } };
        var root = new DivElement { Class = "root", Children = { container } };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];
        var child1Box = containerBox.Children[0];
        var child2Box = containerBox.Children[1];

        float containerRight = containerBox.BoxModel.Content.Right;

        // child1 应该在左边（flex-start）
        child1Box.BoxModel.MarginBox.Left.ShouldBe(containerBox.BoxModel.Content.Left, 0.5f,
            "Child1 should be at the left edge");

        // child2 应该在右边（flex-end），但必须在容器内部
        child2Box.BoxModel.MarginBox.Right.ShouldBeLessThanOrEqualTo(containerRight,
            "Child2 should not overflow container");

        // child2 应该靠近右边缘（space-between效果）
        float expectedChild2Right = containerRight;
        child2Box.BoxModel.MarginBox.Right.ShouldBe(expectedChild2Right, 1f,
            "Child2 should be at the right edge for space-between");
    }

    [Fact]
    public void SpaceBetween_WithThreeChildren_ShouldDistributeEvenly()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".container"] = new()
            {
                Width = Length.Px(300),
                Display = Display.Flex,
                JustifyContent = JustifyContent.SpaceBetween,
            }
        });

        var child1 = new DivElement { TextContent = "A" };
        var child2 = new DivElement { TextContent = "B" };
        var child3 = new DivElement { TextContent = "C" };
        var container = new DivElement { Class = "container", Children = { child1, child2, child3 } };
        var root = new DivElement { Children = { container } };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];
        var child1Box = containerBox.Children[0];
        var child2Box = containerBox.Children[1];
        var child3Box = containerBox.Children[2];

        float containerLeft = containerBox.BoxModel.Content.Left;
        float containerRight = containerBox.BoxModel.Content.Right;

        // child1 在最左
        child1Box.BoxModel.MarginBox.Left.ShouldBe(containerLeft, 0.5f);

        // child3 在最右，不超出容器
        child3Box.BoxModel.MarginBox.Right.ShouldBeLessThanOrEqualTo(containerRight);
        child3Box.BoxModel.MarginBox.Right.ShouldBe(containerRight, 0.5f);

        // child2 在中间，间距相等
        float space1 = child2Box.BoxModel.MarginBox.Left - child1Box.BoxModel.MarginBox.Right;
        float space2 = child3Box.BoxModel.MarginBox.Left - child2Box.BoxModel.MarginBox.Right;
        space1.ShouldBe(space2, 1f, "Spacing should be equal between items");
    }
}
