using BenchmarkDotNet.Attributes;
using Miko.Benchmarks.Helpers;
using Miko.Common;
using Miko.Layout;
using Miko.Rendering;
using SkiaSharp;

namespace Miko.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class DirtyRegionTippingPointBenchmarks
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly RenderEngine _renderEngine = new();

    private SKSurface _surface = null!;
    private LayoutBox _layout = null!;

    [Params(1, 2, 3, 5, 8, 10, 15, 20, 30, 50)]
    public int DirtyRegionCount;

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(800, 5000));
        _renderEngine.SetCanvas(_surface.Canvas);

        var tree = DomBuilder.CreateFlatTree(500);
        var styles = DomBuilder.CreateBlockStyleSheet();
        _layout = _layoutEngine.Layout(tree, styles, 800, 5000);
    }

    [GlobalCleanup]
    public void Cleanup() => _surface.Dispose();

    [Benchmark(Baseline = true, Description = "Full render (baseline)")]
    public void FullRender()
    {
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(_layout);
    }

    [Benchmark(Description = "Incremental render (N dirty regions)")]
    public void IncrementalRender()
    {
        _surface.Canvas.Clear(SKColors.White);
        var regions = new List<RectF>(DirtyRegionCount);
        for (int i = 0; i < DirtyRegionCount; i++)
            regions.Add(new RectF(i * 30, i * 10, 200, 50));
        _renderEngine.RenderDirty(_layout, regions);
    }
}
