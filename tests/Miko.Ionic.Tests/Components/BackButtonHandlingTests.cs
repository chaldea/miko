using Miko.Common;
using Miko.Components;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for the system back button wiring on the Ionic overlays (ISSUE-138): an open menu, modal
/// or action sheet must be closable by the platform's back action — Android's hardware back, iOS's
/// edge gesture, desktop Escape — through the shared <c>BackButtonRegistry</c>, and must not claim
/// the back action when it is closed or when the user has disabled dismiss-by-back.
/// </summary>
public class BackButtonHandlingTests : IonicComponentTestBase
{
    private static readonly RenderFragment Body = builder => builder.AddContent("Body");

    // ---- IonModal ----------------------------------------------------------

    private ComponentUnderTest RenderModal(Action<ComponentParameterBuilder<IonModal>>? configure = null)
        => Context.Render<IonModal>(p =>
        {
            p.Add(nameof(IonModal.IsOpen), true);
            p.Add(nameof(IonModal.ChildContent), Body);
            configure?.Invoke(p);
        });

    [Fact]
    public void OpenModal_ConsumesBack_AndDismissesAsBack()
    {
        string? role = null;
        var cut = RenderModal(p => p.Add(nameof(IonModal.OnDidDismiss),
            EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => role = e.Role)));

        PressBack().ShouldBeTrue();

