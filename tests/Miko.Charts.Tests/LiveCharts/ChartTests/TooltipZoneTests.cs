// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.ChartTests;

using LiveChartsCore.Motion;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class TooltipZoneTests
{
    public TooltipZoneTests() => CoreTestsSetup.Ensure();

    // Issue #2304: the tooltip used to register its DrawnTask in CanvasZone.NoClip
    // (1), but Y-axis ticks live in CanvasZone.YCrosshair (3) and zones draw in
    // declaration order. Result: axis ticks paint on top of the tooltip on any
    // multi-Y-axis chart, regardless of the tooltip's ZIndex = 10100.
    //
    // The pixel manifestation depends on whether the tooltip's auto-placement
    // happens to overlap a tick path in the rendered layout, so this test pins
    // the contract directly: the tooltip's task lands in the LAST canvas zone,
    // which is the one that draws after every axis-related zone. The test does
    // not name CanvasZone.Overlay so it would still fail behaviourally (delta = 0,
    // not 1) if the new constant were renamed; it only asserts the routing.

    [Fact]
    public void TooltipLandsInLastCanvasZone_Issue2304()
    {
        var chart = new InMemoryCartesianChart
        {
            Width = 300,
            Height = 300,
            ExplicitDisposing = true,
            View =
            {
                Series = [new LineSeries<double> { Values = [1, 2, 3, 4, 5] }],
                XAxes = [new Axis()],
                YAxes = [new Axis()],
            }
        };

        // first render — no pointer set, tooltip not shown yet.
        chart.GetImage().Dispose();
        var lastZoneIndex = chart.CoreCanvas.Zones.Length - 1;
        var lastZoneBefore = chart.CoreCanvas.Zones[lastZoneIndex].CountTasks();

        // open the tooltip
        chart.CoreChart._isPointerIn = true;
        chart.CoreChart._isToolTipOpen = true;
        chart.CoreChart._pointerPosition = new(150, 150);
        chart.GetImage().Dispose();

        var lastZoneAfter = chart.CoreCanvas.Zones[lastZoneIndex].CountTasks();

        (lastZoneAfter - lastZoneBefore).ShouldBe(
            1,
            $"Tooltip task must land in the last canvas zone (index {lastZoneIndex}). " +
            $"Before tooltip: {lastZoneBefore} tasks, after: {lastZoneAfter}. " +
            "If the delta is 0 the tooltip went into an earlier zone and axis ticks will paint over it.");
    }

    [Fact]
    public void AddGeometry_ThrowsForMissingZone()
    {
        var canvas = new CoreMotionCanvas();
        var missingZone = canvas.Zones.Length;

        _ = Assert.Throws<ArgumentOutOfRangeException>(
            () => canvas.AddGeometry(missingZone));
    }
}
