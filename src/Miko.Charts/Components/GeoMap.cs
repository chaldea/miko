using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Hosting;
using LiveChartsCore.Painting;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Styling;

namespace Miko.Charts;

/// <summary>
/// A world map whose lands are colored by heat series. A drag pans the map, the wheel zooms it.
/// </summary>
/// <example>
/// <code>
/// &lt;GeoMap Series="@_heat" /&gt;
/// </code>
/// </example>
public sealed class GeoMap : ComponentBase, IDisposable
{
    private GeoMapView? _view;
    private IEnumerable<IGeoSeries>? _series;
    private bool _disposed;

    /// <summary>The heat series that color the lands.</summary>
    [Parameter] public IEnumerable<IGeoSeries>? Series { get; set; }

    /// <summary>The map to draw; the bundled world map when null.</summary>
    [Parameter] public DrawnMap? ActiveMap { get; set; }

    /// <summary>The projection of the map.</summary>
    [Parameter] public MapProjection MapProjection { get; set; } = MapProjection.Default;

    /// <summary>The paint of the land borders; white when null.</summary>
    [Parameter] public Paint? Stroke { get; set; }

    /// <summary>The paint of lands no series colors; light gray when null.</summary>
    [Parameter] public Paint? Fill { get; set; }

    /// <summary>The background of the map area.</summary>
    [Parameter] public Color Background { get; set; } = Color.Transparent;

    /// <summary>Classes of the canvas element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline style of the canvas element, merged over the default block.</summary>
    [Parameter] public Style? Style { get; set; }

    /// <summary>The engine the map renders in.</summary>
    [Inject] public MikoEngine? Engine { get; set; }

    /// <summary>Lays out text when rendering without an engine (tests).</summary>
    [Inject] public Miko.Graphics.IGraphicsFactory? Graphics { get; set; }

    /// <summary>The LiveCharts view; null before the first render.</summary>
    public GeoMapView? View => _view;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (_view is not null) Apply(_view);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var view = _view ??= Create();
        var element = view.Host.Element;
        element.Class = string.IsNullOrEmpty(Class) ? "miko-chart miko-geo-map" : "miko-chart miko-geo-map " + Class;

        var style = new Style();
        if (Style is not null) style.Merge(Style);
        style.Merge(new Style
        {
            Display = Display.Block,
            Width = Length.Percent(100),
            Height = Length.Px(ChartComponentBase<CartesianChartView>.DefaultHeight),
            BackgroundColor = Background,
        });
        element.Style = style;

        builder.AddElement(0, element);
    }

    /// <inheritdoc />
    protected override void OnDispose() => Dispose();

    /// <summary>Unloads the map and releases its canvas.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _view?.Dispose();
    }

    private GeoMapView Create()
    {
        var engine = Engine;
        var graphics = engine?.Graphics ?? Graphics;
        if (graphics is not null) LiveChartsMiko.UseGraphics(graphics);

        Action requestFrame = engine is null ? static () => { } : engine.RequestRepaint;
        Action<Action> post = engine is null ? static action => action() : engine.Dispatcher.Post;

        var host = new MotionCanvasHost(requestFrame);
        var view = new GeoMapView(host, post);

        var element = host.Element;
        element.OnPointerDown = e => view.OnPointerDown(e.OffsetX, e.OffsetY);
        element.OnPointerMove = e => view.OnPointerMove(e.OffsetX, e.OffsetY);
        element.OnPointerUp = e => view.OnPointerUp(e.OffsetX, e.OffsetY);
        element.OnPointerCancel = _ => view.OnPointerLeave();
        element.OnMouseLeave = _ => view.OnPointerLeave();

        Apply(view);
        view.Initialize();
        return view;
    }

    private void Apply(GeoMapView view)
    {
        if (!ReferenceEquals(_series, Series))
        {
            _series = Series;
            view.Series = Series ?? [];
        }

        if (ActiveMap is not null && !ReferenceEquals(view.ActiveMap, ActiveMap)) view.ActiveMap = ActiveMap;
        if (view.MapProjection != MapProjection) view.MapProjection = MapProjection;
        if (Stroke is not null && !ReferenceEquals(view.Stroke, Stroke)) view.Stroke = Stroke;
        if (Fill is not null && !ReferenceEquals(view.Fill, Fill)) view.Fill = Fill;
        view.Host.Background = new LvcColor(Background.R, Background.G, Background.B, Background.A);
    }
}
