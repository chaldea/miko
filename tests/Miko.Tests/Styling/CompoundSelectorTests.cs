using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class CompoundSelectorTests
{
    [Fact]
    public void CompoundSelector_ShouldMatchWhenAllSelectorsMatch()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);

        var selector = new CompoundSelector(
            new TagSelector("button"),
            new HoverSelector()
        );

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void CompoundSelector_ShouldNotMatchWhenOneSelectorsDoesNotMatch()
    {
        var element = new ButtonElement();
        // 没有设置 Hover 状态

        var selector = new CompoundSelector(
            new TagSelector("button"),
            new HoverSelector()
        );

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void CompoundSelector_ShouldNotMatchWhenTagDoesNotMatch()
    {
        var element = new DivElement();
        element.SetState(ElementState.Hover);

        var selector = new CompoundSelector(
            new TagSelector("button"),
            new HoverSelector()
        );

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void CompoundSelector_SpecificityShouldBeSumOfComponents()
    {
        var selector = new CompoundSelector(
            new TagSelector("button"),    // 1
            new HoverSelector()           // 10
        );

        selector.Specificity.ShouldBe(11);
    }

    [Fact]
    public void CompoundSelector_WithClassAndPseudoClass_ShouldCalculateCorrectSpecificity()
    {
        var selector = new CompoundSelector(
            new ClassSelector("btn"),     // 10
            new HoverSelector(),          // 10
            new ActiveSelector()          // 10
        );

        selector.Specificity.ShouldBe(30);
    }

    [Fact]
    public void CompoundSelector_WithIdAndPseudoClass_ShouldCalculateCorrectSpecificity()
    {
        var selector = new CompoundSelector(
            new IdSelector("main"),       // 100
            new FocusSelector()           // 10
        );

        selector.Specificity.ShouldBe(110);
    }

    [Fact]
    public void CompoundSelector_Empty_ShouldNotMatch()
    {
        var element = new ButtonElement();
        var selector = new CompoundSelector();

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void CompoundSelector_WithSingleSelector_ShouldMatchLikeSingleSelector()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);

        var selector = new CompoundSelector(new HoverSelector());

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void CompoundSelector_WithClassSelector_ShouldMatch()
    {
        var element = new ButtonElement { Class = "btn primary" };
        element.SetState(ElementState.Hover);

        var selector = new CompoundSelector(
            new ClassSelector("btn"),
            new HoverSelector()
        );

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void CompoundSelector_Add_ShouldAddSelector()
    {
        var selector = new CompoundSelector();
        selector.Add(new TagSelector("button"));
        selector.Add(new HoverSelector());

        selector.Selectors.Count.ShouldBe(2);
        selector.Specificity.ShouldBe(11);
    }

    // SelectorParser 测试
    [Fact]
    public void SelectorParser_ShouldParseTagSelector()
    {
        var selector = SelectorParser.Parse("button");

        selector.ShouldBeOfType<TagSelector>();
        selector.Specificity.ShouldBe(1);
    }

    [Fact]
    public void SelectorParser_ShouldParseClassSelector()
    {
        var selector = SelectorParser.Parse(".btn");

        selector.ShouldBeOfType<ClassSelector>();
        selector.Specificity.ShouldBe(10);
    }

    [Fact]
    public void SelectorParser_ShouldParseIdSelector()
    {
        var selector = SelectorParser.Parse("#main");

        selector.ShouldBeOfType<IdSelector>();
        selector.Specificity.ShouldBe(100);
    }

    [Fact]
    public void SelectorParser_ShouldParsePseudoClassSelector()
    {
        var selector = SelectorParser.Parse(":hover");

        selector.ShouldBeOfType<HoverSelector>();
        selector.Specificity.ShouldBe(10);
    }

    [Fact]
    public void SelectorParser_ShouldParseTagWithPseudoClass()
    {
        var selector = SelectorParser.Parse("button:hover");

        selector.ShouldBeOfType<CompoundSelector>();
        selector.Specificity.ShouldBe(11);

        var element = new ButtonElement();
        element.SetState(ElementState.Hover);
        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void SelectorParser_ShouldParseClassWithPseudoClass()
    {
        var selector = SelectorParser.Parse(".btn:active");

        selector.ShouldBeOfType<CompoundSelector>();
        selector.Specificity.ShouldBe(20);

        var element = new ButtonElement { Class = "btn" };
        element.SetState(ElementState.Active);
        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void SelectorParser_ShouldParseIdWithPseudoClass()
    {
        var selector = SelectorParser.Parse("#submit:focus");

        selector.ShouldBeOfType<CompoundSelector>();
        selector.Specificity.ShouldBe(110);

        var element = new InputElement { Id = "submit" };
        element.SetState(ElementState.Focus);
        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void SelectorParser_ShouldParseMultiplePseudoClasses()
    {
        var selector = SelectorParser.Parse("button:hover:active");

        selector.ShouldBeOfType<CompoundSelector>();
        selector.Specificity.ShouldBe(21); // tag(1) + hover(10) + active(10)

        var element = new ButtonElement();
        element.SetState(ElementState.Hover);
        element.SetState(ElementState.Active);
        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void SelectorParser_ShouldParseDisabledPseudoClass()
    {
        var selector = SelectorParser.Parse("button:disabled");

        selector.ShouldBeOfType<CompoundSelector>();

        var element = new ButtonElement();
        element.SetState(ElementState.Disabled);
        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void SelectorParser_ShouldParseEnabledPseudoClass()
    {
        var selector = SelectorParser.Parse("button:enabled");

        selector.ShouldBeOfType<CompoundSelector>();

        var element = new ButtonElement();
        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void SelectorParser_ShouldParseComplexSelector()
    {
        var selector = SelectorParser.Parse(".btn.primary:hover");

        selector.ShouldBeOfType<CompoundSelector>();
        selector.Specificity.ShouldBe(30); // class(10) + class(10) + hover(10)

        var element = new ButtonElement { Class = "btn primary" };
        element.SetState(ElementState.Hover);
        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void SelectorParser_UnknownPseudoClass_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => SelectorParser.Parse(":unknown"));
    }
}
