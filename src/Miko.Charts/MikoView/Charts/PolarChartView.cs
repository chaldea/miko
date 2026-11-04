// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors),
// skiasharp/_Shared/SourceGenPolarChart.*.cs.

using System.Collections.ObjectModel;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Observers;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.MikoView.Hosting;

namespace LiveChartsCore.MikoView.Charts;

/// <summary>The view of a polar chart (angle and radius axes).</summary>
public class PolarChartView : ChartView, IPolarChartView
{
    private IEnumerable<IPolarAxis> _angleAxes = new ObservableCollection<IPolarAxis>();
    private IEnumerable<IPolarAxis> _radiusAxes = new ObservableCollection<IPolarAxis>();
    private bool _fitToBounds = LiveCharts.DefaultSettings.PolarFitToBounds;
    private double _totalAngle = LiveCharts.DefaultSettings.PolarTotalAngle;
    private double _innerRadius = LiveCharts.DefaultSettings.PolarInnerRadius;
    private double _initialRotation = LiveCharts.DefaultSettings.PolarInitialRotation;

    /// <summary>Initializes a new instance of the <see cref="PolarChartView"/> class.</summary>
    public PolarChartView(MotionCanvasHost host, Action<Action> post)
        : base(host, post)
    { }

    /// <inheritdoc />
    public PolarChartEngine Core => (PolarChartEngine)CoreChart;

    /// <inheritdoc />
    public IEnumerable<IPolarAxis> AngleAxes
    {
        get => _angleAxes;
        set => SetObserved(nameof(AngleAxes), ref _angleAxes, value);
    }

    /// <inheritdoc />
    public IEnumerable<IPolarAxis> RadiusAxes
    {
        get => _radiusAxes;
        set => SetObserved(nameof(RadiusAxes), ref _radiusAxes, value);
    }

    /// <inheritdoc />
    public bool FitToBounds
    {
        get => _fitToBounds;
        set { _fitToBounds = value; Update(); }
    }

    /// <inheritdoc />
    public double TotalAngle
    {
        get => _totalAngle;
        set { _totalAngle = value; Update(); }
    }

    /// <inheritdoc />
    public double InnerRadius
    {
        get => _innerRadius;
        set { _innerRadius = value; Update(); }
    }

    /// <inheritdoc />
    public double InitialRotation
    {
        get => _initialRotation;
        set { _initialRotation = value; Update(); }
    }

    /// <inheritdoc />
    public LvcPointD ScalePixelsToData(LvcPointD point, int angleAxisIndex = 0, int radiusAxisIndex = 0) =>
        Core.ScalePixelsToData(point, angleAxisIndex, radiusAxisIndex);

    /// <inheritdoc />
    public LvcPointD ScaleDataToPixels(LvcPointD point, int angleAxisIndex = 0, int radiusAxisIndex = 0) =>
        Core.ScaleDataToPixels(point, angleAxisIndex, radiusAxisIndex);

    /// <inheritdoc />
    protected override Chart CreateCoreChart() => new PolarChartEngine(this, CoreCanvas);

    /// <inheritdoc />
    protected override ChartObserver ConfigureObserver(ChartObserver observe) => base.ConfigureObserver(observe)
        .Collection(nameof(AngleAxes), () => AngleAxes)
        .Collection(nameof(RadiusAxes), () => RadiusAxes);
}
