// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors),
// skiasharp/LiveChartsCore.SkiaSharp/SKCharts/InMemorySkiaSharpChart.cs and SourceGenSK*Chart.cs.

using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.MikoView.Drawing;
using LiveChartsCore.MikoView.Hosting;
using Miko.Common;
using Miko.Graphics;

namespace LiveChartsCore.MikoView.Charts;

/// <summary>
/// Renders a chart to an image without a window: set the series, axes and size, then call
/// <see cref="GetImage"/> or <see cref="SaveImage(string)"/>. Animations are skipped, the image shows the
/// final state.
/// </summary>
/// <remarks>
/// Text is laid out by Miko's text pipeline, so <see cref="LiveChartsMiko.UseGraphics"/> must have been called
/// (a Miko app does it when the first chart component renders). The constructor uses <see cref="Graphics"/>
/// when given.
/// </remarks>
internal static class InMemoryChart
{
    /// <summary>Measures <paramref name="chart"/> at its size and draws one frame into a new surface.</summary>
    internal static RenderSurface Render(IChartView view, Chart chart, int width, int height, LvcColor background, IGraphicsFactory graphics, bool unload)
    {
        var surface = graphics.CreateSurface(Math.Max(1, width), Math.Max(1, height));
        Draw(view, chart, surface.Context, background, unload);
        return surface;
    }

    /// <summary>Measures <paramref name="chart"/> and draws one frame into <paramref name="context"/>.</summary>
    internal static void Draw(IChartView view, Chart chart, IDrawingContext context, LvcColor background, bool unload)
    {
        var bg = background == LvcColor.Empty ? chart.GetTheme().VirtualBackroundColor : background;

        chart.Canvas.DisableAnimations = true;
        chart.IsLoaded = true;
        chart._isFirstDraw = true;
        if (!unload) chart.DisableTooltipCache = true;

        chart.Measure();
        chart.Canvas.DrawFrame(new MikoDrawingContext(view.CoreCanvas, context, bg));

        if (unload) chart.Unload();
    }
}

/// <summary>Base class of the charts rendered to an image.</summary>
/// <typeparam name="TView">The chart view.</typeparam>
public abstract class InMemoryChartBase<TView> where TView : ChartView
{
    private readonly IGraphicsFactory? _graphics;

    /// <summary>Initializes a new instance of the class.</summary>
    /// <param name="graphics">Lays out the text; the factory set with <see cref="LiveChartsMiko.UseGraphics"/> when null.</param>
    protected InMemoryChartBase(IGraphicsFactory? graphics)
    {
        _graphics = graphics;
        if (graphics is not null) LiveChartsMiko.UseGraphics(graphics);

        Host = new SizedHost(this);
        View = CreateView(Host.Canvas, static action => action());
        View.AutoUpdateEnabled = false;
        View.EasingFunction = null;
        View.Initialize();
    }

    /// <summary>The chart view: series, axes, legend, tooltip and the rest.</summary>
    public TView View { get; }

    /// <summary>The core chart.</summary>
    public Chart CoreChart => View.CoreChart;

    /// <summary>The motion canvas.</summary>
    public LiveChartsCore.Motion.CoreMotionCanvas CoreCanvas => View.CoreCanvas;

    /// <summary>The image width in pixels.</summary>
    public int Width { get; set; } = 900;

    /// <summary>The image height in pixels.</summary>
    public int Height { get; set; } = 600;

    /// <summary>The background; the theme's when empty.</summary>
    public LvcColor Background { get; set; } = LvcColor.Empty;

    /// <summary>
    /// Keeps the chart loaded after drawing, so its points, tooltips and legends can be inspected. By default a
    /// drawing unloads the chart.
    /// </summary>
    public bool ExplicitDisposing { get; set; }

    internal SizedHost Host { get; }

    /// <summary>Creates the view.</summary>
    protected abstract TView CreateView(MotionCanvasHost host, Action<Action> post);

    /// <summary>Draws the chart into a new surface; the caller disposes it.</summary>
    public RenderSurface GetImage() =>
        InMemoryChart.Render(View, CoreChart, Width, Height, Background, Graphics, unload: !ExplicitDisposing);

    /// <summary>Draws the chart into <paramref name="context"/>, at the origin of its current transform.</summary>
    public void DrawOnCanvas(IDrawingContext context) =>
        InMemoryChart.Draw(View, CoreChart, context, Background, unload: !ExplicitDisposing);

    /// <summary>Draws the chart and encodes it as PNG.</summary>
    public byte[] EncodePng()
    {
        using var surface = GetImage();
        return surface.EncodePng();
    }

    /// <summary>Draws the chart and writes it as a PNG file.</summary>
    public void SaveImage(string path) => File.WriteAllBytes(path, EncodePng());

    /// <summary>Draws the chart and writes it as PNG to <paramref name="stream"/>.</summary>
    public void SaveImage(Stream stream) => stream.Write(EncodePng());

    private IGraphicsFactory Graphics => _graphics ?? LiveChartsMiko.Graphics;

    /// <summary>The motion canvas host of an in-memory chart, sized by the chart instead of a layout.</summary>
    internal sealed class SizedHost(InMemoryChartBase<TView> owner)
    {
        public MotionCanvasHost Canvas { get; } = new(static () => { }, () => new LvcSize(owner.Width, owner.Height));
    }
}

/// <summary>A cartesian chart rendered to an image.</summary>
public class InMemoryCartesianChart(IGraphicsFactory? graphics = null) : InMemoryChartBase<CartesianChartView>(graphics)
{
    /// <inheritdoc />
    protected override CartesianChartView CreateView(MotionCanvasHost host, Action<Action> post) => new(host, post);
}

/// <summary>A pie chart rendered to an image.</summary>
public class InMemoryPieChart(IGraphicsFactory? graphics = null) : InMemoryChartBase<PieChartView>(graphics)
{
    /// <inheritdoc />
    protected override PieChartView CreateView(MotionCanvasHost host, Action<Action> post) => new(host, post);
}

/// <summary>A polar chart rendered to an image.</summary>
public class InMemoryPolarChart(IGraphicsFactory? graphics = null) : InMemoryChartBase<PolarChartView>(graphics)
{
    /// <inheritdoc />
    protected override PolarChartView CreateView(MotionCanvasHost host, Action<Action> post) => new(host, post);
}
