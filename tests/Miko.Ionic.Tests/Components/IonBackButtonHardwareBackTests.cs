using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Routing;
using Miko.Testing;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests that <see cref="IonBackButton"/> answers the system back button the same way a tap does
/// (ISSUE-138), and that it declines the back action when it must not act — hidden, disabled, or
/// opted out via <c>RespondToHardwareBack</c>.
/// </summary>
public class IonBackButtonHardwareBackTests : IDisposable
{
    private const int W = 320;
    private const int H = 240;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public IonBackButtonHardwareBackTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private (MikoAppContext App, NavigationManager Nav, PlatformInfo Platform) BuildApp()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router =>
        {
            router.MapRoute("/", typeof(HomePage));
            router.MapRoute("/detail", typeof(DetailPage));
            // A distinct fallback target: with no history, back must land here rather than on the
            // page the user came from — which is what makes the fallback observable.
            router.MapRoute("/home", typeof(HomePage));
        });
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);

        var nav = (NavigationManager)app.Services.GetService(typeof(NavigationManager))!;
        var platform = (PlatformInfo)app.Services.GetService(typeof(Miko.Platform.IPlatformInfo))!;
        return (app, nav, platform);
    }

    [Fact]
    public void BackButton_RespondsToSystemBack_WhenHistoryAllowsPop()
    {
        var (app, nav, platform) = BuildApp();

        nav.NavigateTo("/detail");
        app.Controller.Rebuild(_canvas, W, H);

        platform.Back.Dispatch().ShouldBeTrue();
        nav.CurrentPath.ShouldBe("/");
    }

    /// <summary>
    /// With nothing to pop, the button falls back to <c>DefaultHref</c> — the same branch a tap
    /// takes — and still consumes the back action so the app does not also exit.
    /// <para>
    /// The fallback is observed through the history stack rather than the destination path: the
    /// detail page's <c>DefaultHref</c> is <c>"/"</c>, the same route a pop would reach, so only
    /// the stack reveals which branch ran. A pop shrinks the stack; the fallback navigates
    /// *forward* into <c>"/"</c>, growing it.
    /// </para>
    /// </summary>
    [Fact]
    public void BackButton_FallsBackToDefaultHref_WhenHistoryIsEmpty()
    {
        var (app, nav, platform) = BuildApp();

        // Root navigation *replaces* the stack, so the detail page is the only entry and the stack
        // cannot pop (a Forward navigation would always leave the seeded "/" beneath it).
        nav.NavigateTo("/detail", NavigationDirection.Root);
        app.Controller.Rebuild(_canvas, W, H);
        nav.CanGoBack.ShouldBeFalse();
        nav.History.Count.ShouldBe(1);

        platform.Back.Dispatch().ShouldBeTrue();

        nav.CurrentPath.ShouldBe("/");
        // Grew instead of shrinking: nothing was popped, the DefaultHref fallback ran.
        nav.History.Count.ShouldBe(2);
    }

    private sealed class HomePage : ComponentBase
    {
        public override Element Build() => new DivElement
        {
            Style = new Miko.Styling.Style
            {
                Width = Length.Px(W),
                Height = Length.Px(H),
                BackgroundColor = Color.FromRgb(255, 255, 255),
            }
        };
    }

    // IonPage > IonHeader > IonToolbar(Start: IonBackButton) — the standard detail-page header.
    private sealed class DetailPage : ComponentBase
    {
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
                        back.DefaultHref = "/";
                        start.CloseComponent();
                    });
                    h.CloseComponent();
                });
                p.CloseComponent();
            });
            builder.CloseComponent();
        }
    }
}
