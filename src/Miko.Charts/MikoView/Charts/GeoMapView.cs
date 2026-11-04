// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors),
// skiasharp/_Shared/SourceGenMapChart.*.cs.

using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.Kernel.Observers;
using LiveChartsCore.Motion;
using LiveChartsCore.MikoView.Hosting;
using LiveChartsCore.Painting;

namespace LiveChartsCore.MikoView.Charts;

/// <summary>The view of a geographic map: lands colored by heat series.</summary>
/// <remarks>Every member is a UI-thread API, as for <see cref="ChartView"/>.</remarks>
public class GeoMapView : IGeoMapView, IDisposable
{
    private readonly Action<Action> _post;
    private readonly CollectionDeepObserver _seriesObserver;
    private IEnumerable<IGeoSeries> _series = [];
    private DrawnMap _activeMap;
    private MapProjection _mapProjection = MapProjection.Default;
    private Paint? _stroke;
    private Paint? _fill;
    private object _syncContext = new();
    private GeoMapChart? _coreChart;
    private bool _disposed;

    static GeoMapView()
    {
        _ = LiveChartsMiko.EnsureInitialized();
    }

    /// <summary>Initializes a new instance of the <see cref="GeoMapView"/> class.</summary>
    /// <param name="host">The canvas the map draws in.</param>
    /// <param name="post">Runs an action on the UI (render) thread at the next frame.</param>
    public GeoMapView(MotionCanvasHost host, Action<Action> post)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(post);

        Host = host;
        _post = post;
        _seriesObserver = new CollectionDeepObserver(() => _coreChart?.Update());
        _activeMap = Maps.GetWorldMap();
        _stroke = DefaultPaint(new LvcColor(255, 255, 255, 255), PaintStyle.Stroke);
        _fill = DefaultPaint(new LvcColor(240, 240, 240, 255), PaintStyle.Fill);

        Host.CanvasCore.Sync = _syncContext;
        Host.SizeChanged += OnHostSizeChanged;
    }

    /// <summary>Gets the canvas host.</summary>
    public MotionCanvasHost Host { get; }

    /// <inheritdoc />
    public CoreMotionCanvas CoreCanvas => Host.CanvasCore;

    /// <inheritdoc />
    public LvcSize ControlSize => Host.Size;

    /// <inheritdoc />
    public GeoMapChart CoreChart => _coreChart ?? throw new InvalidOperationException("The map has not been initialized yet.");

    /// <inheritdoc />
    public bool DesignerMode => false;

    /// <inheritdoc />
    public bool AutoUpdateEnabled { get; set; } = true;

    /// <inheritdoc />
    public DrawnMap ActiveMap
    {
        get => _activeMap;
        set { _activeMap = value; Update(); }
    }

    /// <inheritdoc />
    public MapProjection MapProjection
    {
        get => _mapProjection;
        set { _mapProjection = value; Update(); }
    }

    /// <inheritdoc />
    public Paint? Stroke
    {
        get => _stroke;
        set { _stroke = WithStyle(value, PaintStyle.Stroke); Update(); }
    }

    /// <inheritdoc />
    public Paint? Fill
    {
        get => _fill;
        set { _fill = WithStyle(value, PaintStyle.Fill); Update(); }
    }

    /// <inheritdoc />
    public IEnumerable<IGeoSeries> Series
    {
        get => _series;
        set
        {
            _series = value;
            if (_disposed) return;
            _seriesObserver.Dispose();
            _seriesObserver.Initialize(value);
            Update();
        }
    }

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

    /// <summary>Creates the core chart and loads it. Called once by the component that owns the view.</summary>
    public void Initialize()
    {
        if (_coreChart is not null) return;
        _coreChart = new GeoMapChart(this);
        _seriesObserver.Initialize(_series);
        _coreChart.Load();
    }

    /// <summary>Asks the map to measure again.</summary>
    public void Update()
    {
        if (_coreChart is null || _disposed) return;
        _coreChart.Update();
    }

    /// <summary>Moves the map so that the land under (<paramref name="x"/>, <paramref name="y"/>) zooms.</summary>
    public void OnWheel(float x, float y, float deltaY)
    {
        if (deltaY == 0) return;
        _coreChart?.ViewTo(new ZoomOnPointerView(
            new LvcPoint(x, y), deltaY < 0 ? Measure.ZoomDirection.ZoomIn : Measure.ZoomDirection.ZoomOut));
    }

    /// <summary>Forwards a pointer press, in the map's coordinates.</summary>
    public void OnPointerDown(float x, float y) => _coreChart?.InvokePointerDown(new LvcPoint(x, y));

    /// <summary>Forwards a pointer move, in the map's coordinates.</summary>
    public void OnPointerMove(float x, float y) => _coreChart?.InvokePointerMove(new LvcPoint(x, y));

    /// <summary>Forwards a pointer release, in the map's coordinates.</summary>
    public void OnPointerUp(float x, float y) => _coreChart?.InvokePointerUp(new LvcPoint(x, y));

    /// <summary>Forwards the pointer leaving the map.</summary>
    public void OnPointerLeave() => _coreChart?.InvokePointerLeft();

    /// <inheritdoc />
    void IGeoMapView.InvokeOnUIThread(Action action)
    {
        if (_disposed) return;
        _post(() => { if (!_disposed) action(); });
        Host.RequestFrame();
    }

    private static Paint DefaultPaint(LvcColor color, PaintStyle style)
    {
        var paint = LiveCharts.DefaultSettings.GetProvider().GetSolidColorPaint(color);
        paint.PaintStyle = style;
        return paint;
    }

    private static Paint? WithStyle(Paint? paint, PaintStyle style)
    {
        paint?.PaintStyle = style;
        return paint;
    }

    private void OnHostSizeChanged() => Update();

    /// <summary>Stops observing, unloads the map and releases the canvas.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Host.SizeChanged -= OnHostSizeChanged;
        _seriesObserver.Dispose();
        _coreChart?.Unload();
        Host.Dispose();
        GC.SuppressFinalize(this);
    }
}
