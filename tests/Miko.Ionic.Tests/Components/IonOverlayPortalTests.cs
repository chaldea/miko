using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

public sealed class IonOverlayPortalTests : IonicComponentTestBase
{
    private IonOverlayRegistry RegisterOverlays()
    {
        var registry = new IonOverlayRegistry();
        Context.Services.AddSingleton(registry);
        return registry;
    }

    private static RenderFragment ModalInsideOwner(bool open = true) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "overlay-owner");
        builder.AddAttribute(2, "style", new Style
        {
            Height = Length.Px(48),
            OverflowX = Overflow.Hidden,
            OverflowY = Overflow.Hidden,
        });
        builder.OpenComponent<IonModal>(3);
        builder.AddComponentParameter(4, nameof(IonModal.IsOpen), open);
        builder.AddComponentParameter(5, nameof(IonModal.ChildContent), (RenderFragment)(content =>
        {
            content.OpenElement(0, "div");
            content.AddAttribute(1, "class", "portal-modal-content");
            content.AddContent(2, "Modal content");
            content.CloseElement();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    };

    private static readonly RenderFragment SelectOptions = builder =>
    {
        builder.OpenComponent<IonSelectOption>(0);
        builder.AddComponentParameter(1, nameof(IonSelectOption.Value), "a");
        builder.AddComponentParameter(2, nameof(IonSelectOption.ChildContent),
            (RenderFragment)(text => text.AddContent(0, "Alpha")));
        builder.CloseComponent();
        builder.OpenComponent<IonSelectOption>(3);
        builder.AddComponentParameter(4, nameof(IonSelectOption.Value), "b");
        builder.AddComponentParameter(5, nameof(IonSelectOption.ChildContent),
            (RenderFragment)(text => text.AddContent(0, "Beta")));
        builder.CloseComponent();
    };

    [Fact]
    public void DeclarativeModal_IsMountedUnderIonAppOverlayHost()
    {
        RegisterOverlays();
        var cut = Context.Render<IonApp>(p =>
            p.Add(nameof(IonApp.ChildContent), ModalInsideOwner()));

        var host = cut.TopLayerRoot;
        host.ShouldNotBeNull();
        var modal = cut.FindInTopLayerByClass("ion-modal").ShouldHaveSingleItem();
        var owner = cut.FindByClass("overlay-owner").Single();

        IsInside(modal, host!).ShouldBeTrue();
        IsInside(modal, owner).ShouldBeFalse();
        owner.FindByClass("ion-modal").ShouldBeEmpty();
    }

    [Fact]
    public void OverlayEvent_DoesNotBubbleBackToTheDeclaringOwner()
    {
        RegisterOverlays();
        var cut = Context.Render<IonApp>(p =>
            p.Add(nameof(IonApp.ChildContent), ModalInsideOwner()));

        var ownerClicks = 0;
        var owner = cut.FindByClass("overlay-owner").Single();
        owner.OnClick = _ => ownerClicks++;

        var backdrop = cut.FindInTopLayerByClass("modal-backdrop").Single();
        new EventDispatcher().Dispatch(backdrop, EventTypes.Click,
            new MouseEventArgs { Target = backdrop, Button = MouseButton.Left, Bubbles = true });

        ownerClicks.ShouldBe(0);
    }

    [Fact]
    public void PortalOverlay_EscapesClippingOwner_AndWinsHitTest()
    {
        RegisterOverlays();
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = Context.Render<IonApp>(p =>
            p.Add(nameof(IonApp.ChildContent), ModalInsideOwner()));

        using var surface = SKSurface.Create(new SKImageInfo(
            (int)Context.ViewportWidth, (int)Context.ViewportHeight));
        var engine = new MikoEngine();
        engine.Initialize(cut.Root, Context.StyleSheets, surface.Canvas,
            Context.ViewportWidth, Context.ViewportHeight);

        var modal = cut.FindInTopLayerByClass("ion-modal").Single();
        var hit = engine.HitTest(Context.ViewportWidth / 2f, Context.ViewportHeight - 20f);

        hit.ShouldNotBeNull();
        IsInside(hit!, modal).ShouldBeTrue($"hit {hit!.TagName}.{hit.Class}");
    }

    [Fact]
    public void IonSelect_UsesRootOverlayHost_AndReturnsTheSelectedValue()
    {
        var registry = RegisterOverlays();
        string? changed = null;
        var cut = Context.Render<IonApp>(p => p.Add(nameof(IonApp.ChildContent), (RenderFragment)(builder =>
        {
            builder.OpenComponent<IonSelect>(0);
            builder.AddComponentParameter(1, nameof(IonSelect.Placeholder), "Pick one");
            builder.AddComponentParameter(2, nameof(IonSelect.ValueChanged),
                EventCallback.Factory.Create<string?>(this, value => changed = value));
            builder.AddComponentParameter(3, nameof(IonSelect.ChildContent), SelectOptions);
            builder.CloseComponent();
        })));

        var select = cut.FindByClass("ion-select").Single();
        new IonOverlayManager(registry).Count.ShouldBe(0);
        select.OnClick!.Invoke(new MouseEventArgs { Target = select });

        new IonOverlayManager(registry).Count.ShouldBe(1);
        select.FindByClass("ion-alert").ShouldBeEmpty();
        var alert = cut.FindInTopLayerByClass("ion-alert").ShouldHaveSingleItem();
        alert.FindByClass("alert-radio-button")[1].OnClick!
            .Invoke(new MouseEventArgs { Target = alert });

        var ok = cut.FindInTopLayerByClass("alert-button")
            .First(button => !button.HasClass("alert-button-role-cancel"));
        ok.OnClick!.Invoke(new MouseEventArgs { Target = ok });

        changed.ShouldBe("b");
        cut.FindByClass("select-text").Single().TextContent.ShouldBe("Beta");
        new IonOverlayManager(registry).Count.ShouldBe(0);
    }

    [Fact]
    public void PortalRegistration_IsRemovedWhenDeclaringSubtreeIsDisposed()
    {
        var registry = RegisterOverlays();
        var cut = Context.Render<PortalToggleHost>();

        new IonOverlayManager(registry).Count.ShouldBe(1);
        cut.FindInTopLayerByClass("ion-modal").Count.ShouldBe(1);

        var hide = cut.FindByClass("hide-overlay").Single();
        hide.OnClick!.Invoke(new MouseEventArgs { Target = hide });

        new IonOverlayManager(registry).Count.ShouldBe(0);
        cut.FindInTopLayerByClass("ion-modal").ShouldBeEmpty();
    }

    [Fact]
    public void Toast_AllowsHitTestingThroughItsTransparentArea()
    {
        RegisterOverlays();
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = Context.Render<IonApp>(p => p.Add(nameof(IonApp.ChildContent), (RenderFragment)(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "page-target");
            builder.AddAttribute(2, "style", new Style
            {
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            });
            builder.CloseElement();
            builder.OpenComponent<IonToast>(3);
            builder.AddComponentParameter(4, nameof(IonToast.IsOpen), true);
            builder.AddComponentParameter(5, nameof(IonToast.Message), "Saved");
            builder.CloseComponent();
        })));

        using var surface = SKSurface.Create(new SKImageInfo(
            (int)Context.ViewportWidth, (int)Context.ViewportHeight));
        var engine = new MikoEngine();
        engine.Initialize(cut.Root, Context.StyleSheets, surface.Canvas,
            Context.ViewportWidth, Context.ViewportHeight);

        var page = cut.FindByClass("page-target").Single();
        var hit = engine.HitTest(10, 10);

        hit.ShouldNotBeNull();
        IsInside(hit!, page).ShouldBeTrue($"hit {hit!.TagName}.{hit.Class}");
    }

    [Fact]
    public void LaterOverlay_WinsHitTestWhenMultipleOverlaysAreOpen()
    {
        RegisterOverlays();
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = Context.Render<IonApp>(p => p.Add(nameof(IonApp.ChildContent), (RenderFragment)(builder =>
        {
            builder.OpenComponent<IonModal>(0);
            builder.AddComponentParameter(1, nameof(IonModal.IsOpen), true);
            builder.CloseComponent();
            builder.OpenComponent<IonAlert>(2);
            builder.AddComponentParameter(3, nameof(IonAlert.IsOpen), true);
            builder.AddComponentParameter(4, nameof(IonAlert.Header), "Top alert");
            builder.CloseComponent();
        })));

        using var surface = SKSurface.Create(new SKImageInfo(
            (int)Context.ViewportWidth, (int)Context.ViewportHeight));
        var engine = new MikoEngine();
        engine.Initialize(cut.Root, Context.StyleSheets, surface.Canvas,
            Context.ViewportWidth, Context.ViewportHeight);

        var alert = cut.FindInTopLayerByClass("ion-alert").Single();
        var hit = engine.HitTest(Context.ViewportWidth / 2f, Context.ViewportHeight / 2f);

        hit.ShouldNotBeNull();
        IsInside(hit!, alert).ShouldBeTrue($"hit {hit!.TagName}.{hit.Class}");
    }

    private static bool IsInside(Element element, Element ancestor)
    {
        for (var node = element; node is not null; node = node.Parent)
            if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }

    public sealed class PortalToggleHost : ComponentBase
    {
        private bool _show = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<IonApp>(0);
            builder.AddComponentParameter(1, nameof(IonApp.ChildContent), (RenderFragment)(content =>
            {
                content.OpenElement(0, "button");
                content.AddAttribute(1, "class", "hide-overlay");
                content.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(
                    this, _ => _show = false));
                content.AddContent(3, "Hide");
                content.CloseElement();
                if (_show)
                {
                    content.OpenComponent<IonModal>(4);
                    content.AddComponentParameter(5, nameof(IonModal.IsOpen), true);
                    content.CloseComponent();
                }
            }));
            builder.CloseComponent();
        }
    }
}
