using Android.Media;
using Android.OS;
using Miko.Platform.Video;

namespace Miko.Android.Video;

/// <summary>Android's system A/V and HLS player feeding the existing Skia SurfaceTexture.</summary>
internal sealed class AndroidMediaPlayerSession : IVideoSession
{
    private readonly VideoSourceDescriptor _source;
    private readonly AndroidVideoFrameSource _frames = new();
    private readonly HandlerThread _thread = new("miko-media-player");
    private Handler? _handler;
    private MediaPlayer? _player;
    private volatile bool _disposed, _prepared, _playRequested, _muted, _loop;
    private volatile float _volume, _rate = 1;
    private float _appliedRate = 1;
    private volatile VideoSessionState _state = VideoSessionState.Loading;
    private volatile bool _buffering = true;
    private long _position, _duration;
    private int _width, _height;
    private string? _error;
    private IReadOnlyList<VideoTimeRange> _buffered = Array.Empty<VideoTimeRange>();
    private long _seek = -1;

    public AndroidMediaPlayerSession(VideoSourceDescriptor source, VideoSessionOptions options)
    { _source = source; _playRequested = options.AutoPlay; _muted = options.Muted; _volume = options.InitialVolume; _loop = options.Loop; }
    public VideoSessionState State => _state;
    public TimeSpan Duration => TimeSpan.FromTicks(Interlocked.Read(ref _duration));
    public TimeSpan Position => TimeSpan.FromTicks(Interlocked.Read(ref _position));
    public int VideoWidth => _width;
    public int VideoHeight => _height;
    public bool IsBuffering => _buffering;
    public string? ErrorMessage => _error;
    public IReadOnlyList<VideoTimeRange> BufferedRanges => Volatile.Read(ref _buffered);
    public IVideoFrameSource FrameSource => _frames;
    public event Action<VideoSessionEvent>? Event;

    public void Start()
    {
        _thread.Start();
        _handler = new Handler(_thread.Looper!);
        // SeekComplete can precede the decoder's surface update, especially while paused.
        _frames.FrameAvailable += () => _handler?.Post(PublishFrame);
        _handler.Post(Initialize);
    }
    private void Initialize()
    {
        try
        {
            var surface = _frames.WaitForSurface(TimeSpan.FromSeconds(5));
            if (_disposed) return;
            if (surface == null) throw new InvalidOperationException("Video output surface is unavailable.");
            _player = new MediaPlayer();
            _player.SetSurface(surface);
            _player.Prepared += (_, _) =>
            {
                if (_disposed) return;
                _prepared = true;
                _width = _player.VideoWidth; _height = _player.VideoHeight;
                Interlocked.Exchange(ref _duration, TimeSpan.FromMilliseconds(Math.Max(0, _player.Duration)).Ticks);
                _state = VideoSessionState.Ready;
                _buffering = false;
                Event?.Invoke(new VideoSessionEvent.Loaded(_width, _height, Duration));
                ApplyControls();
                PublishFrame();
                Tick();
            };
            _player.Completion += (_, _) => { _state = VideoSessionState.Ended; _playRequested = false; Event?.Invoke(new VideoSessionEvent.Ended()); };
            _player.Error += (_, e) => { e.Handled = true; Fail($"Android media error: {e.What}, {e.Extra}"); };
            _player.Info += (_, e) =>
            {
                if (e.What is MediaInfo.BufferingStart or MediaInfo.BufferingEnd)
                {
                    _buffering = e.What == MediaInfo.BufferingStart;
                    Event?.Invoke(new VideoSessionEvent.Buffering(_buffering));
                }
            };
            _player.BufferingUpdate += (_, e) => Volatile.Write(ref _buffered,
                new[] { new VideoTimeRange(TimeSpan.Zero, TimeSpan.FromTicks((long)(Duration.Ticks * (e.Percent / 100d)))) });
            _player.SeekComplete += (_, _) =>
            {
                _buffering = false;
                PublishFrame();
                Event?.Invoke(new VideoSessionEvent.Buffering(false));
            };
            _player.SetDataSource(_source.ResolveForBackend());
            _player.PrepareAsync();
        }
        catch (Exception ex) { if (!_disposed) Fail(ex.Message); }
    }
    private void PostControls() { if (!_disposed) _handler?.Post(ApplyControls); }
    public void Play() { _playRequested = true; PostControls(); }
    public void Pause() { _playRequested = false; PostControls(); }
    public void Seek(TimeSpan position) { Interlocked.Exchange(ref _seek, Math.Max(0, (long)position.TotalMilliseconds)); PostControls(); }
    public void SetVolume(float volume) { _volume = Math.Clamp(volume, 0, 1); PostControls(); }
    public void SetMuted(bool muted) { _muted = muted; PostControls(); }
    public void SetPlaybackRate(float rate) { _rate = rate; PostControls(); }
    public void SetLoop(bool loop) { _loop = loop; PostControls(); }
    private void ApplyControls()
    {
        if (_disposed || !_prepared || _player == null || _state == VideoSessionState.Error) return;
        try
        {
            float volume = _muted ? 0 : _volume;
            _player.SetVolume(volume, volume);
            _player.Looping = _loop;
            long seek = Interlocked.Exchange(ref _seek, -1);
            if (seek >= 0)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(26)) _player.SeekTo(seek, MediaPlayerSeekMode.Closest);
                else _player.SeekTo((int)Math.Min(int.MaxValue, seek));
            }
            if (_appliedRate != _rate && OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                using var parameters = new PlaybackParams();
                parameters.SetSpeed(_rate);
                _player.PlaybackParams = parameters;
                _appliedRate = _rate;
            }
            if (_playRequested) { if (!_player.IsPlaying) _player.Start(); _state = VideoSessionState.Playing; }
            else { if (_player.IsPlaying) _player.Pause(); _state = VideoSessionState.Paused; }
        }
        catch (Exception ex) { Fail(ex.Message); }
    }
    private void Tick()
    {
        if (_disposed || _state == VideoSessionState.Error) return;
        try { if (_state == VideoSessionState.Playing) PublishFrame(); }
        catch (Exception ex) { Fail(ex.Message); return; }
        _handler?.PostDelayed(Tick, 16);
    }
    private void PublishFrame()
    {
        if (_disposed || _player == null) return;
        Interlocked.Exchange(ref _position, TimeSpan.FromMilliseconds(Math.Max(0, _player.CurrentPosition)).Ticks);
        _frames.NotifyFrameAvailable(Position, _width, _height);
        Event?.Invoke(new VideoSessionEvent.FrameAvailable(Position));
    }
    private void Fail(string message)
    { _error = message; _state = VideoSessionState.Error; _buffering = false; Event?.Invoke(new VideoSessionEvent.Error(message, null)); }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var handler = _handler;
        _handler = null;
        handler?.Post(() =>
        {
            _player?.Release();
            _player?.Dispose();
            _player = null;
            handler.RemoveCallbacksAndMessages(null);
            _thread.QuitSafely();
            handler.Dispose();
            _thread.Dispose();
        });
        _frames.Dispose();
    }
}
