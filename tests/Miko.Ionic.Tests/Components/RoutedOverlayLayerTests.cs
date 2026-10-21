using Miko.Core;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Ionic.Tests.Fixtures.Routed;
using Miko.Platform;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// The overlay layer of a routed page (ISSUE-153 problems 1 and 2), through the real app pipeline:
/// <see cref="MikoAppBuilder"/> + <c>RouteView</c> + the templates' <c>&lt;IonApp&gt;@Body&lt;/IonApp&gt;</c>
/// layout. The component-level portal tests render <see cref="IonApp"/> around their content
/// directly, so the page sits inside the app's cascading scope by construction — the routed path
/// is the one real apps take.
/// </summary>
public class RoutedOverlayLayerTests : IDisposable
{
    private const int W = 390;
    private const int H = 844;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public RoutedOverlayLayerTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private MikoAppContext BuildApp(Type page)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseDefaultLayout<IonAppLayoutFixture>();
        builder.UseRouter(router => router.MapRoute("/", page));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        return app;
    }

    private static IonOverlayManager Overlays(MikoAppContext app)
        => (IonOverlayManager)app.Services.GetService(typeof(IonOverlayManager))!;

    private static Element? OverlayHost(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("ion-overlay-host").SingleOrDefault();

    private static bool IsInside(Element element, Element ancestor)
    {
        for (var node = element; node is not null; node = node.Parent)
            if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }

    /// <summary>
    /// Problem 1: <c>RouteView</c> built the page before the layout, so the page never saw the
    /// <see cref="IonOverlayContext"/> that the layout's <see cref="IonApp"/> cascades, and every
    /// declarative overlay rendered inline where it was declared.
    /// </summary>
    [Fact]
    public void DeclarativeOverlayOnRoutedPage_RendersInIonAppOverlayLayer()
    {
        var app = BuildApp(typeof(DeclarativeModalPageFixture));

        Overlays(app).Count.ShouldBe(1);

        var root = app.Engine.GetRoot()!;
        var host = OverlayHost(app).ShouldNotBeNull();
        var modal = root.FindByClass("ion-modal").ShouldHaveSingleItem();
        IsInside(modal, host).ShouldBeTrue("the modal must be mounted under IonOverlayHost");

        var body = root.FindByClass("page-body").ShouldHaveSingleItem();
        body.FindByClass("ion-modal").ShouldBeEmpty();
        root.FindByClass("routed-modal-content").ShouldHaveSingleItem();
    }

    /// <summary>
    /// Moving declarative overlays into the layer must not lose their styles. The theme scope class
    /// used to be stamped only on the component's root — for a portaled overlay that is the empty
    /// placeholder left where it was declared — so none of the modal's scoped rules matched the
    /// host drawn in the layer.
    /// </summary>
    [Fact]
    public void DeclarativeOverlayInTheLayer_KeepsItsComponentStyles()
    {
        var app = BuildApp(typeof(DeclarativeModalPageFixture));
        app.Engine.Render(_canvas);

        var modal = app.Engine.GetRoot()!.FindByClass("ion-modal").Single();
        modal.Class!.ShouldContain("ion-theme-");
        var box = FindBox(app.Engine.GetCurrentLayout()!, modal).ShouldNotBeNull();
        box.ComputedStyle.Position.ShouldBe(Miko.Common.Position.Fixed, "the modal host rule applies");
    }

    private static Miko.Layout.LayoutBox? FindBox(Miko.Layout.LayoutBox box, Element element)
    {
        if (ReferenceEquals(box.Element, element)) return box;
        foreach (var child in box.Children)
            if (FindBox(child, element) is { } found) return found;
        return null;
    }

    /// <summary>
    /// Problem 1 also covers <see cref="IonSelect"/>, which opens its alert / popover / action
    /// sheet through the same context.
    /// </summary>
    [Fact]
    public void IonSelectOnRoutedPage_OpensInIonAppOverlayLayer()
    {
        var app = BuildApp(typeof(SelectPageFixture));
        var root = app.Engine.GetRoot()!;

        var select = root.FindByClass("ion-select").ShouldHaveSingleItem();
        select.OnClick!.Invoke(new MouseEventArgs { Target = select });

        Overlays(app).Count.ShouldBe(1);
        var alert = app.Engine.GetRoot()!.FindByClass("ion-alert").ShouldHaveSingleItem();
        IsInside(alert, OverlayHost(app).ShouldNotBeNull()).ShouldBeTrue();
    }

    /// <summary>
    /// Problem 2: a page with two root nodes directly under <see cref="IonApp"/> lost the overlay
    /// host on its first re-render. <c>RenderTreeBuilder</c> reused the page's own
    /// <see cref="Core.DomElements.FragmentElement"/> as the carrier of the app's content, so the
    /// host was attached into the page's fragment and replaced along with it.
    /// </summary>
    [Fact]
    public void MultiRootPageRerender_KeepsTheOverlayHost()
    {
        var app = BuildApp(typeof(MultiRootPageFixture));
        OverlayHost(app).ShouldNotBeNull();

        var toggle = app.Engine.GetRoot()!.FindByClass("toggle-menu").Single();
        toggle.OnClick!.Invoke(new MouseEventArgs { Target = toggle });
        app.Engine.Render(_canvas);

        OverlayHost(app).ShouldNotBeNull("re-rendering the page must not remove IonOverlayHost");
        app.Engine.GetRoot()!.FindByClass("ion-menu-host-open").ShouldHaveSingleItem();
    }

    /// <summary>
    /// The visible consequence of problem 2: once the host was gone, overlays created through a
    /// controller never appeared.
    /// </summary>
    [Fact]
    public async Task MultiRootPageRerender_ControllerOverlaysStillAppear()
    {
        var app = BuildApp(typeof(MultiRootPageFixture));
        var toggle = app.Engine.GetRoot()!.FindByClass("toggle-menu").Single();
        toggle.OnClick!.Invoke(new MouseEventArgs { Target = toggle });
        app.Engine.Render(_canvas);

        var alerts = (IonAlertController)app.Services.GetService(typeof(IonAlertController))!;
        var alert = await alerts.CreateAsync(new IonAlertOptions { Header = "Hello" });
        await alert.PresentAsync();

        var shown = app.Engine.GetRoot()!.FindByClass("ion-alert").ShouldHaveSingleItem();
        IsInside(shown, OverlayHost(app).ShouldNotBeNull()).ShouldBeTrue();
    }

    /// <summary>
    /// An overlay without <c>@bind-IsOpen</c> closes itself by re-rendering with
    /// <c>IsOpen = false</c>. Rendered inline that was enough; in the overlay layer the entry the
    /// first render registered has to be withdrawn as well, or the alert stays on screen.
    /// </summary>
    [Fact]
    public void SelfDismissedOverlayOnRoutedPage_LeavesTheOverlayLayer()
    {
        var app = BuildApp(typeof(SelfDismissPageFixture));
        Overlays(app).Count.ShouldBe(1);

        var ok = app.Engine.GetRoot()!.FindByClass("alert-button").Single();
        ok.OnClick!.Invoke(new MouseEventArgs { Target = ok });
        app.Engine.Render(_canvas);

        Overlays(app).Count.ShouldBe(0);
        app.Engine.GetRoot()!.FindByClass("ion-alert").ShouldBeEmpty();
    }
}
