using System.Buffers.Binary;

namespace Miko.Components.Vnc.Protocol;

/// <summary>Decodes a JPEG (Tight encoding) into 32-bit B, G, R, A pixels.</summary>
public interface IVncJpegDecoder
{
    /// <summary>
    /// Decodes <paramref name="jpeg"/>, which must be <paramref name="width"/> × <paramref name="height"/>, into
    /// <paramref name="destination"/> (rows <paramref name="stride"/> bytes apart). Called on the connection's
    /// receive thread. False when the data cannot be decoded.
    /// </summary>
    bool Decode(ReadOnlySpan<byte> jpeg, int width, int height, Span<byte> destination, int stride);
}

/// <summary>The remote pointer's shape (Cursor pseudo-encoding): B, G, R, A pixels, alpha from the server's mask.</summary>
public sealed record VncCursorShape(int Width, int Height, int HotspotX, int HotspotY, byte[] Pixels)
{
    /// <summary>An empty shape: the server hides the pointer.</summary>
    public bool IsEmpty => Width == 0 || Height == 0;
}

/// <summary>
/// The framebuffer encodings (RFB 3.8 §7.7 and the community extensions): Raw, CopyRect, RRE, Hextile,
/// Zlib, ZRLE and Tight (with JPEG when a decoder is given). Pixels arrive in the client format the
/// connection sets — 32 bits per pixel, depth 24, little-endian, red at bit 16, blue at bit 0 — so a wire
/// pixel is the framebuffer's B, G, R, X bytes.
/// </summary>
internal sealed class VncDecoders
{
    private readonly VncStreamReader _reader;
    private readonly IVncJpegDecoder? _jpeg;
    private readonly ZlibInflater _zlib = new();
    private readonly ZlibInflater _zrle = new();
    private readonly ZlibInflater[] _tight = [new(), new(), new(), new()];
    private byte[] _scratch = new byte[64 * 1024];
    private readonly uint[] _tile = new uint[64 * 64];
    private readonly uint[] _palette = new uint[256];

    public VncDecoders(VncStreamReader reader, IVncJpegDecoder? jpeg)
    {
        _reader = reader;
        _jpeg = jpeg;
    }

    /// <summary>Decoded rectangles per encoding (diagnostics, tests).</summary>
    public Dictionary<int, int> Counts { get; } = new();

    /// <summary>Reads and applies one rectangle of a frame encoding.</summary>
    public void Decode(VncFramebuffer fb, int encoding, int x, int y, int width, int height)
    {
        fb.Check(x, y, width, height);
        switch (encoding)
        {
            case VncEncoding.Raw: Raw(fb, x, y, width, height); break;
            case VncEncoding.CopyRect:
                int sx = _reader.ReadUInt16(), sy = _reader.ReadUInt16();
                fb.Copy(sx, sy, x, y, width, height);
                break;
            case VncEncoding.Rre: Rre(fb, x, y, width, height); break;
            case VncEncoding.Hextile: Hextile(fb, x, y, width, height); break;
            case VncEncoding.Zlib: Zlib(fb, x, y, width, height); break;
            case VncEncoding.Zrle: Zrle(fb, x, y, width, height); break;
            case VncEncoding.Tight: Tight(fb, x, y, width, height); break;
            default: throw new VncProtocolException($"The server used encoding {encoding}, which was not offered.");
        }
        fb.Touch(x, y, width, height);
        Counts[encoding] = Counts.GetValueOrDefault(encoding) + 1;
    }

    /// <summary>Reads a Cursor pseudo-encoding rectangle: pixels then a 1-bit mask (rows padded to bytes).</summary>
    public VncCursorShape ReadCursor(int hotspotX, int hotspotY, int width, int height)
    {
        if (width == 0 || height == 0) return new VncCursorShape(0, 0, 0, 0, []);
        if (width > 512 || height > 512) throw new VncProtocolException($"A {width}×{height} pointer is not plausible.");
        var pixels = _reader.ReadBytes(width * height * 4);
        int maskStride = (width + 7) / 8;
        var mask = _reader.ReadBytes(maskStride * height);
        for (int row = 0; row < height; row++)
            for (int col = 0; col < width; col++)
                pixels[(row * width + col) * 4 + 3] = (mask[row * maskStride + col / 8] & (0x80 >> (col % 8))) != 0 ? (byte)255 : (byte)0;
        return new VncCursorShape(width, height, Math.Min(hotspotX, width - 1), Math.Min(hotspotY, height - 1), pixels);
    }

    /// <summary>Tight streams restart with the next connection.</summary>
    public void Reset()
    {
        _zlib.Reset();
        _zrle.Reset();
        foreach (var z in _tight) z.Reset();
    }

