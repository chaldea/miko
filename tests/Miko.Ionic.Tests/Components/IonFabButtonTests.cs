using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-fab-button</c>. Covers the DOM contract (host → .button-native →
/// .close-icon + .button-inner), the color/size/state class stamping, click interaction, and a few
/// theme-driven styles (the round surface, the 56px/40px box).
/// </summary>
public class IonFabButtonTests : IonicComponentTestBase
{
    private static readonly RenderFragment Icon = builder =>
    {
        builder.OpenComponent<IonIcon>(0);
        builder.AddComponentParameter(1, nameof(IonIcon.Icon), "add");
        builder.CloseComponent();
    };

    private static ComponentUnderTest RenderButton(TestContext ctx,
        Action<ComponentParameterBuilder<IonFabButton>>? configure = null)
        => ctx.Render<IonFabButton>(p =>
        {
            p.Add(nameof(IonFabButton.ChildContent), Icon);
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonFabButton_RendersDomContract()
    {
        var cut = RenderButton(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-fab-button");

        var native = cut.Root.Children[0];
        native.TagName.ShouldBe("button");
        native.Class.ShouldBe("button-native");

        // The close icon and the inner content are siblings inside the native button.
        native.FindByClass("close-icon").ShouldHaveSingleItem();
        native.FindByClass("button-inner").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonFabButton_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderButton(Context);

        cut.Root.Class.ShouldStartWith("ios ion-fab-button");
    }

    // ---- Class stamping ----------------------------------------------------

    [Fact]
    public void IonFabButton_StampsColorClasses()
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonFabButton.Color), "danger"));

        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-danger");
    }

    [Fact]
    public void IonFabButton_Small_StampsSmallClass()
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonFabButton.Size), "small"));

        cut.Root.ShouldHaveClass("fab-button-small");
    }

    [Fact]
    public void IonFabButton_Disabled_StampsDisabledClass()
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonFabButton.Disabled), true));

        cut.Root.ShouldHaveClass("fab-button-disabled");
    }

    [Fact]
    public void IonFabButton_Activated_StampsCloseActiveClass()
    {
        // A standalone button (no fab context) with Activated set directly gets the close-active class.
        var cut = RenderButton(Context, p => p.Add(nameof(IonFabButton.Activated), true));

        cut.Root.ShouldHaveClass("fab-button-close-active");
    }

    // ---- Interaction -------------------------------------------------------

    [Fact]
    public void IonFabButton_Click_InvokesOnClick()
    {
        var clicked = false;
        var cut = RenderButton(Context, p =>
            p.Add(nameof(IonFabButton.OnClick),
                EventCallback.Factory.Create(this, () => clicked = true)));

        var native = cut.FindByClass("button-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        clicked.ShouldBeTrue();
    }

    [Fact]
    public void IonFabButton_Click_WhenDisabled_DoesNotInvokeOnClick()
    {
        var clicked = false;
        var cut = RenderButton(Context, p =>
        {
            p.Add(nameof(IonFabButton.Disabled), true);
            p.Add(nameof(IonFabButton.OnClick),
                EventCallback.Factory.Create(this, () => clicked = true));
        });

        var native = cut.FindByClass("button-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        clicked.ShouldBeFalse();
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonFabButton_Style_NativeSurfaceIsRound()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context);
        var native = cut.FindByClass("button-native").Single();
        var style = cut.GetComputedStyle(native)!;

        // border-radius: 50% → a circle.
        style.BorderTopLeftRadius.ShouldBe(Length.Percent(50));
    }

    [Fact]
    public void IonFabButton_Style_DefaultBoxIs56px()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context);
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Width.ShouldBe(Length.Px(56));
        style.Height.ShouldBe(Length.Px(56));
    }

    [Fact]
    public void IonFabButton_Style_SmallBoxIs40px()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context, p => p.Add(nameof(IonFabButton.Size), "small"));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Width.ShouldBe(Length.Px(40));
        style.Height.ShouldBe(Length.Px(40));
    }
}
