using System.Globalization;
using Miko.Platform.Video;

namespace Miko.Components.Webcam.Streaming;

/// <summary>
/// The video track of an RTSP stream (<c>rtsp://user:password@host[:port]/path</c>) as an
/// <see cref="IEncodedVideoSource"/>: DESCRIBE → SETUP (RTP over the RTSP TCP connection) → PLAY,
/// then H.264 / H.265 access units reassembled from the RTP packets. Digest and Basic
/// authentication; <c>rtsps://</c> runs over TLS. Audio is not set up.
/// </summary>
public sealed class RtspVideoSource : IEncodedVideoSource
{
    private static readonly IReadOnlyDictionary<string, string> s_describe = new Dictionary<string, string> { ["Accept"] = "application/sdp" };

    private readonly RtspConnection _connection;
    private RtpVideoDepacketizer? _depacketizer;
    private RtpVideoDepacketizer.AccessUnit _returned;
    private int _videoChannel;
    private string _controlUri = "";
    private TimeSpan _keepAlive = TimeSpan.FromSeconds(25);
    private long _lastKeepAlive;
    private bool _playing;

    public RtspVideoSource(Uri uri) : this(uri, null)
    {
    }

    /// <param name="uri">The stream.</param>
    /// <param name="readTimeout">How long the camera may stay silent before the connection counts as dead (10 s by default).</param>
    public RtspVideoSource(Uri uri, TimeSpan? readTimeout)
    {
        if (uri.Scheme is not ("rtsp" or "rtsps"))
            throw new ArgumentException("Expected an rtsp:// or rtsps:// URI.", nameof(uri));
        _connection = readTimeout is { } timeout ? new RtspConnection(uri) { ReadTimeout = timeout } : new RtspConnection(uri);
    }

    public long BytesReceived => _connection.BytesReceived;

    /// <summary>The codec negotiated by <see cref="OpenAsync"/>.</summary>
    public VideoCodec? Codec { get; private set; }

    public async Task<EncodedVideoStreamInfo> OpenAsync(CancellationToken cancellationToken)
    {
        await _connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

        var describe = await _connection.SendAsync("DESCRIBE", _connection.RequestUri, s_describe, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(describe, "DESCRIBE");
        var sdp = SdpSession.Parse(describe.Body);
        var media = sdp.Media.FirstOrDefault(m => m.Type == "video" && m.Encoding is "H264" or "H265" or "HEVC")
            ?? throw new RtspException(0, sdp.Media.Any(m => m.Type == "video")
                ? $"The camera streams {sdp.Media.First(m => m.Type == "video").Encoding}; only H.264 and H.265 are supported."
                : "The stream has no video track.");
        var codec = media.Encoding == "H264" ? VideoCodec.H264 : VideoCodec.H265;

        string baseUri = describe.Header("Content-Base") ?? describe.Header("Content-Location") ?? _connection.RequestUri;
        _controlUri = ResolveControl(baseUri, media.Control);

        var setup = await _connection.SendAsync("SETUP", _controlUri,
            new Dictionary<string, string> { ["Transport"] = "RTP/AVP/TCP;unicast;interleaved=0-1" }, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(setup, "SETUP");
        _videoChannel = ParseInterleavedChannel(setup.Header("Transport")) ?? 0;
        var session = setup.Header("Session") ?? throw new RtspException(0, "The camera did not return a session.");
        var parts = session.Split(';');
        _connection.Session = parts[0].Trim();
        foreach (var part in parts.Skip(1))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("timeout", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(kv[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds) && seconds > 2)
                _keepAlive = TimeSpan.FromSeconds(seconds * 0.5);
        }

        string playUri = sdp.Control is { Length: > 0 } control && control != "*" ? ResolveControl(baseUri, control) : baseUri;
        var play = await _connection.SendAsync("PLAY", playUri,
            new Dictionary<string, string> { ["Range"] = "npt=0.000-" }, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(play, "PLAY");
        _playing = true;
        _lastKeepAlive = Environment.TickCount64;

        _depacketizer = new RtpVideoDepacketizer(codec, media.PayloadType, media.ClockRate, media.GetParameterSets(codec));
        Codec = codec;
        return new EncodedVideoStreamInfo(codec);
    }

    public async ValueTask<EncodedVideoFrame?> ReadFrameAsync(CancellationToken cancellationToken)
    {
        var depacketizer = _depacketizer ?? throw new InvalidOperationException("OpenAsync first.");
        // The previous frame's buffer is free again once the caller asks for the next one.
        if (_returned.Buffer != null)
        {
            depacketizer.Recycle(_returned);
            _returned = default;
        }

        while (depacketizer.CompletedCount == 0)
        {
            if (Environment.TickCount64 - _lastKeepAlive > _keepAlive.TotalMilliseconds)
            {
                // Without a request on the session the camera ends it after its timeout.
                _lastKeepAlive = Environment.TickCount64;
                await _connection.SendWithoutResponseAsync("GET_PARAMETER", _controlUri, cancellationToken).ConfigureAwait(false);
            }
            var item = await _connection.ReadItemAsync(cancellationToken).ConfigureAwait(false);
            if (item.Channel == _videoChannel) depacketizer.Push(item.Packet.Span);
            else if (item.Response is { StatusCode: 454 }) throw new RtspException(454, "The camera ended the session.");
        }

        _returned = depacketizer.Dequeue();
        return new EncodedVideoFrame(_returned.Data, _returned.Timestamp, _returned.IsKeyFrame);
    }

    public async ValueTask DisposeAsync()
    {
        if (_playing)
        {
            _playing = false;
            try
            {
                // Best effort: frees the camera's stream slot now instead of at its session timeout.
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await _connection.SendWithoutResponseAsync("TEARDOWN", _controlUri, timeout.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    internal static string ResolveControl(string baseUri, string? control)
    {
        if (string.IsNullOrEmpty(control) || control == "*") return baseUri;
        if (control.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) || control.StartsWith("rtsps://", StringComparison.OrdinalIgnoreCase))
            return control;
        return baseUri.EndsWith('/') ? baseUri + control : baseUri + "/" + control;
    }

    private static int? ParseInterleavedChannel(string? transport)
    {
        if (transport == null) return null;
        foreach (var part in transport.Split(';'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim() == "interleaved")
            {
                var first = kv[1].Split('-')[0];
                if (int.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out int channel)) return channel;
            }
        }
        return null;
    }

    private static void EnsureSuccess(RtspResponse response, string method)
    {
        if (!response.IsSuccess)
            throw new RtspException(response.StatusCode, $"{method} failed: {response.StatusCode} {response.Reason}".TrimEnd());
    }
}

/// <summary>
/// Plays <c>rtsp://</c> and <c>rtsps://</c> sources in <c>&lt;video&gt;</c> through the platform's
/// stream decoder (see <see cref="IVideoStreamDecoderFactory"/>). Registered by
/// <c>AddMikoRtsp()</c> / <c>AddMikoWebcam()</c>.
/// </summary>
public sealed class RtspVideoSourceFactory : IEncodedVideoSourceFactory
{
    public bool CanOpen(VideoSourceDescriptor source) =>
        source.Uri.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) ||
        source.Uri.StartsWith("rtsps://", StringComparison.OrdinalIgnoreCase);

    public IEncodedVideoSource Open(VideoSourceDescriptor source) => new RtspVideoSource(new Uri(source.Uri));
}
