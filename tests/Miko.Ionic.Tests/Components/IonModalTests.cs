using Miko.Components;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-modal</c>. Covers the overlay DOM contract (backdrop + shadow + wrapper; the
/// child content rendering inside the wrapper), the open/closed <c>overlay-hidden</c> gating, the
/// ShowBackdrop toggle, the backdrop-tap dismiss (respecting BackdropDismiss), the will/did dismiss
/// callbacks, and the per-mode (md / ios) class.
/// </summary>
public class IonModalTests : IonicComponentTestBase
{
    private static readonly RenderFragment Body = builder => builder.AddContent(0, "Modal body");

    private static ComponentUnderTest RenderModal(TestContext ctx,
        Action<ComponentParameterBuilder<IonModal>>? configure = null)
        => ctx.Render<IonModal>(p =>
        {
            p.Add(nameof(IonModal.IsOpen), true);
            p.Add(nameof(IonModal.ChildContent), Body);
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonModal_RendersOverlayContract()
    {
        var cut = RenderModal(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-modal");
        cut.Root.ShouldHaveClass("modal-default");
        cut.FindByClass("modal-backdrop").ShouldHaveSingleItem();
        cut.FindByClass("modal-wrapper").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonModal_RendersChildContentInsideWrapper()
    {
        var cut = RenderModal(Context);

        var wrapper = cut.FindByClass("modal-wrapper").ShouldHaveSingleItem();
        // ChildContent renders as a direct text-node child of the wrapper (not the backdrop/shadow).
        wrapper.TextContent.ShouldNotBeNull();
        wrapper.TextContent!.ShouldContain("Modal body");
    }

    [Fact]
    public void IonModal_WrapperCarriesOverlayWrapperClass()
    {
        var cut = RenderModal(Context);

        cut.FindByClass("modal-wrapper").ShouldHaveSingleItem()
            .ShouldHaveClass("ion-overlay-wrapper");
    }

    // ---- ShowBackdrop ------------------------------------------------------

    [Fact]
    public void IonModal_ShowBackdropFalse_OmitsBackdrop()
    {
        var cut = RenderModal(Context, p => p.Add(nameof(IonModal.ShowBackdrop), false));

        cut.FindByClass("modal-backdrop").ShouldBeEmpty();
    }

    [Fact]
    public void IonModal_ShowBackdropTrue_KeepsBackdrop()
    {
        var cut = RenderModal(Context, p => p.Add(nameof(IonModal.ShowBackdrop), true));

        cut.FindByClass("modal-backdrop").ShouldHaveSingleItem();
    }

    // ---- Open / closed gating ---------------------------------------------

    [Fact]
    public void IonModal_Closed_StampsOverlayHidden()
    {
        var cut = Context.Render<IonModal>(p =>
        {
            p.Add(nameof(IonModal.IsOpen), false);
            p.Add(nameof(IonModal.ChildContent), Body);
        });

        cut.Root.ShouldHaveClass("overlay-hidden");
    }

    [Fact]
    public void IonModal_Open_DoesNotStampOverlayHidden()
    {
        var cut = RenderModal(Context);

        cut.Root.ShouldNotHaveClass("overlay-hidden");
    }

    // ---- Sheet marker ------------------------------------------------------

    [Fact]
    public void IonModal_SheetBreakpoints_StampsModalSheet()
    {
        var cut = RenderModal(Context, p =>
        {
            p.Add(nameof(IonModal.Breakpoints), new[] { 0.5, 1.0 });
            p.Add(nameof(IonModal.InitialBreakpoint), 0.5);
        });

        cut.Root.ShouldHaveClass("modal-sheet");
        cut.Root.ShouldNotHaveClass("modal-default");
    }

    // ---- Dismiss interaction ----------------------------------------------

    [Fact]
    public async Task IonModal_BackdropTap_Dismisses_WhenBackdropDismissEnabled()
    {
        var closed = false;
        IonOverlayDismissEventArgs? willDismiss = null;
        IonOverlayDismissEventArgs? didDismiss = null;
        var modal = new IonModal
        {
            IsOpen = true,
            BackdropDismiss = true,
            IsOpenChanged = EventCallback.Factory.Create<bool>(this, v => closed = !v),
            OnWillDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => willDismiss = e),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => didDismiss = e),
        };
        modal.Build();

        await Invoke(modal, "OnBackdropTapAsync", new MouseEventArgs());

        closed.ShouldBeTrue();
        willDismiss.ShouldNotBeNull();
        didDismiss.ShouldNotBeNull();
        didDismiss!.Role.ShouldBe("backdrop");
    }

    [Fact]
    public async Task IonModal_BackdropTap_IsNoOp_WhenBackdropDismissDisabled()
    {
        var invoked = false;
        var modal = new IonModal
        {
            IsOpen = true,
            BackdropDismiss = false,
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, _ => invoked = true),
        };
        modal.Build();

        await Invoke(modal, "OnBackdropTapAsync", new MouseEventArgs());

        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task IonModal_DismissAsync_RaisesWillThenDidDismiss()
    {
        var order = new List<string>();
        var modal = new IonModal
        {
            IsOpen = true,
            OnWillDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, _ => order.Add("will")),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, _ => order.Add("did")),
        };
        modal.Build();

        await modal.DismissAsync("confirm", "payload");

        order.ShouldBe(new[] { "will", "did" });
    }

    // ---- Mode --------------------------------------------------------------

    [Fact]
    public void IonModal_UsesIosClass_OnIosPlatform_AndRendersShadow()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderModal(Context);

        cut.Root.Class.ShouldStartWith("ios ion-modal");
        cut.FindByClass("modal-shadow").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonModal_OmitsShadow_OnMdPlatform()
    {
        var cut = RenderModal(Context);

        cut.FindByClass("modal-shadow").ShouldBeEmpty();
    }

    // Invokes a private async handler on the component (mirrors what a click/tap dispatches).
    private static async Task Invoke(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)mi.Invoke(component, new[] { arg })!;
    }
}
