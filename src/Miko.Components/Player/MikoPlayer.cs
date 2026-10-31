using System.Globalization;
using Miko.Animation;
using Miko.Common;
using Miko.Components.Player.Danmaku;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Platform;
using Miko.Platform.Video;
using Miko.Styling;

namespace Miko.Components.Player;

/// <summary>A retained native video player. Public methods and parameters are UI-thread APIs.</summary>
/// <remarks>
/// The picture fills the player; the controls, the settings panel and the hints are overlays on it.
/// Touch: a tap shows or hides the controls, a double tap plays or pauses, a long press plays at
/// <see cref="LongPressPlaybackRate"/> until released. Mouse: a click plays or pauses, moving the
/// mouse shows the controls. While playing, the controls hide after <see cref="ControlsHideDelay"/>.
/// </remarks>
public sealed partial class MikoPlayer : ComponentBase, IDisposable
{
    [Parameter] public MediaSource Source { get; set; }
    [Parameter] public string? MimeType { get; set; }
    [Parameter] public string? Poster { get; set; }
    [Parameter] public bool AutoPlay { get; set; }
    [Parameter] public bool Loop { get; set; }
    [Parameter] public bool Muted { get; set; }
    [Parameter] public float Volume { get; set; } = 1;
    [Parameter] public float PlaybackRate { get; set; } = 1;
    [Parameter] public string? Class { get; set; }
    [Parameter] public Style? Style { get; set; }
    [Parameter] public IReadOnlyList<DanmakuItem> Danmaku { get; set; } = Array.Empty<DanmakuItem>();
    [Parameter] public DanmakuSettings DanmakuSettings { get; set; } = new();

    /// <summary>
    /// Playback rate while the picture is long-pressed; releasing restores the previous rate.
    /// Clamped to the controller's 0.25–4 range; 0 disables the gesture.
    /// </summary>
    [Parameter] public float LongPressPlaybackRate { get; set; } = 2;

    /// <summary>
    /// How long the controls stay up during playback after the last interaction. Zero or less keeps
    /// them up until the picture is tapped.
    /// </summary>
    [Parameter] public TimeSpan ControlsHideDelay { get; set; } = TimeSpan.FromSeconds(5);

    [Inject] public MikoEngine? Engine { get; set; }
    [Inject] public MikoPlayerOptions Options { get; set; } = new();

    /// <summary>
    /// Component-facing platform service: landscape lock, keep-awake, hidden system bars and the
    /// system back button while fullscreen (ISSUE-149), haptics for the long press. Null without a
    /// service scope; fullscreen then stays a pure viewport layout.
    /// </summary>
    [Inject] public IPlatformService? PlatformService { get; set; }

    /// <summary>Clock of the auto-hide and double-tap timers; the system clock when none is registered.</summary>
    [Inject] public TimeProvider? Clock { get; set; }

    public PlayerController Controller { get; } = new();
    public bool IsFullscreen { get; private set; }
    public bool IsSettingsOpen { get; private set; }

    /// <summary>Whether the control bar is shown over the picture.</summary>
    public bool AreControlsVisible { get; private set; } = true;

    /// <summary>Whether a long press is holding playback at <see cref="LongPressPlaybackRate"/>.</summary>
    public bool IsLongPressActive { get; private set; }

    /// <summary>
    /// Raised after <see cref="IsFullscreen"/> changes — including when the system back button exits
    /// fullscreen, which the host did not initiate. Hosts that render UI off the fullscreen state
    /// re-render from here.
    /// </summary>
    public event Action? FullscreenChanged;

    private readonly DivElement _root = new();
    private readonly DivElement _viewport = new();
    private readonly VideoElement _video = new();
    private readonly CanvasElement _overlay = new();
    private readonly DivElement _chrome = new();
    private readonly CanvasElement _progress = new();
    private readonly DivElement _settings = new();
    private readonly DivElement _settingsPanel = new();
    private readonly DivElement _hint = new();
    private readonly SpanElement _hintText = new();
    private readonly SpanElement _time = new();
    private readonly SpanElement _tooltip = new();
    private readonly DivElement _error = new();
    private readonly SpanElement _errorText = new();
    private readonly PlayerIcons _icons = new();
    private readonly DanmakuTimeline _timeline = new();
    private readonly DanmakuPainter _danmakuPainter = new();
    private static readonly Color TrackColor = Color.FromHex("#494d53");
    private static readonly Color BufferedColor = Color.FromHex("#8d939c");
    private static readonly Color ProgressColor = Color.FromHex("#38cfa1");
    // The controls and the settings fade in and out instead of popping. The list is on both states:
    // the engine reads the transitions of the previous frame's style.
    private static readonly List<Transition> FadeTransitions = [Transition.For("opacity", .2f)];
    private IReadOnlyList<DanmakuItem>? _lastDanmaku;
    private DanmakuSettings? _lastSettings;
    private MediaSource _lastSource;
    private string? _lastMime;
    private bool _built, _disposed, _dragging, _refreshing;
    private float _scrub;
    private VideoSessionState _lastState;
    private long _lastTimeUpdate;
    private bool _lastMuted, _lastLoop;
    private float _lastVolume = float.NaN, _lastRate = float.NaN;

