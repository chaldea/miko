using BenchmarkDotNet.Attributes;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Miko.Styling.Selectors;
using SkiaSharp;

namespace Miko.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of a frame on a fully static page. This is what the ISSUE-131 "skip per-frame
/// whole-tree scans" items target: with no mutations, <c>AssignOwner</c> and the animation
/// declaration scan should not walk the tree at all, and the host should be able to see
/// <c>HasPendingVisualWork == false</c> and skip the frame entirely.
/// </summary>
[MemoryDiagnoser]
public class EngineIdleFrameBenchmarks
{
    private MikoEngine _engine = null!;
    private SKSurface _surface = null!;
    private Element _page = null!;
    private List<StyleSheet> _styles = null!;

    [Params(200, 1000)]
    public int ElementCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(800, 5000));
        _styles =
        [
            new StyleSheet
            {
                Rules =
                [
                    new StyleRule
                    {
                        Selector = new ClassSelector("idle-page"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(800) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("idle-item"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(760),
                            Height = Length.Px(24),
                            BackgroundColor = Color.FromRgb(220, 224, 230)
                        }
                    }
                ]
            }
        ];

        _page = new DivElement { Class = "idle-page" };
        for (int i = 0; i < ElementCount; i++)
            _page.AddChild(new DivElement { Class = "idle-item", TextContent = $"Row {i}" });

        _engine = new MikoEngineBuilder().Build();
        _engine.Initialize(_page, _styles, _surface.Canvas, 800, 5000);
        _engine.Render(_surface.Canvas);
        _engine.Update(_surface.Canvas);
    }

    [GlobalCleanup]
    public void Cleanup() => _surface.Dispose();

    /// <summary>The host's idle-frame probe. Should be near-free and allocation-free.</summary>
    [Benchmark(Baseline = true, Description = "Idle HasPendingVisualWork probe")]
    public bool IdleProbe() => _engine.HasPendingVisualWork;

    /// <summary>An idle incremental frame: no mutations, so layout should be reused.</summary>
    [Benchmark(Description = "Idle incremental Update")]
    public void IdleUpdate() => _engine.Update(_surface.Canvas);
}
