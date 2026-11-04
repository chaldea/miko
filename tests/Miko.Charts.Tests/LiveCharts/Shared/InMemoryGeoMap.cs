// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors),
// skiasharp/LiveChartsCore.SkiaSharp/SKCharts/SKGeoMap.cs and LiveChartsGeneratedCode/SourceGenSKMapChart.cs.
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts;

using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Drawing;
using LiveChartsCore.MikoView.Hosting;
using Miko.Graphics;

/// <summary>
/// The tests' stand-in for LiveCharts' <c>SKGeoMap</c>. Miko.Charts has no in-memory map, so this hosts a
/// <see cref="GeoMapView"/> on a fixed-size <see cref="MotionCanvasHost"/> and draws it as
/// <c>SourceGenSKMapChart.DrawOnCanvas</c> did: animations off, measure, one frame, unload.
/// </summary>
internal sealed class InMemoryGeoMap
{
    public InMemoryGeoMap()
    {
        CoreTestsSetup.Ensure();

        Host = new MotionCanvasHost(static () => { }, () => new LvcSize(Width, Height));
        View = new GeoMapView(Host, static action => action()) { AutoUpdateEnabled = false };
        View.Initialize();
    }

    public MotionCanvasHost Host { get; }

    /// <summary>The map view: series, active map, stroke, fill.</summary>
    public GeoMapView View { get; }

    public GeoMapChart CoreChart => View.CoreChart;

    public CoreMotionCanvas CoreCanvas => View.CoreCanvas;

    public int Width { get; set; } = 900;

    public int Height { get; set; } = 600;

    /// <summary>The background; <c>SKGeoMap</c> defaulted to <c>SKColors.Empty</c> (transparent).</summary>
    public LvcColor Background { get; set; } = TestColors.Transparent;

    /// <summary>Draws the map into a new surface; the caller disposes it.</summary>
    public RenderSurface GetImage()
    {
        var surface = CoreTestsSetup.Graphics.CreateSurface(Math.Max(1, Width), Math.Max(1, Height));
        DrawOnCanvas(surface.Context);
        return surface;
    }

    /// <summary>Measures the map, draws one frame into <paramref name="context"/> and unloads the map.</summary>
    public void DrawOnCanvas(IDrawingContext context)
    {
        CoreCanvas.DisableAnimations = true;
        CoreChart.Measure();
        CoreCanvas.DrawFrame(new MikoDrawingContext(CoreCanvas, context, Background));
        CoreChart.Unload();
    }
}
