using System.Runtime.InteropServices;

namespace Miko.Components.Vnc.Protocol;

/// <summary>
/// The remote screen as 32-bit pixels, bytes B, G, R, X (<c>ImagePixelFormat.Bgrx8888</c>), packed rows.
/// </summary>
/// <remarks>
/// One writer (the connection's receive thread decodes into it) and any number of readers (the view copies
/// changed tiles on the render thread). There is no lock: every write ends with <see cref="Touch"/>, which
/// bumps the version of each 128 × 128 tile it covers after the pixels are stored. A reader takes a tile's
/// version before copying it; a copy that races a write may tear, and the write's version bump makes the
/// reader copy the tile again on its next pass. A resize makes a new framebuffer.
/// </remarks>
public sealed class VncFramebuffer
{
    /// <summary>Side of the tiles change tracking works in, in pixels.</summary>
    public const int TileSize = 128;

    private readonly int[] _tileVersions;

    public VncFramebuffer(int width, int height)
    {
        if (width <= 0 || height <= 0 || width > 16384 || height > 16384)
            throw new ArgumentOutOfRangeException(nameof(width), $"{width}×{height} is not a framebuffer size.");
        Width = width;
        Height = height;
        Pixels = new byte[checked(width * height * 4)];
        TilesX = (width + TileSize - 1) / TileSize;
        TilesY = (height + TileSize - 1) / TileSize;
        _tileVersions = new int[TilesX * TilesY];
        // A new screen starts black, opaque in the X byte (a reader may treat it as alpha).
        MemoryMarshal.Cast<byte, uint>(Pixels.AsSpan()).Fill(0xFF000000);
    }

    public int Width { get; }

    public int Height { get; }

    public int Stride => Width * 4;

    /// <summary>The pixels. Readers must not write.</summary>
    public byte[] Pixels { get; }

    public int TilesX { get; }

    public int TilesY { get; }

    /// <summary>Incremented after each write that changes the tile (read with acquire semantics).</summary>
    public int TileVersion(int tileX, int tileY) => Volatile.Read(ref _tileVersions[tileY * TilesX + tileX]);

    internal Span<uint> Row(int y) => MemoryMarshal.Cast<byte, uint>(Pixels.AsSpan(y * Stride, Stride));

    /// <summary>Publishes a write: bumps the version of every tile the rectangle touches.</summary>
    internal void Touch(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        int tx0 = x / TileSize, ty0 = y / TileSize;
        int tx1 = Math.Min(TilesX - 1, (x + width - 1) / TileSize), ty1 = Math.Min(TilesY - 1, (y + height - 1) / TileSize);
        for (int ty = ty0; ty <= ty1; ty++)
            for (int tx = tx0; tx <= tx1; tx++)
                Interlocked.Increment(ref _tileVersions[ty * TilesX + tx]);
    }

    /// <summary>Throws unless the rectangle lies inside the screen (a server bug must not corrupt memory).</summary>
    internal void Check(int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || width < 0 || height < 0 || x + width > Width || y + height > Height)
            throw new VncProtocolException($"The rectangle {width}×{height} at ({x}, {y}) lies outside the {Width}×{Height} screen.");
    }

    internal void Fill(int x, int y, int width, int height, uint pixel)
    {
        for (int row = y; row < y + height; row++) Row(row).Slice(x, width).Fill(pixel);
    }

    /// <summary>Copies a rectangle within the screen; source and destination may overlap (CopyRect).</summary>
    internal void Copy(int sourceX, int sourceY, int x, int y, int width, int height)
    {
        Check(sourceX, sourceY, width, height);
        int rowBytes = width * 4;
        if (sourceY < y)
        {
            for (int row = height - 1; row >= 0; row--)
                Buffer.BlockCopy(Pixels, (sourceY + row) * Stride + sourceX * 4, Pixels, (y + row) * Stride + x * 4, rowBytes);
        }
        else
        {
            // BlockCopy handles the overlap within one row (same row, shifted sideways).
            for (int row = 0; row < height; row++)
                Buffer.BlockCopy(Pixels, (sourceY + row) * Stride + sourceX * 4, Pixels, (y + row) * Stride + x * 4, rowBytes);
        }
    }

    /// <summary>Copies the pixels of a rectangle into <paramref name="destination"/> as packed rows (Bgrx8888).</summary>
    public void CopyTo(int x, int y, int width, int height, Span<byte> destination)
    {
        Check(x, y, width, height);
        int rowBytes = width * 4;
        for (int row = 0; row < height; row++)
            Pixels.AsSpan((y + row) * Stride + x * 4, rowBytes).CopyTo(destination.Slice(row * rowBytes, rowBytes));
    }
}
