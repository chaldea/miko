using Miko.Components;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-loading</c>. Covers the overlay DOM contract (host + backdrop + wrapper +
/// spinner + content), the open/closed <c>overlay-hidden</c> gating, the per-mode spinner default,
/// the message render, the show-backdrop toggle, and the dismiss behavior (backdrop tap respecting
/// the <c>BackdropDismiss</c> default of false, and <c>OnDidDismiss</c> being raised).
/// </summary>
public class IonLoadingTests : IonicComponentTestBase
{
    private static ComponentUnderTest RenderLoading(TestContext ctx,
        Action<ComponentParameterBuilder<IonLoading>>? configure = null)
        => ctx.Render<IonLoading>(p =>
        {
            p.Add(nameof(IonLoading.IsOpen), true);
            p.Add(nameof(IonLoading.Message), "Loading...");
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonLoading_RendersOverlayContract()
    {
        var cut = RenderLoading(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-loading");
        cut.FindByClass("loading-backdrop").ShouldHaveSingleItem();
        cut.FindByClass("loading-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("loading-spinner").ShouldHaveSingleItem();
        cut.FindByClass("loading-content").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonLoading_WrapperCarriesOverlayWrapperClass()
    {
        var cut = RenderLoading(Context);

        var wrapper = cut.FindByClass("loading-wrapper").ShouldHaveSingleItem();
        wrapper.ShouldHaveClass("ion-overlay-wrapper");
    }

    [Fact]
    public void IonLoading_RendersMessage()
    {
        var cut = RenderLoading(Context, p => p.Add(nameof(IonLoading.Message), "Please wait"));

        cut.GetTextContent().ShouldContain("Please wait");
    }

    [Fact]
    public void IonLoading_NoMessage_OmitsContent()
    {
        var cut = Context.Render<IonLoading>(p => p.Add(nameof(IonLoading.IsOpen), true));

        cut.FindByClass("loading-content").ShouldBeEmpty();
    }

    [Fact]
    public void IonLoading_EmptySpinner_OmitsSpinner()
    {
        var cut = RenderLoading(Context, p => p.Add(nameof(IonLoading.Spinner), ""));

        cut.FindByClass("loading-spinner").ShouldBeEmpty();
    }

    // ---- Spinner default per mode -----------------------------------------

    [Fact]
    public void IonLoading_MdSpinner_DefaultsToCrescent()
    {
        var cut = RenderLoading(Context);

        // The nested IonSpinner stamps spinner-{name}; md defaults to crescent.
        cut.FindByClass("spinner-crescent").ShouldNotBeEmpty();
    }

    [Fact]
    public void IonLoading_IosSpinner_DefaultsToLines()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderLoading(Context);

        cut.FindByClass("spinner-lines").ShouldNotBeEmpty();
    }

    [Fact]
    public void IonLoading_ExplicitSpinner_IsUsed()
    {
        var cut = RenderLoading(Context, p => p.Add(nameof(IonLoading.Spinner), "dots"));

        cut.FindByClass("spinner-dots").ShouldNotBeEmpty();
    }

    // ---- Backdrop ----------------------------------------------------------

    [Fact]
    public void IonLoading_ShowBackdropFalse_OmitsBackdrop()
    {
        var cut = RenderLoading(Context, p => p.Add(nameof(IonLoading.ShowBackdrop), false));

        cut.FindByClass("loading-backdrop").ShouldBeEmpty();
    }

    // ---- Open / closed gating ---------------------------------------------

    [Fact]
    public void IonLoading_Closed_StampsOverlayHidden()
    {
        var cut = Context.Render<IonLoading>(p => p.Add(nameof(IonLoading.IsOpen), false));

        cut.Root.ShouldHaveClass("overlay-hidden");
    }

    [Fact]
    public void IonLoading_Open_DoesNotStampOverlayHidden()
    {
        var cut = RenderLoading(Context);

        cut.Root.ShouldNotHaveClass("overlay-hidden");
    }

    // ---- Dismiss interaction ----------------------------------------------

    [Fact]
    public async Task IonLoading_BackdropTap_IsNoOp_ByDefault()
    {
        // Loading blocks input: BackdropDismiss defaults to false, so a tap does nothing.
        var invoked = false;
        var loading = new IonLoading
        {
            IsOpen = true,
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, _ => invoked = true),
        };
        loading.Build();

        await Invoke(loading, "OnBackdropTapAsync", new MouseEventArgs());

        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task IonLoading_BackdropTap_Dismisses_WhenEnabled()
    {
        var closed = false;
        IonOverlayDismissEventArgs? dismissed = null;
        var loading = new IonLoading
        {
            IsOpen = true,
            BackdropDismiss = true,
            IsOpenChanged = EventCallback.Factory.Create<bool>(this, v => closed = !v),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => dismissed = e),
        };
        loading.Build();

        await Invoke(loading, "OnBackdropTapAsync", new MouseEventArgs());

        closed.ShouldBeTrue();
        dismissed.ShouldNotBeNull();
        dismissed!.Role.ShouldBe("backdrop");
        dismissed!.IsCancel.ShouldBeTrue();
    }

    // ---- Mode --------------------------------------------------------------

    [Fact]
    public void IonLoading_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderLoading(Context);

        cut.Root.Class.ShouldStartWith("ios ion-loading");
    }

    // Invokes a private async handler on the component (mirrors what a click/tap dispatches).
    private static async Task Invoke(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)mi.Invoke(component, new[] { arg })!;
    }
}
