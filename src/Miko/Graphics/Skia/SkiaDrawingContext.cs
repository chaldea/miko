using System.Runtime.CompilerServices;
using Miko.Common;
using Miko.Rendering;
using SkiaSharp;

namespace Miko.Graphics.Skia;

/// <summary>
/// <see cref="IDrawingContext"/> 的 Skia 实现：包装一张 <see cref="SKCanvas"/>。
///
/// <para>画笔按需创建、随上下文复用，每次绘制只改颜色等属性：<see cref="SKPaint"/> 是原生对象，
/// 每次调用新建就是每帧的原生分配（见 ISSUE-144）。引擎的 <see cref="Rendering.Painter"/> 也随上下文
/// 复用，因此复用同一张表面的宿主不再每帧新建绘制器。</para>
/// </summary>
internal sealed class SkiaDrawingContext : IDrawingContext, IDisposable
{
    /// <summary>
    /// 包装外部画布的上下文，按画布实例缓存。测试与基准直接拿 <see cref="SKCanvas"/> 驱动引擎
    /// （见 <c>tests/Shared/SkiaCanvasExtensions.cs</c>），缓存让它们每帧不必新建上下文与绘制器。
    /// </summary>
    private static readonly ConditionalWeakTable<SKCanvas, SkiaDrawingContext> s_wrappers = new();

    private Painter? _painter;
    private SKPaint? _fill;
    private SKPaint? _stroke;
    private SKPaint? _tint;
    private Color _tintColor;

    public SkiaDrawingContext(SKCanvas canvas)
    {
        Canvas = canvas;
    }

    public SKCanvas Canvas { get; }

    /// <summary>引擎在本上下文上的绘制器（与上下文同生命周期）。</summary>
    public Painter Painter => _painter ??= new Painter(Canvas);

    public IGraphicsFactory Graphics => SkiaGraphicsFactory.Instance;

    public float DeviceScale => Painter.DeviceScaleOf(Canvas);

    /// <summary>外部画布的上下文（按画布缓存，不随调用新建）。</summary>
    public static SkiaDrawingContext For(SKCanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        return s_wrappers.GetValue(canvas, static c => new SkiaDrawingContext(c));
    }

    /// <summary>引擎只能画进本库创建的上下文：其它实现没有可供渲染的后端画布。</summary>
    public static SkiaDrawingContext Require(IDrawingContext context, string paramName)
    {
        ArgumentNullException.ThrowIfNull(context, paramName);
        return context as SkiaDrawingContext ?? throw new ArgumentException(
            "The engine can only render into a drawing context created by Miko " +
            "(RenderSurface.Context, a WindowRenderTarget or a GraphicsDevice surface).", paramName);
    }

    public int Save() => Canvas.Save();

    public void Restore() => Canvas.Restore();

    public void RestoreToCount(int count) => Canvas.RestoreToCount(count);

    public void Translate(float dx, float dy) => Canvas.Translate(dx, dy);

    public void Scale(float sx, float sy) => Canvas.Scale(sx, sy);

    public void Rotate(float degrees) => Canvas.RotateDegrees(degrees);

    public void ClipRect(RectF rect) => Canvas.ClipRect(rect.ToSKRect());

    public void ClipRoundRect(RectF rect, float radius)
    {
        using var clip = new SKRoundRect(rect.ToSKRect(), radius);
        Canvas.ClipRoundRect(clip, antialias: true);
    }

    public void Clear(Color color) => Canvas.Clear(color.ToSKColor());

    public void FillRect(RectF rect, Color color) => Canvas.DrawRect(rect.ToSKRect(), Fill(color));

    public void StrokeRect(RectF rect, Color color, float strokeWidth)
        => Canvas.DrawRect(rect.ToSKRect(), Stroke(color, strokeWidth));

    public void FillRoundRect(RectF rect, float radius, Color color)
        => Canvas.DrawRoundRect(rect.ToSKRect(), radius, radius, Fill(color));

