using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-158：<c>:nth-child()</c>、<c>:nth-last-child()</c>、<c>:nth-of-type()</c>、<c>:nth-last-of-type()</c>
/// —— An+B 微语法解析、序号匹配（文本节点不计入）、<c>of S</c>、特异性与级联集成。
/// </summary>
public class NthSelectorTests
{
    private static DivElement ListOf(int count, out List<DivElement> items)
    {
        var list = new DivElement();
        items = new List<DivElement>();
        for (int i = 0; i < count; i++)
        {
            var item = new DivElement();
            list.AddChild(item);
            items.Add(item);
        }
        return list;
    }

    /// <summary>选择器在 1..count 个兄弟中命中的 1 起序号。</summary>
    private static int[] MatchedPositions(string selector, int count)
    {
        ListOf(count, out var items);
        var parsed = CssSelectorParser.Parse(selector);
        return items.Select((item, index) => (item, index)).Where(x => parsed.Matches(x.item))
            .Select(x => x.index + 1).ToArray();
    }

    #region An+B parsing

    [Theory]
    [InlineData("odd", 2, 1)]
    [InlineData("ODD", 2, 1)]
    [InlineData("even", 2, 0)]
    [InlineData("3", 0, 3)]
    [InlineData("+3", 0, 3)]
    [InlineData("-3", 0, -3)]
    [InlineData("n", 1, 0)]
    [InlineData("+n", 1, 0)]
    [InlineData("-n", -1, 0)]
    [InlineData("N", 1, 0)]
    [InlineData("2n", 2, 0)]
    [InlineData("2n+1", 2, 1)]
    [InlineData("2n-1", 2, -1)]
    [InlineData("-n+3", -1, 3)]
    [InlineData("-2n+5", -2, 5)]
    [InlineData("+3n-2", 3, -2)]
    [InlineData("0n+4", 0, 4)]
    [InlineData(" 2n + 1 ", 2, 1)]
    [InlineData("2n+ 1", 2, 1)]
    [InlineData("2n -1", 2, -1)]
    public void Should_ParseAnPlusB(string text, int a, int b)
    {
        CssSelectorParser.ParseAnPlusB(text).ShouldBe((a, b));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("2 n")]
    [InlineData("+ n")]
    [InlineData("n+")]
    [InlineData("2n+-1")]
    [InlineData("2n1")]
    [InlineData("1.5")]
    [InlineData("n+1.5")]
    [InlineData("2nn")]
    public void Should_RejectInvalidAnPlusB(string text)
    {
        Should.Throw<ArgumentException>(() => CssSelectorParser.ParseAnPlusB(text));
    }

    [Fact]
    public void Should_ParseNthChildIntoNthChildSelector()
    {
        var nth = CssSelectorParser.Parse("li:nth-child(2n+1)").ShouldBeOfType<CompoundSelector>()
            .Selectors[1].ShouldBeOfType<NthChildSelector>();
        nth.A.ShouldBe(2);
        nth.B.ShouldBe(1);
        nth.Of.ShouldBeNull();
    }

    [Fact]
    public void Should_ParseEachNthPseudoClass()
    {
        CssSelectorParser.Parse(":nth-child(odd)").ShouldBeOfType<NthChildSelector>();
        CssSelectorParser.Parse(":nth-last-child(odd)").ShouldBeOfType<NthLastChildSelector>();
        CssSelectorParser.Parse(":nth-of-type(odd)").ShouldBeOfType<NthOfTypeSelector>();
        CssSelectorParser.Parse(":nth-last-of-type(odd)").ShouldBeOfType<NthLastOfTypeSelector>();
    }

    [Fact]
    public void Should_ParseOfSelectorList()
    {
        var nth = CssSelectorParser.Parse(":nth-child(odd of .a, .b)").ShouldBeOfType<NthChildSelector>();
        (nth.A, nth.B).ShouldBe((2, 1));
        nth.Of.ShouldBeOfType<GroupSelector>().Selectors.Count.ShouldBe(2);

        CssSelectorParser.Parse(":nth-last-child(-n+2 OF li.x)").ShouldBeOfType<NthLastChildSelector>()
            .Of.ShouldBeOfType<CompoundSelector>();
    }

