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
public class RenderBenchmarks
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly RenderEngine _renderEngine = new();

    private SKSurface _surface = null!;
    private LayoutBox _smallLayout = null!;
    private LayoutBox _largeLayout = null!;
    private LayoutBox _textLayout = null!;

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(1200, 800));
        _renderEngine.SetCanvas(_surface.Canvas);

        var smallTree = DomBuilder.CreateFlatTree(10);
        var blockStyles = DomBuilder.CreateBlockStyleSheet();
        _smallLayout = _layoutEngine.Layout(smallTree, blockStyles, 800, 600);

        var largeTree = DomBuilder.CreateFlatTree(500);
        _largeLayout = _layoutEngine.Layout(largeTree, blockStyles, 800, 5000);

        var textTree = DomBuilder.CreateRealisticPage();
        var realisticStyles = DomBuilder.CreateRealisticStyleSheet();
        _textLayout = _layoutEngine.Layout(textTree, realisticStyles, 1200, 800);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _surface.Dispose();
    }

    [Benchmark]
    public void FullRender_SmallTree()
    {
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(_smallLayout);
    }

    [Benchmark]
    public void FullRender_LargeTree()
    {
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(_largeLayout);
    }

    [Benchmark]
    public void IncrementalRender_SingleDirty()
    {
        _surface.Canvas.Clear(SKColors.White);
        var dirtyRegions = new List<RectF>
        {
            new(100, 100, 200, 50)
        };
        _renderEngine.RenderDirty(_largeLayout, dirtyRegions);
    }

    [Benchmark]
    public void IncrementalRender_ManyDirty()
    {
        _surface.Canvas.Clear(SKColors.White);
        var dirtyRegions = new List<RectF>();
        for (int i = 0; i < 20; i++)
        {
            dirtyRegions.Add(new RectF(i * 40, i * 30, 100, 40));
        }
        _renderEngine.RenderDirty(_largeLayout, dirtyRegions);
    }

    [Benchmark]
    public void RenderWithText()
    {
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(_textLayout);
    }
}