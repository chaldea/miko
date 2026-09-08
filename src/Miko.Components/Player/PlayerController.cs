using Miko.Platform.Video;

namespace Miko.Components.Player;

/// <summary>UI-thread playback controls. The engine, not this controller, owns the session.</summary>
public sealed class PlayerController : IDisposable
{
    private IVideoSession? _session;
    private bool? _playRequested;
    private TimeSpan? _pendingSeek;
    private bool _disposed;
    public VideoSessionState State { get; private set; } = VideoSessionState.Idle;
    public TimeSpan Position { get; private set; }
    public TimeSpan Duration { get; private set; }
    public bool IsBuffering { get; private set; }
    public string? Error { get; private set; }
    public float Volume { get; private set; } = 1;
    public bool Muted { get; private set; }
    public float PlaybackRate { get; private set; } = 1;
    public bool Loop { get; private set; }
    public bool CanSeek => Duration > TimeSpan.Zero && State is not (VideoSessionState.Idle or VideoSessionState.Loading or VideoSessionState.Error);
    public IReadOnlyList<VideoTimeRange> BufferedRanges => _session?.BufferedRanges ?? Array.Empty<VideoTimeRange>();
    public event Action? Changed;

    public void Attach(IVideoSession? session)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _session = session;
        Error = null;
        if (session != null)
        {
            session.SetVolume(Volume);
            session.SetMuted(Muted);
            session.SetPlaybackRate(PlaybackRate);
            session.SetLoop(Loop);
            if (_playRequested == true) session.Play();
            else if (_playRequested == false) session.Pause();
        }
        Refresh();
    }

    public void Refresh()
    {
        if (_disposed) return;
        State = _session?.State ?? VideoSessionState.Idle;
        Position = _session?.Position ?? TimeSpan.Zero;
        Duration = _session?.Duration ?? TimeSpan.Zero;
        IsBuffering = _session?.IsBuffering ?? false;
        if (_session?.ErrorMessage is { } error) Error = error;
        if (State == VideoSessionState.Error) Error ??= "Unable to play this source.";
        if (Error != null) { State = VideoSessionState.Error; IsBuffering = false; }
        if (_pendingSeek is { } seek && CanSeek)
        {
            _pendingSeek = null;
            Seek(seek);
            return;
        }
        Changed?.Invoke();
    }

    public void HandleEvent(VideoSessionEvent value)
    {
        if (_disposed) return;
        Refresh();
        if (value is VideoSessionEvent.Error error)
        {
            Error = error.Message;
            State = VideoSessionState.Error;
            IsBuffering = false;
        }
        else if (value is VideoSessionEvent.Buffering buffering) IsBuffering = buffering.IsBuffering;
        Changed?.Invoke();
    }

    public void Play()
    {
        _playRequested = true;
        if (_session?.State == VideoSessionState.Ended) _session.Seek(TimeSpan.Zero);
        _session?.Play();
        Refresh();
    }

    public void Pause() { _playRequested = false; _session?.Pause(); Refresh(); }
    public void TogglePlayback() { if (State == VideoSessionState.Playing) Pause(); else Play(); }
    public void Seek(TimeSpan position)
    {
        if (!CanSeek) { _pendingSeek = position; return; }
        position = TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, Duration.Ticks));
        _session!.Seek(position);
        Position = position;
        Changed?.Invoke();
    }

    public void SetVolume(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        Volume = Math.Clamp(value, 0, 1);
        _session?.SetVolume(Volume);
        Changed?.Invoke();
    }
    public void SetMuted(bool value) { Muted = value; _session?.SetMuted(value); Changed?.Invoke(); }
    public void SetLoop(bool value) { Loop = value; _session?.SetLoop(value); Changed?.Invoke(); }
    public void SetPlaybackRate(float value)
    {
        if (!float.IsFinite(value) || value < .25f || value > 4) throw new ArgumentOutOfRangeException(nameof(value));
        PlaybackRate = value;
        _session?.SetPlaybackRate(value);
        Changed?.Invoke();
    }

    public void Reset(bool autoPlay)
    {
        _session = null;
        _pendingSeek = null;
        _playRequested = autoPlay;
        Error = null;
        Refresh();
    }

    public void Dispose() { _disposed = true; _session = null; Changed = null; }
}
