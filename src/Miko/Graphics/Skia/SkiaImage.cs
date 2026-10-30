using Miko.Rendering;
using SkiaSharp;
using Svg.Skia;

namespace Miko.Graphics.Skia;

/// <summary>
/// <see cref="Image"/> 的 Skia 实现。三种存储恰有其一：解码出的位图（<see cref="Bitmap"/>）、
/// 后端图像（<see cref="SkImage"/>，CPU 光栅或 GPU 纹理）、矢量图（<see cref="Picture"/>）。
/// </summary>
internal sealed class SkiaImage : Image
{
    private readonly int _width;
    private readonly int _height;
    // 矢量图的 SKPicture 归解析它的 SKSvg 所有，释放 SKSvg 才释放它。
    private readonly SKSvg? _svg;

    public SkiaImage(SKBitmap bitmap)
    {
        Bitmap = bitmap;
        _width = bitmap.Width;
        _height = bitmap.Height;
    }

    public SkiaImage(SKImage image)
    {
        SkImage = image;
        _width = image.Width;
        _height = image.Height;
    }

    private SkiaImage(SKSvg svg, SKPicture picture)
    {
        _svg = svg;
        Picture = picture;
        var cull = picture.CullRect;
        _width = Math.Max(1, (int)MathF.Round(cull.Width));
        _height = Math.Max(1, (int)MathF.Round(cull.Height));
    }

    public SKBitmap? Bitmap { get; }

    public SKImage? SkImage { get; }

    public SKPicture? Picture { get; }

    public override int Width => _width;

    public override int Height => _height;

    public override bool IsVector => Picture != null;

    /// <summary>
    /// 光栅内容的 <see cref="SKImage"/>：位图经绘制器按实例缓存的包装（不重复拷贝像素），矢量图为 <c>null</c>。
    /// </summary>
    public SKImage? ToSKImage() => SkImage ?? (Bitmap != null ? Painter.ImageFor(Bitmap) : null);

    /// <summary>取出本库创建的图像；外部无法派生 <see cref="Image"/>，这里只防 <c>null</c>。</summary>
    public static SkiaImage Require(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        return (SkiaImage)image;
    }

    /// <summary>解析 SVG 为矢量图；不是有效 SVG 时返回 <c>null</c>。</summary>
    public static SkiaImage? FromSvg(Stream stream)
    {
        var svg = new SKSvg();
        try
        {
            svg.Load(stream);
        }
        catch
        {
            svg.Dispose();
            return null;
        }

        var picture = svg.Picture;
        if (picture == null || picture.CullRect.Width <= 0 || picture.CullRect.Height <= 0)
        {
            svg.Dispose();
            return null;
        }
        return new SkiaImage(svg, picture);
    }

    public static SkiaImage? FromPixels(int width, int height, ImagePixelFormat format, ReadOnlySpan<byte> pixels, int rowBytes)
    {
        if (width <= 0 || height <= 0) return null;
        var info = InfoFor(width, height, format);
        if (rowBytes == 0) rowBytes = info.RowBytes;
        if (rowBytes < info.RowBytes || pixels.Length < (long)rowBytes * (height - 1) + info.RowBytes) return null;

        // 有行填充时逐行紧凑化，让图像与源缓冲解耦（只拷贝一次）。
        if (rowBytes == info.RowBytes)
        {
            var image = SKImage.FromPixelCopy(info, pixels[..(info.RowBytes * height)]);
            return image == null ? null : new SkiaImage(image);
        }

        var compact = new byte[info.RowBytes * height];
        for (int y = 0; y < height; y++)
            pixels.Slice(y * rowBytes, info.RowBytes).CopyTo(compact.AsSpan(y * info.RowBytes));
        var copy = SKImage.FromPixelCopy(info, compact);
        return copy == null ? null : new SkiaImage(copy);
    }

    public static SkiaImage? FromPixels(int width, int height, ImagePixelFormat format, IntPtr pixels, int rowBytes)
    {
        if (width <= 0 || height <= 0 || pixels == IntPtr.Zero) return null;
        var info = InfoFor(width, height, format);
        if (rowBytes < info.RowBytes) return null;
        var image = SKImage.FromPixelCopy(info, pixels, rowBytes);
        return image == null ? null : new SkiaImage(image);
    }

    /// <summary>
    /// 把后端纹理包装为图像。用 <c>FromTexture</c>（非 <c>FromAdoptedTexture</c>）：纹理所有权仍属调用方，
    /// 图像释放时不得销毁它。
    /// </summary>
    public static SkiaImage? FromTexture(GRContext context, GRBackendTexture texture, ImagePixelFormat format)
    {
        var (colorType, alphaType) = ColorTypeFor(format);
        var image = SKImage.FromTexture(context, texture, GRSurfaceOrigin.TopLeft, colorType, alphaType);
        return image == null ? null : new SkiaImage(image);
    }

    private static SKImageInfo InfoFor(int width, int height, ImagePixelFormat format)
    {
        var (colorType, alphaType) = ColorTypeFor(format);
        return new SKImageInfo(width, height, colorType, alphaType);
    }

    private static (SKColorType ColorType, SKAlphaType AlphaType) ColorTypeFor(ImagePixelFormat format) => format switch
    {
        ImagePixelFormat.Rgba8888 => (SKColorType.Rgba8888, SKAlphaType.Unpremul),
        ImagePixelFormat.Bgra8888 => (SKColorType.Bgra8888, SKAlphaType.Unpremul),
        ImagePixelFormat.Rgbx8888 => (SKColorType.Rgba8888, SKAlphaType.Opaque),
        ImagePixelFormat.Bgrx8888 => (SKColorType.Bgra8888, SKAlphaType.Opaque),
        ImagePixelFormat.R8 => (SKColorType.R8Unorm, SKAlphaType.Opaque),
        ImagePixelFormat.Rg88 => (SKColorType.Rg88, SKAlphaType.Opaque),
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public override void Dispose()
    {
        Bitmap?.Dispose();
        SkImage?.Dispose();
        _svg?.Dispose();
    }
}
