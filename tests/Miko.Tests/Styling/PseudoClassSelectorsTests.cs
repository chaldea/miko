using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class PseudoClassSelectorsTests
{
    [Fact]
    public void HoverSelector_ShouldMatchHoveredElement()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);
        var selector = new HoverSelector();

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void HoverSelector_ShouldNotMatchNonHoveredElement()
    {
        var element = new ButtonElement();
        var selector = new HoverSelector();

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void HoverSelector_ShouldHaveSpecificity10()
    {
        var selector = new HoverSelector();

        selector.Specificity.ShouldBe(10);
    }

    [Fact]
    public void ActiveSelector_ShouldMatchActiveElement()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Active);
        var selector = new ActiveSelector();

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void ActiveSelector_ShouldNotMatchInactiveElement()
    {
        var element = new ButtonElement();
        var selector = new ActiveSelector();

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void ActiveSelector_ShouldHaveSpecificity10()
    {
        var selector = new ActiveSelector();

        selector.Specificity.ShouldBe(10);
    }

    [Fact]
    public void FocusSelector_ShouldMatchFocusedElement()
    {
        var element = new InputElement();
        element.SetState(ElementState.Focus);
        var selector = new FocusSelector();

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void FocusSelector_ShouldNotMatchUnfocusedElement()
    {
        var element = new InputElement();
        var selector = new FocusSelector();

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void FocusSelector_ShouldHaveSpecificity10()
    {
        var selector = new FocusSelector();

        selector.Specificity.ShouldBe(10);
    }

    [Fact]
    public void DisabledSelector_ShouldMatchDisabledElement()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Disabled);
        var selector = new DisabledSelector();

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void DisabledSelector_ShouldNotMatchEnabledElement()
    {
        var element = new ButtonElement();
        var selector = new DisabledSelector();

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void DisabledSelector_ShouldMatchChildOfDisabledParent()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);
        parent.SetState(ElementState.Disabled);
        var selector = new DisabledSelector();

        selector.Matches(child).ShouldBeTrue();
    }

    [Fact]
    public void DisabledSelector_ShouldHaveSpecificity10()
    {
        var selector = new DisabledSelector();

        selector.Specificity.ShouldBe(10);
    }

    [Fact]
    public void EnabledSelector_ShouldMatchEnabledElement()
    {
        var element = new ButtonElement();
        var selector = new EnabledSelector();

        selector.Matches(element).ShouldBeTrue();
    }

    [Fact]
    public void EnabledSelector_ShouldNotMatchDisabledElement()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Disabled);
        var selector = new EnabledSelector();

        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void EnabledSelector_ShouldNotMatchChildOfDisabledParent()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);
        parent.SetState(ElementState.Disabled);
        var selector = new EnabledSelector();

        selector.Matches(child).ShouldBeFalse();
    }

    [Fact]
    public void EnabledSelector_ShouldHaveSpecificity10()
    {
        var selector = new EnabledSelector();

        selector.Specificity.ShouldBe(10);
    }

    [Fact]
    public void AllPseudoClassSelectors_ShouldHaveSameSpecificity()
    {
        var selectors = new PseudoClassSelector[]
        {
            new HoverSelector(),
            new ActiveSelector(),
            new FocusSelector(),
            new DisabledSelector(),
            new EnabledSelector()
        };

        foreach (var selector in selectors)
        {
            selector.Specificity.ShouldBe(10);
        }
    }
}
