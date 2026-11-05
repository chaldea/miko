using System.Globalization;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Platform;
using Miko.Platform.Video;
using Miko.Styling;

namespace Miko.Components.Webcam;

/// <summary>What <see cref="MikoWebcam"/> shows.</summary>
public enum WebcamState
{
    /// <summary>No device.</summary>
    Idle,

    /// <summary>Resolving the stream, connecting, or waiting for the first picture.</summary>
    Connecting,

    Playing,

    /// <summary>Stopped by the user; the last picture stays up.</summary>
    Stopped,

    Error,
}

/// <summary>
/// A network camera view, laid out like the vendors' apps: the live picture on top with a toolbar
/// under it (play / stop, quality, bitrate, fullscreen), a row of actions (snapshot, privacy, flip) and
/// a pan / tilt disk below. The camera behind it is an <see cref="IWebcamProvider"/> — Ezviz by default
/// (<c>AddMikoWebcam()</c>).
/// </summary>
/// <remarks>
/// <para>
/// The picture is a <c>&lt;video&gt;</c> element playing the provider's stream: a camera on the local
/// network streams straight to the app (RTSP, decoded by the platform), others through the provider's
/// relay. Actions the camera or provider lacks are hidden; pan / tilt is shown disabled with a reason
/// when the device has it but cannot take commands now.
/// </para>
/// <para>
/// Fullscreen fills the screen with the picture — landscape, system bars hidden, back button leaving
/// it, as <c>MikoPlayer</c> does — with the toolbar and a translucent pan / tilt disk over it; a tap on
/// the picture hides or shows them.
/// </para>
/// <para>Public methods are UI-thread APIs.</para>
/// </remarks>
public sealed class MikoWebcam : ComponentBase, IDisposable
{
    private const float PadSize = 200;
    private const float OverlayPadSize = 132;
    private static readonly Color Accent = Color.FromHex("#3d7bff");
    private static readonly Color TextColor = Color.FromHex("#2b2f36");
    private static readonly Color MutedText = Color.FromHex("#9aa1ad");
    private static readonly Color PageColor = Color.FromHex("#f3f5f9");
    private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(2);

    /// <summary>The camera. Changing it reconnects.</summary>
    [Parameter] public WebcamDevice? Device { get; set; }

    /// <summary>The quality to start with; the quality button switches it afterwards (reported through <see cref="QualityChanged"/>).</summary>
    [Parameter] public WebcamQuality Quality { get; set; } = WebcamQuality.Smooth;

    [Parameter] public EventCallback<WebcamQuality> QualityChanged { get; set; }

    /// <summary>Start streaming as soon as the stream is resolved (otherwise a play button waits for a tap).</summary>
    [Parameter] public bool AutoPlay { get; set; } = true;

    [Parameter] public PtzSpeed PtzSpeed { get; set; } = PtzSpeed.Normal;

    /// <summary>Show the pan / tilt disk (when the camera has pan or tilt).</summary>
    [Parameter] public bool ShowPtz { get; set; } = true;

    /// <summary>Show the action row.</summary>
    [Parameter] public bool ShowActions { get; set; } = true;

    /// <summary>Receives every snapshot taken with the snapshot action.</summary>
    [Parameter] public EventCallback<WebcamSnapshot> SnapshotTaken { get; set; }

    /// <summary>Raised when the provider has reported the device (its name, features, privacy state).</summary>
    [Parameter] public EventCallback<WebcamInfo> InfoLoaded { get; set; }

    /// <summary>Receives provider and stream failures (they are shown in the view as well).</summary>
    [Parameter] public EventCallback<Exception> Error { get; set; }

    [Parameter] public WebcamTexts Texts { get; set; } = new();

    [Parameter] public string? Class { get; set; }

    [Parameter] public Style? Style { get; set; }

    [Inject] public IWebcamProvider? Provider { get; set; }

    [Inject] public MikoEngine? Engine { get; set; }

    /// <summary>Fullscreen orientation, system bars and back button; haptics on the pan / tilt disk. Null without a service scope.</summary>
    [Inject] public IPlatformService? PlatformService { get; set; }

    /// <summary>Clock of the bitrate and hint timers; the system clock when none is registered.</summary>
    [Inject] public TimeProvider? Clock { get; set; }

    public WebcamState State { get; private set; }

    /// <summary>Why <see cref="State"/> is <see cref="WebcamState.Error"/>.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>The device as the provider reported it (null until known).</summary>
    public WebcamInfo? Info { get; private set; }

    /// <summary>The stream being played (null until resolved).</summary>
    public WebcamStream? Stream { get; private set; }

    public WebcamQuality CurrentQuality { get; private set; }

    public bool IsPrivacyEnabled { get; private set; }

    public bool IsFullscreen { get; private set; }

    /// <summary>Received bytes per second over the last second, when the stream reports them (direct streams).</summary>
    public long? BytesPerSecond { get; private set; }

    /// <summary>The pan / tilt direction being held, if any.</summary>
    public PtzDirection? ActivePtz => _ptzDirection;

