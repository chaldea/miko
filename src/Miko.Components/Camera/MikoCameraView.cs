using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Platform.Camera;
using Miko.Styling;

namespace Miko.Components.Camera;

/// <summary>Texts of the built-in camera status. Replace them to localize.</summary>
public sealed record CameraViewTexts
{
    public string NoCamera { get; init; } = "No camera available";
    public string PermissionDenied { get; init; } = "Camera access is off. Allow it in the system settings, then retry.";
    public string Unavailable { get; init; } = "The camera is in use by another app.";
    public string Failed { get; init; } = "The camera could not be started.";
    public string Retry { get; init; } = "Retry";
}

/// <summary>
/// Live camera preview with an overlay: the <c>&lt;camera&gt;</c> element filling the view, and the
/// child content laid over it (filling the view too). The camera UIs (<see cref="MikoCamera"/>,
/// <c>MikoBarcodeScanner</c>) are this view plus their own overlay; so is any other camera UI.
/// </summary>
/// <remarks>
/// The <see cref="CameraElement"/> is kept across renders, so a parent re-render never reopens the
/// camera; the engine opens the session when the view is shown and releases it when the view goes
/// away, <see cref="Active"/> turns false, or <see cref="Facing"/> / <see cref="DeviceId"/> /
/// <see cref="Resolution"/> change. Requires a camera backend (<c>UseSystemCamera()</c>,
/// <c>UseAndroidCamera()</c>, <c>UseIosCamera()</c>, <c>UseSimulatorCamera()</c>); without one the
/// view shows <see cref="CameraViewTexts.NoCamera"/>. Members are UI-thread APIs.
/// </remarks>
public sealed class MikoCameraView : ComponentBase, IDisposable
{
    /// <summary>Which camera to open.</summary>
    [Parameter] public CameraFacing Facing { get; set; } = CameraFacing.Back;

    /// <summary>A specific camera from <c>ICameraBackend.GetDevices()</c>; wins over <see cref="Facing"/>.</summary>
    [Parameter] public string? DeviceId { get; set; }

    /// <summary>Preview resolution preset.</summary>
    [Parameter] public CameraResolution Resolution { get; set; } = CameraResolution.High;

    /// <summary>Whether the camera runs; false releases it and leaves the view black.</summary>
    [Parameter] public bool Active { get; set; } = true;

    /// <summary>Torch (continuous light), when the camera has one.</summary>
    [Parameter] public bool Torch { get; set; }

    /// <summary>Zoom factor, clamped to what the camera supports.</summary>
    [Parameter] public float Zoom { get; set; } = 1;

    /// <summary>How the preview fills the view: cropped (cover, the default) or letterboxed (contain).</summary>
    [Parameter] public CameraFit Fit { get; set; } = CameraFit.Cover;

    /// <summary>Mirrors the preview; null mirrors front cameras only.</summary>
    [Parameter] public bool? Mirror { get; set; }

    /// <summary>Receives luminance frames on a thread-pool thread (barcode scanning and the like).</summary>
    [Parameter] public ICameraFrameAnalyzer? Analyzer { get; set; }

    [Parameter] public string? Class { get; set; }

    /// <summary>Merged over the view's own style (block, 100% wide and high, black).</summary>
    [Parameter] public Style? Style { get; set; }

    /// <summary>Show the built-in status (no camera, permission denied, errors with a retry button).</summary>
    [Parameter] public bool ShowStatus { get; set; } = true;

    [Parameter] public CameraViewTexts Texts { get; set; } = new();

    /// <summary>Raised when <see cref="State"/> changes.</summary>
    [Parameter] public EventCallback<CameraSessionState> StateChanged { get; set; }

    [Inject] public MikoEngine? Engine { get; set; }

    /// <summary>The camera element; its <see cref="CameraElement.Session"/> is the open session.</summary>
    public CameraElement Camera { get; } = new() { Class = "miko-camera-preview" };

    /// <summary>State of the session; <see cref="CameraSessionState.Error"/> without a camera backend.</summary>
    public CameraSessionState State { get; private set; } = CameraSessionState.Idle;

    /// <summary>Why the camera is not running; <see cref="CameraErrorKind.NoDevice"/> without a backend.</summary>
    public CameraErrorKind? ErrorKind { get; private set; }

    public string? ErrorMessage { get; private set; }

    /// <summary>Whether the running camera has a torch (and so a flash).</summary>
    public bool HasTorch => Camera.Session is { State: CameraSessionState.Running, HasTorch: true };

    public float MinZoom => Camera.Session?.MinZoom ?? 1;

    public float MaxZoom => Camera.Session?.MaxZoom ?? 1;

    /// <summary>Cameras the backend can open (empty without a backend).</summary>
    public IReadOnlyList<CameraDeviceInfo> Devices => Engine?.CameraBackend?.GetDevices() ?? Array.Empty<CameraDeviceInfo>();

    private readonly DivElement _status = new() { Class = "miko-camera-status" };
    private readonly SpanElement _statusText = new();
    private readonly ButtonElement _retry = new() { Class = "miko-camera-retry" };
    private CameraSessionEvent.Error? _openError;
    private bool _initialized, _disposed;

    /// <summary>Takes a photo from the live preview; see <see cref="CameraElement.CapturePhotoAsync"/>.</summary>
    public Task<CameraPhoto> CapturePhotoAsync(CameraCaptureOptions? options = null, CancellationToken cancellationToken = default)
        => Camera.CapturePhotoAsync(options, cancellationToken);

    /// <summary>Opens the camera again (after an error, or once the permission was granted in the settings).</summary>
    public void Restart()
    {
        _openError = null;
        Camera.Restart();
        Engine?.RequestRepaint();
    }

