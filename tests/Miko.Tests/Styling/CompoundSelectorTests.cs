using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

public class CompoundSelectorTests
{
    [Fact]
    public void ForWithHover_ShouldMatchWhenBothMatch()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Hover().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void ForWithHover_ShouldNotMatchWhenHoverMissing()
    {
        var element = new ButtonElement();
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Hover().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void ForWithHover_ShouldNotMatchWhenTagMismatch()
    {
        var element = new DivElement();
        element.SetState(ElementState.Hover);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>().Hover().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void ForWithHover_Specificity_ShouldBeSumOfComponents()
    {
        // button:hover = tag(1) + hover(10) = 11, class = 10 — button:hover wins
        var element = new ButtonElement { Class = "btn" };
        element.SetState(ElementState.Hover);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("btn").Set(x => x.BackgroundColor, Color.Green));
        styleSheet.AddRule(Style.For<ButtonElement>().Hover().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void ClassWithMultiplePseudoClasses_ShouldMatchAll()
    {
        var element = new ButtonElement { Class = "btn" };
        element.SetState(ElementState.Hover);
        element.SetState(ElementState.Active);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("btn").Hover().Active().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void ClassWithMultiplePseudoClasses_ShouldNotMatchWhenOneMissing()
    {
        var element = new ButtonElement { Class = "btn" };
        element.SetState(ElementState.Hover);
        // Active not set
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("btn").Hover().Active().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void IdWithFocus_ShouldMatchFocusedElementById()
    {
        var element = new InputElement { Id = "submit" };
        element.SetState(ElementState.Focus);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Id("submit").Focus().Set(x => x.BackgroundColor, Color.Blue));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Blue);
    }

    [Fact]
    public void WhereWithHover_ShouldMatchByPredicateAndState()
    {
        var element = new ButtonElement { Class = "btn primary" };
        element.SetState(ElementState.Hover);
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<ButtonElement>()
            .Where(x => x.HasClass("btn") && x.HasClass("primary"))
            .Hover()
            .Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }
}