    /// <summary>The text of the transient hint over the picture, while one is shown.</summary>
    public string? Hint { get; private set; }

    /// <summary>Raised after <see cref="IsFullscreen"/> changes, including through the back button.</summary>
    public event Action? FullscreenChanged;

    private readonly WebcamIcons _icons = new();
    private readonly DivElement _root = new() { Class = "miko-webcam" };
    private readonly DivElement _viewport = new() { Class = "miko-webcam-viewport" };
    private readonly VideoElement _video = new();
    private readonly DivElement _status = new() { Class = "miko-webcam-status" };
    private readonly CanvasElement _statusIcon = new();
    private readonly SpanElement _statusText = new();
    private readonly ButtonElement _statusButton = new() { Class = "miko-webcam-status-button" };
    private readonly SpanElement _badge = new() { Class = "miko-webcam-badge" };
    private readonly DivElement _hintLayer = new();
    private readonly SpanElement _hint = new() { Class = "miko-webcam-hint" };
    private readonly DivElement _toolbar = new() { Class = "miko-webcam-toolbar" };
    private readonly ButtonElement _qualityButton = new() { Class = "miko-webcam-quality" };
    private readonly SpanElement _rate = new() { Class = "miko-webcam-rate" };
    private readonly DivElement _actions = new() { Class = "miko-webcam-actions" };
    private readonly ButtonElement _snapshotAction;
    private readonly ButtonElement _privacyAction;
    private readonly ButtonElement _flipAction;
    private readonly ButtonElement _fullscreenAction;
    private readonly DivElement _ptzArea = new() { Class = "miko-webcam-ptz" };
    private readonly SpanElement _ptzCaption = new() { Class = "miko-webcam-ptz-caption" };
    private readonly DivElement _overlayPadHost = new() { Class = "miko-webcam-ptz-overlay" };
    private readonly WebcamPtzPad _pad;
    private readonly WebcamPtzPad _overlayPad;
    private readonly List<(SpanElement Label, Func<WebcamTexts, string> Text)> _labels = new();

    private WebcamDevice? _device;
    private WebcamQuality _lastQualityParameter;
    private WebcamTexts? _lastTexts;
    private int _generation;
    private CancellationTokenSource? _load;
    private bool _userStopped, _chromeHidden, _built, _disposed, _busy;
    private PtzDirection? _ptzDirection;
    private Task _ptzQueue = Task.CompletedTask;
    private ITimer? _rateTimer, _hintTimer;
    private long _lastBytes = -1;
    private IPlatformService? _fullscreenPlatform;
    private IDisposable? _fullscreenBack;

    public MikoWebcam()
    {
        _pad = new WebcamPtzPad(_icons, PadSize, translucent: false, StartPtz, StopPtz, Repaint);
        _overlayPad = new WebcamPtzPad(_icons, OverlayPadSize, translucent: true, StartPtz, StopPtz, Repaint);
        _snapshotAction = ActionButton("miko-webcam-snapshot", t => t.Snapshot, () => "camera-outline", () => _ = CaptureAsync());
        _privacyAction = ActionButton("miko-webcam-privacy", t => t.Privacy, () => "eye-off-outline", () => _ = SetPrivacyAsync(!IsPrivacyEnabled), () => IsPrivacyEnabled);
        _flipAction = ActionButton("miko-webcam-flip", t => t.Flip, () => "swap-vertical-outline", () => _ = MirrorAsync());
        _fullscreenAction = ActionButton("miko-webcam-fullscreen", t => t.Fullscreen, () => "expand", ToggleFullscreen);
    }

    protected override void OnInitialized()
    {
        BuildElements();
        _video.PlaybackUpdated += SyncState;
        _video.SessionChanged += _ => SyncState();
        _video.PlaybackEvent += OnPlaybackEvent;
    }

