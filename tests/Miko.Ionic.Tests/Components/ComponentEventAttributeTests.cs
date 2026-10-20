using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Microsoft.Extensions.DependencyInjection;
using Miko.Ionic.Tests.Fixtures;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-153 problem 5: <c>@onclick</c> written on a component compiled but never fired. The
/// compiler lowers it to an attribute named <c>"onclick"</c> holding an
/// <c>EventCallback&lt;MouseEventArgs&gt;</c>; the runtime looked it up as a parameter
/// case-sensitively and dropped it. It now binds to a parameter of that name ignoring case
/// (<see cref="Miko.Ionic.Components.IonButton.OnClick"/>), as Blazor does, and otherwise
/// listens on the element the component renders, as on an Ionic host element.
/// </summary>
public class ComponentEventAttributeTests : IonicComponentTestBase
{
    private static void Click(Element target)
        => new EventDispatcher().Dispatch(target, EventTypes.Click,
            new MouseEventArgs { Target = target, Button = MouseButton.Left, Bubbles = true });

    [Fact]
    public void OnClick_OnComponentWithoutClickParameter_ListensOnItsElement()
    {
        ComponentClickFixture.Log.Clear();
        var cut = Context.Render<ComponentClickFixture>();

        var chip = cut.FindByClass("chip").ShouldHaveSingleItem();
        // A tap lands on the chip's content and bubbles to the chip, as in the browser.
        var inner = chip.Children.FirstOrDefault(c => c is not TextNode) ?? chip;
        Click(inner);

        ComponentClickFixture.Log.ShouldBe(["chip"]);
    }

    [Fact]
    public void OnClick_OnComponentWithOnClickParameter_BindsToTheParameter()
    {
        ComponentClickFixture.Log.Clear();
        var cut = Context.Render<ComponentClickFixture>();

        var native = cut.FindByClass("button").ShouldHaveSingleItem()
            .FindByClass("button-native").ShouldHaveSingleItem();
        Click(native);

        // Bound to IonButton.OnClick (not also attached to the host): it fires exactly once.
        ComponentClickFixture.Log.ShouldBe(["button"]);
    }

    [Fact]
    public void HostListener_SurvivesTheFixtureRerender()
    {
        // The handler's receiver is the fixture, which re-renders after each click and builds a
        // new IonChip instance — the listener must be there again on the replacement.
        ComponentClickFixture.Log.Clear();
        var cut = Context.Render<ComponentClickFixture>();

        Click(cut.FindByClass("chip").Single());
        Click(cut.FindByClass("chip").Single());

        ComponentClickFixture.Log.ShouldBe(["chip", "chip"]);
    }
}

/// <summary>
/// ISSUE-153 problem 4: <c>@ref</c> captured nothing, so component methods such as
/// <see cref="Miko.Ionic.Components.IonModal.DismissAsync"/> could not be called from markup.
/// </summary>
public class ComponentRefTests : IonicComponentTestBase
{
    [Fact]
    public async Task ModalRef_DismissAsync_ClosesTheModal()
    {
        ModalRefFixture.Modal = null;
        ModalRefFixture.Role = null;
        Context.Services.AddSingleton(new Miko.Ionic.Components.IonOverlayRegistry());
        var cut = Context.Render<ModalRefFixture>();
        cut.FindInTopLayerByClass("modal-ref-body").ShouldHaveSingleItem();

        ModalRefFixture.Modal.ShouldNotBeNull();
        await ModalRefFixture.Modal!.DismissAsync("cancel");

        ModalRefFixture.Role.ShouldBe("cancel");
        cut.FindInTopLayerByClass("modal-ref-body").ShouldBeEmpty();
    }

    /// <summary>
    /// ISSUE-153: without <c>@bind-IsOpen</c>, an overlay closed from code or by the system back
    /// button stayed on screen — nothing re-rendered it after it set IsOpen to false.
    /// </summary>
    [Fact]
    public async Task UnboundOverlays_ClosedFromCode_LeaveTheScreen()
    {
        Context.Services.AddSingleton(new Miko.Ionic.Components.IonOverlayRegistry());
        var cut = Context.Render<UnboundOverlayRefFixture>();
        cut.FindInTopLayerByClass("unbound-modal-body").ShouldHaveSingleItem();
        cut.FindInTopLayerByClass("unbound-popover-body").ShouldHaveSingleItem();

        await UnboundOverlayRefFixture.Popover!.DismissAsync("cancel");
        cut.FindInTopLayerByClass("unbound-popover-body").ShouldBeEmpty();

        PressBack().ShouldBeTrue("the open modal answers the system back button");
        cut.FindInTopLayerByClass("unbound-modal-body").ShouldBeEmpty();
        PressBack().ShouldBeFalse("a closed modal must not keep the back button");
    }
}
