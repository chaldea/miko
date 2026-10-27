using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-158：<c>:is()</c>、<c>:where()</c>、<c>:has()</c>，以及接受选择器列表的 <c>:not()</c>
/// —— 解析、匹配（含组合器参数）、特异性，以及与规则索引 / 祖先过滤器 / 内容记账的契约。
/// </summary>
public class LogicalCombinationSelectorTests
{
    private static Selector P(string selector) => CssSelectorParser.Parse(selector);

    private static StyleSheet SheetOf(params (string selector, Style style)[] rules)
    {
        var sheet = new StyleSheet();
        foreach (var (selector, style) in rules)
            sheet.AddRule(CssSelectorParser.Parse(selector), style);
        return sheet;
    }

    #region :is() / :where()

    [Fact]
    public void Should_ParseIsAndWhere()
    {
        P(":is(.a)").ShouldBeOfType<IsSelector>().Inner.ShouldBeOfType<ClassSelector>();
        P(":is(.a, #b)").ShouldBeOfType<IsSelector>().Inner.ShouldBeOfType<GroupSelector>();
        P(":where(.a .b)").ShouldBeOfType<WhereSelector>().Inner.ShouldBeOfType<DescendantSelector>();
        P("li:is(.a, .b)").ShouldBeOfType<CompoundSelector>().Selectors[1].ShouldBeOfType<IsSelector>();
    }

    [Fact]
    public void Should_MatchAnyArgument()
    {
        var selector = P(":is(h1, .title, #main)");
        selector.Matches(new H1Element()).ShouldBeTrue();
        selector.Matches(new DivElement { Class = "title" }).ShouldBeTrue();
        selector.Matches(new DivElement { Id = "main" }).ShouldBeTrue();
        selector.Matches(new DivElement()).ShouldBeFalse();

        P(":where(h1, .title)").Matches(new DivElement { Class = "title" }).ShouldBeTrue();
        P(":where(h1, .title)").Matches(new SpanElement()).ShouldBeFalse();
    }

    [Fact]
    public void Should_EvaluateComplexArgumentsAgainstTheSubject()
    {
        // :is(.toolbar .title, h1)：组合器参数以本元素为主体求值。
        var title = new SpanElement { Class = "title" };
        _ = new DivElement { Class = "toolbar", Children = { new DivElement { Children = { title } } } };
        var looseTitle = new SpanElement { Class = "title" };
        _ = new DivElement { Class = "card", Children = { looseTitle } };

        var selector = P(":is(.toolbar .title, h1)");
        selector.Matches(title).ShouldBeTrue();
        selector.Matches(looseTitle).ShouldBeFalse();
    }

    [Fact]
    public void Should_CombineIsWithOuterCombinators()
    {
        // :is(.a, .b) > .c
        var underA = new DivElement { Class = "c" };
        var underB = new DivElement { Class = "c" };
        var underX = new DivElement { Class = "c" };
        _ = new DivElement { Class = "a", Children = { underA } };
        _ = new DivElement { Class = "b", Children = { underB } };
        _ = new DivElement { Class = "x", Children = { underX } };

        var selector = P(":is(.a, .b) > .c");
        selector.ShouldBeOfType<ChildSelector>();
        selector.Matches(underA).ShouldBeTrue();
        selector.Matches(underB).ShouldBeTrue();
        selector.Matches(underX).ShouldBeFalse();
    }

    [Fact]
    public void Should_UseMostSpecificArgumentForIs_AndZeroForWhere()
    {
        P(":is(.a, #b)").Specificity.ShouldBe(100);
        P(":is(div, .a)").Specificity.ShouldBe(10);
        P(".x:is(.a .b)").Specificity.ShouldBe(30);
        P(":where(#b, .a)").Specificity.ShouldBe(0);
        P(":where(#b) .title").Specificity.ShouldBe(10);
    }

    [Fact]
    public void Should_LetWhereRulesLoseToAnyLaterOrMoreSpecificRule()
    {
        // :where(#card) .title 的特异性只算 .title（10），因此输给后写的同特异性规则，也输给 span.title。
        var title = new SpanElement { Class = "title" };
        var root = new DivElement { Id = "card", Children = { title } };
        var sheet = SheetOf(
            (":where(#card) .title", new Style { Color = Color.Red, BackgroundColor = Color.Red }),
            (".title", new Style { Color = Color.Blue }),
            ("#card .title", new Style { Opacity = 0.5f }));

        new LayoutEngine().Layout(root, [sheet], 400, 400);

        title.LayoutBox!.ComputedStyle.Color.ShouldBe(Color.Blue);
        title.LayoutBox.ComputedStyle.BackgroundColor.ShouldBe(Color.Red);   // 无人竞争的属性照常生效
        title.LayoutBox.ComputedStyle.Opacity.ShouldBe(0.5f);
    }

