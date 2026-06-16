using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Platform.Video;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 视频会话生命周期：用 fake 后端验证引擎在渲染时创建会话、写入内禀尺寸、
/// 处理跨线程失效，并在元素离开树时回收会话——不依赖真实 FFmpeg。
/// </summary>
public class VideoSessionLifecycleTests : IDisposable
{
    private readonly SKBitmap _bitmap = new(800, 600);
    private readonly SKCanvas _canvas;

    public VideoSessionLifecycleTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private static List<StyleSheet> VideoSheet() => new()
    {
        new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new() { Selector = new TagSelector("div"), Style = new Style { Display = Display.Block } },
                new() { Selector = new TagSelector("video"), Style = new Style { Display = Display.Block } },
            }
        }
    };

    [Fact]
    public void Initialize_WithVideoElement_CreatesSession()
    {
        var backend = new FakeVideoBackend();
        var engine = new MikoEngine { VideoBackend = backend };
        var video = new VideoElement { Source = "movie.mp4" };
        var root = new DivElement { Children = { video } };

        engine.Initialize(root, VideoSheet(), _canvas, 800, 600);

        backend.CreatedSessions.Count.ShouldBe(1);
        backend.CreatedSessions[0].SourceUri.ShouldBe("movie.mp4");
        video.Session.ShouldNotBeNull();
    }

    [Fact]
    public void VideoWithoutSource_DoesNotCreateSession()
    {
        var backend = new FakeVideoBackend();
        var engine = new MikoEngine { VideoBackend = backend };
        var root = new DivElement { Children = { new VideoElement() } };

        engine.Initialize(root, VideoSheet(), _canvas, 800, 600);

        backend.CreatedSessions.ShouldBeEmpty();
    }

    [Fact]
    public void LoadedEvent_WritesIntrinsicSize_AndRelayoutUsesIt()
    {
        var backend = new FakeVideoBackend();
        var engine = new MikoEngine { VideoBackend = backend };
        var video = new VideoElement { Source = "movie.mp4" };
        var root = new DivElement { Children = { video } };

        engine.Initialize(root, VideoSheet(), _canvas, 800, 600);

        // 首帧前回退到默认 300×150
        engine.GetCurrentLayout().ShouldNotBeNull();
        FindBox(engine.GetCurrentLayout()!, video)!.BoxModel.Content.Width.ShouldBe(300);

        // 后端报告媒体尺寸（模拟解码线程事件）
        backend.CreatedSessions[0].RaiseLoaded(1280, 720, TimeSpan.FromSeconds(10));

        // 跨线程失效在下一次 Render 时排空并重排
        engine.Render(_canvas);

        video.IntrinsicWidth.ShouldBe(1280);
        video.IntrinsicHeight.ShouldBe(720);
        FindBox(engine.GetCurrentLayout()!, video)!.BoxModel.Content.Width.ShouldBe(1280);
    }

    [Fact]
    public void VideoRemovedFromTree_DisposesSession()
    {
        var backend = new FakeVideoBackend();
        var engine = new MikoEngine { VideoBackend = backend };
        var video = new VideoElement { Source = "movie.mp4" };
        var root = new DivElement { Children = { video } };

        engine.Initialize(root, VideoSheet(), _canvas, 800, 600);
        var session = backend.CreatedSessions[0];
        session.Disposed.ShouldBeFalse();

        // 移除 video 后重新渲染：会话应被回收
        root.Children.Clear();
        engine.Render(_canvas);

        session.Disposed.ShouldBeTrue();
        video.Session.ShouldBeNull();
    }

    [Fact]
    public void DisposeVideoSessions_DisposesAll()
    {
        var backend = new FakeVideoBackend();
        var engine = new MikoEngine { VideoBackend = backend };
        var root = new DivElement
        {
            Children =
            {
                new VideoElement { Source = "a.mp4" },
                new VideoElement { Source = "b.mp4" },
            }
        };

        engine.Initialize(root, VideoSheet(), _canvas, 800, 600);
        backend.CreatedSessions.Count.ShouldBe(2);

        engine.DisposeVideoSessions();

        backend.CreatedSessions.ShouldAllBe(s => s.Disposed);
    }

    [Fact]
    public void PostInvalidate_IsThreadSafeEntryPoint()
    {
        var engine = new MikoEngine();
        var root = new DivElement();
        engine.Initialize(root, VideoSheet(), _canvas, 800, 600);

        engine.HasPendingInvalidations.ShouldBeFalse();
        engine.PostInvalidate(root);
        engine.HasPendingInvalidations.ShouldBeTrue();

        // Render 排空待处理失效
        engine.Render(_canvas);
        engine.HasPendingInvalidations.ShouldBeFalse();
    }

    private static LayoutBox? FindBox(LayoutBox box, Element element)
    {
        if (box.Element == element) return box;
        foreach (var child in box.Children)
        {
            var found = FindBox(child, element);
            if (found != null) return found;
        }
        return null;
    }

    // ---- fakes -------------------------------------------------------------

    private sealed class FakeVideoBackend : IVideoBackend
    {
        public List<FakeVideoSession> CreatedSessions { get; } = new();

        public VideoBackendCapabilities Capabilities { get; } =
            new(HardwareDecode: false, Hdr: false, SupportedMimeTypes: new[] { "video/mp4" });

        public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        {
            var session = new FakeVideoSession(source.Uri);
            CreatedSessions.Add(session);
            return session;
        }
    }

    private sealed class FakeVideoSession : IVideoSession
    {
        public string SourceUri { get; }
        public bool Disposed { get; private set; }

        public FakeVideoSession(string uri) => SourceUri = uri;

        public void RaiseLoaded(int w, int h, TimeSpan duration)
            => Event?.Invoke(new VideoSessionEvent.Loaded(w, h, duration));

        public IVideoFrameSource FrameSource { get; } = new FakeFrameSource();
        public VideoSessionState State => VideoSessionState.Ready;
        public TimeSpan Duration => TimeSpan.Zero;
        public TimeSpan Position => TimeSpan.Zero;
        public int VideoWidth => 0;
        public int VideoHeight => 0;
        public event Action<VideoSessionEvent>? Event;

        public void Play() { }
        public void Pause() { }
        public void Seek(TimeSpan position) { }
        public void SetVolume(float volume) { }
        public void SetMuted(bool muted) { }
        public void SetPlaybackRate(float rate) { }
        public void SetLoop(bool loop) { }
        public void Dispose() => Disposed = true;
    }

    private sealed class FakeFrameSource : IVideoFrameSource
    {
        public SKImage? AcquireCurrentFrame(GRContext? grContext) => null;
        public void ReleaseCurrentFrame() { }
    }
}