    protected override void OnParametersSet()
    {
        if (!_built || !Equals(_device, Device))
        {
            _device = Device;
            CurrentQuality = Quality;
            _lastQualityParameter = Quality;
            Reload();
        }
        else if (_lastQualityParameter != Quality)
        {
            _lastQualityParameter = Quality;
            _ = SetQualityAsync(Quality);
        }
        if (!ReferenceEquals(_lastTexts, Texts))
        {
            _lastTexts = Texts;
            foreach (var (label, text) in _labels) label.TextContent = text(Texts);
            UpdateBadge();
            UpdateQualityButton();
        }
        _root.Class = $"miko-webcam {Class}";
        ApplyLayout();
        _built = true;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddElement(0, _root);

    // ---- public API ----------------------------------------------------------------

    /// <summary>Starts (or resumes) the live picture.</summary>
    public void Play()
    {
        _userStopped = false;
        if (State == WebcamState.Error || Stream == null) { Reload(); return; }
        _video.AutoPlay = true;
        _video.Session?.Play();
        SyncState();
    }

    /// <summary>Stops streaming; the last picture stays up.</summary>
    public void Stop()
    {
        _userStopped = true;
        _video.AutoPlay = false;
        _video.Session?.Pause();
        SyncState();
    }

    /// <summary>Resolves the stream again and reconnects.</summary>
    public void Retry()
    {
        _userStopped = false;
        Reload();
    }

    /// <summary>Switches the stream quality (reconnects).</summary>
    public async Task SetQualityAsync(WebcamQuality quality)
    {
        if (quality == CurrentQuality || _device is not { } device || Provider is not { } provider) return;
        CurrentQuality = quality;
        UpdateQualityButton();
        int generation = Restart(keepInfo: true);
        var token = _load!.Token;
        ShowHint(string.Format(CultureInfo.CurrentCulture, Texts.QualityChanged, Texts.QualityName(quality)));
        if (QualityChanged.HasDelegate) await QualityChanged.InvokeAsync(quality);
        await ResolveStreamAsync(provider, device, quality, generation, token).ConfigureAwait(false);
    }

    /// <summary>Has the device take a snapshot; null when it cannot (the reason is shown as a hint).</summary>
    public async Task<WebcamSnapshot?> CaptureAsync()
    {
        if (_device is not { } device || Provider is not { } provider || _busy) return null;
        _busy = true;
        try
        {
            var snapshot = await provider.CaptureAsync(device).ConfigureAwait(false);
            PostToRenderThread(() => ShowHint(Texts.SnapshotTaken));
            if (SnapshotTaken.HasDelegate) PostToRenderThread(() => _ = SnapshotTaken.InvokeAsync(snapshot));
            return snapshot;
        }
        catch (Exception ex)
        {
            Report(ex, hint: true);
            return null;
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>Turns privacy mode on or off.</summary>
    public async Task SetPrivacyAsync(bool enabled)
    {
        if (_device is not { } device || Provider is not { } provider) return;
        try
        {
            await provider.SetPrivacyAsync(device, enabled).ConfigureAwait(false);
            PostToRenderThread(() =>
            {
                IsPrivacyEnabled = enabled;
                Info = Info is null ? null : Info with { PrivacyEnabled = enabled };
                ApplyStatus();
                UpdateActions();
                Repaint();
            });
        }
        catch (Exception ex)
        {
            Report(ex, hint: true);
        }
    }

    /// <summary>Flips the picture (for cameras mounted upside down).</summary>
    public async Task MirrorAsync()
    {
        if (_device is not { } device || Provider is not { } provider) return;
        try
        {
            await provider.MirrorAsync(device).ConfigureAwait(false);
            PostToRenderThread(() => ShowHint(Texts.Flipped));
        }
        catch (Exception ex)
        {
            Report(ex, hint: true);
        }
    }

    /// <summary>Starts turning the camera; <see cref="StopPtz"/> stops it.</summary>
    public void StartPtz(PtzDirection direction)
    {
        if (_device is not { } device || Provider is not { } provider) return;
        if (_ptzDirection != null) StopPtz();
        _ptzDirection = direction;
        PlatformService?.Haptics.Impact(HapticImpactStyle.Light);
        var speed = PtzSpeed;
        // Commands go out in order: a stop must never overtake its start.
        EnqueuePtz(() => provider.StartPtzAsync(device, direction, speed));
    }

    /// <summary>Stops the movement started with <see cref="StartPtz"/>.</summary>
    public void StopPtz()
    {
        if (_ptzDirection is not { } direction || _device is not { } device || Provider is not { } provider) return;
        _ptzDirection = null;
        EnqueuePtz(() => provider.StopPtzAsync(device, direction));
    }

    public void ToggleFullscreen()
    {
        _pad.Release();
        _overlayPad.Release();
        IsFullscreen = !IsFullscreen;
        _chromeHidden = false;
        if (IsFullscreen) AcquireFullscreenPlatform();
        else ReleaseFullscreenPlatform();
        ApplyLayout();
        Repaint();
        FullscreenChanged?.Invoke();
    }

    // ---- loading -------------------------------------------------------------------

    private void Reload()
    {
        int generation = Restart(keepInfo: false);
        if (_device is not { } device)
        {
            SetState(WebcamState.Idle);
            return;
        }
        if (Provider is not { } provider)
        {
            Fail("No webcam provider is registered; call AddMikoWebcam().");
            return;
        }
        var token = _load!.Token;
        var quality = CurrentQuality;
        _ = LoadInfoAsync(provider, device, generation, token);
        _ = ResolveStreamAsync(provider, device, quality, generation, token);
    }

    // Drops the stream (and the info unless kept), cancels pending provider calls; returns the new generation.
    private int Restart(bool keepInfo)
    {
        _load?.Cancel();
        _load?.Dispose();
        _load = new CancellationTokenSource();
        _pad.Release();
        _overlayPad.Release();
        int generation = ++_generation;
        Stream = null;
        ErrorMessage = null;
        if (!keepInfo)
        {
            Info = null;
            IsPrivacyEnabled = false;
        }
        _video.Source = default;
        _video.MimeType = null;
        _lastBytes = -1;
        BytesPerSecond = null;
        StopRateTimer();
        UpdateBadge();
        UpdateActions();
        SetState(_device == null ? WebcamState.Idle : WebcamState.Connecting);
        return generation;
    }

    private async Task LoadInfoAsync(IWebcamProvider provider, WebcamDevice device, int generation, CancellationToken token)
    {
        try
        {
            var info = await provider.GetInfoAsync(device, token).ConfigureAwait(false);
            PostToRenderThread(() =>
            {
                if (generation != _generation) return;
                Info = info;
                IsPrivacyEnabled = info.PrivacyEnabled == true;
                UpdateActions();
                ApplyStatus();
                Repaint();
                if (InfoLoaded.HasDelegate) _ = InfoLoaded.InvokeAsync(info);
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // The stream may still work (a LAN camera without the cloud); its own failure is what shows.
            Report(ex, hint: false, show: false);
        }
    }

    private async Task ResolveStreamAsync(IWebcamProvider provider, WebcamDevice device, WebcamQuality quality, int generation, CancellationToken token)
    {
        try
        {
            var stream = await provider.GetLiveStreamAsync(device, quality, token).ConfigureAwait(false);
            PostToRenderThread(() =>
            {
                if (generation != _generation) return;
                Stream = stream;
                _video.MimeType = stream.MimeType;
                _video.AutoPlay = AutoPlay && !_userStopped;
                _video.Source = stream.Source;
                if (!_video.AutoPlay) _userStopped = true;
                UpdateBadge();
                SyncState();
                Repaint();
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            PostToRenderThread(() =>
            {
                if (generation == _generation) Fail(Describe(ex));
            });
            Report(ex, hint: false, show: false);
        }
    }

    private void EnqueuePtz(Func<Task> command)
    {
        var previous = _ptzQueue;
        _ptzQueue = Run();

        async Task Run()
        {
            try { await previous.ConfigureAwait(false); } catch { }
            try
            {
                await command().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // A limit (or any refusal) ends the hold: the arrow lets go and says why.
                PostToRenderThread(() =>
                {
                    _pad.Release();
                    _overlayPad.Release();
                });
                Report(ex, hint: true);
            }
        }
    }

    // ---- state ---------------------------------------------------------------------

    private void OnPlaybackEvent(VideoSessionEvent evt)
    {
        if (evt is VideoSessionEvent.Error error)
        {
            Fail(error.Cause != null ? Describe(error.Cause) : error.Message);
            if (Error.HasDelegate) _ = Error.InvokeAsync(error.Cause ?? new WebcamException(error.Message));
        }
        else
        {
            SyncState();
        }
    }

    // The session is the truth once a stream is set; called every frame the video is in the tree.
    private void SyncState()
    {
        if (_disposed || State == WebcamState.Error || Stream == null) return;
        var session = _video.Session;
        var state = session?.State switch
        {
            VideoSessionState.Playing => WebcamState.Playing,
            VideoSessionState.Error => WebcamState.Error,
            VideoSessionState.Paused or VideoSessionState.Ready or VideoSessionState.Ended when _userStopped => WebcamState.Stopped,
            _ => _userStopped ? WebcamState.Stopped : WebcamState.Connecting,
        };
        if (state == WebcamState.Error)
        {
            Fail(session?.ErrorMessage ?? "The stream failed.");
            return;
        }
        SetState(state);
    }

    private void SetState(WebcamState state)
    {
        if (State == state && _built) return;
        State = state;
        if (state == WebcamState.Playing) StartRateTimer();
        else StopRateTimer();
        ApplyStatus();
        Repaint();
    }

    private void Fail(string message)
    {
        ErrorMessage = message;
        _pad.Release();
        _overlayPad.Release();
        State = WebcamState.Error;
        StopRateTimer();
        ApplyStatus();
        Repaint();
    }

    // Shows a provider failure as a hint (actions) and passes it to Error; never throws.
    private void Report(Exception ex, bool hint, bool show = true)
    {
        if (ex is OperationCanceledException) return;
        string message = Describe(ex);
        PostToRenderThread(() =>
        {
            if (_disposed) return;
            if (hint && show) ShowHint(message);
            if (Error.HasDelegate) _ = Error.InvokeAsync(ex);
        });
    }

    private string Describe(Exception ex) => Texts.Describe(ex);

    // ---- bitrate -------------------------------------------------------------------

    private void StartRateTimer()
    {
        if (_rateTimer != null || _disposed) return;
        var clock = Clock ?? TimeProvider.System;
        using (ExecutionContext.SuppressFlow())
            _rateTimer = clock.CreateTimer(static s => ((MikoWebcam)s!).PostToRenderThread(((MikoWebcam)s!).UpdateRate), this, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        UpdateRate();
    }

    private void StopRateTimer()
    {
        _rateTimer?.Dispose();
        _rateTimer = null;
    }

    private void UpdateRate()
    {
        if (_disposed) return;
        long? bytes = (_video.Session as EncodedVideoSession)?.BytesReceived;
        long? rate = null;
        if (bytes is { } b)
        {
            if (_lastBytes >= 0 && b >= _lastBytes) rate = b - _lastBytes;
            _lastBytes = b;
        }
        if (rate == BytesPerSecond && !string.IsNullOrEmpty(_rate.TextContent)) return;
        BytesPerSecond = rate;
        _rate.TextContent = rate is { } r ? FormatRate(r) : bytes is null ? "" : "0 KB/s";
        Repaint();
    }

    internal static string FormatRate(long bytesPerSecond) => bytesPerSecond >= 1024 * 1024
        ? (bytesPerSecond / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) + " MB/s"
        : (bytesPerSecond / 1024).ToString(CultureInfo.InvariantCulture) + " KB/s";

    // ---- hint ----------------------------------------------------------------------

    private void ShowHint(string text)
    {
        if (_disposed) return;
        Hint = text;
        _hint.TextContent = text;
        ChangeStyle(_hintLayer, s => s.Display = Display.Flex);
        _hintTimer ??= CreateTimer(static s => { var w = (MikoWebcam)s!; w.PostToRenderThread(w.HideHint); });
        _hintTimer.Change(ToastDuration, Timeout.InfiniteTimeSpan);
        Repaint();
    }

    private void HideHint()
    {
        if (_disposed || Hint == null) return;
        Hint = null;
        ChangeStyle(_hintLayer, s => s.Display = Display.None);
        Repaint();
    }

    // ---- layout --------------------------------------------------------------------

    private void BuildElements()
    {
        // Picture.
        _viewport.Children.Add(_video);
        _video.Style = Fill();
        _viewport.OnClick = OnPictureClick;

        // Status over the picture: spinner, play button, error, privacy.
        _status.Style = new Style
        {
            Display = Display.Flex, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0), Width = Length.Percent(100),
            Height = Length.Percent(100), FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center, PointerEvents = PointerEvents.None, Color = Color.White, FontSize = Length.Px(13),
        };
        _statusIcon.Style = new Style { Display = Display.Block, Width = Length.Px(56), Height = Length.Px(56), FlexShrink = 0 };
        _statusIcon.Paint = DrawStatusIcon;
        _statusText.Style = new Style { Display = Display.Block, MarginTop = Length.Px(6), PaddingLeft = Length.Px(16), PaddingRight = Length.Px(16), TextAlign = TextAlign.Center };
        _statusButton.Style = new Style
        {
            Display = Display.Flex, AlignItems = AlignItems.Center, MarginTop = Length.Px(12), Height = Length.Px(32), PaddingLeft = Length.Px(18), PaddingRight = Length.Px(18),
            BorderRadius = 16, BorderWidth = Length.Px(0), BackgroundColor = Color.FromRgba(255, 255, 255, .2f), Color = Color.White,
            FontSize = Length.Px(13), PointerEvents = PointerEvents.Auto,
        };
        _statusButton.OnClick = e =>
        {
            e.StopPropagation();
            if (State == WebcamState.Error) Retry();
            else if (IsPrivacyEnabled) _ = SetPrivacyAsync(false);
        };
        _status.Children.Add(_statusIcon);
        _status.Children.Add(_statusText);
        _status.Children.Add(_statusButton);
        _viewport.Children.Add(_status);

        _badge.Style = new Style
        {
            Display = Display.None, Position = Position.Absolute, Top = Length.Px(8), Left = Length.Px(8), PaddingLeft = Length.Px(8),
            PaddingRight = Length.Px(8), PaddingTop = Length.Px(2), PaddingBottom = Length.Px(2), BorderRadius = 10,
            BackgroundColor = Color.FromRgba(0, 0, 0, .45f), Color = Color.White, FontSize = Length.Px(11), PointerEvents = PointerEvents.None,
        };
        _viewport.Children.Add(_badge);

        _hintLayer.Style = new Style
        {
            Display = Display.None, Position = Position.Absolute, Left = Length.Px(0), Right = Length.Px(0), Bottom = Length.Px(14),
            JustifyContent = JustifyContent.Center, PointerEvents = PointerEvents.None,
        };
        _hint.Style = new Style
        {
            Display = Display.Block, PaddingLeft = Length.Px(14), PaddingRight = Length.Px(14), PaddingTop = Length.Px(6), PaddingBottom = Length.Px(6),
            BorderRadius = 16, BackgroundColor = Color.FromRgba(0, 0, 0, .6f), Color = Color.White, FontSize = Length.Px(13),
        };
        _hintLayer.Children.Add(_hint);
        _viewport.Children.Add(_hintLayer);

        _overlayPadHost.Children.Add(_overlayPad.Element);
        _viewport.Children.Add(_overlayPadHost);
        _root.Children.Add(_viewport);

        // Toolbar: play / stop | quality | bitrate ……… fullscreen.
        _toolbar.Children.Add(ToolbarIcon("miko-webcam-play", () => State is WebcamState.Playing or WebcamState.Connecting ? "stop" : "play",
            () => { if (State is WebcamState.Playing or WebcamState.Connecting) Stop(); else Play(); }));
        _toolbar.Children.Add(Divider());
        _qualityButton.Style = new Style
        {
            Display = Display.Flex, AlignItems = AlignItems.Center, Height = Length.Px(44), PaddingLeft = Length.Px(14), PaddingRight = Length.Px(14), FlexShrink = 0,
            BorderWidth = Length.Px(0), BackgroundColor = Color.Transparent, Color = Color.White, FontSize = Length.Px(14),
        };
        _qualityButton.OnClick = e =>
        {
            e.StopPropagation();
            _ = SetQualityAsync(CurrentQuality == WebcamQuality.High ? WebcamQuality.Smooth : WebcamQuality.High);
        };
        _toolbar.Children.Add(_qualityButton);
        _toolbar.Children.Add(Divider());
        _rate.Style = new Style
        {
            Display = Display.Block, FlexGrow = 1, MinWidth = Length.Px(0), PaddingLeft = Length.Px(14), FontSize = Length.Px(13),
            Color = Color.FromHex("#d6d9de"), WhiteSpace = WhiteSpace.Nowrap,
        };
        _toolbar.Children.Add(_rate);
        _toolbar.Children.Add(ToolbarIcon("miko-webcam-fullscreen-button", () => IsFullscreen ? "contract" : "expand", ToggleFullscreen));
        _toolbar.OnClick = e => e.StopPropagation();
        _root.Children.Add(_toolbar);

        // Actions.
        _actions.Children.Add(_snapshotAction);
        _actions.Children.Add(_privacyAction);
        _actions.Children.Add(_flipAction);
        _actions.Children.Add(_fullscreenAction);
        _root.Children.Add(_actions);

        // Pan / tilt.
        _ptzArea.Children.Add(_pad.Element);
        _ptzCaption.Style = new Style
        {
            Display = Display.Block, MarginTop = Length.Px(16), FontSize = Length.Px(12), Color = MutedText, TextAlign = TextAlign.Center,
            PaddingLeft = Length.Px(16), PaddingRight = Length.Px(16),
        };
        _ptzArea.Children.Add(_ptzCaption);
        _root.Children.Add(_ptzArea);

        UpdateQualityButton();
        UpdateActions();
        ApplyStatus();
    }

    private void ApplyLayout()
    {
        var root = new Style
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column, Position = Position.Relative, Width = Length.Percent(100),
            MinHeight = Length.Px(0), BackgroundColor = PageColor, Color = TextColor, FontSize = Length.Px(14), BoxSizing = BoxSizing.BorderBox,
        };
        if (Style != null) root.Merge(Style);
        var viewport = new Style
        {
            Display = Display.Block, Position = Position.Relative, Width = Length.Percent(100), AspectRatio = new AspectRatio(16, 9),
            FlexShrink = 0, BackgroundColor = Color.Black, Overflow = Overflow.Hidden,
        };
        var toolbar = new Style
        {
            Display = Display.Flex, AlignItems = AlignItems.Center, Height = Length.Px(48), FlexShrink = 0, PaddingLeft = Length.Px(4),
            PaddingRight = Length.Px(4), BackgroundColor = Color.FromHex("#232323"), Color = Color.White, BoxSizing = BoxSizing.BorderBox,
        };
        var padHost = new Style { Display = Display.None };

        if (IsFullscreen)
        {
            root.Position = Position.Fixed;
            root.Top = Length.Px(0); root.Left = Length.Px(0);
            root.Width = Length.Vw(100); root.Height = Length.Vh(100);
            root.MaxWidth = null; root.MaxHeight = null;
            root.Margin = Length.Px(0); root.Padding = Length.Px(0);
            root.BackgroundColor = Color.Black;
            root.ZIndex = 10000;
            viewport.AspectRatio = null;
            viewport.FlexGrow = 1;
            viewport.Height = Length.Px(0);
            // The toolbar floats over the bottom of the picture, clear of the cutout and gesture bar.
            toolbar.Position = Position.Absolute;
            toolbar.Left = Length.Px(0); toolbar.Right = Length.Px(0); toolbar.Bottom = Length.Px(0);
            toolbar.Height = Length.SafeAreaInsetBottom + Length.Px(48);
            toolbar.PaddingLeft = Length.SafeAreaInsetLeft + Length.Px(8);
            toolbar.PaddingRight = Length.SafeAreaInsetRight + Length.Px(8);
            toolbar.PaddingBottom = Length.SafeAreaInsetBottom;
            toolbar.BackgroundColor = Color.FromRgba(0, 0, 0, .55f);
            if (_chromeHidden) { toolbar.Opacity = 0; toolbar.PointerEvents = PointerEvents.None; }
            if (HasPanOrTilt && ShowPtz && !_chromeHidden)
            {
                padHost.Display = Display.Block;
                padHost.Position = Position.Absolute;
                padHost.Right = Length.SafeAreaInsetRight + Length.Px(20);
                padHost.Bottom = Length.SafeAreaInsetBottom + Length.Px(64);
            }
        }
        else if (Style?.Height != null)
        {
            // A given height: the picture keeps 16:9, the panel below takes the rest.
            root.Height = Style.Height;
        }

        _root.Style = root;
        _viewport.Style = viewport;
        _toolbar.Style = toolbar;
        _overlayPadHost.Style = padHost;
        _actions.Style = new Style
        {
            Display = IsFullscreen || !ShowActions ? Display.None : Display.Flex, JustifyContent = JustifyContent.SpaceAround,
            FlexShrink = 0, PaddingTop = Length.Px(18), PaddingBottom = Length.Px(10), PaddingLeft = Length.Px(8), PaddingRight = Length.Px(8),
        };
        _ptzArea.Style = new Style
        {
            Display = IsFullscreen || !ShowPtz || !ShowsPtzArea ? Display.None : Display.Flex, FlexDirection = FlexDirection.Column,
            AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center, FlexGrow = 1, PaddingTop = Length.Px(20), PaddingBottom = Length.Px(28),
        };
    }

    private bool HasPanOrTilt => Info is { } info && (info.Features & (WebcamFeatures.Pan | WebcamFeatures.Tilt)) != 0;

    // The disk shows for a camera that turns, and — disabled, with the reason — for one the cloud
    // reports offline while it streams on the LAN (it may well turn, the cloud just cannot tell it to).
    private bool ShowsPtzArea => HasPanOrTilt || Info is { IsOnLocalNetwork: true, Features: WebcamFeatures.None };

    private void UpdateActions()
    {
        var features = Info?.Features ?? WebcamFeatures.None;
        SetVisible(_snapshotAction, (features & WebcamFeatures.Capture) != 0);
        SetVisible(_privacyAction, (features & WebcamFeatures.Privacy) != 0);
        SetVisible(_flipAction, (features & WebcamFeatures.Mirror) != 0);
        foreach (var pad in new[] { _pad, _overlayPad })
        {
            pad.Enabled = HasPanOrTilt;
            pad.CanPan = (features & WebcamFeatures.Pan) != 0;
            pad.CanTilt = (features & WebcamFeatures.Tilt) != 0;
        }
        _ptzCaption.TextContent = HasPanOrTilt ? Texts.PtzHint : Texts.PtzUnavailable;
        ApplyLayout();
    }

    private void UpdateBadge()
    {
        _badge.TextContent = Stream?.Transport switch
        {
            WebcamTransport.LocalNetwork => Texts.LocalNetwork,
            WebcamTransport.CloudRelay => Texts.CloudRelay,
            _ => "",
        };
        ChangeStyle(_badge, s => s.Display = Stream == null ? Display.None : Display.Block);
    }

    private void UpdateQualityButton() => _qualityButton.TextContent = Texts.QualityName(CurrentQuality);

    private void ApplyStatus()
    {
        bool privacy = IsPrivacyEnabled && State != WebcamState.Error;
        bool visible = privacy || State is WebcamState.Connecting or WebcamState.Stopped or WebcamState.Error;
        string text = privacy ? Texts.PrivacyOn : State switch
        {
            WebcamState.Connecting => Texts.Connecting,
            WebcamState.Error => ErrorMessage ?? "",
            _ => "",
        };
        string? button = State == WebcamState.Error ? Texts.Retry : privacy && (Info?.Features & WebcamFeatures.Privacy) != 0 ? Texts.TurnOffPrivacy : null;
        _statusText.TextContent = text;
        _statusButton.TextContent = button ?? "";
        ChangeStyle(_statusText, s => s.Display = text.Length > 0 ? Display.Block : Display.None);
        ChangeStyle(_statusButton, s => s.Display = button != null ? Display.Flex : Display.None);
        ChangeStyle(_status, s =>
        {
            s.Display = visible ? Display.Flex : Display.None;
            // Privacy and errors hide the picture; a stopped stream dims its last frame.
            s.BackgroundColor = privacy || State == WebcamState.Error ? Color.FromRgba(18, 20, 24, .92f)
                : State == WebcamState.Stopped ? Color.FromRgba(0, 0, 0, .35f) : Color.Transparent;
        });
    }

    private void DrawStatusIcon(IDrawingContext context, RectF bounds)
    {
        float cx = bounds.X + bounds.Width / 2, cy = bounds.Y + bounds.Height / 2;
        if (IsPrivacyEnabled && State != WebcamState.Error)
            _icons.Draw(context, cx, cy, "eye-off-outline", 40, Color.FromRgba(255, 255, 255, .85f));
        else if (State == WebcamState.Connecting)
        {
            context.StrokeArc(new RectF(cx - 16, cy - 16, 32, 32), (Environment.TickCount64 % 1000) * .36f, 270, Color.White, 3);
            Repaint(); // keeps the spinner turning while connecting, and only then
        }
        else if (State == WebcamState.Stopped)
        {
            context.FillCircle(cx, cy, 26, Color.FromRgba(0, 0, 0, .45f));
            _icons.Draw(context, cx + 2, cy, "play", 26, Color.White);
        }
        else if (State == WebcamState.Error)
            _icons.Draw(context, cx, cy, "alert-circle-outline", 40, Color.FromRgba(255, 255, 255, .85f));
    }

    private void OnPictureClick(MouseEventArgs e)
    {
        if (State == WebcamState.Stopped) { Play(); return; }
        if (!IsFullscreen) return;
        _chromeHidden = !_chromeHidden;
        ApplyLayout();
        Repaint();
    }

    // ---- fullscreen ------------------------------------------------------------------

    private void AcquireFullscreenPlatform()
    {
        if (_fullscreenPlatform is not null || PlatformService is not { } platform) return;
        _fullscreenPlatform = platform;
        platform.Orientation.Lock(ScreenOrientation.Landscape);
        platform.Orientation.KeepAwake();
        platform.SystemBars.Hide();
        _fullscreenBack = platform.Back.Register(() =>
        {
            if (_disposed || !IsFullscreen) return false;
            ToggleFullscreen();
            return true;
        }, BackButtonPriority.Overlay);
    }

    private void ReleaseFullscreenPlatform()
    {
        if (_fullscreenPlatform is not { } platform) return;
        _fullscreenPlatform = null;
        _fullscreenBack?.Dispose();
        _fullscreenBack = null;
        platform.Orientation.Unlock();
        platform.Orientation.AllowSleep();
        platform.SystemBars.Show();
    }

    // ---- element helpers -------------------------------------------------------------

    private ButtonElement ToolbarIcon(string className, Func<string> icon, Action action)
    {
        var button = new ButtonElement
        {
            Class = className,
            Style = new Style
            {
                Display = Display.Block, Position = Position.Relative, Width = Length.Px(44), Height = Length.Px(44), FlexShrink = 0,
                BackgroundColor = Color.Transparent, Padding = Length.Px(0), BorderWidth = Length.Px(0),
            },
        };
        button.Children.Add(new CanvasElement { Style = Fill(), Paint = (c, r) => _icons.Draw(c, r, icon(), 22, Color.White) });
        button.OnClick = e => { e.StopPropagation(); action(); Repaint(); };
        return button;
    }

    private static DivElement Divider() => new()
    {
        Style = new Style { Display = Display.Block, Width = Length.Px(1), Height = Length.Px(18), FlexShrink = 0, BackgroundColor = Color.FromHex("#4a4a4a") },
    };

    private ButtonElement ActionButton(string className, Func<WebcamTexts, string> label, Func<string> icon, Action action, Func<bool>? active = null)
    {
        var button = new ButtonElement
        {
            Class = $"miko-webcam-action {className}",
            Style = new Style
            {
                Display = Display.Flex, FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center, MinWidth = Length.Px(64),
                PaddingTop = Length.Px(4), PaddingBottom = Length.Px(4), BorderWidth = Length.Px(0), BackgroundColor = Color.Transparent,
                Color = TextColor, FontSize = Length.Px(13),
            },
        };
        button.Children.Add(new CanvasElement
        {
            Style = new Style { Display = Display.Block, Width = Length.Px(32), Height = Length.Px(32) },
            Paint = (c, r) => _icons.Draw(c, r, icon(), 28, active?.Invoke() == true ? Accent : TextColor),
        });
        var text = new SpanElement { Style = new Style { Display = Display.Block, MarginTop = Length.Px(6), WhiteSpace = WhiteSpace.Nowrap } };
        _labels.Add((text, label));
        text.TextContent = label(Texts);
        button.Children.Add(text);
        button.OnClick = e => { e.StopPropagation(); action(); Repaint(); };
        return button;
    }

    private static void SetVisible(Element element, bool visible) =>
        ChangeStyle(element, s => s.Display = visible ? Display.Flex : Display.None);

    private static Style Fill() => new()
    {
        Display = Display.Block, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0),
        Width = Length.Percent(100), Height = Length.Percent(100),
    };

    private static void ChangeStyle(Element element, Action<Style> update)
    {
        var style = new Style();
        if (element.Style != null) style.Merge(element.Style);
        update(style);
        element.Style = style;
    }

    private ITimer CreateTimer(TimerCallback callback)
    {
        var clock = Clock ?? TimeProvider.System;
        using (ExecutionContext.SuppressFlow())
            return clock.CreateTimer(callback, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Runs <paramref name="action"/> on the render thread (at once without an engine).</summary>
    private void PostToRenderThread(Action action)
    {
        if (Engine is { } engine) engine.Dispatcher.Post(() => { if (!_disposed) action(); });
        else if (!_disposed) action();
    }

    private void Repaint() => Engine?.RequestRepaint();

    public void Dispose()
    {
        if (_disposed) return;
        // Let go of a held direction first, or the camera keeps turning.
        StopPtz();
        _disposed = true;
        _load?.Cancel();
        _load?.Dispose();
        _load = null;
        StopRateTimer();
        _hintTimer?.Dispose();
        _hintTimer = null;
        ReleaseFullscreenPlatform();
        _icons.Dispose();
    }
}