    // ---- simple encodings -----------------------------------------------------------

    private void Raw(VncFramebuffer fb, int x, int y, int width, int height)
    {
        for (int row = 0; row < height; row++)
        {
            var dest = fb.Pixels.AsSpan((y + row) * fb.Stride + x * 4, width * 4);
            _reader.ReadExactly(dest);
            Opaque(dest);
        }
    }

    // The X byte arrives as whatever the server puts there (usually 0): make it opaque for readers that use it.
    private static void Opaque(Span<byte> bgrx)
    {
        for (int i = 3; i < bgrx.Length; i += 4) bgrx[i] = 255;
    }

    private uint ReadPixel() => _reader.ReadPixel() | 0xFF000000;

    private void Rre(VncFramebuffer fb, int x, int y, int width, int height)
    {
        uint count = _reader.ReadUInt32();
        fb.Fill(x, y, width, height, ReadPixel());
        for (uint i = 0; i < count; i++)
        {
            uint pixel = ReadPixel();
            int sx = _reader.ReadUInt16(), sy = _reader.ReadUInt16(), sw = _reader.ReadUInt16(), sh = _reader.ReadUInt16();
            if (sx + sw > width || sy + sh > height) throw new VncProtocolException("An RRE subrectangle leaves its rectangle.");
            fb.Fill(x + sx, y + sy, sw, sh, pixel);
        }
    }

    private void Hextile(VncFramebuffer fb, int x, int y, int width, int height)
    {
        uint background = 0xFF000000, foreground = 0xFFFFFFFF;
        for (int ty = y; ty < y + height; ty += 16)
        {
            int th = Math.Min(16, y + height - ty);
            for (int tx = x; tx < x + width; tx += 16)
            {
                int tw = Math.Min(16, x + width - tx);
                int sub = _reader.ReadByte();
                if ((sub & 1) != 0)
                {
                    Raw(fb, tx, ty, tw, th);
                    continue;
                }
                if ((sub & 2) != 0) background = ReadPixel();
                fb.Fill(tx, ty, tw, th, background);
                if ((sub & 4) != 0) foreground = ReadPixel();
                if ((sub & 8) == 0) continue;
                int count = _reader.ReadByte();
                bool colored = (sub & 16) != 0;
                for (int i = 0; i < count; i++)
                {
                    uint pixel = colored ? ReadPixel() : foreground;
                    int xy = _reader.ReadByte(), wh = _reader.ReadByte();
                    int sx = xy >> 4, sy = xy & 15, sw = (wh >> 4) + 1, sh = (wh & 15) + 1;
                    if (sx + sw > tw || sy + sh > th) throw new VncProtocolException("A Hextile subrectangle leaves its tile.");
                    fb.Fill(tx + sx, ty + sy, sw, sh, pixel);
                }
            }
        }
    }

    private void Zlib(VncFramebuffer fb, int x, int y, int width, int height)
    {
        var data = _zlib.Inflate(ReadChunk(_reader.ReadUInt32()));
        if (data.Length != width * height * 4) throw new VncProtocolException("Zlib rectangle data has the wrong size.");
        for (int row = 0; row < height; row++)
        {
            var dest = fb.Pixels.AsSpan((y + row) * fb.Stride + x * 4, width * 4);
            data.Slice(row * width * 4, width * 4).CopyTo(dest);
            Opaque(dest);
        }
    }

    private ReadOnlySpan<byte> ReadChunk(uint length)
    {
        if (length > 64 * 1024 * 1024) throw new VncProtocolException($"A {length}-byte rectangle is not plausible.");
        if (_scratch.Length < length) _scratch = new byte[Math.Max((int)length, _scratch.Length * 2)];
        var chunk = _scratch.AsSpan(0, (int)length);
        _reader.ReadExactly(chunk);
        return chunk;
    }

    // ---- ZRLE (RFC 6143 §7.7.6) -------------------------------------------------------

    private void Zrle(VncFramebuffer fb, int x, int y, int width, int height)
    {
        var data = _zrle.Inflate(ReadChunk(_reader.ReadUInt32()));
        int p = 0;
        for (int ty = y; ty < y + height; ty += 64)
        {
            int th = Math.Min(64, y + height - ty);
            for (int tx = x; tx < x + width; tx += 64)
            {
                int tw = Math.Min(64, x + width - tx);
                ZrleTile(data, ref p, tw, th);
                for (int row = 0; row < th; row++)
                    _tile.AsSpan(row * tw, tw).CopyTo(fb.Row(ty + row).Slice(tx, tw));
            }
        }
    }

