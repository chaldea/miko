using System.Collections.Concurrent;
using SkiaSharp;

namespace MikoApp.Media.Api;

/// <summary>
/// 离线资源工厂：用 SkiaSharp 现绘产品缩略图（按 id 派生颜色与编号），用 FFmpeg 生成样例视频。
/// 全部本地生成，demo 不依赖外网。结果按 id 缓存避免重复绘制。
/// </summary>
public static class AssetFactory
{
    private const int ThumbSize = 240;

    // 一组柔和的底色，按 id 轮选，让网格更有辨识度。
    private static readonly SKColor[] Palette =
    {
        new(0x4F, 0x6D, 0x7A), new(0xC0, 0x6C, 0x84), new(0x6C, 0x5B, 0x7B),
        new(0x35, 0x5C, 0x7D), new(0xE8, 0x9F, 0x3C), new(0x2E, 0x86, 0x6B),
        new(0xB5, 0x4D, 0x4D), new(0x55, 0x7A, 0x95), new(0x8E, 0x6C, 0x88),
        new(0x3D, 0x5A, 0x40),
    };

    private static readonly ConcurrentDictionary<int, byte[]> _thumbCache = new();
    private static readonly Lazy<string> _sampleVideo = new(TestClip.EnsureSampleClip);

    /// <summary>样例视频文件路径（首次访问时离线编码）。</summary>
    public static string SampleVideoPath => _sampleVideo.Value;

    /// <summary>返回某产品缩略图的 PNG 字节（按 id 缓存）。</summary>
    public static byte[] Thumbnail(int id) => _thumbCache.GetOrAdd(id, RenderThumbnail);

    private static byte[] RenderThumbnail(int id)
    {
        var bg = Palette[id % Palette.Length];

        var info = new SKImageInfo(ThumbSize, ThumbSize);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        // 斜向双色渐变底
        using (var bgPaint = new SKPaint())
        {
            bgPaint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(ThumbSize, ThumbSize),
                new[] { bg, Darken(bg, 0.65f) },
                null,
                SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, ThumbSize, ThumbSize, bgPaint);
        }

        // 居中编号
        using (var textPaint = new SKPaint())
        {
            textPaint.Color = SKColors.White.WithAlpha(235);
            textPaint.IsAntialias = true;
            using var font = new SKFont(SKTypeface.Default, 96);
            var label = $"#{id}";
            var width = font.MeasureText(label, textPaint);
            canvas.DrawText(label, (ThumbSize - width) / 2f, ThumbSize / 2f + 34, SKTextAlign.Left, font, textPaint);
        }

        // 底部标签条
        using (var barPaint = new SKPaint { Color = SKColors.Black.WithAlpha(60) })
            canvas.DrawRect(0, ThumbSize - 40, ThumbSize, 40, barPaint);

        using (var capPaint = new SKPaint())
        {
            capPaint.Color = SKColors.White.WithAlpha(220);
            capPaint.IsAntialias = true;
            using var font = new SKFont(SKTypeface.Default, 20);
            canvas.DrawText("Miko thumbnail", 12, ThumbSize - 14, SKTextAlign.Left, font, capPaint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private static SKColor Darken(SKColor c, float factor) =>
        new((byte)(c.Red * factor), (byte)(c.Green * factor), (byte)(c.Blue * factor), c.Alpha);
}
