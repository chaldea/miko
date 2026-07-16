using Miko.Components;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-toast</c>. Covers the DOM contract (host + wrapper + container + content, with
/// NO backdrop), the position class on the wrapper, the header/message/icon renders, the buttons
/// split into start/end groups, the open/closed <c>overlay-hidden</c> gating, the color class, and
/// the dismiss callbacks (a normal button runs its handler then dismisses; a cancel button dismisses
/// without running; both raise <c>OnDidDismiss</c> with the role).
/// </summary>
public class IonToastTests : IonicComponentTestBase
{
    private static IReadOnlyList<IonToastButton> Buttons() => new List<IonToastButton>
    {
        new() { Text = "Undo", Side = "start", Handler = null },
        new() { Text = "Close", Role = "cancel" },
    };

    private static ComponentUnderTest RenderToast(TestContext ctx,
        Action<ComponentParameterBuilder<IonToast>>? configure = null)
        => ctx.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), true);
            p.Add(nameof(IonToast.Message), "Saved.");
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonToast_RendersOverlayContract()
    {
        var cut = RenderToast(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-toast");
        cut.FindByClass("toast-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("toast-container").ShouldHaveSingleItem();
        cut.FindByClass("toast-content").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonToast_HasNoBackdrop()
    {
        // A toast is a non-blocking notification: no backdrop, unlike action-sheet/alert/loading.
        var cut = RenderToast(Context);

        cut.FindByClass("ion-backdrop").ShouldBeEmpty();
        cut.FindByClass("toast-backdrop").ShouldBeEmpty();
    }

    [Fact]
    public void IonToast_WrapperCarriesPositionClass_Default()
    {
        var cut = RenderToast(Context);

        var wrapper = cut.FindByClass("toast-wrapper").ShouldHaveSingleItem();
        wrapper.ShouldHaveClass("toast-bottom");
        wrapper.ShouldHaveClass("ion-overlay-wrapper");
    }

    [Theory]
    [InlineData("top", "toast-top")]
    [InlineData("middle", "toast-middle")]
    [InlineData("bottom", "toast-bottom")]
    public void IonToast_WrapperCarriesPositionClass(string position, string expectedClass)
    {
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Position), position));

        cut.FindByClass("toast-wrapper").ShouldHaveSingleItem().ShouldHaveClass(expectedClass);
    }

    [Fact]
    public void IonToast_RendersHeaderAndMessage()
    {
        var cut = RenderToast(Context, p =>
        {
            p.Add(nameof(IonToast.Header), "Success");
            p.Add(nameof(IonToast.Message), "Your changes were saved.");
        });

        cut.FindByClass("toast-header").ShouldHaveSingleItem();
        cut.FindByClass("toast-message").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Success");
        cut.GetTextContent().ShouldContain("Your changes were saved.");
    }

    [Fact]
    public void IonToast_NoHeader_OmitsHeader()
    {
        var cut = RenderToast(Context);

        cut.FindByClass("toast-header").ShouldBeEmpty();
    }

    [Fact]
    public void IonToast_RendersIcon()
    {
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Icon), "information-circle"));

        cut.FindByClass("toast-icon").ShouldNotBeEmpty();
    }

    // ---- Buttons split by side --------------------------------------------

    [Fact]
    public void IonToast_SplitsButtonsIntoStartAndEndGroups()
    {
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Buttons), Buttons()));

        cut.FindByClass("toast-button-group-start").ShouldHaveSingleItem();
        cut.FindByClass("toast-button-group-end").ShouldHaveSingleItem();
        cut.FindByClass("toast-button").Count.ShouldBe(2);
    }

    [Fact]
    public void IonToast_CancelButton_StampsRoleClass()
    {
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Buttons), Buttons()));

        cut.FindByClass("toast-button").ShouldContain(b => b.HasClass("toast-button-cancel"));
    }

    [Fact]
    public void IonToast_IconOnlyButton_StampsIconOnlyClass()
    {
        var buttons = (IReadOnlyList<IonToastButton>)new List<IonToastButton>
        {
            new() { Icon = "close", Role = "cancel" },
        };
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Buttons), buttons));

        cut.FindByClass("toast-button").ShouldHaveSingleItem().ShouldHaveClass("toast-button-icon-only");
    }

    // ---- Open / closed gating ---------------------------------------------

    [Fact]
    public void IonToast_Closed_StampsOverlayHidden()
    {
        var cut = Context.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), false);
            p.Add(nameof(IonToast.Message), "Hidden");
        });

        cut.Root.ShouldHaveClass("overlay-hidden");
    }

    [Fact]
    public void IonToast_Open_DoesNotStampOverlayHidden()
    {
        var cut = RenderToast(Context);

        cut.Root.ShouldNotHaveClass("overlay-hidden");
    }

    // ---- Color -------------------------------------------------------------

    [Fact]
    public void IonToast_Color_StampsColorClass()
    {
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Color), "success"));

        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-success");
    }

    // ---- Dismiss interaction ----------------------------------------------

    [Fact]
    public async Task IonToast_ButtonTap_RunsHandlerAndDismisses()
    {
        var ran = false;
        IonOverlayDismissEventArgs? dismissed = null;
        var button = new IonToastButton { Text = "Undo", Role = "undo", Handler = () => ran = true };
        var toast = new IonToast
        {
            IsOpen = true,
            Buttons = new List<IonToastButton> { button },
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => dismissed = e),
        };
        toast.Build();

        await Invoke(toast, "ButtonClickAsync", button);

        ran.ShouldBeTrue();
        dismissed.ShouldNotBeNull();
        dismissed!.Role.ShouldBe("undo");
    }

    [Fact]
    public async Task IonToast_CancelButton_DismissesWithoutRunningHandler()
    {
        var ran = false;
        var closed = false;
        IonOverlayDismissEventArgs? dismissed = null;
        var button = new IonToastButton { Text = "Close", Role = "cancel", Handler = () => ran = true };
        var toast = new IonToast
        {
            IsOpen = true,
            Buttons = new List<IonToastButton> { button },
            IsOpenChanged = EventCallback.Factory.Create<bool>(this, v => closed = !v),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => dismissed = e),
        };
        toast.Build();

        await Invoke(toast, "ButtonClickAsync", button);

        ran.ShouldBeFalse();
        closed.ShouldBeTrue();
        dismissed!.Role.ShouldBe("cancel");
        dismissed!.IsCancel.ShouldBeTrue();
    }

    // ---- Mode --------------------------------------------------------------

    [Fact]
    public void IonToast_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderToast(Context);

        cut.Root.Class.ShouldStartWith("ios ion-toast");
    }

    // Invokes a private async handler on the component (mirrors what a click/tap dispatches).
    private static async Task Invoke(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)mi.Invoke(component, new[] { arg })!;
    }
}