    private void ZrleTile(ReadOnlySpan<byte> data, ref int p, int tw, int th)
    {
        int count = tw * th;
        var tile = _tile.AsSpan(0, count);
        int sub = Byte(data, ref p);
        if (sub == 0)
        {
            for (int i = 0; i < count; i++) tile[i] = CPixel(data, ref p);
        }
        else if (sub == 1)
        {
            tile.Fill(CPixel(data, ref p));
        }
        else if (sub <= 16)
        {
            for (int i = 0; i < sub; i++) _palette[i] = CPixel(data, ref p);
            int bits = sub == 2 ? 1 : sub <= 4 ? 2 : 4;
            int mask = (1 << bits) - 1;
            for (int row = 0; row < th; row++)
            {
                int shift = 8, b = 0;
                for (int col = 0; col < tw; col++)
                {
                    if (shift == 0 || shift == 8) { b = Byte(data, ref p); shift = 8; }
                    shift -= bits;
                    int index = (b >> shift) & mask;
                    if (index >= sub) throw new VncProtocolException("ZRLE palette index out of range.");
                    tile[row * tw + col] = _palette[index];
                }
            }
        }
        else if (sub == 128)
        {
            for (int i = 0; i < count;)
            {
                uint pixel = CPixel(data, ref p);
                int run = RunLength(data, ref p);
                if (i + run > count) throw new VncProtocolException("A ZRLE run leaves its tile.");
                tile.Slice(i, run).Fill(pixel);
                i += run;
            }
        }
        else if (sub >= 130)
        {
            int size = sub - 128;
            for (int i = 0; i < size; i++) _palette[i] = CPixel(data, ref p);
            for (int i = 0; i < count;)
            {
                int index = Byte(data, ref p);
                int run = 1;
                if ((index & 0x80) != 0)
                {
                    index &= 0x7F;
                    run = RunLength(data, ref p);
                }
                if (index >= size) throw new VncProtocolException("ZRLE palette index out of range.");
                if (i + run > count) throw new VncProtocolException("A ZRLE run leaves its tile.");
                tile.Slice(i, run).Fill(_palette[index]);
                i += run;
            }
        }
        else
        {
            throw new VncProtocolException($"Unknown ZRLE subencoding {sub}.");
        }
    }

    private static int Byte(ReadOnlySpan<byte> data, ref int p)
    {
        if (p >= data.Length) throw new VncProtocolException("ZRLE data ends inside a tile.");
        return data[p++];
    }

    // CPIXEL: the pixel's three colour bytes (the unused byte dropped), B, G, R in our format.
    private static uint CPixel(ReadOnlySpan<byte> data, ref int p)
    {
        if (p + 3 > data.Length) throw new VncProtocolException("ZRLE data ends inside a tile.");
        uint v = data[p] | (uint)data[p + 1] << 8 | (uint)data[p + 2] << 16 | 0xFF000000;
        p += 3;
        return v;
    }

    private static int RunLength(ReadOnlySpan<byte> data, ref int p)
    {
        int run = 1, b;
        do
        {
            b = Byte(data, ref p);
            run += b;
        } while (b == 255);
        return run;
    }

    // ---- Tight (TightVNC / libvncserver / TigerVNC) ---------------------------------

