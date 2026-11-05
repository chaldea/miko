using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Miko.Components.Webcam.Streaming;

/// <summary>
/// An RTSP 1.0 control connection (RFC 2326) that also carries the media, interleaved over the same
/// TCP connection (<c>$</c> frames). TCP interleaving avoids NAT and firewall trouble and packet loss,
/// which is what cameras on a LAN or behind a router need.
/// </summary>
/// <remarks>
/// Not thread-safe: requests and reads come from the one thread that drives the source. Reads abort
/// when the token passed to them is cancelled, when nothing arrives for <see cref="ReadTimeout"/>,
/// or when the connection is disposed.
/// </remarks>
internal sealed class RtspConnection : IAsyncDisposable
{
    private const string UserAgent = "Miko RTSP";
    private const int MaxHeaderBytes = 64 * 1024;

    private readonly Uri _uri;
    private readonly string? _user;
    private readonly string? _password;
    private readonly CancellationTokenSource _abort = new();
    private readonly Timer _watchdog;
    private Socket? _socket;
    private Stream? _stream;
    private byte[] _buffer = new byte[256 * 1024];
    private int _start, _end;
    private int _cseq;
    private long _lastReceive = Environment.TickCount64;
    private volatile bool _timedOut;
    private CancellationToken _registeredToken;
    private CancellationTokenRegistration _registration;
    private DigestChallenge? _digest;
    private bool _basic;
    private int _nonceCount;

