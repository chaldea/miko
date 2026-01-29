using Miko.Common;
using Miko.Utils;
using Shouldly;

namespace Miko.Tests.Utils;

public class GeometryUtilsTests
{
    [Fact]
    public void PointInRect_WithPointInside_ShouldReturnTrue()
    {
        var rect = new RectF(10, 10, 100, 100);

        GeometryUtils.PointInRect(50, 50, rect).ShouldBeTrue();
    }

    [Fact]
    public void PointInRect_WithPointOutside_ShouldReturnFalse()
    {
        var rect = new RectF(10, 10, 100, 100);

        GeometryUtils.PointInRect(150, 150, rect).ShouldBeFalse();
    }

    [Fact]
    public void PointInRect_WithPointOnEdge_ShouldReturnTrue()
    {
        var rect = new RectF(10, 10, 100, 100);

        GeometryUtils.PointInRect(10, 50, rect).ShouldBeTrue();
        GeometryUtils.PointInRect(110, 50, rect).ShouldBeTrue();
    }

    [Fact]
    public void Intersect_WithOverlappingRects_ShouldReturnIntersection()
    {
        var rect1 = new RectF(0, 0, 100, 100);
        var rect2 = new RectF(50, 50, 100, 100);

        var result = GeometryUtils.Intersect(rect1, rect2);

        result.ShouldNotBeNull();
        result!.Value.Left.ShouldBe(50);
        result!.Value.Top.ShouldBe(50);
        result!.Value.Width.ShouldBe(50);
        result!.Value.Height.ShouldBe(50);
    }

    [Fact]
    public void Intersect_WithNonOverlappingRects_ShouldReturnNull()
    {
        var rect1 = new RectF(0, 0, 50, 50);
        var rect2 = new RectF(100, 100, 50, 50);

        var result = GeometryUtils.Intersect(rect1, rect2);

        result.ShouldBeNull();
    }

    [Fact]
    public void Union_ShouldReturnBoundingRect()
    {
        var rect1 = new RectF(0, 0, 50, 50);
        var rect2 = new RectF(25, 25, 50, 50);

        var result = GeometryUtils.Union(rect1, rect2);

        result.Left.ShouldBe(0);
        result.Top.ShouldBe(0);
        result.Right.ShouldBe(75);
        result.Bottom.ShouldBe(75);
    }

    [Fact]
    public void Area_ShouldCalculateCorrectly()
    {
        var rect = new RectF(0, 0, 100, 50);

        var area = GeometryUtils.Area(rect);

        area.ShouldBe(5000);
    }

    [Fact]
    public void IsEmpty_WithZeroSize_ShouldReturnTrue()
    {
        var rect1 = new RectF(0, 0, 0, 100);
        var rect2 = new RectF(0, 0, 100, 0);

        GeometryUtils.IsEmpty(rect1).ShouldBeTrue();
        GeometryUtils.IsEmpty(rect2).ShouldBeTrue();
    }

    [Fact]
    public void IsEmpty_WithPositiveSize_ShouldReturnFalse()
    {
        var rect = new RectF(0, 0, 100, 100);

        GeometryUtils.IsEmpty(rect).ShouldBeFalse();
    }

    [Fact]
    public void Inflate_ShouldExpandRect()
    {
        var rect = new RectF(50, 50, 100, 100);

        var result = GeometryUtils.Inflate(rect, 10, 20);

        result.Left.ShouldBe(40);
        result.Top.ShouldBe(30);
        result.Width.ShouldBe(120);
        result.Height.ShouldBe(140);
    }

    [Fact]
    public void Deflate_ShouldShrinkRect()
    {
        var rect = new RectF(50, 50, 100, 100);

        var result = GeometryUtils.Deflate(rect, 10, 20);

        result.Left.ShouldBe(60);
        result.Top.ShouldBe(70);
        result.Width.ShouldBe(80);
        result.Height.ShouldBe(60);
    }

    [Fact]
    public void Distance_ShouldCalculateCorrectly()
    {
        var distance = GeometryUtils.Distance(0, 0, 3, 4);

        distance.ShouldBe(5); // 3-4-5 triangle
    }

    [Fact]
    public void Lerp_ShouldInterpolateCorrectly()
    {
        GeometryUtils.Lerp(0, 100, 0).ShouldBe(0);
        GeometryUtils.Lerp(0, 100, 0.5f).ShouldBe(50);
        GeometryUtils.Lerp(0, 100, 1).ShouldBe(100);
    }

    [Fact]
    public void Clamp_ShouldLimitValueToRange()
    {
        GeometryUtils.Clamp(50, 0, 100).ShouldBe(50);
        GeometryUtils.Clamp(-10, 0, 100).ShouldBe(0);
        GeometryUtils.Clamp(150, 0, 100).ShouldBe(100);
    }

    [Fact]
    public void FloatEquals_WithSimilarValues_ShouldReturnTrue()
    {
        GeometryUtils.FloatEquals(1.0f, 1.00001f).ShouldBeTrue();
    }

    [Fact]
    public void FloatEquals_WithDifferentValues_ShouldReturnFalse()
    {
        GeometryUtils.FloatEquals(1.0f, 2.0f).ShouldBeFalse();
    }

    [Fact]
    public void FloatEquals_WithCustomEpsilon_ShouldUseIt()
    {
        GeometryUtils.FloatEquals(1.0f, 1.5f, 0.6f).ShouldBeTrue();
        GeometryUtils.FloatEquals(1.0f, 1.5f, 0.4f).ShouldBeFalse();
    }
}
