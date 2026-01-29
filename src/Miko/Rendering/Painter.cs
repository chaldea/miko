using Miko.Common;
using SkiaSharp;

namespace Miko.Rendering;

/// <summary>
/// 绘制工具
/// </summary>
public class Painter
{
    private readonly SKCanvas _canvas;

    public Painter(SKCanvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    /// 绘制背景
    /// </summary>
    public void DrawBackground(RectF rect, Color color, float topLeftRadius = 0, float topRightRadius = 0, float bottomRightRadius = 0, float bottomLeftRadius = 0)
    {
        if (color.A == 0) return; // 透明背景不绘制

        using var paint = new SKPaint
        {
            Color = color.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        if (topLeftRadius > 0 || topRightRadius > 0 || bottomRightRadius > 0 || bottomLeftRadius > 0)
        {
            using var path = CreateRoundRectPath(rect.ToSKRect(), topLeftRadius, topRightRadius, bottomRightRadius, bottomLeftRadius);
            _canvas.DrawPath(path, paint);
        }
        else
        {
            _canvas.DrawRect(rect.ToSKRect(), paint);
        }
    }

    /// <summary>
    /// 绘制边框
    /// </summary>
    public void DrawBorder(RectF rect, float width, Color color, BorderStyle style, float topLeftRadius = 0, float topRightRadius = 0, float bottomRightRadius = 0, float bottomLeftRadius = 0)
    {
        if (width <= 0 || color.A == 0 || style == BorderStyle.None) return;

        using var paint = new SKPaint
        {
            Color = color.ToSKColor(),
            StrokeWidth = width,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        // 根据边框样式设置
        switch (style)
        {
            case BorderStyle.Dotted:
                paint.PathEffect = SKPathEffect.CreateDash(new[] { width, width }, 0);
                break;

            case BorderStyle.Dashed:
                paint.PathEffect = SKPathEffect.CreateDash(new[] { width * 3, width * 2 }, 0);
                break;

            case BorderStyle.Double:
                // TODO: 实现双线边框
                break;
        }

        if (topLeftRadius > 0 || topRightRadius > 0 || bottomRightRadius > 0 || bottomLeftRadius > 0)
        {
            using var path = CreateRoundRectPath(rect.ToSKRect(), topLeftRadius, topRightRadius, bottomRightRadius, bottomLeftRadius);
            _canvas.DrawPath(path, paint);
        }
        else
        {
            _canvas.DrawRect(rect.ToSKRect(), paint);
        }
    }

    /// <summary>
    /// 绘制文本
    /// </summary>
    public void DrawText(string text, RectF rect, Color color, string fontFamily, float fontSize, FontWeight fontWeight, TextAlign textAlign)
    {
        if (string.IsNullOrEmpty(text) || color.A == 0) return;

        using var typeface = SKTypeface.FromFamilyName(fontFamily, (SKFontStyleWeight)(int)fontWeight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        using var font = new SKFont(typeface, fontSize);
        using var paint = new SKPaint
        {
            Color = color.ToSKColor(),
            IsAntialias = true
        };

        // 计算文本位置
        float x = textAlign switch
        {
            TextAlign.Left => rect.Left,
            TextAlign.Right => rect.Right,
            TextAlign.Center => rect.Left + rect.Width / 2,
            _ => rect.Left
        };

        var skTextAlign = textAlign switch
        {
            TextAlign.Left => SKTextAlign.Left,
            TextAlign.Right => SKTextAlign.Right,
            TextAlign.Center => SKTextAlign.Center,
            _ => SKTextAlign.Left
        };

        // 垂直居中
        float y = rect.Top + (rect.Height + fontSize) / 2;

        _canvas.DrawText(text, x, y, skTextAlign, font, paint);
    }

    /// <summary>
    /// 绘制图片
    /// </summary>
    public void DrawImage(SKBitmap bitmap, RectF rect)
    {
        if (bitmap == null) return;

        _canvas.DrawBitmap(bitmap, rect.ToSKRect());
    }

    /// <summary>
    /// 保存画布状态
    /// </summary>
    public int Save() => _canvas.Save();

    /// <summary>
    /// 恢复画布状态
    /// </summary>
    public void Restore() => _canvas.Restore();

    /// <summary>
    /// 设置裁剪区域
    /// </summary>
    public void ClipRect(RectF rect)
    {
        _canvas.ClipRect(rect.ToSKRect());
    }

    /// <summary>
    /// 清空画布
    /// </summary>
    public void Clear(Color color)
    {
        _canvas.Clear(color.ToSKColor());
    }

    /// <summary>
    /// 创建圆角矩形路径
    /// </summary>
    private static SKPath CreateRoundRectPath(SKRect rect, float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        var radii = new SKPoint[]
        {
            new(topLeft, topLeft),
            new(topRight, topRight),
            new(bottomRight, bottomRight),
            new(bottomLeft, bottomLeft)
        };

        var roundRect = new SKRoundRect();
        roundRect.SetRectRadii(rect, radii);

        var path = new SKPath();
        path.AddRoundRect(roundRect);
        return path;
    }
}
