using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Miko.Events;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonItemOptionTests : IonicComponentTestBase
{
    [Fact]
    public void IonItemOption_RendersHostWithNativeButtonStructure()
    {
        var cut = Context.Render<IonItemOption>();

        // DOM contract: a div host wrapping a native <button> > .button-inner > .horizontal-wrapper.
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-item-option ion-activatable");

        var native = cut.FindByClass("button-native").Single();
        native.TagName.ShouldBe("button");
        cut.FindByClass("button-inner").Count.ShouldBe(1);
        cut.FindByClass("horizontal-wrapper").Count.ShouldBe(1);
    }

    [Fact]
    public void IonItemOption_RendersAnchorNative_WhenHref()
    {
        var cut = Context.Render<IonItemOption>(parameters =>
            parameters.Add(nameof(IonItemOption.Href), "/archive"));

        var native = cut.FindByClass("button-native").Single();
        native.TagName.ShouldBe("a");
    }

    [Fact]
    public void IonItemOption_StampsExpandableClass_WhenExpandable()
    {
        var cut = Context.Render<IonItemOption>(parameters =>
            parameters.Add(nameof(IonItemOption.Expandable), true));

        cut.Root.ShouldHaveClass("item-option-expandable");
    }

    [Fact]
    public void IonItemOption_StampsColorClass_WhenColorProvided()
    {
        var cut = Context.Render<IonItemOption>(parameters =>
            parameters.Add(nameof(IonItemOption.Color), "danger"));

        cut.Root.ShouldHaveClass("ion-color-danger");
    }

    [Fact]
    public void IonItemOption_StampsDisabledClass_WhenDisabled()
    {
        var cut = Context.Render<IonItemOption>(parameters =>
            parameters.Add(nameof(IonItemOption.Disabled), true));

        cut.Root.ShouldHaveClass("item-option-disabled");
    }

    [Fact]
    public void IonItemOption_RendersChildContent()
    {
        var cut = Context.Render<IonItemOption>(parameters =>
            parameters.Add(nameof(IonItemOption.ChildContent), (RenderFragment)(builder =>
            {
                builder.AddContent(0, "Delete");
            })));

        cut.GetTextContent().ShouldContain("Delete");
    }

    [Fact]
    public void IonItemOption_DangerColor_ResolvesToDangerBackground()
    {
        // Key style assertion: the danger variant maps to the theme's danger fill.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItemOption>(parameters =>
            parameters.Add(nameof(IonItemOption.Color), "danger"));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.BackgroundColor.ShouldBe(Miko.Common.Color.FromHex("c5000f"));
    }

    [Fact]
    public void IonItemOption_DefaultColor_ResolvesToPrimaryBackground()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItemOption>();

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        // Default option fill is the primary brand color.
        style.BackgroundColor.ShouldBe(Miko.Common.Color.FromHex("0054e9"));
    }

    [Fact]
    public void IonItemOption_InvokesOnClick_WhenTapped()
    {
        // Interaction assertion: tapping the option fires its OnClick callback. The click handler
        // sits on the native surface (the host is a plain div).
        var clicked = false;
        var cut = Context.Render<IonItemOption>(parameters =>
            parameters.Add(nameof(IonItemOption.OnClick),
                EventCallback.Factory.Create(this, () => clicked = true)));

        var native = cut.FindByClass("button-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        clicked.ShouldBeTrue();
    }

    [Fact]
    public void IonItemOption_DoesNotInvokeOnClick_WhenDisabled()
    {
        var clicked = false;
        var cut = Context.Render<IonItemOption>(parameters =>
        {
            parameters.Add(nameof(IonItemOption.Disabled), true);
            parameters.Add(nameof(IonItemOption.OnClick),
                EventCallback.Factory.Create(this, () => clicked = true));
        });

        var native = cut.FindByClass("button-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        clicked.ShouldBeFalse();
    }
}