    [Fact]
    public void Should_LetIsRulesCompeteWithTheirMostSpecificArgument()
    {
        // :is(#card, .x) .title 的特异性按 #card 计（110），胜过后写的 .card .title（20）。
        var title = new SpanElement { Class = "title" };
        var root = new DivElement { Id = "card", Class = "card", Children = { title } };
        var sheet = SheetOf(
            (":is(#card, .x) .title", new Style { Color = Color.Red }),
            (".card .title", new Style { Color = Color.Blue }));

        new LayoutEngine().Layout(root, [sheet], 400, 400);

        title.LayoutBox!.ComputedStyle.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Should_NotUseIsArgumentsAsAncestorFilterKeys()
    {
        // :is(.a, .b) .t 的祖先只需满足其中一支：把 .a 或 .b 当作「必需祖先键」都会错误否决。
        var t = new DivElement { Class = "t" };
        var root = new DivElement { Class = "b", Children = { new DivElement { Children = { t } } } };
        var sheet = SheetOf((":is(.a, .b) .t", new Style { Color = Color.Red }));

        new LayoutEngine().Layout(root, [sheet], 400, 400);

        t.LayoutBox!.ComputedStyle.Color.ShouldBe(Color.Red);
    }

    #endregion

    #region :not() with selector lists

    [Fact]
    public void Should_AcceptSelectorListAndComplexSelectorsInNot()
    {
        var notAOrB = P(":not(.a, .b)").ShouldBeOfType<NotSelector>();
        notAOrB.Inner.ShouldBeOfType<GroupSelector>();
        notAOrB.Matches(new DivElement { Class = "a" }).ShouldBeFalse();
        notAOrB.Matches(new DivElement { Class = "b" }).ShouldBeFalse();
        notAOrB.Matches(new DivElement { Class = "c" }).ShouldBeTrue();
        notAOrB.Specificity.ShouldBe(10);

        // :not(.card .title) 曾让解析器在空格上死循环；现在按复杂选择器求值。
        var inCard = new DivElement { Class = "title" };
        _ = new DivElement { Class = "card", Children = { inCard } };
        var loose = new DivElement { Class = "title" };
        var notInCard = P(".title:not(.card .title)");
        notInCard.Matches(inCard).ShouldBeFalse();
        notInCard.Matches(loose).ShouldBeTrue();
    }

    #endregion

    #region :has()

    [Fact]
    public void Should_ParseRelativeSelectors()
    {
        var has = P(":has(> img, + .a .b, ~ p, span)").ShouldBeOfType<HasSelector>();
        has.Arguments.Count.ShouldBe(4);
        has.Arguments[0].Steps.Select(s => s.Combinator).ShouldBe([SelectorCombinator.Child]);
        has.Arguments[1].Steps.Select(s => s.Combinator)
            .ShouldBe([SelectorCombinator.NextSibling, SelectorCombinator.Descendant]);
        has.Arguments[2].Steps.Select(s => s.Combinator).ShouldBe([SelectorCombinator.SubsequentSibling]);
        has.Arguments[3].Steps.Select(s => s.Combinator).ShouldBe([SelectorCombinator.Descendant]);
        has.Arguments[3].Steps[0].Compound.ShouldBeOfType<TagSelector>();
    }

    [Fact]
    public void Should_MatchDescendants()
    {
        var withImage = new DivElement { Class = "card", Children = { new DivElement { Children = { new ImageElement() } } } };
        var withoutImage = new DivElement { Class = "card", Children = { new SpanElement() } };

        var selector = P(".card:has(img)");
        selector.Matches(withImage).ShouldBeTrue();
        selector.Matches(withoutImage).ShouldBeFalse();
        // 元素自身不算后代。
        P(":has(img)").Matches(new ImageElement()).ShouldBeFalse();
    }

    [Fact]
    public void Should_MatchOnlyDirectChildrenWithChildCombinator()
    {
        var direct = new DivElement { Children = { new ImageElement() } };
        var nested = new DivElement { Children = { new DivElement { Children = { new ImageElement() } } } };

        var selector = P(":has(> img)");
        selector.Matches(direct).ShouldBeTrue();
        selector.Matches(nested).ShouldBeFalse();
    }

    [Fact]
    public void Should_MatchFollowingSiblings()
    {
        // label + input 结构：label:has(+ input) / h2:has(~ p)
        var label = new LabelElement();
        var input = new InputElement();
        var heading = new H2Element();
        var span = new SpanElement();
        var paragraph = new ParagraphElement();
        _ = new DivElement { Children = { label, input, heading, span, paragraph } };

        P(":has(+ input)").Matches(label).ShouldBeTrue();
        P(":has(+ input)").Matches(heading).ShouldBeFalse();
        P(":has(+ p)").Matches(heading).ShouldBeFalse();    // 中间隔着 span
        P(":has(~ p)").Matches(heading).ShouldBeTrue();
        P(":has(~ p)").Matches(paragraph).ShouldBeFalse();  // 前面的兄弟不算
    }

    [Fact]
    public void Should_SkipTextNodesWhenLookingForTheNextSibling()
    {
        var label = new LabelElement();
        var input = new InputElement();
        _ = new DivElement { Children = { label, new TextNode(" "), input } };

        P(":has(+ input)").Matches(label).ShouldBeTrue();
    }

    [Fact]
    public void Should_FollowEveryStepOfARelativeSelector()
    {
        // .list:has(> .item .badge)：.badge 必须在 .list 的某个直接子 .item 之内。
        var inItem = new DivElement
        {
            Class = "list",
            Children = { new DivElement { Class = "item", Children = { new DivElement { Children = { new SpanElement { Class = "badge" } } } } } },
        };
        var deeperItem = new DivElement
        {
            Class = "list",
            Children = { new DivElement { Children = { new DivElement { Class = "item", Children = { new SpanElement { Class = "badge" } } } } } },
        };

        var selector = P(".list:has(> .item .badge)");
        selector.Matches(inItem).ShouldBeTrue();
        selector.Matches(deeperItem).ShouldBeFalse();

        // + 之后再走后代：:has(+ .panel .error)
        var trigger = new ButtonElement();
        _ = new DivElement { Children = { trigger, new DivElement { Class = "panel", Children = { new SpanElement { Class = "error" } } } } };
        P(":has(+ .panel .error)").Matches(trigger).ShouldBeTrue();
        P(":has(+ .panel > .missing)").Matches(trigger).ShouldBeFalse();
    }

    [Fact]
    public void Should_UseNestedPseudoClassesInsideHas()
    {
        var checkedBox = new InputElement { Type = InputType.Checkbox, Checked = true };
        var form = new DivElement { Class = "form", Children = { new LabelElement { Children = { checkedBox } } } };
        var emptyForm = new DivElement { Class = "form", Children = { new InputElement { Type = InputType.Checkbox } } };

        P(".form:has(:checked)").Matches(form).ShouldBeTrue();
        P(".form:has(:checked)").Matches(emptyForm).ShouldBeFalse();
        P(".form:has(input:not(:checked))").Matches(emptyForm).ShouldBeTrue();
        P(":not(:has(img))").Matches(form).ShouldBeTrue();
    }

    [Fact]
    public void Should_UseMostSpecificRelativeSelectorForHas()
    {
        P(":has(img)").Specificity.ShouldBe(1);
        P(":has(> .a .b, #x)").Specificity.ShouldBe(100);
        P(".card:has(> .a .b)").Specificity.ShouldBe(30);
    }

    [Fact]
    public void Should_RestyleAnchorThroughTheCascade()
    {
        var sheet = SheetOf(
            (".card", new Style { BackgroundColor = Color.White }),
            (".card:has(> .badge)", new Style { BackgroundColor = Color.Red }));
        var plain = new DivElement { Class = "card" };
        var badged = new DivElement { Class = "card", Children = { new SpanElement { Class = "badge" } } };
        var root = new DivElement { Children = { plain, badged } };

        new LayoutEngine().Layout(root, [sheet], 400, 400);

        plain.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.White);
        badged.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Should_RematchAnchorWhenADescendantChanges()
    {
        // 后代的类名变化按结构变更记账（整树级联），锚点随之重新匹配——经真实引擎驱动验证。
        var sheet = SheetOf((".card:has(.selected)", new Style { BackgroundColor = Color.Red }));
        var item = new DivElement { Class = "item" };
        var card = new DivElement { Class = "card", Children = { new DivElement { Children = { item } } } };
        var root = new DivElement { Children = { card } };

        var engine = new MikoEngineBuilder().Build();
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(200, 200));
        engine.Initialize(root, [sheet], surface.Canvas, 200, 200);
        card.LayoutBox!.ComputedStyle.BackgroundColor.ShouldNotBe(Color.Red);

        item.Class = "item selected";
        engine.Render(surface.Canvas);
        card.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.Red);

