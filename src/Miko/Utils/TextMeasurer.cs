using Miko.Common;
using Miko.Fonts;
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

        var fontManager = FontManager.Instance;
        var fallbackResolver = new FontFallbackResolver(fontManager);
        var textRuns = fallbackResolver.ResolveTextRuns(text, fontFamily, fontWeight);

        if (textRuns.Count == 0)
        {
            return (0, 0);
        }

        float totalWidth = 0;
        float maxHeight = 0;

        using var paint = new SKPaint();

        foreach (var run in textRuns)
        {
            using var font = new SKFont(run.Typeface, fontSize);
            totalWidth += font.MeasureText(run.Text, paint);

            var metrics = font.Metrics;
            float height = -metrics.Ascent + metrics.Descent;
            maxHeight = Math.Max(maxHeight, height);
        }

        return (totalWidth, maxHeight);
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
        var fontManager = FontManager.Instance;
        var typeface = fontManager.GetTypeface(fontFamily, fontWeight);

        if (typeface == null)
        {
            typeface = SKTypeface.FromFamilyName(
                fontFamily,
                (SKFontStyleWeight)(int)fontWeight,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
        }

        using var font = new SKFont(typeface, fontSize);

        var metrics = font.Metrics;
        return -metrics.Ascent + metrics.Descent;
    }
}