    public RtspConnection(Uri uri)
    {
        _uri = uri;
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            _user = Uri.UnescapeDataString(parts[0]);
            _password = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
        }
        // The request URI never carries the credentials.
        RequestUri = new UriBuilder(uri) { UserName = "", Password = "" }.Uri.ToString();
        _watchdog = new Timer(_ => CheckTimeout(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>The URI without user info, as sent in requests.</summary>
    public string RequestUri { get; }

    /// <summary>How long a read waits for data before the connection counts as dead.</summary>
    public TimeSpan ReadTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>The session id once SETUP succeeded.</summary>
    public string? Session { get; set; }

    public long BytesReceived { get; private set; }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        int port = _uri.Port > 0 ? _uri.Port : _uri.Scheme == "rtsps" ? 322 : 554;
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        _socket = socket;
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _abort.Token))
        {
            timeout.CancelAfter(ConnectTimeout);
            try
            {
                await socket.ConnectAsync(_uri.DnsSafeHost, port, timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Could not connect to {_uri.Host}:{port} within {ConnectTimeout.TotalSeconds:0}s.");
            }
        }
        Stream stream = new NetworkStream(socket, ownsSocket: true);
        if (_uri.Scheme == "rtsps")
        {
            var ssl = new SslStream(stream, leaveInnerStreamOpen: false);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = _uri.DnsSafeHost }, cancellationToken).ConfigureAwait(false);
            stream = ssl;
        }
        _stream = stream;
        _lastReceive = Environment.TickCount64;
        _watchdog.Change(1000, 1000);
    }

    /// <summary>Sends a request and waits for its response, answering one authentication challenge.</summary>
    public async Task<RtspResponse> SendAsync(string method, string uri, IReadOnlyDictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        for (int attempt = 0; ; attempt++)
        {
            int cseq = await WriteRequestAsync(method, uri, headers, cancellationToken).ConfigureAwait(false);
            RtspResponse response;
            // Interleaved data may precede the response (a keep-alive sent while playing); skip it.
            while (true)
            {
                var item = await ReadItemAsync(cancellationToken).ConfigureAwait(false);
                if (item.Response is { } r && (r.CSeq == cseq || r.CSeq < 0)) { response = r; break; }
            }
            if (response.StatusCode == 401 && attempt == 0 && _user != null && Authorize(response))
                continue;
            if (response.StatusCode == 401)
                throw new RtspException(401, _user == null
                    ? "The camera requires a user name and password (rtsp://user:password@host/...)."
                    : "The camera rejected the user name or password.");
            return response;
        }
    }

    /// <summary>Sends a request without waiting for the response (keep-alives while playing).</summary>
    public Task SendWithoutResponseAsync(string method, string uri, CancellationToken cancellationToken) =>
        WriteRequestAsync(method, uri, null, cancellationToken);

    /// <summary>
    /// Reads the next interleaved packet or response. The packet's bytes are valid until the next read.
    /// </summary>
    public async ValueTask<RtspItem> ReadItemAsync(CancellationToken cancellationToken)
    {
        Register(cancellationToken);
        while (true)
        {
            await FillAsync(1, cancellationToken).ConfigureAwait(false);
            byte first = _buffer[_start];
            if (first == (byte)'$')
            {
                await FillAsync(4, cancellationToken).ConfigureAwait(false);
                int channel = _buffer[_start + 1];
                int length = (_buffer[_start + 2] << 8) | _buffer[_start + 3];
                await FillAsync(4 + length, cancellationToken).ConfigureAwait(false);
                var payload = new ReadOnlyMemory<byte>(_buffer, _start + 4, length);
                _start += 4 + length;
                return new RtspItem(channel, payload, null);
            }
            if (first == (byte)'R')
            {
                await FillAsync(5, cancellationToken).ConfigureAwait(false);
                if (_buffer.AsSpan(_start, 5).SequenceEqual("RTSP/"u8))
                    return new RtspItem(-1, default, await ReadResponseAsync(cancellationToken).ConfigureAwait(false));
            }
            // Out of sync (a server request, stray bytes): skip to the next frame or response.
            _start++;
        }
    }

    private async Task<int> WriteRequestAsync(string method, string uri, IReadOnlyDictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        var stream = _stream ?? throw new InvalidOperationException("Not connected.");
        int cseq = ++_cseq;
        var text = new StringBuilder();
        text.Append(method).Append(' ').Append(uri).Append(" RTSP/1.0\r\n");
        text.Append("CSeq: ").Append(cseq.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
        text.Append("User-Agent: ").Append(UserAgent).Append("\r\n");
        var authorization = BuildAuthorization(method, uri);
        if (authorization != null) text.Append("Authorization: ").Append(authorization).Append("\r\n");
        if (Session != null) text.Append("Session: ").Append(Session).Append("\r\n");
        if (headers != null)
            foreach (var (name, value) in headers)
                text.Append(name).Append(": ").Append(value).Append("\r\n");
        text.Append("\r\n");
        var bytes = Encoding.ASCII.GetBytes(text.ToString());
        Register(cancellationToken);
        try
        {
            await stream.WriteAsync(bytes, _abort.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            ThrowAborted(cancellationToken);
        }
        return cseq;
    }

    private async Task<RtspResponse> ReadResponseAsync(CancellationToken cancellationToken)
    {
        int headerEnd;
        while ((headerEnd = _buffer.AsSpan(_start, _end - _start).IndexOf("\r\n\r\n"u8)) < 0)
        {
            if (_end - _start > MaxHeaderBytes) throw new RtspException(0, "The RTSP response header is too long.");
            await FillAsync(_end - _start + 1, cancellationToken).ConfigureAwait(false);
        }
        var head = Encoding.UTF8.GetString(_buffer, _start, headerEnd);
        _start += headerEnd + 4;

        var lines = head.Split("\r\n");
        var status = lines[0].Split(' ', 3);
        int code = status.Length > 1 && int.TryParse(status[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) ? c : 0;
        var headers = new List<KeyValuePair<string, string>>();
        foreach (var line in lines.Skip(1))
        {
            int colon = line.IndexOf(':');
            if (colon > 0) headers.Add(new(line[..colon].Trim(), line[(colon + 1)..].Trim()));
        }
        var response = new RtspResponse(code, status.Length > 2 ? status[2] : "", headers, "");
        int length = int.TryParse(response.Header("Content-Length"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
        if (length > 0)
        {
            await FillAsync(length, cancellationToken).ConfigureAwait(false);
            response = response with { Body = Encoding.UTF8.GetString(_buffer, _start, length) };
            _start += length;
        }
        return response;
    }

    // Makes `count` bytes available at _start.
    private async ValueTask FillAsync(int count, CancellationToken cancellationToken)
    {
        if (_end - _start >= count) return;
        var stream = _stream ?? throw new InvalidOperationException("Not connected.");
        if (count > _buffer.Length)
        {
            var larger = new byte[Math.Max(count, _buffer.Length * 2)];
            Buffer.BlockCopy(_buffer, _start, larger, 0, _end - _start);
            _end -= _start;
            _start = 0;
            _buffer = larger;
        }
        else if (_start + count > _buffer.Length)
        {
            Buffer.BlockCopy(_buffer, _start, _buffer, 0, _end - _start);
            _end -= _start;
            _start = 0;
        }
        while (_end - _start < count)
        {
            int read;
            try
            {
                read = await stream.ReadAsync(_buffer.AsMemory(_end), _abort.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                ThrowAborted(cancellationToken);
                throw;
            }
            catch (IOException) when (_abort.IsCancellationRequested)
            {
                ThrowAborted(cancellationToken);
                throw;
            }
            if (read == 0) throw new EndOfStreamException("The camera closed the connection.");
            _end += read;
            BytesReceived += read;
            _lastReceive = Environment.TickCount64;
        }
    }

    private void ThrowAborted(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_timedOut) throw new TimeoutException($"No data from the camera for {ReadTimeout.TotalSeconds:0}s.");
        throw new ObjectDisposedException(nameof(RtspConnection));
    }

    // One registration per token, not per read: reading a frame must not allocate.
    private void Register(CancellationToken cancellationToken)
    {
        if (cancellationToken == _registeredToken) return;
        _registration.Dispose();
        _registeredToken = cancellationToken;
        _registration = cancellationToken.CanBeCanceled
            ? cancellationToken.Register(static s => ((RtspConnection)s!).Abort(), this)
            : default;
    }

    private void CheckTimeout()
    {
        if (Environment.TickCount64 - Interlocked.Read(ref _lastReceive) > ReadTimeout.TotalMilliseconds)
        {
            _timedOut = true;
            Abort();
        }
    }

    private void Abort()
    {
        try { _abort.Cancel(); } catch (ObjectDisposedException) { }
    }

    // ---- authentication ---------------------------------------------------

    private bool Authorize(RtspResponse challenge)
    {
        var values = challenge.Headers.Where(h => h.Key.Equals("WWW-Authenticate", StringComparison.OrdinalIgnoreCase)).Select(h => h.Value).ToList();
        var digest = values.FirstOrDefault(v => v.StartsWith("Digest", StringComparison.OrdinalIgnoreCase));
        if (digest != null)
        {
            _digest = DigestChallenge.Parse(digest);
            _nonceCount = 0;
            return _digest.Nonce != null;
        }
        if (values.Any(v => v.StartsWith("Basic", StringComparison.OrdinalIgnoreCase)))
        {
            // Basic sends the password in the clear: only after the server asked for it.
            _basic = true;
            return true;
        }
        return false;
    }

    private string? BuildAuthorization(string method, string uri)
    {
        if (_user == null) return null;
        if (_digest is { Nonce: { } nonce } digest)
        {
            string ha1 = Md5($"{_user}:{digest.Realm}:{_password}");
            string ha2 = Md5($"{method}:{uri}");
            var header = new StringBuilder();
            header.Append($"Digest username=\"{_user}\", realm=\"{digest.Realm}\", nonce=\"{nonce}\", uri=\"{uri}\"");
            if (digest.QopAuth)
            {
                string nc = (++_nonceCount).ToString("x8", CultureInfo.InvariantCulture);
                string cnonce = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));
                header.Append($", qop=auth, nc={nc}, cnonce=\"{cnonce}\", response=\"{Md5($"{ha1}:{nonce}:{nc}:{cnonce}:auth:{ha2}")}\"");
            }
            else
            {
                header.Append($", response=\"{Md5($"{ha1}:{nonce}:{ha2}")}\"");
            }
            if (digest.Opaque != null) header.Append($", opaque=\"{digest.Opaque}\"");
            return header.ToString();
        }
        if (_basic) return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_user}:{_password}"));
        return null;
    }

#pragma warning disable CA5351 // RTSP Digest authentication is defined over MD5.
    private static string Md5(string text) => Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(text)));
