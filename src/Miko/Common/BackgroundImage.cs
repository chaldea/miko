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

    /// <summary>
    /// <see cref="RenderAtSize"/> 的栅格化缓存，按目标像素尺寸索引。没有它，渲染循环会
    /// 每帧重新栅格化每一个 SVG 背景（ISSUE-137 按设备像素放大后尺寸更大、代价更高）。
    ///
    /// <para>必须允许多个尺寸并存：同一个 <see cref="BackgroundImage"/> 实例被跨元素共享
    /// （<c>Miko.Ionic.IconResolver</c> 按图标名缓存一份，页面上同名图标可能有 16/24/32 多种
    /// 尺寸，还会叠加不同的设备缩放）。单槽缓存在这种页面上会每帧互相顶掉，退化成没有缓存。</para>
    /// </summary>
    private readonly Dictionary<(int Width, int Height), SKBitmap> _rasterCache = new();

    /// <summary>插入顺序，用于在缓存满时淘汰最早的条目。</summary>
    private readonly List<(int Width, int Height)> _rasterCacheOrder = new();

    /// <summary>
    /// 缓存容量。同一图标同时出现的尺寸种类通常远小于该值（Ionic 的图标尺寸档位有限），
    /// 取 8 既能覆盖常见页面，又把单张图的位图内存上限约束在可预期范围内。
    /// </summary>
    private const int RasterCacheCapacity = 8;

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

    /// <summary>
    /// 取该图在给定<b>像素</b>尺寸下的位图。矢量源（SVG）按该尺寸栅格化并缓存；
    /// 位图源是固定分辨率，忽略尺寸直接返回原图（放大它只会插值，不会更清晰）。
    ///
    /// <para>调用方传入的应当是<b>设备像素</b>尺寸（逻辑尺寸 × <c>Painter.DeviceScale</c>），
    /// 见 ISSUE-137。</para>
    /// </summary>
    public SKBitmap? RenderAtSize(int width, int height)
    {
        if (_picture == null)
            return _bitmap;

        if (width <= 0) width = 16;
        if (height <= 0) height = 16;

        var key = (width, height);
        if (_rasterCache.TryGetValue(key, out var cached))
            return cached;

        var bitmap = RenderPictureToBitmap(width, height);

        // 淘汰最早的条目并释放其位图：SKBitmap 是 native 内存，丢引用不等于释放。
        if (_rasterCacheOrder.Count >= RasterCacheCapacity)
        {
            var oldest = _rasterCacheOrder[0];
            _rasterCacheOrder.RemoveAt(0);
            if (_rasterCache.Remove(oldest, out var evicted))
                evicted.Dispose();
        }

        _rasterCache[key] = bitmap;
        _rasterCacheOrder.Add(key);
        return bitmap;
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
