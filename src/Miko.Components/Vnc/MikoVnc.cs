using System.Globalization;
using Miko.Components.Vnc.Protocol;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Platform;
using Miko.Styling;

namespace Miko.Components.Vnc;

/// <summary>
/// A VNC remote desktop: the remote screen fills the component, with a collapsible toolbar over its top
/// edge (keyboard, special keys, zoom, settings, fullscreen, disconnect), a row of keys a phone keyboard
/// lacks (Esc, Tab, Ctrl, Alt, Win, arrows, F1–F12, Ctrl+Alt+Del) and a password dialog when the server asks
/// for one. Without a <see cref="Host"/> it shows a connect form.
/// </summary>
/// <remarks>
/// <para>
/// The protocol is managed code (<see cref="VncClient"/>): RFB 3.3–3.8; None, VNC and Tight security;
/// Raw, CopyRect, RRE, Hextile, Zlib, ZRLE and Tight (JPEG through the engine's image decoder) encodings,
/// the remote pointer shape and desktop resizing.
/// </para>
/// <para>
/// Mouse input maps one to one (all three buttons, the wheel). Touch: tap clicks, hold clicks with the right
/// button, hold then drag drags with the left button; dragging pans a zoomed picture and scrolls one that
/// fits. Keys go to the remote screen while it has focus (click it, or the keyboard button on a phone).
/// </para>
/// <para>Public methods are UI-thread APIs.</para>
/// </remarks>
public sealed partial class MikoVnc : ComponentBase, IDisposable
{
    /// <summary>Server host or IP. Null shows the connect form, where the user enters it.</summary>
    [Parameter] public string? Host { get; set; }

    /// <summary>TCP port (display <c>n</c> is <c>5900 + n</c>).</summary>
    [Parameter] public int Port { get; set; } = VncAddress.DefaultPort;

    /// <summary>The VNC password. When null and the server needs one, a dialog asks for it.</summary>
    [Parameter] public string? Password { get; set; }

    /// <summary>Connect as soon as the view is shown (otherwise a button waits for a tap).</summary>
    [Parameter] public bool AutoConnect { get; set; } = true;

    /// <summary>Watch without sending keyboard or mouse input.</summary>
    [Parameter] public bool ViewOnly { get; set; }

    [Parameter] public EventCallback<bool> ViewOnlyChanged { get; set; }

    /// <summary>The picture quality to start with; settings change it during the session (<see cref="QualityChanged"/>).</summary>
    [Parameter] public VncQuality Quality { get; set; } = VncQuality.High;

    [Parameter] public EventCallback<VncQuality> QualityChanged { get; set; }

    /// <summary>How the remote screen fits the view at first.</summary>
    [Parameter] public VncScaling Scaling { get; set; } = VncScaling.Fit;

    /// <summary>Let other viewers stay connected.</summary>
    [Parameter] public bool Shared { get; set; } = true;

    /// <summary>Show the toolbar (the API still works without it).</summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    [Parameter] public VncTexts Texts { get; set; } = new();

    [Parameter] public string? Class { get; set; }

    [Parameter] public Style? Style { get; set; }

    /// <summary>The session is up.</summary>
    [Parameter] public EventCallback<VncDesktopInfo> Connected { get; set; }

    /// <summary>The session ended; the argument is the failure (null when it ended cleanly or the user left).</summary>
    [Parameter] public EventCallback<Exception?> Disconnected { get; set; }

    /// <summary>Text the remote side put on its clipboard. Pass it to the device clipboard (Miko.Native's <c>IClipboardService</c>) to sync.</summary>
    [Parameter] public EventCallback<string> ClipboardReceived { get; set; }

    /// <summary>Connection and protocol failures (they are shown in the view as well).</summary>
    [Parameter] public EventCallback<Exception> Error { get; set; }

    [Inject] public MikoEngine? Engine { get; set; }

    /// <summary>Keyboard focus and the soft keyboard. Null without an app (tests, standalone engines).</summary>
    [Inject] public MikoInteractionController? Input { get; set; }

    /// <summary>Fullscreen orientation and system bars, back button, haptics.</summary>
    [Inject] public IPlatformService? PlatformService { get; set; }

    /// <summary>Clock of the hold, hint and bitrate timers; the system clock when none is registered.</summary>
    [Inject] public TimeProvider? Clock { get; set; }

    public VncState State { get; private set; }

    /// <summary>Why the session ended or failed, when <see cref="State"/> is <see cref="VncState.Disconnected"/>.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>The remote desktop (null until connected).</summary>
    public VncDesktopInfo? Desktop { get; private set; }