#pragma warning restore CA5351

    public async ValueTask DisposeAsync()
    {
        _watchdog.Dispose();
        Abort();
        _registration.Dispose();
        if (_stream != null) await _stream.DisposeAsync().ConfigureAwait(false);
        else _socket?.Dispose();
        _abort.Dispose();
    }

    private sealed class DigestChallenge
    {
        public string? Realm { get; private set; }
        public string? Nonce { get; private set; }
        public string? Opaque { get; private set; }
        public bool QopAuth { get; private set; }

        public static DigestChallenge Parse(string header)
        {
            var challenge = new DigestChallenge();
            foreach (var (name, value) in ParseParameters(header["Digest".Length..]))
            {
                switch (name.ToLowerInvariant())
                {
                    case "realm": challenge.Realm = value; break;
                    case "nonce": challenge.Nonce = value; break;
                    case "opaque": challenge.Opaque = value; break;
                    case "qop": challenge.QopAuth = value.Split(',').Any(q => q.Trim() == "auth"); break;
                }
            }
            return challenge;
        }

        private static IEnumerable<(string, string)> ParseParameters(string text)
        {
            int i = 0;
            while (i < text.Length)
            {
                while (i < text.Length && (text[i] == ' ' || text[i] == ',')) i++;
                int eq = text.IndexOf('=', i);
                if (eq < 0) yield break;
                string name = text[i..eq].Trim();
                i = eq + 1;
                string value;
                if (i < text.Length && text[i] == '"')
                {
                    int close = text.IndexOf('"', i + 1);
                    if (close < 0) close = text.Length;
                    value = text[(i + 1)..close];
                    i = close + 1;
                }
                else
                {
                    int comma = text.IndexOf(',', i);
                    if (comma < 0) comma = text.Length;
                    value = text[i..comma].Trim();
                    i = comma;
                }
                yield return (name, value);
            }
        }
    }
}

/// <summary>An RTSP response.</summary>
internal sealed record RtspResponse(int StatusCode, string Reason, IReadOnlyList<KeyValuePair<string, string>> Headers, string Body)
{
    public string? Header(string name) =>
        Headers.FirstOrDefault(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;

    public int CSeq => int.TryParse(Header("CSeq"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : -1;

    public bool IsSuccess => StatusCode is >= 200 and < 300;
}

/// <summary>One item read off the connection: an interleaved packet (channel ≥ 0) or a response.</summary>
internal readonly record struct RtspItem(int Channel, ReadOnlyMemory<byte> Packet, RtspResponse? Response);

/// <summary>
/// An RTSP failure with the server's status code (0 for protocol errors). Authentication failures and
/// missing streams (401, 403, 404) are permanent: the session reports them instead of reconnecting.
/// </summary>
public sealed class RtspException(int statusCode, string message)
    : Miko.Platform.Video.VideoStreamException(message, isPermanent: statusCode is 401 or 403 or 404)
{
    public int StatusCode { get; } = statusCode;
}
