// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.Drawing.Segments;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Painting;
using Miko.Charts.Tests.LiveCharts.MockedObjects;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class StackedAreaSeriesTest
{
    public StackedAreaSeriesTest() => CoreTestsSetup.Ensure();

    [Fact]
    public void ShouldScale()
    {
        var sutSeries = new StackedAreaSeries<double>
        {
            Values = [1, 2, 4, 8, 16, 32, 64, 128, 256],
            GeometrySize = 10
        };

        var sutSeries2 = new StackedAreaSeries<double>
        {
            Values = [1, 2, 4, 8, 16, 32, 64, 128, 256],
            GeometrySize = 10
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = [sutSeries, sutSeries2],
                XAxes = [new Axis { MinLimit = -1, MaxLimit = 10 }],
                YAxes = [new Axis { MinLimit = 0, MaxLimit = 512 }]
            }
        };

        chart.GetImage().Dispose();
        //chart.SaveImage("test.png"); // use this method to see the actual tested image

        var datafactory = sutSeries.DataFactory;
        var points = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();

        var unit = points.First(x => x.Coordinate.PrimaryValue == 1);
        var typedUnit = sutSeries.ConvertToTypedChartPoint(unit);

        var toCompareGuys = points.Where(x => x != unit).Select(sutSeries.ConvertToTypedChartPoint);

        var datafactory2 = sutSeries2.DataFactory;
        var points2 = datafactory2.Fetch(sutSeries2, chart.CoreChart).ToArray();
        var unit2 = points2.First(x => x.Coordinate.PrimaryValue == 1);
        var typedUnit2 = sutSeries.ConvertToTypedChartPoint(unit2);
        var toCompareGuys2 = points2.Where(x => x != unit2).Select(sutSeries2.ConvertToTypedChartPoint);

        // ensure the unit has valid dimensions
        (typedUnit.Visual!.Width == 10 && typedUnit.Visual.Height == 10).ShouldBeTrue();

        var previous = typedUnit;
        float? previousX = null;
        float? previousXArea = null;

        foreach (var sutPoint in toCompareGuys)
        {
            var previousBezier = ((CubicSegmentVisualPoint)previous.Context.AdditionalVisuals!)?.Segment;
            var sutBezier = ((CubicSegmentVisualPoint)sutPoint.Context.AdditionalVisuals!).Segment;

            // test x
            var currentDeltaX = previous.Visual!.X - sutPoint.Visual!.X;
            var currentDeltaAreaX = previousBezier!.Xj - sutBezier.Xj;
            (
                previousX is null
                ||
                Math.Abs(previousX.Value - currentDeltaX) < 0.001).ShouldBeTrue();
            (
                previousXArea is null
                ||
                Math.Abs(previousXArea.Value - currentDeltaX) < 0.001).ShouldBeTrue();

            // test y
            var p = 1f - (sutPoint.Coordinate.PrimaryValue + sutPoint.StackedValue!.Start) / 512f;
            (
                Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutPoint.Visual.Y + chart.CoreChart.DrawMarginLocation.Y) < 0.001).ShouldBeTrue();
            (
                Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutBezier.Yj + chart.CoreChart.DrawMarginLocation.Y) < 0.001).ShouldBeTrue();

            previousX = previous.Visual.X - sutPoint.Visual.X;
            previousXArea = previousBezier.Xj - sutBezier.Xj;
            previous = sutPoint;
        }

        previous = typedUnit2;
        previousX = null;
        previousXArea = null;
        foreach (var sutPoint in toCompareGuys2)
        {
            var previousBezier = ((CubicSegmentVisualPoint)previous.Context.AdditionalVisuals!).Segment;
            var sutBezier = ((CubicSegmentVisualPoint)sutPoint.Context.AdditionalVisuals!).Segment;

            // test x
            var currentDeltaX = previous.Visual!.X - sutPoint.Visual!.X;
            var currentDeltaAreaX = previousBezier.Xj - sutBezier.Xj;
            (
                previousX is null
                ||
                Math.Abs(previousX.Value - currentDeltaX) < 0.001).ShouldBeTrue();
            (
                previousXArea is null
                ||
                Math.Abs(previousXArea.Value - currentDeltaX) < 0.001).ShouldBeTrue();

            // test y
            var p = 1f - (sutPoint.Coordinate.PrimaryValue + sutPoint.StackedValue!.Start) / 512f;
            (
                Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutPoint.Visual.Y + chart.CoreChart.DrawMarginLocation.Y) < 0.001).ShouldBeTrue();
            (
                Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutBezier.Yj + chart.CoreChart.DrawMarginLocation.Y) < 0.001).ShouldBeTrue();

            previousX = previous.Visual.X - sutPoint.Visual.X;
            previousXArea = previousBezier.Xj - sutBezier.Xj;
            previous = sutPoint;
        }
    }

    [Fact]
    public void ShouldPlaceDataLabel()
    {
        var gs = 5f;
        var sutSeries = new StackedAreaSeries<double, RectangleGeometry, TestLabel>
        {
            Values = [-10, -5, -1, 0, 1, 5, 10],
            DataPadding = new LvcPoint(0, 0),
            GeometrySize = gs * 2,
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 500,
            Height = 500,
            View =
            {
                DrawMargin = new Margin(100),
                DrawMarginFrame = new DrawMarginFrame { Stroke = new SolidColorPaint(TestColors.Yellow, 2) },
                TooltipPosition = TooltipPosition.Top,
                Series = [sutSeries],
                XAxes = [new Axis { IsVisible = false }],
                YAxes = [new Axis { IsVisible = false }]
            }
        };

        var datafactory = sutSeries.DataFactory;

        // TEST HIDDEN ===========================================================
        chart.GetImage().Dispose();

        var points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        (sutSeries.DataLabelsPosition == DataLabelsPosition.End).ShouldBeTrue();
        points.All(x => x.Label is null).ShouldBeTrue();

        sutSeries.DataLabelsPaint = new SolidColorPaint
        {
            Color = TestColors.Black,
            FontFamily = "Arial",
            FontWeight = Miko.Common.FontWeight.Bold
        };

        // TEST TOP ===============================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Top;
        chart.GetImage().Dispose();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual!;
            var l = p.Label!;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            (
                Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&    // x is centered
                Math.Abs(v.Y - (l.Y + ls.Height * 0.5 + gs)) < 0.01).ShouldBeTrue();  // y is top
        }

        // TEST BOTTOM ===========================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Bottom;

        chart.GetImage().Dispose();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual!;
            var l = p.Label!;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            (
                Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&              // x is centered
                Math.Abs(v.Y + v.Height - (l.Y - ls.Height * 0.5 + gs)) < 0.01).ShouldBeTrue(); // y is bottom
        }

        // TEST RIGHT ============================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Right;

        chart.GetImage().Dispose();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual!;
            var l = p.Label!;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            (
                Math.Abs(v.X + v.Width - (l.X - ls.Width * 0.5 + gs)) < 0.01 &&  // x is right
                Math.Abs(v.Y + v.Height * 0.5 - l.Y - gs) < 0.01).ShouldBeTrue();               // y is centered
        }

        // TEST LEFT =============================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Left;

        chart.GetImage().Dispose();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual!;
            var l = p.Label!;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            (
                Math.Abs(v.X - (l.X + ls.Width * 0.5f + gs)) < 0.01 &&   // x is left
                Math.Abs(v.Y + v.Height * 0.5f - l.Y - gs) < 0.01).ShouldBeTrue();      // y is centered
        }

        // TEST MIDDLE ===========================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Middle;

        chart.GetImage().Dispose();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual!;
            var l = p.Label!;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            (
                Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&      // x is centered
                Math.Abs(v.Y + v.Height * 0.5f - l.Y - gs) < 0.01).ShouldBeTrue();      // y is centered
        }

        // TEST START ===========================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Start;

        chart.GetImage().Dispose();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual!;
            var l = p.Label!;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            if (p.Model <= 0)
            {
                // it should be placed using the top position
                (
                    Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&    // x is centered
                    Math.Abs(v.Y - (l.Y + ls.Height * 0.5 + gs)) < 0.01).ShouldBeTrue();  // y is top
            }
            else
            {
                // it should be placed using the bottom position
                (
                    Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&              // x is centered
                    Math.Abs(v.Y + v.Height - (l.Y - ls.Height * 0.5 + gs)) < 0.01).ShouldBeTrue(); // y is bottom
            }
        }

        // TEST END ===========================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.End;

        chart.GetImage().Dispose();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual!;
            var l = p.Label!;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            if (p.Model <= 0)
            {
                // it should be placed using the bottom position
                (
                    Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&              // x is centered
                    Math.Abs(v.Y + v.Height - (l.Y - ls.Height * 0.5 + gs)) < 0.01).ShouldBeTrue(); // y is bottom
            }
            else
            {
                // it should be placed using the top position
                (
                    Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&    // x is centered
                    Math.Abs(v.Y - (l.Y + ls.Height * 0.5 + gs)) < 0.01).ShouldBeTrue();  // y is top
            }
        }

        // FINALLY IF LABELS ARE NULL, IT SHOULD REMOVE THE CURRENT LABELS.
        var previousPaint = sutSeries.DataLabelsPaint;
        sutSeries.DataLabelsPaint = null;
        chart.GetImage().Dispose();

        (!chart.CoreCanvas.ContainsPaintTask(previousPaint!)).ShouldBeTrue();
    }

    [Fact]
    public void ShouldHandleMixedPositiveNegativeValues()
    {
        // Regression test for #2073 and #2152.
        //
        // Stacker exposes three independent tracks per index:
        //   * Start/End            — accumulator that only positives contribute to,
        //                            so stacked column/row positives grow upward from 0.
        //   * NegativeStart/End    — accumulator that only negatives contribute to,
        //                            so stacked column/row negatives grow downward from 0.
        //   * CumulativeStart/End  — running sum of every value regardless of sign,
        //                            used by stacked area/line series for a continuous
        //                            baseline across mixed signs (Excel-like).
        //
        // Series1: positive at index 0, negative at index 1
        // Series2: negative at index 0, positive at index 1
        var series1 = new StackedAreaSeries<double>
        {
            Values = [5, -3],
            GeometrySize = 10
        };

        var series2 = new StackedAreaSeries<double>
        {
            Values = [-2, 4],
            GeometrySize = 10
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = [series1, series2],
                XAxes = [new Axis()],
                YAxes = [new Axis()]
            }
        };

        chart.GetImage().Dispose();

        var points1 = series1.DataFactory.Fetch(series1, chart.CoreChart).ToArray();
        var points2 = series2.DataFactory.Fetch(series2, chart.CoreChart).ToArray();

        var point1_0 = points1.Single(p => p.Coordinate.SecondaryValue == 0);
        var point2_0 = points2.Single(p => p.Coordinate.SecondaryValue == 0);
        var point1_1 = points1.Single(p => p.Coordinate.SecondaryValue == 1);
        var point2_1 = points2.Single(p => p.Coordinate.SecondaryValue == 1);

        // Series1[0] = +5 — only feeds the positive and cumulative tracks.
        point1_0.StackedValue!.Start.ShouldBe(0, 0.001);
        point1_0.StackedValue.End.ShouldBe(5, 0.001);
        point1_0.StackedValue.NegativeStart.ShouldBe(0, 0.001);
        point1_0.StackedValue.NegativeEnd.ShouldBe(0, 0.001);
        point1_0.StackedValue.CumulativeStart.ShouldBe(0, 0.001);
        point1_0.StackedValue.CumulativeEnd.ShouldBe(5, 0.001);

        // Series2[0] = -2 stacked on top of Series1[0]:
        //   * positives don't move (Start/End stay at 5 — Series1's positive end)
        //   * negatives extend independently from 0 down to -2
        //   * cumulative = 5 + (-2) = 3 — continuous baseline used by area path
        point2_0.StackedValue!.Start.ShouldBe(5, 0.001);
        point2_0.StackedValue.End.ShouldBe(5, 0.001);
        point2_0.StackedValue.NegativeStart.ShouldBe(0, 0.001);
        point2_0.StackedValue.NegativeEnd.ShouldBe(-2, 0.001);
        point2_0.StackedValue.CumulativeStart.ShouldBe(5, 0.001);
        point2_0.StackedValue.CumulativeEnd.ShouldBe(3, 0.001);

        // Series1[1] = -3 — only feeds the negative and cumulative tracks.
        point1_1.StackedValue!.Start.ShouldBe(0, 0.001);
        point1_1.StackedValue.End.ShouldBe(0, 0.001);
        point1_1.StackedValue.NegativeStart.ShouldBe(0, 0.001);
        point1_1.StackedValue.NegativeEnd.ShouldBe(-3, 0.001);
        point1_1.StackedValue.CumulativeStart.ShouldBe(0, 0.001);
        point1_1.StackedValue.CumulativeEnd.ShouldBe(-3, 0.001);

        // Series2[1] = +4 stacked on top of Series1[1]:
        //   * positives extend independently from 0 up to +4
        //   * negatives don't move (NegativeStart/End stay at -3 — Series1's neg end)
        //   * cumulative = -3 + 4 = 1
        point2_1.StackedValue!.Start.ShouldBe(0, 0.001);
        point2_1.StackedValue.End.ShouldBe(4, 0.001);
        point2_1.StackedValue.NegativeStart.ShouldBe(-3, 0.001);
        point2_1.StackedValue.NegativeEnd.ShouldBe(-3, 0.001);
        point2_1.StackedValue.CumulativeStart.ShouldBe(-3, 0.001);
        point2_1.StackedValue.CumulativeEnd.ShouldBe(1, 0.001);
    }
}
