using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Shouldly;

namespace Miko.Tests.Components;

/// <summary>
/// ISSUE-086：文本与标签混排时必须保留精确的交错顺序。
///
/// 历史上 <see cref="Element.TextContent"/> 是单一字符串，与 <see cref="Element.Children"/>
/// 互不感知顺序，导致 <c>text1 &lt;span/&gt; text3</c> 被折叠为 <c>"text1 text3"</c> + span，
/// 交错顺序丢失。现在文本以有序 <see cref="TextNode"/> 子节点承载，交错顺序由子节点列表表达。
///
/// 这些测试直接驱动 <see cref="RenderTreeBuilder"/>（即 Razor 编译器发射的调用序列），
/// 验证真实构建路径下的交错顺序。
/// </summary>
public class MixedContentOrderTests
{
    /// <summary>
    /// 示例1：<c>&lt;p&gt;we can use &lt;a&gt;stopPropagation&lt;/a&gt; to prevent bubbling.&lt;/p&gt;</c>
    /// anchor 必须夹在两段文本之间。
    /// </summary>
    [Fact]
    public void TextBeforeAndAfterInlineChild_PreservesInterleavedOrder()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement<ParagraphElement>();
        builder.AddContent("we can use ");
        builder.OpenElement<AnchorElement>();
        builder.AddContent("stopPropagation");
        builder.CloseElement();
        builder.AddContent(" to prevent bubbling.");
        builder.CloseElement();

        var p = builder.Build().ShouldBeOfType<ParagraphElement>();

        // 三个子节点：文本 → anchor → 文本，顺序精确保留。
        p.Children.Count.ShouldBe(3);
        p.Children[0].ShouldBeOfType<TextNode>().Text.ShouldBe("we can use ");
        var anchor = p.Children[1].ShouldBeOfType<AnchorElement>();
        anchor.Children.Single().ShouldBeOfType<TextNode>().Text.ShouldBe("stopPropagation");
        p.Children[2].ShouldBeOfType<TextNode>().Text.ShouldBe(" to prevent bubbling.");

        // facade 拼接返回两段直接文本（不含子元素内文本），兼容旧读取。
        p.TextContent.ShouldBe("we can use  to prevent bubbling.");
    }

    /// <summary>
    /// 示例2：<c>&lt;div&gt;test1 &lt;span&gt;test2&lt;/span&gt; test3&lt;/div&gt;</c>
    /// </summary>
    [Fact]
    public void TextSpanText_PreservesInterleavedOrder()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement<DivElement>();
        builder.AddContent("test1 ");
        builder.OpenElement<SpanElement>();
        builder.AddContent("test2");
        builder.CloseElement();
        builder.AddContent(" test3");
        builder.CloseElement();

        var div = builder.Build().ShouldBeOfType<DivElement>();

        div.Children.Count.ShouldBe(3);
        div.Children[0].ShouldBeOfType<TextNode>().Text.ShouldBe("test1 ");
        div.Children[1].ShouldBeOfType<SpanElement>()
            .Children.Single().ShouldBeOfType<TextNode>().Text.ShouldBe("test2");
        div.Children[2].ShouldBeOfType<TextNode>().Text.ShouldBe(" test3");
    }

    [Fact]
    public void ConsecutiveTextFragments_MergeIntoSingleTextNode()
    {
        // Razor 把 `Clicked @_count times` 编译为三次 AddContent；相邻纯文本片段应合并为一段。
        var builder = new RenderTreeBuilder();
        builder.OpenElement<ButtonElement>();
        builder.AddContent("Clicked ");
        builder.AddContent(2, 5);
        builder.AddContent(" times");
        builder.CloseElement();

        var button = builder.Build().ShouldBeOfType<ButtonElement>();
        // 无子元素分隔，三段文本合并为单个 TextNode。
        button.Children.Count.ShouldBe(1);
        button.Children[0].ShouldBeOfType<TextNode>().Text.ShouldBe("Clicked 5 times");
        button.TextContent.ShouldBe("Clicked 5 times");
    }

    [Fact]
    public void TextThenChildThenText_ProducesThreeChildrenNotConcatenated()
    {
        // 被子元素分隔的文本不合并：验证交错不会退化为单一 TextContent 串。
        var builder = new RenderTreeBuilder();
        builder.OpenElement<DivElement>();
        builder.AddContent("A");
        builder.OpenElement<SpanElement>();
        builder.CloseElement();
        builder.AddContent("B");
        builder.CloseElement();

        var div = builder.Build();
        div.Children.Count.ShouldBe(3);
        div.Children[0].ShouldBeOfType<TextNode>().Text.ShouldBe("A");
        div.Children[1].ShouldBeOfType<SpanElement>();
        div.Children[2].ShouldBeOfType<TextNode>().Text.ShouldBe("B");
    }
}
