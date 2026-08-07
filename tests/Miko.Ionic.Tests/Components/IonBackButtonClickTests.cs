using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Routing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Controller-level regression tests for <see cref="IonBackButton"/> default navigation
/// (issues/ion-animation): drives a full app context (router + Ionic stylesheet + interaction
/// controller) and simulates a real pointer click on the back button. Reported bug: in ios
/// mode the click did nothing (no transition, no navigation); md worked.
/// </summary>
public class IonBackButtonClickTests : IDisposable
{
    private const float W = 390;
    private const float H = 844;

    private readonly SKBitmap _bitmap = new((int)W, (int)H);
    private readonly SKCanvas _canvas;

    public IonBackButtonClickTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private (MikoAppContext App, NavigationManager Nav) BuildApp(HostPlatform platform)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = platform);
        builder.UseRouter(router =>
        {
            router.MapRoute("/", typeof(HomePage));
            router.MapRoute("/detail", typeof(DetailPage));
        });
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        var nav = (NavigationManager)app.Services.GetService(typeof(NavigationManager))!;
        return (app, nav);
    }

    [Theory]
    [InlineData(HostPlatform.Android)]
    [InlineData(HostPlatform.Ios)]
    public void BackButton_Click_PopsBackToHome(HostPlatform platform)
    {
        var (app, nav) = BuildApp(platform);

        nav.NavigateTo("/detail");
        app.Controller.Rebuild(_canvas, W, H);

        var root = app.Engine.GetRoot()!;
        root.FindByClass("button-native").Single(); // rendered

        // Locate the click point by probing the public hit test (Element.LayoutBox is internal):
        // scan the header band for a point whose hit element is the back button's native surface.
        float? cx = null, cy = null;
        for (float y = 4; y < 80 && cx == null; y += 4)
        for (float x = 4; x < W / 2f; x += 4)
        {
            var hit = app.Engine.HitTest(x, y);
            if (hit != null && hit.HasClass("button-native"))
            {
                cx = x;
                cy = y;
                break;
            }
        }
        cx.ShouldNotBeNull("back button should be hit-testable in the header band");

        app.Controller.OnPointerDown(cx!.Value, cy!.Value, MouseButton.Left);
        app.Controller.OnPointerUp(cx.Value, cy.Value, MouseButton.Left);

        nav.CurrentPath.ShouldBe("/");
    }

    private sealed class HomePage : ComponentBase
    {
        public override Element Build() => new DivElement
        {
            Style = new Miko.Styling.Style
            {
                Width = Miko.Common.Length.Px(W),
                Height = Miko.Common.Length.Px(H),
                BackgroundColor = Miko.Common.Color.FromRgb(255, 255, 255),
            }
        };
    }

    // Mirrors the demo's detail page: IonPage > IonHeader > IonToolbar(Start: IonBackButton, ChildContent: IonTitle).
    private sealed class DetailPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<IonPage>(0);
            builder.AddComponentParameter(1, nameof(IonPage.ChildContent), (RenderFragment)(page =>
            {
                page.OpenComponent<IonHeader>(0);
                page.AddComponentParameter(1, nameof(IonHeader.ChildContent), (RenderFragment)(header =>
                {
                    header.OpenComponent<IonToolbar>(0);
                    header.AddComponentParameter(1, nameof(IonToolbar.Start), (RenderFragment)(start =>
                    {
                        start.OpenComponent<IonBackButton>(0);
                        start.AddComponentParameter(1, nameof(IonBackButton.DefaultHref), "/");
                        start.CloseComponent();
                    }));
                    header.AddComponentParameter(2, nameof(IonToolbar.ChildContent), (RenderFragment)(content =>
                    {
                        content.OpenComponent<IonTitle>(0);
                        content.AddComponentParameter(1, nameof(IonTitle.ChildContent),
                            (RenderFragment)(t => t.AddContent(0, "Detail")));
                        content.CloseComponent();
                    }));
                    header.CloseComponent();
                }));
                page.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
