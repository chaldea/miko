using Miko.Components.Vnc.Protocol;
using Miko.Graphics;

namespace Miko.Components.Vnc;

/// <summary>Tight's JPEG rectangles through the engine's image decoder (no extra codec, no native package).</summary>
internal sealed class VncJpegDecoder(IGraphicsFactory graphics) : IVncJpegDecoder
{
    public bool Decode(ReadOnlySpan<byte> jpeg, int width, int height, Span<byte> destination, int stride)
    {
        using var image = graphics.DecodeImage(jpeg);
        if (image == null || image.Width != width || image.Height != height) return false;
        // Opaque JPEG: B, G, R, 255 — the framebuffer's layout.
        return image.ReadPixels(destination, ImagePixelFormat.Bgra8888, stride);
    }
}
