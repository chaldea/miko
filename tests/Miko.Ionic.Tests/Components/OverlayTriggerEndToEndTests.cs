using Miko.Core;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Ionic.Tests.Fixtures;
using Miko.Ionic.Tests.Fixtures.Routed;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Overlays open from their <c>Trigger</c> (P0-C1), Ionic's main declarative pattern:
/// <c>&lt;IonButton Id="open-modal"&gt;</c> + <c>&lt;IonModal Trigger="open-modal"&gt;</c> with no
/// <c>IsOpen</c> handling. The parameter used to be accepted and ignored on all six overlays.
/// Driven through a routed app under <see cref="IonApp"/>, which picks the click up.
/// </summary>
public sealed class OverlayTriggerEndToEndTests : IDisposable
{
    private readonly RoutedApp _app = new(("/", typeof(TriggerPageFixture)));

    public void Dispose() => _app.Dispose();

    private IonOverlayManager Overlays => (IonOverlayManager)_app.App.Services.GetService(typeof(IonOverlayManager))!;

    /// <summary>The overlay's host in IonApp's overlay layer, when it is there and not hidden.</summary>
    private Element? OpenHost(string hostClass)
    {
        var layer = _app.Root.FindByClass("ion-overlay-host").ShouldHaveSingleItem();
        return layer.FindByClass(hostClass).SingleOrDefault(e => !e.HasClass("overlay-hidden"));
    }

    [Theory]
    [InlineData("opener-modal", "trigger-modal")]
    [InlineData("opener-alert", "trigger-alert")]
    [InlineData("opener-sheet", "trigger-sheet")]
    [InlineData("opener-loading", "trigger-loading")]
    [InlineData("opener-toast", "trigger-toast")]
    [InlineData("opener-popover", "trigger-popover")]
    public void Should_open_the_overlay_when_its_trigger_is_tapped(string opener, string overlay)
    {
        Overlays.Count.ShouldBe(0);
        OpenHost(overlay).ShouldBeNull();

        _app.Tap(opener);
        _app.Render();

        OpenHost(overlay).ShouldNotBeNull($"tapping .{opener} must open .{overlay}");
        Overlays.Count.ShouldBe(1, "only the overlay bound to that id opens");
    }

    [Fact]
    public void Should_not_open_any_overlay_when_an_unrelated_element_is_tapped()
    {
        _app.Tap("unrelated");
        _app.Render();

        Overlays.Count.ShouldBe(0);
    }

    [Fact]
    public void Should_anchor_a_triggered_popover_to_the_pressed_element()
    {
        _app.Tap("opener-popover");
        _app.Render();

        // Opened from a press the popover is positioned against it (an explicit wrapper position
        // above or below the opener), not centered on the screen as without an anchor.
        var popover = OpenHost("trigger-popover").ShouldNotBeNull();
        var wrapper = popover.FindByClass("popover-wrapper").ShouldHaveSingleItem();
        wrapper.Style.ShouldNotBeNull("an anchored popover gets a computed position");
        (wrapper.Style!.Top ?? wrapper.Style.Bottom).ShouldNotBeNull();
    }

    [Fact]
    public void Should_dismiss_and_reopen_a_triggered_overlay()
    {
        _app.Tap("opener-alert");
        _app.Render();
        _app.Tap("alert-button");
        _app.Render();
        OpenHost("trigger-alert").ShouldBeNull("the alert's button closes it");

        _app.Tap("opener-alert");
        _app.Render();

        OpenHost("trigger-alert").ShouldNotBeNull("the trigger opens it again");
    }

    [Fact]
    public void Should_open_a_context_menu_popover_on_long_press_but_not_on_click()
    {
        _app.Tap("opener-context");
        _app.Render();
        OpenHost("trigger-context").ShouldBeNull("TriggerAction=\"context-menu\" ignores a plain click");

        // The controller raises longpress after holding still for its long-press delay; dispatch the
        // same bubbling event directly rather than wait on that wall-clock timer.
        var dispatcher = (EventDispatcher)_app.App.Services.GetService(typeof(EventDispatcher))!;
        var target = _app.Single("opener-context").FindByClass("button-native").Single();
        dispatcher.Dispatch(target, EventTypes.LongPress, new PointerEventArgs { Target = target, PointerType = PointerType.Touch });
        _app.Render();

        OpenHost("trigger-context").ShouldNotBeNull();
    }
}
