using BenchmarkDotNet.Attributes;
using Miko.Benchmarks.Helpers;
using Miko.Common;
using Miko.Core;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class FramePipelineBenchmarks
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly RenderEngine _renderEngine = new();

    private SKSurface _surface = null!;
    private Element _realisticPage = null!;
    private List<StyleSheet> _realisticStyles = null!;
    private Element _largePage = null!;
    private List<StyleSheet> _blockStyles = null!;

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(1200, 800));
        _renderEngine.SetCanvas(_surface.Canvas);

        _realisticPage = DomBuilder.CreateRealisticPage();
        _realisticStyles = DomBuilder.CreateRealisticStyleSheet();
        _largePage = DomBuilder.CreateFlatTree(500);
        _blockStyles = DomBuilder.CreateBlockStyleSheet();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _surface.Dispose();
    }

    [Benchmark(Description = "Full frame: realistic page (~90 elements)")]
    public void FullFrame_RealisticPage()
    {
        var layout = _layoutEngine.Layout(_realisticPage, _realisticStyles, 1200, 800);
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(layout);
    }

    [Benchmark(Description = "Full frame: large page (500 elements)")]
    public void FullFrame_LargePage()
    {
        var layout = _layoutEngine.Layout(_largePage, _blockStyles, 800, 5000);
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(layout);
    }

    [Benchmark(Description = "Incremental frame: realistic page, 1 dirty element")]
    public void IncrementalFrame_RealisticPage_SingleDirty()
    {
        var layout = _layoutEngine.Layout(_realisticPage, _realisticStyles, 1200, 800);
        _surface.Canvas.Clear(SKColors.White);
        var dirtyRegions = new List<RectF> { new(50, 200, 300, 80) };
        _renderEngine.RenderDirty(layout, dirtyRegions);
    }

    [Benchmark(Description = "Incremental frame: large page, 1 dirty element")]
    public void IncrementalFrame_LargePage_SingleDirty()
    {
        var layout = _layoutEngine.Layout(_largePage, _blockStyles, 800, 5000);
        _surface.Canvas.Clear(SKColors.White);
        var dirtyRegions = new List<RectF> { new(100, 100, 200, 50) };
        _renderEngine.RenderDirty(layout, dirtyRegions);
    }
}
