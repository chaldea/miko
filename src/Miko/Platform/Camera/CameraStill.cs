using Miko.Graphics;
using Miko.Graphics.Skia;
using SkiaSharp;

namespace Miko.Platform.Camera;

/// <summary>
/// A still as a backend captured it: encoded bytes (JPEG/PNG, possibly with an EXIF orientation) or
/// raw pixels, plus the rotation that still has to be applied.
/// <see cref="Core.DomElements.CameraElement.CapturePhotoAsync"/> turns it into an upright
/// <see cref="CameraPhoto"/>.
/// </summary>
public sealed class CameraStill
{
    private CameraStill(byte[]? encoded, int width, int height, ImagePixelFormat format, byte[]? pixels, int rowBytes, int rotation, bool isMirrored)
    {
        EncodedData = encoded;
        Width = width;
        Height = height;
        PixelFormat = format;
        Pixels = pixels;
        RowBytes = rowBytes;
        Rotation = CameraOrientation.Normalize(rotation);
        IsMirrored = isMirrored;
    }

    /// <summary>Encoded image bytes; null for a pixel still.</summary>
    public byte[]? EncodedData { get; }

    /// <summary>Pixel width of a pixel still; 0 for an encoded still.</summary>
    public int Width { get; }

    public int Height { get; }

    public ImagePixelFormat PixelFormat { get; }

    public byte[]? Pixels { get; }

    public int RowBytes { get; }

    /// <summary>Clockwise rotation to apply after any EXIF orientation of the encoded data.</summary>
    public int Rotation { get; }

    /// <summary>Whether the preview showed this mirrored (front camera).</summary>
    public bool IsMirrored { get; }

    /// <summary>A still delivered encoded by the camera pipeline.</summary>
    public static CameraStill FromEncoded(byte[] data, int rotation = 0, bool isMirrored = false)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0) throw new ArgumentException("The still is empty.", nameof(data));
        return new CameraStill(data, 0, 0, default, null, 0, rotation, isMirrored);
    }

    /// <summary>A still taken from a preview frame (32-bit RGB formats only).</summary>
    public static CameraStill FromPixels(int width, int height, ImagePixelFormat format, byte[] pixels, int rowBytes, int rotation = 0, bool isMirrored = false)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (format is ImagePixelFormat.R8 or ImagePixelFormat.Rg88) throw new ArgumentException("A still needs an RGB pixel format.", nameof(format));
        if (rowBytes == 0) rowBytes = width * 4;
        if (rowBytes < width * 4 || pixels.Length < (long)rowBytes * (height - 1) + width * 4)
            throw new ArgumentException("The pixel buffer is smaller than the still.", nameof(pixels));
        return new CameraStill(null, width, height, format, pixels, rowBytes, rotation, isMirrored);
    }
}

/// <summary>An upright, encoded photo.</summary>
/// <param name="Data">The encoded image.</param>
/// <param name="Format">Its encoding.</param>
/// <param name="Width">Pixel width as displayed.</param>
/// <param name="Height">Pixel height as displayed.</param>
public sealed record CameraPhoto(byte[] Data, CameraPhotoFormat Format, int Width, int Height)
{
    /// <summary><c>image/jpeg</c> or <c>image/png</c>.</summary>
    public string MimeType => Format == CameraPhotoFormat.Png ? "image/png" : "image/jpeg";

    /// <summary>The usual file extension, with the dot.</summary>
    public string FileExtension => Format == CameraPhotoFormat.Png ? ".png" : ".jpg";

    /// <summary>Writes the photo to a file.</summary>
    public Task SaveAsync(string path, CancellationToken cancellationToken = default)
        => File.WriteAllBytesAsync(path, Data, cancellationToken);
}

