using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Miko.Events;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonItemOptionTests : IonicComponentTestBase
{
    [Fact]
    public void IonItemOption_RendersAsButton_WithInner()
    {
        var cut = Context.Render<IonItemOption>();

        // DOM contract: a <button> host wrapping a .button-inner span.
        cut.Root.TagName.ShouldBe("button");
        cut.Root.Class.ShouldBe("md ion-item-option");
        cut.Root.FindByClass("button-inner").Count.ShouldBe(1);
    }

    [Fact]
    public void IonItemOption_StampsColorClass_WhenColorProvided()
    {
        var cut = Context.Render<IonItemOption>(parameters =>
            parameters.Add(nameof(IonItemOption.Color), "danger"));

        cut.Root.Class.ShouldBe("md ion-item-option ion-color-danger");
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
        // Interaction assertion: tapping the option fires its OnClick callback.
        var clicked = false;
        var cut = Context.Render<IonItemOption>(parameters =>
            parameters.Add(nameof(IonItemOption.OnClick),
                EventCallback.Factory.Create(this, () => clicked = true)));

        cut.Root.OnClick!.Invoke(new MouseEventArgs { Target = cut.Root });

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

        cut.Root.OnClick!.Invoke(new MouseEventArgs { Target = cut.Root });

        clicked.ShouldBeFalse();
    }
}

