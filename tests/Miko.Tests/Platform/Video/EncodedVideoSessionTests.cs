using System.Threading.Channels;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Platform.Video;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Platform.Video;

/// <summary>
/// Live sessions: an <see cref="IEncodedVideoSource"/> read on a background task and fed to a
/// platform <see cref="IVideoStreamDecoder"/>. Fakes on both sides; the reconnect delay runs on a
/// manual clock, so no test sleeps for it.
/// </summary>
public class EncodedVideoSessionTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private static readonly byte[] KeyFrame = [0, 0, 0, 1, 0x26, 0x01, 0xaf];
    private static readonly byte[] DeltaFrame = [0, 0, 0, 1, 0x02, 0x01, 0xd0];

    private static EncodedVideoFrame Key(double seconds = 0) => new(KeyFrame, TimeSpan.FromSeconds(seconds), true);
    private static EncodedVideoFrame Delta(double seconds = 0) => new(DeltaFrame, TimeSpan.FromSeconds(seconds), false);

    private static void Until(Func<bool> condition, string what) =>
        SpinWait.SpinUntil(condition, Wait).ShouldBeTrue($"timed out waiting for {what}");

    [Fact]
    public void Should_Play_Live_Without_Duration_Once_A_Frame_Is_Decoded()
    {
        var factory = new FakeSourceFactory();
        var decoders = new FakeDecoders();
        var events = new List<VideoSessionEvent>();
        using var session = new EncodedVideoSession(new("rtsp://camera/stream"), factory, decoders, new(AutoPlay: true));
        session.Event += e => { lock (events) events.Add(e); };

        session.State.ShouldBe(VideoSessionState.Loading);
        Until(() => factory.Sources.Count == 1 && factory.Sources[0].Opened, "the source to open");
        factory.Sources[0].Push(Key());

        Until(() => session.State == VideoSessionState.Playing, "playing");
        session.Duration.ShouldBe(TimeSpan.Zero);
        session.VideoWidth.ShouldBe(1280);
        session.Codec.ShouldBe(VideoCodec.H265);
        lock (events)
        {
            events.OfType<VideoSessionEvent.Loaded>().Single().Duration.ShouldBe(TimeSpan.Zero);
            // Buffering(true) once the decoder exists (it makes on-demand hosts paint, see
            // EncodedVideoSession.EnsureDecoder), Buffering(false) with the first picture.
            events.OfType<VideoSessionEvent.Buffering>().Select(b => b.IsBuffering).ShouldBe([true, false]);
        }
    }

    [Fact]
    public void Should_Skip_Frames_Before_The_First_Key_Frame()
    {
        var factory = new FakeSourceFactory();
        var decoders = new FakeDecoders();
        using var session = new EncodedVideoSession(new("rtsp://camera/stream"), factory, decoders, new(AutoPlay: true));
        Until(() => factory.Sources.Count == 1 && factory.Sources[0].Opened, "the source to open");

        var source = factory.Sources[0];
        source.Push(Delta(0));
        source.Push(Delta(.1));
        source.Push(Key(.2));
        source.Push(Delta(.3));

        Until(() => session.FramesDecoded == 2, "two decoded frames");
        decoders.Created.Single().Decoded.Select(f => f.IsKeyFrame).ShouldBe([true, false]);
    }

    [Fact]
    public void Should_Not_Connect_Until_Play_Without_AutoPlay()
    {
        var factory = new FakeSourceFactory();
        using var session = new EncodedVideoSession(new("rtsp://camera/stream"), factory, new FakeDecoders(), new(AutoPlay: false));

        session.State.ShouldBe(VideoSessionState.Paused);
        factory.Sources.ShouldBeEmpty();

        session.Play();
        Until(() => factory.Sources.Count == 1, "a connection");
    }

    [Fact]
    public void Should_Disconnect_On_Pause_And_Reconnect_On_Play_With_The_Same_Decoder()
    {
        var factory = new FakeSourceFactory();
        var decoders = new FakeDecoders();
        using var session = new EncodedVideoSession(new("rtsp://camera/stream"), factory, decoders, new(AutoPlay: true));
        Until(() => factory.Sources.Count == 1 && factory.Sources[0].Opened, "the source to open");
        factory.Sources[0].Push(Key());
        Until(() => session.State == VideoSessionState.Playing, "playing");

        session.Pause();
        session.State.ShouldBe(VideoSessionState.Paused);
        Until(() => factory.Sources[0].Disposed, "the first connection to close");

        session.Play();
        Until(() => factory.Sources.Count == 2 && factory.Sources[1].Opened, "a second connection");
        factory.Sources[1].Push(Key(5));
        Until(() => session.State == VideoSessionState.Playing, "playing again");
        // The picture never goes blank between connections: one decoder for the session.
        decoders.Created.Count.ShouldBe(1);
        decoders.Created[0].Decoded.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_Reconnect_After_A_Dropped_Stream()
    {
        var clock = new StepClock();
        var factory = new FakeSourceFactory();
        var decoders = new FakeDecoders();
        var buffering = new List<bool>();
        using var session = new EncodedVideoSession(new("rtsp://camera/stream"), factory, decoders, new(AutoPlay: true), timeProvider: clock);
        session.Event += e => { if (e is VideoSessionEvent.Buffering b) lock (buffering) buffering.Add(b.IsBuffering); };
        Until(() => factory.Sources.Count == 1 && factory.Sources[0].Opened, "the source to open");
        factory.Sources[0].Push(Key());
        Until(() => session.State == VideoSessionState.Playing, "playing");

        factory.Sources[0].Fail(new IOException("connection reset"));
        Until(() => session.State == VideoSessionState.Loading, "buffering after the drop");
        Until(() => clock.PendingTimers > 0, "the retry delay");
        factory.Sources.Count.ShouldBe(1);

        clock.Advance(TimeSpan.FromSeconds(1));
        Until(() => factory.Sources.Count == 2 && factory.Sources[1].Opened, "the reconnection");
        factory.Sources[1].Push(Key(10));
        Until(() => session.State == VideoSessionState.Playing, "playing after reconnecting");
        // The constructor's Buffering(true) comes before anyone subscribes (the engine reads State /
        // IsBuffering when it attaches); then: decoder created, first picture, drop, picture again.
        lock (buffering) buffering.ShouldBe([true, false, true, false]);
    }

    [Fact]
    public void Should_Fail_At_Once_On_A_Permanent_Error()
    {
        var factory = new FakeSourceFactory { OpenError = new VideoStreamException("The camera rejected the password.", isPermanent: true) };
        VideoSessionEvent.Error? error = null;
        using var session = new EncodedVideoSession(new("rtsp://camera/stream"), factory, new FakeDecoders(), new(AutoPlay: true));
        session.Event += e => { if (e is VideoSessionEvent.Error err) error = err; };

        Until(() => session.State == VideoSessionState.Error, "the error");
        session.ErrorMessage.ShouldBe("The camera rejected the password.");
        factory.Sources.Count.ShouldBe(1);
        Until(() => error != null, "the error event");
        error!.Cause.ShouldBeOfType<VideoStreamException>();
    }

    [Fact]
    public void Should_Give_Up_After_The_Retry_Budget()
    {
        var clock = new StepClock();
        var factory = new FakeSourceFactory { OpenError = new IOException("no route to host") };
        using var session = new EncodedVideoSession(new("rtsp://camera/stream"), factory, new FakeDecoders(), new(AutoPlay: true), timeProvider: clock);

        for (int attempt = 1; attempt <= EncodedVideoSession.MaxReconnectAttempts; attempt++)
        {
            Until(() => clock.PendingTimers > 0, $"retry delay {attempt}");
            clock.Advance(TimeSpan.FromSeconds(8));
        }

        Until(() => session.State == VideoSessionState.Error, "giving up");
        factory.Sources.Count.ShouldBe(EncodedVideoSession.MaxReconnectAttempts + 1);
        session.ErrorMessage.ShouldBe("no route to host");
    }

    [Fact]
    public void Should_Fail_Without_A_Decoder_For_The_Codec()
    {
        var factory = new FakeSourceFactory();
        using var session = new EncodedVideoSession(new("rtsp://camera/stream"), factory, new FakeDecoders { Supported = false }, new(AutoPlay: true));

        Until(() => session.State == VideoSessionState.Error, "the error");
        session.ErrorMessage!.ShouldContain("H265");
        factory.Sources.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_Close_The_Connection_And_Decoder_On_Dispose()
    {
        var factory = new FakeSourceFactory();
        var decoders = new FakeDecoders();
        var session = new EncodedVideoSession(new("rtsp://camera/stream"), factory, decoders, new(AutoPlay: true));
        Until(() => factory.Sources.Count == 1 && factory.Sources[0].Opened, "the source to open");
        factory.Sources[0].Push(Key());
        Until(() => session.FramesDecoded == 1, "a frame");

        session.Dispose();

        Until(() => factory.Sources[0].Disposed && decoders.Created[0].Disposed, "the connection and decoder to close");
        session.FrameSource.AcquireCurrentFrame(GraphicsDevice.Software).ShouldBeNull();
    }

    [Fact]
    public void Should_Count_Bytes_Over_All_Connections()
    {
        var factory = new FakeSourceFactory();
        using var session = new EncodedVideoSession(new("rtsp://camera/stream"), factory, new FakeDecoders(), new(AutoPlay: true));
        Until(() => factory.Sources.Count == 1 && factory.Sources[0].Opened, "the source to open");
        factory.Sources[0].Push(Key());
        Until(() => session.FramesDecoded == 1, "a frame");
        session.BytesReceived.ShouldBe(KeyFrame.Length);

        session.Pause();
        Until(() => factory.Sources[0].Disposed, "the connection to close");
        session.BytesReceived.ShouldBe(KeyFrame.Length);
    }

    [Theory]
    [InlineData("rtsp://admin:QWERTY@192.168.1.5:554/h264/ch1/main/av_stream", "rtsp://***@192.168.1.5:554/h264/ch1/main/av_stream")]
    [InlineData("rtsp://192.168.1.5/live", "rtsp://192.168.1.5/live")]
    [InlineData("https://host/path?user=a@b", "https://host/path?user=a@b")]
    public void Should_Redact_Credentials_From_Logged_Uris(string uri, string expected) =>
        EncodedVideoSession.Redact(uri).ShouldBe(expected);

    // ---- engine routing -------------------------------------------------------------

    [Fact]
    public void Engine_Should_Play_A_Recognised_Source_As_A_Live_Session()
    {
        var backend = new DecodingBackend();
        var engine = new MikoEngineBuilder().Build();
        engine.VideoBackend = backend;
        var factory = new FakeSourceFactory();
        engine.EncodedVideoSources.Add(factory);
        var live = new VideoElement { Source = "rtsp://camera/stream", AutoPlay = true };
        var file = new VideoElement { Source = "movie.mp4" };
        using var surface = engine.Graphics.CreateSurface(320, 240);

        engine.Initialize(new DivElement { Children = { live, file } }, new List<StyleSheet>(), surface.Context, 320, 240);

        live.Session.ShouldBeOfType<EncodedVideoSession>();
        backend.Sessions.ShouldBe(["movie.mp4"]);
        Until(() => factory.Sources.Count == 1, "the live connection");
        engine.DisposeVideoSessions();
    }

    [Fact]
    public void Engine_Should_Report_A_Live_Source_The_Backend_Cannot_Decode()
    {
        var engine = new MikoEngineBuilder().Build();
        engine.VideoBackend = new FileOnlyBackend();
        engine.EncodedVideoSources.Add(new FakeSourceFactory());
        var live = new VideoElement { Source = "rtsp://camera/stream" };
        VideoSessionEvent.Error? error = null;
        live.PlaybackEvent += e => error ??= e as VideoSessionEvent.Error;
        using var surface = engine.Graphics.CreateSurface(320, 240);

        engine.Initialize(new DivElement { Children = { live } }, new List<StyleSheet>(), surface.Context, 320, 240);

        live.Session.ShouldBeNull();
        error.ShouldNotBeNull().Message.ShouldContain("cannot decode live streams");
    }

    // ---- fakes ------------------------------------------------------------------------

    private sealed class FakeSourceFactory : IEncodedVideoSourceFactory
    {
        public List<FakeSource> Sources { get; } = new();
        public Exception? OpenError { get; init; }

        public bool CanOpen(VideoSourceDescriptor source) => source.Uri.StartsWith("rtsp://", StringComparison.Ordinal);

        public IEncodedVideoSource Open(VideoSourceDescriptor source)
        {
            var created = new FakeSource(OpenError);
            lock (Sources) Sources.Add(created);
            return created;
        }
    }

    private sealed class FakeSource(Exception? openError) : IEncodedVideoSource
    {
        private readonly Channel<object> _frames = Channel.CreateUnbounded<object>();
        private long _bytes;

        public volatile bool Opened;
        public volatile bool Disposed;

        public long BytesReceived => Interlocked.Read(ref _bytes);

        public void Push(EncodedVideoFrame frame) => _frames.Writer.TryWrite(frame);
        public void Fail(Exception error) => _frames.Writer.TryWrite(error);

        public Task<EncodedVideoStreamInfo> OpenAsync(CancellationToken cancellationToken)
        {
            if (openError != null) throw openError;
            Opened = true;
            return Task.FromResult(new EncodedVideoStreamInfo(VideoCodec.H265));
        }

        public async ValueTask<EncodedVideoFrame?> ReadFrameAsync(CancellationToken cancellationToken)
        {
            var item = await _frames.Reader.ReadAsync(cancellationToken);
            if (item is Exception error) throw error;
            var frame = (EncodedVideoFrame)item;
            Interlocked.Add(ref _bytes, frame.Data.Length);
            return frame;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeDecoders : IVideoStreamDecoderFactory
    {
        public bool Supported { get; init; } = true;
        public List<FakeDecoder> Created { get; } = new();
        public bool CanDecode(VideoCodec codec) => Supported;
        public IVideoStreamDecoder CreateDecoder(VideoCodec codec)
        {
            var decoder = new FakeDecoder();
            Created.Add(decoder);
            return decoder;
        }
    }

    private sealed class FakeDecoder : IVideoStreamDecoder
    {
        public List<EncodedVideoFrame> Decoded { get; } = new();
        public volatile bool Disposed;
        public IVideoFrameSource FrameSource { get; } = new NoFrames();
        public event Action<VideoSessionEvent>? Event;

        public void Decode(in EncodedVideoFrame frame)
        {
            Decoded.Add(new EncodedVideoFrame(frame.Data.ToArray(), frame.Timestamp, frame.IsKeyFrame));
            if (Decoded.Count == 1) Event?.Invoke(new VideoSessionEvent.Loaded(1280, 720, TimeSpan.Zero));
            Event?.Invoke(new VideoSessionEvent.FrameAvailable(frame.Timestamp));
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class NoFrames : IVideoFrameSource
    {
        public Image? AcquireCurrentFrame(GraphicsDevice device) => null;
        public void ReleaseCurrentFrame() { }
    }

    private class FileOnlyBackend : IVideoBackend
    {
        public List<string> Sessions { get; } = new();
        public VideoBackendCapabilities Capabilities { get; } = new(false, false, []);
        public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        {
            Sessions.Add(source.Uri);
            return new EncodedVideoSession(source, new FakeSourceFactory(), new FakeDecoders(), options with { AutoPlay = false });
        }
    }

    private sealed class DecodingBackend : FileOnlyBackend, IVideoStreamDecoderFactory
    {
        public bool CanDecode(VideoCodec codec) => true;
        public IVideoStreamDecoder CreateDecoder(VideoCodec codec) => new FakeDecoder();
    }

    /// <summary>A clock whose timers fire only when a test advances it.</summary>
    private sealed class StepClock : TimeProvider
    {
        private readonly List<StepTimer> _timers = new();
        private long _ticks;

        public int PendingTimers { get { lock (_timers) return _timers.Count(t => t.DueAt != null); } }

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new StepTimer(this, callback, state);
            timer.Change(dueTime, period);
            return timer;
        }

        public void Advance(TimeSpan by)
        {
            Interlocked.Add(ref _ticks, by.Ticks);
            StepTimer[] due;
            lock (_timers) due = _timers.Where(t => t.DueAt <= _ticks).ToArray();
            foreach (var timer in due)
            {
                timer.DueAt = null;
                timer.Fire();
            }
        }

        private sealed class StepTimer(StepClock clock, TimerCallback callback, object? state) : ITimer
        {
            public long? DueAt { get; set; }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (clock._timers)
                {
                    DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : clock._ticks + dueTime.Ticks;
                    if (!clock._timers.Contains(this)) clock._timers.Add(this);
                }
                return true;
            }

            public void Fire() => callback(state);

            public void Dispose()
            {
                lock (clock._timers) clock._timers.Remove(this);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