/// <summary>Turns a <see cref="CameraStill"/> into an upright photo (decode, orient, scale, encode).</summary>
internal static class CameraPhotoEncoder
{
    public static CameraPhoto Encode(CameraStill still, CameraCaptureOptions options)
    {
        ArgumentNullException.ThrowIfNull(still);
        ArgumentNullException.ThrowIfNull(options);
        bool mirror = still.IsMirrored && options.MirrorFrontCamera;

        using var source = Decode(still, out var origin, out bool isJpeg);
        var (rotation, flip) = Compose(origin, still.Rotation, mirror);

        int uprightWidth = rotation % 180 == 0 ? source.Width : source.Height;
        int uprightHeight = rotation % 180 == 0 ? source.Height : source.Width;
        float scale = 1;
        if (options.MaxDimension is int max && max > 0 && Math.Max(uprightWidth, uprightHeight) > max)
            scale = max / (float)Math.Max(uprightWidth, uprightHeight);
        int width = Math.Max(1, (int)Math.Round(uprightWidth * scale));
        int height = Math.Max(1, (int)Math.Round(uprightHeight * scale));

        // Untouched JPEG: hand the camera's own bytes back (no generation loss, metadata kept).
        if (still.EncodedData != null && isJpeg && options.Format == CameraPhotoFormat.Jpeg && rotation == 0 && !flip && scale == 1)
            return new CameraPhoto(still.EncodedData, CameraPhotoFormat.Jpeg, width, height);

        using var target = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(target))
        {
            canvas.Translate(width / 2f, height / 2f);
            if (flip) canvas.Scale(-1, 1);
            canvas.RotateDegrees(rotation);
            canvas.Scale(scale);
            using var image = SKImage.FromBitmap(source);
            // Quarter turns at full size map pixels one to one; only scaling needs a filter.
            var sampling = scale == 1 ? new SKSamplingOptions(SKFilterMode.Nearest) : new SKSamplingOptions(SKCubicResampler.Mitchell);
            canvas.DrawImage(image, SKRect.Create(-source.Width / 2f, -source.Height / 2f, source.Width, source.Height), sampling);
        }

        var format = options.Format == CameraPhotoFormat.Png ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
        using var data = target.Encode(format, Math.Clamp(options.Quality, 1, 100))
            ?? throw new InvalidOperationException("The photo could not be encoded.");
        return new CameraPhoto(data.ToArray(), options.Format, width, height);
    }

    private static SKBitmap Decode(CameraStill still, out SKEncodedOrigin origin, out bool isJpeg)
    {
        if (still.EncodedData is { } encoded)
        {
            using var stream = new SKMemoryStream(encoded);
            using var codec = SKCodec.Create(stream) ?? throw new InvalidOperationException("The camera returned an image that cannot be decoded.");
            origin = codec.EncodedOrigin;
            isJpeg = codec.EncodedFormat == SKEncodedImageFormat.Jpeg;
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bitmap = new SKBitmap(info);
            var result = codec.GetPixels(info, bitmap.GetPixels());
            if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
            {
                bitmap.Dispose();
                throw new InvalidOperationException($"The camera returned an image that cannot be decoded ({result}).");
            }
            return bitmap;
        }

        origin = SKEncodedOrigin.TopLeft;
        isJpeg = false;
        using var image = SkiaImage.FromPixels(still.Width, still.Height, still.PixelFormat, still.Pixels!, still.RowBytes)
            ?? throw new InvalidOperationException("The still has no pixels.");
        var raster = image.ToSKImage() ?? throw new InvalidOperationException("The still has no pixels.");
        var copy = new SKBitmap(new SKImageInfo(still.Width, still.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (!raster.ReadPixels(copy.Info, copy.GetPixels(), copy.RowBytes, 0, 0))
        {
            copy.Dispose();
            throw new InvalidOperationException("The still has no pixels.");
        }
        return copy;
    }

    /// <summary>
    /// The EXIF origin, then the backend's extra rotation, then the optional mirror, as one clockwise
    /// rotation followed by an optional horizontal flip.
    /// </summary>
    internal static (int Rotation, bool Flip) Compose(SKEncodedOrigin origin, int extraRotation, bool mirror)
    {
        // EXIF origin → (rotation, flip) where flip is applied after the rotation.
        var (rotation, flip) = origin switch
        {
            SKEncodedOrigin.TopRight => (0, true),
            SKEncodedOrigin.BottomRight => (180, false),
            SKEncodedOrigin.BottomLeft => (180, true),
            SKEncodedOrigin.LeftTop => (90, true),
            SKEncodedOrigin.RightTop => (90, false),
            SKEncodedOrigin.RightBottom => (270, true),
            SKEncodedOrigin.LeftBottom => (270, false),
            _ => (0, false),
        };
        // A further clockwise rotation r after a flip F: R(r)·F·R(a) = F·R(-r)·R(a).
        rotation = CameraOrientation.Normalize(flip ? rotation - extraRotation : rotation + extraRotation);
        if (mirror) flip = !flip;
        return (rotation, flip);
    }
}