    [Fact]
    public void Should_RejectOfOnTypeVariants()
    {
        Should.Throw<ArgumentException>(() => CssSelectorParser.Parse(":nth-of-type(2n of .a)"));
        Should.Throw<ArgumentException>(() => CssSelectorParser.Parse(":nth-last-of-type(1 of .a)"));
    }

    [Fact]
    public void Should_RejectMissingOrUnclosedArgument()
    {
        Should.Throw<ArgumentException>(() => CssSelectorParser.Parse(":nth-child()"));
        Should.Throw<ArgumentException>(() => CssSelectorParser.Parse(":nth-child(2n+1"));
        // 不带括号的写法不是已知的伪类。
        Should.Throw<ArgumentException>(() => CssSelectorParser.Parse(":nth-child"));
    }

    #endregion

    #region Matching

    [Theory]
    [InlineData(":nth-child(odd)", new[] { 1, 3, 5, 7 })]
    [InlineData(":nth-child(even)", new[] { 2, 4, 6 })]
    [InlineData(":nth-child(3)", new[] { 3 })]
    [InlineData(":nth-child(0)", new int[0])]
    [InlineData(":nth-child(n)", new[] { 1, 2, 3, 4, 5, 6, 7 })]
    [InlineData(":nth-child(n+5)", new[] { 5, 6, 7 })]
    [InlineData(":nth-child(-n+3)", new[] { 1, 2, 3 })]
    [InlineData(":nth-child(3n)", new[] { 3, 6 })]
    [InlineData(":nth-child(3n-1)", new[] { 2, 5 })]
    [InlineData(":nth-child(-2n+5)", new[] { 1, 3, 5 })]
    [InlineData(":nth-child(-n)", new int[0])]
    [InlineData(":nth-last-child(1)", new[] { 7 })]
    [InlineData(":nth-last-child(-n+2)", new[] { 6, 7 })]
    [InlineData(":nth-last-child(odd)", new[] { 1, 3, 5, 7 })]
    [InlineData(":nth-last-child(2n)", new[] { 2, 4, 6 })]
    public void Should_MatchPositionsAmongSiblings(string selector, int[] expected)
    {
        MatchedPositions(selector, 7).ShouldBe(expected);
    }

    [Fact]
    public void Should_MatchFirstNthChildLikeFirstChild()
    {
        ListOf(4, out var items);
        var nth = new NthChildSelector(0, 1);
        var nthLast = new NthLastChildSelector(0, 1);
        foreach (var item in items)
        {
            nth.Matches(item).ShouldBe(new FirstChildSelector().Matches(item));
            nthLast.Matches(item).ShouldBe(new LastChildSelector().Matches(item));
        }
    }

    [Fact]
    public void Should_NotCountTextNodes()
    {
        // <ul>text<li/>text<li/>text<li/></ul>：文本节点不计入子序号（ISSUE-086）。
        var list = new UlElement();
        var items = new List<LiElement>();
        for (int i = 0; i < 3; i++)
        {
            list.AddChild(new TextNode("gap"));
            var li = new LiElement();
            list.AddChild(li);
            items.Add(li);
        }

        var second = CssSelectorParser.Parse("li:nth-child(2)");
        items.Select(second.Matches).ShouldBe([false, true, false]);
        var lastButOne = CssSelectorParser.Parse("li:nth-last-child(2)");
        items.Select(lastButOne.Matches).ShouldBe([false, true, false]);

        // 文本节点本身永不匹配。
        CssSelectorParser.Parse(":nth-child(n)").Matches(list.Children[0]).ShouldBeFalse();
    }

    [Fact]
    public void Should_NotMatchElementWithoutParent()
    {
        var orphan = new DivElement();
        new NthChildSelector(1, 0).Matches(orphan).ShouldBeFalse();
        new NthLastOfTypeSelector(1, 0).Matches(orphan).ShouldBeFalse();
    }

