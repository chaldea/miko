using Miko.Core.DomElements;
using Shouldly;

namespace Miko.Tests.Core;

public class ElementTests
{
    [Fact]
    public void AddChild_ShouldAddToChildrenList()
    {
        var parent = new DivElement();
        var child = new SpanElement();

        parent.AddChild(child);

        parent.Children.Count.ShouldBe(1);
        parent.Children[0].ShouldBe(child);
    }

    [Fact]
    public void AddChild_ShouldSetParentReference()
    {
        var parent = new DivElement();
        var child = new SpanElement();

        parent.AddChild(child);

        child.Parent.ShouldBe(parent);
    }

    [Fact]
    public void AddChild_ShouldMarkParentAsDirty()
    {
        var parent = new DivElement();
        var child = new SpanElement();

        parent.AddChild(child);

        parent.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void AddChild_WithExistingParent_ShouldRemoveFromOldParent()
    {
        var parent1 = new DivElement();
        var parent2 = new DivElement();
        var child = new SpanElement();

        parent1.AddChild(child);
        parent2.AddChild(child);

        parent1.Children.Count.ShouldBe(0);
        parent2.Children.Count.ShouldBe(1);
        child.Parent.ShouldBe(parent2);
    }

    [Fact]
    public void RemoveChild_ShouldRemoveFromChildrenList()
    {
        var parent = new DivElement();
        var child = new SpanElement();

        parent.AddChild(child);
        var result = parent.RemoveChild(child);

        result.ShouldBeTrue();
        parent.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void RemoveChild_ShouldClearParentReference()
    {
        var parent = new DivElement();
        var child = new SpanElement();

        parent.AddChild(child);
        parent.RemoveChild(child);

        child.Parent.ShouldBeNull();
    }

    [Fact]
    public void RemoveChild_WithNonExistentChild_ShouldReturnFalse()
    {
        var parent = new DivElement();
        var child = new SpanElement();

        var result = parent.RemoveChild(child);

        result.ShouldBeFalse();
    }

    [Fact]
    public void FindById_ShouldReturnElementWithMatchingId()
    {
        var root = new DivElement { Id = "root" };
        var child = new DivElement { Id = "child" };
        root.AddChild(child);

        var found = root.FindById("child");

        found.ShouldBe(child);
    }

    [Fact]
    public void FindById_ShouldReturnSelfIfIdMatches()
    {
        var element = new DivElement { Id = "target" };

        var found = element.FindById("target");

        found.ShouldBe(element);
    }

    [Fact]
    public void FindById_WithNonExistentId_ShouldReturnNull()
    {
        var root = new DivElement { Id = "root" };

        var found = root.FindById("nonexistent");

        found.ShouldBeNull();
    }

    [Fact]
    public void FindByClass_ShouldReturnAllElementsWithClass()
    {
        var root = new DivElement { Class = "container" };
        var child1 = new DivElement { Class = "item" };
        var child2 = new DivElement { Class = "item active" };
        var child3 = new DivElement { Class = "other" };

        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        var found = root.FindByClass("item");

        found.Count.ShouldBe(2);
        found.ShouldContain(child1);
        found.ShouldContain(child2);
    }

    [Fact]
    public void FindByTagName_ShouldReturnAllElementsWithMatchingTag()
    {
        var root = new DivElement();
        var span1 = new SpanElement();
        var span2 = new SpanElement();
        var div = new DivElement();

        root.AddChild(span1);
        root.AddChild(span2);
        root.AddChild(div);

        var found = root.FindByTagName("span");

        found.Count.ShouldBe(2);
        found.ShouldContain(span1);
        found.ShouldContain(span2);
    }

    [Fact]
    public void FindByTagName_ShouldBeCaseInsensitive()
    {
        var root = new DivElement();
        var child = new DivElement();
        root.AddChild(child);

        var found = root.FindByTagName("DIV");

        found.Count.ShouldBe(2); // root + child
    }

    [Fact]
    public void HasClass_WithMatchingClass_ShouldReturnTrue()
    {
        var element = new DivElement { Class = "container primary active" };

        element.HasClass("primary").ShouldBeTrue();
    }

    [Fact]
    public void HasClass_WithNonMatchingClass_ShouldReturnFalse()
    {
        var element = new DivElement { Class = "container primary" };

        element.HasClass("active").ShouldBeFalse();
    }

    [Fact]
    public void HasClass_WithNullClass_ShouldReturnFalse()
    {
        var element = new DivElement { Class = null };

        element.HasClass("anything").ShouldBeFalse();
    }

    // ISSUE-146：HasClass 改为「整串子串查找 + 两侧分隔符核对」。以下用例钉住它必须与
    // 按空白分词的语义逐一等价——尤其是子串命中但不在词边界上的情形。
    [Theory]
    [InlineData("ion-item", "ion-item", true)]
    [InlineData("ion-item-native", "ion-item", false)]
    [InlineData("my-ion-item", "ion-item", false)]
    [InlineData("ion-item-native ion-item", "ion-item", true)]
    [InlineData("a ion-item-native b", "ion-item", false)]
    [InlineData("ion-itemion-item ion-item", "ion-item", true)]
    [InlineData("  ion-item  ", "ion-item", true)]
    [InlineData("x\tion-item\ny", "ion-item", true)]
    [InlineData("ion-item x", "ion-item", true)]
    [InlineData("abc", "abcd", false)]
    [InlineData("a b c", "b c", false)]
    [InlineData("x a b", "a b", false)]
    [InlineData("", "a", false)]
    [InlineData("a", "", false)]
    public void HasClass_MatchesWhitespaceTokenSemantics(string classList, string token, bool expected)
    {
        var element = new DivElement { Class = classList };

        element.HasClass(token).ShouldBe(expected);
        element.HasClass(token).ShouldBe(TokenizedContains(classList, token));
    }

    [Fact]
    public void HasClass_WholeStringEqualityFastPath_IsPreserved()
    {
        // 整串相等的快速路径早于 ISSUE-146 就存在：class 串与 token 完全相同时恒为 true，
        // 哪怕 token 自身含空白。这是唯一偏离分词语义的地方，改写时不得顺手改变它。
        new DivElement { Class = "a b" }.HasClass("a b").ShouldBeTrue();
    }

    private static bool TokenizedContains(string classList, string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var c in classList)
        {
            if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0) tokens.Add(current.ToString());
                current.Clear();
            }
            else current.Append(c);
        }
        if (current.Length > 0) tokens.Add(current.ToString());
        return tokens.Contains(token);
    }

    [Fact]
    public void TagName_ShouldReturnCorrectValue()
    {
        new DivElement().TagName.ShouldBe("div");
        new SpanElement().TagName.ShouldBe("span");
        new H1Element().TagName.ShouldBe("h1");
        new ButtonElement().TagName.ShouldBe("button");
    }
}
