using Miko.Common;
using Miko.Components;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Miko.Styling;
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

    [Fact]
    public void IonModal_DefaultWrapper_FillsViewportWithoutInsetCaps()
    {
        Context.ViewportWidth = 390;
        Context.ViewportHeight = 844;
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderModal(Context);
        var wrapper = cut.FindByClass("modal-wrapper").ShouldHaveSingleItem();
        var style = cut.GetComputedStyle(wrapper)!;
        var box = cut.FindLayoutBox(wrapper)!.BoxModel.BorderBox;

        style.MinWidth.ShouldBe(Length.Auto);
        style.MaxWidth.ShouldBe(Length.Auto);
        style.MinHeight.ShouldBe(Length.Auto);
        style.MaxHeight.ShouldBe(Length.Auto);
        style.BorderTopLeftRadius.ShouldBe(Length.Px(0));
        style.BoxShadow.RefValueOrNull()!.ShouldBeEmpty();
        box.Width.ShouldBe(390f, 0.01f);
        box.Height.ShouldBe(844f, 0.01f);
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

    [Fact]
    public void IonModal_Sheet_AnchorsAtBottomAndUsesInitialBreakpoint()
    {
        Context.ViewportWidth = 400;
        Context.ViewportHeight = 600;
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderModal(Context, p =>
        {
            p.Add(nameof(IonModal.Breakpoints), new[] { 0.0, 0.2, 0.5, 1.0 });
            p.Add(nameof(IonModal.InitialBreakpoint), 0.2);
        });

        var wrapper = cut.FindByClass("modal-wrapper").ShouldHaveSingleItem();
        var style = cut.GetComputedStyle(wrapper)!;
        var box = cut.FindLayoutBox(wrapper)!.BoxModel.BorderBox;

        style.Position.ShouldBe(Position.Absolute);
        style.Bottom.ShouldBe(Length.Auto);
        style.Top.ToPixels(600).ShouldBe(482f, 0.01f);
        box.Height.ShouldBe(590f, 0.01f);
        box.Top.ShouldBe(482f, 0.01f);
        Math.Max(0, Math.Min(600f, box.Bottom) - box.Top).ShouldBe(118f, 0.01f);
        cut.FindByClass("modal-handle").ShouldHaveSingleItem();
    }

    [Fact]
    public async Task IonModal_SetCurrentBreakpoint_UpdatesSheetPosition()
    {
        var modal = new IonModal
        {
            IsOpen = true,
            Breakpoints = new[] { 0.0, 0.2, 0.5, 1.0 },
            InitialBreakpoint = 0.2,
        };
        var root = modal.Build();

        (await modal.GetCurrentBreakpointAsync()).ShouldBe(0.2);
        TopValue(root.FindByClass("modal-wrapper").Single().Style).ShouldBe(482f, 0.01f);

        await modal.SetCurrentBreakpointAsync(0.5);

        (await modal.GetCurrentBreakpointAsync()).ShouldBe(0.5);
        TopValue(root.FindByClass("modal-wrapper").Single().Style).ShouldBe(305f, 0.01f);
    }

    [Fact]
    public async Task IonModal_SetCurrentBreakpoint_IgnoresUnconfiguredValue()
    {
        var modal = new IonModal
        {
            IsOpen = true,
            Breakpoints = new[] { 0.2, 0.5, 1.0 },
            InitialBreakpoint = 0.2,
        };
        modal.Build();

        await modal.SetCurrentBreakpointAsync(0.75);

        (await modal.GetCurrentBreakpointAsync()).ShouldBe(0.2);
    }

    [Fact]
    public async Task IonModal_SheetDrag_SnapsToNearestBreakpoint()
    {
        var modal = new IonModal
        {
            IsOpen = true,
            Breakpoints = new[] { 0.0, 0.2, 0.5, 1.0 },
            InitialBreakpoint = 0.2,
        };
        modal.Build();

        InvokeSync(modal, "OnSheetPointerDown", new MouseEventArgs
        {
            IsButtonPressed = true,
            Button = MouseButton.Left,
            Y = 500,
            TargetHeight = 590,
        });
        InvokeSync(modal, "OnSheetPointerMove", new MouseEventArgs
        {
            IsButtonPressed = true,
            Button = MouseButton.Left,
            Y = 323,
            TargetHeight = 590,
        });
        await Invoke(modal, "OnSheetPointerUpAsync", new MouseEventArgs { Y = 323 });

        (await modal.GetCurrentBreakpointAsync()).ShouldBe(0.5);
    }

    [Fact]
    public async Task IonModal_InvalidInitialBreakpoint_UsesNearestConfiguredBreakpoint()
    {
        var modal = new IonModal
        {
            IsOpen = true,
            Breakpoints = new[] { 0.0, 0.5, 1.0 },
            InitialBreakpoint = 0.6,
        };
        modal.Build();

        (await modal.GetCurrentBreakpointAsync()).ShouldBe(0.5);
    }

    [Fact]
    public void IonModal_NonSheetOrHandleFalse_OmitsHandle()
    {
        RenderModal(Context).FindByClass("modal-handle").ShouldBeEmpty();

        RenderModal(Context, p =>
        {
            p.Add(nameof(IonModal.Breakpoints), new[] { 0.5, 1.0 });
            p.Add(nameof(IonModal.InitialBreakpoint), 0.5);
            p.Add(nameof(IonModal.Handle), false);
        }).FindByClass("modal-handle").ShouldBeEmpty();
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

    private static void InvokeSync(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        mi.Invoke(component, new[] { arg });
    }

    private static float TopValue(Miko.Styling.Style? style)
        => style?.Top.ValueOrNull()?.ToPixels(600) ?? 0;
}