    /// <summary>The live connection, for what the view does not cover (null when not connected).</summary>
    public VncClient? Client => _client;

    /// <summary>The remote screen's pixels (the last ones after a disconnect).</summary>
    public VncFramebuffer? Framebuffer => _view.Framebuffer;

    public VncQuality CurrentQuality { get; private set; }

    public bool IsViewOnly { get; private set; }

    public bool IsFullscreen { get; private set; }

    /// <summary>The remote screen has keyboard focus (and on a phone, the soft keyboard is up).</summary>
    public bool IsKeyboardActive { get; private set; }

    /// <summary>1 = the whole remote screen fits.</summary>
    public float Zoom => _view.Zoom;

    /// <summary>Received bytes per second over the last second.</summary>
    public long BytesPerSecond { get; private set; }

    /// <summary>The text of the transient hint, while one is shown.</summary>
    public string? Hint { get; private set; }

    public event Action? FullscreenChanged;

    private readonly VncView _view = new();
    private readonly VncIcons _icons = new();
    private VncClient? _client;
    private CancellationTokenSource? _connect;
    private int _generation;
    private TaskCompletionSource<string?>? _passwordPrompt;
    private string? _rememberedPassword;
    private string? _targetHost;
    private int _targetPort = VncAddress.DefaultPort;
    private string? _lastHost, _lastPassword;
    private int _lastPort;
    private VncQuality _lastQualityParameter;
    private bool _lastViewOnlyParameter;
    private VncTexts? _lastTexts;
    private bool _built, _disposed, _autoConnectPending, _touchHintShown;
    private int _repaintPending;
    private ITimer? _rateTimer, _hintTimer;
    private long _lastBytes = -1;
    private IPlatformService? _fullscreenPlatform;
    private IDisposable? _fullscreenBack;

    protected override void OnInitialized() => BuildChrome();

