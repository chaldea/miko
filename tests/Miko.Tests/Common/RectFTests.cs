using Miko.Common;
using Shouldly;

namespace Miko.Tests.Common;

public class RectFTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var rect = new RectF(10, 20, 100, 50);

        rect.X.ShouldBe(10);
        rect.Y.ShouldBe(20);
        rect.Width.ShouldBe(100);
        rect.Height.ShouldBe(50);
    }

    [Fact]
    public void Properties_ShouldCalculateEdges()
    {
        var rect = new RectF(10, 20, 100, 50);

        rect.Left.ShouldBe(10);
        rect.Top.ShouldBe(20);
        rect.Right.ShouldBe(110);
        rect.Bottom.ShouldBe(70);
    }

    [Fact]
    public void IntersectsWith_WithOverlappingRects_ShouldReturnTrue()
    {
        var rect1 = new RectF(0, 0, 100, 100);
        var rect2 = new RectF(50, 50, 100, 100);

        rect1.IntersectsWith(rect2).ShouldBeTrue();
        rect2.IntersectsWith(rect1).ShouldBeTrue();
    }

    [Fact]
    public void IntersectsWith_WithNonOverlappingRects_ShouldReturnFalse()
    {
        var rect1 = new RectF(0, 0, 100, 100);
        var rect2 = new RectF(200, 200, 100, 100);

        rect1.IntersectsWith(rect2).ShouldBeFalse();
    }

    [Fact]
    public void Contains_WithPointInside_ShouldReturnTrue()
    {
        var rect = new RectF(10, 20, 100, 50);

        rect.Contains(50, 40).ShouldBeTrue();
    }

    [Fact]
    public void Contains_WithPointOutside_ShouldReturnFalse()
    {
        var rect = new RectF(10, 20, 100, 50);

        rect.Contains(150, 100).ShouldBeFalse();
    }

    [Fact]
    public void Union_ShouldReturnBoundingRect()
    {
        var rect1 = new RectF(0, 0, 50, 50);
        var rect2 = new RectF(25, 25, 50, 50);

        var union = RectF.Union(rect1, rect2);

        union.Left.ShouldBe(0);
        union.Top.ShouldBe(0);
        union.Right.ShouldBe(75);
        union.Bottom.ShouldBe(75);
    }

    [Fact]
    public void Intersect_WithOverlappingRects_ShouldReturnIntersection()
    {
        var rect1 = new RectF(0, 0, 100, 100);
        var rect2 = new RectF(50, 50, 100, 100);

        var intersect = RectF.Intersect(rect1, rect2);

        intersect.Left.ShouldBe(50);
        intersect.Top.ShouldBe(50);
        intersect.Right.ShouldBe(100);
        intersect.Bottom.ShouldBe(100);
    }

    [Fact]
    public void Intersect_WithNonOverlappingRects_ShouldReturnEmptyRect()
    {
        var rect1 = new RectF(0, 0, 50, 50);
        var rect2 = new RectF(100, 100, 50, 50);

        var intersect = RectF.Intersect(rect1, rect2);

        intersect.Width.ShouldBe(0);
        intersect.Height.ShouldBe(0);
    }
}

public class EdgeSizesTests
{
    [Fact]
    public void Constructor_WithAllSides_ShouldSetEqualValues()
    {
        var edge = new EdgeSizes(10);

        edge.Top.ShouldBe(10);
        edge.Right.ShouldBe(10);
        edge.Bottom.ShouldBe(10);
        edge.Left.ShouldBe(10);
    }

    [Fact]
    public void Constructor_WithVerticalHorizontal_ShouldSetCorrectly()
    {
        var edge = new EdgeSizes(10, 20);

        edge.Top.ShouldBe(10);
        edge.Bottom.ShouldBe(10);
        edge.Left.ShouldBe(20);
        edge.Right.ShouldBe(20);
    }

    [Fact]
    public void Constructor_WithAllFourSides_ShouldSetIndividually()
    {
        var edge = new EdgeSizes(10, 20, 30, 40);

        edge.Top.ShouldBe(10);
        edge.Right.ShouldBe(20);
        edge.Bottom.ShouldBe(30);
        edge.Left.ShouldBe(40);
    }

    [Fact]
    public void Horizontal_ShouldReturnSumOfLeftAndRight()
    {
        var edge = new EdgeSizes(10, 20, 30, 40);

        edge.Horizontal.ShouldBe(60); // 20 + 40
    }

    [Fact]
    public void Vertical_ShouldReturnSumOfTopAndBottom()
    {
        var edge = new EdgeSizes(10, 20, 30, 40);

        edge.Vertical.ShouldBe(40); // 10 + 30
    }
}