        item.Class = "item";
        engine.Render(surface.Canvas);
        card.LayoutBox!.ComputedStyle.BackgroundColor.ShouldNotBe(Color.Red);
    }

    [Fact]
    public void Should_BucketHasRuleByTheClassOfItsCompound()
    {
        var sheet = SheetOf((".card:has(img)", new Style()));
        var candidates = new List<RuleIndex.Entry>();

        sheet.RuleIndex.CollectCandidates(new DivElement { Class = "card" }, candidates);
        candidates.Count.ShouldBe(1);
        candidates.Clear();
        sheet.RuleIndex.CollectCandidates(new DivElement { Children = { new ImageElement() } }, candidates);
        candidates.ShouldBeEmpty();
    }

    #endregion

    #region Parser validation and engine contracts

    [Theory]
    [InlineData(":is()")]
    [InlineData(":where( )")]
    [InlineData(":has()")]
    [InlineData(":not()")]
    [InlineData(":is(.a, )")]
    [InlineData(":is(.a")]
    [InlineData(":has(> )")]
    [InlineData(":is(:unknown)")]
    [InlineData(":frobnicate(.a)")]
    public void Should_RejectInvalidArguments(string selector)
    {
        Should.Throw<ArgumentException>(() => P(selector));
    }

    [Theory]
    [InlineData(":is(.a::before)")]
    [InlineData(":not(::after)")]
    [InlineData(":has(> .x::placeholder)")]
    [InlineData(":nth-child(1 of .a::before)")]
    public void Should_RejectPseudoElementsInsideArguments(string selector)
    {
        Should.Throw<ArgumentException>(() => P(selector));
    }

    [Fact]
    public void Should_RejectUnexpectedCharactersInsteadOfLooping()
    {
        Should.Throw<ArgumentException>(() => P(".a)"));
        Should.Throw<ArgumentException>(() => P("div!"));
    }

    [Fact]
    public void Should_KeepQuotedParenthesesAndCommasInsideArguments()
    {
        var titled = new DivElement { Class = "x" };
        var selector = P(":is([class=\"x\"], [class=\"a)b,c\"])");
        selector.ShouldBeOfType<IsSelector>().Inner.ShouldBeOfType<GroupSelector>().Selectors.Count.ShouldBe(2);
        selector.Matches(titled).ShouldBeTrue();
    }

    [Fact]
    public void Should_PropagateContentReadsFromArguments()
    {
        P(":is(.a, .b)").MayReadContentOrInlineStyle.ShouldBeFalse();
        P(":where(.a .b)").MayReadContentOrInlineStyle.ShouldBeFalse();
        P(".card:has(> img)").MayReadContentOrInlineStyle.ShouldBeFalse();
        P(":not(.a, .b)").MayReadContentOrInlineStyle.ShouldBeFalse();

        P(":is(.a, [text])").MayReadContentOrInlineStyle.ShouldBeTrue();
        P(":where([style])").MayReadContentOrInlineStyle.ShouldBeTrue();
        P(":has(> [textcontent])").MayReadContentOrInlineStyle.ShouldBeTrue();
    }

    [Fact]
    public void RandomTrees_NewSelectorsCascadeIdenticallyWithAndWithoutTheAncestorFilter()
    {
        // 与 AncestorFilterTests 同法：带过滤器的样式阶段与逐元素直接 Resolve 必须逐一相同。
        string[] classes = ["a", "b", "c", "card", "item"];
        var sheet = SheetOf(
            (":is(.a, .b) .c", new Style { Color = Color.FromRgb(1, 0, 0) }),
            (".card:has(> .item)", new Style { BackgroundColor = Color.FromRgb(2, 0, 0) }),
            (":where(.a) > .item:nth-child(odd)", new Style { Width = Length.Px(3) }),
            (".b :not(.a, .c) .item", new Style { Height = Length.Px(4) }),
            (".item:nth-last-child(2 of .item)", new Style { MarginLeft = Length.Px(5) }),
            ("div:has(+ .c) ~ .b", new Style { MarginTop = Length.Px(6) }));

        var random = new Random(158);
        for (int trial = 0; trial < 30; trial++)
        {
            var all = new List<Element>();
            var root = new DivElement { Id = "root" };
            all.Add(root);
            Grow(root, depth: 0);

            new LayoutEngine().Layout(root, [sheet], 400, 400);
            var filtered = all.ToDictionary(e => e, e => Snapshot(e.LayoutBox!.ComputedStyle));

            var resolver = new StyleResolver();
            foreach (var element in all)
            {
                var computed = resolver.Resolve(element, [sheet]);
                element.LayoutBox = new LayoutBox { Element = element, ComputedStyle = computed };
                Snapshot(computed).ShouldBe(filtered[element], $"trial {trial}: {element}");
            }

            void Grow(Element parent, int depth)
            {
                if (depth >= 4) return;
                int count = random.Next(1, 4);
                for (int i = 0; i < count; i++)
                {
                    var tokens = classes.Where(_ => random.Next(3) == 0).ToArray();
                    var child = new DivElement { Class = tokens.Length == 0 ? null : string.Join(' ', tokens) };
                    parent.AddChild(child);
                    all.Add(child);
                    Grow(child, depth + 1);
                }
            }
        }

        static string Snapshot(ComputedStyle s) =>
            $"{s.Color}|{s.BackgroundColor}|{s.Width}|{s.Height}|{s.MarginLeft}|{s.MarginTop}";
    }

    #endregion
}
