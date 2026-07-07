using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-085 问题2：flex 容器同时设置 justify-content 和 text-align 时的冲突测试。
/// TextAlign 应该在文本偏移后的矩形内居中，而不是相对原始内容盒居中。
/// </summary>
public class FlexTextAlignConflictTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void FlexCenter_WithTextAlignCenter_ShouldCenterTextNotMoveToRight()
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
                TextAlign = TextAlign.Center,
                FontSize = Length.Px(16),
            }
        });

        var container = new DivElement { Class = "container", TextContent = "text" };
        var root = new DivElement { Children = { container } };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth(
            "text", containerBox.ComputedStyle.FontFamily, 16f, containerBox.ComputedStyle.FontWeight);
        float textHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(containerBox.ComputedStyle);

        // justify-content: center 应该将文本在容器中水平居中
        float expectedOffsetX = (300f - textWidth) / 2f;
        float expectedOffsetY = (100f - textHeight) / 2f;

        containerBox.TextContentOffsetX.ShouldBe(expectedOffsetX, 0.5f,
            "TextContentOffsetX should center the text horizontally");
        containerBox.TextContentOffsetY.ShouldBe(expectedOffsetY, 0.5f,
            "TextContentOffsetY should center the text vertically");

        // text-align: center 不应该再次偏移文本（因为文本宽度等于textRect宽度，居中后位置不变）
        // 问题：如果 text-align 相对于原始内容盒(300px)居中，而不是相对于textRect(textWidth)居中，
        // 那么会产生额外的 (300 - textWidth) / 2 偏移，导致文本移到右边。
    }

    [Fact]
    public void FlexStart_WithTextAlignCenter_ShouldCenterTextInItsRect()
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
                AlignItems = AlignItems.FlexStart,
                TextAlign = TextAlign.Center,
                FontSize = Length.Px(16),
            }
        });

        var container = new DivElement { Class = "container", TextContent = "text" };
        var root = new DivElement { Children = { container } };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];

        // justify-content: flex-start 不产生偏移
        containerBox.TextContentOffsetX.ShouldBe(0f, 0.01f);
        containerBox.TextContentOffsetY.ShouldBe(0f, 0.01f);

        // text-align: center 应该在整个容器宽度(300px)内居中文本
        // 这种情况下 text-align 的行为是正确的
    }
}
