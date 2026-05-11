using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class MediaQueryTests
{
    #region MediaCondition Tests

    [Fact]
    public void MediaCondition_MinWidth_MatchesWhenViewportWiderOrEqual()
    {
        var condition = new MediaCondition { MinWidth = 768 };

        condition.Matches(new ViewportInfo(1024, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(768, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(767, 600)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_MaxWidth_MatchesWhenViewportNarrowerOrEqual()
    {
        var condition = new MediaCondition { MaxWidth = 768 };

        condition.Matches(new ViewportInfo(500, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(768, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(769, 600)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_MinHeight_MatchesWhenViewportTallerOrEqual()
    {
        var condition = new MediaCondition { MinHeight = 600 };

        condition.Matches(new ViewportInfo(800, 800)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(800, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(800, 599)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_MaxHeight_MatchesWhenViewportShorterOrEqual()
    {
        var condition = new MediaCondition { MaxHeight = 600 };

        condition.Matches(new ViewportInfo(800, 400)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(800, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(800, 601)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_CombinedMinMaxWidth_MatchesWithinRange()
    {
        var condition = new MediaCondition { MinWidth = 768, MaxWidth = 1024 };

        condition.Matches(new ViewportInfo(800, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(768, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(1024, 600)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(767, 600)).ShouldBeFalse();
        condition.Matches(new ViewportInfo(1025, 600)).ShouldBeFalse();
    }

    [Fact]
    public void MediaCondition_NoConditions_AlwaysMatches()
    {
        var condition = new MediaCondition();

        condition.Matches(new ViewportInfo(100, 100)).ShouldBeTrue();
        condition.Matches(new ViewportInfo(5000, 5000)).ShouldBeTrue();
    }

    #endregion

    #region StyleResolver with Media Rules

    [Fact]
    public void Resolve_MediaRuleMatchesViewport_AppliesStyles()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(new ClassSelector("box"), new Style { Width = Length.Px(100) });
        styleSheet.MediaRules.Add(new MediaRule
        {
            Condition = new MediaCondition { MinWidth = 768 },
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new ClassSelector("box"),
                    Style = new Style { Width = Length.Px(200) }
                }
            }
        });

        var element = new DivElement { Class = "box" };
        var resolver = new StyleResolver();

        var computed = resolver.Resolve(element, new List<StyleSheet> { styleSheet }, new ViewportInfo(1024, 768));
        computed.Width.Value.ShouldBe(200);
    }

    [Fact]
    public void Resolve_MediaRuleDoesNotMatchViewport_DoesNotApplyStyles()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(new ClassSelector("box"), new Style { Width = Length.Px(100) });
        styleSheet.MediaRules.Add(new MediaRule
        {
            Condition = new MediaCondition { MinWidth = 768 },
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new ClassSelector("box"),
                    Style = new Style { Width = Length.Px(200) }
                }
            }
        });

        var element = new DivElement { Class = "box" };
        var resolver = new StyleResolver();

        var computed = resolver.Resolve(element, new List<StyleSheet> { styleSheet }, new ViewportInfo(480, 768));
        computed.Width.Value.ShouldBe(100);
    }

    [Fact]
    public void Resolve_NoViewportProvided_SkipsMediaRules()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(new ClassSelector("box"), new Style { Width = Length.Px(100) });
        styleSheet.MediaRules.Add(new MediaRule
        {
            Condition = new MediaCondition { MinWidth = 768 },
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new ClassSelector("box"),
                    Style = new Style { Width = Length.Px(200) }
                }
            }
        });

        var element = new DivElement { Class = "box" };
        var resolver = new StyleResolver();

        var computed = resolver.Resolve(element, new List<StyleSheet> { styleSheet });
        computed.Width.Value.ShouldBe(100);
    }

    [Fact]
    public void Resolve_MediaRuleWithHigherSpecificity_WinsOverBaseRule()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(new ClassSelector("box"), new Style { Width = Length.Px(100) });
        styleSheet.MediaRules.Add(new MediaRule
        {
            Condition = new MediaCondition { MinWidth = 768 },
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new IdSelector("main"),
                    Style = new Style { Width = Length.Px(300) }
                }
            }
        });

        var element = new DivElement { Id = "main", Class = "box" };
        var resolver = new StyleResolver();

        var computed = resolver.Resolve(element, new List<StyleSheet> { styleSheet }, new ViewportInfo(1024, 768));
        computed.Width.Value.ShouldBe(300);
    }

    [Fact]
    public void Resolve_MultipleMediaRules_OnlyMatchingOnesApply()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(new ClassSelector("box"), new Style { Width = Length.Px(100) });
        styleSheet.MediaRules.Add(new MediaRule
        {
            Condition = new MediaCondition { MaxWidth = 480 },
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new ClassSelector("box"),
                    Style = new Style { Width = Length.Px(50) }
                }
            }
        });
        styleSheet.MediaRules.Add(new MediaRule
        {
            Condition = new MediaCondition { MinWidth = 1024 },
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new ClassSelector("box"),
                    Style = new Style { Width = Length.Px(400) }
                }
            }
        });

        var element = new DivElement { Class = "box" };
        var resolver = new StyleResolver();

        // Viewport 320px: only max-width:480 matches
        var small = resolver.Resolve(element, new List<StyleSheet> { styleSheet }, new ViewportInfo(320, 600));
        small.Width.Value.ShouldBe(50);

        // Viewport 800px: neither media rule matches
        var medium = resolver.Resolve(element, new List<StyleSheet> { styleSheet }, new ViewportInfo(800, 600));
        medium.Width.Value.ShouldBe(100);

        // Viewport 1200px: only min-width:1024 matches
        var large = resolver.Resolve(element, new List<StyleSheet> { styleSheet }, new ViewportInfo(1200, 600));
        large.Width.Value.ShouldBe(400);
    }

    #endregion

    #region StyleSheet AddMediaRule Helper

    [Fact]
    public void AddMediaRule_SameCondition_GroupsRulesUnderSameMediaRule()
    {
        var styleSheet = new StyleSheet();
        var condition = new MediaCondition { MinWidth = 768 };

        styleSheet.AddMediaRule(condition, new ClassSelector("a"), new Style { Width = Length.Px(100) });
        styleSheet.AddMediaRule(condition, new ClassSelector("b"), new Style { Width = Length.Px(200) });

        styleSheet.MediaRules.Count.ShouldBe(1);
        styleSheet.MediaRules[0].Rules.Count.ShouldBe(2);
    }

    [Fact]
    public void AddMediaRule_DifferentConditions_CreatesSeparateMediaRules()
    {
        var styleSheet = new StyleSheet();

        styleSheet.AddMediaRule(new MediaCondition { MinWidth = 768 }, new ClassSelector("a"), new Style { Width = Length.Px(100) });
        styleSheet.AddMediaRule(new MediaCondition { MinWidth = 1024 }, new ClassSelector("b"), new Style { Width = Length.Px(200) });

        styleSheet.MediaRules.Count.ShouldBe(2);
    }

    #endregion

    #region Integration with LayoutEngine

    [Fact]
    public void LayoutEngine_MediaQueryChangesDisplay_AffectsLayout()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(new ClassSelector("sidebar"), new Style
        {
            Display = Display.Block,
            Width = Length.Px(200)
        });
        styleSheet.MediaRules.Add(new MediaRule
        {
            Condition = new MediaCondition { MaxWidth = 600 },
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new ClassSelector("sidebar"),
                    Style = new Style { Display = Display.None }
                }
            }
        });

        var root = new DivElement { Class = "container" };
        var sidebar = new DivElement { Class = "sidebar" };
        root.AddChild(sidebar);

        var layoutEngine = new LayoutEngine();

        // Wide viewport: sidebar is visible
        var wideLayout = layoutEngine.Layout(root, new List<StyleSheet> { styleSheet }, 1024, 768);
        wideLayout.Children.Count.ShouldBe(1);

        // Narrow viewport: sidebar is hidden (display: none)
        var narrowLayout = layoutEngine.Layout(root, new List<StyleSheet> { styleSheet }, 480, 768);
        narrowLayout.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void LayoutEngine_MediaQueryChangesWidth_AffectsBoxDimensions()
    {
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(new ClassSelector("content"), new Style
        {
            Display = Display.Block,
            Width = Length.Px(600)
        });
        styleSheet.MediaRules.Add(new MediaRule
        {
            Condition = new MediaCondition { MaxWidth = 768 },
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new ClassSelector("content"),
                    Style = new Style { Width = Length.Percent(100) }
                }
            }
        });

        var root = new DivElement { Class = "content" };
        var layoutEngine = new LayoutEngine();

        // Wide viewport: fixed 600px width
        var wideLayout = layoutEngine.Layout(root, new List<StyleSheet> { styleSheet }, 1024, 768);
        wideLayout.BoxModel.Content.Width.ShouldBe(600);

        // Narrow viewport: 100% width = viewport width
        var narrowLayout = layoutEngine.Layout(root, new List<StyleSheet> { styleSheet }, 480, 768);
        narrowLayout.BoxModel.Content.Width.ShouldBe(480);
    }

    #endregion
}
