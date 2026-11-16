using System.Buffers.Binary;
using System.Globalization;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Miko.Components.Vnc.Protocol;

/// <summary>How to reach and talk to a VNC server.</summary>
/// <param name="Host">TCP hostname or a complete ws:// / wss:// URL (in which case Port is ignored).</param>
/// <param name="Port">TCP port, ignored for WebSocket URLs.</param>
public sealed record VncConnectOptions(string Host, int Port = 5900)
{
    /// <summary>The VNC password, or null to ask <see cref="PasswordProvider"/> when the server wants one.</summary>
    public string? Password { get; init; }

    /// <summary>Asked for the password when the server needs one and <see cref="Password"/> is null; null declines.</summary>
    public Func<CancellationToken, Task<string?>>? PasswordProvider { get; init; }

    /// <summary>Let other viewers stay connected (false asks the server to disconnect them).</summary>
    public bool Shared { get; init; } = true;

    /// <summary>TCP frame encodings in order of preference. WebSocket uses Raw/CopyRect plus desktop/cursor extensions.</summary>
    public IReadOnlyList<int> Encodings { get; init; } = DefaultEncodings;

    /// <summary>Tight JPEG quality 0-9, or null for lossless only. Needs <see cref="JpegDecoder"/>.</summary>
    public int? JpegQuality { get; init; } = 7;

    /// <summary>Compression effort the server should spend, 0-9.</summary>
    public int CompressLevel { get; init; } = 2;

    /// <summary>Decodes Tight's JPEG rectangles; without it the server is not asked for JPEG.</summary>
    public IVncJpegDecoder? JpegDecoder { get; init; }

    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public ILogger? Logger { get; init; }

    public static IReadOnlyList<int> DefaultEncodings { get; } =
        [VncEncoding.Tight, VncEncoding.Zrle, VncEncoding.Hextile, VncEncoding.Zlib, VncEncoding.Rre];

    // The password is a secret: keep it out of logs and debugger displays.
    public override string ToString() => Uri.TryCreate(Host, UriKind.Absolute, out var uri) && uri.Scheme is "ws" or "wss"
        ? uri.GetLeftPart(UriPartial.Path) : $"{Host}:{Port.ToString(CultureInfo.InvariantCulture)}";
}

/// <summary>
/// A VNC (RFB 3.3 / 3.7 / 3.8) client connection: the handshake, None / VNC / Tight security, the framebuffer
/// and the input messages. Everything is managed code.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ConnectAsync"/> completes the handshake; <see cref="Start"/> then starts the receive loop, which
/// decodes into <see cref="Framebuffer"/> and raises the events on the receiving context. Input methods
/// (<see cref="SendPointer"/>, <see cref="SendKey"/>…) can be called from any thread and never block: messages go
/// through a queue that a writer loop drains, and pointer moves with the same buttons coalesce.
/// </para>
/// <para>
/// The client asks for the next update as soon as one is decoded, so the frame rate follows the network and
/// the server. VNC security (DES challenge) is not encryption: use it on trusted networks or through a tunnel.
/// </para>
/// </remarks>
public sealed class VncClient : IDisposable
{
    private const int MaxNameLength = 64 * 1024;
    private const int MaxClipboardLength = 16 * 1024 * 1024;

