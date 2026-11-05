// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Extensions;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.MikoView.Tooltips;
using Miko.Charts.Tests.LiveCharts.MockedObjects;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class PieSeriesTest : IDisposable
{
    public PieSeriesTest() => CoreTestsSetup.Ensure();

    // ShouldPlaceDataLabel turns the label debug lines on (process-wide) and the original never turned them off;
    // reset them so the pixel tests that run later in this process do not see them.
    public void Dispose() => LabelGeometry.ShowDebugLines = false;

    [Fact]
    public void ShouldScale()
    {
        var values = new double[] { 100, 50, 25, 25 };
        var total = values.Sum();
        var seriesCollection = values.AsPieSeries((value, series) =>
        {
            series.HoverPushout = 50;
        });

        var chart = new InMemoryPieChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = seriesCollection,
            }
        };

        chart.GetImage().Dispose();
        // chart.SaveImage("test.png"); // use this method to see the actual tested image

        var toCompareGuys = seriesCollection.Select(sutSeries =>
        {
            var datafactory = sutSeries.DataFactory;
            var points = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();

            var unit = points.First();
            return sutSeries.ConvertToTypedChartPoint(unit);
        });

        var startAngle = 0f;
        foreach (var sutPoint in toCompareGuys)
        {
            var series = (PieSeries<double>)sutPoint.Context.Series;

            // test the angle of the slice
            (
                Math.Abs(sutPoint.Visual!.SweepAngle / 360f - sutPoint.Model / total) < 0.001).ShouldBeTrue();

            // test the start rotation
            (
                Math.Abs(sutPoint.Visual.StartAngle - startAngle) < 0.001).ShouldBeTrue();

            // test the width and height
            (Math.Abs(sutPoint.Visual.Width - (chart.Width - series.HoverPushout * 2)) < 0.001).ShouldBeTrue();
            (Math.Abs(sutPoint.Visual.Height - (chart.Height - series.HoverPushout * 2)) < 0.001).ShouldBeTrue();

            // test the center
            (Math.Abs(sutPoint.Visual.CenterX - chart.Width * 0.5f) < 0.001).ShouldBeTrue();
            (Math.Abs(sutPoint.Visual.CenterY - chart.Width * 0.5f) < 0.001).ShouldBeTrue();

            startAngle += sutPoint.Visual.SweepAngle;
        }
    }

    [Fact]
    public void ShouldPlaceToolTips()
    {
        var tooltip = new DefaultTooltip { Easing = null! };

        var chart = new InMemoryPieChart
        {
            Width = 300,
            Height = 300,
            ExplicitDisposing = true,
            View =
            {
                Tooltip = tooltip,
                TooltipPosition = TooltipPosition.Auto,
                Series = new double[] { 1, 1, 1, 1 }.AsPieSeries()
            }
        };

        chart.CoreChart._isPointerIn = true;
        chart.CoreChart._isToolTipOpen = true;
        chart.CoreChart._pointerPosition = new(150 + 10, 150 + 10);

        chart.GetImage().Dispose();

        LvcRectangle tp;
        void UpdateTooltipRect()
        {
            var g = tooltip;
            tp = new LvcRectangle(new(g.X, g.Y), tooltip.Measure());
        }

        UpdateTooltipRect();
        (
            tp.X - 150 > 0 &&
            tp.Y + tp.Height - 150 > 0).ShouldBeTrue(
            "Tool tip failed");

        chart.CoreChart._pointerPosition = new(150 - 10, 150 + 10);
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            tp.X - 150 < 0 &&
            tp.Y + tp.Height - 150 > 0).ShouldBeTrue(
            "Tool tip failed");

        chart.CoreChart._pointerPosition = new(150 - 10, 150 - 10);
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            tp.X - 150 < 0 &&
            tp.Y + tp.Height - 150 < 0).ShouldBeTrue(
            "Tool tip failed");

        chart.CoreChart._pointerPosition = new(150 + 10, 150 - 10);
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            tp.X - 150 > 0 &&
            tp.Y + tp.Height - 150 < 0).ShouldBeTrue(
            "Tool tip failed");
    }

    [Fact]
    public void ShouldPlaceDataLabel()
    {
        LabelGeometry.ShowDebugLines = true;

        var vals = new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        var seriesCollection = vals.AsPieSeries<int, DoughnutGeometry, TestLabel>();

        var chart = new InMemoryPieChart
        {
            Width = 500,
            Height = 500,
            View =
            {
                DrawMargin = new Margin(50),
                TooltipPosition = TooltipPosition.Top,
                Series = seriesCollection
            }
        };

        // TEST HIDDEN ===========================================================
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-hidden.png

        var points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        (seriesCollection.All(x => x.DataLabelsPosition == PolarLabelsPosition.Middle)).ShouldBeTrue();
        (points.All(x => x.Label is null)).ShouldBeTrue();

        foreach (var series in seriesCollection)
            series.DataLabelsPaint = new SolidColorPaint
            {
                Color = TestColors.Black,
                FontFamily = "Arial",
                FontWeight = Miko.Common.FontWeight.Bold
            };

        #region normal

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.ChartCenter;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-center.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            (
                Math.Abs(p.Label!.X - 250) < 0.01 &&
                Math.Abs(p.Label!.Y - 250) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Middle;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-middle.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle * 0.5f;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Start;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-start.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.End;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-end.png

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle;
            if (Math.Abs(r - 360) < 0.001) r = 0;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Outer;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-outer.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h > p.Visual!.Width * 0.5f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle * 0.5f;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        #endregion

        #region inner

        foreach (var series in seriesCollection) series.InnerRadius = 100;

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.ChartCenter;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-center-inner.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            (
                Math.Abs(p.Label!.X - 250) < 0.01 &&
                Math.Abs(p.Label!.Y - 250) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Middle;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-middle-inner.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle * 0.5f;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Start;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-start-inner.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.End;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-end-inner.png

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle;
            if (Math.Abs(r - 360) < 0.001) r = 0;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Outer;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-outer-inner.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h > p.Visual!.Width * 0.5f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle * 0.5f;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        #endregion

        #region outer

        foreach (var series in seriesCollection) series.InnerRadius = 0;
        foreach (var series in seriesCollection) series.OuterRadiusOffset = 10;

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.ChartCenter;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-center-outer.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            (
                Math.Abs(p.Label!.X - 250) < 0.01 &&
                Math.Abs(p.Label!.Y - 250) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Middle;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-middle-outer.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle * 0.5f;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Start;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-start-outer.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.End;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-end-outer.png

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle;
            if (Math.Abs(r - 360) < 0.001) r = 0;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Outer;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-outer-outer.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h > p.Visual!.Width * 0.5f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle * 0.5f;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        #endregion

        #region inner and outer

        foreach (var series in seriesCollection) series.InnerRadius = 50;
        foreach (var series in seriesCollection) series.OuterRadiusOffset = 10;

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.ChartCenter;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-center-inner-outer.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            (
                Math.Abs(p.Label!.X - 250) < 0.01 &&
                Math.Abs(p.Label!.Y - 250) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Middle;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-middle-inner-outer.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle * 0.5f;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Start;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-start-inner-outer.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.End;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-end-inner-outer.png

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h < 500 * 0.5f * 0.65f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle;
            if (Math.Abs(r - 360) < 0.001) r = 0;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        foreach (var series in seriesCollection) series.DataLabelsPosition = PolarLabelsPosition.Outer;
        chart.GetImage().Dispose();
        chart.GetImage().Dispose(); // the original also saved _pie-outer-inner-outer.png

        points = seriesCollection.SelectMany(x =>
            x.DataFactory
                .Fetch(x, chart.CoreChart)
                .Select(x.ConvertToTypedChartPoint));

        foreach (var p in points)
        {
            var h = Math.Sqrt(Math.Pow(p.Label!.X - 250, 2) + Math.Pow(p.Label!.Y - 250, 2));
            (h > p.Visual!.Width * 0.5f).ShouldBeTrue();

            var a = Math.Atan2(p.Label!.Y - 250, p.Label!.X - 250) * 180 / Math.PI;
            if (a < 0) a += 360;
            var r = p.Visual!.StartAngle + p.Visual!.SweepAngle * 0.5f;

            (Math.Abs(a - r) < 0.01).ShouldBeTrue();
        }

        #endregion
    }
}
