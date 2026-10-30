using System.Globalization;
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
public sealed class MikoPlayer : ComponentBase, IDisposable
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
    [Inject] public MikoEngine? Engine { get; set; }
    [Inject] public MikoPlayerOptions Options { get; set; } = new();

    /// <summary>
    /// Component-facing platform service: landscape lock, keep-awake and the system back button
    /// while fullscreen (ISSUE-149). Null without a service scope; fullscreen then stays a pure
    /// viewport layout, as before.
    /// </summary>
    [Inject] public IPlatformService? PlatformService { get; set; }

    public PlayerController Controller { get; } = new();
    public bool IsFullscreen { get; private set; }
    public bool IsSettingsOpen { get; private set; }

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
    private readonly CanvasElement _progress = new();
    private readonly DivElement _settings = new();
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
        if (_lastRate != PlaybackRate) { Controller.SetPlaybackRate(PlaybackRate); _lastRate = PlaybackRate; }
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
    public void Retry() { Controller.Reset(AutoPlay); _video.Reload(); Repaint(); }

    public void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        if (IsFullscreen) AcquireFullscreenPlatform();
        else ReleaseFullscreenPlatform();
        ApplyRootStyle();
        ApplyViewportStyle();
        Repaint();
        FullscreenChanged?.Invoke();
    }

    /// <summary>
    /// Fullscreen on a phone means landscape with the screen kept on, and the system back button
    /// (Android back, iOS edge swipe, desktop Escape) leaving fullscreen before it leaves the page.
    /// The back slot uses overlay priority so it outranks the page's back button.
    /// </summary>
    private void AcquireFullscreenPlatform()
    {
        if (_fullscreenPlatform is not null || PlatformService is not { } platform) return;

        _fullscreenPlatform = platform;
        platform.Orientation.Lock(ScreenOrientation.Landscape);
        platform.Orientation.KeepAwake();
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
    }

    public void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
        if (IsSettingsOpen) BuildSettings();
        ChangeStyle(_settings, s => s.Display = IsSettingsOpen ? Display.Flex : Display.None);
        Repaint();
    }

    private void ApplyRootStyle()
    {
        var style = new Style
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Length.Percent(100),
            MinWidth = Length.Px(0), BackgroundColor = Color.FromHex("#151719"), Color = Color.White,
            FontFamily = "Arial", FontSize = Length.Px(14), Position = Position.Relative, BoxSizing = BoxSizing.BorderBox
        };
        if (Style != null) style.Merge(Style);
        if (IsFullscreen)
        {
            style.Position = Position.Fixed;
            style.Top = Length.Px(0); style.Left = Length.Px(0);
            style.Width = Length.Vw(100); style.Height = Length.Vh(100);
            style.MaxWidth = null; style.MaxHeight = null; style.Margin = Length.Px(0);
            style.PaddingTop = Length.SafeAreaInsetTop;
            style.PaddingBottom = Length.SafeAreaInsetBottom;
            style.PaddingLeft = Length.SafeAreaInsetLeft;
            style.PaddingRight = Length.SafeAreaInsetRight;
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
        _viewport.Class = "miko-player-viewport";
        ApplyViewportStyle();
        _video.Style = Fill();
        _overlay.Style = Fill();
        _overlay.Paint = DrawOverlay;
        _overlay.OnClick = _ => Controller.TogglePlayback();
        _viewport.Children.Add(_video);
        _viewport.Children.Add(_overlay);
        _error.Style = new Style { Display = Display.None, Position = Position.Absolute, Top = Length.Px(12), Left = Length.Px(12),
            Right = Length.Px(12), FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center, BackgroundColor = Color.FromHex("#292c30"), Padding = Length.Px(12) };
        _errorText.Style = new Style { Display = Display.Block, Width = Length.Percent(100), FontSize = Length.Px(13) };
        _error.Children.Add(_errorText);
        _error.Children.Add(IconButton("Retry", () => "refresh", Retry));
        _viewport.Children.Add(_error);
        _root.Children.Add(_viewport);

        _progress.Class = "miko-player-progress";
        _progress.Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Px(28), FlexShrink = 0 };
        _progress.Paint = DrawProgress;
        _progress.OnPointerDown = e => { if (!Controller.CanSeek) return; _dragging = true; Scrub(e); e.PreventDefault(); e.StopPropagation(); };
        _progress.OnPointerMove = e => { if (_dragging) { Scrub(e); e.PreventDefault(); e.StopPropagation(); } };
        _progress.OnPointerUp = e => { if (_dragging) { Scrub(e); _dragging = false; Seek(TimeSpan.FromSeconds(_scrub * Controller.Duration.TotalSeconds)); e.StopPropagation(); } };
        _progress.OnPointerCancel = _ => { _dragging = false; Repaint(); };
        _root.Children.Add(_progress);

        var controls = new DivElement { Class = "miko-player-controls", Style = new Style
        { Display = Display.Flex, Width = Length.Percent(100), Height = Length.Px(48), FlexShrink = 0, AlignItems = AlignItems.Center, PaddingLeft = Length.Px(4), PaddingRight = Length.Px(4), BoxSizing = BoxSizing.BorderBox } };
        controls.Children.Add(IconButton("Play / pause", () => Controller.State == VideoSessionState.Playing ? "pause" : "play", Controller.TogglePlayback));
        _time.Style = new Style { Display = Display.Block, FlexGrow = 1, MinWidth = Length.Px(0), FontSize = Length.Px(12), PaddingLeft = Length.Px(4) };
        controls.Children.Add(_time);
        controls.Children.Add(IconButton("Mute", () => Controller.Muted || Controller.Volume == 0 ? "volume-mute" : "volume-high", () => Controller.SetMuted(!Controller.Muted)));
        controls.Children.Add(IconButton("Danmaku", () => "chatbox-outline", () => SetDanmakuSettings(_timeline.Settings with { Enabled = !_timeline.Settings.Enabled })));
        controls.Children.Add(IconButton("Settings", () => "settings-outline", ToggleSettings));
        controls.Children.Add(IconButton("Fullscreen", () => IsFullscreen ? "contract" : "expand", ToggleFullscreen));
        _root.Children.Add(controls);
        _settings.Class = "miko-player-settings";
        _settings.Style = new Style { Display = Display.None, FlexDirection = FlexDirection.Column, Width = Length.Percent(100), MaxHeight = Length.Px(250),
            OverflowY = Overflow.Auto, BackgroundColor = Color.FromHex("#25282c"), Padding = Length.Px(12), FlexShrink = 0, BoxSizing = BoxSizing.BorderBox };
        _root.Children.Add(_settings);
        _tooltip.Style = new Style { Display = Display.None, Position = Position.Absolute, Bottom = Length.Px(80), Right = Length.Px(12),
            BackgroundColor = Color.FromHex("#34383d"), Color = Color.White, Padding = Length.Px(6), FontSize = Length.Px(12), PointerEvents = PointerEvents.None };
        _root.Children.Add(_tooltip);
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
                // With a platform service, Escape reaches the back-button chain first: it leaves
                // fullscreen there and is swallowed. This branch only serves hosts without one.
                case "Escape": if (IsFullscreen && _fullscreenPlatform is null) ToggleFullscreen(); if (IsSettingsOpen) ToggleSettings(); break;
                default: return;
            }
            e.PreventDefault(); e.StopPropagation();
        };
    }

    private ButtonElement IconButton(string label, Func<string> icon, Action action)
    {
        var button = new ButtonElement { Class = "miko-player-button", Style = new Style
        { Display = Display.Block, Position = Position.Relative, Width = Length.Px(44), Height = Length.Px(44), FlexShrink = 0, BackgroundColor = Color.Transparent, Padding = Length.Px(0), BorderWidth = Length.Px(0) } };
        button.Children.Add(new CanvasElement { Style = Fill(), Paint = (c, r) => _icons.Draw(c, r, icon()) });
        button.OnClick = e => { e.StopPropagation(); action(); Repaint(); };
        button.OnMouseEnter = _ => { _tooltip.TextContent = label; ChangeStyle(_tooltip, s => s.Display = Display.Block); };
        button.OnMouseLeave = _ => ChangeStyle(_tooltip, s => s.Display = Display.None);
        return button;
    }

    private void BuildSettings()
    {
        _settings.Children.Clear();
        AddSlider("Volume", Controller.Volume, 0, 1, Controller.SetVolume);
        AddChoice("Speed", ["0.5", "0.75", "1", "1.25", "1.5", "2"], Controller.PlaybackRate.ToString(CultureInfo.InvariantCulture),
            value => Controller.SetPlaybackRate(float.Parse(value, CultureInfo.InvariantCulture)));
        AddToggle("Loop", Controller.Loop, Controller.SetLoop);
        AddToggle("Danmaku", _timeline.Settings.Enabled, value => SetDanmakuSettings(_timeline.Settings with { Enabled = value }));
        AddSlider("Opacity", _timeline.Settings.Opacity, 0, 1, value => SetDanmakuSettings(_timeline.Settings with { Opacity = value }));
        AddSlider("Text size", _timeline.Settings.FontSize, 12, 48, value => SetDanmakuSettings(_timeline.Settings with { FontSize = value }));
        AddSlider("Travel time", _timeline.Settings.Duration, 2, 20, value => SetDanmakuSettings(_timeline.Settings with { Duration = value }));
        AddSlider("Area", _timeline.Settings.Area, .2f, 1, value => SetDanmakuSettings(_timeline.Settings with { Area = value }));
        AddSlider("Density", _timeline.Settings.MaxVisible, 1, Options.MaxVisibleDanmaku,
            value => SetDanmakuSettings(_timeline.Settings with { MaxVisible = (int)value }));
    }

    private DivElement SettingRow(string label)
    {
        var row = new DivElement { Style = new Style { Display = Display.Flex, AlignItems = AlignItems.Center, MinHeight = Length.Px(44), Width = Length.Percent(100), FlexShrink = 0 } };
        row.Children.Add(new SpanElement { TextContent = label, Style = new Style { Display = Display.Block, Width = Length.Px(100), FlexShrink = 0 } });
        _settings.Children.Add(row);
        return row;
    }
    private void AddSlider(string label, float value, float min, float max, Action<float> change)
    {
        var input = new InputElement { Type = InputType.Range, Min = min, Max = max, NumericValue = value,
            Style = new Style { Display = Display.Block, FlexGrow = 1, FlexBasis = Length.Px(0), MinWidth = Length.Px(0), Height = Length.Px(36) } };
        input.OnInput = _ => { change(input.NumericValue); Repaint(); };
        input.OnChange = _ => { change(input.NumericValue); Repaint(); };
        SettingRow(label).Children.Add(input);
    }
    private void AddToggle(string label, bool value, Action<bool> change)
    {
        var input = new InputElement { Type = InputType.Checkbox, Checked = value,
            Style = new Style { Display = Display.Block, Width = Length.Px(24), Height = Length.Px(24) } };
        input.OnChange = _ => { change(input.Checked); Repaint(); };
        SettingRow(label).Children.Add(input);
    }
    private void AddChoice(string label, string[] values, string value, Action<string> change)
    {
        var input = new SelectElement { Value = value, Style = new Style { Display = Display.Block, FlexGrow = 1, MinWidth = Length.Px(0), Height = Length.Px(36), Color = Color.Black, FontSize = Length.Px(14) } };
        foreach (var option in values)
            input.Children.Add(new OptionElement { Value = option, TextContent = option + "x", Style = new Style { Display = Display.None } });
        input.Value = value;
        input.OnChange = _ => change(input.Value ?? "1");
        SettingRow(label).Children.Add(input);
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

    /// <summary>
    /// Inline: 16:9 of the player width, resolved by layout through <c>aspect-ratio</c> (ISSUE-150).
    /// The height used to be written back as pixels once a frame had measured the width, so opening a
    /// page laid out twice and every resize lagged one frame behind. Fullscreen: fill the height the
    /// controls leave free.
    /// </summary>
    private void ApplyViewportStyle()
    {
        var style = new Style
        {
            Display = Display.Block, Position = Position.Relative, Width = Length.Percent(100), MinHeight = Length.Px(100),
            BackgroundColor = Color.Black, Overflow = Overflow.Hidden
        };
        if (IsFullscreen) { style.FlexGrow = 1; style.Height = Length.Px(0); }
        else style.AspectRatio = new AspectRatio(16, 9);
        _viewport.Style = style;
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
        _lastState = Controller.State;
        if (!_refreshing) Repaint();
    }
    public static string FormatTime(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
        : $"{(int)value.TotalMinutes}:{value.Seconds:00}";
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

    protected override void OnDispose() => Dispose();
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Leaving the page while fullscreen must not leave the screen locked landscape and awake.
        ReleaseFullscreenPlatform();
        _video.SessionChanged -= Controller.Attach;
        _video.PlaybackEvent -= Controller.HandleEvent;
        _video.PlaybackUpdated -= Refresh;
        Controller.Changed -= UpdateControls;
        Controller.Dispose();
        _danmakuPainter.Clear();
        _icons.Dispose();
    }
}
