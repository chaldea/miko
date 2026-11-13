using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Platform;
using Miko.Routing;
using Miko.Styling;

namespace Miko.Docs.Demo;

public enum DemoRegion
{
    Component,
    Properties,
    Console,
}

/// <summary>
/// Hosts the component example in an independent app/engine and composites its offscreen surface.
/// Only playground state is shared; viewport, overlays, media, layout, scrolling and invalidation are isolated.
/// </summary>
public sealed class IsolatedDemoPreview : ComponentBase
{
    public const float LogicalWidth = 346;
    public const float LogicalHeight = 606;

    private readonly PreviewCanvasElement _canvas;
    private MikoAppContext? _preview;
    private NavigationManager? _navigation;
    private RenderSurface? _surface;
    private Image? _image;
    private float _scale;
    private bool _initialized;
    private bool _pointerActive;
    private float _viewportWidth = LogicalWidth;
    private float _viewportHeight = LogicalHeight;
    private long _lastFrameTimestamp;

    [Parameter] public DemoRegion Region { get; set; }
    [Inject] public DemoState State { get; set; } = null!;
    [Inject] public PlatformInfo Platform { get; set; } = null!;
    [Inject] public MikoEngine OuterEngine { get; set; } = null!;
    [Inject] public MikoInteractionController OuterController { get; set; } = null!;
    [Inject] public IGraphicsFactory Graphics { get; set; } = null!;

    public IsolatedDemoPreview()
    {
        _canvas = new PreviewCanvasElement(this);
        _canvas.Style = new Style
        {
            Display = Display.Block,
            Width = Length.Percent(100),
            Height = Length.Percent(100),
            BackgroundColor = Color.White,
        };
        _canvas.Paint = PaintPreview;
        _canvas.OnPointerDown = OnPointerDown;
        _canvas.OnPointerUp = OnPointerUp;
        _canvas.OnPointerMove = OnPointerMove;
        _canvas.OnPointerCancel = OnPointerCancel;
        _canvas.OnMouseLeave = OnMouseLeave;
        _canvas.OnWheel = OnWheel;
        _canvas.OnKeyDown = OnKeyDown;
        _canvas.OnKeyUp = OnKeyUp;
        _canvas.OnBlur = _ =>
        {
            _preview?.Controller.SetFocus(null);
            WakeOuter();
        };
    }

    protected override void OnInitialized()
    {
        _preview = Region switch
        {
            DemoRegion.Properties => App.CreateRegionContext<DemoPropertyPanel>(State, Platform, 260, 640),
            DemoRegion.Console => App.CreateRegionContext<DemoConsolePanel>(State, Platform, 1200, 142),
            _ => App.CreatePreviewContext(State, Platform),
        };
        _preview.Controller.Engine.GraphicsDevice = OuterEngine.GraphicsDevice;
        _preview.Controller.AttachInputMethod(_canvas.InputMethod);
        if (Region == DemoRegion.Component)
            _navigation = _preview.Services.GetRequiredService<NavigationManager>();
        _preview.Engine.Dispatcher.Posted += WakeOuter;
        State.ComponentChanged += OnComponentChanged;
        State.PreviewChanged += OnPreviewChanged;
        if (Region == DemoRegion.Console) State.Changed += OnConsoleChanged;
        SyncNavigation();
    }

