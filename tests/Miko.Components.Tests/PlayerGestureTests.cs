using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components.Player;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform;
using Miko.Platform.Video;
using Miko.Styling;
using Shouldly;

namespace Miko.Components.Tests;

/// <summary>
/// The controls are an overlay on the picture (issues/feat-components-player.md, problems 2–4): a
/// tap shows or hides them, a double tap plays or pauses, they hide on their own during playback,
/// a long press plays faster until released, and settings open as a drawer on phones and a pop-up
/// on the desktop. Taps go through the real input controller wherever hit testing matters.
/// </summary>
public sealed class PlayerGestureTests : IDisposable
{
    private readonly RenderSurface _surface;
    private readonly IDrawingContext _canvas;
    private readonly MikoEngine _engine;
    private readonly Backend _backend = new();
    private readonly ManualClock _clock = new();
    private readonly RecordingHaptics _haptics = new();
    private readonly PlatformInfo _platform;
    private readonly MikoPlayer _player;
    private readonly DivElement _root;
    private readonly MikoInteractionController _input;
    private readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();

    public PlayerGestureTests() : this(HostPlatform.Android) { }

    private PlayerGestureTests(HostPlatform platform)
    {
        _engine = new MikoEngineBuilder().Build();
        _surface = _engine.Graphics.CreateSurface(360, 740);
        _canvas = _surface.Context;
        _engine.VideoBackend = _backend;
        _platform = new PlatformInfo(platform) { Haptics = _haptics };
        _player = new MikoPlayer { Engine = _engine, Source = "movie.mp4", Clock = _clock, PlatformService = _platform };
        _root = new DivElement { Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100) } };
        _root.Children.Add(_player.Build());
        _engine.Initialize(_root, [], _canvas, 360, 740);
        _input = new MikoInteractionController(
            Options.Create(new MikoAppOptions()), _services, _engine, new EventDispatcher(), new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance), NullLogger<MikoInteractionController>.Instance,
            platformService: _platform);
        Render();
    }

    public void Dispose()
    {
        _player.Dispose();
        _engine.DisposeVideoSessions();
        _surface.Dispose();
        _services.Dispose();
    }

    private void Render() { for (int i = 0; i < 3; i++) _engine.Render(_canvas); }

    /// <summary>Moves the clock; timer callbacks post to the engine's dispatcher, which a frame drains.</summary>
    private void Advance(int milliseconds)
    {
        _clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
        Render();
    }

    private TestVideoSession Session => _backend.Sessions.Single();
    private Element Single(string className) => _root.FindByClass(className).Single();

    /// <summary>The element's box in the current layout; null when it is not laid out (display: none).</summary>
    private LayoutBox? LayoutOf(Element element)
    {
        static LayoutBox? Find(LayoutBox box, Element element)
            => box.Element == element ? box : box.Children.Select(c => Find(c, element)).FirstOrDefault(b => b != null);
        return Find(_engine.GetCurrentLayout()!, element);
    }

    private RectF Box(Element element) => LayoutOf(element)!.BoxModel.BorderBox;

    private void Tap(float x, float y, PointerType type = PointerType.Touch)
    {
        _input.OnPointerDown(x, y, MouseButton.Left, type);
        _input.OnPointerUp(x, y, MouseButton.Left, type);
        Render();
    }

    /// <summary>The middle of the picture, above the control bar.</summary>
    private void TapPicture(PointerType type = PointerType.Touch)
    {
        var picture = Box(Single("miko-player-viewport"));
        Tap(picture.X + picture.Width / 2, picture.Y + picture.Height / 3, type);
    }

    private void StartPlaying()
    {
        _player.Play();
        Render();
        _player.Controller.State.ShouldBe(VideoSessionState.Playing);
    }

    private void LongPress()
    {
        var viewport = Single("miko-player-viewport");
        viewport.OnLongPress!(new PointerEventArgs { Target = viewport, PointerType = PointerType.Touch });
        Render();
    }

    private void Release()
    {
        var viewport = Single("miko-player-viewport");
        viewport.OnPointerUp!(new PointerEventArgs { Target = viewport, PointerType = PointerType.Touch });
        Render();
    }

    // ---- tap, double tap, auto-hide -------------------------------------------------------------

    [Fact]
    public void Should_toggle_the_controls_on_a_single_tap_once_the_double_tap_window_passes()
    {
        TapPicture();
        _player.AreControlsVisible.ShouldBeTrue("a single tap waits to see whether a second one follows");

        Advance(300);
        _player.AreControlsVisible.ShouldBeFalse();
        LayoutOf(Single("miko-player-chrome"))!.ComputedStyle.PointerEvents.ShouldBe(PointerEvents.None);

        TapPicture();
        Advance(300);
        _player.AreControlsVisible.ShouldBeTrue();
    }

    [Fact]
    public void Should_let_taps_through_hidden_controls_to_the_picture()
    {
        _player.HideControls();
        Render();
        var play = Box(Single("miko-player-controls").FindByTagName("button").First());
        float x = play.X + play.Width / 2, y = play.Y + play.Height / 2;

        _engine.HitTest(x, y).ShouldNotBeOfType<ButtonElement>();

        Tap(x, y);
        Advance(300);
        _player.AreControlsVisible.ShouldBeTrue("the tap reached the picture, which brings the controls back");
        _player.Controller.State.ShouldNotBe(VideoSessionState.Playing, "the hidden play button was not pressed");
    }

    [Fact]
    public void Should_treat_a_tap_on_the_blank_control_bar_as_a_tap_on_the_picture()
    {
        var time = Box(Single("miko-player-controls").Children[1]);
        Tap(time.X + 10, time.Y + time.Height / 2);
        Advance(300);
        _player.AreControlsVisible.ShouldBeFalse();
    }

    [Fact]
    public void Should_play_and_pause_on_a_double_tap_without_toggling_the_controls()
    {
        StartPlaying();
        _player.HideControls();
        Render();

        TapPicture();
        Advance(120);
        TapPicture();
        Advance(300);

        _player.Controller.State.ShouldBe(VideoSessionState.Paused);
        _player.AreControlsVisible.ShouldBeTrue("pausing brings the controls up");

        TapPicture();
        TapPicture();
        Advance(300);
        _player.Controller.State.ShouldBe(VideoSessionState.Playing);
        _player.AreControlsVisible.ShouldBeTrue("the double tap did not also count as a single tap");
    }

    [Fact]
    public void Should_treat_taps_further_apart_than_the_window_as_two_single_taps()
    {
        StartPlaying();
        TapPicture();
        Advance(350);
        TapPicture();
        Advance(300);

        _player.Controller.State.ShouldBe(VideoSessionState.Playing);
        _player.AreControlsVisible.ShouldBeTrue("hidden by the first tap, shown by the second");
    }

    [Fact]
    public void Should_hide_the_controls_five_seconds_into_playback()
    {
        StartPlaying();
        Advance(4900);
        _player.AreControlsVisible.ShouldBeTrue();
        Advance(100);
        _player.AreControlsVisible.ShouldBeFalse();
    }

    [Fact]
    public void Should_keep_the_controls_up_while_paused()
    {
        StartPlaying();
        _player.Pause();
        Render();
        Advance(10_000);
        _player.AreControlsVisible.ShouldBeTrue();
    }

    [Fact]
    public void Should_restart_the_hide_timer_when_a_control_is_used()
    {
        StartPlaying();
        Advance(4000);
        var mute = Box(Single("miko-player-controls").FindByTagName("button").ElementAt(1));
        Tap(mute.X + mute.Width / 2, mute.Y + mute.Height / 2);
        Session.Muted.ShouldBeTrue();

        Advance(4000);
        _player.AreControlsVisible.ShouldBeTrue("the button press restarted the five seconds");
        Advance(1000);
        _player.AreControlsVisible.ShouldBeFalse();
    }

    [Fact]
    public void Should_not_hide_the_controls_when_the_delay_is_zero()
    {
        _player.ControlsHideDelay = TimeSpan.Zero;
        _player.Build();
        StartPlaying();
        Advance(60_000);
        _player.AreControlsVisible.ShouldBeTrue();
    }

    [Fact]
    public void Should_play_and_pause_on_a_mouse_click_at_once()
    {
        TapPicture(PointerType.Mouse);
        _player.Controller.State.ShouldBe(VideoSessionState.Playing);
        TapPicture(PointerType.Mouse);
        _player.Controller.State.ShouldBe(VideoSessionState.Paused);
        _player.AreControlsVisible.ShouldBeTrue();
    }

    [Fact]
    public void Should_show_the_controls_when_the_mouse_moves_over_the_player()
    {
        StartPlaying();
        Advance(5000);
        _player.AreControlsVisible.ShouldBeFalse();

        var picture = Box(Single("miko-player-viewport"));
        _input.OnPointerMove(picture.X + 50, picture.Y + 50);
        Render();
        _player.AreControlsVisible.ShouldBeTrue();
        Advance(5000);
        _player.AreControlsVisible.ShouldBeFalse();
    }

    // ---- long press -----------------------------------------------------------------------------

    [Fact]
    public void Should_play_at_double_speed_while_long_pressed()
    {
        StartPlaying();
        _player.SetPlaybackRate(1.25f);

        LongPress();

        _player.IsLongPressActive.ShouldBeTrue();
        Session.Rate.ShouldBe(2);
        LayoutOf(Single("miko-player-speed-hint")).ShouldNotBeNull("the hint is laid out (display: flex)");
        _root.FindByClass("miko-player-speed-hint").Single().FindByTagName("span").Single().TextContent.ShouldBe("2x");
        _haptics.Impacts.ShouldBe([HapticImpactStyle.Light]);

        Release();

        _player.IsLongPressActive.ShouldBeFalse();
        Session.Rate.ShouldBe(1.25f);
        LayoutOf(Single("miko-player-speed-hint")).ShouldBeNull("display: none again");
        _haptics.Impacts.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_end_the_long_press_when_the_pointer_is_cancelled()
    {
        StartPlaying();
        LongPress();
        var viewport = Single("miko-player-viewport");
        viewport.OnPointerCancel!(new PointerEventArgs { Target = viewport, PointerType = PointerType.Touch });
        _player.IsLongPressActive.ShouldBeFalse();
        Session.Rate.ShouldBe(1);
    }

    [Fact]
    public void Should_keep_the_page_from_scrolling_while_long_pressed()
    {
        StartPlaying();
        LongPress();
        var viewport = Single("miko-player-viewport");
        var move = new PointerEventArgs { Target = viewport, PointerType = PointerType.Touch };
        viewport.OnPointerMove!(move);
        move.DefaultPrevented.ShouldBeTrue();
    }

    [Fact]
    public void Should_ignore_a_long_press_while_paused()
    {
        LongPress();
        _player.IsLongPressActive.ShouldBeFalse();
        _haptics.Impacts.ShouldBeEmpty();
    }

    [Fact]
    public void Should_apply_a_rate_set_during_the_long_press_when_it_ends()
    {
        StartPlaying();
        LongPress();
        _player.PlaybackRate = 1.5f;
        _player.Build();
        Session.Rate.ShouldBe(2, "the press still holds its rate");

        Release();
        Session.Rate.ShouldBe(1.5f);
    }

    [Fact]
    public void Should_not_long_press_when_disabled()
    {
        _player.LongPressPlaybackRate = 0;
        _player.Build();
        StartPlaying();
        LongPress();
        _player.IsLongPressActive.ShouldBeFalse();
        Session.Rate.ShouldBe(1);
    }

    // ---- settings -------------------------------------------------------------------------------

    [Fact]
    public void Should_open_settings_as_a_drawer_on_the_right_on_a_phone()
    {
        _player.ToggleFullscreen();
        _engine.SetViewportSize(800, 360);
        _engine.SetSafeAreaInsets(new SafeAreaInsets(34, 0, 0, 20));
        Render();
        _player.ToggleSettings();
        Render();

        var screen = Box(Single("miko-player"));
        var drawer = Box(Single("miko-player-settings"));
        drawer.Right.ShouldBe(screen.Right, .5f);
        drawer.Top.ShouldBe(screen.Top, .5f);
        drawer.Height.ShouldBe(screen.Height, .5f);
        drawer.Width.ShouldBe(300, .5f);
        _player.AreControlsVisible.ShouldBeFalse("the drawer replaces the controls");
        // The rows are touch-sized.
        _root.FindByClass("miko-player-setting").ShouldAllBe(row => row.OffsetHeight >= 44);
    }

    [Fact]
    public void Should_close_the_drawer_on_a_tap_on_the_picture_without_other_effects()
    {
        StartPlaying();
        _player.ToggleSettings();
        Render();

        Tap(20, 60);
        Advance(300);

        _player.IsSettingsOpen.ShouldBeFalse();
        _player.Controller.State.ShouldBe(VideoSessionState.Playing);
        _player.AreControlsVisible.ShouldBeFalse("the tap only closed the panel");
    }

    [Fact]
    public void Should_not_bring_the_controls_back_behind_an_open_drawer()
    {
        StartPlaying();
        _player.ToggleSettings();
        Render();
        _player.Pause();
        Render();
        _player.AreControlsVisible.ShouldBeFalse();
    }

    [Fact]
    public void Should_close_settings_on_back_before_leaving_fullscreen()
    {
        _player.ToggleFullscreen();
        _player.ToggleSettings();

        _platform.Back.Dispatch().ShouldBeTrue();
        _player.IsSettingsOpen.ShouldBeFalse();
        _player.IsFullscreen.ShouldBeTrue();

        _platform.Back.Dispatch().ShouldBeTrue();
        _player.IsFullscreen.ShouldBeFalse();
        _platform.Back.Count.ShouldBe(0);
    }

    [Fact]
    public void Should_not_hide_the_controls_while_settings_are_open()
    {
        using var desktop = new PlayerGestureTests(HostPlatform.Windows);
        desktop.StartPlaying();
        desktop._player.ToggleSettings();
        desktop.Advance(10_000);
        desktop._player.AreControlsVisible.ShouldBeTrue();

        desktop._player.ToggleSettings();
        desktop.Advance(5000);
        desktop._player.AreControlsVisible.ShouldBeFalse("closing the panel started the timer again");
    }

    [Fact]
    public void Should_open_settings_as_a_pop_up_above_the_button_on_the_desktop()
    {
        using var desktop = new PlayerGestureTests(HostPlatform.Windows);
        desktop._engine.SetViewportSize(960, 740);
        desktop.Render();
        desktop._player.ToggleSettings();
        desktop.Render();

        var player = desktop.Box(desktop.Single("miko-player"));
        var panel = desktop.Box(desktop.Single("miko-player-settings-panel"));
        var bar = desktop.Box(desktop.Single("miko-player-controls"));
        var progress = desktop.Box(desktop.Single("miko-player-progress"));
        panel.Bottom.ShouldBeLessThanOrEqualTo(progress.Top, "above the control bar");
        panel.Top.ShouldBeGreaterThan(player.Top + 8, "as tall as its content, not the whole player");
        panel.Right.ShouldBe(player.Right - 12, .5f);
        panel.Width.ShouldBe(260, .5f);
        desktop._player.AreControlsVisible.ShouldBeTrue("the pop-up is anchored to the controls");
        desktop._root.FindByClass("miko-player-setting").ShouldAllBe(row => row.OffsetHeight >= 36);
        bar.Bottom.ShouldBe(player.Bottom, .5f);

        // The area around the pop-up belongs to the picture: a click there closes the panel.
        desktop.Tap(player.X + 40, panel.Y + 10, PointerType.Mouse);
        desktop._player.IsSettingsOpen.ShouldBeFalse();
        desktop._player.Controller.State.ShouldNotBe(VideoSessionState.Playing);
    }

    [Fact]
    public void Should_toggle_loop_from_its_switch_row()
    {
        _player.ToggleSettings();
        Render();
        var row = _root.FindByClass("miko-player-setting").Single(r => r.Children[0].TextContent == "Loop");
        var box = Box(row);

        Tap(box.X + 20, box.Y + box.Height / 2);

        _player.Controller.Loop.ShouldBeTrue();
        _player.IsSettingsOpen.ShouldBeTrue();
    }

    [Fact]
    public void Should_drag_the_volume_slider_without_scrolling_the_panel()
    {
        _player.ToggleSettings();
        Render();
        var row = _root.FindByClass("miko-player-setting").Single(r => r.Children[0].TextContent == "Volume");
        var slider = Box(row.FindByClass("miko-player-slider").Single());
        float y = slider.Y + slider.Height / 2;

        _input.OnPointerDown(slider.Right - 2, y, MouseButton.Left, PointerType.Touch);
        Render();
        _input.OnPointerMove(slider.X + slider.Width / 2, y + 1);
        Render();
        _input.OnPointerUp(slider.X + slider.Width / 2, y + 1, MouseButton.Left, PointerType.Touch);
        Render();

        _player.Controller.Volume.ShouldBe(.5f, .02f);
        Session.Volume.ShouldBe(.5f, .02f);
        row.Children[^1].TextContent.ShouldBe("50%");
        LayoutOf(Single("miko-player-settings-panel"))!.ScrollTop.ShouldBe(0);
    }

    // ---- fullscreen layout ----------------------------------------------------------------------

    [Fact]
    public void Should_fill_the_screen_and_keep_the_controls_clear_of_the_cutout_in_fullscreen()
    {
        _player.ToggleFullscreen();
        _engine.SetViewportSize(800, 360);
        _engine.SetSafeAreaInsets(new SafeAreaInsets(34, 0, 0, 20));
        Render();

        // The picture covers the whole screen — the cutout strip included, no safe-area padding.
        Box(Single("miko-player-viewport")).ShouldBe(new RectF(0, 0, 800, 360));
        // The controls keep clear of it, and of the gesture bar at the bottom.
        var play = Box(Single("miko-player-controls").FindByTagName("button").First());
        play.X.ShouldBeGreaterThanOrEqualTo(34 + 8);
        play.Bottom.ShouldBeLessThanOrEqualTo(360 - 20);
        Box(Single("miko-player-progress")).X.ShouldBeGreaterThanOrEqualTo(34 + 8);
    }

    private sealed class Backend : IVideoBackend
    {
        public List<TestVideoSession> Sessions { get; } = [];
        public VideoBackendCapabilities Capabilities { get; } = new(false, false, ["video/mp4"]);
        public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        { var session = new TestVideoSession(); Sessions.Add(session); return session; }
    }

    private sealed class RecordingHaptics : IHapticFeedback
    {
        public List<HapticImpactStyle> Impacts { get; } = [];
        public void SelectionStart() { }
        public void SelectionChanged() { }
        public void SelectionEnd() { }
        public void Impact(HapticImpactStyle style = HapticImpactStyle.Medium) => Impacts.Add(style);
    }
}
