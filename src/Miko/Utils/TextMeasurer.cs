using System.Collections.Concurrent;
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
    /// 文本测量缓存键：测量结果是 (文本, 字体, 字号, 字重) 的纯函数。
    /// </summary>
    private readonly record struct MeasureKey(string Text, string FontFamily, float FontSize, FontWeight FontWeight);

    private readonly record struct HeightKey(string FontFamily, float FontSize, FontWeight FontWeight);

    // 测量结果缓存。字体注册变化时由 FontManager 调用 ClearCache 失效。
    private static readonly ConcurrentDictionary<MeasureKey, (float Width, float Height)> _measureCache = new();
    private static readonly ConcurrentDictionary<HeightKey, float> _heightCache = new();

    // 缓存容量上限，超限时整体清空，避免动态文本场景下无限增长。
    private const int MaxCacheEntries = 4096;

    // 共享 paint：SKFont.MeasureText 的 paint 参数不影响字形宽度，可全局复用避免每次分配。
    private static readonly SKPaint _sharedPaint = new();

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

        var key = new MeasureKey(text, fontFamily, fontSize, fontWeight);
        if (_measureCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = MeasureUncached(text, fontFamily, fontSize, fontWeight);

        if (_measureCache.Count >= MaxCacheEntries)
        {
            _measureCache.Clear();
        }
        _measureCache[key] = result;

        return result;
    }

    private static (float Width, float Height) MeasureUncached(
        string text,
        string fontFamily,
        float fontSize,
        FontWeight fontWeight)
    {
        var fontManager = FontManager.Instance;
        var fallbackResolver = new FontFallbackResolver(fontManager);
        var textRuns = fallbackResolver.ResolveTextRuns(text, fontFamily, fontWeight);

        if (textRuns.Count == 0)
        {
            return (0, 0);
        }

        float totalWidth = 0;
        float maxHeight = 0;

        foreach (var run in textRuns)
        {
            using var font = new SKFont(run.Typeface, fontSize);
            totalWidth += font.MeasureText(run.Text, _sharedPaint);

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
        var key = new HeightKey(fontFamily, fontSize, fontWeight);
        if (_heightCache.TryGetValue(key, out var cachedHeight))
        {
            return cachedHeight;
        }

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
        float height = -metrics.Ascent + metrics.Descent;

        if (_heightCache.Count >= MaxCacheEntries)
        {
            _heightCache.Clear();
        }
        _heightCache[key] = height;

        return height;
    }

    /// <summary>
    /// 清空文本测量缓存。当字体注册发生变化（注册/注销/清空字体缓存）时调用，
    /// 因为旧的测量结果可能已不再有效。
    /// </summary>
    public static void ClearCache()
    {
        _measureCache.Clear();
        _heightCache.Clear();
    }
}
