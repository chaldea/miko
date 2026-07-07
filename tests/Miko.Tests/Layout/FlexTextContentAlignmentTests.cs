using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-085 回归测试：
/// flex 容器的直接文本内容（无包裹元素）应作为匿名 flex 项参与
/// justify-content（主轴）与 align-items（交叉轴）对齐。
/// 之前直接文本不是独立 LayoutBox，渲染阶段恒定锚定在内容盒左上角，
/// 导致 JustifyContent / AlignItems 对其不起作用。
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

    [Fact]
    public void CenterCenter_ShouldCenterTextOnBothAxes()
    {
        var box = LayoutContainer(JustifyContent.Center, AlignItems.Center);

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth("text", box.ComputedStyle.FontFamily, 16f, box.ComputedStyle.FontWeight);
        float lineHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(box.ComputedStyle);

        // 主轴（水平）居中
        box.TextContentOffsetX.ShouldBe((300f - textWidth) / 2f, 0.5f);
        // 交叉轴（垂直）居中
        box.TextContentOffsetY.ShouldBe((300f - lineHeight) / 2f, 0.5f);
    }

    [Fact]
    public void FlexStart_ShouldLeaveTextAtTopLeft()
    {
        var box = LayoutContainer(JustifyContent.FlexStart, AlignItems.FlexStart);

        box.TextContentOffsetX.ShouldBe(0f, 0.01f);
        box.TextContentOffsetY.ShouldBe(0f, 0.01f);
    }

    [Fact]
    public void FlexEnd_ShouldPushTextToBottomRight()
    {
        var box = LayoutContainer(JustifyContent.FlexEnd, AlignItems.FlexEnd);

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth("text", box.ComputedStyle.FontFamily, 16f, box.ComputedStyle.FontWeight);
        float lineHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(box.ComputedStyle);

        box.TextContentOffsetX.ShouldBe(300f - textWidth, 0.5f);
        box.TextContentOffsetY.ShouldBe(300f - lineHeight, 0.5f);
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

        // 交叉轴（水平）居中
        box.TextContentOffsetX.ShouldBe((300f - textWidth) / 2f, 0.5f);
        // 主轴（垂直）flex-end
        box.TextContentOffsetY.ShouldBe(300f - lineHeight, 0.5f);
    }
}
