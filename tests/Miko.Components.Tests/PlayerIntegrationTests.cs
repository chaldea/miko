using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components.Player;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using Miko.Platform.Video;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Components.Tests;

public class PlayerIntegrationTests : IDisposable
{
    private readonly SKBitmap _bitmap = new(960, 760);
    private readonly SKCanvas _canvas;
    private readonly MikoEngine _engine;
    private readonly Backend _backend = new();
    private readonly MikoPlayer _player;
    private readonly DivElement _root;
    public PlayerIntegrationTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _engine = new MikoEngineBuilder().Build();
        _engine.VideoBackend = _backend;
        _player = new MikoPlayer { Engine = _engine, Source = "movie.mp4" };
        _root = new DivElement { Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100) } };
        _root.Children.Add(_player.Build());
        _engine.Initialize(_root, [], _canvas, 960, 760);
        Render();
    }
    private void Render() { for (int i = 0; i < 4; i++) _engine.Render(_canvas); }
    public void Dispose() { _player.Dispose(); _engine.DisposeVideoSessions(); _canvas.Dispose(); _bitmap.Dispose(); }

    [Fact]
    public void Should_Retain_Session_While_Using_Controls_And_Settings()
    {
        var video = _root.FindByTagName("video").Single();
        _player.Play(); _player.SetMuted(true); _player.SetVolume(.2f); _player.SetPlaybackRate(1.5f);
        _player.ToggleSettings(); Render(); _player.ToggleSettings();
        _player.ToggleFullscreen(); Render(); _player.ToggleFullscreen(); Render();
        _backend.Sessions.Count.ShouldBe(1);
        _root.FindByTagName("video").Single().ShouldBeSameAs(video);
        _backend.Sessions[0].Volume.ShouldBe(.2f);
        _backend.Sessions[0].Rate.ShouldBe(1.5f);
        _backend.Sessions[0].Muted.ShouldBeTrue();
    }

    [Fact]
    public void Should_Resize_Without_Clipping_Controls_At_Phone_Width()
    {
        _engine.SetViewportSize(360, 740); Render();
        _root.FindByClass("miko-player-viewport").Single().OffsetHeight.ShouldBe(202.5f, .5f);
        var controls = _root.FindByClass("miko-player-controls").Single();
        controls.OffsetWidth.ShouldBe(360, .5f);
        controls.Children.Sum(c => c.OffsetWidth).ShouldBeLessThanOrEqualTo(360);
        controls.FindByTagName("button").ShouldAllBe(x => x.OffsetWidth == 44 && x.OffsetHeight == 44);
        _player.ToggleSettings(); Render();
        var speed = (SelectElement)_root.FindByTagName("select").Single();
        speed.GetDisplayText().ShouldBe("1x");
    }

    [Fact]
    public void Should_Fill_Viewport_Inside_A_Padded_Parent()
    {
        _root.Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100),
            Padding = Length.Px(20), BoxSizing = BoxSizing.BorderBox };
        _engine.SetViewportSize(360, 740); Render();
        _player.ToggleFullscreen(); Render();
        _root.FindByClass("miko-player").Single().OffsetWidth.ShouldBe(360, .5f);
        _root.FindByClass("miko-player").Single().OffsetHeight.ShouldBe(740, .5f);
    }

    [Fact]
    public void Should_Seek_From_Touch_And_Cancel_Without_Seeking()
    {
        var progress = _root.FindByClass("miko-player-progress").Single();
        var input = new PointerEventArgs { Target = progress, OffsetX = 180, TargetWidth = 360, PointerType = PointerType.Touch };
        progress.OnPointerDown!(input); progress.OnPointerUp!(input);
        _backend.Sessions[0].Position.ShouldBe(TimeSpan.FromSeconds(30));
        var cancel = new PointerEventArgs { Target = progress, OffsetX = 300, TargetWidth = 360 };
        progress.OnPointerDown!(cancel); progress.OnPointerCancel!(cancel);
        _backend.Sessions[0].Position.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Should_Drag_Progress_Through_Platform_Input_And_Update_Paused_Time()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var input = new MikoInteractionController(
            Options.Create(new MikoAppOptions()), services, _engine, new EventDispatcher(), new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance), NullLogger<MikoInteractionController>.Instance);
        _player.Pause(); Render();
        var progress = _root.FindByClass("miko-player-progress").Single();
        var box = FindBox(_engine.GetCurrentLayout()!, progress)!;
        float y = box.BoxModel.BorderBox.Y + progress.OffsetHeight / 2;
        input.OnPointerDown(progress.OffsetWidth * .2f, y, MouseButton.Left, PointerType.Touch, 0);
        Render();
        input.OnPointerMove(progress.OffsetWidth * .75f, y);
        Render();
        input.OnPointerUp(progress.OffsetWidth * .75f, y, MouseButton.Left, PointerType.Touch, 0);
        _backend.Sessions.Single().Position.ShouldBe(TimeSpan.FromSeconds(45));
        _root.FindByClass("miko-player-controls").Single().Children[1].TextContent.ShouldBe("0:45 / 1:00");
    }

    private static Miko.Layout.LayoutBox? FindBox(Miko.Layout.LayoutBox box, Element element)
    {
        if (box.Element == element) return box;
        foreach (var child in box.Children)
            if (FindBox(child, element) is { } found) return found;
        return null;
    }

    [Fact]
    public void Should_Select_Speed_When_Dropdown_Overlaps_A_Slider()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var input = new MikoInteractionController(
            Options.Create(new MikoAppOptions()), services, _engine, new EventDispatcher(), new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance), NullLogger<MikoInteractionController>.Instance);
        _engine.SetViewportSize(360, 900);
        _player.ToggleSettings(); Render();
        var select = (SelectElement)_root.FindByTagName("select").Single();
        var bounds = FindBox(_engine.GetCurrentLayout()!, select)!.BoxModel.BorderBox;
        float x = bounds.X + bounds.Width / 2;
        _engine.HitTest(x, bounds.Y + bounds.Height / 2).ShouldBeSameAs(select);
        void Tap(float y)
        {
            input.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);
            input.OnPointerUp(x, y, MouseButton.Left, PointerType.Touch);
        }
        Tap(bounds.Y + bounds.Height / 2); Render();
        select.IsOpen.ShouldBeTrue();
        Tap(bounds.Bottom + 22 * 5.5f); Render();
        select.IsOpen.ShouldBeFalse();
        _player.Controller.PlaybackRate.ShouldBe(2);
        _backend.Sessions.Single().Rate.ShouldBe(2);
    }

    [Fact]
    public void Should_Replace_Changed_Source_And_Ignore_Queued_Old_Error()
    {
        var old = _backend.Sessions[0];
        old.Raise(new VideoSessionEvent.Error("old source", null));
        _player.Source = "https://example.com/video.m3u8?token=sample";
        _player.Build(); Render();
        old.Disposed.ShouldBeTrue();
        _backend.Sources.Last().IsHls.ShouldBeTrue();
        _player.Controller.Error.ShouldBeNull();
        _backend.Sessions.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_Replace_Session_When_Only_Mime_Type_Changes()
    {
        _player.Pause(); Render();
        var video = (VideoElement)_root.FindByTagName("video").Single();
        video.MimeType = "application/vnd.apple.mpegurl";
        _engine.HasPendingVisualWork.ShouldBeTrue();
        Render();
        _backend.Sessions.Count.ShouldBe(2);
        _backend.Sessions[0].Disposed.ShouldBeTrue();
        _backend.Sources.Last().IsHls.ShouldBeTrue();
    }

    [Fact]
    public void Should_Explain_Missing_Platform_Backend()
    {
        var engine = new MikoEngineBuilder().Build();
        using var player = new MikoPlayer { Engine = engine, Source = "movie.mp4" };
        player.Build();
        player.Controller.State.ShouldBe(VideoSessionState.Error);
        player.Controller.Error.ShouldNotBeNull().ShouldContain("backend");
    }

    [Fact]
    public async Task Should_Marshal_Background_Errors_And_Allow_Retry()
    {
        await Task.Run(() => _backend.Sessions[0].Raise(new VideoSessionEvent.Error("offline", null)));
        _player.Controller.Error.ShouldBeNull();
        Render();
        _player.Controller.Error.ShouldBe("offline");
        _player.Controller.State.ShouldBe(VideoSessionState.Error);
        _player.Retry(); Render();
        _backend.Sessions.Count.ShouldBe(2);
        _player.Controller.Error.ShouldBeNull();
    }

    [Fact]
    public void Should_Stop_Requesting_Frames_When_Paused_And_Release_On_Removal()
    {
        _player.Pause(); Render();
        _engine.HasPendingVisualWork.ShouldBeFalse();
        _root.Children.Clear(); Render();
        _backend.Sessions.Single().Disposed.ShouldBeTrue();
    }

    [Fact]
    public void Should_Register_Only_Player_Services_Without_Replacing_Options()
    {
        var services = new ServiceCollection();
        services.AddMikoPlayer(o => o.MaxVisibleDanmaku = 40);
        services.AddMikoPlayer();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<MikoPlayerOptions>().MaxVisibleDanmaku.ShouldBe(40);
        provider.GetService<IVideoBackend>().ShouldBeNull();
    }
    private sealed class Backend : IVideoBackend
    {
        public List<TestVideoSession> Sessions { get; } = [];
        public List<VideoSourceDescriptor> Sources { get; } = [];
        public VideoBackendCapabilities Capabilities { get; } = new(false, false, ["video/mp4", "application/vnd.apple.mpegurl"]);
        public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        { Sources.Add(source); var session = new TestVideoSession(); Sessions.Add(session); return session; }
    }
}
