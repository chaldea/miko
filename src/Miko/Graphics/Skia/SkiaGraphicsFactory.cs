using Miko.Fonts;
using Miko.Rendering;
using Miko.Text;
using SkiaSharp;

namespace Miko.Graphics.Skia;

/// <summary>
/// <see cref="IGraphicsFactory"/> 的 Skia 实现。没有实例状态（文本缓存与字体注册表本就是进程级、
/// 按内容寻址的），所以全进程共用一个实例；容器里注册的也是它。
/// </summary>
internal sealed class SkiaGraphicsFactory : IGraphicsFactory
{
    public static readonly SkiaGraphicsFactory Instance = new();

    private SkiaGraphicsFactory()
    {
    }

    public TextBlock CreateText(string text, TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(style.FontFamily, nameof(style));
        if (!float.IsFinite(style.FontSize) || style.FontSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(style), "The font size must be positive.");

        var shaped = TextShaping.Shape(text, style.FontFamily, style.FontSize, style.FontWeight, style.Direction, style.LetterSpacing);
        var typeface = shaped.BaselineTypeface
            ?? FontManager.Instance.GetTypeface(style.FontFamily, style.FontWeight)
            ?? SKTypeface.Default;
        var metrics = Painter.CreateHighQualityFont(typeface, style.FontSize).Metrics;
        return new TextBlock(text, style, shaped, -metrics.Ascent, metrics.Descent);
    }

    public Image? DecodeImage(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return DecodeImage(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
    }

    public Image? DecodeImage(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return null;
        if (LooksLikeSvg(data))
        {
            using var svg = new MemoryStream(data.ToArray(), writable: false);
            return SkiaImage.FromSvg(svg);
        }

        // SKBitmap.Decode throws on data no codec recognizes; the contract here is to return null.
        using var skData = SKData.CreateCopy(data);
        using var codec = SKCodec.Create(skData);
        if (codec == null) return null;
        var bitmap = SKBitmap.Decode(codec);
        return bitmap == null ? null : new SkiaImage(bitmap);
    }

    public Image? CreateImage(int width, int height, ImagePixelFormat format, ReadOnlySpan<byte> pixels, int rowBytes = 0)
        => SkiaImage.FromPixels(width, height, format, pixels, rowBytes);

    public RenderSurface CreateSurface(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        return GraphicsDevice.Software.CreateSurface(width, height)
            ?? throw new InvalidOperationException($"Could not create a {width}x{height} surface.");
    }

    /// <summary>
    /// SVG 是文本：跳过 BOM 与空白后以 <c>&lt;</c> 开头，且开头一段里出现 <c>&lt;svg</c>。
    /// 位图格式都以二进制魔数开头，不会误判。
    /// </summary>
    private static bool LooksLikeSvg(ReadOnlySpan<byte> data)
    {
        var head = data[..Math.Min(data.Length, 1024)];
        int i = 0;
        if (head.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF])) i = 3;
        while (i < head.Length && head[i] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n') i++;
        if (i >= head.Length || head[i] != (byte)'<') return false;

        ReadOnlySpan<byte> tag = "<svg"u8;
        for (; i + tag.Length <= head.Length; i++)
        {
            if (head.Slice(i, tag.Length).SequenceEqual(tag)) return true;
        }
        return false;
    }
}