    private readonly Socket? _socket;
    private readonly Stream _stream;
    private readonly SemaphoreSlim _outSignal = new(0, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly VncStreamReader _reader;
    private readonly VncDecoders _decoders;
    private readonly ILogger _logger;
    private readonly object _outLock = new();
    private readonly List<byte[]> _outbox = new();
    private int _pendingPointer = -1;   // index in _outbox of a pointer move that a newer one may replace
    private byte _lastPointerMask;      // buttons of the latest pointer event queued
    private bool _started;
    private volatile bool _disposed;
    private int _disconnectRaised;
    private volatile VncFramebuffer _framebuffer;
    private IReadOnlyList<int> _encodings;
    private int? _jpegQuality;
    private int _compressLevel;
    private readonly bool _canJpeg;
    private bool _fullRefresh;

    private VncClient(Socket? socket, Stream stream, VncStreamReader reader, VncConnectOptions options,
        int width, int height, string name, int minor, int security)
    {
        _socket = socket;
        _stream = stream;
        _reader = reader;
        _logger = options.Logger ?? NullLogger.Instance;
        _canJpeg = options.JpegDecoder != null;
        _decoders = new VncDecoders(reader, options.JpegDecoder);
        _framebuffer = new VncFramebuffer(width, height);
        _encodings = options.Encodings;
        _jpegQuality = options.JpegQuality;
        _compressLevel = options.CompressLevel;
        DesktopName = name;
        ProtocolVersion = $"3.{minor.ToString(CultureInfo.InvariantCulture)}";
        SecurityType = security;
        Host = options.Host;
        Port = options.Port;
    }

    public string Host { get; }

    public int Port { get; }

    /// <summary>The remote screen. Replaced (and <see cref="FramebufferResized"/> raised) when the desktop changes size.</summary>
    public VncFramebuffer Framebuffer => _framebuffer;

    public string DesktopName { get; private set; }

    /// <summary>The RFB version spoken, <c>3.3</c>, <c>3.7</c> or <c>3.8</c>.</summary>
    public string ProtocolVersion { get; }

    /// <summary>The security type used: 1 None, 2 VNC authentication, 16 Tight.</summary>
    public int SecurityType { get; }

    /// <summary>Bytes received since the connection opened.</summary>
    public long BytesReceived => _reader.BytesRead;

    /// <summary>Framebuffer updates decoded.</summary>
    public long UpdateCount { get; private set; }

    /// <summary>Rectangles decoded per encoding (see <see cref="VncEncoding.Name"/>).</summary>
    public IReadOnlyDictionary<int, int> EncodingCounts => _decoders.Counts;

    /// <summary>The desktop changed size; <see cref="Framebuffer"/> is the new one (receive loop).</summary>
    public event Action<VncFramebuffer>? FramebufferResized;

    /// <summary>Pixels changed: after each update, and during long ones (receive loop).</summary>
    public event Action? FramebufferUpdated;

    /// <summary>The remote pointer's shape changed (receive loop).</summary>
    public event Action<VncCursorShape>? CursorChanged;

    /// <summary>The server's clipboard text (receive loop).</summary>
    public event Action<string>? ClipboardReceived;

    public event Action? Bell;

    public event Action<string>? DesktopNameChanged;

    /// <summary>The connection ended — the server closed it or it failed (null for a clean close). Not raised by <see cref="Dispose"/>.</summary>
    public event Action<Exception?>? Disconnected;

    // ---- connect -------------------------------------------------------------------

    /// <summary>Connects and completes the handshake. Call <see cref="Start"/> once the events are attached.</summary>
    /// <exception cref="VncAuthenticationException">The server refused the password or the client.</exception>
    /// <exception cref="VncPasswordRequiredException">The server wants a password and none was given.</exception>
    /// <exception cref="VncException">Anything else that ends the attempt (unreachable, not a VNC server…).</exception>
    public static async Task<VncClient> ConnectAsync(VncConnectOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        Socket? socket = null;
        Stream? stream = null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.ConnectTimeout);
        try
        {
            if (Uri.TryCreate(options.Host, UriKind.Absolute, out var uri) && uri.Scheme is "ws" or "wss")
            {
                var webSocket = new ClientWebSocket();
                stream = new VncWebSocketStream(webSocket);
                webSocket.Options.AddSubProtocol("binary");
                await webSocket.ConnectAsync(uri, timeout.Token).ConfigureAwait(false);
            }
            else
            {
                if (OperatingSystem.IsBrowser()) throw new VncException("Browser VNC requires a ws:// or wss:// server address.");
                socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                await socket.ConnectAsync(options.Host, options.Port, timeout.Token).ConfigureAwait(false);
                stream = new NetworkStream(socket, ownsSocket: true);
            }
            return await HandshakeAsync(socket, stream, options, timeout).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            stream?.Dispose();
            socket?.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
            if (ex is OperationCanceledException)
                throw new VncException($"{options} did not answer within {options.ConnectTimeout.TotalSeconds:0} s.", new TimeoutException());
            if (ex is IOException or SocketException or WebSocketException or ObjectDisposedException)
                throw new VncException($"Cannot connect to {options}: {ex.Message}", ex);
            throw;
        }
    }

