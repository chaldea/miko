using Miko.Common;
using SkiaSharp;

namespace Miko.Utils;

/// <summary>
/// 文本测量工具
/// </summary>
public static class TextMeasurer
{
    /// <summary>
    /// 测量文本的尺寸
    /// </summary>
    /// <param name="text">要测量的文本</param>
    /// <param name="fontFamily">字体家族</param>
    /// <param name="fontSize">字体大小（像素）</param>
    /// <param name="fontWeight">字体粗细</param>
    /// <returns>文本的宽度和高度</returns>
    public static (float Width, float Height) MeasureText(
        string? text,
        string fontFamily,
        float fontSize,
        FontWeight fontWeight)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (0, 0);
        }

        using var typeface = SKTypeface.FromFamilyName(
            fontFamily,
            (SKFontStyleWeight)(int)fontWeight,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright);

        using var font = new SKFont(typeface, fontSize);
        using var paint = new SKPaint();

        // 测量文本宽度
        float width = font.MeasureText(text, paint);

        // 获取字体度量以计算高度
        var metrics = font.Metrics;
        // 使用 ascent 的绝对值加上 descent 来计算行高
        float height = -metrics.Ascent + metrics.Descent;

        return (width, height);
    }

    /// <summary>
    /// 测量文本的宽度
    /// </summary>
    public static float MeasureTextWidth(
        string? text,
        string fontFamily,
        float fontSize,
        FontWeight fontWeight)
    {
        return MeasureText(text, fontFamily, fontSize, fontWeight).Width;
    }

    /// <summary>
    /// 测量文本的高度
    /// </summary>
    public static float MeasureTextHeight(
        string fontFamily,
        float fontSize,
        FontWeight fontWeight)
    {
        using var typeface = SKTypeface.FromFamilyName(
            fontFamily,
            (SKFontStyleWeight)(int)fontWeight,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright);

        using var font = new SKFont(typeface, fontSize);

        var metrics = font.Metrics;
        return -metrics.Ascent + metrics.Descent;
    }
}
