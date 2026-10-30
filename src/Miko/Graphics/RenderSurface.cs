using Miko.Common;
using Miko.Graphics.Skia;
using SkiaSharp;

namespace Miko.Graphics;

/// <summary>
/// 一块可绘制的像素表面：CPU 离屏表面、GPU 离屏表面或窗口帧缓冲（ISSUE-162）。
/// 在 <see cref="Context"/> 上绘制，用 <see cref="Snapshot"/> / <see cref="EncodePng"/> 取回结果。
///
/// <para>由 <see cref="IGraphicsFactory.CreateSurface"/>、<see cref="GraphicsDevice.CreateSurface"/>、
/// <see cref="GraphicsDevice.CreateMetalSurface"/> 与 <see cref="WindowRenderTarget.Acquire"/> 创建。
/// 后者返回的表面归渲染目标所有，调用方不得释放；其余由调用方释放。</para>
///
/// <para>不是线程安全的：GPU 表面只能在持有其上下文的渲染线程使用。</para>
/// </summary>
public sealed class RenderSurface : IDisposable
{
    private readonly SKSurface _surface;
    private readonly SkiaDrawingContext _context;
    private readonly IDisposable? _target;
    private bool _disposed;

    /// <param name="target">与表面同生共死的后端对象（如包装的渲染目标），在表面之后释放。</param>
    internal RenderSurface(SKSurface surface, int width, int height, IDisposable? target = null)
    {
        _surface = surface;
        _context = new SkiaDrawingContext(surface.Canvas);
        _target = target;
        Width = width;
        Height = height;
    }

    /// <summary>像素宽度。</summary>
    public int Width { get; }

    /// <summary>像素高度。</summary>
    public int Height { get; }

    /// <summary>在本表面上绘制的上下文（逻辑坐标初始等于像素坐标）。</summary>
    public IDrawingContext Context => _context;

    internal SKSurface Surface => _surface;

    /// <summary>把已记录的绘制命令提交给后端（GPU 表面在合成或呈现之前调用）。</summary>
    public void Flush() => _surface.Flush();

    /// <summary>当前内容的快照（独立于之后的绘制）。调用方拥有返回值。</summary>
    public Image Snapshot() => new SkiaImage(_surface.Snapshot());

    /// <summary>
    /// 把当前内容编码为 PNG。按像素读回再编码，GPU 表面（包括复用的窗口表面）也总是拿到最新一帧。
    /// </summary>
    public byte[] EncodePng()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (!_surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
            throw new InvalidOperationException("The surface could not be read back.");
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("The surface could not be encoded as PNG.");
        return data.ToArray();
    }

    /// <summary>读取一个像素（alpha 未预乘）。坐标越界时抛出 <see cref="ArgumentOutOfRangeException"/>。</summary>
    public Color GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));

        using var pixel = new SKBitmap(new SKImageInfo(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        if (!_surface.ReadPixels(pixel.Info, pixel.GetPixels(), pixel.RowBytes, x, y))
            throw new InvalidOperationException("The surface could not be read back.");
        var c = pixel.GetPixel(0, 0);
        return new Color(c.Red, c.Green, c.Blue, c.Alpha);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // 先上下文与表面、后渲染目标：表面引用渲染目标，反序释放会访问已销毁的目标。
        _context.Dispose();
        _surface.Dispose();
        _target?.Dispose();
    }
}
