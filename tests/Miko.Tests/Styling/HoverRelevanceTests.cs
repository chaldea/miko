using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-104 问题1：:hover 相关性分析（<see cref="StyleSheet.IsHoverRelevant"/>）。
/// 只有元素的 Hover 状态可能影响某条规则匹配时才需要因悬停触发样式重算。
/// </summary>
public class HoverRelevanceTests
{
    private static StyleSheet SheetWith(params string[] selectors)
    {
        var sheet = new StyleSheet();
        foreach (var selector in selectors)
            sheet.AddRule(CssSelectorParser.Parse(selector), new Style());
        return sheet;
    }

    [Fact]
    public void NoHoverRules_NotRelevant()
    {
        var sheet = SheetWith(".btn", "p", "div > span");
        sheet.UsesHoverPseudo.ShouldBeFalse();
        sheet.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();
    }

    [Fact]
    public void CompoundHover_RelevantOnlyForStrippedMatch()
    {
        var sheet = SheetWith(".btn:hover");
        sheet.UsesHoverPseudo.ShouldBeTrue();
        sheet.IsHoverRelevant(new ButtonElement { Class = "btn" }).ShouldBeTrue();
        sheet.IsHoverRelevant(new ButtonElement()).ShouldBeFalse();      // 无 btn 类
        sheet.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();
    }

    [Fact]
    public void BareHover_MatchesEverything()
    {
        var sheet = SheetWith(":hover");
        sheet.IsHoverRelevant(new ParagraphElement()).ShouldBeTrue();
    }

    [Fact]
    public void NotHover_RelevantForCompoundBase()
    {
        var sheet = SheetWith(".btn:not(:hover)");
        sheet.IsHoverRelevant(new ButtonElement { Class = "btn" }).ShouldBeTrue();
        sheet.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();
    }

    [Fact]
    public void DescendantCombinator_RelevantForHoverCompoundSide()
    {
        // .card:hover .title：持有 Hover 状态的是 .card（悬停复合选择器一侧），
        // .title 无需置状态——级联时按完整选择器重新匹配。
        var sheet = SheetWith(".card:hover .title");
        sheet.IsHoverRelevant(new DivElement { Class = "card" }).ShouldBeTrue();
        sheet.IsHoverRelevant(new DivElement { Class = "title" }).ShouldBeFalse();
        sheet.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();
    }

    [Fact]
    public void SiblingCombinator_RelevantForPreviousSide()
    {
        var sheet = SheetWith(".a:hover + .b");
        sheet.IsHoverRelevant(new DivElement { Class = "a" }).ShouldBeTrue();
        sheet.IsHoverRelevant(new DivElement { Class = "b" }).ShouldBeFalse();
    }

    [Fact]
    public void MediaAndPseudoElementRules_AreAnalyzed()
    {
        var sheet = new StyleSheet();
        sheet.AddMediaRule(new MediaCondition(_ => true), CssSelectorParser.Parse(".m:hover"), new Style());
        sheet.IsHoverRelevant(new DivElement { Class = "m" }).ShouldBeTrue();

        var sheet2 = new StyleSheet();
        sheet2.PseudoElementRules.Add(new PseudoElementRule
        {
            Selector = CssSelectorParser.Parse(".range:hover"),
            Type = PseudoElementType.RangeThumb,
            Style = new Style(),
        });
        sheet2.UsesHoverPseudo.ShouldBeTrue();
        sheet2.IsHoverRelevant(new InputElement { Class = "range" }).ShouldBeTrue();
    }

    [Fact]
    public void AnalysisCache_InvalidatedOnMutation()
    {
        var sheet = SheetWith(".btn");
        sheet.UsesHoverPseudo.ShouldBeFalse();   // 触发一次分析并缓存

        sheet.AddRule(CssSelectorParser.Parse(".x:hover"), new Style());

        sheet.UsesHoverPseudo.ShouldBeTrue();
        sheet.IsHoverRelevant(new DivElement { Class = "x" }).ShouldBeTrue();
    }

    // ISSUE-158：函数式伪类的参数可能把 :hover 放在主体之外的元素上，被悬停的那个元素必须判为相关。

    [Fact]
    public void Has_RelevantForTheHoveredDescendant()
    {
        // .card:has(.btn:hover)：被悬停的是 .card 里的 .btn；只剥离外层会得到 .card，漏掉 .btn。
        var sheet = SheetWith(".card:has(.btn:hover)");
        sheet.UsesHoverPseudo.ShouldBeTrue();
        sheet.IsHoverRelevant(new ButtonElement { Class = "btn" }).ShouldBeTrue();
        sheet.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();
    }

    [Fact]
    public void Has_BareHoverStep_MatchesEverything()
    {
        var sheet = SheetWith(".card:has(> :hover)");
        sheet.IsHoverRelevant(new ParagraphElement()).ShouldBeTrue();
    }

    [Fact]
    public void Is_RelevantForSubjectAndForHoveredAncestorInsideArgument()
    {
        var subject = SheetWith("li:is(.a:hover, .b)");
        subject.IsHoverRelevant(new LiElement { Class = "a" }).ShouldBeTrue();
        subject.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();

        // .x :is(.menu:hover .item)：被悬停的是祖先 .menu。
        var ancestor = SheetWith(".x :is(.menu:hover .item)");
        ancestor.IsHoverRelevant(new DivElement { Class = "menu" }).ShouldBeTrue();
        ancestor.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();

        var where = SheetWith(".y:where(.menu:hover > .item)");
        where.IsHoverRelevant(new DivElement { Class = "menu" }).ShouldBeTrue();
        where.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();
    }

    [Fact]
    public void Not_WithComplexArgument_RelevantForHoveredAncestor()
    {
        var sheet = SheetWith(".y:not(.menu:hover .item)");
        sheet.IsHoverRelevant(new DivElement { Class = "menu" }).ShouldBeTrue();
        sheet.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();
    }

    [Fact]
    public void NotHoverList_StaysScopedToTheCompound()
    {
        // :not() 现在接受选择器列表；深入参数不能把主体自身的 :hover 变成通用模式。
        var sheet = SheetWith(".btn:not(:hover, .active)");
        sheet.IsHoverRelevant(new ButtonElement { Class = "btn" }).ShouldBeTrue();
        sheet.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();
    }

    [Fact]
    public void NthChildOf_RelevantForHoveredSiblings()
    {
        // :nth-child(1 of .tab:hover) 数的是被悬停的兄弟 .tab。
        var sheet = SheetWith(".panel:nth-child(1 of .tab:hover)");
        sheet.IsHoverRelevant(new DivElement { Class = "tab" }).ShouldBeTrue();
        sheet.IsHoverRelevant(new ParagraphElement()).ShouldBeFalse();
    }

    [Fact]
    public void FunctionalPseudoClassesWithoutHover_AreNotRelevant()
    {
        var sheet = SheetWith(".card:has(img)", ":is(.a, .b) .c", "li:nth-child(odd of .x)", ":where(.a):not(.b)");
        sheet.UsesHoverPseudo.ShouldBeFalse();
        sheet.IsHoverRelevant(new DivElement { Class = "card a b c x" }).ShouldBeFalse();
    }
}