    private static async Task<VncClient> HandshakeAsync(Socket? socket, Stream stream, VncConnectOptions options, CancellationTokenSource timeout)
    {
        var ct = timeout.Token;
        var reader = new VncStreamReader(stream);

        // ProtocolVersion: "RFB xxx.yyy\n".
        var versionBytes = await reader.ReadBytesAsync(12, ct);
        var version = Encoding.ASCII.GetString(versionBytes);
        if (!version.StartsWith("RFB ", StringComparison.Ordinal) || version[7] != '.' || version[11] != '\n'
            || !int.TryParse(version.AsSpan(4, 3), NumberStyles.None, CultureInfo.InvariantCulture, out int major)
            || !int.TryParse(version.AsSpan(8, 3), NumberStyles.None, CultureInfo.InvariantCulture, out int serverMinor))
            throw new VncProtocolException($"{options} is not a VNC server.");
        // 3.3 ≤ ours ≤ 3.8. Apple's 3.889 and any 4.x speak 3.8; UltraVNC's 3.4 / 3.6 are 3.3.
        int minor = major > 3 || serverMinor >= 8 ? 8 : serverMinor == 7 ? 7 : serverMinor >= 3 ? 3 : -1;
        if (major < 3 || minor < 0) throw new VncProtocolException($"{options} speaks RFB {major}.{serverMinor}, which is too old.");
        await stream.WriteAsync(Encoding.ASCII.GetBytes($"RFB 003.00{minor}\n"), ct);

        // Security.
        int security;
        if (minor >= 7)
        {
            int count = await reader.ReadByteAsync(ct);
            if (count == 0) throw new VncAuthenticationException(await ReadReasonAsync(reader, ct, "The server refused the connection."), false);
            var types = await reader.ReadBytesAsync(count, ct);
            security = ChooseSecurity(types, options);
            await stream.WriteAsync(new byte[] { (byte)security }, ct);
        }
        else
        {
            security = (int)await reader.ReadUInt32Async(ct);
            if (security == 0) throw new VncAuthenticationException(await ReadReasonAsync(reader, ct, "The server refused the connection."), false);
            if (security is not (1 or 2)) throw new VncProtocolException($"The server wants security type {security}, which is not supported.");
        }

        int authType = security;
        if (security == 16) authType = await TightSecurityAsync(reader, stream, ct);

        bool usedPassword = false;
        if (authType == 2)
        {
            var challenge = await reader.ReadBytesAsync(16, ct);
            string? password = options.Password;
            if (password == null && options.PasswordProvider != null)
            {
                // The user may take a while: no read timeout while asking.
                timeout.CancelAfter(Timeout.InfiniteTimeSpan);
                try { password = await options.PasswordProvider(ct).ConfigureAwait(false); }
                finally { timeout.CancelAfter(options.ConnectTimeout); }
            }
            if (password == null) throw new VncPasswordRequiredException();
            await stream.WriteAsync(VncDes.EncryptChallenge(password, challenge), ct);
            usedPassword = true;
        }

        // SecurityResult: always in 3.8, only after authentication before.
        if (minor >= 8 || authType == 2)
        {
            uint result = await reader.ReadUInt32Async(ct);
            if (result != 0)
            {
                string reason = minor >= 8 ? await ReadReasonAsync(reader, ct, "") : "";
                if (reason.Length == 0) reason = usedPassword ? "The password was not accepted." : "The server refused the connection.";
                throw new VncAuthenticationException(reason, usedPassword);
            }
        }

        // ClientInit / ServerInit.
        await stream.WriteAsync(new byte[] { options.Shared ? (byte)1 : (byte)0 }, ct);
        int width = await reader.ReadUInt16Async(ct), height = await reader.ReadUInt16Async(ct);
        await reader.SkipAsync(16, ct);   // the server's pixel format: ours is set below
        uint nameLength = await reader.ReadUInt32Async(ct);
        if (nameLength > MaxNameLength) throw new VncProtocolException("The desktop name is implausibly long.");
        string name = Encoding.UTF8.GetString(await reader.ReadBytesAsync((int)nameLength, ct));
        if (security == 16)
        {
            // Tight interaction capabilities: server messages, client messages, encodings (16 bytes each).
            int serverMessages = await reader.ReadUInt16Async(ct), clientMessages = await reader.ReadUInt16Async(ct), encodings = await reader.ReadUInt16Async(ct);
            await reader.SkipAsync(2, ct);
            await reader.SkipAsync(16L * (serverMessages + clientMessages + encodings), ct);
        }
        if (width == 0 || height == 0) throw new VncProtocolException("The server has an empty desktop.");

        var client = new VncClient(socket, stream, reader, options, width, height, name, minor, security);
        client._logger.LogInformation("VNC connected to {Server}: {Name} {Width}×{Height}, RFB 3.{Minor}, security {Security}",
            options, name, width, height, minor, security);
        return client;
    }

