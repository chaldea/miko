using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

public class MediaQueryTests
{
    #region MediaCondition Tests

    [Fact]
    public void MediaCondition_MinWidth_MatchesWhenViewportWiderOrEqual()
    {
        var condition = MediaCondition.MinWidth(768);

        condition.Matches(new ViewportInfo(1024, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(768, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(767, 600)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_MaxWidth_MatchesWhenViewportNarrowerOrEqual()
    {
        var condition = MediaCondition.MaxWidth(768);

        condition.Matches(new ViewportInfo(500, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(768, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(769, 600)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_MinHeight_MatchesWhenViewportTallerOrEqual()
    {
        var condition = MediaCondition.MinHeight(600);

        condition.Matches(new ViewportInfo(800, 800)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(800, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(800, 599)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_MaxHeight_MatchesWhenViewportShorterOrEqual()
    {
        var condition = MediaCondition.MaxHeight(600);

        condition.Matches(new ViewportInfo(800, 400)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(800, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(800, 601)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_CombinedExpression_MatchesWithinRange()
    {
        var condition = new MediaCondition(v => v.Width >= 768 && v.Width <= 1024);

        condition.Matches(new ViewportInfo(800, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(768, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(1024, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(767, 600)).ShouldBeFalse();
        condition.Matches(new ViewportInfo(1025, 600)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_CustomExpression_SupportsArbitraryLogic()
    {
        var condition = new MediaCondition(v => v.Width > v.Height);

        condition.Matches(new ViewportInfo(1024, 768)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(768, 1024)).ShouldBeFalse();
        condition.Matches(new ViewportInfo(800, 800)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_ExpressionProperty_IsAccessible()
    {
        MediaCondition.MinWidth(768).Expression.ShouldNotBeNull();
    }

    #endregion

    #region StyleResolver with Media Rules

    [Fact]
    public void Resolve_MediaRuleMatchesViewport_AppliesStyles()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("box").Set(x => x.Width, Length.Px(100)));
        var condition = MediaCondition.MinWidth(768);
        styleSheet.AddMediaRule(condition, Style.Class("box").Set(x => x.Width, Length.Px(200)));

        var element = new DivElement { Class = "box" };
        var computed = new StyleResolver().Resolve(element, [styleSheet], new ViewportInfo(1024, 768));
        computed.Width.Value.ShouldBe(200);
    }

    [Fact]
    public void Resolve_MediaRuleDoesNotMatchViewport_DoesNotApplyStyles()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("box").Set(x => x.Width, Length.Px(100)));
        var condition = MediaCondition.MinWidth(768);
        styleSheet.AddMediaRule(condition, Style.Class("box").Set(x => x.Width, Length.Px(200)));

        var element = new DivElement { Class = "box" };
        var computed = new StyleResolver().Resolve(element, [styleSheet], new ViewportInfo(480, 768));
        computed.Width.Value.ShouldBe(100);
    }

    [Fact]
    public void Resolve_NoViewportProvided_SkipsMediaRules()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("box").Set(x => x.Width, Length.Px(100)));
        var condition = MediaCondition.MinWidth(768);
        styleSheet.AddMediaRule(condition, Style.Class("box").Set(x => x.Width, Length.Px(200)));

        var element = new DivElement { Class = "box" };
        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.Width.Value.ShouldBe(100);
    }

    [Fact]
    public void Resolve_MediaRuleWithHigherSpecificity_WinsOverBaseRule()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("box").Set(x => x.Width, Length.Px(100)));
        var condition = MediaCondition.MinWidth(768);
        styleSheet.AddMediaRule(condition, Style.Id("main").Set(x => x.Width, Length.Px(300)));

        var element = new DivElement { Id = "main", Class = "box" };
        var computed = new StyleResolver().Resolve(element, [styleSheet], new ViewportInfo(1024, 768));
        computed.Width.Value.ShouldBe(300);
    }

    [Fact]
    public void Resolve_MultipleMediaRules_OnlyMatchingOnesApply()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("box").Set(x => x.Width, Length.Px(100)));
        styleSheet.AddMediaRule(MediaCondition.MaxWidth(480), Style.Class("box").Set(x => x.Width, Length.Px(50)));
        styleSheet.AddMediaRule(MediaCondition.MinWidth(1024), Style.Class("box").Set(x => x.Width, Length.Px(400)));

        var element = new DivElement { Class = "box" };
        var resolver = new StyleResolver();

        resolver.Resolve(element, [styleSheet], new ViewportInfo(320, 600)).Width.Value.ShouldBe(50);
        resolver.Resolve(element, [styleSheet], new ViewportInfo(800, 600)).Width.Value.ShouldBe(100);
        resolver.Resolve(element, [styleSheet], new ViewportInfo(1200, 600)).Width.Value.ShouldBe(400);
    }

    #endregion

    #region StyleSheet AddMediaRule Helper

    [Fact]
    public void AddMediaRule_SameCondition_GroupsRulesUnderSameMediaRule()
    {
        var styleSheet = new StyleSheet();
        var condition = MediaCondition.MinWidth(768);

        styleSheet.AddMediaRule(condition, Style.Class("a").Set(x => x.Width, Length.Px(100)));
        styleSheet.AddMediaRule(condition, Style.Class("b").Set(x => x.Width, Length.Px(200)));

        styleSheet.MediaRules.Count.ShouldBe(1);
        styleSheet.MediaRules[0].Rules.Count.ShouldBe(2);
    }

    [Fact]
    public void AddMediaRule_DifferentConditions_CreatesSeparateMediaRules()
    {
        var styleSheet = new StyleSheet();

        styleSheet.AddMediaRule(MediaCondition.MinWidth(768), Style.Class("a").Set(x => x.Width, Length.Px(100)));
        styleSheet.AddMediaRule(MediaCondition.MinWidth(1024), Style.Class("b").Set(x => x.Width, Length.Px(200)));

        styleSheet.MediaRules.Count.ShouldBe(2);
    }

    #endregion

    #region Integration with LayoutEngine

    [Fact]
    public void LayoutEngine_MediaQueryChangesDisplay_AffectsLayout()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("sidebar").Set(x => x.Display, Display.Block));
        styleSheet.AddRule(Style.Class("sidebar").Set(x => x.Width, Length.Px(200)));
        styleSheet.AddMediaRule(MediaCondition.MaxWidth(600), Style.Class("sidebar").Set(x => x.Display, Display.None));

        var root = new DivElement { Class = "container" };
        var sidebar = new DivElement { Class = "sidebar" };
        root.AddChild(sidebar);

        var layoutEngine = new LayoutEngine();

        layoutEngine.Layout(root, [styleSheet], 1024, 768).Children.Count.ShouldBe(1);
        layoutEngine.Layout(root, [styleSheet], 480, 768).Children.Count.ShouldBe(0);
    }

    [Fact]
    public void LayoutEngine_MediaQueryChangesWidth_AffectsBoxDimensions()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("content").Set(x => x.Display, Display.Block));
        styleSheet.AddRule(Style.Class("content").Set(x => x.Width, Length.Px(600)));
        styleSheet.AddMediaRule(MediaCondition.MaxWidth(768), Style.Class("content").Set(x => x.Width, Length.Percent(100)));

        var root = new DivElement { Class = "content" };
        var layoutEngine = new LayoutEngine();

        layoutEngine.Layout(root, [styleSheet], 1024, 768).BoxModel.Content.Width.ShouldBe(600);
        layoutEngine.Layout(root, [styleSheet], 480, 768).BoxModel.Content.Width.ShouldBe(480);
    }

    #endregion
}
