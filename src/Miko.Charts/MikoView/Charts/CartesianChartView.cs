// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors),
// skiasharp/_Shared/SourceGenCartesianChart.*.cs.

using System.Collections.ObjectModel;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Observers;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView.Hosting;

namespace LiveChartsCore.MikoView.Charts;

/// <summary>The view of a cartesian chart (x and y axes): line, column, scatter, financial series and so on.</summary>
public class CartesianChartView : ChartView, ICartesianChartView
{
    private IEnumerable<ICartesianAxis> _xAxes = new ObservableCollection<ICartesianAxis>();
    private IEnumerable<ICartesianAxis> _yAxes = new ObservableCollection<ICartesianAxis>();
    private IEnumerable<IChartElement> _sections = new ObservableCollection<IChartElement>();
    private IChartElement? _drawMarginFrame;
    private bool _matchAxesScreenDataRatio;

    /// <summary>Initializes a new instance of the <see cref="CartesianChartView"/> class.</summary>
    public CartesianChartView(MotionCanvasHost host, Action<Action> post)
        : base(host, post)
    { }

    /// <inheritdoc />
    public CartesianChartEngine Core => (CartesianChartEngine)CoreChart;

    /// <inheritdoc />
    public IEnumerable<ICartesianAxis> XAxes
    {
        get => _xAxes;
        set => SetObserved(nameof(XAxes), ref _xAxes, value);
    }

    /// <inheritdoc />
    public IEnumerable<ICartesianAxis> YAxes
    {
        get => _yAxes;
        set => SetObserved(nameof(YAxes), ref _yAxes, value);
    }

    /// <inheritdoc />
    public IEnumerable<IChartElement> Sections
    {
        get => _sections;
        set => SetObserved(nameof(Sections), ref _sections, value);
    }

    /// <inheritdoc />
    public IChartElement? DrawMarginFrame
    {
        get => _drawMarginFrame;
        set => SetObserved(nameof(DrawMarginFrame), ref _drawMarginFrame, value);
    }

    /// <inheritdoc />
    public ZoomAndPanMode ZoomMode { get; set; } = LiveCharts.DefaultSettings.ZoomMode;

    /// <inheritdoc />
    public FindingStrategy FindingStrategy { get; set; } = LiveCharts.DefaultSettings.FindingStrategy;

    /// <inheritdoc />
    public double ZoomingSpeed { get; set; } = LiveCharts.DefaultSettings.ZoomSpeed;

    /// <inheritdoc />
    public bool MatchAxesScreenDataRatio
    {
        get => _matchAxesScreenDataRatio;
        set
        {
            _matchAxesScreenDataRatio = value;
            if (value) SharedAxes.MatchAxesScreenDataRatio(this);
            else SharedAxes.DisposeMatchAxesScreenDataRatio(this);
        }
    }

    /// <inheritdoc />
    public LvcPointD ScalePixelsToData(LvcPointD point, int xAxisIndex = 0, int yAxisIndex = 0) =>
        Core.ScalePixelsToData(point, xAxisIndex, yAxisIndex);

    /// <inheritdoc />
    public LvcPointD ScaleDataToPixels(LvcPointD point, int xAxisIndex = 0, int yAxisIndex = 0) =>
        Core.ScaleDataToPixels(point, xAxisIndex, yAxisIndex);

    /// <inheritdoc />
    public override void OnWheel(float x, float y, float deltaY)
    {
        if (ZoomMode == ZoomAndPanMode.None || deltaY == 0) return;
        Core.Zoom(ZoomMode, new LvcPoint(x, y), deltaY < 0 ? ZoomDirection.ZoomIn : ZoomDirection.ZoomOut);
    }

    /// <inheritdoc />
    protected override Chart CreateCoreChart() => new CartesianChartEngine(this, CoreCanvas);

    /// <inheritdoc />
    protected override ChartObserver ConfigureObserver(ChartObserver observe) => base.ConfigureObserver(observe)
        .Collection(nameof(XAxes), () => XAxes)
        .Collection(nameof(YAxes), () => YAxes)
        .Collection(nameof(Sections), () => Sections)
        .Property(nameof(DrawMarginFrame), () => DrawMarginFrame!);
}
