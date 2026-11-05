using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Graphics;

namespace Miko.Platform.Video;

/// <summary>
/// A live <c>&lt;video&gt;</c> session: an <see cref="IEncodedVideoSource"/> read on a background task
/// and decoded by the platform decoder (<see cref="IVideoStreamDecoderFactory"/>). The engine creates
/// it for sources an <see cref="IEncodedVideoSourceFactory"/> recognises.
/// </summary>
/// <remarks>
/// <para>
/// Live semantics: the duration is zero (players show "live" and do not seek), frames are shown as
/// soon as they are decoded, and <see cref="Pause"/> disconnects — the last picture stays up and
/// <see cref="Play"/> reconnects, resuming at the next key frame. Without <c>AutoPlay</c> nothing
/// connects until <see cref="Play"/>.
/// </para>
/// <para>
/// A dropped connection is retried <see cref="MaxReconnectAttempts"/> times with a growing delay
/// (buffering meanwhile) before the session reports an error. One decoder serves every connection
/// of the session, so the picture never goes blank between them.
/// </para>
/// </remarks>
public sealed class EncodedVideoSession : IVideoSession
{
    /// <summary>Reconnects after a dropped stream before the session gives up.</summary>
    public const int MaxReconnectAttempts = 5;

    private static readonly TimeSpan s_firstRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_maxRetryDelay = TimeSpan.FromSeconds(8);

    private readonly VideoSourceDescriptor _source;
    private readonly IEncodedVideoSourceFactory _sources;
    private readonly IVideoStreamDecoderFactory _decoders;
    private readonly ILogger _logger;
    private readonly TimeProvider _time;
    private readonly object _lock = new();

    private volatile IVideoStreamDecoder? _decoder;
    private CancellationTokenSource? _connection;
    private Task _loop = Task.CompletedTask;
    private volatile VideoSessionState _state = VideoSessionState.Idle;
    private volatile string? _error;
    private volatile bool _disposed;
    private int _width, _height;
    private long _bytesBefore;
    private IEncodedVideoSource? _current;
    private long _framesDecoded;
    private long _startTimestamp = -1;
    private volatile bool _connectionDecoded;

    public EncodedVideoSession(
        VideoSourceDescriptor source,
        IEncodedVideoSourceFactory sources,
        IVideoStreamDecoderFactory decoders,
        VideoSessionOptions options,
        ILogger? logger = null,
        TimeProvider? timeProvider = null)
    {
        _source = source;
        _sources = sources;
        _decoders = decoders;
        _logger = logger ?? NullLogger.Instance;
        _time = timeProvider ?? TimeProvider.System;
        if (options.AutoPlay) Play();
        else _state = VideoSessionState.Paused;
    }

    public event Action<VideoSessionEvent>? Event;

    public VideoSessionState State => _state;

    /// <summary>Always zero: a live stream has no duration.</summary>
    public TimeSpan Duration => TimeSpan.Zero;

    /// <summary>Time since the first frame was shown.</summary>
    public TimeSpan Position => _startTimestamp < 0 ? TimeSpan.Zero : _time.GetElapsedTime(_startTimestamp);

    public bool IsBuffering => _state == VideoSessionState.Loading;

    public string? ErrorMessage => _error;

    public int VideoWidth => Volatile.Read(ref _width);

    public int VideoHeight => Volatile.Read(ref _height);

    /// <summary>The codec of the stream, once connected.</summary>
    public VideoCodec? Codec { get; private set; }

    /// <summary>Bytes received over every connection of this session.</summary>
    public long BytesReceived
    {
        get
        {
            lock (_lock) return _bytesBefore + (_current?.BytesReceived ?? 0);
        }
    }

    /// <summary>Frames handed to the decoder.</summary>
    public long FramesDecoded => Interlocked.Read(ref _framesDecoded);

    public IVideoFrameSource FrameSource => _disposed ? EmptyFrameSource.Instance : _decoder?.FrameSource ?? EmptyFrameSource.Instance;

