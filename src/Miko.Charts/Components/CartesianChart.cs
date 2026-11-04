using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Hosting;
using Miko.Components;

namespace Miko.Charts;

/// <summary>
/// A chart with x and y axes: line, column, row, scatter, stacked, step, financial, box and heat series.
/// </summary>
/// <example>
/// <code>
/// &lt;CartesianChart Series="@_series" XAxes="@_xAxes" ZoomMode="ZoomAndPanMode.X" /&gt;
/// </code>
/// </example>
public sealed class CartesianChart : ChartComponentBase<CartesianChartView>
{
    private IEnumerable<ICartesianAxis>? _xAxes;
    private IEnumerable<ICartesianAxis>? _yAxes;
    private IEnumerable<IChartElement>? _sections;

    /// <summary>The x axes; one default axis when null.</summary>
    [Parameter] public IEnumerable<ICartesianAxis>? XAxes { get; set; }

    /// <summary>The y axes; one default axis when null.</summary>
    [Parameter] public IEnumerable<ICartesianAxis>? YAxes { get; set; }

    /// <summary>Highlighted ranges of the plot area, for example <c>RectangularSection</c>s.</summary>
    [Parameter] public IEnumerable<IChartElement>? Sections { get; set; }

    /// <summary>A frame drawn around the plot area, for example a <c>DrawMarginFrame</c>.</summary>
    [Parameter] public IChartElement? DrawMarginFrame { get; set; }

    /// <summary>
    /// Which axes a drag pans and <see cref="Zoom"/> zooms; none by default. Secondary-button drags draw a zoom
    /// section. Miko has no wheel event yet, so zooming is done by calling <see cref="Zoom"/>.
    /// </summary>
    [Parameter] public ZoomAndPanMode ZoomMode { get; set; } = LiveCharts.DefaultSettings.ZoomMode;

    /// <summary>How much a wheel step zooms, from 0 to 1.</summary>
    [Parameter] public double ZoomingSpeed { get; set; } = LiveCharts.DefaultSettings.ZoomSpeed;

    /// <summary>How the tooltip finds the points under the pointer.</summary>
    [Parameter] public FindingStrategy FindingStrategy { get; set; } = LiveCharts.DefaultSettings.FindingStrategy;

    /// <summary>Keeps one data unit the same length on both axes.</summary>
    [Parameter] public bool MatchAxesScreenDataRatio { get; set; }

    /// <summary>
    /// Zooms in or out around <paramref name="pivot"/> (in the chart's coordinates; the center of the plot area
    /// when null), on the axes <see cref="ZoomMode"/> allows.
    /// </summary>
    public void Zoom(ZoomDirection direction, LvcPoint? pivot = null)
    {
        if (View is not { } view || ZoomMode == ZoomAndPanMode.None) return;
        var core = view.Core;
        var center = pivot ?? new LvcPoint(
            core.DrawMarginLocation.X + core.DrawMarginSize.Width / 2,
            core.DrawMarginLocation.Y + core.DrawMarginSize.Height / 2);
        core.Zoom(ZoomMode, center, direction);
    }

    /// <inheritdoc />
    protected override CartesianChartView CreateView(MotionCanvasHost host, Action<Action> post) => new(host, post);

    /// <inheritdoc />
    protected override void ApplyParameters(CartesianChartView view)
    {
        if (!ReferenceEquals(_xAxes, XAxes))
        {
            _xAxes = XAxes;
            view.XAxes = XAxes ?? [];
        }

        if (!ReferenceEquals(_yAxes, YAxes))
        {
            _yAxes = YAxes;
            view.YAxes = YAxes ?? [];
        }

        if (!ReferenceEquals(_sections, Sections))
        {
            _sections = Sections;
            view.Sections = Sections ?? [];
        }

        if (!ReferenceEquals(view.DrawMarginFrame, DrawMarginFrame)) view.DrawMarginFrame = DrawMarginFrame;

        view.ZoomMode = ZoomMode;
        view.ZoomingSpeed = ZoomingSpeed;
        view.FindingStrategy = FindingStrategy;
        if (view.MatchAxesScreenDataRatio != MatchAxesScreenDataRatio) view.MatchAxesScreenDataRatio = MatchAxesScreenDataRatio;
    }
}