        role.ShouldBe("back");
    }

    [Fact]
    public void ClosedModal_DoesNotConsumeBack()
    {
        RenderModal(p => p.Add(nameof(IonModal.IsOpen), false));

        PressBack().ShouldBeFalse();
        Platform.Back.Count.ShouldBe(0);
    }

    /// <summary>
    /// <c>BackdropDismiss=false</c> means "this overlay requires an explicit decision" — the back
    /// action is the same intent as the backdrop tap, so it must be declined too.
    /// </summary>
    [Fact]
    public void ModalWithBackdropDismissDisabled_DoesNotConsumeBack()
    {
        RenderModal(p => p.Add(nameof(IonModal.BackdropDismiss), false));

        PressBack().ShouldBeFalse();
    }

    // ---- IonActionSheet ----------------------------------------------------

    private ComponentUnderTest RenderSheet(Action<ComponentParameterBuilder<IonActionSheet>>? configure = null)
        => Context.Render<IonActionSheet>(p =>
        {
            p.Add(nameof(IonActionSheet.IsOpen), true);
            p.Add(nameof(IonActionSheet.Header), "Actions");
            configure?.Invoke(p);
        });

    [Fact]
    public void OpenActionSheet_ConsumesBack_AndDismissesAsBack()
    {
        string? role = null;
        RenderSheet(p => p.Add(nameof(IonActionSheet.OnDidDismiss),
            EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => role = e.Role)));

        PressBack().ShouldBeTrue();

        role.ShouldBe("back");
    }

    [Fact]
    public void ClosedActionSheet_DoesNotConsumeBack()
    {
        RenderSheet(p => p.Add(nameof(IonActionSheet.IsOpen), false));

        PressBack().ShouldBeFalse();
        Platform.Back.Count.ShouldBe(0);
    }

    [Fact]
    public void ActionSheetWithBackdropDismissDisabled_DoesNotConsumeBack()
    {
        RenderSheet(p => p.Add(nameof(IonActionSheet.BackdropDismiss), false));

        PressBack().ShouldBeFalse();
    }

    // ---- IonMenu -----------------------------------------------------------

    [Fact]
    public void OpenMenu_ConsumesBack_AndRaisesOnClose()
    {
        var closed = 0;
        Context.Render<IonMenu>(p =>
        {
            p.Add(nameof(IonMenu.IsOpen), true);
            p.Add(nameof(IonMenu.ChildContent), Body);
            p.Add(nameof(IonMenu.OnClose), EventCallback.Factory.Create(this, () => closed++));
        });

        PressBack().ShouldBeTrue();

        closed.ShouldBe(1);
    }

    [Fact]
    public void ClosedMenu_DoesNotConsumeBack()
    {
        Context.Render<IonMenu>(p =>
        {
            p.Add(nameof(IonMenu.IsOpen), false);
            p.Add(nameof(IonMenu.ChildContent), Body);
            p.Add(nameof(IonMenu.OnClose), EventCallback.Factory.Create(this, () => { }));
        });

        PressBack().ShouldBeFalse();
    }

    /// <summary>
    /// The menu is a controlled component: with no host listening there is nothing to close, so it
    /// must not swallow the back action (which would leave the user with a dead back button).
    /// </summary>
    [Fact]
    public void OpenMenuWithoutOnCloseHandler_DoesNotConsumeBack()
    {
        Context.Render<IonMenu>(p =>
        {
            p.Add(nameof(IonMenu.IsOpen), true);
            p.Add(nameof(IonMenu.ChildContent), Body);
        });

        PressBack().ShouldBeFalse();
    }

    // ---- After dismiss -----------------------------------------------------

    /// <summary>
    /// Once back has closed the modal, the next back press belongs to whatever is beneath it.
    /// A modal whose host does not re-render on <c>IsOpenChanged</c> must still let go of the back
    /// button — otherwise the user is left with a dead back button and duplicate dismiss events.
    /// </summary>
    [Fact]
    public void Modal_SecondBackAfterDismiss_IsNotConsumed()
    {
        var dismissals = 0;
        RenderModal(p => p.Add(nameof(IonModal.OnDidDismiss),
            EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, _ => dismissals++)));

        PressBack().ShouldBeTrue();
        PressBack().ShouldBeFalse();

        dismissals.ShouldBe(1);
    }

    [Fact]
    public void ActionSheet_SecondBackAfterDismiss_IsNotConsumed()
    {
        var dismissals = 0;
        RenderSheet(p => p.Add(nameof(IonActionSheet.OnDidDismiss),
            EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, _ => dismissals++)));

        PressBack().ShouldBeTrue();
        PressBack().ShouldBeFalse();

        dismissals.ShouldBe(1);
    }

    // ---- IonBackButton -----------------------------------------------------

    /// <summary>
    /// Without a navigation manager the button has nothing to do, so it must hand the back action
    /// to the platform (exit) instead of reporting it consumed.
    /// </summary>
    [Fact]
    public void BackButtonWithoutNavigation_DoesNotConsumeBack()
    {
        Context.Render<IonBackButton>(p => p.Add(nameof(IonBackButton.DefaultHref), "/"));

        PressBack().ShouldBeFalse();
    }

    // ---- Ordering ----------------------------------------------------------

    /// <summary>
    /// Nested components are recreated on every parent render, so registration order follows
    /// build order, not what is on screen. A header that re-renders after a modal opened would
    /// otherwise put the page's back button on top and navigate away under the open modal. Overlays
    /// therefore outrank navigation regardless of order (Ionic: overlay 100 &gt; navigation 0).
    /// </summary>
    [Fact]
    public void OpenOverlay_OutranksBackButton_RegisteredAfterIt()
    {
        string? role = null;
        RenderModal(p => p.Add(nameof(IonModal.OnDidDismiss),
            EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => role = e.Role)));
        Context.Render<IonBackButton>(p => p.Add(nameof(IonBackButton.DefaultHref), "/"));

        PressBack().ShouldBeTrue();

        role.ShouldBe("back");
    }

    /// <summary>
    /// Overlays render through the portal above the page, menu included, so an open modal is the
    /// topmost thing on screen and takes back first — even when the menu registered later
    /// (Ionic: overlay 100 &gt; menu 99). The menu then gets the next press.
    /// </summary>
    [Fact]
    public void OpenOverlay_TakesBackBeforeMenu_RegardlessOfOrder()
    {
        string? modalRole = null;
        var menuClosed = 0;

        RenderModal(p => p.Add(nameof(IonModal.OnDidDismiss),
            EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => modalRole = e.Role)));
        Context.Render<IonMenu>(p =>
        {
            p.Add(nameof(IonMenu.IsOpen), true);
            p.Add(nameof(IonMenu.ChildContent), Body);
            p.Add(nameof(IonMenu.OnClose), EventCallback.Factory.Create(this, () => menuClosed++));
        });

        PressBack().ShouldBeTrue();
        modalRole.ShouldBe("back");
        menuClosed.ShouldBe(0);

        PressBack().ShouldBeTrue();
        menuClosed.ShouldBe(1);
    }
}
