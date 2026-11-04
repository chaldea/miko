using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Hosting;
using LiveChartsCore.Painting;
using LiveChartsCore.Themes;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Styling;

namespace Miko.Charts;

/// <summary>
/// Base class of the chart components: hosts a LiveCharts view in a <c>&lt;canvas&gt;</c> and forwards the
/// parameters every chart shares.
/// </summary>
/// <typeparam name="TView">The LiveCharts view the component drives.</typeparam>
/// <remarks>
/// <para>The canvas fills the component's box. Size it with <see cref="Style"/> or <see cref="Class"/>; with no
/// height the chart takes <see cref="DefaultHeight"/>.</para>
/// <para>Parameters are assigned to the view on every render of the parent: assign a collection once and change
/// its contents (an <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/> updates the chart), or
/// assign a new collection to replace it.</para>
/// </remarks>
public abstract class ChartComponentBase<TView> : ComponentBase, IDisposable
    where TView : ChartView
{
    /// <summary>The height a chart takes when its style sets none.</summary>
    public const float DefaultHeight = 300;

    private TView? _view;
    private bool _disposed;
    private IEnumerable<ISeries>? _series;
    private IEnumerable<IChartElement>? _visualElements;

    /// <summary>The series to draw.</summary>
    [Parameter] public IEnumerable<ISeries>? Series { get; set; }

    /// <summary>Visual elements (labels, shapes, needles) drawn with the chart.</summary>
    [Parameter] public IEnumerable<IChartElement>? VisualElements { get; set; }

    /// <summary>The chart title, for example a <c>LabelVisual</c>.</summary>
    [Parameter] public IChartElement? Title { get; set; }

    /// <summary>Where the legend goes; hidden by default.</summary>
    [Parameter] public LegendPosition LegendPosition { get; set; } = LiveCharts.DefaultSettings.LegendPosition;

    /// <summary>Where the tooltip opens; <see cref="TooltipPosition.Hidden"/> turns it off.</summary>
    [Parameter] public TooltipPosition TooltipPosition { get; set; } = LiveCharts.DefaultSettings.TooltipPosition;

    /// <summary>The legend; the theme's default legend when null.</summary>
    [Parameter] public IChartLegend? Legend { get; set; }

    /// <summary>The tooltip; the theme's default tooltip when null.</summary>
    [Parameter] public IChartTooltip? Tooltip { get; set; }

    /// <summary>The paint of the legend text; the theme's when null.</summary>
    [Parameter] public Paint? LegendTextPaint { get; set; }

    /// <summary>The paint behind the legend; the theme's when null.</summary>
    [Parameter] public Paint? LegendBackgroundPaint { get; set; }

    /// <summary>The paint of the tooltip text; the theme's when null.</summary>
    [Parameter] public Paint? TooltipTextPaint { get; set; }

    /// <summary>The paint behind the tooltip; the theme's when null.</summary>
    [Parameter] public Paint? TooltipBackgroundPaint { get; set; }

    /// <summary>The space between the control edges and the plot area; computed from the axes and legend when null.</summary>
    [Parameter] public LiveChartsCore.Measure.Margin? DrawMargin { get; set; }

    /// <summary>How long transitions take; the global default when null.</summary>
    [Parameter] public TimeSpan? AnimationsSpeed { get; set; }

    /// <summary>The easing of transitions; null turns animations off.</summary>
    [Parameter] public Func<float, float>? EasingFunction { get; set; } = LiveCharts.DefaultSettings.EasingFunction;

    /// <summary>A theme for this chart only; the global theme when null.</summary>
    [Parameter] public Theme? ChartTheme { get; set; }

    /// <summary>The background of the chart area. Transparent shows what is behind the component.</summary>
    [Parameter] public Color Background { get; set; } = Color.Transparent;

    /// <summary>Classes of the canvas element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline style of the canvas element, merged over the default block of <see cref="DefaultHeight"/>.</summary>
    [Parameter] public Style? Style { get; set; }

    /// <summary>Raised when the pointer goes down over data points.</summary>
    [Parameter] public EventCallback<IEnumerable<ChartPoint>> DataPointerDown { get; set; }

    /// <summary>Raised when the points under the pointer change.</summary>
    [Parameter] public EventCallback<ChartHoverChange> HoveredPointsChanged { get; set; }

    /// <summary>Raised after the chart measured.</summary>
    [Parameter] public EventCallback UpdateFinished { get; set; }

    /// <summary>The engine the chart renders in: it supplies the frame requests and the render-thread dispatcher.</summary>
    [Inject] public MikoEngine? Engine { get; set; }

    /// <summary>Lays out the label text when the component renders without an engine (tests).</summary>
    [Inject] public Miko.Graphics.IGraphicsFactory? Graphics { get; set; }

    /// <summary>The chart options registered by <c>AddMikoCharts</c>.</summary>
    [Inject] public MikoChartsOptions? Options { get; set; }

    /// <summary>The LiveCharts view; null before the first render.</summary>
    public TView? View => _view;

    /// <summary>The core chart; null before the first render.</summary>
    public Chart? CoreChart => _view?.CoreChart;

    /// <summary>Creates the view the component drives.</summary>
    protected abstract TView CreateView(MotionCanvasHost host, Action<Action> post);

    /// <summary>Copies the chart-specific parameters to the view.</summary>
    protected virtual void ApplyParameters(TView view) { }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // the engine (and so the view) only exists once the component renders inside an app: injection
        // runs after the first OnParametersSet. Parameters reach the view in BuildRenderTree then.
        if (_view is not null) Apply(_view);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var view = _view ??= Create();
        var element = view.Host.Element;
        element.Class = string.IsNullOrEmpty(Class) ? "miko-chart" : "miko-chart " + Class;
        element.Style = BuildStyle();
        builder.AddElement(0, element);
    }

    /// <inheritdoc />
    protected override void OnDispose() => Dispose();

    /// <summary>Unloads the chart and releases its canvas.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _view?.Dispose();
        GC.SuppressFinalize(this);
    }

    private TView Create()
    {
        var engine = Engine;
        var graphics = engine?.Graphics ?? Graphics;
        if (graphics is not null) LiveChartsMiko.UseGraphics(graphics);

        Action requestFrame = engine is null ? static () => { } : engine.RequestRepaint;
        Action<Action> post = engine is null ? static action => action() : engine.Dispatcher.Post;

        var host = new MotionCanvasHost(requestFrame);
        var view = CreateView(host, post);

        view.DataPointerDown += OnDataPointerDown;
        view.HoveredPointsChanged += OnHoveredPointsChanged;
        view.UpdateFinished += OnUpdateFinished;

        WirePointer(host, view);
        Apply(view);
        view.Initialize();
        return view;
    }

    private void Apply(TView view)
    {
        // collections are assigned only when the parameter changed: assigning restarts the observers and the
        // series ids (the palette colors), which a parent render must not do with the same collection.
        if (!ReferenceEquals(_series, Series))
        {
            _series = Series;
            view.Series = Series ?? [];
        }

        if (!ReferenceEquals(_visualElements, VisualElements))
        {
            _visualElements = VisualElements;
            view.VisualElements = VisualElements ?? [];
        }

        if (!ReferenceEquals(view.Title, Title)) view.Title = Title;
        if (view.LegendPosition != LegendPosition) view.LegendPosition = LegendPosition;
        if (view.TooltipPosition != TooltipPosition) view.TooltipPosition = TooltipPosition;
        if (Legend is not null && !ReferenceEquals(view.Legend, Legend)) view.Legend = Legend;
        if (Tooltip is not null && !ReferenceEquals(view.Tooltip, Tooltip)) view.Tooltip = Tooltip;
        if (!ReferenceEquals(view.LegendTextPaint, LegendTextPaint)) view.LegendTextPaint = LegendTextPaint;
        if (!ReferenceEquals(view.LegendBackgroundPaint, LegendBackgroundPaint)) view.LegendBackgroundPaint = LegendBackgroundPaint;
        if (!ReferenceEquals(view.TooltipTextPaint, TooltipTextPaint)) view.TooltipTextPaint = TooltipTextPaint;
        if (!ReferenceEquals(view.TooltipBackgroundPaint, TooltipBackgroundPaint)) view.TooltipBackgroundPaint = TooltipBackgroundPaint;
        if (!Equals(view.DrawMargin, DrawMargin)) view.DrawMargin = DrawMargin;
        if (!ReferenceEquals(view.ChartTheme, ChartTheme)) view.ChartTheme = ChartTheme;

        view.AnimationsSpeed = AnimationsSpeed ?? Options?.AnimationsSpeed ?? LiveCharts.DefaultSettings.AnimationsSpeed;
        view.EasingFunction = EasingFunction;
        view.Host.Background = new LvcColor(Background.R, Background.G, Background.B, Background.A);

        ApplyParameters(view);
    }

    private Style BuildStyle()
    {
        // Merge keeps the values already set, so the user's style goes in first and the defaults fill the
        // rest. A fresh style each time: the parameter object belongs to the caller.
        var style = new Style();
        if (Style is not null) style.Merge(Style);
        style.Merge(new Style
        {
            Display = Display.Block,
            Width = Length.Percent(100),
            Height = Length.Px(DefaultHeight),
            BackgroundColor = Background,
        });
        return style;
    }

    private static void WirePointer(MotionCanvasHost host, TView view)
    {
        var element = host.Element;

        // offsets are relative to the border box; the chart draws in the content box, which is the same
        // box for the plain <canvas> the component renders (no border or padding of its own).
        // the interaction controller keeps sending moves to the element a press started on, so a drag
        // (panning, a zoom section) keeps going when the pointer leaves the chart.
        element.OnPointerDown = e => view.OnPointerDown(e.OffsetX, e.OffsetY, e.Button == MouseButton.Right);
        element.OnPointerMove = e => view.OnPointerMove(e.OffsetX, e.OffsetY);
        element.OnPointerUp = e => view.OnPointerUp(e.OffsetX, e.OffsetY, e.Button == MouseButton.Right);
        element.OnPointerCancel = _ => view.OnPointerLeave();
        element.OnMouseLeave = _ => view.OnPointerLeave();
    }

    private void OnDataPointerDown(IChartView chart, IEnumerable<ChartPoint> points)
    {
        if (DataPointerDown.HasDelegate) _ = DataPointerDown.InvokeAsync(points);
    }

    private void OnHoveredPointsChanged(IChartView chart, IEnumerable<ChartPoint>? newPoints, IEnumerable<ChartPoint>? oldPoints)
    {
        if (HoveredPointsChanged.HasDelegate)
            _ = HoveredPointsChanged.InvokeAsync(new ChartHoverChange(newPoints ?? [], oldPoints ?? []));
    }

    private void OnUpdateFinished(IChartView chart)
    {
        if (UpdateFinished.HasDelegate) _ = UpdateFinished.InvokeAsync();
    }
}

/// <summary>The points under the pointer before and after a move.</summary>
/// <param name="NewPoints">The points now hovered.</param>
/// <param name="OldPoints">The points no longer hovered.</param>
public sealed record ChartHoverChange(IEnumerable<ChartPoint> NewPoints, IEnumerable<ChartPoint> OldPoints);
