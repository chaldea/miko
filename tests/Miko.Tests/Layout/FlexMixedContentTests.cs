using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-085 问题1 / ISSUE-086：flex 容器同时包含直接文本和子元素时的布局测试。
/// 文本以 TextNode 子盒形式作为普通 flex 项参与，排在 DOM 顺序对应的位置，
/// 并与其它子元素一起参与 justify-content/align-items 对齐。
/// </summary>
public class FlexMixedContentTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static List<StyleSheet> BuildSheets()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".container"] = new()
            {
                Width = Length.Px(300),
                Height = Length.Px(100),
                Display = Display.Flex,
                JustifyContent = JustifyContent.FlexStart,
                AlignItems = AlignItems.Center,
                FontSize = Length.Px(16),
            },
            [".child"] = new()
            {
                Width = Length.Px(50),
                Height = Length.Px(50),
                BackgroundColor = Color.FromRgb(255, 0, 0),
            }
        });
        return new List<StyleSheet> { sheet };
    }

    private static LayoutBox TextBox(LayoutBox container) =>
        container.Children.Single(c => c.Element is TextNode);

    [Fact]
    public void FlexContainer_WithTextAndChild_ShouldLayoutTextBeforeChild()
    {
        // <div class="container">text<div class="child"></div></div>
        var child = new DivElement { Class = "child" };
        var container = new DivElement { Class = "container", TextContent = "text", Children = { child } };
        var root = new DivElement { Children = { container } };

        var layout = _layoutEngine.Layout(root, BuildSheets(), 800, 600);
        var containerBox = layout.Children[0];

        // 子节点顺序：文本节点在前（TextContent setter 插入到索引 0），子元素在后。
        containerBox.Children[0].Element.ShouldBeOfType<TextNode>();
        var textBox = containerBox.Children[0];
        var childBox = containerBox.Children[1];

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth(
            "text", containerBox.ComputedStyle.FontFamily, 16f, containerBox.ComputedStyle.FontWeight);

        // 文本节点从内容盒左缘开始
        textBox.BoxModel.Content.Left.ShouldBe(containerBox.BoxModel.Content.Left, 0.5f);
        // 子元素应该从文本宽度之后开始
        childBox.BoxModel.Content.Left.ShouldBe(
            containerBox.BoxModel.Content.Left + textWidth, 0.5f,
            "Child should start after the text content");
    }

    [Fact]
    public void FlexContainer_WithTextAndChild_BothShouldBeCentered()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".container"] = new()
            {
                Width = Length.Px(300),
                Height = Length.Px(100),
                Display = Display.Flex,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                FontSize = Length.Px(16),
            },
            [".child"] = new()
            {
                Width = Length.Px(50),
                Height = Length.Px(50),
            }
        });

        var child = new DivElement { Class = "child" };
        var container = new DivElement { Class = "container", TextContent = "text", Children = { child } };
        var root = new DivElement { Children = { container } };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];
        var textBox = TextBox(containerBox);
        var childBox = containerBox.Children[1];

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth(
            "text", containerBox.ComputedStyle.FontFamily, 16f, containerBox.ComputedStyle.FontWeight);
        float textHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(containerBox.ComputedStyle);

        float totalWidth = textWidth + childBox.BoxModel.MarginBox.Width;
        float expectedTextOffsetX = (300f - totalWidth) / 2f;
        float expectedTextOffsetY = (100f - textHeight) / 2f;

        // 文本节点应该作为第一个 flex 项居中排布（含子元素整体居中）。
        textBox.BoxModel.Content.Left.ShouldBe(
            containerBox.BoxModel.Content.Left + expectedTextOffsetX, 0.5f,
            "Text should be horizontally centered along with child");
        (textBox.BoxModel.Content.Top - containerBox.BoxModel.Content.Top).ShouldBe(
            expectedTextOffsetY, 0.5f, "Text should be vertically centered");

        // 子元素也应该居中（在文本之后）
        childBox.BoxModel.Content.Left.ShouldBe(
            containerBox.BoxModel.Content.Left + expectedTextOffsetX + textWidth, 0.5f,
            "Child should be centered after the text");
    }
}
