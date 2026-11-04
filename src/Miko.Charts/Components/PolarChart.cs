using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Hosting;
using Miko.Components;

namespace Miko.Charts;

/// <summary>
/// A chart with an angle and a radius axis: radar and polar line series.
/// </summary>
public sealed class PolarChart : ChartComponentBase<PolarChartView>
{
    private IEnumerable<IPolarAxis>? _angleAxes;
    private IEnumerable<IPolarAxis>? _radiusAxes;

    /// <summary>The angle axes; one default axis when null.</summary>
    [Parameter] public IEnumerable<IPolarAxis>? AngleAxes { get; set; }

    /// <summary>The radius axes; one default axis when null.</summary>
    [Parameter] public IEnumerable<IPolarAxis>? RadiusAxes { get; set; }

    /// <summary>Scales the plot to the control instead of keeping it round.</summary>
    [Parameter] public bool FitToBounds { get; set; } = LiveCharts.DefaultSettings.PolarFitToBounds;

    /// <summary>The angle the plot spans, in degrees.</summary>
    [Parameter] public double TotalAngle { get; set; } = LiveCharts.DefaultSettings.PolarTotalAngle;

    /// <summary>The empty radius at the center, in pixels.</summary>
    [Parameter] public double InnerRadius { get; set; } = LiveCharts.DefaultSettings.PolarInnerRadius;

    /// <summary>Where the angle axis starts, in degrees; -90 is the top.</summary>
    [Parameter] public double InitialRotation { get; set; } = LiveCharts.DefaultSettings.PolarInitialRotation;

    /// <inheritdoc />
    protected override PolarChartView CreateView(MotionCanvasHost host, Action<Action> post) => new(host, post);

    /// <inheritdoc />
    protected override void ApplyParameters(PolarChartView view)
    {
        if (!ReferenceEquals(_angleAxes, AngleAxes))
        {
            _angleAxes = AngleAxes;
            view.AngleAxes = AngleAxes ?? [];
        }

        if (!ReferenceEquals(_radiusAxes, RadiusAxes))
        {
            _radiusAxes = RadiusAxes;
            view.RadiusAxes = RadiusAxes ?? [];
        }

        if (view.FitToBounds != FitToBounds) view.FitToBounds = FitToBounds;
        if (view.TotalAngle != TotalAngle) view.TotalAngle = TotalAngle;
        if (view.InnerRadius != InnerRadius) view.InnerRadius = InnerRadius;
        if (view.InitialRotation != InitialRotation) view.InitialRotation = InitialRotation;
    }
}
