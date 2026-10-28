using Miko.Common;
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
/// ISSUE-151: the Infinite Scroll demo with its rows in a <c>Virtualize</c>, through the real app
/// pipeline — <see cref="MikoAppBuilder"/> with the Ionic style registry, the demo's component tree
/// (<see cref="VirtualizedInfiniteScrollPageFixture"/>) and finger drags entering through
/// <see cref="MikoInteractionController"/> with a frame after every move, as the Android host does.
/// The list keeps the DOM at about a screenful of rows however many are loaded, never shows blank space
/// where rows should be, and still drives <c>IonInfiniteScroll</c>.
/// </summary>
public class VirtualizeEndToEndTests : IDisposable
{
    private const int W = 400;
    private const int H = 800;
    private const float PressX = 200;
    private const float PressY = 600;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public VirtualizeEndToEndTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void ALongList_RendersAboutAScreenful_AndNeverShowsAGap()
    {
        var (app, _) = BuildApp<VirtualizedInfiniteScrollPageFixture>(rows: 500);
        Items(app).Count.ShouldBeLessThan(20, "an 800px screen holds about ten 72px rows");

        for (int i = 0; i < 40; i++)
        {
            Drag(app, 600);
            AssertCovered(app, total: 500, $"drag {i}");
            Items(app).Count.ShouldBeLessThan(25, $"drag {i}");
        }

        for (int i = 0; i < 40; i++)
        {
            Drag(app, -600);
            AssertCovered(app, total: 500, $"drag back {i}");
        }
        Scroller(app).LayoutBox!.ScrollTop.ShouldBe(0f, 1f, "back at the top");
        Items(app)[0].FindByTagName("h2")[0].TextContent.ShouldBe("User 1");
    }

    [Fact]
    public void ScrollingWithinTheRenderedRows_OnlyRepaints()
    {
        var (app, _) = BuildApp<VirtualizedInfiniteScrollPageFixture>(rows: 500);
        Drag(app, 1500);

        // A 400px drag in 8px moves: most frames must not touch the tree; only the few where the window
        // moves (and its spacers are brought in step) do.
        int mutated = 0;
        app.Controller.OnPointerDown(PressX, PressY, MouseButton.Left, PointerType.Touch, 7);
        Frame(app);
        for (int i = 1; i <= 50; i++)
        {
            var version = app.Engine.Mutations.Version;
            app.Controller.OnPointerMove(PressX, PressY - 8 * i);
            Frame(app);
            if (app.Engine.Mutations.Version != version) mutated++;
        }
        app.Controller.OnPointerUp(PressX, PressY - 400, MouseButton.Left, PointerType.Touch, 7);
        Frame(app);

        mutated.ShouldBeLessThan(12, "most scroll frames are paint-only");
        mutated.ShouldBeGreaterThan(0, "the drag must move the window for this to prove anything");
    }

    [Fact]
    public void InfiniteScroll_KeepsLoading_AndTheDomStaysBounded()
    {
        var (app, probe) = BuildApp<VirtualizedInfiniteScrollPageFixture>(rows: 15);

        for (int load = 1; load <= 25; load++)
        {
            ScrollToBottom(app);
            probe.Loads.ShouldBe(load, $"load {load}: one threshold crossing, one load");
            probe.Release();
            PumpUntil(app, () => !InfiniteHost(app).HasClass("infinite-scroll-loading"));
            AssertCovered(app, total: 15 + 5 * load, $"after load {load}");
        }

        Items(app).Count.ShouldBeLessThan(25, "140 rows loaded, about a screenful rendered");

        ScrollToBottom(app);
        Items(app)[^1].FindByTagName("h2")[0].TextContent.ShouldBe("User 140", "the last row is reachable");
    }

