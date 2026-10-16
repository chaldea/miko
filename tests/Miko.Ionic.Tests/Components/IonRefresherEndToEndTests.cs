using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Ionic.Tests.Fixtures;
using Miko.Platform;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Pull-to-refresh through the real app pipeline (issues/ion-refresher.md problem 2): an app built by
/// <see cref="MikoAppBuilder"/> with the Ionic style registry, the demo page's component tree
/// (<see cref="RefresherPageFixture"/>), and touches entering through
/// <see cref="MikoInteractionController"/> exactly as the Android host forwards them, with a frame
/// rendered after every move. The component-level tests in <see cref="IonRefresherTests"/> render
/// against the unscoped global sheet and never re-render the page, which is how all three defects
/// below went unnoticed.
/// </summary>
public class IonRefresherEndToEndTests : IDisposable
{
    private const int W = 400;
    private const int H = 800;

    // Below the 56px md header, inside the content.
    private const float PressX = 200;
    private const float PressY = 300;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public IonRefresherEndToEndTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void Pulling_ShowsTheIndicatorInTheGapAboveTheContent()
    {
        // The refresher used to overwrite its host's class on every state change, dropping the
        // ion-theme-* scope class every Ionic rule matches through. None of its styles applied: the
        // host stayed 0px tall and the pulling block display:none, so the content moved down over a
        // blank gap.
        var (app, _) = BuildApp();
        var contentTop = BorderBox(InnerScroll(app)).Top;

        Press(app, PressY);
        MoveTo(app, PressY + 50);

        var refresher = Refresher(app);
        refresher.ShouldHaveClass("refresher-pulling");
        refresher.Class!.ShouldContain("ion-theme-");
        refresher.ShouldHaveClass("ion-slot-fixed");
        BorderBox(refresher).Height.ShouldBe(60f, 0.5f);
        Pulling(app).LayoutBox!.ComputedStyle.Display.ShouldBe(Display.Block);
        TranslateY(InnerScroll(app)).ShouldBe(50f, 0.5f);

        // The arrow is actually painted in the uncovered strip.
        DarkPixels(contentTop, contentTop + 50).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Releasing_HoldsTheRefreshUntilTheHandlerCompletes_AcrossThePageRerender()
    {
        // OnRefresh's receiver is the page, and an async handler re-renders it the moment it yields,
        // which rebuilds IonContent and IonRefresher as new instances. The refreshing state lived on
        // the old instance, so the content snapped back and the spinner never showed while the app
        // was still loading.
        var (app, probe) = BuildApp();
        var innerBefore = InnerScroll(app);

        Press(app, PressY);
        MoveTo(app, PressY + 90);
        Release(app, PressY + 90);
        for (var i = 0; i < 30; i++) Frame(app);

        probe.Refreshes.ShouldBe(1);
        InnerScroll(app).ShouldNotBeSameAs(innerBefore, "the page must have re-rendered for this to prove anything");
        Refresher(app).ShouldHaveClass("refresher-refreshing");
        TranslateY(InnerScroll(app)).ShouldBe(60f, 0.5f);
        Refreshing(app).LayoutBox!.ComputedStyle.Display.ShouldBe(Display.Block);

        probe.Release();
        PumpUntil(app, () => !Refresher(app).HasClass("refresher-active"));

        TranslateY(InnerScroll(app)).ShouldBe(0f);
        Headings(app).First().ShouldBe("New 1");
        probe.Refreshes.ShouldBe(1);
    }

    [Fact]
    public void Complete_SlidesTheContentBackRatherThanJumping()
    {
        var (app, probe) = BuildApp();
        Press(app, PressY);
        MoveTo(app, PressY + 90);
        Release(app, PressY + 90);
        for (var i = 0; i < 30; i++) Frame(app);

        probe.Release();
        PumpUntil(app, () => Refresher(app).HasClass("refresher-completing"));

        // refresher.tsx complete(): closes over 280ms after a 120ms delay.
        var samples = new List<float>();
        for (var i = 0; i < 30; i++)
        {
            app.Engine.Tick(1f / 60f, _canvas);
            samples.Add(ComputedTranslateY(InnerScroll(app)));
        }

        samples.ShouldContain(y => y > 1f && y < 59f, "the content must pass through intermediate offsets");
        samples[^1].ShouldBe(0f, 0.5f);
    }

    [Fact]
    public void AJitteryFirstMove_DoesNotEndThePull()
    {
        // A real finger's first move events are often sideways or flat (dy = 0). The gesture used to
        // be judged on the very first move and given up for the rest of the press, so on a device
        // the pull usually never started at all.
        var (app, probe) = BuildApp();

        Press(app, PressY);
        MoveTo(app, PressY, x: PressX + 1);
        MoveTo(app, PressY + 2, x: PressX - 1);
        MoveTo(app, PressY + 90);
        Release(app, PressY + 90);

        probe.Refreshes.ShouldBe(1);
        Refresher(app).ShouldHaveClass("refresher-refreshing");
    }

    [Fact]
    public void AShortPull_SlidesBack_AndTheNextPullStillWorks_EvenIfNoFrameRanTheReset()
    {
        // A release short of PullMin cancels; the refresher returns to inactive on a timer whose
        // continuation runs on the render thread. An idle mobile host draws no frame to run it, so
        // the next press must not be held off by a reset that never happened.
        var (app, probe) = BuildApp();

        Press(app, PressY);
        MoveTo(app, PressY + 30);
        Release(app, PressY + 30);
        Refresher(app).ShouldHaveClass("refresher-cancelling");
        probe.Refreshes.ShouldBe(0);

        Thread.Sleep(700); // past the reset delay, with no frame rendered meanwhile

        Press(app, PressY);
        MoveTo(app, PressY + 90);
        Release(app, PressY + 90);

        probe.Refreshes.ShouldBe(1);
        Refresher(app).ShouldHaveClass("refresher-refreshing");
    }

    [Fact]
    public void ASecondRefresh_WorksAfterTheFirstCompletes()
    {
        var (app, probe) = BuildApp();

        for (var round = 1; round <= 2; round++)
        {
            Press(app, PressY);
            MoveTo(app, PressY + 90);
            Release(app, PressY + 90);
            probe.Refreshes.ShouldBe(round);

            probe.Release();
            PumpUntil(app, () => !Refresher(app).HasClass("refresher-active"));
            Headings(app).First().ShouldBe($"New {round}");
        }
    }

    [Fact]
    public void PullingUpward_ScrollsAndLeavesTheRefresherAlone()
    {
        var (app, probe) = BuildApp();

        Press(app, PressY);
        MoveTo(app, PressY - 40);
        MoveTo(app, PressY - 80);
        Release(app, PressY - 80);

        probe.Refreshes.ShouldBe(0);
        Refresher(app).ShouldNotHaveClass("refresher-active");
        TranslateY(InnerScroll(app)).ShouldBe(0f);
    }

    // ================================ helpers ================================

    private (MikoAppContext App, RefresherProbe Probe) BuildApp()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.Services.AddSingleton<RefresherProbe>();
        builder.UseRouter(router => router.MapRoute("/", typeof(RefresherPageFixture)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        Frame(app);
        return (app, app.Services.GetRequiredService<RefresherProbe>());
    }

    private void Frame(MikoAppContext app)
        => app.Controller.RenderFrame(_canvas, W, H, 1f / 60f, c => app.Engine.Render(c));

    private void Press(MikoAppContext app, float y)
    {
        app.Controller.OnPointerDown(PressX, y, MouseButton.Left, PointerType.Touch, 7);
        Frame(app);
    }

    /// <summary>Moves in small steps with a frame after each, the way a finger reaches the host.</summary>
    private void MoveTo(MikoAppContext app, float y, float x = PressX)
    {
        var from = _lastY ?? y;
        var steps = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(y - from) / 8f));
        for (var i = 1; i <= steps; i++)
        {
            app.Controller.OnPointerMove(x, from + (y - from) * i / steps);
            Frame(app);
        }
        _lastY = y;
    }

