// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors),
// skiasharp/_Shared/SourceGenChart.*.cs: the view-side chart shared by every UI framework, written
// out by hand for Miko instead of being source generated.

using System.Collections.ObjectModel;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Observers;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.Motion;
using LiveChartsCore.MikoView.Hosting;
using LiveChartsCore.Painting;
using LiveChartsCore.Themes;
using LiveChartsCore.VisualElements;

namespace LiveChartsCore.MikoView.Charts;

/// <summary>
/// The view side of a chart drawn by Miko: the properties a chart reads (series, legend, tooltip,
/// animation, …), the events it raises and the canvas it draws in.
/// </summary>
/// <remarks>
/// <para>Every member is a UI-thread API: in a Miko app that is the render thread, where components and
/// event handlers already run. The chart measures there too (<see cref="IChartView.InvokeOnUIThread"/>
/// posts to the engine's dispatcher), so series and axes are never read while they are being changed.</para>
/// <para>Assigning a property updates the chart. Collections that implement
/// <see cref="System.Collections.Specialized.INotifyCollectionChanged"/> (such as
/// <see cref="ObservableCollection{T}"/>) and items that implement
/// <see cref="System.ComponentModel.INotifyPropertyChanged"/> update it as they change.</para>
/// </remarks>
public abstract class ChartView : IChartView, IDisposable
{
    private readonly ChartObserver _observer;
    private readonly Action<Action> _post;
    private Chart? _coreChart;
    private bool _disposed;

    private IEnumerable<ISeries> _series = new ObservableCollection<ISeries>();
    private IEnumerable<IChartElement> _visualElements = new ObservableCollection<IChartElement>();
    private IChartElement? _title;
    private IChartLegend? _legend;
    private Theme? _chartTheme;
    private Margin? _drawMargin;
    private LegendPosition _legendPosition = LiveCharts.DefaultSettings.LegendPosition;
    private TooltipPosition _tooltipPosition = LiveCharts.DefaultSettings.TooltipPosition;
    private Paint? _legendTextPaint = LiveCharts.DefaultSettings.LegendTextPaint;
    private Paint? _legendBackgroundPaint = LiveCharts.DefaultSettings.LegendBackgroundPaint;
    private double _legendTextSize = LiveCharts.DefaultSettings.LegendTextSize;
    private Paint? _tooltipTextPaint = LiveCharts.DefaultSettings.TooltipTextPaint;
    private Paint? _tooltipBackgroundPaint = LiveCharts.DefaultSettings.TooltipBackgroundPaint;
    private double _tooltipTextSize = LiveCharts.DefaultSettings.TooltipTextSize;
    private object _syncContext = new();

    static ChartView()
    {
        _ = LiveChartsMiko.EnsureInitialized();
    }

    /// <summary>Initializes a new instance of the <see cref="ChartView"/> class.</summary>
    /// <param name="host">The canvas the chart draws in.</param>
    /// <param name="post">Runs an action on the UI (render) thread at the next frame.</param>
    protected ChartView(MotionCanvasHost host, Action<Action> post)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(post);

        Host = host;
        _post = post;
        _observer = new ChartObserver(ConfigureObserver, () => _coreChart?.Update());

