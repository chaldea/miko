using System.Buffers.Binary;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Miko.Components.Vnc.Protocol;

/// <summary>How to reach and talk to a VNC server.</summary>
public sealed record VncConnectOptions(string Host, int Port = 5900)
{
    /// <summary>The VNC password, or null to ask <see cref="PasswordProvider"/> when the server wants one.</summary>
    public string? Password { get; init; }

    /// <summary>Asked for the password when the server needs one and <see cref="Password"/> is null; null declines.</summary>
    public Func<CancellationToken, Task<string?>>? PasswordProvider { get; init; }

    /// <summary>Let other viewers stay connected (false asks the server to disconnect them).</summary>
    public bool Shared { get; init; } = true;

    /// <summary>Frame encodings in order of preference. CopyRect and Raw are always offered.</summary>
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
    public override string ToString() => $"{Host}:{Port.ToString(CultureInfo.InvariantCulture)}";
}

/// <summary>
/// A VNC (RFB 3.3 / 3.7 / 3.8) client connection: the handshake, None / VNC / Tight security, the framebuffer
/// and the input messages. Everything is managed code.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ConnectAsync"/> completes the handshake; <see cref="Start"/> then starts the receive thread, which
/// decodes into <see cref="Framebuffer"/> and raises the events (on that thread). Input methods
/// (<see cref="SendPointer"/>, <see cref="SendKey"/>…) can be called from any thread and never block: messages go
/// through a queue that a writer thread drains, and pointer moves with the same buttons coalesce.
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

    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private readonly VncStreamReader _reader;
    private readonly VncDecoders _decoders;
    private readonly ILogger _logger;
    private readonly object _outLock = new();
    private readonly List<byte[]> _outbox = new();
    private int _pendingPointer = -1;   // index in _outbox of a pointer move that a newer one may replace
    private byte _lastPointerMask;      // buttons of the latest pointer event queued
    private Thread? _receiveThread, _sendThread;
    private volatile bool _disposed;
    private int _disconnectRaised;
    private volatile VncFramebuffer _framebuffer;
    private IReadOnlyList<int> _encodings;
    private int? _jpegQuality;
    private int _compressLevel;
    private readonly bool _canJpeg;
    private bool _fullRefresh;

    private VncClient(Socket socket, NetworkStream stream, VncStreamReader reader, VncConnectOptions options,
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

    /// <summary>The desktop changed size; <see cref="Framebuffer"/> is the new one (receive thread).</summary>
    public event Action<VncFramebuffer>? FramebufferResized;

    /// <summary>Pixels changed: after each update, and during long ones (receive thread).</summary>
    public event Action? FramebufferUpdated;

    /// <summary>The remote pointer's shape changed (receive thread).</summary>
    public event Action<VncCursorShape>? CursorChanged;

    /// <summary>The server's clipboard text (receive thread).</summary>
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
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(options.ConnectTimeout);
                try
                {
                    await socket.ConnectAsync(options.Host, options.Port, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new VncException($"{options} did not answer within {options.ConnectTimeout.TotalSeconds:0} s.", new TimeoutException());
                }
                catch (SocketException ex)
                {
                    throw new VncException($"Cannot connect to {options}: {ex.Message}", ex);
                }
            }

            var stream = new NetworkStream(socket, ownsSocket: true) { ReadTimeout = (int)options.ConnectTimeout.TotalMilliseconds };
            using var registration = cancellationToken.Register(static s => ((Socket)s!).Dispose(), socket);
            try
            {
                return await Task.Run(() => HandshakeAsync(socket, stream, options, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new VncException($"The connection to {options} failed during the handshake: {ex.Message}", ex);
            }
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static async Task<VncClient> HandshakeAsync(Socket socket, NetworkStream stream, VncConnectOptions options, CancellationToken ct)
    {
        var reader = new VncStreamReader(stream);

        // ProtocolVersion: "RFB xxx.yyy\n".
        var versionBytes = reader.ReadBytes(12);
        var version = Encoding.ASCII.GetString(versionBytes);
        if (!version.StartsWith("RFB ", StringComparison.Ordinal) || version[7] != '.' || version[11] != '\n'
            || !int.TryParse(version.AsSpan(4, 3), NumberStyles.None, CultureInfo.InvariantCulture, out int major)
            || !int.TryParse(version.AsSpan(8, 3), NumberStyles.None, CultureInfo.InvariantCulture, out int serverMinor))
            throw new VncProtocolException($"{options} is not a VNC server.");
        // 3.3 ≤ ours ≤ 3.8. Apple's 3.889 and any 4.x speak 3.8; UltraVNC's 3.4 / 3.6 are 3.3.
        int minor = major > 3 || serverMinor >= 8 ? 8 : serverMinor == 7 ? 7 : serverMinor >= 3 ? 3 : -1;
        if (major < 3 || minor < 0) throw new VncProtocolException($"{options} speaks RFB {major}.{serverMinor}, which is too old.");
        stream.Write(Encoding.ASCII.GetBytes($"RFB 003.00{minor}\n"));

        // Security.
        int security;
        if (minor >= 7)
        {
            int count = reader.ReadByte();
            if (count == 0) throw new VncAuthenticationException(ReadReason(reader, "The server refused the connection."), false);
            var types = reader.ReadBytes(count);
            security = ChooseSecurity(types, options);
            stream.WriteByte((byte)security);
        }
        else
        {
            security = (int)reader.ReadUInt32();
            if (security == 0) throw new VncAuthenticationException(ReadReason(reader, "The server refused the connection."), false);
            if (security is not (1 or 2)) throw new VncProtocolException($"The server wants security type {security}, which is not supported.");
        }

        int authType = security;
        if (security == 16) authType = TightSecurity(reader, stream);

        bool usedPassword = false;
        if (authType == 2)
        {
            var challenge = reader.ReadBytes(16);
            string? password = options.Password;
            if (password == null && options.PasswordProvider != null)
            {
                // The user may take a while: no read timeout while asking.
                password = await options.PasswordProvider(ct).ConfigureAwait(false);
            }
            if (password == null) throw new VncPasswordRequiredException();
            stream.Write(VncDes.EncryptChallenge(password, challenge));
            usedPassword = true;
        }

        // SecurityResult: always in 3.8, only after authentication before.
        if (minor >= 8 || authType == 2)
        {
            uint result = reader.ReadUInt32();
            if (result != 0)
            {
                string reason = minor >= 8 ? ReadReason(reader, "") : "";
                if (reason.Length == 0) reason = usedPassword ? "The password was not accepted." : "The server refused the connection.";
                throw new VncAuthenticationException(reason, usedPassword);
            }
        }

        // ClientInit / ServerInit.
        stream.WriteByte(options.Shared ? (byte)1 : (byte)0);
        int width = reader.ReadUInt16(), height = reader.ReadUInt16();
        reader.Skip(16);   // the server's pixel format: ours is set below
        uint nameLength = reader.ReadUInt32();
        if (nameLength > MaxNameLength) throw new VncProtocolException("The desktop name is implausibly long.");
        string name = Encoding.UTF8.GetString(reader.ReadBytes((int)nameLength));
        if (security == 16)
        {
            // Tight interaction capabilities: server messages, client messages, encodings (16 bytes each).
            int serverMessages = reader.ReadUInt16(), clientMessages = reader.ReadUInt16(), encodings = reader.ReadUInt16();
            reader.Skip(2);
            reader.Skip(16L * (serverMessages + clientMessages + encodings));
        }
        if (width == 0 || height == 0) throw new VncProtocolException("The server has an empty desktop.");

        stream.ReadTimeout = Timeout.Infinite;
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
    private static int TightSecurity(VncStreamReader reader, NetworkStream stream)
    {
        uint tunnels = reader.ReadUInt32();
        if (tunnels > 0)
        {
            reader.Skip(16L * tunnels);
            stream.Write(new byte[] { 0, 0, 0, 0 });   // NOTUNNEL
        }
        uint authCount = reader.ReadUInt32();
        if (authCount == 0) return 1;
        var codes = new List<int>();
        for (uint i = 0; i < authCount; i++)
        {
            codes.Add(reader.ReadInt32());
            reader.Skip(12);
        }
        int chosen = codes.Contains(1) ? 1 : codes.Contains(2) ? 2 : throw new VncAuthenticationException(
            "The server's Tight security only offers authentication schemes this viewer does not support.", false);
        Span<byte> code = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(code, chosen);
        stream.Write(code);
        return chosen;
    }

    private static string ReadReason(VncStreamReader reader, string fallback)
    {
        try
        {
            uint length = reader.ReadUInt32();
            if (length > MaxNameLength) return fallback;
            var text = Encoding.UTF8.GetString(reader.ReadBytes((int)length)).Trim();
            return text.Length > 0 ? text : fallback;
        }
        catch (Exception ex) when (ex is IOException or EndOfStreamException)
        {
            return fallback;
        }
    }

    // ---- session -------------------------------------------------------------------

    /// <summary>Sets the pixel format and encodings, asks for the first frame and starts the receive and send threads.</summary>
    public void Start()
    {
        if (_receiveThread != null) throw new InvalidOperationException("The client is already started.");
        ObjectDisposedException.ThrowIf(_disposed, this);
        Enqueue(SetPixelFormatMessage());
        Enqueue(SetEncodingsMessage());
        Enqueue(UpdateRequest(incremental: false));
        _sendThread = new Thread(SendLoop) { IsBackground = true, Name = "Miko VNC send" };
        _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "Miko VNC receive" };
        _sendThread.Start();
        _receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        Exception? failure = null;
        try
        {
            while (!_disposed)
            {
                int type = _reader.ReadByte();
                switch (type)
                {
                    case 0: FramebufferUpdate(); break;
                    case 1:
                        // SetColourMapEntries: not used with a true-colour format, but skip it if sent.
                        _reader.Skip(3);
                        int colors = _reader.ReadUInt16();
                        _reader.Skip(6L * colors);
                        break;
                    case 2: Bell?.Invoke(); break;
                    case 3:
                        _reader.Skip(3);
                        uint length = _reader.ReadUInt32();
                        if (length > MaxClipboardLength) throw new VncProtocolException("The server's clipboard text is implausibly long.");
                        var text = Encoding.Latin1.GetString(_reader.ReadBytes((int)length));
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

    private void FramebufferUpdate()
    {
        _reader.Skip(1);
        int count = _reader.ReadUInt16();
        bool unbounded = count == 0xFFFF;   // with LastRect, the server may not know the count up front
        long lastNotify = Environment.TickCount64;
        for (int i = 0; unbounded || i < count; i++)
        {
            int x = _reader.ReadUInt16(), y = _reader.ReadUInt16(), w = _reader.ReadUInt16(), h = _reader.ReadUInt16();
            int encoding = _reader.ReadInt32();
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
                    int screens = _reader.ReadByte();
                    _reader.Skip(3 + 16L * screens);
                    // y is the status: 0 = the size is in effect (a change, or the answer to our request).
                    if (y == 0) Resize(w, h);
                    continue;
                case VncEncoding.Cursor:
                    var shape = _decoders.ReadCursor(x, y, w, h);
                    CursorChanged?.Invoke(shape);
                    continue;
                case VncEncoding.DesktopName:
                    uint length = _reader.ReadUInt32();
                    if (length > MaxNameLength) throw new VncProtocolException("The desktop name is implausibly long.");
                    DesktopName = Encoding.UTF8.GetString(_reader.ReadBytes((int)length));
                    DesktopNameChanged?.Invoke(DesktopName);
                    continue;
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
            Monitor.Pulse(_outLock);
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
        foreach (var e in _encodings)
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

    private void Enqueue(byte[] message)
    {
        lock (_outLock)
        {
            if (_disposed) return;
            _outbox.Add(message);
            _pendingPointer = -1;
            Monitor.Pulse(_outLock);
        }
    }

    private void SendLoop()
    {
        var batch = new List<byte[]>();
        try
        {
            while (true)
            {
                lock (_outLock)
                {
                    while (_outbox.Count == 0 && !_disposed) Monitor.Wait(_outLock);
                    if (_disposed) return;
                    batch.AddRange(_outbox);
                    _outbox.Clear();
                    _pendingPointer = -1;
                }
                foreach (var message in batch) _stream.Write(message);
                batch.Clear();
            }
        }
        catch (Exception ex)
        {
            if (!_disposed) _logger.LogWarning(ex, "VNC send to {Host}:{Port} failed", Host, Port);
            // The receive thread notices the broken connection too; closing makes sure it does.
            try { _socket.Dispose(); } catch { }
        }
    }

    private void RaiseDisconnected(Exception? failure)
    {
        if (Interlocked.Exchange(ref _disconnectRaised, 1) != 0) return;
        lock (_outLock)
        {
            bool byUser = _disposed;
            _disposed = true;
            Monitor.PulseAll(_outLock);
            if (byUser) return;
        }
        try { _socket.Dispose(); } catch { }
        Disconnected?.Invoke(failure);
    }

    /// <summary>Closes the connection. <see cref="Disconnected"/> is not raised.</summary>
    public void Dispose()
    {
        lock (_outLock)
        {
            if (_disposed) return;
            _disposed = true;
            Monitor.PulseAll(_outLock);
        }
        try { _socket.Shutdown(SocketShutdown.Both); } catch { }
        _stream.Dispose();
        _socket.Dispose();
    }
}
