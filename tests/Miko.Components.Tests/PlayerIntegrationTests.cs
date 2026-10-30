using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components.Player;
using Miko.Components.Player.Danmaku;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform;
using Miko.Platform.Video;
using Miko.Styling;
using Miko.Graphics;
using Shouldly;

namespace Miko.Components.Tests;

public class PlayerIntegrationTests : IDisposable
{
    private readonly RenderSurface _surface;
    private readonly IDrawingContext _canvas;
    private readonly MikoEngine _engine;
    private readonly Backend _backend = new();
    private readonly MikoPlayer _player;
    private readonly DivElement _root;
    public PlayerIntegrationTests()
    {
        _engine = new MikoEngineBuilder().Build();
        _surface = _engine.Graphics.CreateSurface(960, 760);
        _canvas = _surface.Context;
        _engine.VideoBackend = _backend;
        _player = new MikoPlayer { Engine = _engine, Source = "movie.mp4" };
        _root = new DivElement { Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100) } };
        _root.Children.Add(_player.Build());
        _engine.Initialize(_root, [], _canvas, 960, 760);
        Render();
    }
    private void Render() { for (int i = 0; i < 4; i++) _engine.Render(_canvas); }
    public void Dispose() { _player.Dispose(); _engine.DisposeVideoSessions(); _surface.Dispose(); }

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
    public void Should_Size_Viewport_To_16_By_9_On_The_First_Layout()
    {
        // The height used to be written back as pixels once a frame had measured the width, so the first
        // layout used a 240px placeholder and the page laid out a second time (ISSUE-146 §5.3, ISSUE-150).
        var engine = new MikoEngineBuilder().Build();
        using var surface = engine.Graphics.CreateSurface(640, 480);
        engine.VideoBackend = new Backend();
        using var player = new MikoPlayer { Engine = engine, Source = "movie.mp4" };
        var root = new DivElement { Style = new Style { Display = Display.Block, Width = Length.Percent(100) } };
        root.Children.Add(player.Build());

        engine.Initialize(root, [], surface.Context, 640, 480);

        root.FindByClass("miko-player-viewport").Single().OffsetHeight.ShouldBe(360, .5f);
        engine.DisposeVideoSessions();
    }

    [Fact]
    public void Should_Keep_Viewport_Style_Across_Frames()
    {
        // Each frame raises PlaybackUpdated; replacing the viewport's inline style there invalidated the
        // layout that had just been computed.
        var viewport = _root.FindByClass("miko-player-viewport").Single();
        var style = viewport.Style;
        _player.Play(); Render();
        _engine.SetViewportSize(360, 740); Render();
        viewport.Style.ShouldBeSameAs(style);
    }

    [Fact]
    public void Should_Follow_Width_Changes_In_The_Same_Frame()
    {
        var viewport = _root.FindByClass("miko-player-viewport").Single();
        _engine.SetViewportSize(480, 740);
        _engine.Render(_canvas);
        viewport.OffsetHeight.ShouldBe(270, .5f);
    }

    [Fact]
    public void Should_Restore_16_By_9_After_Leaving_Fullscreen()
    {
        _engine.SetViewportSize(360, 740); Render();
        var viewport = _root.FindByClass("miko-player-viewport").Single();
        _player.ToggleFullscreen(); Render();
        // Fullscreen: the viewport takes the height the 28px progress bar and 48px controls leave free.
        viewport.OffsetHeight.ShouldBe(740 - 28 - 48, .5f);
        _player.ToggleFullscreen(); Render();
        viewport.OffsetHeight.ShouldBe(202.5f, .5f);
    }

    [Fact]
    public void Should_Shrink_Viewport_In_A_Fixed_Height_Player()
    {
        // The Anime detail page fixes the player height; the viewport gives way to the controls.
        _player.Style = new Style { Height = Length.Px(230), FlexShrink = 0 };
        _player.Build();
        _engine.SetViewportSize(360, 740); Render();
        _root.FindByClass("miko-player").Single().OffsetHeight.ShouldBe(230, .5f);
        _root.FindByClass("miko-player-viewport").Single().OffsetHeight.ShouldBe(230 - 28 - 48, .5f);
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
    [Fact]
    public void Should_Draw_Without_Referencing_The_Rendering_Backend()
    {
        // ISSUE-162: the player draws through Miko.Graphics; the backend stays inside the core library.
        typeof(MikoPlayer).Assembly.GetReferencedAssemblies().Select(a => a.Name)
            .ShouldNotContain(name => name!.StartsWith("SkiaSharp") || name.StartsWith("Svg.Skia") || name.StartsWith("HarfBuzzSharp"));
    }

    [Fact]
    public void Should_Paint_Progress_Icons_And_Danmaku()
    {
        _player.Danmaku = [new(TimeSpan.FromSeconds(29.5), "日本語のコメント", DanmakuMode.Top, Color.FromHex("#ffe066"))];
        _player.Build();
        _backend.Sessions[0].State = VideoSessionState.Playing;
        _backend.Sessions[0].Position = TimeSpan.FromSeconds(30);
        _backend.Sessions[0].Raise(new VideoSessionEvent.Loaded(640, 360, TimeSpan.FromSeconds(60)));
        Render();

        var progress = _root.FindByClass("miko-player-progress").Single();
        var bar = BorderBox(progress);
        float y = bar.Y + bar.Height / 2;
        // Played half (accent), the rest (track): the thumb sits at 50%.
        _surface.GetPixel((int)(bar.X + bar.Width * .25f), (int)y).ShouldBe(Color.FromHex("#38cfa1"));
        _surface.GetPixel((int)(bar.X + bar.Width * .75f), (int)y).ShouldBe(Color.FromHex("#494d53"));

        // Each control button has a white icon painted in its center.
        foreach (var button in _root.FindByClass("miko-player-controls").Single().FindByTagName("button"))
        {
            var box = BorderBox(button);
            CountNear(box, Color.White).ShouldBeGreaterThan(10, "icon missing");
        }

        // The pinned comment is drawn in its color (at the danmaku opacity, over black) at the top of the viewport.
        var viewport = _root.FindByClass("miko-player-viewport").Single();
        var viewportBox = BorderBox(viewport);
        var top = new RectF(viewportBox.X, viewportBox.Y, viewportBox.Width, 40);
        CountNear(top, new Color(230, 202, 92)).ShouldBeGreaterThan(50, "danmaku missing");
    }

    private RectF BorderBox(Element element)
    {
        static LayoutBox? Find(LayoutBox box, Element element)
            => box.Element == element ? box : box.Children.Select(c => Find(c, element)).FirstOrDefault(b => b != null);
        return Find(_engine.GetCurrentLayout()!, element)!.BoxModel.BorderBox;
    }

    private int CountNear(RectF area, Color color)
    {
        int count = 0;
        for (int y = (int)area.Top; y < (int)area.Bottom; y++)
        for (int x = (int)area.Left; x < (int)area.Right; x++)
        {
            var c = _surface.GetPixel(x, y);
            if (Math.Abs(c.R - color.R) < 24 && Math.Abs(c.G - color.G) < 24 && Math.Abs(c.B - color.B) < 24) count++;
        }
        return count;
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
