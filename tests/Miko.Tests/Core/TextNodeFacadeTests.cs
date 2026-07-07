using Miko.Core;
using Miko.Core.DomElements;
using Shouldly;

namespace Miko.Tests.Core;

/// <summary>
/// ISSUE-086：<see cref="Element.TextContent"/> 作为便利外观（facade）的行为契约。
///
/// 文本以有序 <see cref="TextNode"/> 子节点承载。TextContent 的 get 拼接所有直接文本子节点，
/// set 移除已有文本节点并（若非空）重建单个前置文本节点。此契约保护既有大量按字符串读写
/// TextContent 的代码与测试。
/// </summary>
public class TextNodeFacadeTests
{
    [Fact]
    public void Setter_CreatesSingleLeadingTextNode()
    {
        var div = new DivElement { TextContent = "hello" };

        div.Children.Count.ShouldBe(1);
        div.Children[0].ShouldBeOfType<TextNode>().Text.ShouldBe("hello");
    }

    [Fact]
    public void Setter_InsertsTextBeforeExistingChildren()
    {
        var div = new DivElement();
        div.AddChild(new SpanElement());
        div.TextContent = "lead";

        // 文本节点插入到索引 0，保持旧「文本在前」语义。
        div.Children.Count.ShouldBe(2);
        div.Children[0].ShouldBeOfType<TextNode>().Text.ShouldBe("lead");
        div.Children[1].ShouldBeOfType<SpanElement>();
    }

    [Fact]
    public void Getter_ReturnsNull_WhenNoTextNodes()
    {
        var div = new DivElement();
        div.AddChild(new SpanElement());

        div.TextContent.ShouldBeNull();
    }

    [Fact]
    public void Getter_ConcatenatesDirectTextNodesOnly()
    {
        var div = new DivElement();
        div.AddChild(new TextNode("a"));
        var span = new SpanElement();
        span.AddChild(new TextNode("INNER"));
        div.AddChild(span);
        div.AddChild(new TextNode("b"));

        // 只拼接直接文本子节点，不含子元素内部文本。
        div.TextContent.ShouldBe("ab");
    }

    [Fact]
    public void Setter_Null_RemovesTextNodes_KeepsElementChildren()
    {
        var div = new DivElement { TextContent = "x" };
        div.AddChild(new SpanElement());

        div.TextContent = null;

        div.Children.Count.ShouldBe(1);
        div.Children[0].ShouldBeOfType<SpanElement>();
        div.TextContent.ShouldBeNull();
    }

    [Fact]
    public void Setter_Overwrite_ReplacesPreviousText()
    {
        var div = new DivElement { TextContent = "first" };
        div.TextContent = "second";

        div.Children.Count.ShouldBe(1);
        div.Children[0].ShouldBeOfType<TextNode>().Text.ShouldBe("second");
        div.TextContent.ShouldBe("second");
    }

    [Fact]
    public void TextNode_TextContent_MapsToOwnText()
    {
        var node = new TextNode("abc");
        node.TextContent.ShouldBe("abc");

        node.TextContent = "xyz";
        node.Text.ShouldBe("xyz");
    }

    [Fact]
    public void Setter_RoundTrip_PreservesValue()
    {
        var div = new DivElement();
        div.TextContent = "round trip";
        div.TextContent.ShouldBe("round trip");
    }
}