        Host.CanvasCore.Sync = _syncContext;
        Host.SizeChanged += OnHostSizeChanged;
    }

    /// <summary>Gets the canvas host.</summary>
    public MotionCanvasHost Host { get; }

    /// <inheritdoc />
    public CoreMotionCanvas CoreCanvas => Host.CanvasCore;

    /// <inheritdoc />
    public Chart CoreChart => _coreChart ?? throw new InvalidOperationException("The chart has not been initialized yet.");

    /// <inheritdoc />
    public LvcSize ControlSize => Host.Size;

    /// <inheritdoc />
    public bool DesignerMode => false;

    /// <inheritdoc />
    public bool IsDarkMode { get; set; }

    /// <inheritdoc />
    public LvcColor BackColor => Host.Background;

    /// <inheritdoc />
    public IEnumerable<ISeries> Series
    {
        get => _series;
        set => SetObserved(nameof(Series), ref _series, value, resetSeriesIds: true);
    }

    /// <inheritdoc />
    public IEnumerable<IChartElement> VisualElements
    {
        get => _visualElements;
        set => SetObserved(nameof(VisualElements), ref _visualElements, value);
    }

    /// <inheritdoc />
    public IChartElement? Title
    {
        get => _title;
        set => SetObserved(nameof(Title), ref _title, value);
    }

    /// <inheritdoc />
    public IChartLegend? Legend
    {
        get => _legend;
        set { _legend = value; Update(); }
    }

    /// <inheritdoc />
    public IChartTooltip? Tooltip { get; set; }

    /// <inheritdoc />
    public Theme? ChartTheme
    {
        get => _chartTheme;
        set { _chartTheme = value; Update(); }
    }

    /// <inheritdoc />
    public Margin? DrawMargin
    {
        get => _drawMargin;
        set { _drawMargin = value; Update(); }
    }

    /// <inheritdoc />
    public TimeSpan AnimationsSpeed { get; set; } = LiveCharts.DefaultSettings.AnimationsSpeed;

    /// <inheritdoc />
    public Func<float, float>? EasingFunction { get; set; } = LiveCharts.DefaultSettings.EasingFunction;

    /// <inheritdoc />
    public TimeSpan UpdaterThrottler { get; set; } = LiveCharts.DefaultSettings.UpdateThrottlingTimeout;

    /// <inheritdoc />
    public LegendPosition LegendPosition
    {
        get => _legendPosition;
        set { _legendPosition = value; Update(); }
    }

    /// <inheritdoc />
    public TooltipPosition TooltipPosition
    {
        get => _tooltipPosition;
        set { _tooltipPosition = value; Update(); }
    }

    /// <inheritdoc />
    public Paint? LegendTextPaint
    {
        get => _legendTextPaint;
        set { _legendTextPaint = AsText(value); Update(); }
    }

    /// <inheritdoc />
    public Paint? LegendBackgroundPaint
    {
        get => _legendBackgroundPaint;
        set { _legendBackgroundPaint = value; Update(); }
    }

    /// <inheritdoc />
    public double LegendTextSize
    {
        get => _legendTextSize;
        set { _legendTextSize = value; Update(); }
    }

    /// <inheritdoc />
    public Paint? TooltipTextPaint
    {
        get => _tooltipTextPaint;
        set { _tooltipTextPaint = AsText(value); Update(); }
    }

    /// <inheritdoc />
    public Paint? TooltipBackgroundPaint
    {
        get => _tooltipBackgroundPaint;
        set { _tooltipBackgroundPaint = value; Update(); }
    }

    /// <inheritdoc />
    public double TooltipTextSize
    {
        get => _tooltipTextSize;
        set { _tooltipTextSize = value; Update(); }
    }

    /// <inheritdoc />
    public bool AutoUpdateEnabled { get; set; } = true;

    /// <inheritdoc />
    public object SyncContext
    {
        get => _syncContext;
        set
        {
            _syncContext = value ?? new object();
            Host.CanvasCore.Sync = _syncContext;
            Update();
        }
    }

    /// <inheritdoc />
    public event ChartEventHandler? Measuring;

    /// <inheritdoc />
    public event ChartEventHandler? UpdateStarted;

    /// <inheritdoc />
    public event ChartEventHandler? UpdateFinished;

    /// <inheritdoc />
    public event ChartPointsHandler? DataPointerDown;

    /// <inheritdoc />
    public event ChartPointHoverHandler? HoveredPointsChanged;

    /// <inheritdoc />
    [Obsolete($"Use the {nameof(DataPointerDown)} event instead with a {nameof(FindingStrategy)} that used TakeClosest.")]
    public event ChartPointHandler? ChartPointPointerDown;

    /// <inheritdoc />
    public event VisualElementsHandler? VisualElementsPointerDown;

    /// <summary>
    /// Creates the core chart, starts observing the collections and loads the chart. Called once by the
    /// component that owns the view, after the derived constructor ran.
    /// </summary>
    public void Initialize()
    {
        if (_coreChart is not null) return;

        _coreChart = CreateCoreChart();
        _coreChart.Measuring += _ => Measuring?.Invoke(this);
        _coreChart.UpdateStarted += _ => UpdateStarted?.Invoke(this);
        _coreChart.UpdateFinished += _ => UpdateFinished?.Invoke(this);

        _observer.Initialize();
        _coreChart.Load();
    }

    /// <summary>Asks the chart to measure again (throttled, as LiveCharts does for every change).</summary>
    public void Update()
    {
        if (_coreChart is null || _disposed) return;
        _coreChart.Update();
    }

    /// <inheritdoc />
    public IEnumerable<ChartPoint> GetPointsAt(
        LvcPointD point, FindingStrategy strategy = FindingStrategy.Automatic, FindPointFor findPointFor = FindPointFor.HoverEvent)
    {
        if (strategy == FindingStrategy.Automatic)
            strategy = CoreChart.Series.GetFindingStrategy();

        return CoreChart.Series.SelectMany(series =>
            series.FindHitPoints(CoreChart, new(point), strategy, findPointFor));
    }

    /// <inheritdoc />
    public IEnumerable<IChartElement> GetVisualsAt(LvcPointD point) =>
        CoreChart.VisualElements.SelectMany(visual =>
            ((VisualElement)visual).IsHitBy(CoreChart, new(point)));

    /// <summary>Forwards a pointer press, in the chart's coordinates.</summary>
    public void OnPointerDown(float x, float y, bool isSecondaryAction = false) =>
        _coreChart?.InvokePointerDown(new LvcPoint(x, y), isSecondaryAction);

    /// <summary>Forwards a pointer move, in the chart's coordinates.</summary>
    public void OnPointerMove(float x, float y) =>
        _coreChart?.InvokePointerMove(new LvcPoint(x, y));

    /// <summary>Forwards a pointer release, in the chart's coordinates.</summary>
    public void OnPointerUp(float x, float y, bool isSecondaryAction = false) =>
        _coreChart?.InvokePointerUp(new LvcPoint(x, y), isSecondaryAction);

    /// <summary>Forwards the pointer leaving the chart.</summary>
    public void OnPointerLeave() => _coreChart?.InvokePointerLeft();

    /// <summary>Forwards a wheel or trackpad scroll over the chart; only cartesian charts zoom.</summary>
    public virtual void OnWheel(float x, float y, float deltaY) { }

    /// <inheritdoc />
    void IChartView.InvokeOnUIThread(Action action)
    {
        if (_disposed) return;

        // the chart measures from a thread-pool task (its update throttler); the engine's dispatcher runs
        // the measure inside the next frame, on the thread that owns the DOM and the chart's collections.
        _post(() => { if (!_disposed) action(); });
        Host.RequestFrame();
    }

    /// <inheritdoc />
    void IChartView.Invalidate() => Host.CanvasCore.Invalidate();

    /// <inheritdoc />
    void IChartView.OnDataPointerDown(IEnumerable<ChartPoint> points, LvcPoint pointer)
    {
        DataPointerDown?.Invoke(this, points);
#pragma warning disable CS0618 // Type or member is obsolete
        ChartPointPointerDown?.Invoke(this, points.FindClosestTo(pointer));
#pragma warning restore CS0618
    }

    /// <inheritdoc />
    void IChartView.OnHoveredPointsChanged(IEnumerable<ChartPoint>? newPoints, IEnumerable<ChartPoint>? oldPoints) =>
        HoveredPointsChanged?.Invoke(this, newPoints, oldPoints);

    /// <inheritdoc />
    void IChartView.OnVisualElementPointerDown(IEnumerable<IInteractable> visualElements, LvcPoint pointer) =>
        VisualElementsPointerDown?.Invoke(this, new VisualElementsEventArgs(CoreChart, visualElements, pointer));

    /// <summary>Creates the core chart (cartesian, pie, polar) for this view.</summary>
    protected abstract Chart CreateCoreChart();

    /// <summary>Registers the collections and properties whose changes update the chart.</summary>
    protected virtual ChartObserver ConfigureObserver(ChartObserver observe) => observe
        .Collection(nameof(Series), () => Series)
        .Collection(nameof(VisualElements), () => VisualElements)
        .Property(nameof(Title), () => Title!);

    /// <summary>Assigns an observed property: the observer follows the new value and the chart updates.</summary>
    protected void SetObserved<T>(string name, ref T field, T value, bool resetSeriesIds = false)
    {
        field = value;
        if (_disposed) return;

        _observer[name].Observer.Initialize(value);
        if (_coreChart is null) return;

        _coreChart.Update();

        // a new series collection restarts the series ids, which choose the palette colors.
        if (resetSeriesIds) _coreChart.ResetNextSeriesId();
    }

    private static Paint? AsText(Paint? paint)
    {
        paint?.PaintStyle = PaintStyle.Text;
        return paint;
    }

    private void OnHostSizeChanged() => Update();

    /// <summary>Stops observing, unloads the chart and releases the canvas.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Host.SizeChanged -= OnHostSizeChanged;
        _observer.Dispose();
        _coreChart?.Unload();
        Host.Dispose();
        GC.SuppressFinalize(this);
    }
}