    // The platform requests held while fullscreen (ISSUE-149): the service they were taken from and
    // the back-button slot. Non-null exactly while held, so leaving fullscreen and Dispose release
    // each request once.
    private IPlatformService? _fullscreenPlatform;
    private IDisposable? _fullscreenBack;

    protected override void OnInitialized()
    {
        BuildElements();
        Controller.Changed += UpdateControls;
        _video.SessionChanged += Controller.Attach;
        _video.PlaybackEvent += Controller.HandleEvent;
        _video.PlaybackUpdated += Refresh;
    }

    protected override void OnParametersSet()
    {
        if (!_built || !_lastSource.Equals(Source) || _lastMime != MimeType)
        {
            EndLongPress();
            Controller.Reset(AutoPlay);
            _video.Source = Source;
            _video.MimeType = MimeType;
            _video.AutoPlay = AutoPlay;
            _lastSource = Source;
            _lastMime = MimeType;
            _timeline.Reset();
        }
        _video.Poster = Poster;
        if (!_built || _lastMuted != Muted) { Controller.SetMuted(Muted); _lastMuted = Muted; }
        if (!_built || _lastLoop != Loop) { Controller.SetLoop(Loop); _lastLoop = Loop; }
        if (_lastVolume != Volume) { Controller.SetVolume(Volume); _lastVolume = Volume; }
        if (_lastRate != PlaybackRate)
        {
            // A long press holds its own rate; the new one takes over when the press ends.
            if (IsLongPressActive) _rateBeforeLongPress = PlaybackRate;
            else Controller.SetPlaybackRate(PlaybackRate);
            _lastRate = PlaybackRate;
        }
        _video.Muted = Controller.Muted;
        _video.Loop = Controller.Loop;
        if (!ReferenceEquals(_lastDanmaku, Danmaku)) { _timeline.SetItems(Danmaku); _lastDanmaku = Danmaku; }
        if (_lastSettings != DanmakuSettings)
        {
            _timeline.Settings = DanmakuSettings with { MaxVisible = Math.Min(DanmakuSettings.MaxVisible, Options.MaxVisibleDanmaku) };
            _lastSettings = DanmakuSettings;
        }
        _root.Class = $"miko-player {Class}";
        ApplyRootStyle();
        if (!Source.IsEmpty && Engine is { VideoBackend: null or NullVideoBackend })
            Controller.HandleEvent(new VideoSessionEvent.Error("No video backend is available.", null));
        _built = true;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddElement(0, _root);
    public void Play() => Controller.Play();
    public void Pause() => Controller.Pause();
    public void Seek(TimeSpan position) { _timeline.Reset(); Controller.Seek(position); Repaint(); }
    public void SetVolume(float volume) => Controller.SetVolume(volume);
    public void SetMuted(bool muted) => Controller.SetMuted(muted);
    public void SetPlaybackRate(float rate) => Controller.SetPlaybackRate(rate);
    public void SetDanmakuSettings(DanmakuSettings settings)
    {
        _timeline.Settings = settings with { MaxVisible = Math.Min(settings.MaxVisible, Options.MaxVisibleDanmaku) };
        Repaint();
    }
    public void Retry() { EndLongPress(); Controller.Reset(AutoPlay); _video.Reload(); Repaint(); }

    public void ToggleFullscreen()
    {
        // The panel is laid out for the mode it was opened in.
        CloseSettings();
        IsFullscreen = !IsFullscreen;
        if (IsFullscreen) AcquireFullscreenPlatform();
        else ReleaseFullscreenPlatform();
        ApplyRootStyle();
        ApplyChromeStyle();
        ApplyHintStyle();
        ShowControls();
        Repaint();
        FullscreenChanged?.Invoke();
    }

    /// <summary>
    /// Fullscreen on a phone means landscape with the screen kept on and the system bars hidden, and
    /// the system back button (Android back, iOS edge swipe, desktop Escape) leaving fullscreen before
    /// it leaves the page. The back slot uses overlay priority so it outranks the page's back button.
    /// </summary>
    private void AcquireFullscreenPlatform()
    {
        if (_fullscreenPlatform is not null || PlatformService is not { } platform) return;

        _fullscreenPlatform = platform;
        platform.Orientation.Lock(ScreenOrientation.Landscape);
        platform.Orientation.KeepAwake();
        platform.SystemBars.Hide();
        // Checked again when invoked: the slot may outlive the state that registered it.
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

    /// <summary>
    /// Inline: 16:9 of the player width, resolved by layout through <c>aspect-ratio</c> (ISSUE-150);
    /// a height from <see cref="Style"/> wins. Fullscreen: the whole viewport, under the camera cutout
    /// too — only the overlays keep clear of it (<see cref="ApplyChromeStyle"/>), the picture is
    /// letterboxed in black like any fullscreen player.
    /// </summary>
    private void ApplyRootStyle()
    {
        var style = new Style
        {
            Display = Display.Block, Position = Position.Relative, Width = Length.Percent(100), MinWidth = Length.Px(0),
            AspectRatio = new AspectRatio(16, 9), FlexShrink = 0, Overflow = Overflow.Hidden, BackgroundColor = Color.Black,
            Color = Color.White, FontFamily = "Arial", FontSize = Length.Px(14), BoxSizing = BoxSizing.BorderBox
        };
        if (Style != null) style.Merge(Style);
        if (IsFullscreen)
        {
            style.Position = Position.Fixed;
            style.Top = Length.Px(0); style.Left = Length.Px(0);
            style.Width = Length.Vw(100); style.Height = Length.Vh(100);
            style.MaxWidth = null; style.MaxHeight = null; style.AspectRatio = null;
            style.Margin = Length.Px(0); style.Padding = Length.Px(0);
            style.ZIndex = 10000;
        }
        _root.Style = style;
    }

    private static Style Fill() => new()
    {
        Display = Display.Block, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0),
        Width = Length.Percent(100), Height = Length.Percent(100)
    };

    private void BuildElements()
    {
        // Paint order is tree order (every layer is positioned): picture, error, hint, controls,
        // settings, tooltip. Hit testing runs the other way, so an open panel takes its taps first.
        _viewport.Class = "miko-player-viewport";
        var viewport = Fill();
        viewport.BackgroundColor = Color.Black;
        viewport.Overflow = Overflow.Hidden;
        _viewport.Style = viewport;
        _video.Style = Fill();
        _overlay.Style = Fill();
        _overlay.Paint = DrawOverlay;
        _viewport.Children.Add(_video);
        _viewport.Children.Add(_overlay);
        _viewport.OnClick = OnSurfaceClick;
        _viewport.OnLongPress = OnSurfaceLongPress;
        _viewport.OnPointerMove = e => { if (IsLongPressActive) e.PreventDefault(); };
        _viewport.OnPointerUp = _ => EndLongPress();
        _viewport.OnPointerCancel = _ => EndLongPress();
        _root.Children.Add(_viewport);

        _error.Style = new Style { Display = Display.None, Position = Position.Absolute, Top = Length.Px(12), Left = Length.Px(12),
            Right = Length.Px(12), FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center, BackgroundColor = Color.FromHex("#292c30"), Padding = Length.Px(12) };
        _errorText.Style = new Style { Display = Display.Block, Width = Length.Percent(100), FontSize = Length.Px(13) };
        _error.Children.Add(_errorText);
        _error.Children.Add(IconButton("Retry", () => "refresh", Retry));
        _root.Children.Add(_error);

        BuildHint();
        BuildChrome();

        _settings.Class = "miko-player-settings";
        _settingsPanel.Class = "miko-player-settings-panel";
        _settings.Children.Add(_settingsPanel);
        ApplySettingsStyle();
        _root.Children.Add(_settings);

        _tooltip.Style = new Style { Display = Display.None, Position = Position.Absolute, Bottom = Length.Px(76), Right = Length.Px(12),
            BackgroundColor = Color.FromRgba(28, 28, 30, .9f), Color = Color.White, Padding = Length.Px(6), FontSize = Length.Px(12), PointerEvents = PointerEvents.None };
        _root.Children.Add(_tooltip);

        // Moving the mouse over the player brings the controls back (and restarts their timer).
        _root.OnPointerMove = e => { if (e.PointerType == PointerType.Mouse) ShowControls(); };
        _root.OnKeyDown = e =>
        {
            if (e.Target is InputElement or SelectElement) return;
            switch (e.Key)
            {
                case " ": case "Space": Controller.TogglePlayback(); break;
                case "ArrowLeft": Seek(Controller.Position - TimeSpan.FromSeconds(5)); break;
                case "ArrowRight": Seek(Controller.Position + TimeSpan.FromSeconds(5)); break;
                case "m": case "M": Controller.SetMuted(!Controller.Muted); break;
                case "f": case "F": ToggleFullscreen(); break;
                // With a platform service, Escape reaches the back-button chain first: it closes the
                // settings or leaves fullscreen there and is swallowed. This branch only serves hosts
                // without one.
                case "Escape":
                    if (IsSettingsOpen) CloseSettings();
                    else if (IsFullscreen && _fullscreenPlatform is null) ToggleFullscreen();
                    break;
                default: return;
            }
            ShowControls();
            e.PreventDefault(); e.StopPropagation();
        };
    }

    /// <summary>
    /// The control bar: a gradient scrim, the progress bar and the button row, overlaid on the bottom
    /// of the picture. Its blank parts are part of the picture for gestures.
    /// </summary>
    private void BuildChrome()
    {
        _chrome.Class = "miko-player-chrome";
        ApplyChromeStyle();
        // Darkens the picture under the controls so they stay legible on bright frames.
        _chrome.Children.Add(new CanvasElement
        {
            Class = "miko-player-scrim",
            Style = new Style { Display = Display.Block, Position = Position.Absolute, Left = Length.Px(0), Right = Length.Px(0),
                Top = Length.Px(-40), Bottom = Length.Px(0), PointerEvents = PointerEvents.None },
            Paint = DrawScrim,
        });
        // A tap that reaches the bar itself (the time text, the gaps between buttons) is a tap on the
        // picture; buttons and the progress bar stop their own clicks.
        _chrome.OnClick = OnSurfaceClick;

        _progress.Class = "miko-player-progress";
        // Positioned so that it paints above the scrim, which is positioned too.
        _progress.Style = new Style { Display = Display.Block, Position = Position.Relative, Height = Length.Px(28), FlexShrink = 0,
            MarginLeft = Length.Px(12), MarginRight = Length.Px(12) };
        _progress.Paint = DrawProgress;
        _progress.OnPointerDown = e =>
        {
            if (!Controller.CanSeek) return;
            _dragging = true;
            CancelAutoHide();
            Scrub(e); e.PreventDefault(); e.StopPropagation();
        };
        _progress.OnPointerMove = e => { if (_dragging) { Scrub(e); e.PreventDefault(); e.StopPropagation(); } };
        _progress.OnPointerUp = e =>
        {
            if (!_dragging) return;
            Scrub(e); _dragging = false;
            Seek(TimeSpan.FromSeconds(_scrub * Controller.Duration.TotalSeconds));
            ScheduleAutoHide();
            e.StopPropagation();
        };
        _progress.OnPointerCancel = _ => { _dragging = false; ScheduleAutoHide(); Repaint(); };
        _progress.OnClick = e => e.StopPropagation();
        _chrome.Children.Add(_progress);

        var controls = new DivElement { Class = "miko-player-controls", Style = new Style
        { Display = Display.Flex, Position = Position.Relative, Height = Length.Px(44), FlexShrink = 0, AlignItems = AlignItems.Center,
            PaddingLeft = Length.Px(4), PaddingRight = Length.Px(4), BoxSizing = BoxSizing.BorderBox } };
        controls.Children.Add(IconButton("Play / pause", () => Controller.State == VideoSessionState.Playing ? "pause" : "play", Controller.TogglePlayback));
        _time.Style = new Style { Display = Display.Block, FlexGrow = 1, MinWidth = Length.Px(0), FontSize = Length.Px(12), PaddingLeft = Length.Px(4) };
        controls.Children.Add(_time);
        controls.Children.Add(IconButton("Mute", () => Controller.Muted || Controller.Volume == 0 ? "volume-mute" : "volume-high", () => Controller.SetMuted(!Controller.Muted)));
        controls.Children.Add(IconButton("Danmaku", () => "chatbox-outline", () => SetDanmakuSettings(_timeline.Settings with { Enabled = !_timeline.Settings.Enabled })));
        controls.Children.Add(IconButton("Settings", () => "settings-outline", ToggleSettings));
        controls.Children.Add(IconButton("Fullscreen", () => IsFullscreen ? "contract" : "expand", ToggleFullscreen));
        _chrome.Children.Add(controls);
        _root.Children.Add(_chrome);
    }

    private void ApplyChromeStyle()
    {
        var style = new Style
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column, Position = Position.Absolute,
            Left = Length.Px(0), Right = Length.Px(0), Bottom = Length.Px(0),
            // Hidden controls take no taps: they reach the picture, which brings the controls back.
            Opacity = AreControlsVisible ? 1 : 0,
            PointerEvents = AreControlsVisible ? PointerEvents.Auto : PointerEvents.None,
            Transitions = FadeTransitions,
        };
        if (IsFullscreen)
        {
            // The picture runs under the camera cutout and the gesture bar; the controls do not.
            style.PaddingLeft = Length.SafeAreaInsetLeft + Length.Px(8);
            style.PaddingRight = Length.SafeAreaInsetRight + Length.Px(8);
            style.PaddingBottom = Length.SafeAreaInsetBottom + Length.Px(4);
        }
        _chrome.Style = style;
    }