    [Fact]
    public void TheVirtualizedList_LooksTheSame_AsThePlainList()
    {
        // Every row has the same height, so once one row is measured the spacers stand in exactly: the
        // virtualized list must lay out, style and draw its items like the plain list — the fragment host
        // and the spacers must change nothing about how Ionic styles the items (dividers, padding, lines).
        var plain = BuildApp<InfiniteScrollPageFixture>(rows: 300).App;
        var virtualized = BuildApp<VirtualizedInfiniteScrollPageFixture>(rows: 300).App;

        foreach (var delta in new[] { 0f, 1234f, 4000f, -2500f, 9000f })
        {
            if (delta != 0f)
            {
                plain.Engine.ScrollBy(PressX, PressY, 0, delta).ShouldBeTrue();
                virtualized.Engine.ScrollBy(PressX, PressY, 0, delta).ShouldBeTrue();
            }
            var expected = Snapshot(plain);
            var actual = Snapshot(virtualized);
            var when = $"after scrolling by {delta}px";

            Scroller(virtualized).LayoutBox!.ScrollTop.ShouldBe(Scroller(plain).LayoutBox!.ScrollTop, 0.01f, when);

            // Layout and computed styles, element by element, for every row both lists render.
            var plainRows = Items(plain).ToDictionary(e => e.FindByTagName("h2")[0].TextContent!);
            foreach (var row in Items(virtualized))
            {
                if (!plainRows.TryGetValue(row.FindByTagName("h2")[0].TextContent!, out var reference)) continue;
                var where = $"{when}, {row.FindByTagName("h2")[0].TextContent}";
                ScreenTop(virtualized, row).ShouldBe(ScreenTop(plain, reference), 0.05f, where);
                AssertSame(reference, row, where);
            }

            // Pixels: the plain list reaches the same offset by adding row heights one float at a time,
            // the virtualized one through a double prefix sum — at 9,000px they differ by about 0.015px
            // (the virtualized one being the exact multiple), which moves anti-aliased text edges by a
            // shade. Bitmaps are snapped to whole device pixels, so where that drift straddles a .5
            // boundary (an avatar at 271.499 in one list, 271.502 in the other) the image is drawn a whole
            // pixel apart: the avatars are left out of the pixel compare — AssertSame has already checked
            // that each sits at the same place in its row, to a hundredth of a pixel. Anything structural —
            // a missing divider, other padding, a blank row — differs by far more than a shade.
            var snapped = SnappedImageMask(virtualized);
            int differing = 0, worst = 0;
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] == actual[i] || snapped[i]) continue;
                differing++;
                worst = Math.Max(worst, ChannelDelta(expected[i], actual[i]));
            }
            worst.ShouldBeLessThan(64, when);
            differing.ShouldBeLessThan(expected.Length / 50, when);
        }
    }

    /// <summary>The screen pixels of the rows' images, grown by a pixel for their anti-aliased edges.</summary>
    private static bool[] SnappedImageMask(MikoAppContext app)
    {
        var mask = new bool[W * H];
        float scrollTop = Scroller(app).LayoutBox!.ScrollTop;
        foreach (var row in Items(app))
        {
            foreach (var image in row.FindByTagName("img"))
            {
                var rect = image.LayoutBox!.BoxModel.BorderBox;
                int x0 = Math.Max(0, (int)MathF.Floor(rect.Left) - 1), x1 = Math.Min(W - 1, (int)MathF.Ceiling(rect.Right) + 1);
                int y0 = Math.Max(0, (int)MathF.Floor(rect.Top - scrollTop) - 1), y1 = Math.Min(H - 1, (int)MathF.Ceiling(rect.Bottom - scrollTop) + 1);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        mask[y * W + x] = true;
            }
        }
        return mask;
    }

    private static int ChannelDelta(uint a, uint b)
    {
        int delta = 0;
        for (int shift = 0; shift < 32; shift += 8)
            delta = Math.Max(delta, Math.Abs((int)((a >> shift) & 0xFF) - (int)((b >> shift) & 0xFF)));
        return delta;
    }

    /// <summary>The same element tree, laid out at the same place relative to the row, with the same styles.</summary>
    private void AssertSame(Element expected, Element actual, string where)
        => AssertSame(expected, actual, expected.LayoutBox!.BoxModel.BorderBox, actual.LayoutBox!.BoxModel.BorderBox, where);

    private void AssertSame(Element expected, Element actual, RectF expectedRow, RectF actualRow, string where)
    {
        actual.TagName.ShouldBe(expected.TagName, where);
        actual.Class.ShouldBe(expected.Class, where);
        var e = expected.LayoutBox!;
        var a = actual.LayoutBox!;
        a.BoxModel.BorderBox.Height.ShouldBe(e.BoxModel.BorderBox.Height, 0.01f, where);
        a.BoxModel.BorderBox.Width.ShouldBe(e.BoxModel.BorderBox.Width, 0.01f, where);
        (a.BoxModel.BorderBox.Left - actualRow.Left).ShouldBe(e.BoxModel.BorderBox.Left - expectedRow.Left, 0.01f, where);
        (a.BoxModel.BorderBox.Top - actualRow.Top).ShouldBe(e.BoxModel.BorderBox.Top - expectedRow.Top, 0.01f, where);
        var es = e.ComputedStyle;
        var s = a.ComputedStyle;
        s.BorderBottomWidth.ShouldBe(es.BorderBottomWidth, where);
        s.BorderBottomColor.ShouldBe(es.BorderBottomColor, where);
        s.BackgroundColor.ShouldBe(es.BackgroundColor, where);
        s.PaddingTop.ShouldBe(es.PaddingTop, where);
        s.Color.ShouldBe(es.Color, where);
        actual.Children.Count.ShouldBe(expected.Children.Count, where);
        for (int i = 0; i < expected.Children.Count; i++)
        {
            if (expected.Children[i].LayoutBox is null) continue;
            AssertSame(expected.Children[i], actual.Children[i], expectedRow, actualRow, where);
        }
    }

    private uint[] Snapshot(MikoAppContext app)
    {
        _canvas.Clear(SKColors.White);
        Frame(app);
        PumpUntil(app, () => !app.Controller.HasPendingWork, timeoutMs: 1000, settleOnly: true);
        _canvas.Clear(SKColors.White);
        Frame(app);
        _canvas.Flush();
        return _bitmap.Pixels.Select(p => (uint)p).ToArray();
    }

    // ================================ helpers ================================

    private (MikoAppContext App, InfiniteScrollProbe Probe) BuildApp<TPage>(int rows)
        where TPage : Miko.Components.ComponentBase
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseScrollbarStyle(Miko.Common.ScrollbarStyle.None);
        builder.AddResourceAssembly(typeof(VirtualizeEndToEndTests).Assembly);
        builder.Services.AddSingleton(new InfiniteScrollProbe { InitialRows = rows });
        builder.UseRouter(router => router.MapRoute("/", typeof(TPage)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        PumpUntil(app, () => !app.Controller.HasPendingWork, timeoutMs: 1000, settleOnly: true);
        return (app, app.Services.GetRequiredService<InfiniteScrollProbe>());
    }

    private static List<Element> Items(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("ion-item");

    private static Element Scroller(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("inner-scroll").ShouldHaveSingleItem();

    private static Element InfiniteHost(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("ion-infinite-scroll").ShouldHaveSingleItem();

    private static float ScreenTop(MikoAppContext app, Element row)
        => row.LayoutBox!.BoxModel.BorderBox.Top - Scroller(app).LayoutBox!.ScrollTop;

    /// <summary>The rows cover the scroller's visible area, unless the first or last row is the list's own.</summary>
    private static void AssertCovered(MikoAppContext app, int total, string when)
    {
        var items = Items(app);
        items.ShouldNotBeEmpty(when);
        var box = Scroller(app).LayoutBox!;
        var pb = box.BoxModel.PaddingBox;
        float viewTop = pb.Top + box.ScrollTop;
        float viewBottom = viewTop + pb.Height;

        var first = items[0];
        var last = items[^1];
        var listTop = app.Engine.GetRoot()!.FindByClass("ion-list")[0].LayoutBox!.BoxModel.BorderBox.Top;
        if (first.FindByTagName("h2")[0].TextContent != "User 1" && viewTop > listTop)
            first.LayoutBox!.BoxModel.BorderBox.Top.ShouldBeLessThanOrEqualTo(viewTop + 0.5f, $"blank above the rows ({when})");
        if (last.FindByTagName("h2")[0].TextContent != $"User {total}")
            last.LayoutBox!.BoxModel.BorderBox.Bottom.ShouldBeGreaterThanOrEqualTo(viewBottom - 0.5f, $"blank below the rows ({when})");
    }

    private void Frame(MikoAppContext app)
        => app.Controller.RenderFrame(_canvas, W, H, 1f / 60f, c => app.Engine.Render(c));

    private void Drag(MikoAppContext app, float distance)
    {
        var startY = distance > 0 ? PressY : PressY - 500;
        app.Controller.OnPointerDown(PressX, startY, MouseButton.Left, PointerType.Touch, 7);
        Frame(app);
        var steps = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(distance) / 8f));
        for (var i = 1; i <= steps; i++)
        {
            app.Controller.OnPointerMove(PressX, startY - distance * i / steps);
            Frame(app);
        }
        app.Controller.OnPointerUp(PressX, startY - distance, MouseButton.Left, PointerType.Touch, 7);
        // Let the fling settle.
        for (int i = 0; i < 120 && app.Controller.HasPendingWork; i++) Frame(app);
    }

    private void ScrollToBottom(MikoAppContext app)
    {
        for (var i = 0; i < 40; i++)
        {
            var top = Scroller(app).LayoutBox!.ScrollTop;
            Drag(app, 500);
            if (Math.Abs(Scroller(app).LayoutBox!.ScrollTop - top) < 0.5f) return;
        }
    }

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
}