    protected override void OnInitialized()
    {
        Camera.Style = new Style
        {
            Display = Display.Block, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0),
            Width = Length.Percent(100), Height = Length.Percent(100),
        };
        Camera.SessionChanged += OnSessionChanged;
        Camera.SessionEvent += OnSessionEvent;

        // Fills the view to center its content; only the retry button takes taps.
        _status.Style = new Style
        {
            Display = Display.None, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0), Width = Length.Percent(100), Height = Length.Percent(100),
            FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center, Gap = Length.Px(16),
            PaddingLeft = Length.Px(32), PaddingRight = Length.Px(32), BoxSizing = BoxSizing.BorderBox, PointerEvents = PointerEvents.None,
        };
        _statusText.Style = new Style { Display = Display.Block, Color = Color.FromRgba(255, 255, 255, .85f), FontSize = Length.Px(15), TextAlign = TextAlign.Center, LineHeight = Length.Px(21) };
        _retry.Style = new Style
        {
            Display = Display.None, Height = Length.Px(40), PaddingLeft = Length.Px(20), PaddingRight = Length.Px(20), BorderWidth = Length.Px(0),
            BorderRadius = 20, BackgroundColor = Color.FromRgba(255, 255, 255, .18f), Color = Color.White, FontSize = Length.Px(15),
            PointerEvents = PointerEvents.Auto,
        };
        _retry.OnClick = e => { e.StopPropagation(); Restart(); };
        _status.Children.Add(_statusText);
        _status.Children.Add(_retry);
    }

    protected override void OnParametersSet()
    {
        Camera.Facing = Facing;
        Camera.DeviceId = DeviceId;
        Camera.Resolution = Resolution;
        Camera.Active = Active;
        Camera.Torch = Torch;
        Camera.Zoom = Zoom;
        Camera.Fit = Fit;
        Camera.Mirror = Mirror;
        Camera.Analyzer = Analyzer;
        _retry.TextContent = Texts.Retry;
        UpdateState(notify: _initialized);
        _initialized = true;
        Engine?.RequestRepaint();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var style = new Style
        {
            Display = Display.Block, Position = Position.Relative, Width = Length.Percent(100), Height = Length.Percent(100),
            Overflow = Overflow.Hidden, BackgroundColor = Color.Black, Color = Color.White, BoxSizing = BoxSizing.BorderBox,
        };
        if (Style != null) style.Merge(Style);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", string.IsNullOrEmpty(Class) ? "miko-camera-view" : "miko-camera-view " + Class);
        builder.AddAttribute(2, "style", style);
        // Paint order: preview, overlay, status (on top while an error is shown).
        builder.AddElement(3, Camera);
        builder.OpenElement(4, "div");
        builder.AddAttribute(5, "class", "miko-camera-overlay");
        builder.AddAttribute(6, "style", OverlayStyle);
        builder.AddContent(ChildContent);
        builder.CloseElement();
        builder.AddElement(7, _status);
        builder.CloseElement();
    }

    private static readonly Style OverlayStyle = new()
    {
        Display = Display.Block, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0),
        Width = Length.Percent(100), Height = Length.Percent(100),
    };

    private void OnSessionChanged(ICameraSession? session)
    {
        if (session != null) _openError = null;
        UpdateState(notify: true);
    }

    private void OnSessionEvent(CameraSessionEvent value)
    {
        // Without a session the error is the backend refusing to open one.
        if (value is CameraSessionEvent.Error error && Camera.Session == null) _openError = error;
        UpdateState(notify: true);
    }

    private void UpdateState(bool notify)
    {
        if (_disposed) return;
        var previous = State;
        if (Engine is { CameraBackend: null or NullCameraBackend })
        {
            (State, ErrorKind, ErrorMessage) = (CameraSessionState.Error, CameraErrorKind.NoDevice, null);
        }
        else if (Camera.Session is { } session)
        {
            (State, ErrorKind, ErrorMessage) = (session.State, session.State == CameraSessionState.Error ? session.ErrorKind ?? CameraErrorKind.Failed : null, session.ErrorMessage);
        }
        else if (Camera.Active && _openError is { } error)
        {
            (State, ErrorKind, ErrorMessage) = (CameraSessionState.Error, error.Kind, error.Message);
        }
        else
        {
            (State, ErrorKind, ErrorMessage) = (CameraSessionState.Idle, null, null);
        }
        UpdateStatus();
        if (notify && State != previous && StateChanged.HasDelegate)
            _ = StateChanged.InvokeAsync(State);
    }

    private void UpdateStatus()
    {
        string? text = State == CameraSessionState.Error && ShowStatus
            ? ErrorKind switch
            {
                CameraErrorKind.NoDevice => Texts.NoCamera,
                CameraErrorKind.PermissionDenied => Texts.PermissionDenied,
                CameraErrorKind.Unavailable => Texts.Unavailable,
                _ => Texts.Failed,
            }
            : null;
        bool canRetry = ErrorKind is not CameraErrorKind.NoDevice || Engine is not { CameraBackend: null or NullCameraBackend };
        SetDisplay(_status, text != null ? Display.Flex : Display.None);
        SetDisplay(_retry, text != null && canRetry ? Display.Block : Display.None);
        if (text != null && _statusText.TextContent != text) _statusText.TextContent = text;
        Engine?.RequestRepaint();
    }

    private static void SetDisplay(Element element, Display display)
    {
        if (element.Style!.Display.ValueOrNull() == display) return;
        var style = new Style();
        style.Merge(element.Style);
        style.Display = display;
        element.Style = style;
    }

    protected override void OnDispose() => Dispose();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Camera.SessionChanged -= OnSessionChanged;
        Camera.SessionEvent -= OnSessionEvent;
        Camera.Analyzer = null;
    }
}
