using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Styling;

namespace Miko.Components.Map;

public sealed class MikoMap : ComponentBase, IDisposable
{
    [Inject] public IMapProvider Provider { get; set; } = null!;
    [Inject] public MikoEngine Engine { get; set; } = null!;
    [Parameter] public MapCamera Camera { get; set; } = MapCamera.Beijing;
    [Parameter] public EventCallback<MapCamera> CameraChanged { get; set; }
    [Parameter] public IReadOnlyList<MapMarker>? Markers { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public Style? Style { get; set; }

    public MapCamera CurrentCamera { get; private set; } = MapCamera.Beijing;
    public Exception? Error => _map?.Error;
    private readonly DivElement _root = new();
    private readonly CanvasElement _canvas = new() { Class = "miko-map-canvas" };
    private MapEngine? _map;
    private MapCamera? _lastParameter;
    private int? _pointer;
    private float _lastX, _lastY;
    private bool _dragged;

    protected override void OnInitialized()
    {
        if (Provider is null) throw new InvalidOperationException("Register a map provider with AddMikoMap() or AddAMap().");
        _map = new MapEngine(Provider, Engine);
        _canvas.Style = new() { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100) };
        _canvas.Paint = (context, bounds) => _map.Draw(context, bounds, CurrentCamera, Markers);
        _canvas.OnPointerDown = e =>
        {
            if (!e.IsPrimary || e.Button != MouseButton.Left) return;
            e.PreventDefault();
            _pointer = e.PointerId;
            _dragged = false;
            _lastX = e.X;
            _lastY = e.Y;
        };
        _canvas.OnPointerMove = e =>
        {
            if (e.PointerId != _pointer) return;
            float dx = e.X - _lastX, dy = e.Y - _lastY;
            if (!_dragged && Math.Abs(dx) + Math.Abs(dy) < 3) return;
            e.PreventDefault();
            _dragged = true;
            _lastX = e.X;
            _lastY = e.Y;
            MoveTo(CurrentCamera.PanBy(dx, dy));
        };
        _canvas.OnPointerUp = e =>
        {
            if (e.PointerId != _pointer) return;
            _pointer = null;
            if (!_dragged) Retry();
        };
        _canvas.OnPointerCancel = e => { if (e.PointerId == _pointer) _pointer = null; };
        _canvas.OnWheel = e =>
        {
            if (e.DeltaY == 0) return;
            e.PreventDefault();
            e.StopPropagation();
            MoveTo(CurrentCamera.ZoomAt(CurrentCamera.Zoom - Math.Sign(e.DeltaY),
                e.OffsetX - e.TargetWidth / 2, e.OffsetY - e.TargetHeight / 2));
        };
        var controls = new DivElement { Style = new()
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column, Position = Position.Absolute,
            Top = Length.Px(12), Right = Length.Px(12), Gap = Length.Px(2)
        } };
        controls.Children.Add(ZoomButton("+", 1));
        controls.Children.Add(ZoomButton("−", -1));
        _root.Children.AddRange([_canvas, controls]);
    }

    private ButtonElement ZoomButton(string text, int step) => new()
    {
        TextContent = text, Class = step > 0 ? "miko-map-zoom-in" : "miko-map-zoom-out",
        Style = new() { Width = Length.Px(42), Height = Length.Px(42), FontSize = Length.Px(22),
            BackgroundColor = Color.White, Color = Color.FromHex("#263b41"), BorderRadius = Length.Px(5) },
        OnClick = _ => MoveTo(CurrentCamera.ZoomAt(CurrentCamera.Zoom + step))
    };

    protected override void OnParametersSet()
    {
        if (Camera != _lastParameter) CurrentCamera = Camera;
        _lastParameter = Camera;
        _root.Class = $"miko-map {Class}".TrimEnd();
        var style = new Style();
        if (Style is not null) style.Merge(Style);
        style.Merge(new Style { Position = Position.Relative, Width = Length.Percent(100),
            Height = Length.Px(360), Overflow = Overflow.Hidden });
        _root.Style = style;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddElement(0, _root);

    public void MoveTo(MapCamera camera)
    {
        if (camera == CurrentCamera) return;
        CurrentCamera = camera;
        Engine.RequestRepaint();
        _ = CameraChanged.InvokeAsync(camera);
    }

    public void Retry() => _map?.Retry();

    protected override void OnDispose() => Dispose();
    public void Dispose()
    {
        _canvas.Paint = null;
        _map?.Dispose();
    }
}
