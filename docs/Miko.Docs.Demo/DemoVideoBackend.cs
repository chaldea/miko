using Miko.Common;
using Miko.Graphics;
using Miko.Platform.Video;

namespace Miko.Docs.Demo;

/// <summary>
/// Demo-only media backend: a fixed locally drawn picture and a deterministic 60-second clock.
/// It deliberately does not claim to decode MP4/HLS in the Browser host.
/// </summary>
internal sealed class DemoVideoBackend(IGraphicsFactory graphics) : IVideoBackend
{
    public VideoBackendCapabilities Capabilities { get; } = new(false, false, ["video/mp4"]);

    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        => new DemoVideoSession(graphics, options);
}

internal sealed class DemoVideoSession : IVideoSession
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(250);
    private readonly object _sync = new();
    private readonly Timer _timer;
    private readonly DemoVideoFrameSource _frameSource;
    private VideoSessionState _state = VideoSessionState.Ready;
    private TimeSpan _position;
    private float _rate = 1;
    private bool _loop;
    private bool _disposed;

    public DemoVideoSession(IGraphicsFactory graphics, VideoSessionOptions options)
    {
        _frameSource = new DemoVideoFrameSource(graphics);
        _loop = options.Loop;
        _timer = new Timer(Tick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        if (options.AutoPlay) Play();
    }

    public event Action<VideoSessionEvent>? Event;

    public VideoSessionState State { get { lock (_sync) return _state; } }
    public TimeSpan Duration { get; } = TimeSpan.FromSeconds(60);
    public TimeSpan Position { get { lock (_sync) return _position; } }
    public IReadOnlyList<VideoTimeRange> BufferedRanges { get; } = [new VideoTimeRange(TimeSpan.Zero, TimeSpan.FromSeconds(60))];
    public int VideoWidth => 640;
    public int VideoHeight => 360;
    public IVideoFrameSource FrameSource => _frameSource;

    public void Play()
    {
        lock (_sync)
        {
            if (_disposed) return;
            if (_position >= Duration) _position = TimeSpan.Zero;
            _state = VideoSessionState.Playing;
            _timer.Change(TickInterval, TickInterval);
        }
        Raise(new VideoSessionEvent.FrameAvailable(Position));
    }

    public void Pause()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _state = VideoSessionState.Paused;
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        Raise(new VideoSessionEvent.FrameAvailable(Position));
    }

    public void Seek(TimeSpan position)
    {
        lock (_sync)
        {
            if (_disposed) return;
            _position = TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, Duration.Ticks));
            if (_state == VideoSessionState.Ended) _state = VideoSessionState.Paused;
        }
        Raise(new VideoSessionEvent.FrameAvailable(Position));
    }

    public void SetVolume(float volume) { }
    public void SetMuted(bool muted) { }

    public void SetPlaybackRate(float rate)
    {
        lock (_sync) _rate = Math.Clamp(rate, .25f, 4f);
    }

    public void SetLoop(bool loop)
    {
        lock (_sync) _loop = loop;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Dispose();
        }
        _frameSource.Dispose();
        Event = null;
    }

    private void Tick(object? state)
    {
        VideoSessionEvent notification;
        lock (_sync)
        {
            if (_disposed || _state != VideoSessionState.Playing) return;
            _position += TimeSpan.FromTicks((long)(TickInterval.Ticks * _rate));
            if (_position >= Duration)
            {
                if (_loop)
                {
                    _position = TimeSpan.FromTicks(_position.Ticks % Duration.Ticks);
                }
                else
                {
                    _position = Duration;
                    _state = VideoSessionState.Ended;
                    _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    notification = new VideoSessionEvent.Ended();
                    goto Notify;
                }
            }
            notification = new VideoSessionEvent.FrameAvailable(_position);
        }

    Notify:
        Raise(notification);
    }

    private void Raise(VideoSessionEvent value)
    {
        if (!_disposed) Event?.Invoke(value);
    }
}

internal sealed class DemoVideoFrameSource(IGraphicsFactory graphics) : IVideoFrameSource, IDisposable
{
    private Image? _image;

    public Image? AcquireCurrentFrame(GraphicsDevice device)
    {
        if (_image is not null) return _image;
        using var surface = graphics.CreateSurface(640, 360);
        var context = surface.Context;
        context.Clear(Color.FromHex("#12162b"));
        context.FillCircle(540, 54, 190, Color.FromHex("#512bd4"));
        context.FillCircle(72, 340, 170, Color.FromHex("#176b67"));
        context.FillRoundRect(new RectF(42, 40, 556, 280), 24, Color.FromHex("#202941"));
        context.StrokeRect(new RectF(42, 40, 556, 280), Color.FromHex("#5b647c"), 1);
        context.DrawText(graphics.CreateText("MIKO", new TextStyle("Inter", 68, FontWeight.Bold)), 72, 155, Color.FromHex("#ffffff"));
        context.DrawText(graphics.CreateText("LOCAL MEDIA SESSION", new TextStyle("Inter", 17, FontWeight.Bold, 2)), 76, 198, Color.FromHex("#b8a7ed"));
        context.DrawText(graphics.CreateText("A fixed picture with a live 60-second timeline", new TextStyle("Inter", 16)), 76, 242, Color.FromHex("#a8b1c5"));
        context.FillRoundRect(new RectF(76, 267, 180, 5), 2.5f, Color.FromHex("#38cfa1"));
        _image = surface.Snapshot();
        return _image;
    }

    public void ReleaseCurrentFrame() { }
    public void Dispose() { _image?.Dispose(); _image = null; }
}