    /// <summary>Connects (or reconnects after <see cref="Pause"/>).</summary>
    public void Play()
    {
        lock (_lock)
        {
            if (_disposed || _connection != null) return;
            _error = null;
            _state = VideoSessionState.Loading;
            var connection = new CancellationTokenSource();
            _connection = connection;
            var previous = _loop;
            _loop = Task.Run(() => RunAsync(previous, connection.Token));
        }
        Raise(new VideoSessionEvent.Buffering(true));
    }

    /// <summary>Disconnects; the last decoded picture stays up.</summary>
    public void Pause()
    {
        lock (_lock)
        {
            if (_connection == null) return;
            _connection.Cancel();
            _connection = null;
            if (!_disposed) _state = VideoSessionState.Paused;
        }
    }

    // A live stream has no timeline, no rate and (for now) no audio track.
    public void Seek(TimeSpan position) { }
    public void SetVolume(float volume) { }
    public void SetMuted(bool muted) { }
    public void SetPlaybackRate(float rate) { }
    public void SetLoop(bool loop) { }

    public void Dispose()
    {
        Task loop;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _connection?.Cancel();
            _connection = null;
            loop = _loop;
        }
        // The decoder goes once the read loop has let go of it (Decode must not race Dispose);
        // the render thread stops asking for frames as soon as _disposed is set.
        loop.ContinueWith(_ =>
        {
            var decoder = _decoder;
            _decoder = null;
            if (decoder != null)
            {
                decoder.Event -= OnDecoderEvent;
                decoder.Dispose();
            }
        }, TaskScheduler.Default);
    }

    private async Task RunAsync(Task previous, CancellationToken cancellation)
    {
        // A Pause → Play pair must not run two loops on one decoder.
        try { await previous.ConfigureAwait(false); } catch { }

        int failures = 0;
        while (!cancellation.IsCancellationRequested)
        {
            _connectionDecoded = false;
            try
            {
                await ConnectAndDecodeAsync(cancellation).ConfigureAwait(false);
                if (cancellation.IsCancellationRequested) return;
                throw new EndOfStreamException("The stream ended.");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                return;
            }
            catch (VideoStreamException ex) when (ex.IsPermanent)
            {
                Fail(ex.Message, ex);
                return;
            }
            catch (Exception ex) when (ex is not FatalStreamException)
            {
                // A stream that worked for a while and then dropped gets the full retry budget again.
                if (_connectionDecoded) failures = 0;
                failures++;
                if (failures > MaxReconnectAttempts)
                {
                    Fail(ex.Message, ex);
                    return;
                }
                _logger.LogWarning(ex, "Live stream {Uri} dropped; reconnecting ({Attempt}/{Max})",
                    Redact(_source.Uri), failures, MaxReconnectAttempts);
                _state = VideoSessionState.Loading;
                Raise(new VideoSessionEvent.Buffering(true));
                var delay = TimeSpan.FromTicks(Math.Min(s_maxRetryDelay.Ticks, s_firstRetryDelay.Ticks << (failures - 1)));
                try { await Task.Delay(delay, _time, cancellation).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }
            catch (FatalStreamException ex)
            {
                Fail(ex.Message, ex.InnerException);
                return;
            }
        }
    }

    // Runs one connection until the stream ends (returns) or fails (throws); _connectionDecoded
    // records whether it got as far as decoding.
    private async Task ConnectAndDecodeAsync(CancellationToken cancellation)
    {
        var source = _sources.Open(_source);
        lock (_lock) _current = source;
        try
        {
            var info = await source.OpenAsync(cancellation).ConfigureAwait(false);
            var decoder = EnsureDecoder(info.Codec);

            bool waitingForKeyFrame = true;
            while (true)
            {
                var next = await source.ReadFrameAsync(cancellation).ConfigureAwait(false);
                if (next is not { } frame) return;
                // A decoder joining (or rejoining) the stream needs a random access point first.
                if (waitingForKeyFrame && !frame.IsKeyFrame) continue;
                waitingForKeyFrame = false;
                cancellation.ThrowIfCancellationRequested();
                decoder.Decode(frame);
                Interlocked.Increment(ref _framesDecoded);
                _connectionDecoded = true;
            }
        }
        finally
        {
            lock (_lock)
            {
                _bytesBefore += source.BytesReceived;
                _current = null;
            }
            try { await source.DisposeAsync().ConfigureAwait(false); }
            catch (Exception ex) { _logger.LogDebug(ex, "Closing live stream {Uri} failed", Redact(_source.Uri)); }
        }
    }

    private IVideoStreamDecoder EnsureDecoder(VideoCodec codec)
    {
        if (_decoder is { } existing)
        {
            if (Codec == codec) return existing;
            throw new FatalStreamException($"The stream changed codec from {Codec} to {codec}.");
        }
        if (!_decoders.CanDecode(codec))
            throw new FatalStreamException($"This device has no {codec} decoder.");
        var decoder = _decoders.CreateDecoder(codec);
        decoder.Event += OnDecoderEvent;
        Codec = codec;
        _decoder = decoder;
        // The render thread asks the new frame source for pictures only when it paints the video,
        // and a decoder may need that first (Android creates the decoder's output surface on the
        // render thread). Hosts that draw on demand paint on an event: give them one.
        Raise(new VideoSessionEvent.Buffering(true));
        return decoder;
    }

    private void OnDecoderEvent(VideoSessionEvent evt)
    {
        if (_disposed) return;
        switch (evt)
        {
            case VideoSessionEvent.Loaded loaded:
                Volatile.Write(ref _width, loaded.Width);
                Volatile.Write(ref _height, loaded.Height);
                Raise(new VideoSessionEvent.Loaded(loaded.Width, loaded.Height, TimeSpan.Zero));
                break;
            case VideoSessionEvent.FrameAvailable available:
                if (_state == VideoSessionState.Loading)
                {
                    lock (_lock)
                    {
                        if (_connection == null) break;
                        _state = VideoSessionState.Playing;
                    }
                    if (_startTimestamp < 0) _startTimestamp = _time.GetTimestamp();
                    Raise(new VideoSessionEvent.Buffering(false));
                }
                Raise(available);
                break;
            case VideoSessionEvent.Error error:
                // A broken decoder does not heal by reconnecting.
                lock (_lock)
                {
                    _connection?.Cancel();
                    _connection = null;
                }
                Fail(error.Message, error.Cause);
                break;
            default:
                Raise(evt);
                break;
        }
    }

    private void Fail(string message, Exception? cause)
    {
        if (_disposed) return;
        _logger.LogWarning(cause, "Live stream {Uri} failed: {Message}", Redact(_source.Uri), message);
        _error = message;
        _state = VideoSessionState.Error;
        lock (_lock) _connection = null;
        Raise(new VideoSessionEvent.Error(message, cause));
    }

    private void Raise(VideoSessionEvent evt)
    {
        if (!_disposed) Event?.Invoke(evt);
    }

    /// <summary>Strips user info (camera passwords) from a URI before it reaches a log.</summary>
    internal static string Redact(string uri)
    {
        int scheme = uri.IndexOf("://", StringComparison.Ordinal);
        if (scheme < 0) return uri;
        int at = uri.IndexOf('@', scheme + 3);
        int slash = uri.IndexOf('/', scheme + 3);
        return at < 0 || (slash >= 0 && at > slash) ? uri : uri[..(scheme + 3)] + "***@" + uri[(at + 1)..];
    }

    /// <summary>A failure reconnecting cannot fix (no decoder, codec change).</summary>
    private sealed class FatalStreamException(string message, Exception? inner = null) : Exception(message, inner);

    private sealed class EmptyFrameSource : IVideoFrameSource
    {
        public static readonly EmptyFrameSource Instance = new();
        public Image? AcquireCurrentFrame(GraphicsDevice device) => null;
        public void ReleaseCurrentFrame() { }
    }
}