    private void Tight(VncFramebuffer fb, int x, int y, int width, int height)
    {
        int control = _reader.ReadByte();
        for (int i = 0; i < 4; i++)
            if ((control & (1 << i)) != 0) _tight[i].Reset();
        int type = control >> 4;

        if (type == 8)
        {
            // Fill: one TPIXEL.
            Span<byte> rgb = stackalloc byte[3];
            _reader.ReadExactly(rgb);
            fb.Fill(x, y, width, height, Rgb(rgb[0], rgb[1], rgb[2]));
            return;
        }
        if (type == 9)
        {
            var jpeg = ReadChunk((uint)CompactLength());
            if (_jpeg == null) throw new VncProtocolException("The server sent JPEG, which was not offered.");
            var dest = fb.Pixels.AsSpan(y * fb.Stride + x * 4);
            if (!_jpeg.Decode(jpeg, width, height, dest, fb.Stride))
                throw new VncProtocolException("A Tight JPEG rectangle could not be decoded.");
            return;
        }
        if (type > 9) throw new VncProtocolException($"Unknown Tight compression {type}.");

        // Basic compression: stream (bits 4-5), optional filter id (bit 6).
        int stream = type & 3;
        int filter = (type & 4) != 0 ? _reader.ReadByte() : 0;
        int colors = 0;
        int dataSize;
        switch (filter)
        {
            case 0:  // copy
            case 2:  // gradient
                dataSize = width * height * 3;
                break;
            case 1:  // palette
                colors = _reader.ReadByte() + 1;
                Span<byte> rgb = stackalloc byte[3];
                for (int i = 0; i < colors; i++)
                {
                    _reader.ReadExactly(rgb);
                    _palette[i] = Rgb(rgb[0], rgb[1], rgb[2]);
                }
                dataSize = colors == 2 ? (width + 7) / 8 * height : width * height;
                break;
            default:
                throw new VncProtocolException($"Unknown Tight filter {filter}.");
        }

        ReadOnlySpan<byte> data;
        if (dataSize < 12)
        {
            data = ReadChunk((uint)dataSize);
        }
        else
        {
            var compressed = ReadChunk((uint)CompactLength());
            data = _tight[stream].Inflate(compressed);
        }
        if (data.Length != dataSize) throw new VncProtocolException("Tight rectangle data has the wrong size.");

        if (filter == 1)
        {
            if (colors == 2)
            {
                int rowBytes = (width + 7) / 8;
                for (int row = 0; row < height; row++)
                {
                    var dest = fb.Row(y + row).Slice(x, width);
                    for (int col = 0; col < width; col++)
                        dest[col] = _palette[(data[row * rowBytes + col / 8] >> (7 - col % 8)) & 1];
                }
            }
            else
            {
                for (int row = 0; row < height; row++)
                {
                    var dest = fb.Row(y + row).Slice(x, width);
                    for (int col = 0; col < width; col++)
                    {
                        int index = data[row * width + col];
                        if (index >= colors) throw new VncProtocolException("Tight palette index out of range.");
                        dest[col] = _palette[index];
                    }
                }
            }
            return;
        }

        if (filter == 0)
        {
            for (int row = 0; row < height; row++)
            {
                var dest = fb.Row(y + row).Slice(x, width);
                int o = row * width * 3;
                for (int col = 0; col < width; col++, o += 3) dest[col] = Rgb(data[o], data[o + 1], data[o + 2]);
            }
            return;
        }

        // Gradient: each component predicted from left + above - above-left (clamped), plus the sent value.
        int stride = width * 3;
        if (_gradient.Length < stride * 2) _gradient = new byte[stride * 2];
        var previous = _gradient.AsSpan(0, stride);
        var current = _gradient.AsSpan(stride, stride);
        previous.Clear();
        for (int row = 0; row < height; row++)
        {
            var sent = data.Slice(row * stride, stride);
            for (int col = 0; col < width; col++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int i = col * 3 + c;
                    int left = col > 0 ? current[i - 3] : 0;
                    int above = previous[i];
                    int aboveLeft = col > 0 ? previous[i - 3] : 0;
                    int prediction = Math.Clamp(left + above - aboveLeft, 0, 255);
                    current[i] = (byte)(prediction + sent[i]);
                }
            }
            var dest = fb.Row(y + row).Slice(x, width);
            for (int col = 0; col < width; col++) dest[col] = Rgb(current[col * 3], current[col * 3 + 1], current[col * 3 + 2]);
            current.CopyTo(previous);
        }
    }

    private byte[] _gradient = [];

    // TPIXEL (24-bit, client format with 8-bit channels): R, G, B.
    private static uint Rgb(byte r, byte g, byte b) => 0xFF000000 | (uint)r << 16 | (uint)g << 8 | b;

    private int CompactLength()
    {
        int b = _reader.ReadByte();
        int length = b & 0x7F;
        if ((b & 0x80) != 0)
        {
            b = _reader.ReadByte();
            length |= (b & 0x7F) << 7;
            if ((b & 0x80) != 0) length |= _reader.ReadByte() << 14;
        }
        return length;
    }
}

/// <summary>RFB encoding numbers (frame and pseudo-encodings).</summary>
public static class VncEncoding
{
    public const int Raw = 0;
    public const int CopyRect = 1;
    public const int Rre = 2;
    public const int Hextile = 5;
    public const int Zlib = 6;
    public const int Tight = 7;
    public const int Zrle = 16;

    public const int DesktopSize = -223;
    public const int LastRect = -224;
    public const int Cursor = -239;
    public const int ExtendedDesktopSize = -308;
    public const int DesktopName = -307;

    /// <summary>Tight JPEG quality: level 0 (worst) to 9 (best).</summary>
    public static int JpegQuality(int level) => -32 + Math.Clamp(level, 0, 9);

    /// <summary>Tight / Zlib compression effort: level 0 (fastest) to 9 (smallest).</summary>
    public static int CompressLevel(int level) => -256 + Math.Clamp(level, 0, 9);

    /// <summary>The name of an encoding number, for logs and diagnostics.</summary>
    public static string Name(int encoding) => encoding switch
    {
        Raw => "Raw",
        CopyRect => "CopyRect",
        Rre => "RRE",
        Hextile => "Hextile",
        Zlib => "Zlib",
        Tight => "Tight",
        Zrle => "ZRLE",
        _ => encoding.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };
}
