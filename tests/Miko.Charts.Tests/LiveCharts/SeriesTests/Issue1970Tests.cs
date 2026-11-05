// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using Shouldly;

// Regression coverage for https://github.com/Live-Charts/LiveCharts2/issues/1970.
//
// The reporter saw old markers/geometries piling up after a real-time data update.
// Both reported flavors are exercised here:
//   (a) Series.Values reassigned to a fresh, shorter collection.
//   (b) ObservableCollection.Clear() + Add() shrinking the bound collection in place.
// And both reported series types (Line + Scatter).
//
// Each test asserts two things after the shrink:
//   1. Series.everFetched.Count equals the new dataset size — points rendered last
//      frame but no longer in Values must be soft-deleted and pruned from the set.
//      This is the contract enforced by ChartPointCleanupContext.CollectPoints,
//      invoked at the end of every Core{Line,Scatter}Series.Measure pass.
//   2. CoreCanvas.CountGeometries() equals what a freshly-built chart with the
//      smaller dataset produces — i.e. no zombie shapes on the canvas, which is
//      what the user actually sees.
[Collection(ChartsCollection.Name)]
public class Issue1970Tests
{
    public Issue1970Tests() => CoreTestsSetup.Ensure();

    private const int InitialCount = 20;
    private const int UpdatedCount = 5;

    [Fact]
    public void LineSeries_ValuesReassignedToSmallerArray_PrunesOldPoints()
    {
        var sutSeries = new LineSeries<ObservableValue> { Values = MakeCollection(InitialCount) };
        var sutChart = NewChart(sutSeries);

        _ = ChartDrawing.DrawChart(sutChart);
        sutSeries.everFetched.Count.ShouldBe(InitialCount, "first draw should fetch all initial points");

        sutSeries.Values = MakeCollection(UpdatedCount);
        _ = ChartDrawing.DrawChart(sutChart);

        sutSeries.everFetched.Count.ShouldBe(UpdatedCount, "after Values reassignment, stale ChartPoints must be pruned from everFetched");

        var referenceChart = NewChart(new LineSeries<ObservableValue> { Values = MakeCollection(UpdatedCount) });
        _ = ChartDrawing.DrawChart(referenceChart);

        sutChart.CoreCanvas.CountGeometries().ShouldBe(referenceChart.CoreCanvas.CountGeometries(), "after the shrink, the canvas should hold the same number of geometries as a chart " +
            "freshly built with the smaller dataset (no zombie shapes left over)");
    }

    [Fact]
    public void LineSeries_ObservableCollectionClearedAndRepopulatedSmaller_PrunesOldPoints()
    {
        var values = MakeCollection(InitialCount);
        var sutSeries = new LineSeries<ObservableValue> { Values = values };
        var sutChart = NewChart(sutSeries);

        _ = ChartDrawing.DrawChart(sutChart);
        sutSeries.everFetched.Count.ShouldBe(InitialCount);

        // Exact pattern from the issue body: mutate the same bound collection in place.
        values.Clear();
        for (var i = 0; i < UpdatedCount; i++) values.Add(new ObservableValue(i + 1));
        _ = ChartDrawing.DrawChart(sutChart);

        sutSeries.everFetched.Count.ShouldBe(UpdatedCount, "after Clear()+Add() on the bound ObservableCollection, stale ChartPoints must be pruned");

        var referenceChart = NewChart(new LineSeries<ObservableValue> { Values = MakeCollection(UpdatedCount) });
        _ = ChartDrawing.DrawChart(referenceChart);

        sutChart.CoreCanvas.CountGeometries().ShouldBe(referenceChart.CoreCanvas.CountGeometries(), "no zombie shapes should remain on the canvas after the in-place shrink");
    }

    [Fact]
    public void ScatterSeries_ValuesReassignedToSmallerArray_PrunesOldPoints()
    {
        var sutSeries = new ScatterSeries<ObservableValue> { Values = MakeCollection(InitialCount) };
        var sutChart = NewChart(sutSeries);

        _ = ChartDrawing.DrawChart(sutChart);
        sutSeries.everFetched.Count.ShouldBe(InitialCount);

        sutSeries.Values = MakeCollection(UpdatedCount);
        _ = ChartDrawing.DrawChart(sutChart);

        sutSeries.everFetched.Count.ShouldBe(UpdatedCount, "ScatterSeries: after Values reassignment, stale ChartPoints must be pruned");

        var referenceChart = NewChart(new ScatterSeries<ObservableValue> { Values = MakeCollection(UpdatedCount) });
        _ = ChartDrawing.DrawChart(referenceChart);

        sutChart.CoreCanvas.CountGeometries().ShouldBe(referenceChart.CoreCanvas.CountGeometries(), "ScatterSeries: no zombie shapes should remain on the canvas after the shrink");
    }

    [Fact]
    public void ScatterSeries_ObservableCollectionClearedAndRepopulatedSmaller_PrunesOldPoints()
    {
        var values = MakeCollection(InitialCount);
        var sutSeries = new ScatterSeries<ObservableValue> { Values = values };
        var sutChart = NewChart(sutSeries);

        _ = ChartDrawing.DrawChart(sutChart);
        sutSeries.everFetched.Count.ShouldBe(InitialCount);

        values.Clear();
        for (var i = 0; i < UpdatedCount; i++) values.Add(new ObservableValue(i + 1));
        _ = ChartDrawing.DrawChart(sutChart);

        sutSeries.everFetched.Count.ShouldBe(UpdatedCount, "ScatterSeries: after Clear()+Add() on the bound ObservableCollection, stale ChartPoints must be pruned");

        var referenceChart = NewChart(new ScatterSeries<ObservableValue> { Values = MakeCollection(UpdatedCount) });
        _ = ChartDrawing.DrawChart(referenceChart);

        sutChart.CoreCanvas.CountGeometries().ShouldBe(referenceChart.CoreCanvas.CountGeometries(), "ScatterSeries: no zombie shapes should remain on the canvas after the in-place shrink");
    }

    private static ObservableCollection<ObservableValue> MakeCollection(int count)
    {
        var values = new ObservableCollection<ObservableValue>();
        for (var i = 0; i < count; i++) values.Add(new ObservableValue(i + 1));
        return values;
    }

    private static InMemoryCartesianChart NewChart(ISeries series) => new()
    {
        View = { Series = [series] },
        Width = 300,
        Height = 200
    };
}
