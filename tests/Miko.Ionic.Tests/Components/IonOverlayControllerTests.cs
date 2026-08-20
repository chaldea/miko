using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Events;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public sealed class IonOverlayControllerTests : IonicComponentTestBase
{
    private readonly IonOverlayRegistry _registry = new();

    private void RegisterServices()
    {
        Context.Services.AddSingleton(_registry);
    }

    [Fact]
    public async Task ModalController_PresentsDynamicComponent_InTopLayer()
    {
        RegisterServices();
        var controller = new IonModalController(_registry);
        var cut = Context.Render<IonApp>();
        var reference = await controller.CreateAsync<ControllerModalContent>(
            new Dictionary<string, object?> { [nameof(ControllerModalContent.Text)] = "Created by controller" });

        cut.FindInTopLayerByClass("ion-modal").ShouldBeEmpty();
        await reference.PresentAsync();

        cut.FindInTopLayerByClass("ion-modal").ShouldHaveSingleItem();
        cut.FindInTopLayerByClass("controller-modal-content")
            .ShouldHaveSingleItem().TextContent.ShouldBe("Created by controller");
    }

    [Fact]
    public async Task ModalController_ForwardsSheetConfiguration()
    {
        RegisterServices();
        var controller = new IonModalController(_registry);
        var cut = Context.Render<IonApp>();
        var reference = await controller.CreateAsync(new IonModalOptions
        {
            Breakpoints = new[] { 0.0, 0.25, 1.0 },
            InitialBreakpoint = 0.25,
            Handle = true,
        });

        await reference.PresentAsync();

        cut.FindInTopLayerByClass("ion-modal").ShouldHaveSingleItem()
            .ShouldHaveClass("modal-sheet");
        cut.FindInTopLayerByClass("modal-handle").ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ControllerDismiss_IsIdempotent_AndReturnsRoleAndData()
    {
        RegisterServices();
        var controller = new IonModalController(_registry);
        var cut = Context.Render<IonApp>();
        var reference = await controller.CreateAsync(new IonModalOptions
        {
            Content = builder => builder.AddContent(0, "Body"),
        });
        await reference.PresentAsync();

        (await reference.DismissAsync(42, "confirm")).ShouldBeTrue();
        (await reference.DismissAsync(99, "again")).ShouldBeFalse();
        var result = await reference.OnDidDismissAsync();

        result.Role.ShouldBe("confirm");
        result.Data.ShouldBe(42);
        cut.FindInTopLayerByClass("ion-modal").ShouldBeEmpty();
    }

    [Fact]
    public async Task AlertButtonDismiss_CompletesControllerResult_AndRemovesOverlay()
    {
        RegisterServices();
        var controller = new IonAlertController(_registry);
        var cut = Context.Render<IonApp>();
        var reference = await controller.CreateAsync(new IonAlertOptions
        {
            Header = "Delete item?",
            Buttons = new[] { new IonAlertButton { Text = "Delete", Role = "destructive" } },
        });
        await reference.PresentAsync();

        var button = cut.FindInTopLayerByClass("alert-button").Single();
        button.OnClick!.Invoke(new Miko.Events.MouseEventArgs { Target = button });
        var result = await reference.OnDidDismissAsync();

        result.Role.ShouldBe("destructive");
        cut.FindInTopLayerByClass("ion-alert").ShouldBeEmpty();
    }

    [Fact]
    public async Task ModalController_GetTop_ReturnsLastPresentedModal()
    {
        RegisterServices();
        var controller = new IonModalController(_registry);
        Context.Render<IonApp>();
        var first = await controller.CreateAsync(new IonModalOptions());
        var second = await controller.CreateAsync(new IonModalOptions());

        await first.PresentAsync();
        await second.PresentAsync();

        var top = await controller.GetTopAsync();
        top.ShouldNotBeNull();
        top!.Id.ShouldBe(second.Id);
    }

    [Fact]
    public async Task ControllerGetTop_ReturnsItsOwnTopOverlay_WhenAnotherTypeIsAboveIt()
    {
        RegisterServices();
        var modalController = new IonModalController(_registry);
        var toastController = new IonToastController(_registry);
        Context.Render<IonApp>();
        var modal = await modalController.CreateAsync(new IonModalOptions());
        var toast = await toastController.CreateAsync(new IonToastOptions { Message = "Saved" });

        await modal.PresentAsync();
        await toast.PresentAsync();

        (await modalController.GetTopAsync())!.Id.ShouldBe(modal.Id);
        (await toastController.GetTopAsync())!.Id.ShouldBe(toast.Id);
        (await new IonOverlayManager(_registry).GetTopIdAsync()).ShouldBe(toast.Id);
    }

    [Fact]
    public async Task BackdropDismiss_CompletesControllerResult_AndCleansTheStack()
    {
        RegisterServices();
        var controller = new IonModalController(_registry);
        var cut = Context.Render<IonApp>();
        var reference = await controller.CreateAsync(new IonModalOptions());
        await reference.PresentAsync();

        var backdrop = cut.FindInTopLayerByClass("modal-backdrop").Single();
        backdrop.OnClick!.Invoke(new Miko.Events.MouseEventArgs { Target = backdrop });
        var result = await reference.OnDidDismissAsync();

        result.Role.ShouldBe("backdrop");
        new IonOverlayManager(_registry).Count.ShouldBe(0);
        cut.FindInTopLayerByClass("ion-modal").ShouldBeEmpty();
    }

    [Fact]
    public async Task AllControllers_ShareOneOrderedOverlayHost()
    {
        RegisterServices();
        var cut = Context.Render<IonApp>();
        var modal = await new IonModalController(_registry).CreateAsync(new IonModalOptions());
        var alert = await new IonAlertController(_registry).CreateAsync(new IonAlertOptions());
        var sheet = await new IonActionSheetController(_registry).CreateAsync(new IonActionSheetOptions());
        var loading = await new IonLoadingController(_registry).CreateAsync(new IonLoadingOptions());
        var popover = await new IonPopoverController(_registry).CreateAsync(new IonPopoverOptions());
        var toast = await new IonToastController(_registry).CreateAsync(new IonToastOptions { Message = "Saved" });

        await modal.PresentAsync();
        await alert.PresentAsync();
        await sheet.PresentAsync();
        await loading.PresentAsync();
        await popover.PresentAsync();
        await toast.PresentAsync();

        cut.FindInTopLayerByClass("ion-modal").Count.ShouldBe(1);
        cut.FindInTopLayerByClass("ion-alert").Count.ShouldBe(1);
        cut.FindInTopLayerByClass("ion-action-sheet").Count.ShouldBe(1);
        cut.FindInTopLayerByClass("ion-loading").Count.ShouldBe(1);
        cut.FindInTopLayerByClass("ion-popover").Count.ShouldBe(1);
        cut.FindInTopLayerByClass("ion-toast").Count.ShouldBe(1);

        var manager = new IonOverlayManager(_registry);
        manager.Count.ShouldBe(6);
        (await manager.GetTopIdAsync()).ShouldBe(toast.Id);
    }

    [Fact]
    public async Task PopoverController_ForwardsPresentEventToAnchorPosition()
    {
        RegisterServices();
        Context.ViewportWidth = 390;
        Context.ViewportHeight = 844;
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = Context.Render<IonApp>();
        var trigger = new Miko.Core.DomElements.DivElement();
        var controller = new IonPopoverController(_registry);
        var reference = await controller.CreateAsync(new IonPopoverOptions
        {
            Event = new MouseEventArgs
            {
                Target = trigger,
                X = 80,
                Y = 120,
                OffsetX = 20,
                OffsetY = 30,
                TargetWidth = 120,
                TargetHeight = 48,
            },
            Content = builder => builder.AddContent(0, "Body"),
        });

        await reference.PresentAsync();

        var wrapper = cut.FindInTopLayerByClass("popover-wrapper").ShouldHaveSingleItem();
        var style = wrapper.Style!;
        style.Position.ShouldBe(Position.Absolute);
        style.Left.ShouldBe(Length.Px(60));
        style.Top.ShouldBe(Length.Px(138));
    }

    public sealed class ControllerModalContent : ComponentBase
    {
        [Parameter] public string? Text { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "controller-modal-content");
            builder.AddContent(2, Text);
            builder.CloseElement();
        }
    }
}
