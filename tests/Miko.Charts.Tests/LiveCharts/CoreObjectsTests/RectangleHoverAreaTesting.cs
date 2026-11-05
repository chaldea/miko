// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.CoreObjectsTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Drawing;
using LiveChartsCore.Measure;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class RectangleHoverAreaTesting
{
    public RectangleHoverAreaTesting() => CoreTestsSetup.Ensure();

    // Contract that the documented #1847 sentinel-coordinate recipe relies on:
    // a zero-height column hover area (produced by Mapping non-finite values
    // to Coordinate(index, 0)) must still trigger a tooltip when the chart
    // resolves the find via an X-only strategy. Internally the rectangle is
    // clamped to at least 1 px in each dimension before the X containment
    // check so the user can hover anywhere in the bar's column and still hit
    // the area.
    //
    // If this test breaks, the docs section "NaN and Infinity" -> "Showing
    // the infinity sign in the tooltip" needs to be revisited.
    [Fact]
    public void IsPointerOver_ZeroHeight_Tooltipable_OnXOnlyStrategy()
    {
        // Bar's column at x in [100, 150], collapsed to zero height at y=200.
        var ha = new RectangleHoverArea();
        _ = ha.SetDimensions(x: 100, y: 200, width: 50, height: 0);

        // Pointer anywhere within the column on the X-only strategy must hit.
        ha.IsPointerOver(new LvcPoint(125, 50), FindingStrategy.CompareOnlyXTakeClosest).ShouldBeTrue(
            "X within column, Y far above the zero-height area should still hit.");
        ha.IsPointerOver(new LvcPoint(125, 200), FindingStrategy.CompareOnlyXTakeClosest).ShouldBeTrue(
            "X within column, Y at the area should hit.");
        ha.IsPointerOver(new LvcPoint(125, 350), FindingStrategy.CompareOnlyXTakeClosest).ShouldBeTrue(
            "X within column, Y far below the zero-height area should still hit.");

        // Pointer outside the column must miss.
        ha.IsPointerOver(new LvcPoint(50, 200), FindingStrategy.CompareOnlyXTakeClosest).ShouldBeFalse(
            "X to the left of the column should miss.");
        ha.IsPointerOver(new LvcPoint(180, 200), FindingStrategy.CompareOnlyXTakeClosest).ShouldBeFalse(
            "X to the right of the column should miss.");
    }

    [Fact]
    public void IsPointerOver_ZeroHeight_NotTooltipable_OnYOnlyStrategy()
    {
        // The Y-only strategy intentionally rejects the zero-height area,
        // even after the 1 px clamp - the clamp is just enough to give an
        // exact-match a chance, not enough to span the chart vertically.
        var ha = new RectangleHoverArea();
        _ = ha.SetDimensions(x: 100, y: 200, width: 50, height: 0);

        ha.IsPointerOver(new LvcPoint(125, 50), FindingStrategy.CompareOnlyYTakeClosest).ShouldBeFalse();
        ha.IsPointerOver(new LvcPoint(125, 350), FindingStrategy.CompareOnlyYTakeClosest).ShouldBeFalse();

        // The clamp does still gives an exact-Y hit at the area's row.
        ha.IsPointerOver(new LvcPoint(125, 200.5f), FindingStrategy.CompareOnlyYTakeClosest).ShouldBeTrue();
    }
}
