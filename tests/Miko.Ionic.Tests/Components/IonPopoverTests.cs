using Miko.Components;
using Miko.Common;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Miko.Styling;
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

    [Fact]
    public void IonPopover_ContentAndWrapper_ExpandToChildContent()
    {
        Context.ViewportWidth = 390;
        Context.ViewportHeight = 844;
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonPopover>(p =>
        {
            p.Add(nameof(IonPopover.IsOpen), true);
            p.Add(nameof(IonPopover.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonList>(0);
                builder.AddAttribute(1, nameof(IonList.ChildContent), (RenderFragment)(list =>
                {
                    list.OpenComponent<IonItem>(0);
                    list.AddAttribute(1, nameof(IonItem.ChildContent),
                        (RenderFragment)(item => item.AddContent(0, "Body")));
                    list.CloseComponent();
                }));
                builder.CloseComponent();
            }));
        });
        var wrapper = cut.FindByClass("popover-wrapper").ShouldHaveSingleItem();
        var content = cut.FindByClass("popover-content").ShouldHaveSingleItem();

        var wrapperBox = cut.GetBoxModel(wrapper).ShouldNotBeNull();
        var contentBox = cut.GetBoxModel(content).ShouldNotBeNull();
        wrapperBox.BorderBox.Height.ShouldBeGreaterThan(48f);
        contentBox.BorderBox.Height.ShouldBeGreaterThan(48f);
        wrapperBox.BorderBox.Height.ShouldBe(contentBox.BorderBox.Height, 0.01f);
    }

    [Fact]
    public void IonPopover_Event_AnchorsWrapperToTrigger()
    {
        Context.ViewportWidth = 390;
        Context.ViewportHeight = 844;
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var trigger = new Miko.Core.DomElements.DivElement();
        var cut = Context.Render<IonPopover>(p =>
        {
            p.Add(nameof(IonPopover.IsOpen), true);
            p.Add(nameof(IonPopover.Event), new MouseEventArgs
            {
                Target = trigger,
                X = 50,
                Y = 100,
                OffsetX = 10,
                OffsetY = 20,
                TargetWidth = 100,
                TargetHeight = 40,
            });
            p.Add(nameof(IonPopover.ChildContent), Body);
        });

        var wrapper = cut.FindByClass("popover-wrapper").ShouldHaveSingleItem();
        var style = cut.GetComputedStyle(wrapper)!;
        style.Position.ShouldBe(Position.Absolute);
        style.Left.ShouldBe(Length.Px(40));
        style.Top.ShouldBe(Length.Px(120));
    }

    [Fact]
    public void IonPopover_NearRightEdge_ShiftsContentLeftIntoViewport()
    {
        Context.ViewportWidth = 390;
        Context.ViewportHeight = 844;
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var trigger = new Miko.Core.DomElements.DivElement();
        var cut = Context.Render<IonPopover>(p =>
        {
            p.Add(nameof(IonPopover.IsOpen), true);
            p.Add(nameof(IonPopover.Event), new MouseEventArgs
            {
                Target = trigger,
                X = 365,
                Y = 50,
                OffsetX = 5,
                OffsetY = 10,
                TargetWidth = 30,
                TargetHeight = 40,
                ViewportWidth = 390,
                ViewportHeight = 844,
            });
            p.Add(nameof(IonPopover.ChildContent), Body);
        });

        var wrapper = cut.FindByClass("popover-wrapper").ShouldHaveSingleItem();
        var style = cut.GetComputedStyle(wrapper)!;
        var box = cut.GetBoxModel(wrapper).ShouldNotBeNull().BorderBox;
        style.Left.ShouldBe(Length.Px(132));
        box.Right.ShouldBeLessThanOrEqualTo(382.01f);
        cut.Root.ShouldHaveClass("popover-side-bottom");
    }

    [Fact]
    public void IonPopover_NearBottomEdge_FlipsAboveTrigger()
    {
        Context.ViewportWidth = 390;
        Context.ViewportHeight = 844;
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var trigger = new Miko.Core.DomElements.DivElement();
        var cut = Context.Render<IonPopover>(p =>
        {
            p.Add(nameof(IonPopover.IsOpen), true);
            p.Add(nameof(IonPopover.Event), new MouseEventArgs
            {
                Target = trigger,
                X = 100,
                Y = 790,
                OffsetX = 10,
                OffsetY = 10,
                TargetWidth = 100,
                TargetHeight = 40,
                ViewportWidth = 390,
                ViewportHeight = 844,
            });
            p.Add(nameof(IonPopover.ChildContent), Body);
        });

        var wrapper = cut.FindByClass("popover-wrapper").ShouldHaveSingleItem();
        var style = cut.GetComputedStyle(wrapper)!;
        var box = cut.GetBoxModel(wrapper).ShouldNotBeNull().BorderBox;
        style.Bottom.ToPixels(844).ShouldBe(64f, 0.01f);
        box.Bottom.ShouldBe(780f, 0.01f);
        cut.Root.ShouldHaveClass("popover-side-top");
    }

    // Invokes a private async handler on the component (mirrors what a click/tap dispatches).
    private static async Task Invoke(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)mi.Invoke(component, new[] { arg })!;
    }
}
