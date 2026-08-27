using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace Miko.Benchmarks.Benchmarks;

/// <summary>
/// Pure-Skia probe for the root cause of the opacity-animation blowup. Miko's
/// <c>Painter.SaveLayerAlpha</c> calls <c>SKCanvas.SaveLayer(paint)</c> with no bounds, so Skia
/// sizes the offscreen layer to the entire current clip (the full 800x5000 surface here) for every
/// element with <c>opacity &lt; 1</c>. Passing the element's own bounds should make the cost
/// proportional to the element instead of the page.
/// </summary>
[MemoryDiagnoser]
public class SaveLayerBoundsBenchmarks
{
    private SKSurface _surface = null!;
    private SKCanvas _canvas = null!;
    private SKPaint _layerPaint = null!;
    private SKPaint _fill = null!;

    private const int ElementCount = 50;
    private const int Width = 800;
    private const int Height = 5000;

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(Width, Height));
        _canvas = _surface.Canvas;
        _layerPaint = new SKPaint { Color = new SKColor(255, 255, 255, 128) };
        _fill = new SKPaint { Color = SKColors.CornflowerBlue };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _layerPaint.Dispose();
        _fill.Dispose();
        _surface.Dispose();
    }

    private static SKRect BoundsFor(int i) => new(20, i * 40, 780, i * 40 + 32);

    [Benchmark(Baseline = true, Description = "SaveLayer without bounds (current)")]
    public void Unbounded()
    {
        for (int i = 0; i < ElementCount; i++)
        {
            _canvas.SaveLayer(_layerPaint);
            _canvas.DrawRect(BoundsFor(i), _fill);
            _canvas.Restore();
        }
    }

    [Benchmark(Description = "SaveLayer with element bounds")]
    public void Bounded()
    {
        for (int i = 0; i < ElementCount; i++)
        {
            _canvas.SaveLayer(BoundsFor(i), _layerPaint);
            _canvas.DrawRect(BoundsFor(i), _fill);
            _canvas.Restore();
        }
    }
}
