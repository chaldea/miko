// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Drawing.Segments;
using LiveChartsCore.Kernel;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.MikoView.Tooltips;
using LiveChartsCore.Themes;
using Miko.Charts.Tests.LiveCharts.MockedObjects;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class LineSeriesTest
{
    public LineSeriesTest() => CoreTestsSetup.Ensure();

    [Fact]
    public void ShouldScale()
    {
        var sutSeries = new LineSeries<double>
        {
            Values = [1, 2, 4, 8, 16, 32, 64, 128, 256, 512],
            GeometrySize = 10
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = [sutSeries],
                XAxes = [new Axis { MinLimit = -1, MaxLimit = 10 }],
                YAxes = [new Axis { MinLimit = 0, MaxLimit = 512 }]
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
        (typedUnit.Visual!.Width == 10 && typedUnit.Visual.Height == 10).ShouldBeTrue();

        var previous = typedUnit;
        float? previousX = null;
        float? previousXArea = null;

        foreach (var sutPoint in toCompareGuys)
        {
            // test x
            var currentDeltaX = previous.Visual!.X - sutPoint.Visual!.X;

            var previousBezier = ((CubicSegmentVisualPoint?)previous.Context.AdditionalVisuals)?.Segment;
            var sutBezier = ((CubicSegmentVisualPoint)sutPoint.Context.AdditionalVisuals!).Segment;

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
            var p = 1f - sutPoint.Coordinate.PrimaryValue / 512f;
            (Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutPoint.Visual.Y + chart.CoreChart.DrawMarginLocation.Y) < 0.001)
                .ShouldBeTrue();
            (Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutBezier.Yj + chart.CoreChart.DrawMarginLocation.Y) < 0.001)
                .ShouldBeTrue();

            previousX = previous.Visual.X - sutPoint.Visual.X;
            previousXArea = previousBezier.Xj - sutBezier.Xj;
            previous = sutPoint;
        }
    }

    [Fact]
    public void ShouldPlaceToolTips()
    {
        var sutSeries = new LineSeries<double>
        {
            GeometrySize = 0,
            Values = [1, 2, 3, 4, 5],
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
                Series = [sutSeries],
                XAxes = [new Axis { IsVisible = false }],
                YAxes = [new Axis { IsVisible = false }],
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

        sutSeries.Values = [-1, -2, -3, -4, -5];
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X + tp.Width * 0.5f - 150) < 0.1 &&
            Math.Abs(tp.Y - 150) < 0.1 &&
            chart.CoreChart.AutoToolTipsInfo.ToolTipPlacement == PopUpPlacement.Bottom).ShouldBeTrue(
            "Tool tip on bottom failed [AUTO]");

        sutSeries.Values = [1, 2, 3, 4, 5];
        chart.CoreChart._pointerPosition = new(299, 150);
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            // that 2... it seems that the lineseries.DataPadding takes more space than expected
            Math.Abs(tp.X - (300 - tp.Width)) < 2 &&
            //Math.Abs(tp.Y - -tp.Height * 0.5f) < 2 &&
            chart.CoreChart.AutoToolTipsInfo.ToolTipPlacement == PopUpPlacement.Left).ShouldBeTrue(
            "Tool tip on left failed [AUTO]");

        chart.CoreChart._pointerPosition = new(1, 150);
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X) < 2 &&
            //Math.Abs(tp.Y - (300 - tp.Height * 0.5f)) < 2 &&
            chart.CoreChart.AutoToolTipsInfo.ToolTipPlacement == PopUpPlacement.Right).ShouldBeTrue(
            "Tool tip on left failed [AUTO]");
    }

    [Fact]
    public void ShouldPlaceDataLabel()
    {
        var gs = 5f;
        var sutSeries = new LineSeries<double, RectangleGeometry, TestLabel>
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

        (!chart.CoreCanvas.ContainsPaintTask(previousPaint)).ShouldBeTrue();
    }

    [Fact]
    public void ShouldScaleBeziersOnAddRemoveOrInsert()
    {
        var values = new List<double> { 1, 2, 3, 4, 5 };

        var series = new LineSeries<double>
        {
            Values = values,
            GeometrySize = 10
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = [series]
            }
        };

        void AssertIsStraightLine(ChartPoint[] points)
        {
            (points.Length > 0).ShouldBeTrue();

            CubicBezierSegment? previous = null;
            var slope = 0f;

            foreach (var p in points)
            {
                var tp = series.ConvertToTypedChartPoint(p);
                var segment = (CubicSegmentVisualPoint?)tp.Context.AdditionalVisuals;

                segment.ShouldNotBeNull();

                if (previous is null)
                {
                    previous = segment?.Segment;
                    continue;
                }

                if (slope == 0f)
                {
                    var dx = segment!.Segment.Xj - previous.Xj;
                    var dy = segment.Segment.Yj - previous.Yj;
                    slope = dx / dy;
                }

                var currentSlope = (segment!.Segment.Xj - previous.Xj) / (segment.Segment.Yj - previous.Yj);

                (Math.Abs(slope - currentSlope) < 0.1).ShouldBeTrue();
                previous = segment.Segment;
            }
        }

        ChartPoint[] points;
        int segments;
        chart.GetImage().Dispose();

        points = [.. series.DataFactory.Fetch(series, chart.CoreChart)];
        segments = series._fillPathHelperDictionary.Sum(x => x.Value.Sum(y => y.Commands.Count));
        AssertIsStraightLine(points);
        (segments == 5).ShouldBeTrue();

        // if a the first point is removed, it should still be a straight line
        values.RemoveAt(0);
        chart.GetImage().Dispose();

        points = [.. series.DataFactory.Fetch(series, chart.CoreChart)];
        segments = series._fillPathHelperDictionary.Sum(x => x.Value.Sum(y => y.Commands.Count));
        AssertIsStraightLine(points);
        (segments == 4).ShouldBeTrue();

        // if we insert a point at the start that keeps the straight line, it should be a straight line
        values.Insert(0, 1);
        chart.GetImage().Dispose();

        points = [.. series.DataFactory.Fetch(series, chart.CoreChart)];
        segments = series._fillPathHelperDictionary.Sum(x => x.Value.Sum(y => y.Commands.Count));
        AssertIsStraightLine(points);
        (segments == 5).ShouldBeTrue();

        // if the last point is removed, it should still be a straight line
        values.RemoveAt(values.Count - 1);
        chart.GetImage().Dispose();

        points = [.. series.DataFactory.Fetch(series, chart.CoreChart)];
        segments = series._fillPathHelperDictionary.Sum(x => x.Value.Sum(y => y.Commands.Count));
        AssertIsStraightLine(points);
        (segments == 4).ShouldBeTrue();

        // if we add a point that keeps the straight line, it should be a straight line
        values.Add(5);
        chart.GetImage().Dispose();

        points = [.. series.DataFactory.Fetch(series, chart.CoreChart)];
        segments = series._fillPathHelperDictionary.Sum(x => x.Value.Sum(y => y.Commands.Count));
        AssertIsStraightLine(points);
        (segments == 5).ShouldBeTrue();

        // if we insert a point in the middle that keeps the straight line, it should be a straight line
        values.RemoveAt(1);
        chart.GetImage().Dispose();
        // at this point the values are [1,3,4,5] it is not a straight line
        //AssertIsStraightLine([.. series.DataFactory.Fetch(series, chart.CoreChart)]);

        values.Insert(1, 2);
        chart.GetImage().Dispose();

        points = [.. series.DataFactory.Fetch(series, chart.CoreChart)];
        segments = series._fillPathHelperDictionary.Sum(x => x.Value.Sum(y => y.Commands.Count));
        AssertIsStraightLine(points);
        (segments == 5).ShouldBeTrue();
    }

    [Fact]
    public void MarkerColorMatchesUserStroke()
    {
        // Regression for #2064: when the user sets Stroke explicitly, the legend
        // miniature and per-point marker should match the line color. The default
        // theme used to rotate GeometryStroke from the palette by SeriesId, so a
        // user-set blue/green/red line ended up with palette-rotated marker dots,
        // giving each series two different identifying colors.

        var blue = new LvcColor(0, 0, 255);
        var green = new LvcColor(0, 128, 0);
        var red = new LvcColor(255, 0, 0);

        var s0 = new LineSeries<int> { Values = [1, 2, 3], Stroke = new SolidColorPaint(blue) };
        var s1 = new LineSeries<int> { Values = [1, 2, 3], Stroke = new SolidColorPaint(green) };
        var s2 = new LineSeries<int> { Values = [1, 2, 3], Stroke = new SolidColorPaint(red) };

        var chart = new InMemoryCartesianChart
        {
            Width = 400,
            Height = 400,
            View =
            {
                Series = [s0, s1, s2]
            }
        };
        chart.GetImage().Dispose();

        ((SolidColorPaint)s0.GeometryStroke!).Color.ShouldBe(blue);
        ((SolidColorPaint)s1.GeometryStroke!).Color.ShouldBe(green);
        ((SolidColorPaint)s2.GeometryStroke!).Color.ShouldBe(red);
    }

    [Fact]
    public void MarkerColorFollowsActiveThemeAfterSwitch()
    {
        // Regression for PR #2236 review: when the theme is re-applied after a
        // dark/light switch the marker color must follow the new palette, not be
        // stuck on the previous theme's palette color that was already written
        // into Stroke. An earlier draft used `Stroke is SolidColorPaint` as a
        // heuristic for "user-set", which on the second apply captured the
        // previous theme's palette color and froze it into GeometryStroke.

        var theme = LiveCharts.DefaultSettings.GetTheme();
        var originalColors = theme.Colors;

        var series = new LineSeries<int> { Values = [1, 2, 3] };
        ((ISeries)series).SeriesId = 0;
        var ce = ((IChartElement)series).ChartElementSource;

        ce._isInternalSet = true;
        try
        {
            // 1st apply with the light palette.
            theme.Colors = ColorPalletes.MaterialDesign500;
            theme.ApplyStyleToSeries(series);
            var lightMarker = ((SolidColorPaint)series.GeometryStroke!).Color;

            // Swap palette and re-apply (the engine does this on theme switch).
            // Stroke is theme-set (not in _userSets) so it must be rewritten to
            // the new palette color before GeometryStroke derives from it.
            theme.Colors = ColorPalletes.MaterialDesign200;
            theme.ApplyStyleToSeries(series);
            var darkMarker = ((SolidColorPaint)series.GeometryStroke!).Color;

            lightMarker.ShouldBe(new LvcColor(33, 150, 243));    // MD500[0]
            darkMarker.ShouldBe(new LvcColor(144, 202, 249));    // MD200[0]
        }
        finally
        {
            ce._isInternalSet = false;
            theme.Colors = originalColors;
        }
    }
}
