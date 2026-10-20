using Miko.Animation;
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
/// ISSUE-153: the menu backdrop never faded out. The page re-renders to set IsOpen to false, which
/// builds a new IonMenu whose "backdrop still mounted" flag started false, so the backdrop went
/// straight to display:none. Driven through the real pipeline with the sidemenu template's shape:
/// a routed page under <c>&lt;IonApp&gt;@Body&lt;/IonApp&gt;</c> owning the open state, frames
/// pumped as the host's render loop does.
/// </summary>
public class IonMenuFadeTests : IDisposable
{
    private const int W = 390;
    private const int H = 844;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public IonMenuFadeTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private MikoAppContext BuildApp()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseDefaultLayout<IonAppLayoutFixture>();
        builder.UseRouter(router => router.MapRoute("/", typeof(MultiRootPageFixture)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        return app;
    }

    private void Frame(MikoAppContext app, float dt = 1f / 60f)
        => app.Controller.RenderFrame(_canvas, W, H, dt, c => app.Engine.Render(c));

    private static Element Backdrop(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("ion-menu-backdrop").Single();

    private static void Toggle(MikoAppContext app)
    {
        var toggle = app.Engine.GetRoot()!.FindByClass("toggle-menu").Single();
        toggle.OnClick!.Invoke(new MouseEventArgs { Target = toggle });
    }

    [Fact]
    public void ClosingTheMenu_FadesTheBackdropOut_AndLetsTapsThrough()
    {
        var app = BuildApp();
        var animations = (AnimationManager)app.Services.GetService(typeof(AnimationManager))!;
        Frame(app);

        Toggle(app);
        for (var i = 0; i < 30; i++) Frame(app);   // fully open
        Backdrop(app).ShouldHaveClass("ion-menu-backdrop-open");

        // Close through the backdrop, as a user does: OnClose re-renders the page.
        var backdrop = Backdrop(app);
        backdrop.OnClick!.Invoke(new MouseEventArgs { Target = backdrop });
        Frame(app);

        // A new IonMenu instance, and the backdrop is fading — still laid out (so it is painted
        // at its animated opacity), not dropped to display:none. A tracked transition alone
        // proves nothing: the old code tracked one too, on a backdrop that was no longer drawn.
        var fading = Backdrop(app);
        fading.ShouldNotHaveClass("ion-menu-backdrop-open");
        animations.HasActiveTransition(fading, "Opacity").ShouldBeTrue("the backdrop fades out");
        FindBox(app.Engine.GetCurrentLayout()!, fading).ShouldNotBeNull("the fading backdrop is still laid out");

        // While it fades the host already lets taps through to the page.
        app.Engine.GetRoot()!.FindByClass("ion-menu-host").Single().ShouldHaveClass("ion-menu-host-idle");
        var hit = app.Engine.HitTest(W - 10, H / 2f);
        hit.ShouldNotBeNull();
        IsInside(hit!, fading).ShouldBeFalse("taps pass through the fading backdrop");
    }

    private static Miko.Layout.LayoutBox? FindBox(Miko.Layout.LayoutBox box, Element element)
    {
        if (ReferenceEquals(box.Element, element)) return box;
        foreach (var child in box.Children)
            if (FindBox(child, element) is { } found) return found;
        return null;
    }

    private static bool IsInside(Element element, Element ancestor)
    {
        for (var node = element; node is not null; node = node.Parent)
            if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }
}
