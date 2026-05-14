using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

public class PseudoClassSelectorsTests
{
    [Fact]
    public void Hover_ShouldMatchHoveredElement()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Hover().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Hover_ShouldNotMatchNonHoveredElement()
    {
        var element = new ButtonElement();
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Hover().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void Active_ShouldMatchActiveElement()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Active);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Active().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Active_ShouldNotMatchInactiveElement()
    {
        var element = new ButtonElement();
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Active().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void Focus_ShouldMatchFocusedElement()
    {
        var element = new InputElement();
        element.SetState(ElementState.Focus);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<InputElement>().Focus().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Focus_ShouldNotMatchUnfocusedElement()
    {
        var element = new InputElement();
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<InputElement>().Focus().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void Disabled_ShouldMatchDisabledElement()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Disabled);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Disabled().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Disabled_ShouldNotMatchEnabledElement()
    {
        var element = new ButtonElement();
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Disabled().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void Disabled_ShouldMatchChildOfDisabledParent()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);
        parent.SetState(ElementState.Disabled);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Disabled().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(child, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Enabled_ShouldMatchEnabledElement()
    {
        var element = new ButtonElement();
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Enabled().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Enabled_ShouldNotMatchDisabledElement()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Disabled);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Enabled().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void Enabled_ShouldNotMatchChildOfDisabledParent()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);
        parent.SetState(ElementState.Disabled);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Enabled().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(child, [styleSheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void PseudoClass_Specificity_ShouldBeAddedToBaseSelector()
    {
        // button:hover has specificity 11 (tag=1 + hover=10), class has 10 — button:hover wins
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("btn").Set(x => x.BackgroundColor, Color.Green));
        styleSheet.AddRule(Style.For<ButtonElement>().Hover().Set(x => x.BackgroundColor, Color.Red));
        element.Class = "btn";

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }
}