    /// <summary>The "▸▸ 2x" pill shown at the top of the picture while a long press speeds playback up.</summary>
    private void BuildHint()
    {
        _hint.Class = "miko-player-speed-hint";
        ApplyHintStyle();
        var pill = new DivElement { Style = new Style
        {
            Display = Display.Flex, AlignItems = AlignItems.Center, Height = Length.Px(32), PaddingLeft = Length.Px(12),
            PaddingRight = Length.Px(14), BorderRadius = 16, BackgroundColor = Color.FromRgba(0, 0, 0, .55f), FontSize = Length.Px(13)
        } };
        pill.Children.Add(new CanvasElement { Style = new Style { Display = Display.Block, Width = Length.Px(18), Height = Length.Px(14),
            MarginRight = Length.Px(6), FlexShrink = 0 }, Paint = DrawFastForward });
        _hintText.Style = new Style { Display = Display.Block, WhiteSpace = WhiteSpace.Nowrap };
        pill.Children.Add(_hintText);
        _hint.Children.Add(pill);
        _root.Children.Add(_hint);
    }

    private void ApplyHintStyle() => _hint.Style = new Style
    {
        Display = IsLongPressActive ? Display.Flex : Display.None, Position = Position.Absolute, Left = Length.Px(0), Right = Length.Px(0),
        Top = IsFullscreen ? Length.SafeAreaInsetTop + Length.Px(16) : Length.Px(16),
        JustifyContent = JustifyContent.Center, PointerEvents = PointerEvents.None
    };