    // None beats a password; Tight (16) wraps None / VNC and is only taken when nothing else is offered.
    private static int ChooseSecurity(ReadOnlySpan<byte> types, VncConnectOptions options)
    {
        if (types.Contains((byte)1)) return 1;
        if (types.Contains((byte)2)) return 2;
        if (types.Contains((byte)16)) return 16;
        var names = string.Join(", ", types.ToArray().Select(SecurityName));
        throw new VncAuthenticationException($"{options} only offers {names}, which this viewer does not support.", false);
    }

    private static string SecurityName(byte type) => type switch
    {
        5 or 6 => "RA2",
        18 => "TLS",
        19 => "VeNCrypt",
        30 or 35 => "Apple Remote Desktop",
        113 or 129 => "UltraVNC MS-Logon",
        _ => $"security type {type}",
    };

    // Tight security (TightVNC 2.x): no tunnel, then None or VNC authentication.
    private static async Task<int> TightSecurityAsync(VncStreamReader reader, Stream stream, CancellationToken ct)
    {
        uint tunnels = await reader.ReadUInt32Async(ct);
        if (tunnels > 0)
        {
            await reader.SkipAsync(16L * tunnels, ct);
            await stream.WriteAsync(new byte[] { 0, 0, 0, 0 }, ct);   // NOTUNNEL
        }
        uint authCount = await reader.ReadUInt32Async(ct);
        if (authCount == 0) return 1;
        var codes = new List<int>();
        for (uint i = 0; i < authCount; i++)
        {
            codes.Add(await reader.ReadInt32Async(ct));
            await reader.SkipAsync(12, ct);
        }
        int chosen = codes.Contains(1) ? 1 : codes.Contains(2) ? 2 : throw new VncAuthenticationException(
            "The server's Tight security only offers authentication schemes this viewer does not support.", false);
        var code = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(code, chosen);
        await stream.WriteAsync(code, ct);
        return chosen;
    }

    private static async Task<string> ReadReasonAsync(VncStreamReader reader, CancellationToken ct, string fallback)
    {
        try
        {
            uint length = await reader.ReadUInt32Async(ct);
            if (length > MaxNameLength) return fallback;
            var text = Encoding.UTF8.GetString(await reader.ReadBytesAsync((int)length, ct)).Trim();
            return text.Length > 0 ? text : fallback;
        }
        catch (Exception ex) when (ex is IOException or EndOfStreamException)
        {
            return fallback;
        }
    }

    // ---- session -------------------------------------------------------------------