    [Fact]
    public void Should_CountOnlySameTagForOfType()
    {
        // span div span div span
        var parent = new DivElement();
        var spans = new List<SpanElement>();
        var divs = new List<DivElement>();
        for (int i = 0; i < 3; i++)
        {
            var span = new SpanElement();
            parent.AddChild(span);
            spans.Add(span);
            if (i < 2)
            {
                var div = new DivElement();
                parent.AddChild(div);
                divs.Add(div);
            }
        }

        var evenOfType = CssSelectorParser.Parse(":nth-of-type(even)");
        spans.Select(evenOfType.Matches).ShouldBe([false, true, false]);
        divs.Select(evenOfType.Matches).ShouldBe([false, true]);

        var firstFromEnd = CssSelectorParser.Parse(":nth-last-of-type(1)");
        spans.Select(firstFromEnd.Matches).ShouldBe([false, false, true]);
        divs.Select(firstFromEnd.Matches).ShouldBe([false, true]);

        // 对照 nth-child：第 2 个子元素是 div。
        var secondChild = CssSelectorParser.Parse(":nth-child(2)");
        secondChild.Matches(divs[0]).ShouldBeTrue();
        secondChild.Matches(spans[1]).ShouldBeFalse();
    }

    [Fact]
    public void Should_CountOnlyMatchingSiblingsWithOf()
    {
        // a b a a b a：:nth-child(2 of .a) 是第二个 .a（第 3 个子元素），不是排在第二位的 .a。
        var parent = new DivElement();
        var classes = new[] { "a", "b", "a", "a", "b", "a" };
        var children = classes.Select(c => new DivElement { Class = c }).ToList();
        foreach (var child in children) parent.AddChild(child);

        var secondA = CssSelectorParser.Parse(":nth-child(2 of .a)");
        children.Select(secondA.Matches).ShouldBe([false, false, true, false, false, false]);

        var lastA = CssSelectorParser.Parse(":nth-last-child(1 of .a)");
        children.Select(lastA.Matches).ShouldBe([false, false, false, false, false, true]);

        var oddA = CssSelectorParser.Parse(":nth-child(odd of .a)");
        children.Select(oddA.Matches).ShouldBe([true, false, false, true, false, false]);

        // 元素自身不匹配 S 时永不匹配，哪怕它的序号符合。
        CssSelectorParser.Parse(":nth-child(2 of .b)").Matches(children[1]).ShouldBeFalse();
        CssSelectorParser.Parse(":nth-child(2 of .b)").Matches(children[4]).ShouldBeTrue();
    }

    [Fact]
    public void Should_SupportComplexSelectorsInOf()
    {
        var shell = new DivElement { Class = "shell" };
        var plain = new DivElement();
        var a1 = new DivElement { Class = "a" };
        var a2 = new DivElement { Class = "a" };
        shell.AddChild(a1);
        shell.AddChild(a2);
        var outsideA1 = new DivElement { Class = "a" };
        var outsideA2 = new DivElement { Class = "a" };
        plain.AddChild(outsideA1);
        plain.AddChild(outsideA2);

        var selector = CssSelectorParser.Parse(":nth-child(2 of .shell > .a)");
        selector.Matches(a2).ShouldBeTrue();
        selector.Matches(outsideA2).ShouldBeFalse();
    }

    [Theory]
    [InlineData(2, 1, 1, true)]
    [InlineData(2, 1, 2, false)]
    [InlineData(-1, 3, 3, true)]
    [InlineData(-1, 3, 4, false)]
    [InlineData(0, 0, 1, false)]
    [InlineData(3, -2, 1, true)]
    [InlineData(1, int.MinValue, 1, true)]
    [InlineData(0, int.MinValue, 1, false)]
    public void Should_EvaluateAnPlusBForPosition(int a, int b, int position, bool expected)
    {
        NthSelector.IsMatch(a, b, position).ShouldBe(expected);
    }

