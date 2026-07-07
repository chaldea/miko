using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-085 问题2 / ISSUE-086：flex 容器同时设置 justify-content 和 text-align。
///
/// ISSUE-086 后，文本作为 TextNode 子盒由 flex 布局定位；text-align 在渲染阶段以父内容盒为
/// 对齐容器（当文本是唯一在流内容时）。因此布局阶段 justify-content 决定 TextNode 盒的位置，
/// 不再存在 TextContentOffset 与 text-align 的叠加冲突。此处断言 TextNode 盒的 flex 定位正确。
/// </summary>
public class FlexTextAlignConflictTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static LayoutBox TextBox(LayoutBox container) =>
        container.Children.Single(c => c.Element is TextNode);

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
        var textBox = TextBox(containerBox);

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth(
            "text", containerBox.ComputedStyle.FontFamily, 16f, containerBox.ComputedStyle.FontWeight);
        float textHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(containerBox.ComputedStyle);

        // justify-content: center 将 TextNode 盒在容器中水平居中；
        // 文本宽度即 TextNode 盒宽度，渲染阶段 text-align:center 在文本自身宽度内无额外偏移。
        (textBox.BoxModel.Content.Left - containerBox.BoxModel.Content.Left)
            .ShouldBe((300f - textWidth) / 2f, 0.5f, "TextNode box should be centered horizontally by justify-content");
        (textBox.BoxModel.Content.Top - containerBox.BoxModel.Content.Top)
            .ShouldBe((100f - textHeight) / 2f, 0.5f, "TextNode box should be centered vertically by align-items");
    }

    [Fact]
    public void FlexStart_WithTextAlignCenter_ShouldLeaveTextNodeAtStart()
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
        var textBox = TextBox(containerBox);

        // justify-content: flex-start / align-items: flex-start：TextNode 盒锚定内容盒左上角。
        // （text-align:center 的渲染阶段居中以父内容盒 300px 为参照，不影响布局盒位置。）
        (textBox.BoxModel.Content.Left - containerBox.BoxModel.Content.Left).ShouldBe(0f, 0.01f);
        (textBox.BoxModel.Content.Top - containerBox.BoxModel.Content.Top).ShouldBe(0f, 0.01f);
    }
}