    public void FillCircle(float centerX, float centerY, float radius, Color color)
        => Canvas.DrawCircle(centerX, centerY, radius, Fill(color));

    public void StrokeArc(RectF oval, float startAngle, float sweepAngle, Color color, float strokeWidth)
        => Canvas.DrawArc(oval.ToSKRect(), startAngle, sweepAngle, false, Stroke(color, strokeWidth));

    public void StrokeLine(float x0, float y0, float x1, float y1, Color color, float strokeWidth)
        => Canvas.DrawLine(x0, y0, x1, y1, Stroke(color, strokeWidth));

    public void DrawText(TextBlock text, float x, float baseline, Color color)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (color.A == 0) return;
        var blob = text.Shaped.GetBlob(Painter.CreateHighQualityFont);
        if (blob != null) Canvas.DrawText(blob, x, baseline, Fill(color));
    }

    public void StrokeText(TextBlock text, float x, float baseline, Color color, float strokeWidth)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (color.A == 0) return;
        var blob = text.Shaped.GetBlob(Painter.CreateHighQualityFont);
        if (blob != null) Canvas.DrawText(blob, x, baseline, Stroke(color, strokeWidth));
    }

    public void DrawImage(Image image, RectF destination, ImageSampling sampling = ImageSampling.Auto, Color? tint = null)
    {
        var source = SkiaImage.Require(image);
        if (destination.Width <= 0 || destination.Height <= 0) return;
        var paint = tint is { } color ? Tint(color) : null;

        if (source.Picture is { } picture)
        {
            var cull = picture.CullRect;
            int save = Canvas.Save();
            Canvas.Translate(destination.X, destination.Y);
            Canvas.Scale(destination.Width / cull.Width, destination.Height / cull.Height);
            if (cull.Left != 0 || cull.Top != 0) Canvas.Translate(-cull.Left, -cull.Top);
            Canvas.DrawPicture(picture, paint);
            Canvas.RestoreToCount(save);
            return;
        }

        var raster = source.ToSKImage();
        if (raster == null) return;
        Canvas.DrawImage(raster, destination.ToSKRect(), SamplingFor(sampling, raster, destination), paint);
    }

    private SKSamplingOptions SamplingFor(ImageSampling sampling, SKImage image, RectF destination) => sampling switch
    {
        ImageSampling.Nearest => new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None),
        ImageSampling.Linear => new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None),
        ImageSampling.Cubic => new SKSamplingOptions(SKCubicResampler.Mitchell),
        _ => Painter.SamplingFor(
            destination.Width * DeviceScale / image.Width,
            destination.Height * DeviceScale / image.Height),
    };

    private SKPaint Fill(Color color)
    {
        var paint = _fill ??= new SKPaint { IsAntialias = true };
        paint.Color = color.ToSKColor();
        return paint;
    }

    private SKPaint Stroke(Color color, float width)
    {
        var paint = _stroke ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke };
        paint.Color = color.ToSKColor();
        paint.StrokeWidth = width;
        return paint;
    }

    /// <summary>以图像 alpha 为遮罩、用 <paramref name="color"/> 填充（SrcIn），颜色不变时复用滤镜。</summary>
    private SKPaint Tint(Color color)
    {
        if (_tint != null && _tintColor.Equals(color)) return _tint;
        _tint ??= new SKPaint();
        var previous = _tint.ColorFilter;
        _tint.ColorFilter = SKColorFilter.CreateBlendMode(color.ToSKColor(), SKBlendMode.SrcIn);
        previous?.Dispose();
        _tintColor = color;
        return _tint;
    }

    public void Dispose()
    {
        _fill?.Dispose();
        _stroke?.Dispose();
        _tint?.ColorFilter?.Dispose();
        _tint?.Dispose();
        _fill = _stroke = _tint = null;
    }
}
