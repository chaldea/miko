using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-085 / ISSUE-086 回归测试：
/// flex 容器的直接文本内容（无包裹元素）应作为一等文本节点（TextNode）参与
/// justify-content（主轴）与 align-items（交叉轴）对齐。
///
/// ISSUE-086 后，文本以有序 TextNode 子盒形式存在，直接被 flex 布局定位（不再通过
/// 已废弃的 LayoutBox.TextContentOffsetX/Y 偏移）。因此断言改为检查 TextNode 子盒的
/// 内容盒相对容器内容原点的位置。
/// </summary>
public class FlexTextContentAlignmentTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static List<StyleSheet> BuildSheets(JustifyContent justify, AlignItems align)
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".container"] = new()
            {
                Width = Length.Px(300),
                Height = Length.Px(300),
                Display = Display.Flex,
                JustifyContent = justify,
                AlignItems = align,
                FontSize = Length.Px(16),
            }
        });
        return new List<StyleSheet> { sheet };
    }

    private LayoutBox LayoutContainer(JustifyContent justify, AlignItems align)
    {
        var container = new DivElement { Class = "container", TextContent = "text" };
        var root = new DivElement { Children = { container } };
        var layout = _layoutEngine.Layout(root, BuildSheets(justify, align), 800, 600);
        return layout.Children[0];
    }

    /// <summary>容器内唯一的文本节点子盒。</summary>
    private static LayoutBox TextBox(LayoutBox container) =>
        container.Children.Single(c => c.Element is TextNode);

    /// <summary>文本节点相对容器内容原点的偏移。</summary>
    private static (float dx, float dy) TextOffset(LayoutBox container)
    {
        var text = TextBox(container).BoxModel.Content;
        var content = container.BoxModel.Content;
        return (text.X - content.X, text.Y - content.Y);
    }

    [Fact]
    public void CenterCenter_ShouldCenterTextOnBothAxes()
    {
        var box = LayoutContainer(JustifyContent.Center, AlignItems.Center);

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth("text", box.ComputedStyle.FontFamily, 16f, box.ComputedStyle.FontWeight);
        float lineHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(box.ComputedStyle);

        var (dx, dy) = TextOffset(box);
        // 主轴（水平）居中
        dx.ShouldBe((300f - textWidth) / 2f, 0.5f);
        // 交叉轴（垂直）居中
        dy.ShouldBe((300f - lineHeight) / 2f, 0.5f);
    }

    [Fact]
    public void FlexStart_ShouldLeaveTextAtTopLeft()
    {
        var box = LayoutContainer(JustifyContent.FlexStart, AlignItems.FlexStart);

        var (dx, dy) = TextOffset(box);
        dx.ShouldBe(0f, 0.01f);
        dy.ShouldBe(0f, 0.01f);
    }

    [Fact]
    public void FlexEnd_ShouldPushTextToBottomRight()
    {
        var box = LayoutContainer(JustifyContent.FlexEnd, AlignItems.FlexEnd);

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth("text", box.ComputedStyle.FontFamily, 16f, box.ComputedStyle.FontWeight);
        float lineHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(box.ComputedStyle);

        var (dx, dy) = TextOffset(box);
        dx.ShouldBe(300f - textWidth, 0.5f);
        dy.ShouldBe(300f - lineHeight, 0.5f);
    }

    [Fact]
    public void ColumnDirection_ShouldSwapAxes()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".container"] = new()
            {
                Width = Length.Px(300),
                Height = Length.Px(300),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.FlexEnd, // 主轴=垂直
                AlignItems = AlignItems.Center,           // 交叉轴=水平
                FontSize = Length.Px(16),
            }
        });
        var container = new DivElement { Class = "container", TextContent = "text" };
        var root = new DivElement { Children = { container } };
        var box = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600).Children[0];

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth("text", box.ComputedStyle.FontFamily, 16f, box.ComputedStyle.FontWeight);
        float lineHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(box.ComputedStyle);

        var (dx, dy) = TextOffset(box);
        // 交叉轴（水平）居中
        dx.ShouldBe((300f - textWidth) / 2f, 0.5f);
        // 主轴（垂直）flex-end
        dy.ShouldBe(300f - lineHeight, 0.5f);
    }
}
