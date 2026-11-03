using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Graphics;
using Miko.Platform;
using Miko.Platform.Camera;
using Miko.Styling;

namespace Miko.Components.Camera;

/// <summary>
/// A full-screen photo camera: the live preview with a close button at the top and a control panel at
/// the bottom — flash, shutter, switch camera. The shutter takes the photo itself and reports it
/// through <see cref="PhotoTaken"/>; <see cref="Close"/> fires the close button.
/// </summary>
/// <remarks>
/// Taking the photo with the live camera (rather than raising an event and leaving it to
/// <c>ICameraService.TakePhotoAsync</c>, which opens the system camera UI) is the point of the
/// component: the preview, the flash and the capture belong to the same session. Apps that want the
/// system camera call <c>ICameraService</c> directly instead of showing this component.
/// <see cref="Capturing"/> lets a parent veto or record a capture before it happens. The system back
/// button raises <see cref="Close"/> while it has a delegate.
/// </remarks>
public sealed class MikoCamera : ComponentBase, IDisposable
{
    /// <summary>The camera shown first; the switch button toggles between back and front.</summary>
    [Parameter] public CameraFacing Facing { get; set; } = CameraFacing.Back;

    /// <summary>Raised after <see cref="CurrentFacing"/> changes through the switch button.</summary>
    [Parameter] public EventCallback<CameraFacing> FacingChanged { get; set; }

    [Parameter] public CameraFlashMode Flash { get; set; } = CameraFlashMode.Off;

    [Parameter] public EventCallback<CameraFlashMode> FlashChanged { get; set; }

    [Parameter] public CameraResolution Resolution { get; set; } = CameraResolution.High;

    /// <summary>Encoding, quality, size limit and front-camera mirroring of the photo (the flash comes from <see cref="Flash"/>).</summary>
    [Parameter] public CameraCaptureOptions CaptureOptions { get; set; } = new();

    /// <summary>Receives every photo taken with the shutter.</summary>
    [Parameter] public EventCallback<CameraPhoto> PhotoTaken { get; set; }

    /// <summary>Raised before a capture; setting <see cref="CameraCapturingEventArgs.Cancel"/> skips it.</summary>
    [Parameter] public EventCallback<CameraCapturingEventArgs> Capturing { get; set; }

    /// <summary>Raised when a capture fails.</summary>
    [Parameter] public EventCallback<Exception> CaptureFailed { get; set; }

    /// <summary>The close button (top left) and the system back button; the button is hidden without a delegate.</summary>
    [Parameter] public EventCallback Close { get; set; }

    [Parameter] public bool ShowFlashButton { get; set; } = true;

    [Parameter] public bool ShowSwitchButton { get; set; } = true;

    /// <summary>Whether the camera runs (false releases it).</summary>
    [Parameter] public bool Active { get; set; } = true;

    [Parameter] public string? Class { get; set; }

    [Parameter] public Style? Style { get; set; }

    [Parameter] public CameraViewTexts Texts { get; set; } = new();

    [Inject] public MikoEngine? Engine { get; set; }

    /// <summary>Back button, haptics; null without a service scope.</summary>
    [Inject] public IPlatformService? PlatformService { get; set; }

    /// <summary>The camera the view shows now (changes with the switch button).</summary>
    public CameraFacing CurrentFacing { get; private set; }

    public CameraFlashMode CurrentFlash { get; private set; }

    /// <summary>Whether a capture is in progress (the shutter is disabled meanwhile).</summary>
    public bool IsCapturing { get; private set; }

    /// <summary>The preview; its <see cref="MikoCameraView.Camera"/> is the camera element.</summary>
    public MikoCameraView? View { get; private set; }

