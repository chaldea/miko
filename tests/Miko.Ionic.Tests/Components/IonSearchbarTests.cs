using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-searchbar</c>. Covers the DOM contract (host → .searchbar-input-container →
/// input / search-icon / clear-button, plus the mode-specific cancel-button placement), the
/// value/disabled/animated/should-show/cancel/clear state class stamping, the input wiring done in
/// <c>Build()</c>, key theme-driven styles, and the cancel-button content per mode.
/// </summary>
public class IonSearchbarTests : IonicComponentTestBase
{
    private static ComponentUnderTest RenderBar(TestContext ctx,
        Action<ComponentParameterBuilder<IonSearchbar>>? configure = null)
        => ctx.Render<IonSearchbar>(p => configure?.Invoke(p));

    // --- DOM structure --------------------------------------------------------------------

    [Fact]
    public void IonSearchbar_HasCorrectDOMStructure()
    {
        // DOM contract: host <div> → .searchbar-input-container → input.searchbar-input +
        // .searchbar-search-icon + .searchbar-clear-button (with .searchbar-clear-icon inside).
        var cut = RenderBar(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("ion-searchbar");

        var container = cut.Root.Children[0];
        container.Class.ShouldBe("searchbar-input-container");

        var input = cut.FindByClass("searchbar-input").Single();
        input.TagName.ShouldBe("input");
        input.ShouldBeOfType<InputElement>();
    }

    [Fact]
    public void IonSearchbar_RendersSearchAndClearIcons()
    {
        var cut = RenderBar(Context);

        // Search icon + clear button (wrapping the clear icon) are always present.
        cut.FindByClass("searchbar-search-icon").ShouldHaveSingleItem();
        var clearButton = cut.FindByClass("searchbar-clear-button").Single();
        clearButton.TagName.ShouldBe("button");
        // The clear icon is an IonIcon stamped with the searchbar-clear-icon class.
        clearButton.FindByClass("searchbar-clear-icon").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonSearchbar_Md_PlacesCancelButtonInsideContainer()
    {
        // md: the cancel button (an icon) is a child of the input-container.
        var cut = RenderBar(Context, p => p.Add(nameof(IonSearchbar.ShowCancelButton), "always"));

        var container = cut.Root.Children[0];
        var cancel = cut.FindByClass("searchbar-cancel-button").Single();
        container.Children.ShouldContain(cancel);
    }

    [Fact]
    public void IonSearchbar_Ios_PlacesCancelButtonAsSiblingOfContainer()
    {
        // ios: the cancel button (text) is a sibling of the input-container, not inside it.
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        var cut = RenderBar(Context, p => p.Add(nameof(IonSearchbar.ShowCancelButton), "always"));

        var container = cut.Root.Children[0];
        var cancel = cut.FindByClass("searchbar-cancel-button").Single();
        container.Children.ShouldNotContain(cancel);
        cut.Root.Children.ShouldContain(cancel);
    }

    // --- State classes --------------------------------------------------------------------

    [Fact]
    public void IonSearchbar_DefaultsToLeftAligned_WithoutValue()
    {
        var cut = RenderBar(Context);
        // shouldAlignLeft starts true; with no value there is no has-value class (the clear button
        // stays hidden because the CSS reveal rule keys on has-value + should-show-clear).
        cut.Root.ShouldHaveClass("searchbar-left-aligned");
        cut.Root.ShouldNotHaveClass("searchbar-has-value");
    }

    [Fact]
    public void IonSearchbar_StampsHasValue_WhenValueSet()
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonSearchbar.Value), "hello"));
        cut.Root.ShouldHaveClass("searchbar-has-value");
        // default showClearButton="always" → should-show-clear is stamped.
        cut.Root.ShouldHaveClass("searchbar-should-show-clear");
    }

    [Fact]
    public void IonSearchbar_OmitsShouldShowClear_WhenShowClearButtonNever()
    {
        var cut = RenderBar(Context, p =>
        {
            p.Add(nameof(IonSearchbar.Value), "hello");
            p.Add(nameof(IonSearchbar.ShowClearButton), "never");
        });
        cut.Root.ShouldHaveClass("searchbar-has-value");
        cut.Root.ShouldNotHaveClass("searchbar-should-show-clear");
    }

    [Fact]
    public void IonSearchbar_StampsDisabledClass()
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonSearchbar.Disabled), true));
        cut.Root.ShouldHaveClass("searchbar-disabled");
    }

    [Fact]
    public void IonSearchbar_StampsAnimatedClass()
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonSearchbar.Animated), true));
        cut.Root.ShouldHaveClass("searchbar-animated");
    }

    [Theory]
    [InlineData("always", true)]
    [InlineData("never", false)]
    public void IonSearchbar_StampsShouldShowCancel_PerShowCancelButton(string mode, bool expected)
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonSearchbar.ShowCancelButton), mode));
        if (expected)
            cut.Root.ShouldHaveClass("searchbar-should-show-cancel");
        else
            cut.Root.ShouldNotHaveClass("searchbar-should-show-cancel");
    }

    [Fact]
    public void IonSearchbar_StampsColorClass()
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonSearchbar.Color), "danger"));
        cut.Root.ShouldHaveClass("ion-color-danger");
    }

    [Fact]
    public void IonSearchbar_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        var cut = RenderBar(Context);
        cut.Root.Class.ShouldStartWith("ios ion-searchbar");
    }

    // --- Input wiring (Build) -------------------------------------------------------------

    [Fact]
    public void IonSearchbar_StampsValueAndPlaceholderOnInput()
    {
        var cut = RenderBar(Context, p =>
        {
            p.Add(nameof(IonSearchbar.Value), "find me");
            p.Add(nameof(IonSearchbar.Placeholder), "Custom Placeholder");
        });

        var input = (InputElement)cut.FindByClass("searchbar-input").Single();
        input.Value.ShouldBe("find me");
        input.Placeholder.ShouldBe("Custom Placeholder");
    }

    [Fact]
    public void IonSearchbar_StampsSearchInputType()
    {
        var cut = RenderBar(Context);
        var input = (InputElement)cut.FindByClass("searchbar-input").Single();
        input.Type.ShouldBe(InputType.Search);
    }

    [Fact]
    public void IonSearchbar_StampsDisabledStateOnInput()
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonSearchbar.Disabled), true));
        var input = (InputElement)cut.FindByClass("searchbar-input").Single();
        input.IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void IonSearchbar_DefaultPlaceholderIsSearch()
    {
        var cut = RenderBar(Context);
        var input = (InputElement)cut.FindByClass("searchbar-input").Single();
        input.Placeholder.ShouldBe("Search");
    }

    // --- Cancel button content ------------------------------------------------------------

    [Fact]
    public void IonSearchbar_Md_CancelButtonRendersIcon()
    {
        var cut = RenderBar(Context, p =>
        {
            p.Add(nameof(IonSearchbar.ShowCancelButton), "always");
            p.Add(nameof(IonSearchbar.CancelButtonIcon), "arrow-back");
        });

        var cancel = cut.FindByClass("searchbar-cancel-button").Single();
        // md cancel button wraps an IonIcon carrying the arrow-back icon.
        cancel.FindByClass("ion-icon").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldNotContain("Cancel");
    }

    [Fact]
    public void IonSearchbar_Ios_CancelButtonRendersCancelText()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        var cut = RenderBar(Context, p =>
        {
            p.Add(nameof(IonSearchbar.ShowCancelButton), "always");
            p.Add(nameof(IonSearchbar.CancelButtonText), "Done");
        });

        var cancel = cut.FindByClass("searchbar-cancel-button").Single();
        // ios cancel button renders the text, not an icon.
        cancel.FindByClass("ion-icon").Count.ShouldBe(0);
        cut.GetTextContent().ShouldContain("Done");
    }

    // --- Key style assertions (theme-driven) ----------------------------------------------

    [Fact]
    public void IonSearchbar_Md_InputHasSmallRadiusAndBoxShadow()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = RenderBar(Context);
        var input = cut.FindByClass("searchbar-input").Single();
        var style = cut.GetComputedStyle(input)!;

        // md input: 2px radius and a 3-layer elevation shadow.
        style.BorderTopLeftRadius.Value.ShouldBe(2f);
        style.BoxShadow.ShouldNotBeNull();
        style.BoxShadow!.Value.Value.Count.ShouldBe(3);
    }

    [Fact]
    public void IonSearchbar_Ios_InputHasLargerRadius_NoShadow()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        var cut = RenderBar(Context);
        var input = cut.FindByClass("searchbar-input").Single();
        var style = cut.GetComputedStyle(input)!;

        // ios input: 10px radius, translucent fill, no shadow.
        style.BorderTopLeftRadius.Value.ShouldBe(10f);
        (style.BoxShadow == null || style.BoxShadow.Value.Value.Count == 0).ShouldBeTrue();
    }

    [Fact]
    public void IonSearchbar_DisabledHost_IsDimmed()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = RenderBar(Context, p => p.Add(nameof(IonSearchbar.Disabled), true));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Opacity.ShouldBe(0.4f);
    }

    [Fact]
    public void IonSearchbar_ClearButton_HiddenByDefault_ShownWithValue()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        // No value → the host lacks searchbar-has-value, so the
        // :host(.has-value.should-show-clear) .clear-button reveal rule does not match and the
        // button keeps its base display:none (and is therefore absent from the layout tree).
        var empty = RenderBar(Context);
        empty.Root.ShouldNotHaveClass("searchbar-has-value");

        // With a value (default showClearButton="always") the host carries both has-value and
        // should-show-clear, so the clear button is laid out as display:block.
        var filled = RenderBar(Context, p => p.Add(nameof(IonSearchbar.Value), "x"));
        filled.Root.ShouldHaveClass("searchbar-has-value");
        filled.Root.ShouldHaveClass("searchbar-should-show-clear");
        var filledClear = cut_ClearStyle(filled);
        filledClear.Display.ShouldBe(Display.Block);
    }

    [Fact]
    public void IonSearchbar_CancelButton_HiddenWhenShowCancelNever()
    {
        // default showCancelButton="never" → the host carries neither should-show-cancel nor
        // has-focus, so the cancel button keeps its base display:none (excluded from the layout
        // tree). Asserting the driving host state rather than the (unlaid-out) computed display.
        var cut = RenderBar(Context);
        cut.Root.ShouldNotHaveClass("searchbar-should-show-cancel");
        cut.Root.ShouldNotHaveClass("searchbar-has-focus");
    }

    [Fact]
    public void IonSearchbar_ColoredInput_UsesColorBackground()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = RenderBar(Context, p => p.Add(nameof(IonSearchbar.Color), "danger"));
        var input = cut.FindByClass("searchbar-input").Single();
        var style = cut.GetComputedStyle(input)!;

        // ion-color-danger fills the input with the danger base color.
        style.BackgroundColor.ShouldBe(Color.FromHex("c5000f"));
    }

    private static Miko.Styling.ComputedStyle cut_ClearStyle(ComponentUnderTest cut)
    {
        var clear = cut.FindByClass("searchbar-clear-button").Single();
        return cut.GetComputedStyle(clear)!;
    }
}