    private ButtonElement IconButton(string label, Func<string> icon, Action action)
    {
        var button = new ButtonElement { Class = "miko-player-button", Style = new Style
        { Display = Display.Block, Position = Position.Relative, Width = Length.Px(44), Height = Length.Px(44), FlexShrink = 0, BackgroundColor = Color.Transparent, Padding = Length.Px(0), BorderWidth = Length.Px(0) } };
        button.Children.Add(new CanvasElement { Style = Fill(), Paint = (c, r) => _icons.Draw(c, r, icon()) });
        // Using a control restarts the hide timer first: the action may hide the controls itself
        // (the settings drawer replaces them).
        button.OnClick = e => { e.StopPropagation(); ShowControls(); action(); Repaint(); };
        button.OnMouseEnter = _ => { _tooltip.TextContent = label; ChangeStyle(_tooltip, s => s.Display = Display.Block); };
        button.OnMouseLeave = _ => ChangeStyle(_tooltip, s => s.Display = Display.None);
        return button;
    }

    private void Scrub(PointerEventArgs e)
    {
        _scrub = Math.Clamp(e.OffsetX / Math.Max(1, e.TargetWidth), 0, 1);
        Repaint();
    }
    private void Refresh()
    {
        if (_disposed) return;
        _refreshing = true;
        try { Controller.Refresh(); }
        finally { _refreshing = false; }
    }

