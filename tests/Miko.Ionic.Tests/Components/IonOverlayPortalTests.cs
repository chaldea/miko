using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Core;
using Miko.Events;
using Miko.Hosting;
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
        var __e1 = builder.OpenElement<DivElement>();
        __e1.Class = "overlay-owner";
        __e1.Style = new Style
        {
            Height = Length.Px(48),
            OverflowX = Overflow.Hidden,
            OverflowY = Overflow.Hidden,
        };
        var __c5 = builder.OpenComponent<IonModal>();
        __c5.IsOpen = open;
        __c5.ChildContent = (RenderFragment)(content =>
        {
            var __e2 = content.OpenElement<DivElement>();
            __e2.Class = "portal-modal-content";
            content.AddContent("Modal content");
            content.CloseElement();
        });
        builder.CloseComponent();
        builder.CloseElement();
    };

    private static readonly RenderFragment SelectOptions = builder =>
    {
        var __c6 = builder.OpenComponent<IonSelectOption>();
        __c6.Value = "a";
        __c6.ChildContent = 
            (RenderFragment)(text => text.AddContent("Alpha"));
        builder.CloseComponent();
        var __c7 = builder.OpenComponent<IonSelectOption>();
        __c7.Value = "b";
        __c7.ChildContent = 
            (RenderFragment)(text => text.AddContent("Beta"));
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
        var engine = new MikoEngineBuilder().Build();
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
            var __c8 = builder.OpenComponent<IonSelect>();
            __c8.Placeholder = "Pick one";
            __c8.ValueChanged = 
                EventCallback.Factory.Create<string?>(this, value => changed = value);
            __c8.ChildContent = SelectOptions;
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
            var __e3 = builder.OpenElement<DivElement>();
            __e3.Class = "page-target";
            __e3.Style = new Style
            {
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            };
            builder.CloseElement();
            var __c9 = builder.OpenComponent<IonToast>();
            __c9.IsOpen = true;
            __c9.Message = "Saved";
            builder.CloseComponent();
        })));

        using var surface = SKSurface.Create(new SKImageInfo(
            (int)Context.ViewportWidth, (int)Context.ViewportHeight));
        var engine = new MikoEngineBuilder().Build();
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
            var __c10 = builder.OpenComponent<IonModal>();
            __c10.IsOpen = true;
            builder.CloseComponent();
            var __c11 = builder.OpenComponent<IonAlert>();
            __c11.IsOpen = true;
            __c11.Header = "Top alert";
            builder.CloseComponent();
        })));

        using var surface = SKSurface.Create(new SKImageInfo(
            (int)Context.ViewportWidth, (int)Context.ViewportHeight));
        var engine = new MikoEngineBuilder().Build();
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
            var __c12 = builder.OpenComponent<IonApp>();
            __c12.ChildContent = (RenderFragment)(content =>
            {
                var __e4 = content.OpenElement<ButtonElement>();
                __e4.Class = "hide-overlay";
                __e4.OnClick = global::Miko.Components.RenderTreeBuilder.ToHandler(EventCallback.Factory.Create<MouseEventArgs>(
                    this, _ => _show = false));
                content.AddContent("Hide");
                content.CloseElement();
                if (_show)
                {
                    var __c13 = content.OpenComponent<IonModal>();
                    __c13.IsOpen = true;
                    content.CloseComponent();
                }
            });
            builder.CloseComponent();
        }
    }
}
