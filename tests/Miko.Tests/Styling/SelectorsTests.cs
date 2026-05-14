using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

public class SelectorsTests
{
    [Fact]
    public void ClassSelector_ShouldMatchElementWithClass()
    {
        var element = new DivElement { Class = "container primary" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("container").Set(x => x.BackgroundColor, Color.Red));

        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void ClassSelector_ShouldNotMatchElementWithoutClass()
    {
        var element = new DivElement { Class = "primary" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("container").Set(x => x.BackgroundColor, Color.Red));

        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void ClassSelector_ShouldHaveCorrectSpecificity()
    {
        var element = new DivElement { Id = "main", Class = "container" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("container").Set(x => x.BackgroundColor, Color.Red));
        styleSheet.AddRule(Style.Id("main").Set(x => x.BackgroundColor, Color.Blue));

        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.BackgroundColor.ShouldBe(Color.Blue); // ID wins (specificity 100 > 10)
    }

    [Fact]
    public void IdSelector_ShouldMatchElementWithId()
    {
        var element = new DivElement { Id = "main" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Id("main").Set(x => x.BackgroundColor, Color.Blue));

        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.BackgroundColor.ShouldBe(Color.Blue);
    }

    [Fact]
    public void IdSelector_ShouldNotMatchElementWithDifferentId()
    {
        var element = new DivElement { Id = "sidebar" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Id("main").Set(x => x.BackgroundColor, Color.Blue));

        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void TagSelector_ShouldMatchElementByTagName()
    {
        var element = new DivElement();
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<DivElement>().Set(x => x.BackgroundColor, Color.Green));

        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.BackgroundColor.ShouldBe(Color.Green);
    }

    [Fact]
    public void TagSelector_ShouldNotMatchDifferentTag()
    {
        var element = new SpanElement();
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<DivElement>().Set(x => x.BackgroundColor, Color.Green));

        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void Specificity_ShouldOrderCorrectly()
    {
        var element = new DivElement { Id = "main", Class = "container" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<DivElement>().Set(x => x.BackgroundColor, Color.Red));
        styleSheet.AddRule(Style.Class("container").Set(x => x.BackgroundColor, Color.Green));
        styleSheet.AddRule(Style.Id("main").Set(x => x.BackgroundColor, Color.Blue));

        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.BackgroundColor.ShouldBe(Color.Blue); // ID > Class > Tag
    }

    [Fact]
    public void WhereSelector_ShouldMatchByPredicate()
    {
        var element = new AnchorElement { Href = "https://example.com" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<AnchorElement>()
            .Where(x => x.Href != null)
            .Set(x => x.Color, Color.Blue));

        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void WhereSelector_ShouldNotMatchWhenPredicateFails()
    {
        var element = new AnchorElement();
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<AnchorElement>()
            .Where(x => x.Href != null)
            .Set(x => x.FontSize, Length.Px(24)));

        var computed = new StyleResolver().Resolve(element, [styleSheet]);
        computed.FontSize.Value.ShouldBe(16); // default, not 24
    }
}