    private void UpdateControls()
    {
        if (_disposed) return;
        long now = Environment.TickCount64;
        if (!_refreshing || Controller.State != VideoSessionState.Playing || now - _lastTimeUpdate >= 250 || Controller.State != _lastState || _time.TextContent == null)
        {
            string duration = Controller.Duration > TimeSpan.Zero ? FormatTime(Controller.Duration) : "LIVE";
            string text = $"{FormatTime(Controller.Position)} / {duration}";
            if (_time.TextContent != text) _time.TextContent = text;
            _lastTimeUpdate = now;
        }
        bool hasError = Controller.Error != null;
        var display = hasError ? Display.Flex : Display.None;
        if (_error.Style!.Display.ValueOrNull() != display)
        {
            ChangeStyle(_error, s => s.Display = display);
        }
        if (hasError && _errorText.TextContent != Controller.Error) _errorText.TextContent = Controller.Error;
        if (Controller.State != _lastState)
        {
            _lastState = Controller.State;
            OnPlaybackStateChanged();
        }
        if (!_refreshing) Repaint();
    }
    public static string FormatTime(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
        : $"{(int)value.TotalMinutes}:{value.Seconds:00}";
    private static string FormatRate(float rate) => rate.ToString("0.##", CultureInfo.InvariantCulture) + "x";
    private void Repaint() => Engine?.RequestRepaint();
    private static void ChangeStyle(Element element, Action<Style> update)
    {
        var style = new Style();
        if (element.Style != null) style.Merge(element.Style);
        update(style);
        element.Style = style;
    }

    private void DrawOverlay(IDrawingContext context, RectF bounds)
    {
        if (_disposed) return;
        _danmakuPainter.Paint(context, bounds, _timeline, Controller.Position);
        if (!Controller.IsBuffering) return;
        float x = bounds.X + bounds.Width / 2, y = bounds.Y + bounds.Height / 2;
        context.StrokeArc(new RectF(x - 14, y - 14, 28, 28), (Environment.TickCount64 % 1000) * .36f, 270, Color.White, 3);
    }
    private void DrawProgress(IDrawingContext context, RectF bounds)
    {
        float y = bounds.Y + bounds.Height / 2;
        context.FillRect(new RectF(bounds.X, y - 2, bounds.Width, 4), TrackColor);
        double duration = Controller.Duration.TotalSeconds;
        if (duration <= 0) return;
        foreach (var range in Controller.BufferedRanges)
        {
            float left = (float)Math.Clamp(range.Start.TotalSeconds / duration, 0, 1);
            float right = (float)Math.Clamp(range.End.TotalSeconds / duration, 0, 1);
            context.FillRect(new RectF(bounds.X + left * bounds.Width, y - 2, Math.Max(0, right - left) * bounds.Width, 4), BufferedColor);
        }
        float progress = _dragging ? _scrub : (float)Math.Clamp(Controller.Position.TotalSeconds / duration, 0, 1);
        context.FillRect(new RectF(bounds.X, y - 2, progress * bounds.Width, 4), ProgressColor);
        context.FillCircle(bounds.X + progress * bounds.Width, y, 5, ProgressColor);
    }

    /// <summary>
    /// Black from transparent at the top to 60% at the bottom, eased in. Two-pixel bands: the drawing
    /// API has no gradients, and the scrim is a few dozen rectangles.
    /// </summary>
    private static void DrawScrim(IDrawingContext context, RectF bounds)
    {
        const float band = 2;
        for (float y = 0; y < bounds.Height; y += band)
        {
            float t = (y + band / 2) / bounds.Height;
            var alpha = (byte)(t * t * 153);
            if (alpha == 0) continue;
            context.FillRect(new RectF(bounds.X, bounds.Y + y, bounds.Width, Math.Min(band, bounds.Height - y)), new Color(0, 0, 0, alpha));
        }
    }

    private static void DrawFastForward(IDrawingContext context, RectF bounds)
    {
        float top = bounds.Y + 2, bottom = bounds.Bottom - 2, middle = bounds.Y + bounds.Height / 2;
        for (int i = 0; i < 2; i++)
        {
            float x = bounds.X + 2 + i * 7;
            context.StrokeLine(x, top, x + 5, middle, Color.White, 2);
            context.StrokeLine(x + 5, middle, x, bottom, Color.White, 2);
        }
    }

    protected override void OnDispose() => Dispose();
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Leaving the page while fullscreen must not leave the screen locked landscape and awake.
        ReleaseFullscreenPlatform();
        _settingsBack?.Dispose();
        _settingsBack = null;
        DisposeTimers();
        _video.SessionChanged -= Controller.Attach;
        _video.PlaybackEvent -= Controller.HandleEvent;
        _video.PlaybackUpdated -= Refresh;
        Controller.Changed -= UpdateControls;
        Controller.Dispose();
        _danmakuPainter.Clear();
        _icons.Dispose();
    }
}
