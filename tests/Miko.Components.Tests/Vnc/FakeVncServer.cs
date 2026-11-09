using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Miko.Components.Vnc.Protocol;

namespace Miko.Components.Tests.Vnc;

/// <summary>What a client sent, as the scripted server parsed it.</summary>
internal sealed record ClientMessage(string Kind, bool Down = false, uint Keysym = 0, int Buttons = 0, int X = 0, int Y = 0,
    int Width = 0, int Height = 0, bool Incremental = false, string? Text = null, int[]? Encodings = null);

/// <summary>
/// A scripted RFB server on loopback: the handshake (version, security, VNC authentication against
/// <see cref="Password"/>), then whatever the test sends; the client's messages are parsed into
/// <see cref="Messages"/>. Accepts any number of connections, one at a time.
/// </summary>
internal sealed class FakeVncServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private Socket? _socket;
    private volatile NetworkStream? _stream;
    private readonly object _writeLock = new();
    private readonly MemoryStream _zrleBuffer = new();
    private ZLibStream? _zrle;
    private readonly MemoryStream[] _tightBuffers = [new(), new(), new(), new()];
    private readonly ZLibStream?[] _tight = new ZLibStream?[4];

    public FakeVncServer(int width = 64, int height = 32)
    {
        Width = width;
        Height = height;
        _listener.Start();
        _ = Task.Run(AcceptLoop);
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public int Width { get; set; }

    public int Height { get; set; }

    public string Name { get; set; } = "fake-desktop";

    /// <summary>"RFB 003.008\n" by default.</summary>
    public string Version { get; set; } = "RFB 003.008\n";

    /// <summary>Security types offered (3.7+), or the one chosen (3.3: the first).</summary>
    public byte[] SecurityTypes { get; set; } = [2];

    /// <summary>The VNC password; a response for any other is refused.</summary>
    public string? Password { get; set; } = "secret";

    /// <summary>The challenge sent with VNC authentication.</summary>
    public byte[] Challenge { get; } = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();

    /// <summary>The response the client sent to the challenge (last connection).</summary>
    public byte[]? Response { get; private set; }

    public int Connections => _connections;

    private int _connections;

    /// <summary>Messages from the client of the current connection.</summary>
    public BlockingCollection<ClientMessage> Messages { get; private set; } = new();

    public Exception? HandshakeError { get; private set; }

    private async Task AcceptLoop()
    {
        while (!_stop.IsCancellationRequested)
        {
            Socket socket;
            try { socket = await _listener.AcceptSocketAsync(_stop.Token); }
            catch { return; }
            Interlocked.Increment(ref _connections);
            _socket?.Dispose();
            _socket = socket;
            _stream = new NetworkStream(socket, ownsSocket: true);
            Messages = new BlockingCollection<ClientMessage>();
            _zrle = null;
            _zrleBuffer.SetLength(0);
            for (int i = 0; i < 4; i++) { _tight[i] = null; _tightBuffers[i].SetLength(0); }
            try
            {
                bool accepted = Handshake(_stream);
                if (!accepted) { socket.Dispose(); continue; }
                var stream = _stream;
                var messages = Messages;
                _ = Task.Run(() => ReadLoop(stream, messages));
            }
            catch (Exception ex)
            {
                HandshakeError = ex;
                socket.Dispose();
            }
        }
    }

    private bool Handshake(NetworkStream s)
    {
        s.Write(Encoding.ASCII.GetBytes(Version));
        var reply = Encoding.ASCII.GetString(ReadExactly(s, 12));
        int minor = reply[10] - '0';
        int security;
        if (minor >= 7)
        {
            s.WriteByte((byte)SecurityTypes.Length);
            s.Write(SecurityTypes);
            security = s.ReadByte();
        }
        else
        {
            security = SecurityTypes[0];
            WriteUInt32(s, (uint)security);
        }
        bool ok = true;
        if (security == 2)
        {
            s.Write(Challenge);
            Response = ReadExactly(s, 16);
            ok = Password != null && Response.AsSpan().SequenceEqual(VncDes.EncryptChallenge(Password, Challenge));
        }
        if (minor >= 8 || security == 2)
        {
            WriteUInt32(s, ok ? 0u : 1u);
            if (!ok && minor >= 8)
            {
                var reason = Encoding.ASCII.GetBytes("Authentication failed");
                WriteUInt32(s, (uint)reason.Length);
                s.Write(reason);
            }
        }
        if (!ok) return false;

        s.ReadByte();   // ClientInit
        var init = new byte[24];
        BinaryPrimitives.WriteUInt16BigEndian(init, (ushort)Width);
        BinaryPrimitives.WriteUInt16BigEndian(init.AsSpan(2), (ushort)Height);
        // The server's own format (32 bpp BGRX) — the client replaces it.
        new byte[] { 32, 24, 0, 1, 0, 255, 0, 255, 0, 255, 16, 8, 0, 0, 0, 0 }.CopyTo(init, 4);
        var name = Encoding.UTF8.GetBytes(Name);
        BinaryPrimitives.WriteUInt32BigEndian(init.AsSpan(20), (uint)name.Length);
        s.Write(init);
        s.Write(name);
        return true;
    }

    private static void ReadLoop(NetworkStream s, BlockingCollection<ClientMessage> messages)
    {
        try
        {
            while (true)
            {
                int type = s.ReadByte();
                if (type < 0) break;
                switch (type)
                {
                    case 0: ReadExactly(s, 19); messages.Add(new("SetPixelFormat")); break;
                    case 2:
                        ReadExactly(s, 1);
                        int count = BinaryPrimitives.ReadUInt16BigEndian(ReadExactly(s, 2));
                        var list = new int[count];
                        for (int i = 0; i < count; i++) list[i] = BinaryPrimitives.ReadInt32BigEndian(ReadExactly(s, 4));
                        messages.Add(new("SetEncodings", Encodings: list));
                        break;
                    case 3:
                        var r = ReadExactly(s, 9);
                        messages.Add(new("Update", Incremental: r[0] != 0, X: Be16(r, 1), Y: Be16(r, 3), Width: Be16(r, 5), Height: Be16(r, 7)));
                        break;
                    case 4:
                        var k = ReadExactly(s, 7);
                        messages.Add(new("Key", Down: k[0] != 0, Keysym: BinaryPrimitives.ReadUInt32BigEndian(k.AsSpan(3))));
                        break;
                    case 5:
                        var p = ReadExactly(s, 5);
                        messages.Add(new("Pointer", Buttons: p[0], X: Be16(p, 1), Y: Be16(p, 3)));
                        break;
                    case 6:
                        ReadExactly(s, 3);
                        int length = (int)BinaryPrimitives.ReadUInt32BigEndian(ReadExactly(s, 4));
                        messages.Add(new("CutText", Text: Encoding.Latin1.GetString(ReadExactly(s, length))));
                        break;
                    default: return;
                }
            }
        }
        catch { }
        finally { messages.CompleteAdding(); }
    }

    private static int Be16(byte[] b, int o) => BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(o));

    /// <summary>Waits for the next client message that matches (skipping others); null on timeout.</summary>
    public ClientMessage? Next(Func<ClientMessage, bool> match, int timeoutMs = 5000)
    {
        var until = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < until)
        {
            if (!Messages.TryTake(out var m, (int)Math.Max(1, until - Environment.TickCount64))) return null;
            if (match(m)) return m;
        }
        return null;
    }

    /// <summary>All messages that arrive within <paramref name="ms"/>.</summary>
    public List<ClientMessage> Drain(int ms = 300)
    {
        var list = new List<ClientMessage>();
        var until = Environment.TickCount64 + ms;
        while (Messages.TryTake(out var m, (int)Math.Max(0, until - Environment.TickCount64))) list.Add(m);
        return list;
    }

    // ---- server → client ----------------------------------------------------------

    /// <summary>A FramebufferUpdate of the given rectangles (header + payload each).</summary>
    public void Update(params (int X, int Y, int W, int H, int Encoding, byte[] Payload)[] rects) => UpdateWithCount(rects.Length, rects);

    public void UpdateWithCount(int count, params (int X, int Y, int W, int H, int Encoding, byte[] Payload)[] rects)
    {
        var ms = new MemoryStream();
        ms.WriteByte(0);
        ms.WriteByte(0);
        WriteUInt16(ms, (ushort)count);
        foreach (var (x, y, w, h, encoding, payload) in rects)
        {
            WriteUInt16(ms, (ushort)x);
            WriteUInt16(ms, (ushort)y);
            WriteUInt16(ms, (ushort)w);
            WriteUInt16(ms, (ushort)h);
            WriteUInt32(ms, unchecked((uint)encoding));
            ms.Write(payload);
        }
        Send(ms.ToArray());
    }

    public void Send(byte[] bytes)
    {
        lock (_writeLock) _stream!.Write(bytes);
    }

    public void CutText(string text)
    {
        var bytes = Encoding.Latin1.GetBytes(text);
        var ms = new MemoryStream();
        ms.WriteByte(3);
        ms.Write(new byte[3]);
        WriteUInt32(ms, (uint)bytes.Length);
        ms.Write(bytes);
        Send(ms.ToArray());
    }

    /// <summary>Closes the current connection from the server side.</summary>
    public void Drop()
    {
        try { _socket?.Shutdown(SocketShutdown.Both); } catch { }
        _socket?.Dispose();
    }

    // ---- encoders (what a server sends) --------------------------------------------

    /// <summary>B, G, R, X for an opaque colour.</summary>
    public static byte[] Pixel(byte r, byte g, byte b) => [b, g, r, 0];

    public static byte[] RawPixels(int w, int h, Func<int, int, (byte R, byte G, byte B)> color)
    {
        var bytes = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var (r, g, b) = color(x, y);
                Pixel(r, g, b).CopyTo(bytes, (y * w + x) * 4);
            }
        return bytes;
    }

    /// <summary>The next sync-flushed piece of the connection's ZRLE stream, length-prefixed.</summary>
    public byte[] Zrle(byte[] tiles)
    {
        _zrle ??= new ZLibStream(_zrleBuffer, CompressionLevel.Optimal, leaveOpen: true);
        return LengthPrefixed(Flush(_zrle, _zrleBuffer, tiles));
    }

    /// <summary>The next sync-flushed piece of Tight stream <paramref name="stream"/>.</summary>
    public byte[] TightCompress(int stream, byte[] data, bool reset = false)
    {
        if (reset || _tight[stream] == null)
        {
            _tightBuffers[stream].SetLength(0);
            _tight[stream] = new ZLibStream(_tightBuffers[stream], CompressionLevel.Optimal, leaveOpen: true);
        }
        return Flush(_tight[stream]!, _tightBuffers[stream], data);
    }

    private static byte[] Flush(ZLibStream z, MemoryStream buffer, byte[] data)
    {
        long before = buffer.Length;
        z.Write(data);
        z.Flush();
        return buffer.ToArray()[(int)before..];
    }

    public static byte[] LengthPrefixed(byte[] data)
    {
        var bytes = new byte[4 + data.Length];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)data.Length);
        data.CopyTo(bytes, 4);
        return bytes;
    }

    public static byte[] CompactLength(int length) => length < 128 ? [(byte)length]
        : length < 16384 ? [(byte)(length & 0x7F | 0x80), (byte)(length >> 7)]
        : [(byte)(length & 0x7F | 0x80), (byte)((length >> 7) & 0x7F | 0x80), (byte)(length >> 14)];

    private static byte[] ReadExactly(Stream s, int count)
    {
        var bytes = new byte[count];
        s.ReadExactly(bytes);
        return bytes;
    }

    private static void WriteUInt16(Stream s, ushort v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16BigEndian(b, v); s.Write(b); }

    private static void WriteUInt32(Stream s, uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, v); s.Write(b); }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        Drop();
    }
}
