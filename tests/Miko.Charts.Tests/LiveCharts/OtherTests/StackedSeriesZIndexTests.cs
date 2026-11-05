// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore.Painting;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Painting;
using Shouldly;

// Regression for issue #1923. Stacked area / step / polar line series
// silently overrode the user's ZIndex with `1000 - stacker.Position`,
// so a non-stacked LineSeries left at default ZIndex always drew BEHIND
// a stacked area, and the ZIndex property reported the user's value
// while the actual paint Z was the workaround value.
[Collection(ChartsCollection.Name)]
public class StackedSeriesZIndexTests
{
    public StackedSeriesZIndexTests() => CoreTestsSetup.Ensure();

    [Fact]
    public void StackedArea_HonorsUserZIndex_OnFillAndStroke()
    {
        var fill = new SolidColorPaint(TestColors.Red);
        var stroke = new SolidColorPaint(TestColors.Black, 4);

        var chart = new InMemoryCartesianChart
        {
            Width = 200,
            Height = 200,
            View =
            {
                Series = [
                    new StackedAreaSeries<double>
                    {
                        Values = [1, 2, 3, 4],
                        Fill = fill,
                        Stroke = stroke,
                        ZIndex = 50,
                    }
                ]
            }
        };

        using var image = chart.GetImage();

        fill.ZIndex.ShouldBe(
            50 + PaintConstants.SeriesFillZIndexOffset,
            "StackedArea Fill must honor user ZIndex (was overridden by 1000 - stack position).");
        stroke.ZIndex.ShouldBe(
            50 + PaintConstants.SeriesStrokeZIndexOffset,
            "StackedArea Stroke must honor user ZIndex (was overridden by 1000 - stack position).");
    }

    [Fact]
    public void DefaultLineSeries_DrawsAboveDefaultStackedArea_BySeriesId()
    {
        // With both at default ZIndex, the LineSeries (added second, so
        // SeriesId == 1) should draw on top of the StackedArea (SeriesId
        // == 0). Previously the StackedArea jumped to 1000 unconditionally
        // and won regardless of insertion order.
        var lineFill = new SolidColorPaint(TestColors.Blue);
        var stackedFill = new SolidColorPaint(TestColors.Red);

        var chart = new InMemoryCartesianChart
        {
            Width = 200,
            Height = 200,
            View =
            {
                Series = [
                    new StackedAreaSeries<double>
                    {
                        Values = [1, 2, 3, 4],
                        Fill = stackedFill,
                    },
                    new LineSeries<double>
                    {
                        Values = [1, 2, 3, 4],
                        Fill = lineFill,
                    }
                ]
            }
        };

        using var image = chart.GetImage();

        (lineFill.ZIndex > stackedFill.ZIndex).ShouldBeTrue(
            $"LineSeries Fill (Z={lineFill.ZIndex}) should draw above StackedArea Fill (Z={stackedFill.ZIndex}); see #1923.");
    }

    [Fact]
    public void StackedStepArea_HonorsUserZIndex_OnFillAndStroke()
    {
        var fill = new SolidColorPaint(TestColors.Red);
        var stroke = new SolidColorPaint(TestColors.Black, 4);

        var chart = new InMemoryCartesianChart
        {
            Width = 200,
            Height = 200,
            View =
            {
                Series = [
                    new StackedStepAreaSeries<double>
                    {
                        Values = [1, 2, 3, 4],
                        Fill = fill,
                        Stroke = stroke,
                        ZIndex = 50,
                    }
                ]
            }
        };

        using var image = chart.GetImage();

        fill.ZIndex.ShouldBe(
            50 + PaintConstants.SeriesFillZIndexOffset,
            "StackedStepArea Fill must honor user ZIndex.");
        stroke.ZIndex.ShouldBe(
            50 + PaintConstants.SeriesStrokeZIndexOffset,
            "StackedStepArea Stroke must honor user ZIndex.");
    }
}
