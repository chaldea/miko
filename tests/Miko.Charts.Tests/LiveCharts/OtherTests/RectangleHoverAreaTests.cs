// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Drawing;
using LiveChartsCore.Measure;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class RectangleHoverAreaTests
{
    public RectangleHoverAreaTests() => CoreTestsSetup.Ensure();

    // Regression for #2165: stacked column/row series with negative values produce
    // RectangleHoverArea instances whose Width or Height is negative. IsPointerOver
    // must treat (X, X+Width) and (Y, Y+Height) as unordered ranges.

    [Fact]
    public void RectangleHoverAreaIsPointerOverNegativeHeight()
    {
        // Same rect as a stacked column going downward from baseline:
        // top edge at y=20, bottom edge at y=70, expressed as Y=70 / Height=-50.
        var area = new RectangleHoverArea(10, 70, 100, -50);

        // Point that is geometrically inside the rectangle.
        area.IsPointerOver(new LvcPoint(50, 40), FindingStrategy.CompareAll).ShouldBeTrue();
        area.IsPointerOver(new LvcPoint(50, 40), FindingStrategy.CompareAllTakeClosest).ShouldBeTrue();
        area.IsPointerOver(new LvcPoint(50, 40), FindingStrategy.CompareOnlyY).ShouldBeTrue();

        // Outside in Y on either side of the (unordered) range.
        (!area.IsPointerOver(new LvcPoint(50, 10), FindingStrategy.CompareAll)).ShouldBeTrue();
        (!area.IsPointerOver(new LvcPoint(50, 80), FindingStrategy.CompareAll)).ShouldBeTrue();
    }

    [Fact]
    public void RectangleHoverAreaIsPointerOverNegativeWidth()
    {
        // Same shape but for a row series: X=110 / Width=-100 spans x in [10, 110].
        var area = new RectangleHoverArea(110, 20, -100, 50);

        area.IsPointerOver(new LvcPoint(50, 40), FindingStrategy.CompareAll).ShouldBeTrue();
        area.IsPointerOver(new LvcPoint(50, 40), FindingStrategy.CompareOnlyX).ShouldBeTrue();

        (!area.IsPointerOver(new LvcPoint(5, 40), FindingStrategy.CompareAll)).ShouldBeTrue();
        (!area.IsPointerOver(new LvcPoint(120, 40), FindingStrategy.CompareAll)).ShouldBeTrue();
    }

    [Fact]
    public void RectangleHoverAreaIsPointerOverNegativeWidthAndHeight()
    {
        // Both negative — rectangle covers x in [10, 110], y in [20, 70].
        var area = new RectangleHoverArea(110, 70, -100, -50);

        area.IsPointerOver(new LvcPoint(50, 40), FindingStrategy.CompareAll).ShouldBeTrue();
        (!area.IsPointerOver(new LvcPoint(5, 40), FindingStrategy.CompareAll)).ShouldBeTrue();
        (!area.IsPointerOver(new LvcPoint(50, 80), FindingStrategy.CompareAll)).ShouldBeTrue();
    }

    [Fact]
    public void RectangleHoverAreaDistanceToWithNegativeDimensions()
    {
        // Center is at (X + Width/2, Y + Height/2) regardless of sign.
        var area = new RectangleHoverArea(110, 70, -100, -50); // center: (60, 45)
        var distance = area.DistanceTo(new LvcPoint(60, 45), FindingStrategy.CompareAll);

        (Math.Abs(distance - 0) < 0.001).ShouldBeTrue();
    }
}
