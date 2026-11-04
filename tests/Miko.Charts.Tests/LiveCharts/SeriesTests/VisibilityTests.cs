// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using System.Collections;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.MikoView.Charts;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class VisibilityTests
{
    public VisibilityTests() => CoreTestsSetup.Ensure();

    // Issue #2054: a series declared with IsVisible=false never reached the theme-application
    // loop (which iterated VisibleSeries), leaving its paints null. The legend still listed it
    // (IsVisibleAtLegend defaults to true) and the miniature crashed in SkiaSharp with
    // "Value cannot be null. (Parameter 'paint')" at the first draw.
    [Fact]
    public void InvisibleSeriesWithLegend_DoesNotCrash_Issue2054()
    {
        var cartesian = new InMemoryCartesianChart
        {
            Width = 500,
            Height = 500,
            ExplicitDisposing = true,
            View =
            {
                LegendPosition = LegendPosition.Top,
                Series = [new LineSeries<double> { IsVisible = false }]
            }
        };
        cartesian.GetImage().Dispose();

        var pie = new InMemoryPieChart
        {
            Width = 500,
            Height = 500,
            ExplicitDisposing = true,
            View =
            {
                LegendPosition = LegendPosition.Top,
                Series = [new PieSeries<double> { IsVisible = false, Values = [1] }]
            }
        };
        pie.GetImage().Dispose();

        var polar = new InMemoryPolarChart
        {
            Width = 500,
            Height = 500,
            ExplicitDisposing = true,
            View =
            {
                LegendPosition = LegendPosition.Top,
                Series = [new PolarLineSeries<double> { IsVisible = false }]
            }
        };
        polar.GetImage().Dispose();
    }

    [Fact]
    public void VisibilityChangingTest()
    {
        TestObservablesChanging(new CartesianSut(new BoxSeries<int>(), "box"));
        TestObservablesChanging(new CartesianSut(new ColumnSeries<int>(), "colum"));
        TestObservablesChanging(new CartesianSut(new CandlesticksSeries<int>(), "candle"));
        TestObservablesChanging(new CartesianSut(new HeatSeries<int>(), "heat"));
        TestObservablesChanging(new CartesianSut(new LineSeries<int>(), "line"));
        TestObservablesChanging(new CartesianSut(new RowSeries<int>(), "row"));
        TestObservablesChanging(new CartesianSut(new ScatterSeries<int>(), "scatter"));
        TestObservablesChanging(new CartesianSut(new StepLineSeries<int>(), "step line"));
        TestObservablesChanging(new PieSut(new PieSeries<int>(), "pie"));

        // ToDo: polar series needs to be updated, it is also not passing the Memory test...
        // TestObservablesChanging(new PolarSut(new PolarLineSeries<ObservableValue>(), "polar"));

        // stacked series are irrelevant for this test because they inherit from some type above.
    }

    private static void TestObservablesChanging<T>(ChartSut<T> sut)
        where T : IList
    {
        _ = ChartDrawing.DrawChart(sut.Chart, true);

        sut.Series.IsVisible = true;
        _ = ChartDrawing.DrawChart(sut.Chart, true);

        sut.Series.IsVisible = false;
        _ = ChartDrawing.DrawChart(sut.Chart, true);

        var values = sut.Series.Fetch(sut.Chart.CoreChart);
        var first = values.First();

        (first.Context.Visual is null &&
            first.Context.Label is null).ShouldBeTrue();
    }

    private class CartesianSut(
        ISeries series,
        string name)
            : ChartSut<int[]>(new InMemoryCartesianChart
            {
                Width = 1000,
                Height = 1000,
                View =
                {
                    Series = [series],
                    AnimationsSpeed = TimeSpan.FromMilliseconds(10),
                    EasingFunction = EasingFunctions.Lineal
                }
            }.View,
            series,
            name,
            [1, 2, 3])
    { }

    private class PieSut(
        ISeries series,
        string name)
            : ChartSut<int[]>(new InMemoryPieChart
            {
                Width = 1000,
                Height = 1000,
                View =
                {
                    Series = [series],
                    AnimationsSpeed = TimeSpan.FromMilliseconds(10),
                    EasingFunction = EasingFunctions.Lineal
                }
            }.View,
            series,
            name,
            [1, 2, 3])
    { }

    private class PolarSut(
        ISeries series,
        string name)
            : ChartSut<int[]>(new InMemoryPolarChart
            {
                Width = 1000,
                Height = 1000,
                View =
                {
                    Series = [series],
                    AnimationsSpeed = TimeSpan.FromMilliseconds(10),
                    EasingFunction = EasingFunctions.Lineal
                }
            }.View,
            series,
            name,
            [1, 2, 3])
    { }

    private abstract class ChartSut<T>
        where T : IEnumerable
    {
        public ChartView Chart { get; set; }
        public ISeries Series { get; set; }
        public T Values { get; set; }

        protected ChartSut(
            ChartView chart,
            ISeries series,
            string name,
            T initialValues)
        {
            series.Name = name;
            series.Values = Values = initialValues;
            series.DataLabelsPaint = new SolidColorPaint(TestColors.Black);
            Series = series;
            Chart = chart;
        }
    }
}