    // The controls are retained elements with direct handlers: pressing the shutter must not
    // re-render the component (an EventCallback re-renders its receiver after every handler).
    private readonly CameraIcons _icons = new();
    private readonly DivElement _controls = new() { Class = "miko-camera-controls" };
    private readonly ButtonElement _flashButton = new() { Class = "miko-camera-flash" };
    private readonly ButtonElement _shutter = new() { Class = "miko-camera-shutter" };
    private readonly ButtonElement _switchButton = new() { Class = "miko-camera-switch" };
    private readonly ButtonElement _closeButton = new() { Class = "miko-camera-close" };
    private readonly DivElement _flashFx = new() { Class = "miko-camera-flash-fx" };
    private EventCallback<CameraSessionState>? _stateChanged;
    private CameraFacing _lastFacing;
    private CameraFlashMode _lastFlash;
    private bool _initialized, _disposed;
    private IDisposable? _back;
    private float _shutterPress;

    protected override void OnInitialized()
    {
        _flashButton.Style = RoundButtonStyle;
        _flashButton.Children.Add(IconCanvas((c, r) => _icons.Draw(c, r, CurrentFlash == CameraFlashMode.Off ? "flash-off-outline" : "flash-outline", 28,
            CurrentFlash == CameraFlashMode.On ? Color.FromHex("#ffd60a") : Color.White)));
        _flashButton.OnClick = e => { e.StopPropagation(); _ = CycleFlash(); };

        _shutter.Style = ShutterStyle;
        _shutter.Children.Add(IconCanvas(DrawShutter));
        _shutter.OnPointerDown = _ => SetShutterPress(1);
        _shutter.OnPointerUp = _ => SetShutterPress(0);
        _shutter.OnPointerCancel = _ => SetShutterPress(0);
        _shutter.OnMouseLeave = _ => SetShutterPress(0);
        _shutter.OnClick = e => { e.StopPropagation(); _ = TakePhotoAsync(); };

        _switchButton.Style = RoundButtonStyle;
        _switchButton.Children.Add(IconCanvas((c, r) => _icons.Draw(c, r, "camera-reverse-outline", 30, Color.White)));
        _switchButton.OnClick = e => { e.StopPropagation(); _ = SwitchCamera(); };

        _closeButton.Style = CloseButtonStyle;
        _closeButton.Children.Add(IconCanvas((c, r) => _icons.Draw(c, r, "close-outline", 34, Color.White)));
        _closeButton.OnClick = e => { e.StopPropagation(); _ = Close.InvokeAsync(); };

        var flashSlot = new DivElement { Style = SideSlotStyle };
        flashSlot.Children.Add(_flashButton);
        var switchSlot = new DivElement { Style = SideSlotStyle };
        switchSlot.Children.Add(_switchButton);
        _controls.Style = ControlPanelStyle;
        _controls.Children.Add(flashSlot);
        _controls.Children.Add(_shutter);
        _controls.Children.Add(switchSlot);

        _flashFx.Style = new Style
        {
            Display = Display.None, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0), Width = Length.Percent(100),
            Height = Length.Percent(100), BackgroundColor = Color.Black, Opacity = .6f, PointerEvents = PointerEvents.None,
        };
        UpdateButtons();
    }

    protected override void OnParametersSet()
    {
        // Parameters seed the state; the buttons change it afterwards (reported through the *Changed callbacks).
        if (!_initialized || _lastFacing != Facing) { CurrentFacing = Facing; _lastFacing = Facing; }
        if (!_initialized || _lastFlash != Flash) { CurrentFlash = Flash; _lastFlash = Flash; }
        _initialized = true;
        SyncBackButton();
        UpdateButtons();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var root = new Style
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column, Position = Position.Relative, Width = Length.Percent(100),
            Height = Length.Percent(100), BackgroundColor = Color.Black, Color = Color.White, Overflow = Overflow.Hidden,
        };
        if (Style != null) root.Merge(Style);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", string.IsNullOrEmpty(Class) ? "miko-camera" : "miko-camera " + Class);
        builder.AddAttribute(2, "style", root);

        // The picture takes what the control panel leaves.
        builder.OpenElement(3, "div");
        builder.AddAttribute(4, "style", PreviewAreaStyle);
        builder.OpenComponent<MikoCameraView>(5);
        builder.AddComponentParameter(6, nameof(MikoCameraView.Facing), CurrentFacing);
        builder.AddComponentParameter(7, nameof(MikoCameraView.Resolution), Resolution);
        builder.AddComponentParameter(8, nameof(MikoCameraView.Active), Active);
        // The flash fires with the capture; "on" does not light the torch while framing.
        builder.AddComponentParameter(9, nameof(MikoCameraView.Texts), Texts);
        builder.AddComponentParameter(10, nameof(MikoCameraView.StateChanged), _stateChanged ??= EventCallback.Factory.Create<CameraSessionState>(this, OnCameraState));
        builder.AddComponentParameter(11, nameof(MikoCameraView.ChildContent), (RenderFragment)BuildPreviewOverlay);
        builder.AddComponentReferenceCapture(12, v => View = (MikoCameraView)v);
        builder.CloseComponent();
        builder.CloseElement();

        builder.AddElement(13, _controls);
        builder.CloseElement();
    }

    private void BuildPreviewOverlay(RenderTreeBuilder builder)
    {
        builder.AddElement(20, _flashFx);
        builder.AddElement(21, _closeButton);
    }

    /// <summary>Takes a photo as the shutter does: raises <see cref="Capturing"/>, captures, then <see cref="PhotoTaken"/>.</summary>
    /// <returns>The photo, or null when the capture was cancelled, is already running or failed.</returns>
    public async Task<CameraPhoto?> TakePhotoAsync()
    {
        if (IsCapturing || View is not { State: CameraSessionState.Running } view) return null;
        var args = new CameraCapturingEventArgs();
        if (Capturing.HasDelegate) await Capturing.InvokeAsync(args);
        if (args.Cancel || _disposed) return null;

        IsCapturing = true;
        PlatformService?.Haptics.Impact(HapticImpactStyle.Light);
        ShowFlashEffect(true);
        Engine?.RequestRepaint();
        try
        {
            var photo = await view.CapturePhotoAsync(CaptureOptions with { Flash = CurrentFlash });
            if (PhotoTaken.HasDelegate) await PhotoTaken.InvokeAsync(photo);
            return photo;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !_disposed)
        {
            if (CaptureFailed.HasDelegate) await CaptureFailed.InvokeAsync(ex);
            return null;
        }
        finally
        {
            IsCapturing = false;
            ShowFlashEffect(false);
            _shutterPress = 0;
        }
    }

    /// <summary>Switches between the back and the front camera.</summary>
    public async Task SwitchCamera()
    {
        CurrentFacing = CurrentFacing == CameraFacing.Front ? CameraFacing.Back : CameraFacing.Front;
        // The preview opens the other camera when it receives the new facing.
        StateHasChanged();
        if (FacingChanged.HasDelegate) await FacingChanged.InvokeAsync(CurrentFacing);
    }

    /// <summary>Off → auto → on → off.</summary>
    public async Task CycleFlash()
    {
        CurrentFlash = CurrentFlash switch
        {
            CameraFlashMode.Off => CameraFlashMode.Auto,
            CameraFlashMode.Auto => CameraFlashMode.On,
            _ => CameraFlashMode.Off,
        };
        Engine?.RequestRepaint();
        if (FlashChanged.HasDelegate) await FlashChanged.InvokeAsync(CurrentFlash);
    }

    // The flash button appears once the running camera reports a flash, the switch button when the
    // backend lists cameras facing both ways; both are known once the session runs.
    private void OnCameraState(CameraSessionState state) => UpdateButtons();

    private void UpdateButtons()
    {
        SetDisplay(_closeButton, Close.HasDelegate);
        SetDisplay(_flashButton, ShowFlashButton && View is { HasTorch: true });
        SetDisplay(_switchButton, ShowSwitchButton && View is { } view
            && view.Devices.Any(d => d.Facing == CameraFacing.Front) && view.Devices.Any(d => d.Facing != CameraFacing.Front));
        Engine?.RequestRepaint();
    }

    private static void SetDisplay(Element element, bool visible)
    {
        var display = visible ? Display.Block : Display.None;
        if (element.Style!.Display.ValueOrNull() == display) return;
        var style = new Style();
        style.Merge(element.Style);
        style.Display = display;
        element.Style = style;
    }

    private static CanvasElement IconCanvas(Action<IDrawingContext, RectF> paint) => new()
    {
        Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100), PointerEvents = PointerEvents.None },
        Paint = paint,
    };

    private void SetShutterPress(float value)
    {
        _shutterPress = value;
        Engine?.RequestRepaint();
    }

    /// <summary>Darkens the preview while the still is taken, as the system cameras blink.</summary>
    private void ShowFlashEffect(bool on)
    {
        var style = new Style();
        style.Merge(_flashFx.Style!);
        style.Display = on ? Display.Block : Display.None;
        _flashFx.Style = style;
        Engine?.RequestRepaint();
    }

    private void DrawShutter(IDrawingContext context, RectF bounds)
    {
        float cx = bounds.X + bounds.Width / 2, cy = bounds.Y + bounds.Height / 2;
        float radius = Math.Min(bounds.Width, bounds.Height) / 2;
        context.StrokeArc(new RectF(cx - radius + 2.5f, cy - radius + 2.5f, (radius - 2.5f) * 2, (radius - 2.5f) * 2), 0, 360, Color.White, 5);
        float inner = radius - 10 - _shutterPress * 3;
        context.FillCircle(cx, cy, inner, IsCapturing ? Color.FromRgba(255, 255, 255, .6f) : Color.White);
    }

    private void SyncBackButton()
    {
        if (Close.HasDelegate && _back == null && PlatformService is { } platform)
            _back = platform.Back.Register(() =>
            {
                if (_disposed || !Close.HasDelegate) return false;
                _ = Close.InvokeAsync();
                return true;
            }, BackButtonPriority.Overlay);
        else if (!Close.HasDelegate && _back != null)
        {
            _back.Dispose();
            _back = null;
        }
    }

    private static readonly Style PreviewAreaStyle = new()
    {
        Display = Display.Block, Position = Position.Relative, FlexGrow = 1, FlexShrink = 1, MinHeight = Length.Px(0), Width = Length.Percent(100),
    };

    private static readonly Style CloseButtonStyle = new()
    {
        Display = Display.Block, Position = Position.Absolute, Top = Length.SafeAreaInsetTop + Length.Px(12), Left = Length.SafeAreaInsetLeft + Length.Px(12),
        Width = Length.Px(48), Height = Length.Px(48), Padding = Length.Px(0), BorderWidth = Length.Px(0), BackgroundColor = Color.Transparent,
        PointerEvents = PointerEvents.Auto,
    };

    private static readonly Style ControlPanelStyle = new()
    {
        Display = Display.Flex, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.SpaceAround, FlexShrink = 0,
        Height = Length.SafeAreaInsetBottom + Length.Px(136), PaddingBottom = Length.SafeAreaInsetBottom, BoxSizing = BoxSizing.BorderBox,
        PaddingLeft = Length.SafeAreaInsetLeft + Length.Px(16), PaddingRight = Length.SafeAreaInsetRight + Length.Px(16), BackgroundColor = Color.Black,
    };

    private static readonly Style SideSlotStyle = new()
    {
        Display = Display.Flex, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center, Width = Length.Px(64), Height = Length.Px(64), FlexShrink = 0,
    };

    private static readonly Style RoundButtonStyle = new()
    {
        Display = Display.Block, Width = Length.Px(56), Height = Length.Px(56), Padding = Length.Px(0), BorderWidth = Length.Px(0),
        BorderRadius = 28, BackgroundColor = Color.Transparent,
    };

    private static readonly Style ShutterStyle = new()
    {
        Display = Display.Block, Width = Length.Px(78), Height = Length.Px(78), FlexShrink = 0, Padding = Length.Px(0), BorderWidth = Length.Px(0),
        BorderRadius = 39, BackgroundColor = Color.Transparent,
    };

    protected override void OnDispose() => Dispose();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _back?.Dispose();
        _back = null;
        _icons.Dispose();
    }
}

/// <summary>Arguments of <see cref="MikoCamera.Capturing"/>.</summary>
public sealed class CameraCapturingEventArgs
{
    /// <summary>Set to skip this capture.</summary>
    public bool Cancel { get; set; }
}
