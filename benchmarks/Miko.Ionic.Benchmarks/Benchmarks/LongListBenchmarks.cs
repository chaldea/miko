using BenchmarkDotNet.Attributes;
using Miko.Core;
using Miko.Hosting;
using Miko.Ionic.Benchmarks.Helpers;
using Miko.Ionic.Benchmarks.Pages;
using SkiaSharp;

namespace Miko.Ionic.Benchmarks.Benchmarks;

/// <summary>
/// ISSUE-151: the Infinite Scroll demo's page holding a growing number of rows, rendered as the demo does
/// (every row in the DOM) and through <c>Virtualize</c>, measured through <see cref="MikoEngine"/> so the
/// engine's invalidation model and the virtualization settle pass decide what each frame does.
///
/// <para>What must not grow with the row count once the list is virtualized:</para>
/// <list type="bullet">
/// <item><b>First frame</b> — opening the page: build, style, layout, paint.</item>
/// <item><b>Scroll frame</b> — one 16px step of a steady scroll. Most such frames only repaint; one in
/// every few moves the window and re-renders the list.</item>
/// <item><b>Jump frame</b> — a 600px step, which moves the window on every frame: the worst case of a
/// fling.</item>
/// <item><b>Data change frame</b> — what an infinite-scroll load does: five rows appended (or, on every
/// other call, removed again so the list keeps its size) and the page re-rendered.</item>
/// </list>
/// <para>The plain list is measured up to 2,000 rows: beyond that one frame takes seconds and the DOM
/// alone needs hundreds of megabytes, which is the problem being solved.</para>
/// </summary>
[MemoryDiagnoser]
public class LongListBenchmarks
{
    private const int ViewportWidth = 390;
    private const int ViewportHeight = 844;
    private const float ScrollX = ViewportWidth / 2f;
    private const float ScrollY = ViewportHeight / 2f;

    public readonly record struct Case(bool Virtualized, int Rows)
    {
        public override string ToString() => $"{(Virtualized ? "Virtualized" : "Plain")}-{Rows}";
    }

    public static IEnumerable<Case> Cases =>
    [
        new(false, 100), new(false, 1000), new(false, 2000),
        new(true, 100), new(true, 1000), new(true, 10_000), new(true, 100_000),
    ];

    [ParamsSource(nameof(Cases))]
    public Case List { get; set; }

    private IonicAppHarness _harness = null!;
    private SKSurface _surface = null!;
    private MikoEngine _engine = null!;
    private LongListPage _page = null!;
    private List<LongListPage.User> _users = null!;
    private float _direction = 1f;
    private bool _appended;

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(ViewportWidth, ViewportHeight));
        _harness = IonicAppHarness.Create<LongListPage>();
        _engine = new MikoEngineBuilder().Build();
        _users = LongListPage.MakeUsers(List.Rows);
        OpenPage();
        // Warm the JIT on the steady-state paths before measuring.
        for (int i = 0; i < 50; i++) ScrollFrame();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _surface.Dispose();
        _harness.Dispose();
    }

    [Benchmark(Description = "First frame (open the page)")]
    public void FirstFrame() => OpenPage();

    [Benchmark(Description = "Scroll frame (16px step)")]
    public void ScrollFrame() => Step(16f);

    [Benchmark(Description = "Jump frame (600px step, window moves)")]
    public void JumpFrame() => Step(600f);

    [Benchmark(Description = "Data change frame (5 rows appended or removed)")]
    public void DataChangeFrame()
    {
        if (_appended) _page.Trim(List.Rows);
        else _page.Append(5);
        _appended = !_appended;
        _engine.Render(_surface.Canvas);
    }

    private void OpenPage()
    {
        _page = new LongListPage { Virtualized = List.Virtualized, Users = _users };
        var root = _harness.Build(_page);
        _engine.Initialize(root, _harness.StyleSheets, _surface.Canvas, ViewportWidth, ViewportHeight,
            transition: null, paint: false);
        _engine.Render(_surface.Canvas);
        _appended = false;
    }

    /// <summary>Scrolls by <paramref name="distance"/> and renders; turns around at either end.</summary>
    private void Step(float distance)
    {
        if (!_engine.ScrollBy(ScrollX, ScrollY, 0, distance * _direction))
        {
            _direction = -_direction;
            _engine.ScrollBy(ScrollX, ScrollY, 0, distance * _direction);
        }
        _engine.Render(_surface.Canvas);
    }
}
