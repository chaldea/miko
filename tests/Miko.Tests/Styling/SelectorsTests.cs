using Miko.Core.DomElements;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class SelectorsTests
{
    [Fact]
    public void ClassSelector_ShouldMatchElementWithClass()
    {
        var element = new DivElement { Class = "container primary" };
        var selector = new ClassSelector(".container");

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void ClassSelector_ShouldNotMatchElementWithoutClass()
    {
        var element = new DivElement { Class = "primary" };
        var selector = new ClassSelector(".container");

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void ClassSelector_ShouldHaveCorrectSpecificity()
    {
        var selector = new ClassSelector(".container");

        selector.Specificity.ShouldBe(10);
    }

    [Fact]
    public void IdSelector_ShouldMatchElementWithId()
    {
        var element = new DivElement { Id = "main" };
        var selector = new IdSelector("#main");

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void IdSelector_ShouldNotMatchElementWithDifferentId()
    {
        var element = new DivElement { Id = "sidebar" };
        var selector = new IdSelector("#main");

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void IdSelector_ShouldHaveCorrectSpecificity()
    {
        var selector = new IdSelector("#main");

        selector.Specificity.ShouldBe(100);
    }

    [Fact]
    public void TagSelector_ShouldMatchElementByTagName()
    {
        var element = new DivElement();
        var selector = new TagSelector("div");

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void TagSelector_ShouldBeCaseInsensitive()
    {
        var element = new DivElement();
        var selector = new TagSelector("DIV");

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void TagSelector_ShouldNotMatchDifferentTag()
    {
        var element = new DivElement();
        var selector = new TagSelector("span");

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void TagSelector_ShouldHaveCorrectSpecificity()
    {
        var selector = new TagSelector("div");

        selector.Specificity.ShouldBe(1);
    }

    [Fact]
    public void UniversalSelector_ShouldMatchAnyElement()
    {
        var div = new DivElement();
        var span = new SpanElement();
        var selector = new UniversalSelector();

        selector.Matches(div).ShouldBeTrue();
        selector.Matches(span).ShouldBeTrue();
    }

    [Fact]
    public void UniversalSelector_ShouldHaveZeroSpecificity()
    {
        var selector = new UniversalSelector();

        selector.Specificity.ShouldBe(0);
    }

    [Fact]
    public void Specificity_ShouldOrderCorrectly()
    {
        var tagSelector = new TagSelector("div");
        var classSelector = new ClassSelector(".container");
        var idSelector = new IdSelector("#main");

        idSelector.Specificity.ShouldBeGreaterThan(classSelector.Specificity);
        classSelector.Specificity.ShouldBeGreaterThan(tagSelector.Specificity);
    }
}
