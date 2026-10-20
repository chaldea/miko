using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Routing;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// The back button through the real app pipeline (ISSUE-138): an app built by
/// <see cref="MikoAppBuilder"/>, components resolving <see cref="IPlatformService"/> from DI, and
/// the desktop back key (Escape) entering through <see cref="MikoInteractionController"/> exactly
/// as <c>SilkDesktopHost</c> forwards it.
/// </summary>
public class BackButtonEndToEndTests : IDisposable
{
    private const int W = 320;
    private const int H = 240;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public BackButtonEndToEndTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
        ModalPage.Roles.Clear();
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private (MikoAppContext App, NavigationManager Nav, IPlatformService Platform) BuildApp()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router =>
        {
            router.MapRoute("/", typeof(HomePage));
            router.MapRoute("/modal", typeof(ModalPage));
            router.MapRoute("/detail", typeof(DetailPage));
        });
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);

        var nav = (NavigationManager)app.Services.GetService(typeof(NavigationManager))!;
        var platform = (IPlatformService)app.Services.GetService(typeof(IPlatformService))!;
        return (app, nav, platform);
    }

    [Fact]
    public void Escape_ClosesOpenModal_ThenFallsThrough()
    {
        var (app, nav, _) = BuildApp();
        nav.NavigateTo("/modal");
        app.Controller.Rebuild(_canvas, W, H);

        app.Controller.OnKeyDown(MikoKey.Escape, MikoKeyModifiers.None).ShouldBeTrue();
        ModalPage.Roles.ShouldBe(new[] { "back" });

        // The modal is closed: the next Escape is nobody's and must not fire a second dismiss.
        app.Controller.OnKeyDown(MikoKey.Escape, MikoKeyModifiers.None).ShouldBeFalse();
        ModalPage.Roles.Count.ShouldBe(1);
    }

    /// <summary>
    /// Navigating away discards the page's components; their back registrations must go with
    /// them, or a stale back button from a page no longer on screen keeps answering back.
    /// <para>
    /// The leaving page stays alive while the pop transition paints it, and is torn down when the
    /// transition ends — so frames are pumped the way the host's render loop does.
    /// </para>
    /// </summary>
    [Fact]
    public void Navigation_ReleasesRegistrationsOfTheLeftPage()
    {
        var (app, nav, platform) = BuildApp();
        nav.NavigateTo("/detail");
        app.Controller.Rebuild(_canvas, W, H);
        platform.Back.Count.ShouldBe(1);

        app.Controller.OnBackButton().ShouldBeTrue();
        nav.CurrentPath.ShouldBe("/");

        for (var frame = 0; frame < 120 && platform.Back.Count > 0; frame++)
            app.Controller.RenderFrame(_canvas, W, H, 1f / 30f, c => app.Engine.Render(c));

        platform.Back.Count.ShouldBe(0);
    }

    /// <summary>
    /// Mid-transition the leaving page's back button is still registered. At the root with its
    /// DefaultHref already current it has nothing to do, so it must not swallow the press.
    /// </summary>
    [Fact]
    public void LeavingBackButton_AtRoot_DoesNotSwallowBack()
    {
        var (app, nav, _) = BuildApp();
        nav.NavigateTo("/detail");
        app.Controller.Rebuild(_canvas, W, H);

        app.Controller.OnBackButton().ShouldBeTrue();
        nav.CurrentPath.ShouldBe("/");

        // DetailPage's DefaultHref is "/" — already current — and the stack cannot pop.
        app.Controller.OnBackButton().ShouldBeFalse();
        nav.CurrentPath.ShouldBe("/");
    }

    /// <summary>Repeated rebuilds of the same page must not stack duplicate registrations.</summary>
    [Fact]
    public void Rebuild_DoesNotAccumulateRegistrations()
    {
        var (app, nav, platform) = BuildApp();
        nav.NavigateTo("/detail");

        for (var i = 0; i < 3; i++)
            app.Controller.Rebuild(_canvas, W, H);

        platform.Back.Count.ShouldBe(1);
    }

    private sealed class HomePage : ComponentBase
    {
        public override Element Build() => new DivElement
        {
            Style = new Miko.Styling.Style { Width = Length.Px(W), Height = Length.Px(H) }
        };
    }

    // A page that opens a modal and binds its open state the way `@bind-IsOpen` does, recording
    // every dismiss role. The binding matters: the dismiss callbacks re-render this page (their
    // receiver), and an unbound `IsOpen = true` would simply re-open the modal.
    private sealed class ModalPage : ComponentBase
    {
        public static readonly List<string?> Roles = new();

        private bool _open = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var modal = builder.OpenComponent<IonModal>();
            modal.IsOpen = _open;
            modal.IsOpenChanged = EventCallback.Factory.Create<bool>(this, open => _open = open);
            modal.ChildContent = (RenderFragment)(b => b.AddContent("Body"));
            modal.OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => Roles.Add(e.Role));
            builder.CloseComponent();
        }
    }

    // IonPage > IonHeader > IonToolbar(Start: IonBackButton) — the standard detail-page header.
    private class DetailPage : ComponentBase
    {
        protected virtual string? DefaultHref => "/";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var page = builder.OpenComponent<IonPage>();
            page.ChildContent = (RenderFragment)(p =>
            {
                var header = p.OpenComponent<IonHeader>();
                header.ChildContent = (RenderFragment)(h =>
                {
                    var toolbar = h.OpenComponent<IonToolbar>();
                    toolbar.Start = (RenderFragment)(start =>
                    {
                        var back = start.OpenComponent<IonBackButton>();
                        back.DefaultHref = DefaultHref;
                        start.CloseComponent();
                    });
                    h.CloseComponent();
                });
                p.CloseComponent();
            });
            builder.CloseComponent();
        }
    }

    /// <summary>The detail header with a bare <c>&lt;IonBackButton /&gt;</c> (no DefaultHref).</summary>
    private sealed class BareBackButtonPage : DetailPage
    {
        protected override string? DefaultHref => null;
    }

    private (MikoAppContext App, NavigationManager Nav) BuildAppWithBarePage()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router =>
        {
            router.MapRoute("/", typeof(BareBackButtonPage));
            router.MapRoute("/bare", typeof(BareBackButtonPage));
        });
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        return (app, (NavigationManager)app.Services.GetService(typeof(NavigationManager))!);
    }

    /// <summary>
    /// ISSUE-153: without DefaultHref the button stayed hidden even with history to go back to.
    /// Ionic shows it on any page that can go back (core.scss <c>.can-go-back</c>).
    /// </summary>
    [Fact]
    public void BareBackButton_IsShownOnlyWhenThereIsHistory()
    {
        var (app, nav) = BuildAppWithBarePage();
        app.Engine.GetRoot()!.FindByClass("ion-back-button").Single().ShouldNotHaveClass("show-back-button");

        nav.NavigateTo("/bare");
        app.Controller.Rebuild(_canvas, W, H);

        var back = app.Engine.GetRoot()!.FindByClass("ion-back-button")
            .Single(b => b.HasClass("show-back-button"));
        back.ShouldNotBeNull();

        app.Controller.OnBackButton().ShouldBeTrue("the shown button pops the history");
        nav.CurrentPath.ShouldBe("/");
    }

    /// <summary>IonPage &gt; IonContent &gt; IonBreadcrumbs &gt; IonBreadcrumb(Href) ×2.</summary>
    private sealed class BreadcrumbPage : ComponentBase
    {
        internal static int Clicks;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var page = builder.OpenComponent<IonPage>();
            page.ChildContent = p =>
            {
                var content = p.OpenComponent<IonContent>();
                content.ChildContent = c =>
                {
                    var crumbs = c.OpenComponent<IonBreadcrumbs>();
                    crumbs.ChildContent = b =>
                    {
                        var home = b.OpenComponent<IonBreadcrumb>();
                        home.Href = "/";
                        home.Class = "crumb-home";
                        home.OnClick = EventCallback.Factory.Create(this, () => Clicks++);
                        home.ChildContent = t => t.AddContent("Home");
                        b.CloseComponent();
                        var here = b.OpenComponent<IonBreadcrumb>();
                        here.Href = "/crumbs";
                        here.ChildContent = t => t.AddContent("Here");
                        b.CloseComponent();
                    };
                    c.CloseComponent();
                };
                p.CloseComponent();
            };
            builder.CloseComponent();
        }
    }

    /// <summary>
    /// ISSUE-153: a breadcrumb's Href did not navigate and there was no click event. Ionic opens
    /// the URL through the router (breadcrumb.tsx openURL).
    /// </summary>
    [Fact]
    public void BreadcrumbHref_NavigatesAndRaisesOnClick()
    {
        BreadcrumbPage.Clicks = 0;
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router =>
        {
            router.MapRoute("/", typeof(HomePage));
            router.MapRoute("/crumbs", typeof(BreadcrumbPage));
        });
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        var nav = (NavigationManager)app.Services.GetService(typeof(NavigationManager))!;
        nav.NavigateTo("/crumbs");
        app.Controller.Rebuild(_canvas, W, H);

        var link = app.Engine.GetRoot()!.FindByClass("crumb-home").Single().FindByClass("breadcrumb-native").Single();
        link.OnClick!.Invoke(new Miko.Events.MouseEventArgs { Target = link });

        BreadcrumbPage.Clicks.ShouldBe(1);
        nav.CurrentPath.ShouldBe("/");
    }
}
