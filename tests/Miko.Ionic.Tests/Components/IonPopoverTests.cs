using Miko.Components;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-popover</c>. Covers the overlay DOM contract (backdrop + wrapper + arrow +
/// content; the child content rendering inside <c>.popover-content</c>), the arrow present/absent by
/// <c>Arrow</c>, the <c>popover-side-*</c> class from <c>Side</c>, the open/closed
/// <c>overlay-hidden</c> gating, the ShowBackdrop toggle, the backdrop-tap dismiss (respecting
/// BackdropDismiss), the will/did dismiss callbacks, the stored triggerAction, and the per-mode class.
/// </summary>
public class IonPopoverTests : IonicComponentTestBase
{
    private static readonly RenderFragment Body = builder => builder.AddContent(0, "Popover body");

    private static ComponentUnderTest RenderPopover(TestContext ctx,
        Action<ComponentParameterBuilder<IonPopover>>? configure = null)
        => ctx.Render<IonPopover>(p =>
        {
            p.Add(nameof(IonPopover.IsOpen), true);
            p.Add(nameof(IonPopover.ChildContent), Body);
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonPopover_RendersOverlayContract()
    {
        var cut = RenderPopover(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-popover");
        cut.FindByClass("popover-backdrop").ShouldHaveSingleItem();
        cut.FindByClass("popover-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("popover-content").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonPopover_RendersChildContentInsideContent()
    {
        var cut = RenderPopover(Context);

        var content = cut.FindByClass("popover-content").ShouldHaveSingleItem();
        content.TextContent.ShouldNotBeNull();
        content.TextContent!.ShouldContain("Popover body");
    }

    [Fact]
    public void IonPopover_WrapperCarriesOverlayWrapperClass()
    {
        var cut = RenderPopover(Context);

        cut.FindByClass("popover-wrapper").ShouldHaveSingleItem()
            .ShouldHaveClass("ion-overlay-wrapper");
    }

    // ---- Arrow -------------------------------------------------------------

    [Fact]
    public void IonPopover_Arrow_RendersByDefault()
    {
        var cut = RenderPopover(Context);

        cut.FindByClass("popover-arrow").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonPopover_ArrowFalse_OmitsArrow()
    {
        var cut = RenderPopover(Context, p => p.Add(nameof(IonPopover.Arrow), false));

        cut.FindByClass("popover-arrow").ShouldBeEmpty();
    }

    // ---- Side --------------------------------------------------------------

    [Fact]
    public void IonPopover_DefaultSide_IsBottom()
    {
        var cut = RenderPopover(Context);

        cut.Root.ShouldHaveClass("popover-side-bottom");
    }

    [Fact]
    public void IonPopover_Side_StampsSideClass()
    {
        var cut = RenderPopover(Context, p => p.Add(nameof(IonPopover.Side), "top"));

        cut.Root.ShouldHaveClass("popover-side-top");
        cut.Root.ShouldNotHaveClass("popover-side-bottom");
    }

    // ---- Translucent -------------------------------------------------------

    [Fact]
    public void IonPopover_Translucent_StampsClass()
    {
        var cut = RenderPopover(Context, p => p.Add(nameof(IonPopover.Translucent), true));

        cut.Root.ShouldHaveClass("popover-translucent");
    }

    // ---- ShowBackdrop ------------------------------------------------------

    [Fact]
    public void IonPopover_ShowBackdropFalse_OmitsBackdrop()
    {
        var cut = RenderPopover(Context, p => p.Add(nameof(IonPopover.ShowBackdrop), false));

        cut.FindByClass("popover-backdrop").ShouldBeEmpty();
    }

    // ---- Open / closed gating ---------------------------------------------

    [Fact]
    public void IonPopover_Closed_StampsOverlayHidden()
    {
        var cut = Context.Render<IonPopover>(p =>
        {
            p.Add(nameof(IonPopover.IsOpen), false);
            p.Add(nameof(IonPopover.ChildContent), Body);
        });

        cut.Root.ShouldHaveClass("overlay-hidden");
    }

    [Fact]
    public void IonPopover_Open_DoesNotStampOverlayHidden()
    {
        var cut = RenderPopover(Context);

        cut.Root.ShouldNotHaveClass("overlay-hidden");
    }

    // ---- TriggerAction stored ---------------------------------------------

    [Fact]
    public void IonPopover_TriggerAction_DefaultsToClick()
    {
        var popover = new IonPopover();

        popover.TriggerAction.ShouldBe("click");
    }

    [Fact]
    public void IonPopover_TriggerAction_IsStored()
    {
        var popover = new IonPopover { TriggerAction = "hover" };

        popover.TriggerAction.ShouldBe("hover");
    }

    // ---- Dismiss interaction ----------------------------------------------

    [Fact]
    public async Task IonPopover_BackdropTap_Dismisses_WhenBackdropDismissEnabled()
    {
        var closed = false;
        IonOverlayDismissEventArgs? willDismiss = null;
        IonOverlayDismissEventArgs? didDismiss = null;
        var popover = new IonPopover
        {
            IsOpen = true,
            BackdropDismiss = true,
            IsOpenChanged = EventCallback.Factory.Create<bool>(this, v => closed = !v),
            OnWillDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => willDismiss = e),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => didDismiss = e),
        };
        popover.Build();

        await Invoke(popover, "OnBackdropTapAsync", new MouseEventArgs());

        closed.ShouldBeTrue();
        willDismiss.ShouldNotBeNull();
        didDismiss.ShouldNotBeNull();
        didDismiss!.Role.ShouldBe("backdrop");
    }

    [Fact]
    public async Task IonPopover_BackdropTap_IsNoOp_WhenBackdropDismissDisabled()
    {
        var invoked = false;
        var popover = new IonPopover
        {
            IsOpen = true,
            BackdropDismiss = false,
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, _ => invoked = true),
        };
        popover.Build();

        await Invoke(popover, "OnBackdropTapAsync", new MouseEventArgs());

        invoked.ShouldBeFalse();
    }

    // ---- Mode --------------------------------------------------------------

    [Fact]
    public void IonPopover_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderPopover(Context);

        cut.Root.Class.ShouldStartWith("ios ion-popover");
    }

    // Invokes a private async handler on the component (mirrors what a click/tap dispatches).
    private static async Task Invoke(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)mi.Invoke(component, new[] { arg })!;
    }
}
