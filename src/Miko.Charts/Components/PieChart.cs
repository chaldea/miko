using LiveChartsCore;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Hosting;
using Miko.Components;

namespace Miko.Charts;

/// <summary>
/// A pie chart: pie, doughnut and gauge series.
/// </summary>
/// <example>
/// <code>
/// &lt;PieChart Series="@_slices" LegendPosition="LegendPosition.Right" /&gt;
/// </code>
/// </example>
public sealed class PieChart : ChartComponentBase<PieChartView>
{
    /// <summary>Whether the slices run clockwise.</summary>
    [Parameter] public bool IsClockwise { get; set; } = LiveCharts.DefaultSettings.PieIsClockwise;

    /// <summary>Where the first slice starts, in degrees; 0 is the right, -90 the top.</summary>
    [Parameter] public double InitialRotation { get; set; } = LiveCharts.DefaultSettings.PieInitialRotation;

    /// <summary>The angle the whole pie spans; 180 makes a half pie, as in a gauge.</summary>
    [Parameter] public double MaxAngle { get; set; } = LiveCharts.DefaultSettings.PieMaxAngle;

    /// <summary>The value that fills <see cref="MaxAngle"/>; the sum of the series when NaN.</summary>
    [Parameter] public double MaxValue { get; set; } = LiveCharts.DefaultSettings.PieMaxValue;

    /// <summary>The value at the start angle.</summary>
    [Parameter] public double MinValue { get; set; } = LiveCharts.DefaultSettings.PieMinValue;

    /// <inheritdoc />
    protected override PieChartView CreateView(MotionCanvasHost host, Action<Action> post) => new(host, post);

    /// <inheritdoc />
    protected override void ApplyParameters(PieChartView view)
    {
        if (view.IsClockwise != IsClockwise) view.IsClockwise = IsClockwise;
        if (view.InitialRotation != InitialRotation) view.InitialRotation = InitialRotation;
        if (view.MaxAngle != MaxAngle) view.MaxAngle = MaxAngle;
        if (!view.MaxValue.Equals(MaxValue)) view.MaxValue = MaxValue;
        if (view.MinValue != MinValue) view.MinValue = MinValue;
    }
}