    protected override void OnParametersSet()
    {
        if (Region == DemoRegion.Properties)
        {
            _canvas.Style = new Style
            {
                Display = Display.Block, Position = Position.Absolute,
                Top = Length.Px(0), Right = Length.Px(0), Bottom = Length.Px(0), Width = Length.Px(260),
                BackgroundColor = Color.White,
            };
        }
        else if (Region == DemoRegion.Console)
        {
            _canvas.Style = new Style
            {
                Display = Display.Block, Position = Position.Absolute,
                Right = Length.Px(0), Bottom = Length.Px(0), Left = Length.Px(0), Height = Length.Px(142),
                BackgroundColor = Color.White,
            };
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddElement(0, _canvas);

    private void OnComponentChanged()
    {
        SyncNavigation();
        WakeOuter();
    }

    private void OnPreviewChanged() => WakeOuter();
    private void OnConsoleChanged() => WakeOuter();

    private void SyncNavigation()
    {
        if (_navigation is null) return;
        var path = State.Current.Id == "button" ? "/" : $"/{State.Current.Id}";
        if (_navigation.CurrentPath != path) _navigation.NavigateTo(path, NavigationDirection.Root);
    }

    private void WakeOuter()
    {
        if (_initialized) OuterEngine.InvalidateElement(_canvas);
        else OuterEngine.RequestRepaint();
        // A nested engine has no host of its own. Wake the outer on-demand Browser host as well,
        // especially for chart measurement, image decoding, animations and media timers.
        OuterEngine.Dispatcher.Post(static () => { });
    }

    private void PaintPreview(IDrawingContext canvas, RectF bounds)
    {
        if (_preview is null || bounds.Width <= 0 || bounds.Height <= 0) return;
        _viewportWidth = bounds.Width;
        _viewportHeight = bounds.Height;
        EnsureInitialized(canvas.DeviceScale);
        if (_image is null || _preview.Controller.HasPendingWork)
        {
            var now = Stopwatch.GetTimestamp();
            var delta = _lastFrameTimestamp == 0 ? 0 : Stopwatch.GetElapsedTime(_lastFrameTimestamp, now).TotalSeconds;
            _lastFrameTimestamp = now;
            _preview.Controller.RenderFrame(_surface!.Context, _viewportWidth, _viewportHeight, (float)delta, context =>
            {
                context.Clear(_preview.Controller.Engine.GetRootBackgroundColor() ?? Color.White);
                context.Save();
                context.Scale(_scale, _scale);
                _preview.Controller.Engine.Render(context);
                context.Restore();
            });
            _surface!.Flush();
            _image?.Dispose();
            _image = _surface.Snapshot();
        }

        if (_image is not null) canvas.DrawImage(_image, bounds, ImageSampling.Nearest);
        // Mirror BrowserHost's frame-end rule: nested motion canvases may request another frame
        // while painting. Keep the outer host alive until the inner engine becomes idle.
        if (_preview.Controller.HasPendingWork) WakeOuter();
    }

    private void EnsureInitialized(float scale)
    {
        if (_preview is null) return;
        var pixelWidth = Math.Max(1, (int)MathF.Round(_viewportWidth * scale));
        var pixelHeight = Math.Max(1, (int)MathF.Round(_viewportHeight * scale));
        if (_surface is not null && _surface.Width == pixelWidth && _surface.Height == pixelHeight && _scale == scale) return;

        _image?.Dispose();
        _image = null;
        _surface?.Dispose();
        _scale = scale;
        _surface = Graphics.CreateSurface(pixelWidth, pixelHeight);
        if (!_initialized)
        {
            SyncNavigation();
            _preview.Controller.Initialize(_surface.Context, _viewportWidth, _viewportHeight);
            _preview.Controller.RunPostInitHooks();
            _initialized = true;
        }
        else
        {
            _preview.Controller.SetViewportSize(_viewportWidth, _viewportHeight);
        }
    }

    private void OnPointerDown(PointerEventArgs args)
    {
        if (_preview is null) return;
        OuterController.SetFocus(_canvas);
        args.PreventDefault();
        if (args.PointerType == PointerType.Touch && _preview.Controller.ScrollBehavior is DefaultScrollBehavior)
            _preview.Controller.SetScrollBehavior(new ElasticScrollBehavior(new InertialScrollBehavior()));
        _pointerActive = true;
        _preview.Controller.OnPointerDown(args.OffsetX, args.OffsetY, args.Button, args.PointerType, args.PointerId);
        WakeOuter();
    }

    private void OnPointerUp(PointerEventArgs args)
    {
        if (_preview is null) return;
        _pointerActive = false;
        _preview.Controller.OnPointerUp(args.OffsetX, args.OffsetY, args.Button, args.PointerType, args.PointerId);
        WakeOuter();
    }

    private void OnPointerMove(PointerEventArgs args)
    {
        if (_preview is null) return;
        if (_pointerActive || IsInside(args.OffsetX, args.OffsetY))
            _preview.Controller.OnPointerMove(args.OffsetX, args.OffsetY);
    }

    private void OnPointerCancel(PointerEventArgs args)
    {
        if (_preview is null) return;
        _pointerActive = false;
        _preview.Controller.OnPointerCancel(args.OffsetX, args.OffsetY, args.PointerId);
        WakeOuter();
    }

    private void OnMouseLeave(MouseEventArgs args)
    {
        if (!_pointerActive) _preview?.Controller.OnPointerMove(-1, -1);
    }

    private void OnWheel(WheelEventArgs args)
    {
        if (_preview is null) return;
        args.PreventDefault();
        _preview.Controller.OnScroll(args.OffsetX, args.OffsetY, args.DeltaX, args.DeltaY);
        WakeOuter();
    }

    private void OnKeyDown(KeyboardEventArgs args)
    {
        if (_preview is null) return;
        var handled = false;
        if (args.Repeat) _preview.Controller.RepeatKey(args.Code);
        else handled = _preview.Controller.OnKeyDown(args.Code, Modifiers(args));
        // Editing keys were already applied by the inner controller. Ordinary keys must reach
        // the browser's native editor so it can emit beforeinput / IME text commits.
        if (handled || args.Code is MikoKey.Backspace or MikoKey.Delete or MikoKey.Left or MikoKey.Right
            or MikoKey.Home or MikoKey.End or MikoKey.Enter)
            args.PreventDefault();
        WakeOuter();
    }

    private void OnKeyUp(KeyboardEventArgs args)
    {
        _preview?.Controller.OnKeyUp(args.Code, Modifiers(args));
        args.PreventDefault();
        WakeOuter();
    }

    private static MikoKeyModifiers Modifiers(KeyboardEventArgs args)
    {
        var modifiers = MikoKeyModifiers.None;
        if (args.CtrlKey) modifiers |= MikoKeyModifiers.Control;
        if (args.ShiftKey) modifiers |= MikoKeyModifiers.Shift;
        if (args.AltKey) modifiers |= MikoKeyModifiers.Alt;
        if (args.MetaKey) modifiers |= MikoKeyModifiers.Meta;
        return modifiers;
    }

    private bool IsInside(float x, float y) => x >= 0 && y >= 0 && x <= _viewportWidth && y <= _viewportHeight;

    protected override void OnDispose()
    {
        State.ComponentChanged -= OnComponentChanged;
        State.PreviewChanged -= OnPreviewChanged;
        if (Region == DemoRegion.Console) State.Changed -= OnConsoleChanged;
        if (_preview is not null)
        {
            _preview.Engine.Dispatcher.Posted -= WakeOuter;
            _preview.Controller.OnPointerCancel(0, 0);
            _preview.Dispose();
        }
        _image?.Dispose();
        _surface?.Dispose();
        _image = null;
        _surface = null;
    }

    /// <summary>Focus/keyboard bridge for the outer controller; actual editing happens in the inner controller.</summary>
    private sealed class PreviewCanvasElement(IsolatedDemoPreview owner) : CanvasElement, ITextEditable
    {
        public PreviewInputMethod InputMethod { get; } = new();
        public string? Value { get => InputMethod.State?.Text ?? string.Empty; set { } }
        public int CursorPosition
        {
            get => InputMethod.State?.CursorPosition ?? 0;
            set => owner._preview?.Controller.SetTextSelection(value, value);
        }
        public bool IsEditable => true;
        public bool IsMultiline => InputMethod.State?.IsMultiline == true;

        public void InsertText(string text)
        {
            owner._preview?.Controller.OnTextInput(text);
            owner.WakeOuter();
        }

        public bool Backspace()
        {
            var before = Value;
            owner._preview?.Controller.DeleteSurroundingText(1, 0);
            owner.WakeOuter();
            return Value != before;
        }

        public bool Delete()
        {
            var before = Value;
            owner._preview?.Controller.DeleteSurroundingText(0, 1);
            owner.WakeOuter();
            return Value != before;
        }

        public void MoveCursorToEnd() => CursorPosition = Value?.Length ?? 0;
    }

    private sealed class PreviewInputMethod : InputMethodBase
    {
        public InputMethodState? State { get; private set; }
        public override void SetState(InputMethodState? state) => State = state;
    }
}