    #endregion

    #region Specificity, cascade and engine contracts

    [Fact]
    public void Should_HavePseudoClassSpecificity()
    {
        CssSelectorParser.Parse(":nth-child(odd)").Specificity.ShouldBe(10);
        CssSelectorParser.Parse(":nth-last-of-type(2)").Specificity.ShouldBe(10);
        CssSelectorParser.Parse("li:nth-child(2n)").Specificity.ShouldBe(11);
    }

    [Fact]
    public void Should_AddMostSpecificOfArgumentToSpecificity()
    {
        // 10（伪类）+ max(#x = 100, .a = 10)
        CssSelectorParser.Parse(":nth-child(2 of #x, .a)").Specificity.ShouldBe(110);
        CssSelectorParser.Parse(":nth-last-child(1 of li.a)").Specificity.ShouldBe(21);
    }

    [Fact]
    public void Should_NotReadContentOrInlineStyle_UnlessOfDoes()
    {
        CssSelectorParser.Parse(":nth-child(odd)").MayReadContentOrInlineStyle.ShouldBeFalse();
        CssSelectorParser.Parse(":nth-of-type(odd)").MayReadContentOrInlineStyle.ShouldBeFalse();
        CssSelectorParser.Parse(":nth-child(odd of .a)").MayReadContentOrInlineStyle.ShouldBeFalse();
        CssSelectorParser.Parse(":nth-child(odd of [text])").MayReadContentOrInlineStyle.ShouldBeTrue();
    }

    [Fact]
    public void Should_StripeListThroughCssObjectCascade()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".list > .row"] = new() { BackgroundColor = Color.White },
            [".list > .row:nth-child(even)"] = new() { BackgroundColor = Color.FromRgb(240, 240, 240) },
            [".list > .row:nth-last-child(1)"] = new() { Opacity = 0.5f },
        });
        var list = new DivElement { Class = "list" };
        var rows = Enumerable.Range(0, 4).Select(_ => new DivElement { Class = "row" }).ToList();
        foreach (var row in rows) list.AddChild(row);

        new LayoutEngine().Layout(list, [sheet], 400, 400);

        rows.Select(r => r.LayoutBox!.ComputedStyle.BackgroundColor).ShouldBe(
            [Color.White, Color.FromRgb(240, 240, 240), Color.White, Color.FromRgb(240, 240, 240)]);
        rows.Select(r => r.LayoutBox!.ComputedStyle.Opacity).ShouldBe([1f, 1f, 1f, 0.5f]);
    }

    [Fact]
    public void Should_RematchAfterSiblingIsInserted()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div:nth-child(odd)"] = new() { Color = Color.Red },
        });
        var list = ListOf(3, out var items);
        var engine = new LayoutEngine();

        engine.Layout(list, [sheet], 400, 400);
        items[1].LayoutBox!.ComputedStyle.Color.ShouldNotBe(Color.Red);

        list.Children.Insert(0, new DivElement());
        engine.Layout(list, [sheet], 400, 400);
        items[1].LayoutBox!.ComputedStyle.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Should_BucketCompoundRuleByItsClass()
    {
        // RuleIndex 只按 Id/Class/Tag 分桶；:nth-child 不是关键选择器，复合里的 .row 是。
        var sheet = new StyleSheet();
        sheet.AddRule(CssSelectorParser.Parse(".row:nth-child(odd)"), new Style { Color = Color.Red });
        var list = ListOf(0, out _);
        var row = new DivElement { Class = "row" };
        var other = new DivElement();
        list.AddChild(row);
        list.AddChild(other);

        var candidates = new List<RuleIndex.Entry>();
        sheet.RuleIndex.CollectCandidates(row, candidates);
        candidates.Count.ShouldBe(1);
        candidates.Clear();
        sheet.RuleIndex.CollectCandidates(other, candidates);
        candidates.ShouldBeEmpty();
    }

    #endregion
}