    /// <summary>Sets the pixel format and encodings, asks for the first frame and starts the receive and send loops.</summary>
    public void Start()
    {
        if (_started) throw new InvalidOperationException("The client is already started.");
        ObjectDisposedException.ThrowIf(_disposed, this);
        Enqueue(SetPixelFormatMessage());
        Enqueue(SetEncodingsMessage());
        Enqueue(UpdateRequest(incremental: false));
        _started = true;
        _ = SendLoopAsync();
        // TCP may use the blocking compressed decoders; WebSocket only offers buffered Raw/CopyRect.
        if (_stream is VncWebSocketStream) _ = ReceiveLoopAsync();
        else _ = Task.Run(ReceiveLoopAsync);
    }

    private async Task ReceiveLoopAsync()
    {
        Exception? failure = null;
        try
        {
            while (!_disposed)
            {
                int type = await _reader.ReadByteAsync(_lifetime.Token);
                switch (type)
                {
                    case 0: await FramebufferUpdateAsync(); break;
                    case 1:
                        // SetColourMapEntries: not used with a true-colour format, but skip it if sent.
                        await _reader.SkipAsync(3, _lifetime.Token);
                        int colors = await _reader.ReadUInt16Async(_lifetime.Token);
                        await _reader.SkipAsync(6L * colors, _lifetime.Token);
                        break;
                    case 2: Bell?.Invoke(); break;
                    case 3:
                        await _reader.SkipAsync(3, _lifetime.Token);
                        uint length = await _reader.ReadUInt32Async(_lifetime.Token);
                        if (length > MaxClipboardLength) throw new VncProtocolException("The server's clipboard text is implausibly long.");
                        var text = Encoding.Latin1.GetString(await _reader.ReadBytesAsync((int)length, _lifetime.Token));
                        ClipboardReceived?.Invoke(text);
                        break;
                    default:
                        throw new VncProtocolException($"The server sent message type {type}, which this viewer does not know.");
                }
            }
        }
        catch (Exception ex)
        {
            if (!_disposed)
            {
                failure = ex is EndOfStreamException ? null : ex;
                if (failure != null) _logger.LogWarning(failure, "VNC connection to {Host}:{Port} failed", Host, Port);
                else _logger.LogInformation("VNC server {Host}:{Port} closed the connection", Host, Port);
            }
        }
        RaiseDisconnected(failure);
    }

    private async Task FramebufferUpdateAsync()
    {
        await _reader.SkipAsync(1, _lifetime.Token);
        int count = await _reader.ReadUInt16Async(_lifetime.Token);
        bool unbounded = count == 0xFFFF;   // with LastRect, the server may not know the count up front
        long lastNotify = Environment.TickCount64;
        for (int i = 0; unbounded || i < count; i++)
        {
            int x = await _reader.ReadUInt16Async(_lifetime.Token), y = await _reader.ReadUInt16Async(_lifetime.Token), w = await _reader.ReadUInt16Async(_lifetime.Token), h = await _reader.ReadUInt16Async(_lifetime.Token);
            int encoding = await _reader.ReadInt32Async(_lifetime.Token);
            switch (encoding)
            {
                case VncEncoding.LastRect:
                    i = count;
                    unbounded = false;
                    continue;
                case VncEncoding.DesktopSize:
                    Resize(w, h);
                    continue;
                case VncEncoding.ExtendedDesktopSize:
                    int screens = await _reader.ReadByteAsync(_lifetime.Token);
                    await _reader.SkipAsync(3 + 16L * screens, _lifetime.Token);
                    // y is the status: 0 = the size is in effect (a change, or the answer to our request).
                    if (y == 0) Resize(w, h);
                    continue;
                case VncEncoding.Cursor:
                    if (_stream is VncWebSocketStream)
                    {
                        if (w > 512 || h > 512) throw new VncProtocolException("Pointer is too large.");
                        await _reader.EnsureAsync(w * h * 4 + ((w + 7) / 8) * h, _lifetime.Token);
                    }
                    var shape = _decoders.ReadCursor(x, y, w, h);
                    CursorChanged?.Invoke(shape);
                    continue;
                case VncEncoding.DesktopName:
                    uint length = await _reader.ReadUInt32Async(_lifetime.Token);
                    if (length > MaxNameLength) throw new VncProtocolException("The desktop name is implausibly long.");
                    DesktopName = Encoding.UTF8.GetString(await _reader.ReadBytesAsync((int)length, _lifetime.Token));
                    DesktopNameChanged?.Invoke(DesktopName);
                    continue;
            }
            if (_stream is VncWebSocketStream)
            {
                _framebuffer.Check(x, y, w, h);
                int bytes = encoding switch
                {
                    VncEncoding.Raw => checked(w * h * 4),
                    VncEncoding.CopyRect => 4,
                    _ => throw new VncProtocolException($"WebSocket server used unrequested encoding {encoding}.")
                };
                await _reader.EnsureAsync(bytes, _lifetime.Token);
            }
            _decoders.Decode(_framebuffer, encoding, x, y, w, h);
            // A big first frame over a slow link shows as it arrives.
            if (Environment.TickCount64 - lastNotify > 50)
            {
                lastNotify = Environment.TickCount64;
                FramebufferUpdated?.Invoke();
            }
        }
        UpdateCount++;
        bool full = _fullRefresh;
        _fullRefresh = false;
        Enqueue(UpdateRequest(incremental: !full));
        FramebufferUpdated?.Invoke();
    }

