using Miko.Common;
using Miko.Fonts;
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

        var fontManager = FontManager.Instance;
        var fallbackResolver = new FontFallbackResolver(fontManager);
        var textRuns = fallbackResolver.ResolveTextRuns(text, fontFamily, fontWeight);

        if (textRuns.Count == 0) return;

        using var paint = new SKPaint
        {
            Color = color.ToSKColor(),
            IsAntialias = true
        };

        // 计算总宽度用于对齐
        float totalWidth = 0;
        foreach (var run in textRuns)
        {
            using var font = new SKFont(run.Typeface, fontSize);
            totalWidth += font.MeasureText(run.Text, paint);
        }

        // 计算起始X位置
        float x = textAlign switch
        {
            TextAlign.Left => rect.Left,
            TextAlign.Right => rect.Right - totalWidth,
            TextAlign.Center => rect.Left + (rect.Width - totalWidth) / 2,
            _ => rect.Left
        };

        // 垂直居中
        float y = rect.Top + (rect.Height + fontSize) / 2;

        // 绘制每个文本段
        foreach (var run in textRuns)
        {
            using var font = new SKFont(run.Typeface, fontSize);
            _canvas.DrawText(run.Text, x, y, SKTextAlign.Left, font, paint);
            x += font.MeasureText(run.Text, paint);
        }
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
    /// 绘制复选框
    /// </summary>
    public void DrawCheckbox(RectF rect, bool isChecked, Color borderColor, Color checkColor, Color backgroundColor)
    {
        // 复选框使用整个内容区域（通常是 13x13px）
        float size = Math.Min(rect.Width, rect.Height);
        float x = rect.Left + (rect.Width - size) / 2;
        float y = rect.Top + (rect.Height - size) / 2;
        var boxRect = new RectF(x, y, size, size);

        // 绘制背景
        DrawBackground(boxRect, backgroundColor, 2, 2, 2, 2);

        // 绘制边框
        DrawBorder(boxRect, 2, borderColor, BorderStyle.Solid, 2, 2, 2, 2);

        // 如果选中，绘制勾选标记
        if (isChecked)
        {
            using var paint = new SKPaint
            {
                Color = checkColor.ToSKColor(),
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            // 绘制勾选标记
            var path = new SKPath();
            path.MoveTo(x + size * 0.2f, y + size * 0.5f);
            path.LineTo(x + size * 0.4f, y + size * 0.7f);
            path.LineTo(x + size * 0.8f, y + size * 0.3f);
            _canvas.DrawPath(path, paint);
        }
    }

    /// <summary>
    /// 绘制单选按钮
    /// </summary>
    public void DrawRadio(RectF rect, bool isChecked, Color borderColor, Color dotColor, Color backgroundColor)
    {
        // 单选按钮使用整个内容区域（通常是 13x13px）
        float size = Math.Min(rect.Width, rect.Height);
        float centerX = rect.Left + rect.Width / 2;
        float centerY = rect.Top + rect.Height / 2;
        float radius = size / 2;

        // 绘制背景圆
        using var bgPaint = new SKPaint
        {
            Color = backgroundColor.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        _canvas.DrawCircle(centerX, centerY, radius, bgPaint);

        // 绘制边框圆
        using var borderPaint = new SKPaint
        {
            Color = borderColor.ToSKColor(),
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        _canvas.DrawCircle(centerX, centerY, radius, borderPaint);

        // 如果选中，绘制内部圆点
        if (isChecked)
        {
            using var dotPaint = new SKPaint
            {
                Color = dotColor.ToSKColor(),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            _canvas.DrawCircle(centerX, centerY, radius * 0.5f, dotPaint);
        }
    }

    /// <summary>
    /// 绘制滑块（Range）
    /// </summary>
    public void DrawRange(RectF rect, float value, float min, float max, Color trackColor, Color fillColor, Color thumbColor)
    {
        // 计算滑块轨道的位置
        float trackHeight = 4;
        float trackY = rect.Top + (rect.Height - trackHeight) / 2;
        float thumbRadius = Math.Min(rect.Height / 2 - 2, 8);

        // 计算当前值的位置
        float percentage = (value - min) / (max - min);
        float thumbX = rect.Left + thumbRadius + (rect.Width - thumbRadius * 2) * percentage;

        // 绘制轨道背景
        var trackRect = new RectF(rect.Left, trackY, rect.Width, trackHeight);
        DrawBackground(trackRect, trackColor, 2, 2, 2, 2);

        // 绘制已填充部分
        var fillRect = new RectF(rect.Left, trackY, thumbX - rect.Left, trackHeight);
        DrawBackground(fillRect, fillColor, 2, 0, 0, 2);

        // 绘制滑块
        using var thumbPaint = new SKPaint
        {
            Color = thumbColor.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        _canvas.DrawCircle(thumbX, rect.Top + rect.Height / 2, thumbRadius, thumbPaint);

        // 绘制滑块边框
        using var thumbBorderPaint = new SKPaint
        {
            Color = new Color(0, 0, 0, 50).ToSKColor(),
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        _canvas.DrawCircle(thumbX, rect.Top + rect.Height / 2, thumbRadius, thumbBorderPaint);
    }

    /// <summary>
    /// 绘制密码文本（显示为圆点）
    /// </summary>
    public void DrawPasswordText(int length, RectF rect, Color color, float fontSize)
    {
        if (length <= 0) return;

        float dotRadius = fontSize * 0.2f;
        float spacing = fontSize * 0.6f;
        float totalWidth = length * spacing;
        float startX = rect.Left + 4; // 左边距
        float centerY = rect.Top + rect.Height / 2;

        using var paint = new SKPaint
        {
            Color = color.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        for (int i = 0; i < length; i++)
        {
            float x = startX + i * spacing + dotRadius;
            if (x + dotRadius > rect.Right) break; // 超出边界则停止
            _canvas.DrawCircle(x, centerY, dotRadius, paint);
        }
    }

    /// <summary>
    /// 绘制下拉选择框
    /// </summary>
    public void DrawSelect(RectF rect, string displayText, bool isOpen, Color borderColor, Color backgroundColor, Color textColor, Color arrowColor, float fontSize)
    {
        // 绘制背景
        DrawBackground(rect, backgroundColor, 2, 2, 2, 2);

        // 绘制边框
        DrawBorder(rect, 1, borderColor, BorderStyle.Solid, 2, 2, 2, 2);

        // 绘制文本区域（留出右侧箭头空间）
        float arrowWidth = 16;
        var textRect = new RectF(rect.Left + 4, rect.Top, rect.Width - arrowWidth - 8, rect.Height);
        if (!string.IsNullOrEmpty(displayText))
        {
            DrawText(displayText, textRect, textColor, "Arial", fontSize, FontWeight.Normal, TextAlign.Left);
        }

        // 绘制下拉箭头
        DrawDropdownArrow(rect, arrowColor, isOpen);
    }

    /// <summary>
    /// 绘制下拉箭头
    /// </summary>
    public void DrawDropdownArrow(RectF rect, Color color, bool isOpen)
    {
        float arrowWidth = 8;
        float arrowHeight = 4;
        float arrowX = rect.Right - 12;
        float arrowY = rect.Top + (rect.Height - arrowHeight) / 2;

        using var paint = new SKPaint
        {
            Color = color.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var path = new SKPath();
        if (isOpen)
        {
            // 向上箭头
            path.MoveTo(arrowX, arrowY + arrowHeight);
            path.LineTo(arrowX + arrowWidth / 2, arrowY);
            path.LineTo(arrowX + arrowWidth, arrowY + arrowHeight);
        }
        else
        {
            // 向下箭头
            path.MoveTo(arrowX, arrowY);
            path.LineTo(arrowX + arrowWidth / 2, arrowY + arrowHeight);
            path.LineTo(arrowX + arrowWidth, arrowY);
        }
        path.Close();
        _canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// 绘制下拉选项列表
    /// </summary>
    public void DrawSelectDropdown(RectF rect, List<(string text, bool isSelected, bool isDisabled, bool isGroupLabel)> options, Color backgroundColor, Color borderColor, Color textColor, Color selectedBackgroundColor, Color selectedTextColor, Color disabledTextColor, Color groupLabelColor, float fontSize)
    {
        // 绘制下拉列表背景
        DrawBackground(rect, backgroundColor);

        // 绘制边框
        DrawBorder(rect, 1, borderColor, BorderStyle.Solid);

        // 绘制选项
        float optionHeight = fontSize + 8;
        float y = rect.Top;

        foreach (var (text, isSelected, isDisabled, isGroupLabel) in options)
        {
            var optionRect = new RectF(rect.Left, y, rect.Width, optionHeight);

            if (isGroupLabel)
            {
                // 绘制分组标签
                var labelRect = new RectF(rect.Left + 4, y, rect.Width - 8, optionHeight);
                DrawText(text, labelRect, groupLabelColor, "Arial", fontSize, FontWeight.Bold, TextAlign.Left);
            }
            else
            {
                // 绘制选项背景
                if (isSelected)
                {
                    DrawBackground(optionRect, selectedBackgroundColor);
                }

                // 绘制选项文本
                var textRect = new RectF(rect.Left + 8, y, rect.Width - 16, optionHeight);
                var color = isDisabled ? disabledTextColor : (isSelected ? selectedTextColor : textColor);
                DrawText(text, textRect, color, "Arial", fontSize, FontWeight.Normal, TextAlign.Left);
            }

            y += optionHeight;
        }
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
