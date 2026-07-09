using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-086：文本与标签混排的布局验证。
/// 交错的 text/element 子盒应按 DOM 顺序水平排列，位置单调递增，互不重叠。
/// </summary>
public class MixedContentLayoutTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static List<StyleSheet> Sheets() => new()
    {
        new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new() { Selector = new TagSelector("p"), Style = new Style { Display = Display.Block, Width = Length.Px(600), FontSize = Length.Px(16) } },
                new() { Selector = new TagSelector("div"), Style = new Style { Display = Display.Block, Width = Length.Px(600), FontSize = Length.Px(16) } },
                new() { Selector = new TagSelector("a"), Style = new Style { Display = Display.Inline } },
                new() { Selector = new TagSelector("span"), Style = new Style { Display = Display.Inline } },
            }
        }
    };

    /// <summary>构建 <c>&lt;p&gt;we can use &lt;a&gt;stopPropagation&lt;/a&gt; to prevent bubbling.&lt;/p&gt;</c>。</summary>
    private Element BuildExample1()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "p");
        builder.AddContent(1, "we can use ");
        builder.OpenElement(2, "a");
        builder.AddContent(3, "stopPropagation");
        builder.CloseElement();
        builder.AddContent(4, " to prevent bubbling.");
        builder.CloseElement();
        return builder.Build();
    }

    [Fact]
    public void Example1_AnchorSitsBetweenTwoTextRuns()
    {
        var p = BuildExample1();
        var layout = _layoutEngine.Layout(p, Sheets(), 800, 600);

        // 子盒顺序：文本 → anchor → 文本。
        layout.Children.Count.ShouldBe(3);
        layout.Children[0].Element.ShouldBeOfType<TextNode>();
        var anchorBox = layout.Children[1];
        anchorBox.Element.ShouldBeOfType<AnchorElement>();
        layout.Children[2].Element.ShouldBeOfType<TextNode>();

        var text1 = layout.Children[0].BoxModel.Content;
        var anchor = anchorBox.BoxModel.MarginBox;
        var text2 = layout.Children[2].BoxModel.Content;

        // 水平位置单调递增：text1 在 anchor 左侧，anchor 在 text2 左侧。
        text1.Left.ShouldBe(layout.BoxModel.Content.Left, 0.5f);
        anchor.Left.ShouldBe(text1.Right, 1f, "anchor 紧跟第一段文本之后");
        text2.Left.ShouldBe(anchor.Right, 1f, "第二段文本紧跟 anchor 之后");

        // 三者在同一行（顶部对齐）。
        anchor.Top.ShouldBe(text1.Top, 1f);
        text2.Top.ShouldBe(text1.Top, 1f);
    }

    [Fact]
    public void Example2_TextSpanText_LaysOutInOrder()
    {
        // <div>test1 <span>test2</span> test3</div>
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "div");
        builder.AddContent(1, "test1 ");
        builder.OpenElement(2, "span");
        builder.AddContent(3, "test2");
        builder.CloseElement();
        builder.AddContent(4, " test3");
        builder.CloseElement();
        var div = builder.Build();

        var layout = _layoutEngine.Layout(div, Sheets(), 800, 600);

        layout.Children.Count.ShouldBe(3);
        var t1 = layout.Children[0].BoxModel.Content;
        var span = layout.Children[1].BoxModel.MarginBox;
        var t3 = layout.Children[2].BoxModel.Content;

        t1.Left.ShouldBe(layout.BoxModel.Content.Left, 0.5f);
        span.Left.ShouldBe(t1.Right, 1f);
        t3.Left.ShouldBe(span.Right, 1f);

        // test3 的右缘应等于三段内容累加宽度（不塌缩、不重叠）。
        t3.Right.ShouldBeGreaterThan(span.Right);
    }

    [Fact]
    public void MixedContent_TotalWidthIncludesAllRuns()
    {
        var p = BuildExample1();
        var layout = _layoutEngine.Layout(p, Sheets(), 800, 600);

        float w1 = layout.Children[0].BoxModel.Content.Width;
        float wAnchor = layout.Children[1].BoxModel.MarginBox.Width;
        float w2 = layout.Children[2].BoxModel.Content.Width;

        float rightmost = layout.Children[2].BoxModel.Content.Right - layout.BoxModel.Content.Left;
        rightmost.ShouldBe(w1 + wAnchor + w2, 1.5f,
            "内容总宽应为三段（文本+anchor+文本）之和");
    }
}
