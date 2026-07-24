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
}