    protected override void OnParametersSet()
    {
        if (!_built)
        {
            CurrentQuality = Quality;
            IsViewOnly = ViewOnly;
        }
        else
        {
            if (_lastQualityParameter != Quality) SetQuality(Quality);
            if (_lastViewOnlyParameter != ViewOnly) SetViewOnly(ViewOnly);
        }
        _lastQualityParameter = Quality;
        _lastViewOnlyParameter = ViewOnly;

        bool targetChanged = !_built || _lastHost != Host || _lastPort != Port || _lastPassword != Password;
        _lastHost = Host;
        _lastPort = Port;
        _lastPassword = Password;
        if (targetChanged)
        {
            _rememberedPassword = null;
            if (Host != null)
            {
                _targetHost = Host;
                _targetPort = Port;
                if (_built) Disconnect();
                if (AutoConnect) _autoConnectPending = true;
                else SetState(VncState.Disconnected, null);
            }
            else if (_built)
            {
                Disconnect();
            }
        }

        if (!ReferenceEquals(_lastTexts, Texts))
        {
            _lastTexts = Texts;
            ApplyTexts();
        }
        _root.Class = $"miko-vnc {Class}";
        ApplyLayout();
        _built = true;
        ApplyState();
        // Connect once the view is in the engine's tree (first paint): an instance that is built but never
        // shown does not open a connection.
        if (_autoConnectPending && Engine != null) RequestRepaint();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddElement(0, _root);

    // ---- public API ----------------------------------------------------------------

    /// <summary>Connects to <see cref="Host"/> (or the address in the connect form). Ends a session in progress first.</summary>
    public void Connect()
    {
        _autoConnectPending = false;
        if (_disposed) return;
        if (_targetHost == null)
        {
            SetState(VncState.Idle, null);
            return;
        }
        Disconnect(silently: true);
        int generation = ++_generation;
        var cts = _connect = new CancellationTokenSource();
        ErrorMessage = null;
        SetState(VncState.Connecting, null);
        var options = new VncConnectOptions(_targetHost, _targetPort) { Shared = Shared };
        // What the user typed beats the parameter: it was typed because the parameter was wrong.
        string? password = _rememberedPassword ?? Password;
        _ = ConnectAsync(options, password, generation, cts.Token);
    }

    /// <summary>Connects to an address entered by code (as the connect form does).</summary>
    public void Connect(string host, int port = VncAddress.DefaultPort, string? password = null)
    {
        _targetHost = host;
        _targetPort = port;
        _rememberedPassword = password;
        Connect();
    }

    /// <summary>Ends the session (or the attempt).</summary>
    public void Disconnect() => Disconnect(silently: false);

    private void Disconnect(bool silently)
    {
        _autoConnectPending = false;
        _connect?.Cancel();
        _connect?.Dispose();
        _connect = null;
        _passwordPrompt?.TrySetResult(null);
        _passwordPrompt = null;
        ClosePasswordDialog();
        var client = _client;
        if (client != null)
        {
            ReleaseAllKeys();
            client.Dispose();
            _client = null;
        }
        StopRateTimer();
        ResetGesture();
        if (silently) return;
        _view.Dimmed = true;
        if (State != VncState.Idle)
            SetState(Host == null ? VncState.Idle : VncState.Disconnected, null);
        if (client != null && Disconnected.HasDelegate) _ = Disconnected.InvokeAsync(null);
    }

    /// <summary>Types text on the remote side (each character as a key press).</summary>
    public void SendText(string text)
    {
        if (_client is not { } client || IsViewOnly) return;
        foreach (var rune in text.EnumerateRunes())
        {
            uint keysym = VncKeys.FromChar(rune.Value);
            client.SendKey(keysym, true);
            client.SendKey(keysym, false);
        }
    }

    /// <summary>Presses the keys in order and releases them in reverse (a shortcut: <c>SendKeys(VncKeys.ControlLeft, 'c')</c>).</summary>
    public void SendKeys(params uint[] keysyms)
    {
        if (_client is not { } client || IsViewOnly) return;
        foreach (var k in keysyms) client.SendKey(k, true);
        for (int i = keysyms.Length - 1; i >= 0; i--) client.SendKey(keysyms[i], false);
    }

    /// <summary>Sends Ctrl+Alt+Del (Windows' secure attention sequence: lock screen, sign-in).</summary>
    public void SendCtrlAltDel()
    {
        if (_client == null || IsViewOnly) return;
        SendKeys(VncKeys.ControlLeft, VncKeys.AltLeft, VncKeys.Delete);
        ShowHint(Texts.CtrlAltDelSent);
    }

    /// <summary>Puts text on the remote side's clipboard.</summary>
    public void SendClipboard(string text) => _client?.SendClipboard(text);

    /// <summary>Asks the server for the whole screen again.</summary>
    public void RefreshScreen() => _client?.RequestFullUpdate();

    public void SetQuality(VncQuality quality)
    {
        if (quality == CurrentQuality) return;
        CurrentQuality = quality;
        var (encodings, jpeg, compress) = quality.Settings();
        _client?.SetEncodings(encodings, jpeg, compress);
        UpdateSettings();
        if (QualityChanged.HasDelegate) _ = QualityChanged.InvokeAsync(quality);
    }

    public void SetViewOnly(bool viewOnly)
    {
        if (viewOnly == IsViewOnly) return;
        IsViewOnly = viewOnly;
        if (viewOnly)
        {
            ReleaseAllKeys();
            ReleaseButtons();
            if (IsKeyboardActive) SetKeyboard(false);
        }
        UpdateSettings();
        ApplyState();
        if (ViewOnlyChanged.HasDelegate) _ = ViewOnlyChanged.InvokeAsync(viewOnly);
    }

    public void ZoomIn() => StepZoom(+1);

    public void ZoomOut() => StepZoom(-1);

    /// <summary>Shows the whole remote screen.</summary>
    public void FitToScreen()
    {
        _view.SetZoom(1);
        Repaint();
    }

    /// <summary>One remote pixel per logical pixel.</summary>
    public void ActualSize()
    {
        _view.SetZoom(_view.ActualSizeZoom);
        if (_view.Pointer is { } p) _view.Follow(p.X, p.Y);
        Repaint();
    }

    private static readonly float[] ZoomSteps = [1, 1.5f, 2, 3, 4, 6, 8];

    private void StepZoom(int direction)
    {
        float zoom = _view.Zoom;
        float next = direction > 0
            ? ZoomSteps.FirstOrDefault(z => z > zoom + .01f, VncView.MaxZoom)
            : ZoomSteps.LastOrDefault(z => z < zoom - .01f, 1);
        // Keep the remote pointer where it is when zooming (that is where the user is working).
        if (_view.Pointer is { } p)
        {
            var (vx, vy) = _view.ToView(p.X, p.Y);
            _view.SetZoom(next, Math.Clamp(vx, 0, _view.ViewWidth), Math.Clamp(vy, 0, _view.ViewHeight));
        }
        else
        {
            _view.SetZoom(next);
        }
        Repaint();
    }

    // ---- connection ----------------------------------------------------------------

    private async Task ConnectAsync(VncConnectOptions options, string? password, int generation, CancellationToken token)
    {
        var graphics = Engine?.Graphics;
        var (encodings, jpeg, compress) = CurrentQuality.Settings();
        options = options with
        {
            Encodings = encodings,
            JpegQuality = jpeg,
            CompressLevel = compress,
            JpegDecoder = graphics == null ? null : new VncJpegDecoder(graphics),
            PasswordProvider = ct => PromptPasswordAsync(generation, null, ct),
        };
        string server = VncAddress.Format(options.Host, options.Port);
        while (true)
        {
            try
            {
                var client = await VncClient.ConnectAsync(options with { Password = password }, token).ConfigureAwait(false);
                PostToRenderThread(() =>
                {
                    if (generation != _generation || token.IsCancellationRequested) { client.Dispose(); return; }
                    // A password the user typed is kept for reconnecting (memory only).
                    if (password != null && Password == null) _rememberedPassword = password;
                    Attach(client);
                });
                return;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (VncPasswordRequiredException)
            {
                // The user closed the password dialog.
                PostToRenderThread(() =>
                {
                    if (generation != _generation) return;
                    SetState(Host == null ? VncState.Idle : VncState.Disconnected, Texts.Cancelled);
                });
                return;
            }
            catch (VncAuthenticationException ex) when (ex.PasswordRejected)
            {
                // Wrong password: ask again, with the reason, and try a new connection (the server closed this one).
                password = await PromptPasswordAsync(generation, Texts.WrongPassword, token).ConfigureAwait(false);
                if (password == null)
                {
                    PostToRenderThread(() =>
                    {
                        if (generation != _generation) return;
                        if (Password == null) _rememberedPassword = null;
                        SetState(Host == null ? VncState.Idle : VncState.Disconnected, Texts.WrongPassword);
                    });
                    return;
                }
                PostToRenderThread(() => { if (generation == _generation) SetState(VncState.Connecting, null); });
            }
            catch (Exception ex)
            {
                PostToRenderThread(() =>
                {
                    if (generation != _generation) return;
                    SetState(Host == null ? VncState.Idle : VncState.Disconnected, Texts.Describe(ex, server));
                    if (Error.HasDelegate) _ = Error.InvokeAsync(ex);
                });
                return;
            }
        }
    }

    // Opens the password dialog on the render thread and waits for the user's answer (null: cancelled).
    private Task<string?> PromptPasswordAsync(int generation, string? error, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        token.Register(() => tcs.TrySetResult(null));
        PostToRenderThread(() =>
        {
            if (generation != _generation || token.IsCancellationRequested) { tcs.TrySetResult(null); return; }
            _passwordPrompt?.TrySetResult(null);
            _passwordPrompt = tcs;
            SetState(VncState.Authenticating, null);
            OpenPasswordDialog(VncAddress.Format(_targetHost ?? "", _targetPort), error);
        });
        return tcs.Task;
    }

    private void Attach(VncClient client)
    {
        _client = client;
        _view.Dimmed = false;
        _view.SetFramebuffer(client.Framebuffer);
        _view.Pointer = null;
        _lastBytes = -1;
        client.FramebufferResized += fb => PostToRenderThread(() =>
        {
            if (!ReferenceEquals(_client, client)) return;
            _view.SetFramebuffer(fb);
            Desktop = Desktop is null ? null : Desktop with { Width = fb.Width, Height = fb.Height };
            if (Scaling == VncScaling.Actual) _view.SetZoom(_view.ActualSizeZoom);
            UpdateToolbarText();
            Repaint();
        });
        client.FramebufferUpdated += RequestRepaint;
        client.CursorChanged += shape =>
        {
            _view.SetCursor(shape);
            PostToRenderThread(() => { if (ReferenceEquals(_client, client)) ApplyScreenCursor(); });
        };
        client.ClipboardReceived += text => PostToRenderThread(() =>
        {
            if (ReferenceEquals(_client, client) && ClipboardReceived.HasDelegate) _ = ClipboardReceived.InvokeAsync(text);
        });
        client.Bell += () => PlatformService?.Haptics.Impact(HapticImpactStyle.Light);
        client.DesktopNameChanged += name => PostToRenderThread(() =>
        {
            if (!ReferenceEquals(_client, client)) return;
            Desktop = Desktop is null ? null : Desktop with { Name = name };
            UpdateToolbarText();
        });
        client.Disconnected += failure => PostToRenderThread(() => OnRemoteDisconnect(client, failure));

        var fb = client.Framebuffer;
        Desktop = new VncDesktopInfo(client.DesktopName, fb.Width, fb.Height, client.Host, client.Port, client.ProtocolVersion, client.SecurityType);
        client.Start();
        SetState(VncState.Connected, null);
        if (Scaling == VncScaling.Actual) _pendingActualSize = true;
        StartRateTimer();
        UpdateToolbarText();
        ApplyScreenCursor();
        if (IsTouchPlatform && !_touchHintShown && !IsViewOnly)
        {
            _touchHintShown = true;
            ShowHint(Texts.TouchHint, TimeSpan.FromSeconds(4));
        }
        if (Connected.HasDelegate) _ = Connected.InvokeAsync(Desktop);
    }

    // 1:1 needs the view's size, known at the first paint after connecting.
    private bool _pendingActualSize;

    private void OnRemoteDisconnect(VncClient client, Exception? failure)
    {
        if (!ReferenceEquals(_client, client)) return;
        _client = null;
        StopRateTimer();
        ResetGesture();
        _heldModifiers.Clear();
        _pressedKeys.Clear();
        _sticky.Clear();
        _view.Dimmed = true;
        string server = VncAddress.Format(client.Host, client.Port);
        SetState(VncState.Disconnected, failure == null ? Texts.ServerClosed : Texts.Describe(failure, server));
        if (Disconnected.HasDelegate) _ = Disconnected.InvokeAsync(failure);
        if (failure != null && Error.HasDelegate) _ = Error.InvokeAsync(failure);
    }

    private void SetState(VncState state, string? error)
    {
        State = state;
        ErrorMessage = error;
        if (state != VncState.Connected)
        {
            if (IsKeyboardActive) SetKeyboard(false);
            CloseSettings();
        }
        ApplyState();
        Repaint();
    }

    // ---- rate ----------------------------------------------------------------------

    private void StartRateTimer()
    {
        if (_rateTimer != null || _disposed) return;
        var clock = Clock ?? TimeProvider.System;
        using (ExecutionContext.SuppressFlow())
            _rateTimer = clock.CreateTimer(static s => { var v = (MikoVnc)s!; v.PostToRenderThread(v.UpdateRate); }, this,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void StopRateTimer()
    {
        _rateTimer?.Dispose();
        _rateTimer = null;
        BytesPerSecond = 0;
    }

    private void UpdateRate()
    {
        if (_client is not { } client) return;
        long bytes = client.BytesReceived;
        BytesPerSecond = _lastBytes >= 0 ? Math.Max(0, bytes - _lastBytes) : 0;
        _lastBytes = bytes;
        UpdateToolbarText();
    }

    internal static string FormatRate(long bytesPerSecond) => bytesPerSecond >= 1024 * 1024
        ? (bytesPerSecond / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) + " MB/s"
        : (bytesPerSecond / 1024).ToString(CultureInfo.InvariantCulture) + " KB/s";

    // ---- threading -----------------------------------------------------------------

    /// <summary>Runs <paramref name="action"/> on the render thread (at once without an engine).</summary>
    private void PostToRenderThread(Action action)
    {
        if (Engine is { } engine) engine.Dispatcher.Post(() => { if (!_disposed) action(); });
        else if (!_disposed) action();
    }

    // From any thread: one pending repaint at a time (the receive thread calls this per update).
    private void RequestRepaint()
    {
        if (Engine is not { } engine) return;
        if (Interlocked.Exchange(ref _repaintPending, 1) == 1) return;
        engine.Dispatcher.Post(() =>
        {
            Volatile.Write(ref _repaintPending, 0);
            if (_disposed) return;
            engine.InvalidateElement(_screen);
            engine.RequestRepaint();
        });
    }

    private void Repaint() => Engine?.RequestRepaint();

    private bool IsTouchPlatform => (PlatformService?.Platform ?? PlatformInfo.Detect()) is HostPlatform.Android or HostPlatform.Ios;

    public void Dispose()
    {
        if (_disposed) return;
        Disconnect(silently: true);
        _disposed = true;
        StopRateTimer();
        _hintTimer?.Dispose();
        _hintTimer = null;
        _holdTimer?.Dispose();
        _holdTimer = null;
        ReleaseFullscreenPlatform();
        _passwordBack?.Dispose();
        _settingsBack?.Dispose();
        // Images belong to the render thread's device.
        if (Engine is { } engine) engine.Dispatcher.Post(() => { _view.Dispose(); _icons.Dispose(); });
        else { _view.Dispose(); _icons.Dispose(); }
    }
}
