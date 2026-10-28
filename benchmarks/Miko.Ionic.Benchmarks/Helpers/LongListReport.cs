using System.Diagnostics;
using Miko.Core;
using Miko.Hosting;
using Miko.Ionic.Benchmarks.Pages;
using SkiaSharp;

namespace Miko.Ionic.Benchmarks.Helpers;

/// <summary>
/// "--long-list": the frames of <see cref="Benchmarks.LongListBenchmarks"/> timed with a stopwatch for
/// every row count, as one table — seconds instead of a BenchmarkDotNet run, for checking that the
/// virtualized list's frames do not grow with the row count (ISSUE-151). BenchmarkDotNet remains the
/// reference for the numbers themselves.
/// </summary>
internal static class LongListReport
{
    private const int Width = 390, Height = 844;

    public static void Run()
    {
        Console.WriteLine("| List | Rows | DOM elements | First frame | Scroll frame (16px) | Jump frame (600px) | Data change frame |");
        Console.WriteLine("|---|---:|---:|---:|---:|---:|---:|");
        foreach (var (virtualized, rows) in new[]
                 {
                     (false, 100), (false, 1000), (false, 2000),
                     (true, 100), (true, 1000), (true, 10_000), (true, 100_000),
                 })
        {
            Measure(virtualized, rows);
        }
    }

    private static void Measure(bool virtualized, int rows)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        using var harness = IonicAppHarness.Create<LongListPage>();
        var engine = new MikoEngineBuilder().Build();
        var users = LongListPage.MakeUsers(rows);
        LongListPage page = null!;

        void Open()
        {
            page = new LongListPage { Virtualized = virtualized, Users = users };
            var root = harness.Build(page);
            engine.Initialize(root, harness.StyleSheets, surface.Canvas, Width, Height, transition: null, paint: false);
            engine.Render(surface.Canvas);
        }

        float direction = 1f;
        void Step(float distance)
        {
            if (!engine.ScrollBy(Width / 2f, Height / 2f, 0, distance * direction))
            {
                direction = -direction;
                engine.ScrollBy(Width / 2f, Height / 2f, 0, distance * direction);
            }
            engine.Render(surface.Canvas);
        }

        bool appended = false;
        void DataChange()
        {
            if (appended) page.Trim(rows);
            else page.Append(5);
            appended = !appended;
            engine.Render(surface.Canvas);
        }

        // Fewer repetitions where one frame is slow.
        int heavy = rows >= 1000 && !virtualized ? 3 : 20;
        double first = Time(Open, warmup: 2, runs: heavy);
        int elements = Count(engine.GetRoot()!);
        double scroll = Time(() => Step(16f), warmup: 100, runs: 400);
        double jump = Time(() => Step(600f), warmup: 20, runs: heavy * 5);
        double change = Time(DataChange, warmup: 4, runs: heavy * 2);

        Console.WriteLine($"| {(virtualized ? "Virtualize" : "plain")} | {rows:N0} | {elements:N0} | {first:F2} ms | {scroll:F3} ms | {jump:F2} ms | {change:F2} ms |");
    }

    private static double Time(Action action, int warmup, int runs)
    {
        for (int i = 0; i < warmup; i++) action();
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < runs; i++) action();
        return watch.Elapsed.TotalMilliseconds / runs;
    }

    private static int Count(Element element)
    {
        int count = 1;
        foreach (var child in element.Children) count += Count(child);
        return count;
    }
}
