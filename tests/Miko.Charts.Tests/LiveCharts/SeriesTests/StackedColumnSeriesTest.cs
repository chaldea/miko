// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Painting;
using Miko.Charts.Tests.LiveCharts.MockedObjects;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class StackedColumnSeriesTest
{
    public StackedColumnSeriesTest() => CoreTestsSetup.Ensure();

    [Fact]
    public void ShouldScale()
    {
        var sutSeries = new StackedColumnSeries<double>
        {
            Values = new double[] { 1, 2, 4, 8, 16, 32, 64, 128, 256 }
        };

        var sutSeries2 = new StackedColumnSeries<double>
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
                XAxes = new[] { new Axis { MinLimit = -1, MaxLimit = 10 } },
                YAxes = new[] { new Axis { MinLimit = 0, MaxLimit = 512 } }
            }
        };

        chart.GetImage().Dispose();
        // chart.SaveImage("test.png"); // use this method to see the actual tested image

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

        // ensure the unit has valid dimensions
        (typedUnit.Visual!.Width > 1 && typedUnit.Visual.Height > 1).ShouldBeTrue();

        var previous = typedUnit;
        float? previousX = null;

        foreach (var sutPoint in toCompareGuys)
        {
            // test height
            (
                // the idea is, the second bar should be 2 times bigger than the first one
                // and the third bar should be 4 times bigger than the first one and so on
                Math.Abs(typedUnit.Visual.Height - sutPoint.Visual!.Height / (float)sutPoint.Model) < 0.001).ShouldBeTrue();

            // test width
            (
                // and also the width should be the same.
                Math.Abs(typedUnit.Visual.Width - sutPoint.Visual.Width) < 0.001).ShouldBeTrue();

            // test x
            var currentDeltaX = previous.Visual!.X - sutPoint.Visual.X;
            (
                previousX is null
                ||
                Math.Abs(previousX.Value - currentDeltaX) < 0.001).ShouldBeTrue();

            // test y
            var p = 1f - (sutPoint.Coordinate.PrimaryValue + sutPoint.StackedValue!.Start) / 512f;
            (
                Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutPoint.Visual.Y + chart.CoreChart.DrawMarginLocation.Y) < 0.001).ShouldBeTrue();

            previousX = previous.Visual.X - sutPoint.Visual.X;
            previous = sutPoint;
        }

        previous = typedUnit2;
        previousX = null;
        foreach (var sutPoint in toCompareGuys2)
        {
            // test height
            (
                // the idea is, the second bar should be 2 times bigger than the first one
                // and the third bar should be 4 times bigger than the first one and so on
                Math.Abs(typedUnit.Visual.Height - sutPoint.Visual!.Height / (float)sutPoint.Model) < 0.001).ShouldBeTrue();

            // test width
            (
                // and also the width should be the same.
                Math.Abs(typedUnit.Visual.Width - sutPoint.Visual.Width) < 0.001).ShouldBeTrue();

            // test x
            var currentDeltaX = previous.Visual!.X - sutPoint.Visual.X;
            (
                previousX is null
                ||
                Math.Abs(previousX.Value - currentDeltaX) < 0.001).ShouldBeTrue();

            // test y
            var p = 1f - (sutPoint.Coordinate.PrimaryValue + sutPoint.StackedValue!.Start) / 512f;
            (
                Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutPoint.Visual.Y + chart.CoreChart.DrawMarginLocation.Y) < 0.001).ShouldBeTrue();

            previousX = previous.Visual.X - sutPoint.Visual.X;
            previous = sutPoint;
        }
    }

    [Fact]
    public void ShouldPlaceDataLabel()
    {
        var sutSeries = new StackedColumnSeries<double, RectangleGeometry, TestLabel>
        {
            Values = new double[] { -10, -5, -1, 0, 1, 5, 10 },
            DataPadding = new LvcPoint(0, 0),
            DataLabelsPosition = DataLabelsPosition.End
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
                Series = new[] { sutSeries },
                XAxes = new[] { new Axis { IsVisible = false } },
                YAxes = new[] { new Axis { IsVisible = false } }
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
                Math.Abs(v.X + v.Width * 0.5f - l.X) < 0.01 &&    // x is centered
                Math.Abs(v.Y - (l.Y + ls.Height * 0.5)) < 0.01).ShouldBeTrue();  // y is top
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
                Math.Abs(v.X + v.Width * 0.5f - l.X) < 0.01 &&              // x is centered
                Math.Abs(v.Y + v.Height - (l.Y - ls.Height * 0.5)) < 0.01).ShouldBeTrue(); // y is bottom
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
                Math.Abs(v.X + v.Width - (l.X - ls.Width * 0.5)) < 0.01 &&  // x is right
                Math.Abs(v.Y + v.Height * 0.5 - l.Y) < 0.01).ShouldBeTrue();               // y is centered
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
                Math.Abs(v.X - (l.X + ls.Width * 0.5f)) < 0.01 &&   // x is left
                Math.Abs(v.Y + v.Height * 0.5f - l.Y) < 0.01).ShouldBeTrue();      // y is centered
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
                Math.Abs(v.X + v.Width * 0.5f - l.X) < 0.01 &&      // x is centered
                Math.Abs(v.Y + v.Height * 0.5f - l.Y) < 0.01).ShouldBeTrue();      // y is centered
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
                    Math.Abs(v.X + v.Width * 0.5f - l.X) < 0.01 &&    // x is centered
                    Math.Abs(v.Y - (l.Y + ls.Height * 0.5)) < 0.01).ShouldBeTrue();  // y is top
            }
            else
            {
                // it should be placed using the bottom position
                (
                    Math.Abs(v.X + v.Width * 0.5f - l.X) < 0.01 &&              // x is centered
                    Math.Abs(v.Y + v.Height - (l.Y - ls.Height * 0.5)) < 0.01).ShouldBeTrue(); // y is bottom
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
                    Math.Abs(v.X + v.Width * 0.5f - l.X) < 0.01 &&              // x is centered
                    Math.Abs(v.Y + v.Height - (l.Y - ls.Height * 0.5)) < 0.01).ShouldBeTrue(); // y is bottom
            }
            else
            {
                // it should be placed using the top position
                (
                    Math.Abs(v.X + v.Width * 0.5f - l.X) < 0.01 &&    // x is centered
                    Math.Abs(v.Y - (l.Y + ls.Height * 0.5)) < 0.01).ShouldBeTrue();  // y is top
            }
        }

        // FINALLY IF LABELS ARE NULL, IT SHOULD REMOVE THE CURRENT LABELS.
        var previousPaint = sutSeries.DataLabelsPaint;
        sutSeries.DataLabelsPaint = null;
        chart.GetImage().Dispose();

        (!chart.CoreCanvas.ContainsPaintTask(previousPaint!)).ShouldBeTrue();
    }
}
