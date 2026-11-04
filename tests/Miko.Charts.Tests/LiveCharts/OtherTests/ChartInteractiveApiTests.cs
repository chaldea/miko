// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class ChartInteractiveApiTests
{
    public ChartInteractiveApiTests() => CoreTestsSetup.Ensure();

    private static (InMemoryCartesianChart chart, Axis xAxis, Axis yAxis, CartesianChartEngine core) CreatePinnedChart()
    {
        // MinLimit/MaxLimit are pinned so Zoom/Pan operations leave a measurable change
        // on the axis limits without the auto-fit logic snapping things back.
        var xAxis = new Axis { MinLimit = 0, MaxLimit = 10 };
        var yAxis = new Axis { MinLimit = 0, MaxLimit = 10 };

        var chart = new InMemoryCartesianChart
        {
            Width = 400,
            Height = 400,
            View =
            {
                XAxes = [xAxis],
                YAxes = [yAxis],
                Series = [new LineSeries<double> { Values = [1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d, 10d] }]
            }
        };

        chart.GetImage().Dispose();
        var core = (CartesianChartEngine)chart.CoreChart;
        return (chart, xAxis, yAxis, core);
    }

    [Fact]
    public void Zoom_WithZoomXOnly_AffectsOnlyXAxis()
    {
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xRangeBefore = xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value;
        var yRangeBefore = yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value;

        core.Zoom(ZoomAndPanMode.ZoomX | ZoomAndPanMode.NoFit, new LvcPoint(200, 200), ZoomDirection.ZoomIn);

        var xRangeAfter = xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value;
        var yRangeAfter = yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value;

        (xRangeAfter < xRangeBefore).ShouldBeTrue("ZoomX should shrink the X axis range.");
        yRangeAfter.ShouldBe(yRangeBefore, 1e-9, "ZoomX must leave the Y axis untouched.");
    }

    [Fact]
    public void Zoom_WithPanXOnly_DoesNotZoomXAxis()
    {
        // PanX alone enables panning but NOT zooming; a Zoom call should be a no-op.
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xRangeBefore = xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value;
        var yRangeBefore = yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value;

        core.Zoom(ZoomAndPanMode.PanX | ZoomAndPanMode.NoFit, new LvcPoint(200, 200), ZoomDirection.ZoomIn);

        (xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value).ShouldBe(xRangeBefore, 1e-9);
        (yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value).ShouldBe(yRangeBefore, 1e-9);
    }

    [Fact]
    public void Pan_WithPanXOnly_AffectsOnlyXAxis()
    {
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xMinBefore = xAxis.MinLimit!.Value;
        var yMinBefore = yAxis.MinLimit!.Value;

        core.Pan(ZoomAndPanMode.PanX | ZoomAndPanMode.NoFit, new LvcPoint(20, 20));

        xAxis.MinLimit!.Value.ShouldNotBe(xMinBefore, "PanX should shift the X axis.");
        yAxis.MinLimit!.Value.ShouldBe(yMinBefore, 1e-9, "PanX must leave the Y axis untouched.");
    }

    [Fact]
    public void Pan_WithZoomXOnly_DoesNotPanXAxis()
    {
        // ZoomX alone enables zoom but NOT pan; a Pan call should be a no-op.
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xMinBefore = xAxis.MinLimit!.Value;
        var yMinBefore = yAxis.MinLimit!.Value;

        core.Pan(ZoomAndPanMode.ZoomX | ZoomAndPanMode.NoFit, new LvcPoint(20, 20));

        xAxis.MinLimit!.Value.ShouldBe(xMinBefore, 1e-9);
        yAxis.MinLimit!.Value.ShouldBe(yMinBefore, 1e-9);
    }

    [Fact]
    public void Zoom_WithZoomYOnly_AffectsOnlyYAxis()
    {
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xRangeBefore = xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value;
        var yRangeBefore = yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value;

        core.Zoom(ZoomAndPanMode.ZoomY | ZoomAndPanMode.NoFit, new LvcPoint(200, 200), ZoomDirection.ZoomIn);

        (xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value).ShouldBe(xRangeBefore, 1e-9);
        (yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value < yRangeBefore).ShouldBeTrue();
    }

    [Fact]
    public void Pan_WithPanYOnly_AffectsOnlyYAxis()
    {
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xMinBefore = xAxis.MinLimit!.Value;
        var yMinBefore = yAxis.MinLimit!.Value;

        core.Pan(ZoomAndPanMode.PanY | ZoomAndPanMode.NoFit, new LvcPoint(20, 20));

        xAxis.MinLimit!.Value.ShouldBe(xMinBefore, 1e-9);
        yAxis.MinLimit!.Value.ShouldNotBe(yMinBefore);
    }

    [Fact]
    public void CompositeXFlag_StillEnablesBothPanAndZoomOnX()
    {
        // Backward-compat guarantee: ZoomAndPanMode.X must keep pan+zoom semantics.
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xRangeBefore = xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value;
        var yRangeBefore = yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value;
        var xMinBefore = xAxis.MinLimit!.Value;

        core.Zoom(ZoomAndPanMode.X | ZoomAndPanMode.NoFit, new LvcPoint(200, 200), ZoomDirection.ZoomIn);
        core.Pan(ZoomAndPanMode.X | ZoomAndPanMode.NoFit, new LvcPoint(20, 0));

        (xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value < xRangeBefore).ShouldBeTrue();
        xAxis.MinLimit!.Value.ShouldNotBe(xMinBefore);
        (yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value).ShouldBe(yRangeBefore, 1e-9);
    }

    [Fact]
    public async Task Zoom_NoFitArgumentIsHonored_EvenWhenViewZoomModeOmitsIt()
    {
        // Reproduces issue #2119: a manual core.Zoom(... | NoFit, ...) call must keep
        // the zoomed range. Previously the post-zoom debounced fit checked the view's
        // ZoomMode (None here) instead of the flags argument, snapping limits back.
        var xAxis = new Axis { MinLimit = 50, MaxLimit = 60 };
        var yAxis = new Axis { MinLimit = 50, MaxLimit = 60 };

        var chart = new InMemoryCartesianChart
        {
            Width = 400,
            Height = 400,
            View =
            {
                ZoomMode = ZoomAndPanMode.None,
                XAxes = [xAxis],
                YAxes = [yAxis],
                Series = [new LineSeries<double> { Values = [1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d, 10d] }]
            }
        };
        chart.GetImage().Dispose();

        var core = (CartesianChartEngine)chart.CoreChart;

        // Pinned MaxLimit (60) sits above the data bounds (1..10). Without NoFit, the
        // post-zoom Fit would clamp MaxLimit down toward the data max (~10).
        core.Zoom(ZoomAndPanMode.Both | ZoomAndPanMode.NoFit, new LvcPoint(200, 200), ZoomDirection.ZoomIn);

        // Wait past the 300 ms debounce so FitAllOnZoom has had a chance to fire.
        await Task.Delay(450);
        chart.GetImage().Dispose();

        (xAxis.MaxLimit!.Value > 50).ShouldBeTrue($"NoFit must prevent X MaxLimit snap-to-data; X MaxLimit={xAxis.MaxLimit}.");
        (yAxis.MaxLimit!.Value > 50).ShouldBeTrue($"NoFit must prevent Y MaxLimit snap-to-data; Y MaxLimit={yAxis.MaxLimit}.");
    }
}
