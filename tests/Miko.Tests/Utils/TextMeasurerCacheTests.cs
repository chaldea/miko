using Miko.Common;
using Miko.Fonts;
using Miko.Utils;
using Shouldly;

namespace Miko.Tests.Utils;

/// <summary>
/// Tests for TextMeasurer measurement caching (ISSUE-036 performance optimization).
/// </summary>
public class TextMeasurerCacheTests : IDisposable
{
    public TextMeasurerCacheTests()
    {
        FontManager.ResetInstance();
        TextMeasurer.ClearCache();
    }

    public void Dispose()
    {
        FontManager.ResetInstance();
        TextMeasurer.ClearCache();
    }

    [Fact]
    public void MeasureText_RepeatedCall_ReturnsSameResult()
    {
        var first = TextMeasurer.MeasureText("Hello World", "Arial", 14f, FontWeight.Normal);
        var second = TextMeasurer.MeasureText("Hello World", "Arial", 14f, FontWeight.Normal);

        second.Width.ShouldBe(first.Width);
        second.Height.ShouldBe(first.Height);
    }

    [Fact]
    public void MeasureText_CachedResult_MatchesFreshMeasurement()
    {
        // Prime the cache
        var cached = TextMeasurer.MeasureText("Cached text", "Arial", 16f, FontWeight.Normal);

        // Clear and measure fresh — must produce the same value
        TextMeasurer.ClearCache();
        var fresh = TextMeasurer.MeasureText("Cached text", "Arial", 16f, FontWeight.Normal);

        cached.Width.ShouldBe(fresh.Width);
        cached.Height.ShouldBe(fresh.Height);
    }

    [Fact]
    public void MeasureText_EmptyText_ReturnsZero()
    {
        var result = TextMeasurer.MeasureText("", "Arial", 14f, FontWeight.Normal);

        result.Width.ShouldBe(0);
        result.Height.ShouldBe(0);
    }

    [Fact]
    public void MeasureText_DifferentFontSize_ProducesDifferentWidth()
    {
        var small = TextMeasurer.MeasureText("Sample", "Arial", 10f, FontWeight.Normal);
        var large = TextMeasurer.MeasureText("Sample", "Arial", 30f, FontWeight.Normal);

        // Distinct cache keys must not collide
        large.Width.ShouldBeGreaterThan(small.Width);
    }

    [Fact]
    public void MeasureTextHeight_RepeatedCall_ReturnsSameResult()
    {
        var first = TextMeasurer.MeasureTextHeight("Arial", 14f, FontWeight.Normal);
        var second = TextMeasurer.MeasureTextHeight("Arial", 14f, FontWeight.Normal);

        second.ShouldBe(first);
    }
}
