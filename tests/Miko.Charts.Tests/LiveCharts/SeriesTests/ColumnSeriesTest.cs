// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.MikoView.Tooltips;
using Miko.Charts.Tests.LiveCharts.MockedObjects;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class ColumnSeriesTest
{
    public ColumnSeriesTest() => CoreTestsSetup.Ensure();

    [Fact]
    public void ShouldScale()
    {
        var sutSeries = new ColumnSeries<double>
        {
            Values = new double[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 }
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new[] { sutSeries },
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
            var p = 1f - sutPoint.Coordinate.PrimaryValue / 512f;
            (Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutPoint.Visual.Y + chart.CoreChart.DrawMarginLocation.Y) < 0.001)
                .ShouldBeTrue();

            previousX = previous.Visual.X - sutPoint.Visual.X;
            previous = sutPoint;
        }
    }

    [Fact]
    public void ShouldPlaceToolTips()
    {
        var sutSeries = new ColumnSeries<double>
        {
            Values = new double[] { 1, 2, 3, 4, 5 },
            Name = "Series #1",
            DataPadding = new LvcPoint(0, 0)
        };

        var tooltip = new DefaultTooltip { Easing = null! };

        var chart = new InMemoryCartesianChart
        {
            Width = 300,
            Height = 300,
            View =
            {
                Tooltip = tooltip,
                TooltipPosition = TooltipPosition.Top,
                // Miko port: at the default 16px, Miko's "Arial, sans-serif" lays the tooltip out 150.49px wide
                // (Skia's default typeface: under 150px), so it no longer fits the 150px left of the pointer and
                // LiveCharts flips it to the right. 15px keeps the geometry the assertions were written for.
                TooltipTextSize = 15,
                Series = new[] { sutSeries },
                XAxes = new[] { new Axis { IsVisible = false } },
                YAxes = new[] { new Axis { IsVisible = false } },
            },
            ExplicitDisposing = true
        };

        chart.CoreChart._isPointerIn = true;
        chart.CoreChart._isToolTipOpen = true;
        chart.CoreChart._pointerPosition = new(150, 150);

        chart.View.TooltipPosition = TooltipPosition.Top;
        chart.GetImage().Dispose();

        LvcRectangle tp = default;
        void UpdateTooltipRect()
        {
            var g = tooltip;
            tp = new LvcRectangle(new(g.X, g.Y), tooltip.Measure());
        }

        UpdateTooltipRect();
        (
            Math.Abs(tp.X + tp.Width * 0.5f - 150) < 0.1 &&
            Math.Abs(tp.Y - (150 - tp.Height)) < 0.1).ShouldBeTrue(
            "Tool tip on top failed");

        chart.View.TooltipPosition = TooltipPosition.Bottom;
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X + tp.Width * 0.5f - 150) < 0.1 &&
            Math.Abs(tp.Y - 150) < 0.1).ShouldBeTrue(
            "Tool tip on bottom failed");

        chart.View.TooltipPosition = TooltipPosition.Left;
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X - (150 - tp.Width)) < 0.1 &&
            Math.Abs(tp.Y + tp.Height * 0.5f - 150) < 0.1).ShouldBeTrue(
            "Tool tip on left failed");

        chart.View.TooltipPosition = TooltipPosition.Right;
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X - 150) < 0.1 &&
            Math.Abs(tp.Y + tp.Height * 0.5f - 150) < 0.1).ShouldBeTrue(
            "Tool tip on right failed");

        chart.View.TooltipPosition = TooltipPosition.Center;
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X + tp.Width * 0.5f - 150) < 0.1 &&
            Math.Abs(tp.Y + tp.Height * 0.5f - 150) < 0.1).ShouldBeTrue(
            "Tool tip on center failed");

        chart.View.TooltipPosition = TooltipPosition.Auto;
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X + tp.Width * 0.5f - 150) < 0.1 &&
            Math.Abs(tp.Y - (150 - tp.Height)) < 0.1 &&
            chart.CoreChart.AutoToolTipsInfo.ToolTipPlacement == PopUpPlacement.Top).ShouldBeTrue(
            "Tool tip on top failed [AUTO]");

        sutSeries.Values = new double[] { -1, -2, -3, -4, -5 };
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X + tp.Width * 0.5f - 150) < 0.1 &&
            Math.Abs(tp.Y - 150) < 0.1 &&
            chart.CoreChart.AutoToolTipsInfo.ToolTipPlacement == PopUpPlacement.Bottom).ShouldBeTrue(
            "Tool tip on bottom failed [AUTO]");

        sutSeries.Values = new double[] { 1, 2, 3, 4, 5 };
        chart.CoreChart._pointerPosition = new(299, 150);
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X - (300 - 300 * (1 / 5d) * 0.5 - tp.Width)) < 0.0001 &&
            //Math.Abs(tp.Y - -tp.Height * 0.5f) < 0.1 &&
            chart.CoreChart.AutoToolTipsInfo.ToolTipPlacement == PopUpPlacement.Left).ShouldBeTrue(
            "Tool tip on left failed [AUTO]");

        chart.CoreChart._pointerPosition = new(1, 150);
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X - 300 * (1 / 5d) * 0.5) < 0.0001 &&
            //Math.Abs(tp.Y - (300 - tp.Height * 0.5f)) < 0.1 &&
            chart.CoreChart.AutoToolTipsInfo.ToolTipPlacement == PopUpPlacement.Right).ShouldBeTrue(
            "Tool tip on right failed [AUTO]");
    }

    [Fact]
    public void ShouldPlaceDataLabel()
    {
        var sutSeries = new ColumnSeries<double, RectangleGeometry, TestLabel>
        {
            Values = new double[] { -10, -5, -1, 0, 1, 5, 10 },
            DataPadding = new LvcPoint(0, 0),
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

        (!chart.CoreCanvas.ContainsPaintTask(previousPaint)).ShouldBeTrue();
    }
}