    private void Resize(int width, int height)
    {
        if (width == _framebuffer.Width && height == _framebuffer.Height) return;
        _logger.LogInformation("VNC desktop resized to {Width}×{Height}", width, height);
        _framebuffer = new VncFramebuffer(width, height);
        _fullRefresh = true;
        FramebufferResized?.Invoke(_framebuffer);
    }

    // ---- input ---------------------------------------------------------------------

    /// <summary>
    /// The pointer at (<paramref name="x"/>, <paramref name="y"/>) in framebuffer pixels with <paramref name="buttons"/>
    /// pressed (bit 0 left, 1 middle, 2 right, 3 / 4 wheel up / down, 5 / 6 wheel left / right).
    /// </summary>
    public void SendPointer(int x, int y, int buttons)
    {
        var fb = _framebuffer;
        x = Math.Clamp(x, 0, fb.Width - 1);
        y = Math.Clamp(y, 0, fb.Height - 1);
        var message = new byte[6];
        message[0] = 5;
        message[1] = (byte)buttons;
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(2), (ushort)x);
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(4), (ushort)y);
        lock (_outLock)
        {
            if (_disposed) return;
            // A move replaces a move that is still waiting: only the latest position matters. A press or
            // release (the buttons changed) is never replaced: it happens where it was sent.
            bool move = message[1] == _lastPointerMask;
            _lastPointerMask = message[1];
            if (move && _pendingPointer >= 0 && _pendingPointer == _outbox.Count - 1)
            {
                _outbox[_pendingPointer] = message;
                return;
            }
            _outbox.Add(message);
            _pendingPointer = move ? _outbox.Count - 1 : -1;
            SignalOutput();
        }
    }

    /// <summary>A key press or release (<see cref="VncKeys"/>).</summary>
    public void SendKey(uint keysym, bool down)
    {
        if (keysym == 0) return;
        var message = new byte[8];
        message[0] = 4;
        message[1] = down ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteUInt32BigEndian(message.AsSpan(4), keysym);
        Enqueue(message);
    }

    /// <summary>Sets the server's clipboard (Latin-1: characters outside it become <c>?</c>).</summary>
    public void SendClipboard(string text)
    {
        var bytes = Encoding.Latin1.GetBytes(text.Replace("\r\n", "\n"));
        var message = new byte[8 + bytes.Length];
        message[0] = 6;
        BinaryPrimitives.WriteUInt32BigEndian(message.AsSpan(4), (uint)bytes.Length);
        bytes.CopyTo(message, 8);
        Enqueue(message);
    }

    /// <summary>Changes the encodings and JPEG / compression levels for the following updates.</summary>
    public void SetEncodings(IReadOnlyList<int> encodings, int? jpegQuality, int compressLevel)
    {
        _encodings = encodings;
        _jpegQuality = jpegQuality;
        _compressLevel = compressLevel;
        Enqueue(SetEncodingsMessage());
    }

    /// <summary>Asks for the whole screen again (repairs a view that went wrong).</summary>
    public void RequestFullUpdate() => Enqueue(UpdateRequest(incremental: false));

    // ---- messages ------------------------------------------------------------------

    // 32 bpp, depth 24, little-endian, true colour, 8 bits per channel, red << 16, green << 8, blue << 0:
    // on the wire and in memory a pixel is B, G, R, X — the framebuffer's layout, copied as is.
    private static byte[] SetPixelFormatMessage() =>
    [
        0, 0, 0, 0,
        32, 24, 0, 1, 0, 255, 0, 255, 0, 255, 16, 8, 0, 0, 0, 0,
    ];

    private byte[] SetEncodingsMessage()
    {
        var list = new List<int>();
        // CopyRect first: it is cheap, and servers take the first acceptable encoding for each rectangle.
        list.Add(VncEncoding.CopyRect);
        foreach (var e in _stream is VncWebSocketStream ? Array.Empty<int>() : _encodings)
            if (e is not (VncEncoding.CopyRect or VncEncoding.Raw) && !list.Contains(e)) list.Add(e);
        list.Add(VncEncoding.Raw);
        list.Add(VncEncoding.Cursor);
        list.Add(VncEncoding.DesktopSize);
        list.Add(VncEncoding.ExtendedDesktopSize);
        list.Add(VncEncoding.LastRect);
        list.Add(VncEncoding.DesktopName);
        list.Add(VncEncoding.CompressLevel(_compressLevel));
        if (_canJpeg && _jpegQuality is { } q) list.Add(VncEncoding.JpegQuality(q));

        var message = new byte[4 + 4 * list.Count];
        message[0] = 2;
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(2), (ushort)list.Count);
        for (int i = 0; i < list.Count; i++) BinaryPrimitives.WriteInt32BigEndian(message.AsSpan(4 + 4 * i), list[i]);
        return message;
    }

    private byte[] UpdateRequest(bool incremental)
    {
        var fb = _framebuffer;
        var message = new byte[10];
        message[0] = 3;
        message[1] = incremental ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(6), (ushort)fb.Width);
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(8), (ushort)fb.Height);
        return message;
    }

    private void SignalOutput()
    {
        if (_outSignal.CurrentCount == 0) _outSignal.Release();
    }

    private void Enqueue(byte[] message)
    {
        lock (_outLock)
        {
            if (_disposed) return;
            _outbox.Add(message);
            _pendingPointer = -1;
            SignalOutput();
        }
    }

    private async Task SendLoopAsync()
    {
        var batch = new List<byte[]>();
        try
        {
            while (true)
            {
                await _outSignal.WaitAsync(_lifetime.Token).ConfigureAwait(false);
                lock (_outLock)
                {
                    if (_disposed) return;
                    batch.AddRange(_outbox);
                    _outbox.Clear();
                    _pendingPointer = -1;
                }
                foreach (var message in batch) await _stream.WriteAsync(message, _lifetime.Token).ConfigureAwait(false);
                batch.Clear();
            }
        }
        catch (Exception ex)
        {
            if (!_disposed) _logger.LogWarning(ex, "VNC send to {Host}:{Port} failed", Host, Port);
            // The receive thread notices the broken connection too; closing makes sure it does.
            try { _stream.Dispose(); } catch { }
        }
    }

    private void RaiseDisconnected(Exception? failure)
    {
        if (Interlocked.Exchange(ref _disconnectRaised, 1) != 0) return;
        lock (_outLock)
        {
            bool byUser = _disposed;
            _disposed = true;
            _lifetime.Cancel();
            if (byUser) return;
        }
        try { _stream.Dispose(); } catch { }
        Disconnected?.Invoke(failure);
    }

    /// <summary>Closes the connection. <see cref="Disconnected"/> is not raised.</summary>
    public void Dispose()
    {
        lock (_outLock)
        {
            if (_disposed) return;
            _disposed = true;
            _lifetime.Cancel();
        }
        try { _socket?.Shutdown(SocketShutdown.Both); } catch { }
        _stream.Dispose();
        _socket?.Dispose();
    }
}
