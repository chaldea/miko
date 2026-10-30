using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Graphics;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using Miko.Platform.Video;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// Release-1.0.0 A1：应用进入后台 / 回到前台 / 内存告警。宿主把系统信号转给
/// <see cref="MikoInteractionController"/>；后台期间视频必须停播，回到前台只恢复被后台暂停的那些。
/// </summary>
public class AppLifecycleTests
{
    private static (MikoInteractionController Controller, MikoEngine Engine, FakeVideoBackend Backend, SKSurface Surface)
        Create(params VideoElement[] videos)
    {
        var root = new DivElement();
        foreach (var video in videos)
            root.Children.Add(video);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Display = Display.Block, Width = Length.Px(400), Height = Length.Px(400) },
            ["video"] = new() { Display = Display.Block, Width = Length.Px(100), Height = Length.Px(100) },
        });

        var backend = new FakeVideoBackend();
        var engine = new MikoEngineBuilder().Build();
        engine.VideoBackend = backend;
        var controller = new MikoInteractionController(
            Options.Create(new MikoAppOptions { RootComponentFactory = () => root, StyleSheets = { sheet } }),
            new EmptyServiceProvider(),
            engine,
            new EventDispatcher(),
            new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance);

        var surface = SKSurface.Create(new SKImageInfo(400, 400));
        controller.Initialize(surface.Canvas, 400, 400);
        engine.Render(surface.Canvas);
        return (controller, engine, backend, surface);
    }

    [Fact]
    public void Should_pause_playing_videos_on_suspend_and_resume_only_those()
    {
        var (controller, _, backend, surface) = Create(
            new VideoElement { Source = "playing.mp4" },
            new VideoElement { Source = "paused-by-app.mp4" });
        using var _ = surface;
        var playing = backend.Session("playing.mp4");
        var pausedByApp = backend.Session("paused-by-app.mp4");
        playing.Play();

        controller.Suspend();

        playing.State.ShouldBe(VideoSessionState.Paused);
        pausedByApp.PlayCalls.ShouldBe(0);
        controller.IsSuspended.ShouldBeTrue();

        controller.Resume();

        playing.State.ShouldBe(VideoSessionState.Playing);
        pausedByApp.PlayCalls.ShouldBe(0, "a video the app paused itself must stay paused");
        controller.IsSuspended.ShouldBeFalse();
    }

    [Fact]
    public void Should_raise_each_event_once_and_ignore_repeated_calls()
    {
        var (controller, _, backend, surface) = Create(new VideoElement { Source = "a.mp4" });
        using var _ = surface;
        var session = backend.Session("a.mp4");
        session.Play();
        int suspended = 0, resumed = 0;
        controller.Suspended += () => suspended++;
        controller.Resumed += () => resumed++;

        controller.Suspend();
        controller.Suspend();
        controller.Resume();
        controller.Resume();

        suspended.ShouldBe(1);
        resumed.ShouldBe(1);
        session.PlayCalls.ShouldBe(2, "the app's own Play plus one resume, not one per Resume call");
    }

    [Fact]
    public void Should_request_a_frame_on_resume()
    {
        var (controller, engine, _, surface) = Create();
        using var _ = surface;
        engine.HasPendingVisualWork.ShouldBeFalse();

        controller.Suspend();
        controller.Resume();

        engine.HasPendingVisualWork.ShouldBeTrue();
    }

    [Fact]
    public void Should_not_resume_a_session_released_while_in_background()
    {
        var video = new VideoElement { Source = "a.mp4" };
        var (controller, engine, backend, surface) = Create(video);
        using var _ = surface;
        var session = backend.Session("a.mp4");
        session.Play();
        controller.Suspend();

        // 后台期间元素离开了树（例如推送导航换页）：下一帧回收会话。
        ((DivElement)video.Parent!).Children.Remove(video);
        engine.Render(surface.Canvas);
        session.Disposed.ShouldBeTrue();

        controller.Resume();

        session.PlayCalls.ShouldBe(1, "a disposed session must not be played again");
    }

    [Fact]
    public void Should_schedule_a_frame_for_a_memory_warning()
    {
        var (controller, engine, _, surface) = Create();
        using var _ = surface;
        engine.HasPendingVisualWork.ShouldBeFalse();

        controller.TrimMemory();

        engine.HasPendingVisualWork.ShouldBeTrue("the purge runs at the start of the next frame, on the render thread");
        controller.RenderFrame(surface.Canvas, 400, 400, 1f / 60f, engine.Render);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class FakeVideoBackend : IVideoBackend
    {
        private readonly List<FakeVideoSession> _sessions = new();

        public VideoBackendCapabilities Capabilities { get; } =
            new(HardwareDecode: false, Hdr: false, SupportedMimeTypes: new[] { "video/mp4" });

        public FakeVideoSession Session(string uri) => _sessions.Single(s => s.SourceUri == uri);

        public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        {
            var session = new FakeVideoSession(source.Uri);
            _sessions.Add(session);
            return session;
        }
    }

    private sealed class FakeVideoSession(string uri) : IVideoSession
    {
        public string SourceUri { get; } = uri;
        public bool Disposed { get; private set; }
        public int PlayCalls { get; private set; }

        public VideoSessionState State { get; private set; } = VideoSessionState.Ready;
        public IVideoFrameSource FrameSource { get; } = new NullFrameSource();
        public TimeSpan Duration => TimeSpan.Zero;
        public TimeSpan Position => TimeSpan.Zero;
        public int VideoWidth => 0;
        public int VideoHeight => 0;
        public event Action<VideoSessionEvent>? Event { add { } remove { } }

        public void Play() { PlayCalls++; State = VideoSessionState.Playing; }
        public void Pause() => State = VideoSessionState.Paused;
        public void Seek(TimeSpan position) { }
        public void SetVolume(float volume) { }
        public void SetMuted(bool muted) { }
        public void SetPlaybackRate(float rate) { }
        public void SetLoop(bool loop) { }
        public void Dispose() => Disposed = true;
    }

    private sealed class NullFrameSource : IVideoFrameSource
    {
        public Image? AcquireCurrentFrame(GraphicsDevice device) => null;
        public void ReleaseCurrentFrame() { }
    }
}
