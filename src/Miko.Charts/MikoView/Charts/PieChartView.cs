// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors),
// skiasharp/_Shared/SourceGenPieChart.*.cs.

using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.MikoView.Hosting;

namespace LiveChartsCore.MikoView.Charts;

/// <summary>The view of a pie chart: pie, doughnut and gauge series.</summary>
public class PieChartView : ChartView, IPieChartView
{
    private bool _isClockwise = LiveCharts.DefaultSettings.PieIsClockwise;
    private double _initialRotation = LiveCharts.DefaultSettings.PieInitialRotation;
    private double _maxAngle = LiveCharts.DefaultSettings.PieMaxAngle;
    private double _maxValue = LiveCharts.DefaultSettings.PieMaxValue;
    private double _minValue = LiveCharts.DefaultSettings.PieMinValue;

    /// <summary>Initializes a new instance of the <see cref="PieChartView"/> class.</summary>
    public PieChartView(MotionCanvasHost host, Action<Action> post)
        : base(host, post)
    { }

    /// <inheritdoc />
    public PieChartEngine Core => (PieChartEngine)CoreChart;

    /// <inheritdoc />
    public bool IsClockwise
    {
        get => _isClockwise;
        set { _isClockwise = value; Update(); }
    }

    /// <inheritdoc />
    public double InitialRotation
    {
        get => _initialRotation;
        set { _initialRotation = value; Update(); }
    }

    /// <inheritdoc />
    public double MaxAngle
    {
        get => _maxAngle;
        set { _maxAngle = value; Update(); }
    }

    /// <inheritdoc />
    public double MaxValue
    {
        get => _maxValue;
        set { _maxValue = value; Update(); }
    }

    /// <inheritdoc />
    public double MinValue
    {
        get => _minValue;
        set { _minValue = value; Update(); }
    }

    /// <inheritdoc />
    protected override Chart CreateCoreChart() => new PieChartEngine(this, CoreCanvas);
}
