using Miko.Core;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Tests.Fixtures;
using Miko.Platform;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Scrolling the infinite scroll demo through the real app pipeline (issues/ion-infinite-scroll.md
/// problem 2: the page stuttered once the infinite scroll had loaded): an app built by
/// <see cref="MikoAppBuilder"/> with the Ionic style registry, the demo page's component tree
/// (<see cref="InfiniteScrollPageFixture"/>), and touches entering through
/// <see cref="MikoInteractionController"/> exactly as the Android host forwards them, with a frame
/// rendered after every move. Three defects added up to the stutter:
/// <list type="bullet">
/// <item>The scroll listener was an <c>@onscroll</c> EventCallback, which re-rendered the infinite
///   scroll after every scroll event — so every scrolled frame restyled and relaid out the whole page,
///   at a cost that grew with every row loaded.</item>
/// <item>OnInfinite re-renders the page as soon as the handler yields, which rebuilt the infinite
///   scroll as a fresh instance that had forgotten it was loading. Each further scroll event fired
///   OnInfinite again, so one threshold crossing loaded batch after batch.</item>
/// <item>The hidden loading spinner kept the host rendering every frame, forever.</item>
/// </list>
/// </summary>
public class IonInfiniteScrollEndToEndTests : IDisposable
{
    private const int W = 400;
    private const int H = 800;

    // Inside the content, below the 56px md header.
    private const float PressX = 200;
    private const float PressY = 600;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public IonInfiniteScrollEndToEndTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void Scrolling_DoesNotRerenderThePage_BeforeOrAfterALoad()
    {
        var (app, probe) = BuildApp();

        AssertScrollIsPaintOnly(app, "before any load");

        LoadOnce(app, probe);
        Rows(app).ShouldBe(20);

        AssertScrollIsPaintOnly(app, "after a load");
    }

    [Fact]
    public void OneThresholdCrossing_LoadsOneBatch_EvenAsThePageRerenders()
    {
        var (app, probe) = BuildApp();
        var hostBefore = Host(app);

        ScrollToBottom(app);
        probe.Loads.ShouldBe(1);

        // OnInfinite's receiver is the page, and the handler is still waiting, so the page has
        // re-rendered and the infinite scroll is a new instance. It must still know it is loading.
        Host(app).ShouldNotBeSameAs(hostBefore, "the page must have re-rendered for this to prove anything");
        Host(app).ShouldHaveClass("infinite-scroll-loading");

        // Keep scrolling inside the threshold band while the load is in flight.
        for (var i = 0; i < 3; i++)
        {
            Drag(app, -40);
            Drag(app, 40);
        }

        probe.Loads.ShouldBe(1, "a load in flight must not fire OnInfinite again");
        Host(app).ShouldHaveClass("infinite-scroll-loading");

        probe.Release();
        PumpUntil(app, () => !Host(app).HasClass("infinite-scroll-loading"));
        Rows(app).ShouldBe(20, "one threshold crossing loads exactly one batch");

        // Complete() re-armed it: the next crossing loads the next batch.
        ScrollToBottom(app);
        probe.Loads.ShouldBe(2);
    }

    [Fact]
    public void TheHiddenSpinner_LetsTheHostIdle_AndAnimatesOnlyWhileLoading()
    {
        var (app, probe) = BuildApp();
        app.Controller.HasPendingWork.ShouldBeFalse("the hidden spinner must not keep the host rendering");

        ScrollToBottom(app);
        probe.Loads.ShouldBe(1);
        for (var i = 0; i < 3; i++)
        {
            Frame(app);
            app.Controller.HasPendingWork.ShouldBeTrue("the visible spinner animates every frame");
        }

        probe.Release();
        PumpUntil(app, () => !Host(app).HasClass("infinite-scroll-loading"));
        Frame(app);
        app.Controller.HasPendingWork.ShouldBeFalse("hidden again once the load completes");
    }

    // ================================ helpers ================================

    private (MikoAppContext App, InfiniteScrollProbe Probe) BuildApp()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.AddResourceAssembly(typeof(IonInfiniteScrollEndToEndTests).Assembly);
        builder.Services.AddSingleton<InfiniteScrollProbe>();
        builder.UseRouter(router => router.MapRoute("/", typeof(InfiniteScrollPageFixture)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        // Settle: image loads and the like finish within the first frames.
        PumpUntil(app, () => !app.Controller.HasPendingWork, timeoutMs: 1000, settleOnly: true);
        return (app, app.Services.GetRequiredService<InfiniteScrollProbe>());
    }

    /// <summary>
    /// Drags a short distance mid-list and checks that no scrolled frame touched the DOM: the page
    /// keeps its mutation version (so the layout cache is reused and the frame only repaints) and the
    /// infinite scroll keeps its host element.
    /// </summary>
    private void AssertScrollIsPaintOnly(MikoAppContext app, string when)
    {
        Drag(app, -30); // away from the bottom, so the drag below stays clear of the threshold
        var scroller = InnerScroll(app);
        var top = scroller.LayoutBox!.ScrollTop;
        var host = Host(app);
        var version = app.Engine.Mutations.Version;

        Drag(app, 30);

        scroller.LayoutBox!.ScrollTop.ShouldNotBe(top, $"the drag must actually scroll ({when})");
        app.Engine.Mutations.Version.ShouldBe(version, $"scrolling must not mutate the page ({when})");
        Host(app).ShouldBeSameAs(host, $"scrolling must not re-render the infinite scroll ({when})");
    }

    private void LoadOnce(MikoAppContext app, InfiniteScrollProbe probe)
    {
        var loads = probe.Loads;
        ScrollToBottom(app);
        probe.Loads.ShouldBe(loads + 1);
        probe.Release();
        PumpUntil(app, () => !Host(app).HasClass("infinite-scroll-loading"));
    }

    private void Frame(MikoAppContext app)
        => app.Controller.RenderFrame(_canvas, W, H, 1f / 60f, c => app.Engine.Render(c));

    /// <summary>
    /// A finger drag of <paramref name="distance"/> px (positive scrolls down), in 8px moves with a
    /// frame after each, the way a finger reaches the host.
    /// </summary>
    private void Drag(MikoAppContext app, float distance)
    {
        // Leave room on screen for the whole drag.
        var startY = distance > 0 ? PressY : PressY - 400;
        app.Controller.OnPointerDown(PressX, startY, MouseButton.Left, PointerType.Touch, 7);
        Frame(app);
        var steps = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(distance) / 8f));
        for (var i = 1; i <= steps; i++)
        {
            app.Controller.OnPointerMove(PressX, startY - distance * i / steps);
            Frame(app);
        }
        app.Controller.OnPointerUp(PressX, startY - distance, MouseButton.Left, PointerType.Touch, 7);
        Frame(app);
    }

    /// <summary>Drags until the content stops moving, i.e. it rests at the bottom.</summary>
    private void ScrollToBottom(MikoAppContext app)
    {
        for (var i = 0; i < 10; i++)
        {
            var top = InnerScroll(app).LayoutBox!.ScrollTop;
            Drag(app, 400);
            if (InnerScroll(app).LayoutBox!.ScrollTop == top) return;
        }
    }

    /// <summary>
    /// Renders frames until <paramref name="done"/> holds. The load handler's continuation is posted
    /// to the render thread and drained by frames.
    /// </summary>
    private void PumpUntil(MikoAppContext app, Func<bool> done, int timeoutMs = 5000, bool settleOnly = false)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!done())
        {
            if (Environment.TickCount64 > deadline)
            {
                if (settleOnly) return;
                throw new TimeoutException("condition not reached");
            }
            Thread.Sleep(5);
            Frame(app);
        }
    }

    private static Element Host(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("ion-infinite-scroll").ShouldHaveSingleItem();

    private static Element InnerScroll(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("inner-scroll").ShouldHaveSingleItem();

    private static int Rows(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByTagName("h2").Count();
}