    private float? _lastY;

    private void Release(MikoAppContext app, float y)
    {
        app.Controller.OnPointerUp(PressX, y, MouseButton.Left, PointerType.Touch, 7);
        _lastY = null;
        Frame(app);
    }

    /// <summary>
    /// Renders frames until <paramref name="done"/> holds. The refresh handler's continuation and
    /// the refresher's return to inactive both run off timers and are drained by frames; waiting on
    /// the outcome (with a generous ceiling) rather than a fixed sleep keeps this robust on a slow
    /// CI machine.
    /// </summary>
    private void PumpUntil(MikoAppContext app, Func<bool> done, int timeoutMs = 5000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!done())
        {
            if (Environment.TickCount64 > deadline) throw new TimeoutException("condition not reached");
            Thread.Sleep(10);
            Frame(app);
        }
    }

    private static Element Refresher(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("ion-refresher").ShouldHaveSingleItem();

    private static Element InnerScroll(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("inner-scroll").ShouldHaveSingleItem();

    private static Element Pulling(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("refresher-pulling").Single(e => e.Class == "refresher-pulling");

    private static Element Refreshing(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("refresher-refreshing").Single(e => e.Class == "refresher-refreshing");

    private static IEnumerable<string?> Headings(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByTagName("h2").Select(h => h.TextContent);

    private static RectF BorderBox(Element element)
    {
        element.LayoutBox.ShouldNotBeNull();
        return element.LayoutBox.BoxModel.BorderBox;
    }

    /// <summary>The offset the refresher wrote onto the element's inline style.</summary>
    private static float TranslateY(Element element)
        => element.Style?.Transform?.Value.Functions
            .OfType<TransformFunction.TranslateY>().LastOrDefault()?.Y.Value ?? 0f;

    /// <summary>The offset in effect this frame, including a running close animation.</summary>
    private static float ComputedTranslateY(Element element)
        => element.LayoutBox?.ComputedStyle?.Transform?.Functions
            .OfType<TransformFunction.TranslateY>().LastOrDefault()?.Y.Value ?? 0f;

    private int DarkPixels(float top, float bottom)
    {
        var count = 0;
        for (var y = (int)MathF.Ceiling(top) + 4; y < (int)bottom; y++)
        {
            for (var x = 0; x < W; x++)
            {
                var c = _bitmap.GetPixel(x, y);
                if (c.Red < 100 && c.Green < 100 && c.Blue < 100) count++;
            }
        }
        return count;
    }
}
