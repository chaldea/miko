using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-085 问题1：flex 容器同时包含直接文本和子元素时的布局测试。
/// 验证文本作为匿名 flex 项排在子元素之前，且参与 justify-content/align-items 对齐。
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

    [Fact]
    public void FlexContainer_WithTextAndChild_ShouldLayoutTextBeforeChild()
    {
        // <div class="container">text<div class="child"></div></div>
        var child = new DivElement { Class = "child" };
        var container = new DivElement { Class = "container", TextContent = "text", Children = { child } };
        var root = new DivElement { Children = { container } };

        var layout = _layoutEngine.Layout(root, BuildSheets(), 800, 600);
        var containerBox = layout.Children[0];
        var childBox = containerBox.Children[0];

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth(
            "text", containerBox.ComputedStyle.FontFamily, 16f, containerBox.ComputedStyle.FontWeight);

        // 文本在前，子元素应该从文本宽度之后开始
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
        var childBox = containerBox.Children[0];

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth(
            "text", containerBox.ComputedStyle.FontFamily, 16f, containerBox.ComputedStyle.FontWeight);
        float textHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(containerBox.ComputedStyle);

        float totalWidth = textWidth + childBox.BoxModel.MarginBox.Width;
        float expectedTextOffsetX = (300f - totalWidth) / 2f;
        float expectedTextOffsetY = (100f - textHeight) / 2f;

        // 文本应该居中对齐
        containerBox.TextContentOffsetX.ShouldBe(expectedTextOffsetX, 0.5f,
            "Text should be horizontally centered along with child");
        containerBox.TextContentOffsetY.ShouldBe(expectedTextOffsetY, 0.5f,
            "Text should be vertically centered");

        // 子元素也应该居中（在文本之后）
        childBox.BoxModel.Content.Left.ShouldBe(
            containerBox.BoxModel.Content.Left + expectedTextOffsetX + textWidth, 0.5f,
            "Child should be centered after the text");
    }
}
