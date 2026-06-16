using Miko.Common;
using Miko.Fonts;
using Miko.Styling;
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
    /// 绘制盒阴影（支持多层阴影）
    /// </summary>
    public void DrawBoxShadow(List<BoxShadow> shadows, RectF rect, float topLeftRadius = 0, float topRightRadius = 0, float bottomRightRadius = 0, float bottomLeftRadius = 0)
    {
        if (shadows == null || shadows.Count == 0) return;

        // 阴影按顺序绘制（第一个在最上层）
        foreach (var shadow in shadows)
        {
            if (shadow.Color.A == 0) continue;

            // 计算阴影矩形（应用 offset 和 spread）
            var shadowRect = new RectF(
                rect.X + shadow.OffsetX - shadow.SpreadRadius,
                rect.Y + shadow.OffsetY - shadow.SpreadRadius,
                rect.Width + shadow.SpreadRadius * 2,
                rect.Height + shadow.SpreadRadius * 2
            );

            // 阴影圆角也需要应用 spread
            float shadowTopLeftRadius = topLeftRadius + shadow.SpreadRadius;
            float shadowTopRightRadius = topRightRadius + shadow.SpreadRadius;
            float shadowBottomRightRadius = bottomRightRadius + shadow.SpreadRadius;
            float shadowBottomLeftRadius = bottomLeftRadius + shadow.SpreadRadius;

            using var paint = new SKPaint
            {
                Color = shadow.Color.ToSKColor(),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            // 应用模糊
            if (shadow.BlurRadius > 0)
            {
                paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, shadow.BlurRadius / 2f);
            }

            if (shadow.Inset)
            {
                // Inset shadow: 绘制在元素内部（需要裁剪）
                // 暂时跳过 inset 实现（Ionic 的 header shadow 不使用 inset）
                // TODO: 完整实现 inset shadow
            }
            else
            {
                // 外阴影: 在背景之下绘制
                if (shadowTopLeftRadius > 0 || shadowTopRightRadius > 0 ||
                    shadowBottomRightRadius > 0 || shadowBottomLeftRadius > 0)
                {
                    using var path = CreateRoundRectPath(shadowRect.ToSKRect(),
                        shadowTopLeftRadius, shadowTopRightRadius,
                        shadowBottomRightRadius, shadowBottomLeftRadius);
                    _canvas.DrawPath(path, paint);
                }
                else
                {
                    _canvas.DrawRect(shadowRect.ToSKRect(), paint);
                }
            }
        }
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
    /// 绘制边框（统一样式）
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

        // 将绘制矩形向内收缩半个边框宽度，使描边完全在 BorderBox 内部
        float halfWidth = width / 2;
        var insetRect = new SKRect(
            rect.Left + halfWidth,
            rect.Top + halfWidth,
            rect.Right - halfWidth,
            rect.Bottom - halfWidth
        );

        if (topLeftRadius > 0 || topRightRadius > 0 || bottomRightRadius > 0 || bottomLeftRadius > 0)
        {
            using var path = CreateRoundRectPath(insetRect, topLeftRadius, topRightRadius, bottomRightRadius, bottomLeftRadius);
            _canvas.DrawPath(path, paint);
        }
        else
        {
            _canvas.DrawRect(insetRect, paint);
        }
    }

    /// <summary>
    /// 绘制边框（每边可以有不同的样式）
    /// </summary>
    public void DrawBorderSides(
        RectF rect,
        BorderSide top, BorderSide right, BorderSide bottom, BorderSide left,
        float topLeftRadius = 0, float topRightRadius = 0,
        float bottomRightRadius = 0, float bottomLeftRadius = 0)
    {
        // 快速路径：如果所有边都相同，使用优化的单边框绘制
        if (AreBordersUniform(top, right, bottom, left))
        {
            DrawBorder(rect, top.Width.Value, top.Color, top.Style,
                topLeftRadius, topRightRadius, bottomRightRadius, bottomLeftRadius);
            return;
        }

        // 分别绘制每条边（含圆角弧线）
        DrawTopBorder(rect, top, left, right, topLeftRadius, topRightRadius);
        DrawRightBorder(rect, right, top, bottom, topRightRadius, bottomRightRadius);
        DrawBottomBorder(rect, bottom, right, left, bottomRightRadius, bottomLeftRadius);
        DrawLeftBorder(rect, left, bottom, top, bottomLeftRadius, topLeftRadius);
    }

    /// <summary>
    /// 检查所有边框是否统一
    /// </summary>
    private static bool AreBordersUniform(BorderSide top, BorderSide right, BorderSide bottom, BorderSide left)
    {
        return Math.Abs(top.Width.Value - right.Width.Value) < 0.01f &&
               Math.Abs(right.Width.Value - bottom.Width.Value) < 0.01f &&
               Math.Abs(bottom.Width.Value - left.Width.Value) < 0.01f &&
               top.Color.Equals(right.Color) &&
               right.Color.Equals(bottom.Color) &&
               bottom.Color.Equals(left.Color) &&
               top.Style == right.Style &&
               right.Style == bottom.Style &&
               bottom.Style == left.Style;
    }

    /// <summary>
    /// 绘制上边框
    /// </summary>
    private void DrawTopBorder(RectF rect, BorderSide border, BorderSide leftBorder, BorderSide rightBorder, float leftRadius, float rightRadius)
    {
        if (!border.IsVisible) return;

        using var paint = CreateBorderPaint(border);
        float halfWidth = border.Width.Value / 2;

        float insetTop = rect.Top + halfWidth;
        float insetLeft = rect.Left + halfWidth;
        float insetRight = rect.Right - halfWidth;

        using var path = new SKPath();

        if (leftRadius > 0)
        {
            float arcRadius = Math.Max(0, leftRadius - halfWidth);
            path.MoveTo(insetLeft + arcRadius, insetTop);
        }
        else
        {
            path.MoveTo(insetLeft, insetTop);
        }

        if (rightRadius > 0)
        {
            float arcRadius = Math.Max(0, rightRadius - halfWidth);
            path.LineTo(insetRight - arcRadius, insetTop);
            path.ArcTo(
                new SKRect(insetRight - arcRadius * 2, insetTop, insetRight, insetTop + arcRadius * 2),
                270, 90, false);
        }
        else
        {
            path.LineTo(insetRight, insetTop);
        }

        _canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// 绘制右边框
    /// </summary>
    private void DrawRightBorder(RectF rect, BorderSide border, BorderSide topBorder, BorderSide bottomBorder, float topRadius, float bottomRadius)
    {
        if (!border.IsVisible) return;

        using var paint = CreateBorderPaint(border);
        float halfWidth = border.Width.Value / 2;

        float insetRight = rect.Right - halfWidth;
        float insetTop = rect.Top + halfWidth;
        float insetBottom = rect.Bottom - halfWidth;

        using var path = new SKPath();

        if (topRadius > 0)
        {
            float arcRadius = Math.Max(0, topRadius - halfWidth);
            path.MoveTo(insetRight, insetTop + arcRadius);
        }
        else
        {
            path.MoveTo(insetRight, insetTop);
        }

        if (bottomRadius > 0)
        {
            float arcRadius = Math.Max(0, bottomRadius - halfWidth);
            path.LineTo(insetRight, insetBottom - arcRadius);
            path.ArcTo(
                new SKRect(insetRight - arcRadius * 2, insetBottom - arcRadius * 2, insetRight, insetBottom),
                0, 90, false);
        }
        else
        {
            path.LineTo(insetRight, insetBottom);
        }

        _canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// 绘制下边框
    /// </summary>
    private void DrawBottomBorder(RectF rect, BorderSide border, BorderSide rightBorder, BorderSide leftBorder, float rightRadius, float leftRadius)
    {
        if (!border.IsVisible) return;

        using var paint = CreateBorderPaint(border);
        float halfWidth = border.Width.Value / 2;

        float insetBottom = rect.Bottom - halfWidth;
        float insetLeft = rect.Left + halfWidth;
        float insetRight = rect.Right - halfWidth;

        using var path = new SKPath();

        if (rightRadius > 0)
        {
            float arcRadius = Math.Max(0, rightRadius - halfWidth);
            path.MoveTo(insetRight - arcRadius, insetBottom);
        }
        else
        {
            path.MoveTo(insetRight, insetBottom);
        }

        if (leftRadius > 0)
        {
            float arcRadius = Math.Max(0, leftRadius - halfWidth);
            path.LineTo(insetLeft + arcRadius, insetBottom);
            path.ArcTo(
                new SKRect(insetLeft, insetBottom - arcRadius * 2, insetLeft + arcRadius * 2, insetBottom),
                90, 90, false);
        }
        else
        {
            path.LineTo(insetLeft, insetBottom);
        }

        _canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// 绘制左边框
    /// </summary>
    private void DrawLeftBorder(RectF rect, BorderSide border, BorderSide bottomBorder, BorderSide topBorder, float bottomRadius, float topRadius)
    {
        if (!border.IsVisible) return;

        using var paint = CreateBorderPaint(border);
        float halfWidth = border.Width.Value / 2;

        float insetLeft = rect.Left + halfWidth;
        float insetTop = rect.Top + halfWidth;
        float insetBottom = rect.Bottom - halfWidth;

        using var path = new SKPath();

        if (bottomRadius > 0)
        {
            float arcRadius = Math.Max(0, bottomRadius - halfWidth);
            path.MoveTo(insetLeft, insetBottom - arcRadius);
        }
        else
        {
            path.MoveTo(insetLeft, insetBottom);
        }

        if (topRadius > 0)
        {
            float arcRadius = Math.Max(0, topRadius - halfWidth);
            path.LineTo(insetLeft, insetTop + arcRadius);
            path.ArcTo(
                new SKRect(insetLeft, insetTop, insetLeft + arcRadius * 2, insetTop + arcRadius * 2),
                180, 90, false);
        }
        else
        {
            path.LineTo(insetLeft, insetTop);
        }

        _canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// 创建边框画笔
    /// </summary>
    private static SKPaint CreateBorderPaint(BorderSide border)
    {
        var paint = new SKPaint
        {
            Color = border.Color.ToSKColor(),
            StrokeWidth = border.Width.Value,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Butt
        };

        switch (border.Style)
        {
            case BorderStyle.Dotted:
                paint.PathEffect = SKPathEffect.CreateDash(
                    new[] { border.Width.Value, border.Width.Value }, 0);
                break;
            case BorderStyle.Dashed:
                paint.PathEffect = SKPathEffect.CreateDash(
                    new[] { border.Width.Value * 3, border.Width.Value * 2 }, 0);
                break;
        }

        return paint;
    }

    /// <summary>
    /// 绘制文本
    /// </summary>
    public void DrawText(string text, RectF rect, Color color, string fontFamily, float fontSize, FontWeight fontWeight, TextAlign textAlign, VerticalAlign verticalAlign = VerticalAlign.Top)
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

        // 计算文本基线Y位置
        using var baselineFont = new SKFont(textRuns[0].Typeface, fontSize);
        float y;
        if (verticalAlign == VerticalAlign.Middle)
        {
            // 垂直居中：将文本行居中于 rect 高度内
            float textHeight = baselineFont.Metrics.Descent - baselineFont.Metrics.Ascent;
            float centeredTop = rect.Top + (rect.Height - textHeight) / 2;
            y = centeredTop - baselineFont.Metrics.Ascent;
        }
        else
        {
            // 默认顶部对齐（baseline ≈ top + ascent）
            y = rect.Top - baselineFont.Metrics.Ascent;
        }

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
    /// 绘制一张 <see cref="SKImage"/>（可能由 GPU 纹理零拷贝包装而来，如视频帧）。
    /// <paramref name="srcRect"/> 为源采样矩形（用于 object-fit 裁剪），null 表示整图。
    /// </summary>
    public void DrawImage(SKImage image, RectF dstRect, RectF? srcRect = null)
    {
        if (image == null) return;

        using var paint = new SKPaint { IsAntialias = true };
        if (srcRect is { } src)
            _canvas.DrawImage(image, src.ToSKRect(), dstRect.ToSKRect(), paint);
        else
            _canvas.DrawImage(image, dstRect.ToSKRect(), paint);
    }

    /// <summary>
    /// 保存画布状态
    /// </summary>
    public int Save() => _canvas.Save();

    public int SaveLayerAlpha(byte alpha) => _canvas.SaveLayer(new SKPaint { Color = new SKColor(255, 255, 255, alpha) });

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

    public void SaveClip(RectF rect)
    {
        _canvas.Save();
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
    /// 绘制滑块（Range），支持伪元素样式
    /// </summary>
    public void DrawRange(RectF rect, float value, float min, float max,
        Style? trackStyle, Style? progressStyle, Style? thumbStyle, float fontSize)
    {
        // 默认值（当没有伪元素样式时使用）
        // 使用 ToPixels() 正确解析 rem/em/percent 等单位
        float trackHeight = trackStyle?.Height?.ToPixels(rect.Height, fontSize) ?? 4;
        Color trackColor = trackStyle?.BackgroundColor ?? Color.LightGray;
        float trackBorderRadius = trackStyle?.BorderTopLeftRadius?.ToPixels(trackHeight, fontSize) ?? 2;

        Color progressColor = progressStyle?.BackgroundColor ?? Color.Gray;

        // thumb 尺寸 - 使用 rect.Width 作为容器尺寸（用于百分比）
        float thumbWidth = thumbStyle?.Width?.ToPixels(rect.Width, fontSize) ?? 16;
        float thumbHeight = thumbStyle?.Height?.ToPixels(rect.Height, fontSize) ?? 16;
        float thumbSize = Math.Max(thumbWidth, thumbHeight); // 使用较大值作为尺寸
        Color thumbColor = thumbStyle?.BackgroundColor ?? Color.Gray;
        float thumbBorderRadius = thumbStyle?.BorderTopLeftRadius?.ToPixels(thumbSize, fontSize) ?? thumbSize / 2;
        float thumbBorderWidth = thumbStyle?.BorderWidth?.ToPixels(thumbSize, fontSize) ?? 0;
        Color? thumbBorderColor = thumbStyle?.BorderTopColor;

        // 计算滑块轨道的位置
        float trackY = rect.Top + (rect.Height - trackHeight) / 2;
        float thumbRadius = thumbSize / 2;

        // 计算当前值的位置
        float percentage = max > min ? (value - min) / (max - min) : 0;
        percentage = Math.Clamp(percentage, 0, 1);
        float thumbX = rect.Left + thumbRadius + (rect.Width - thumbRadius * 2) * percentage;

        // 绘制轨道背景（::range-track）
        var trackRect = new RectF(rect.Left, trackY, rect.Width, trackHeight);
        DrawBackground(trackRect, trackColor, trackBorderRadius, trackBorderRadius, trackBorderRadius, trackBorderRadius);

        // 绘制已填充部分（::range-progress）
        if (percentage > 0)
        {
            var fillRect = new RectF(rect.Left, trackY, thumbX - rect.Left, trackHeight);
            DrawBackground(fillRect, progressColor, trackBorderRadius, 0, 0, trackBorderRadius);
        }

        // 绘制滑块（::range-thumb）
        using var thumbPaint = new SKPaint
        {
            Color = thumbColor.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        float thumbCenterY = rect.Top + rect.Height / 2;

        if (thumbBorderRadius >= thumbRadius - 0.5f)
        {
            // 圆形滑块
            _canvas.DrawCircle(thumbX, thumbCenterY, thumbRadius, thumbPaint);
        }
        else
        {
            // 圆角矩形滑块
            var thumbRect = new SKRect(
                thumbX - thumbRadius,
                thumbCenterY - thumbRadius,
                thumbX + thumbRadius,
                thumbCenterY + thumbRadius);
            _canvas.DrawRoundRect(thumbRect, thumbBorderRadius, thumbBorderRadius, thumbPaint);
        }

        // 绘制滑块边框
        if (thumbBorderWidth > 0 && thumbBorderColor.HasValue)
        {
            using var thumbBorderPaint = new SKPaint
            {
                Color = thumbBorderColor.Value.ToSKColor(),
                StrokeWidth = thumbBorderWidth,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            if (thumbBorderRadius >= thumbRadius - 0.5f)
            {
                _canvas.DrawCircle(thumbX, thumbCenterY, thumbRadius - thumbBorderWidth / 2, thumbBorderPaint);
            }
            else
            {
                var thumbRect = new SKRect(
                    thumbX - thumbRadius + thumbBorderWidth / 2,
                    thumbCenterY - thumbRadius + thumbBorderWidth / 2,
                    thumbX + thumbRadius - thumbBorderWidth / 2,
                    thumbCenterY + thumbRadius - thumbBorderWidth / 2);
                _canvas.DrawRoundRect(thumbRect, thumbBorderRadius, thumbBorderRadius, thumbBorderPaint);
            }
        }
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

    /// <summary>
    /// 绘制垂直滚动条
    /// </summary>
    public void DrawVerticalScrollbar(RectF trackRect, float scrollTop, float contentHeight, float viewportHeight)
    {
        if (viewportHeight <= 0 || contentHeight <= viewportHeight) return;

        // 绘制轨道
        using var trackPaint = new SKPaint
        {
            Color = new SKColor(0xF1, 0xF1, 0xF1),
            Style = SKPaintStyle.Fill
        };
        _canvas.DrawRect(trackRect.ToSKRect(), trackPaint);

        // 计算滑块尺寸和位置
        float thumbRatio = viewportHeight / contentHeight;
        float thumbHeight = Math.Max(trackRect.Height * thumbRatio, 20f);
        float scrollableTrack = trackRect.Height - thumbHeight;
        float maxScroll = contentHeight - viewportHeight;
        float thumbY = trackRect.Top + (maxScroll > 0 ? (scrollTop / maxScroll) * scrollableTrack : 0);

        // 绘制滑块
        var thumbRect = new SKRect(
            trackRect.Left + 2,
            thumbY + 2,
            trackRect.Right - 2,
            thumbY + thumbHeight - 2);

        using var thumbPaint = new SKPaint
        {
            Color = new SKColor(0xC1, 0xC1, 0xC1),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        float radius = (trackRect.Width - 4) / 2;
        _canvas.DrawRoundRect(thumbRect, radius, radius, thumbPaint);
    }

    /// <summary>
    /// 绘制水平滚动条
    /// </summary>
    public void DrawHorizontalScrollbar(RectF trackRect, float scrollLeft, float contentWidth, float viewportWidth)
    {
        if (viewportWidth <= 0 || contentWidth <= viewportWidth) return;

        // 绘制轨道
        using var trackPaint = new SKPaint
        {
            Color = new SKColor(0xF1, 0xF1, 0xF1),
            Style = SKPaintStyle.Fill
        };
        _canvas.DrawRect(trackRect.ToSKRect(), trackPaint);

        // 计算滑块尺寸和位置
        float thumbRatio = viewportWidth / contentWidth;
        float thumbWidth = Math.Max(trackRect.Width * thumbRatio, 20f);
        float scrollableTrack = trackRect.Width - thumbWidth;
        float maxScroll = contentWidth - viewportWidth;
        float thumbX = trackRect.Left + (maxScroll > 0 ? (scrollLeft / maxScroll) * scrollableTrack : 0);

        // 绘制滑块
        var thumbRect = new SKRect(
            thumbX + 2,
            trackRect.Top + 2,
            thumbX + thumbWidth - 2,
            trackRect.Bottom - 2);

        using var thumbPaint = new SKPaint
        {
            Color = new SKColor(0xC1, 0xC1, 0xC1),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        float radius = (trackRect.Height - 4) / 2;
        _canvas.DrawRoundRect(thumbRect, radius, radius, thumbPaint);
    }

    /// <summary>
    /// 绘制文本光标
    /// </summary>
    public void DrawTextCursor(RectF contentRect, string text, int cursorPosition, string fontFamily, float fontSize, FontWeight fontWeight)
    {
        var fontManager = FontManager.Instance;
        var textBeforeCursor = text.Substring(0, Math.Min(cursorPosition, text.Length));

        float cursorX = contentRect.Left;
        if (textBeforeCursor.Length > 0)
        {
            var fallbackResolver = new FontFallbackResolver(fontManager);
            var runs = fallbackResolver.ResolveTextRuns(textBeforeCursor, fontFamily, fontWeight);
            using var measurePaint = new SKPaint { IsAntialias = true };
            foreach (var run in runs)
            {
                using var font = new SKFont(run.Typeface, fontSize);
                cursorX += font.MeasureText(run.Text, measurePaint);
            }
        }

        float cursorY = contentRect.Top + (contentRect.Height - fontSize) / 2;
        float cursorHeight = fontSize;

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        _canvas.DrawLine(cursorX, cursorY, cursorX, cursorY + cursorHeight, paint);
    }

    /// <summary>
    /// 绘制文本装饰（下划线、上划线、删除线）
    /// </summary>
    public void DrawTextDecoration(string text, RectF rect, Color color, string fontFamily, float fontSize, FontWeight fontWeight, TextAlign textAlign, TextDecoration decoration)
    {
        if (decoration == TextDecoration.None || string.IsNullOrEmpty(text) || color.A == 0) return;

        var fontManager = FontManager.Instance;
        var fallbackResolver = new FontFallbackResolver(fontManager);
        var textRuns = fallbackResolver.ResolveTextRuns(text, fontFamily, fontWeight);

        if (textRuns.Count == 0) return;

        using var measurePaint = new SKPaint { IsAntialias = true };

        float totalWidth = 0;
        foreach (var run in textRuns)
        {
            using var font = new SKFont(run.Typeface, fontSize);
            totalWidth += font.MeasureText(run.Text, measurePaint);
        }

        float x = textAlign switch
        {
            TextAlign.Left => rect.Left,
            TextAlign.Right => rect.Right - totalWidth,
            TextAlign.Center => rect.Left + (rect.Width - totalWidth) / 2,
            _ => rect.Left
        };

        using var baselineFont = new SKFont(textRuns[0].Typeface, fontSize);
        float baselineY = rect.Top - baselineFont.Metrics.Ascent;
        float lineY = decoration switch
        {
            TextDecoration.Underline => baselineY + fontSize * 0.15f,
            TextDecoration.Overline => baselineY - fontSize,
            TextDecoration.LineThrough => baselineY - fontSize * 0.35f,
            _ => baselineY
        };

        using var paint = new SKPaint
        {
            Color = color.ToSKColor(),
            StrokeWidth = Math.Max(1f, fontSize / 14f),
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        _canvas.DrawLine(x, lineY, x + totalWidth, lineY, paint);
    }

    /// <summary>
    /// 平移画布
    /// </summary>
    public void Translate(float dx, float dy)
    {
        _canvas.Translate(dx, dy);
    }

    public void Rotate(float degrees)
    {
        _canvas.RotateDegrees(degrees);
    }

    public void Scale(float sx, float sy)
    {
        _canvas.Scale(sx, sy);
    }

    public void Skew(float degreesX, float degreesY)
    {
        float tanX = MathF.Tan(degreesX * MathF.PI / 180f);
        float tanY = MathF.Tan(degreesY * MathF.PI / 180f);
        _canvas.Skew(tanX, tanY);
    }

    public void Concat(SKMatrix matrix)
    {
        _canvas.Concat(ref matrix);
    }
}
