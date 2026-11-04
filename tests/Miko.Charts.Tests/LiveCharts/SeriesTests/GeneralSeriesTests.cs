// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Extensions;
using LiveChartsCore.MikoView.Charts;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class GeneralSeriesTests
{
    public GeneralSeriesTests() => CoreTestsSetup.Ensure();

    [Fact]
    public void MustHandleDataChanges()
    {
        RunCartesianTestFor(new ColumnSeries<int> { Name = "column" });
        RunCartesianTestFor(new CandlesticksSeries<int> { Name = "candle" });
        RunCartesianTestFor(new HeatSeries<int> { Name = "heat" });
        RunCartesianTestFor(new LineSeries<int> { Name = "line" });
        RunPieTest();
        RunPolarTestFor(new PolarLineSeries<int> { Name = "polar line" });
        RunCartesianTestFor(new ScatterSeries<int> { Name = "scatter" });
        RunCartesianTestFor(new StackedAreaSeries<int> { Name = "stacked area" });
        RunCartesianTestFor(new StackedColumnSeries<int> { Name = "stacked column" });
        RunCartesianHorzontalTestFor(new StackedRowSeries<int> { Name = "stacked row" });
        RunCartesianTestFor(new StackedStepAreaSeries<int> { Name = "stacked step area" });
        RunCartesianTestFor(new StepLineSeries<int> { Name = "step line" });
    }

    private void RunCartesianTestFor<TVisual, TLabel>(
        Series<int, TVisual, TLabel> series)
            where TVisual : DrawnGeometry, new()
            where TLabel : BaseLabelGeometry, new()
    {
        var values = new List<int> { 1 };
        series.Values = values;

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new[] { series },
                YAxes = new[] { new Axis { MinLimit = 0, MaxLimit = 10 } }
            }
        };

        chart.GetImage().Dispose();
        (series.everFetched.Count == 1).ShouldBeTrue($"Basic case for {series.Name}");

        values.Add(2);
        chart.GetImage().Dispose();
        var totalY = series.everFetched.Sum(x => series.ConvertToTypedChartPoint(x).Visual!.Y);
        (series.everFetched.Count == 2).ShouldBeTrue($"Add case for {series.Name}");

        values[1] = 3;
        chart.GetImage().Dispose();
        // there are still 2 elements, but the shapes must have different position in the canvas
        // the size is not tested here, there is another test for that
        var newTotalY = series.everFetched.Sum(x => series.ConvertToTypedChartPoint(x).Visual!.Y);
        (series.everFetched.Count == 2 && totalY != newTotalY).ShouldBeTrue($"Replace case for {series.Name}");

        values.RemoveAt(1);
        chart.GetImage().Dispose();
        (series.everFetched.Count == 1).ShouldBeTrue($"Delete case for {series.Name}");
    }

    private void RunPolarTestFor<TVisual, TLabel>(
        Series<int, TVisual, TLabel> series)
            where TVisual : DrawnGeometry, new()
            where TLabel : BaseLabelGeometry, new()
    {
        var values = new List<int> { 1 };
        series.Values = values;

        var chart = new InMemoryPolarChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new[] { series },
                RadiusAxes = new[] { new PolarAxis { MinLimit = 0, MaxLimit = 10 } }
            }
        };

        chart.GetImage().Dispose();
        (series.everFetched.Count == 1).ShouldBeTrue($"Basic case for {series.Name}");

        values.Add(2);
        chart.GetImage().Dispose();
        var totalY = series.everFetched.Sum(x => series.ConvertToTypedChartPoint(x).Visual!.Y);
        (series.everFetched.Count == 2).ShouldBeTrue($"Add case for {series.Name}");

        values[1] = 3;
        chart.GetImage().Dispose();
        // there are still 2 elements, but the shapes must have different position in the canvas
        // the size is not tested here, there is another test for that
        var newTotalY = series.everFetched.Sum(x => series.ConvertToTypedChartPoint(x).Visual!.Y);
        (series.everFetched.Count == 2 && totalY != newTotalY).ShouldBeTrue($"Replace case for {series.Name}");

        values.RemoveAt(1);
        chart.GetImage().Dispose();
        (series.everFetched.Count == 1).ShouldBeTrue($"Delete case for {series.Name}");
    }

    private void RunCartesianHorzontalTestFor<TVisual, TLabel>(
        Series<int, TVisual, TLabel> series)
            where TVisual : DrawnGeometry, new()
            where TLabel : BaseLabelGeometry, new()
    {
        var values = new List<int> { 1 };
        series.Values = values;

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new[] { series },
                YAxes = new[] { new Axis { MinLimit = 0, MaxLimit = 10 } }
            }
        };

        chart.GetImage().Dispose();
        (series.everFetched.Count == 1).ShouldBeTrue($"Basic case for {series.Name}");

        values.Add(2);
        chart.GetImage().Dispose();
        var totalX = series.everFetched.Sum(x => series.ConvertToTypedChartPoint(x).Visual!.X);
        (series.everFetched.Count == 2).ShouldBeTrue($"Add case for {series.Name}");

        values[1] = 3;
        chart.GetImage().Dispose();
        // was chart.SaveImage($"{series.Name} 3.png"): the same draw, without writing a file into the test directory
        chart.GetImage().Dispose();
        // there are still 2 elements, but the shapes must have different position in the canvas
        // the size is not tested here, there is another test for that
        var newTotalX = series.everFetched.Sum(x => series.ConvertToTypedChartPoint(x).Visual!.X);
        (series.everFetched.Count == 2 && totalX != newTotalX).ShouldBeTrue($"Replace case for {series.Name}");

        values.RemoveAt(1);
        chart.GetImage().Dispose();
        (series.everFetched.Count == 1).ShouldBeTrue($"Delete case for {series.Name}");
    }

    private void RunPieTest()
    {
        var seriesCollection = new ObservableCollection<ISeries>(new[] { 1, 2, 3 }.AsPieSeries());

        var chart = new InMemoryPieChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = seriesCollection
            }
        };

        int getSum() => seriesCollection.Sum(x => ((PieSeries<int>)x).everFetched.Count);

        chart.GetImage().Dispose();
        (getSum() == 3).ShouldBeTrue($"Basic case for pie");

        seriesCollection.Add(new PieSeries<int> { Values = new[] { 2 } });
        chart.GetImage().Dispose();
        (getSum() == 4).ShouldBeTrue($"Basic case for pie");

        seriesCollection.RemoveAt(1);
        chart.GetImage().Dispose();
        (getSum() == 3).ShouldBeTrue($"Delete case for pie");
    }
}
