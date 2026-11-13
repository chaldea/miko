using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

namespace Miko.MockServer;

/// <summary>RFB 3.3/3.7/3.8 server. Each connection owns a separate virtual desktop.</summary>
public sealed class VncServer(MockOptions options, MediaLibrary media, ILogger<VncServer> logger)
    : TcpServer(options, options.VncPort, logger)
{
    protected override async Task ServeAsync(TcpClient client, CancellationToken token)
    {
        var stream = client.GetStream();
        using (var handshake = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            handshake.CancelAfter(TimeSpan.FromSeconds(10));
            var ct = handshake.Token;
            await stream.WriteAsync("RFB 003.008\n"u8.ToArray(), ct);
            var version = Encoding.ASCII.GetString(await Read(stream, 12, ct));
            int minor = version switch { "RFB 003.003\n" => 3, "RFB 003.007\n" => 7, "RFB 003.008\n" => 8, _ => throw new InvalidDataException("Unsupported RFB version.") };
            if (minor == 3) await stream.WriteAsync(new byte[] { 0, 0, 0, 1 }, ct);
            else
            {
                await stream.WriteAsync(new byte[] { 1, 1 }, ct); // None security for this demo service.
                if ((await Read(stream, 1, ct))[0] != 1) throw new InvalidDataException("Unsupported security type.");
                if (minor == 8) await stream.WriteAsync(new byte[4], ct);
            }
            await Read(stream, 1, ct); // Shared flag: sessions have independent desktops, so neither evicts another.
            var name = Encoding.UTF8.GetBytes("Miko interactive mock desktop");
            var init = new byte[24 + name.Length];
            U16(init, 0, 640); U16(init, 2, 360);
            PixelFormat.Default.Bytes.CopyTo(init, 4);
            U32(init, 20, (uint)name.Length); name.CopyTo(init, 24);
            await stream.WriteAsync(init, ct);
        }

        var format = PixelFormat.Default;
        var raster = media.Background.Clone();
        int clicks = 0, revision = 0, sentRevision = -1;
        byte buttons = 0;
        string label = "READY";
        UpdateLabel();
        Request? pending = null;
        using var session = CancellationTokenSource.CreateLinkedTokenSource(token);
        var read = ReadMessage(stream, session.Token);
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (pending is { } request && (!request.Incremental || sentRevision != revision))
                {
                    await SendFrame(stream, raster, format, request, token);
                    // A partial request must not mark unseen regions as delivered.
                    sentRevision = request.X == 0 && request.Y == 0 && request.Width >= 640 && request.Height >= 360 ? revision : -1;
                    pending = null;
                }
                var message = await read;
                switch (message.Type)
                {
                    case 0:
                        format = new PixelFormat(message.Body.AsSpan(3, 16).ToArray());
                        format.Validate();
                        if (!format.TrueColor) await SendPalette(stream, token);
                        sentRevision = -1;
                        break;
                    case 1: // FixColourMapEntries is obsolete; consume it as required by RFB 3.x.
                    case 2: break; // Raw is mandatory and legal regardless of the encoding preferences.
                    case 3:
                        pending = new Request(message.Body[0] != 0, R16(message.Body, 1), R16(message.Body, 3), R16(message.Body, 5), R16(message.Body, 7));
                        break;
                    case 4:
                        if (message.Body[0] != 0)
                        {
                            uint key = BinaryPrimitives.ReadUInt32BigEndian(message.Body.AsSpan(3));
                            label = key is >= 32 and <= 126 ? "KEY: " + (char)key : $"KEY: {key:X}";
                            if (key is 32 or 0xff0d) clicks++;
                            UpdateLabel();
                        }
                        break;
                    case 5:
                        byte next = message.Body[0];
                        int x = R16(message.Body, 1), y = R16(message.Body, 3);
                        if ((next & 1) != 0 && (buttons & 1) == 0)
                        {
                            if (y is >= 174 and < 236 && x is >= 28 and < 218) { clicks++; label = "BUTTON CLICKED"; }
                            else if (y is >= 174 and < 236 && x is >= 246 and < 416) { clicks = 0; label = "RESET"; }
                            else label = $"POINTER: {x} {y}";
                            UpdateLabel();
                        }
                        buttons = next;
                        break;
                    case 6:
                        label = "CLIPBOARD: " + Encoding.Latin1.GetString(message.Body)[..Math.Min(message.Body.Length, 28)];
                        UpdateLabel();
                        var clipboard = new byte[8 + message.Body.Length]; clipboard[0] = 3;
                        U32(clipboard, 4, (uint)message.Body.Length); message.Body.CopyTo(clipboard, 8);
                        await stream.WriteAsync(clipboard, token);
                        break;
                }
                read = ReadMessage(stream, session.Token);
                // Coalesce requests while keeping input processing responsive and avoiding an empty-update spin.
            }
        }
        finally
        {
            await session.CancelAsync();
            try { await read; } catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException or InvalidDataException) { }
        }

        void UpdateLabel()
        {
            raster.Fill(28, 254, 584, 48, 0x101b2e);
            raster.Text(28, 254, $"CLICKS: {clicks}", 0x67e8f9);
            raster.Text(28, 280, label, 0xfbbf24);
            raster.Fill(28, 174, 190, 4, clicks % 2 == 0 ? 0x147d92u : 0x67e8f9u);
            revision++;
        }
    }

    private static async Task<(byte Type, byte[] Body)> ReadMessage(NetworkStream stream, CancellationToken token)
    {
        byte type = (await Read(stream, 1, token))[0];
        int length = type switch { 0 => 19, 1 => 5, 2 => 3, 3 => 9, 4 => 7, 5 => 5, 6 => 7, _ => throw new InvalidDataException("Unsupported RFB message.") };
        var body = await Read(stream, length, token);
        if (type == 1) await Read(stream, R16(body, 3) * 6, token);
        if (type == 2)
        {
            int count = R16(body, 1);
            if (count > 1024) throw new InvalidDataException("Too many encodings.");
            await Read(stream, count * 4, token);
        }
        if (type == 6)
        {
            uint count = BinaryPrimitives.ReadUInt32BigEndian(body.AsSpan(3));
            if (count > 4096) throw new InvalidDataException("Clipboard is too large.");
            body = await Read(stream, (int)count, token);
        }
        return (type, body);
    }

    private static async Task SendFrame(NetworkStream stream, Raster raster, PixelFormat format, Request request, CancellationToken token)
    {
        int x = Math.Min(request.X, raster.Width), y = Math.Min(request.Y, raster.Height);
        int w = Math.Min(request.Width, raster.Width - x), h = Math.Min(request.Height, raster.Height - y);
        if (w == 0 || h == 0) { await stream.WriteAsync(new byte[4], token); return; }
        var output = new byte[16 + w * h * format.Size];
        U16(output, 2, 1); U16(output, 4, x); U16(output, 6, y); U16(output, 8, w); U16(output, 10, h); // encoding 0 = Raw
        int target = 16;
        for (int row = y; row < y + h; row++) for (int col = x; col < x + w; col++)
        {
            int source = (row * raster.Width + col) * 3;
            uint r = raster.Pixels[source], g = raster.Pixels[source + 1], b = raster.Pixels[source + 2];
            uint value = format.TrueColor ? ((r * format.RedMax / 255) << format.Bytes[10]) | ((g * format.GreenMax / 255) << format.Bytes[11]) | ((b * format.BlueMax / 255) << format.Bytes[12])
                : ((r >> 5) << 5) | ((g >> 5) << 2) | (b >> 6);
            for (int i = 0; i < format.Size; i++) output[target++] = (byte)(value >> (8 * (format.Bytes[2] == 0 ? i : format.Size - 1 - i)));
        }
        await stream.WriteAsync(output, token);
    }

    private static async Task SendPalette(NetworkStream stream, CancellationToken token)
    {
        var output = new byte[6 + 256 * 6]; output[0] = 1; U16(output, 4, 256);
        for (int i = 0; i < 256; i++)
        {
            U16(output, 6 + i * 6, ((i >> 5) & 7) * 65535 / 7);
            U16(output, 8 + i * 6, ((i >> 2) & 7) * 65535 / 7);
            U16(output, 10 + i * 6, (i & 3) * 65535 / 3);
        }
        await stream.WriteAsync(output, token);
    }

    private sealed record Request(bool Incremental, int X, int Y, int Width, int Height);
    private sealed record PixelFormat(byte[] Bytes)
    {
        public static PixelFormat Default => new([32, 24, 0, 1, 0, 255, 0, 255, 0, 255, 16, 8, 0, 0, 0, 0]);
        public int Size => Bytes[0] / 8;
        public bool TrueColor => Bytes[3] != 0;
        public uint RedMax => (uint)R16(Bytes, 4);
        public uint GreenMax => (uint)R16(Bytes, 6);
        public uint BlueMax => (uint)R16(Bytes, 8);
        public void Validate()
        {
            if (Bytes[0] is not (8 or 16 or 32) || Bytes[1] == 0 || Bytes[1] > Bytes[0] || Bytes[2] > 1 || Bytes[3] > 1)
                throw new InvalidDataException("Invalid RFB pixel format.");
            if (!TrueColor) return;
            ulong mask = 0;
            foreach (int i in new[] { 0, 1, 2 })
            {
                uint maximum = (uint)R16(Bytes, 4 + 2 * i);
                int shift = Bytes[10 + i];
                if (maximum == 0 || (maximum & (maximum + 1)) != 0 || shift >= Bytes[0]) throw new InvalidDataException("Invalid color maximum/shift.");
                ulong part = (ulong)maximum << shift;
                if ((part >> Bytes[0]) != 0 || (part & mask) != 0) throw new InvalidDataException("Overlapping pixel channels.");
                mask |= part;
            }
        }
    }

    private static async Task<byte[]> Read(NetworkStream stream, int length, CancellationToken token)
    {
        var data = new byte[length]; await stream.ReadExactlyAsync(data, token); return data;
    }
    private static int R16(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset));
    private static void U16(byte[] bytes, int offset, int value) => BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset), (ushort)value);
    private static void U32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset), value);
}
