// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class StackedRowSeriesTest
{
    public StackedRowSeriesTest() => CoreTestsSetup.Ensure();

    [Fact]
    public void ShouldScaleProperly()
    {
        var sutSeries = new StackedRowSeries<double>
        {
            Values = new double[] { 1, 2, 4, 8, 16, 32, 64, 128, 256 }
        };

        var sutSeries2 = new StackedRowSeries<double>
        {
            Values = new double[] { 1, 2, 4, 8, 16, 32, 64, 128, 256 }
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new[] { sutSeries, sutSeries2 },
                XAxes = new[] { new Axis { MinLimit = 0, MaxLimit = 512 } },
                YAxes = new[] { new Axis { MinLimit = -1, MaxLimit = 10 } }
            }
        };

        // the original saved "test.png" here (chart.SaveImage); drawing is what the assertions need.
        chart.GetImage().Dispose();

        var datafactory = sutSeries.DataFactory;
        var points = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();

        var unit = points.First(x => x.Coordinate.PrimaryValue == 1);
        var typedUnit = sutSeries.ConvertToTypedChartPoint(unit);

        var toCompareGuys = points.Where(x => x != unit).Select(sutSeries.ConvertToTypedChartPoint);

        var datafactory2 = sutSeries2.DataFactory;
        var points2 = datafactory2.Fetch(sutSeries2, chart.CoreChart).ToArray();

        var unit2 = points2.First(x => x.Coordinate.PrimaryValue == 1);
        var typedUnit2 = sutSeries2.ConvertToTypedChartPoint(unit2);

        var toCompareGuys2 = points2.Where(x => x != unit2).Select(sutSeries2.ConvertToTypedChartPoint);

        // Note #1
        // TODO: for a strange reason, widths are negative... fix that...

        // ensure the unit has valid dimensions
        (typedUnit.Visual!.Width < 0 && typedUnit.Visual.Height > 1).ShouldBeTrue(); // ?

        var previous = typedUnit;
        float? previousY = null;

        foreach (var sutPoint in toCompareGuys)
        {
            // test width
            (
                // the idea is, the second bar should be 2 times bigger than the first one
                // and the third bar should be 4 times bigger than the first one and so on
                Math.Abs(typedUnit.Visual.Width - sutPoint.Visual!.Width / (float)sutPoint.Model) < 0.001).ShouldBeTrue();

            // test height
            (
                // and also the width should be the same.
                Math.Abs(typedUnit.Visual.Height - sutPoint.Visual.Height) < 0.001).ShouldBeTrue();

            // test y
            var currentDeltaY = previous.Visual!.Y - sutPoint.Visual.Y;
            (
                previousY is null
                ||
                Math.Abs(previousY.Value - currentDeltaY) < 0.001).ShouldBeTrue();

            // test x
            // NOTICE THE + sutPoint.Visual.Width IS JUST TO FIX THE NOTE #1
            var p = sutPoint.StackedValue!.Start / 512f;
            var stacked = -p * chart.CoreChart.DrawMarginSize.Width + sutPoint.Visual.Width;
            (
                Math.Abs(sutPoint.Visual.X + stacked - chart.CoreChart.DrawMarginLocation.X) < 0.001).ShouldBeTrue();

            previousY = previous.Visual.Y - sutPoint.Visual.Y;
            previous = sutPoint;
        }

        previous = typedUnit2;
        previousY = null;
        foreach (var sutPoint in toCompareGuys2)
        {
            // test width
            (
                // the idea is, the second bar should be 2 times bigger than the first one
                // and the third bar should be 4 times bigger than the first one and so on
                Math.Abs(typedUnit.Visual.Width - sutPoint.Visual!.Width / (float)sutPoint.Model) < 0.001).ShouldBeTrue();

            // test height
            (
                // and also the width should be the same.
                Math.Abs(typedUnit.Visual.Height - sutPoint.Visual.Height) < 0.001).ShouldBeTrue();

            // test y
            var currentDeltaY = previous.Visual!.Y - sutPoint.Visual.Y;
            (
                previousY is null
                ||
                Math.Abs(previousY.Value - currentDeltaY) < 0.001).ShouldBeTrue();

            // test x
            // NOTICE THE + sutPoint.Visual.Width IS JUST TO FIX THE NOTE #1
            var p = sutPoint.StackedValue!.Start / 512f;
            var stacked = -p * chart.CoreChart.DrawMarginSize.Width + sutPoint.Visual.Width;
            (
                Math.Abs(sutPoint.Visual.X + stacked - chart.CoreChart.DrawMarginLocation.X) < 0.001).ShouldBeTrue();

            previousY = previous.Visual.Y - sutPoint.Visual.Y;
            previous = sutPoint;
        }
    }
}
