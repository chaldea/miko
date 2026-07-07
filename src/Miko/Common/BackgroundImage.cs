using SkiaSharp;
using Svg.Skia;
using System.Reflection;

namespace Miko.Common;

public class BackgroundImage
{
    private SKBitmap? _bitmap;
    private SKPicture? _picture;
    private float _viewBoxWidth;
    private float _viewBoxHeight;

    public SKBitmap? Bitmap
    {
        get
        {
            if (_bitmap != null) return _bitmap;
            if (_picture != null)
            {
                _bitmap = RenderPictureToBitmap((int)_viewBoxWidth, (int)_viewBoxHeight);
            }
            return _bitmap;
        }
        private set => _bitmap = value;
    }

    public float OriginalWidth => _bitmap?.Width ?? _viewBoxWidth;
    public float OriginalHeight => _bitmap?.Height ?? _viewBoxHeight;

    public BackgroundRepeat Repeat { get; set; } = BackgroundRepeat.Repeat;
    public BackgroundSize Size { get; set; } = BackgroundSize.Auto;
    public BackgroundPosition Position { get; set; } = BackgroundPosition.LeftTop;

    /// <summary>
    /// Marks this image as a monochrome "template" (e.g. an Ionicons SVG glyph) whose shape
    /// should be tinted with the element's <c>color</c> at draw time — mirroring CSS
    /// <c>fill: currentColor</c>. The image's alpha channel is used as a mask; its own RGB is
    /// ignored. Colorful images (logos, photos) must leave this <c>false</c> so they render as-is.
    /// </summary>
    public bool IsTemplate { get; set; }

    private BackgroundImage() { }

    public static BackgroundImage FromFile(string path)
    {
        var image = new BackgroundImage();
        if (IsSvgFile(path))
        {
            using var stream = File.OpenRead(path);
            image.LoadSvg(stream);
        }
        else
        {
            using var stream = File.OpenRead(path);
            image._bitmap = SKBitmap.Decode(stream);
        }
        return image;
    }

    public static BackgroundImage FromBase64(string base64)
    {
        var image = new BackgroundImage();
        var bytes = Convert.FromBase64String(base64);
        image._bitmap = SKBitmap.Decode(bytes);
        return image;
    }

    public static BackgroundImage FromResource(Assembly assembly, string resourceName)
    {
        var image = new BackgroundImage();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly '{assembly.FullName}'.");

        if (IsSvgResource(resourceName))
            image.LoadSvg(stream);
        else
            image._bitmap = SKBitmap.Decode(stream);

        return image;
    }

    public static BackgroundImage FromStream(Stream stream)
    {
        var image = new BackgroundImage();
        image._bitmap = SKBitmap.Decode(stream);
        return image;
    }

    public static BackgroundImage FromBytes(byte[] data)
    {
        var image = new BackgroundImage();
        image._bitmap = SKBitmap.Decode(data);
        return image;
    }

    public static BackgroundImage FromBitmap(SKBitmap bitmap)
    {
        return new BackgroundImage { _bitmap = bitmap };
    }

    public static BackgroundImage FromSvgStream(Stream stream)
    {
        var image = new BackgroundImage();
        image.LoadSvg(stream);
        return image;
    }

    public SKBitmap? RenderAtSize(int width, int height)
    {
        if (_picture != null)
            return RenderPictureToBitmap(width, height);
        return _bitmap;
    }

    private void LoadSvg(Stream stream)
    {
        var svg = new SKSvg();
        svg.Load(stream);
        if (svg.Picture != null)
        {
            _picture = svg.Picture;
            _viewBoxWidth = svg.Picture.CullRect.Width;
            _viewBoxHeight = svg.Picture.CullRect.Height;
            if (_viewBoxWidth <= 0) _viewBoxWidth = 16;
            if (_viewBoxHeight <= 0) _viewBoxHeight = 16;
        }
    }

    private SKBitmap RenderPictureToBitmap(int width, int height)
    {
        if (width <= 0) width = 16;
        if (height <= 0) height = 16;

        // 创建高质量的 Bitmap，启用抗锯齿以改善 SVG 渲染质量
        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(imageInfo);
        using var canvas = new SKCanvas(bitmap);

        // 启用高质量渲染选项
        canvas.Clear(SKColors.Transparent);

        var scaleX = width / _picture!.CullRect.Width;
        var scaleY = height / _picture.CullRect.Height;
        canvas.Scale(scaleX, scaleY);

        // 使用高质量的绘制选项，启用抗锯齿
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawPicture(_picture, paint);

        return bitmap;
    }

    private static bool IsSvgFile(string path) =>
        path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);

    private static bool IsSvgResource(string resourceName) =>
        resourceName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
}
