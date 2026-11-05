// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using Shouldly;

// Leak detection for series point instances.
//
// The old version of these tests stressed each series with ~150,000 operations and asserted
// on a fuzzy GC.GetTotalMemory() threshold. That caught leaks but was slow (~3 min per test
// method) and imprecise. This version uses WeakReference: create N points, wire them into a
// chart, detach, force GC, assert nothing survives. Deterministic, precise, and runs in
// seconds. See https://github.com/Live-Charts/LiveCharts2/pull/2160 for the benchmarks that
// separately track allocation-per-operation regressions.
[Collection(ChartsCollection.Name)]
public class _MemoryTests
{
    public _MemoryTests() => CoreTestsSetup.Ensure();

    // Small enough to keep the test fast, large enough that a leak is obvious.
    private const int PointCount = 20;

    // --- Cartesian series ------------------------------------------------------------

    [Fact] public void BoxSeries_Releases() => AssertCartesianReleases(() => new BoxSeries<ObservableValue>());
    [Fact] public void CandlesticksSeries_Releases() => AssertCartesianReleases(() => new CandlesticksSeries<ObservableValue>());
    [Fact] public void ColumnSeries_Releases() => AssertCartesianReleases(() => new ColumnSeries<ObservableValue>());
    [Fact] public void HeatSeries_Releases() => AssertCartesianReleases(() => new HeatSeries<ObservableValue>());
    [Fact] public void LineSeries_Releases() => AssertCartesianReleases(() => new LineSeries<ObservableValue>());
    [Fact] public void RowSeries_Releases() => AssertCartesianReleases(() => new RowSeries<ObservableValue>());
    [Fact] public void ScatterSeries_Releases() => AssertCartesianReleases(() => new ScatterSeries<ObservableValue>());
    [Fact] public void StepLineSeries_Releases() => AssertCartesianReleases(() => new StepLineSeries<ObservableValue>());
    [Fact] public void StackedAreaSeries_Releases() => AssertCartesianReleases(() => new StackedAreaSeries<ObservableValue>());
    [Fact] public void StackedColumnSeries_Releases() => AssertCartesianReleases(() => new StackedColumnSeries<ObservableValue>());

    [Fact] public void PieSeries_Releases() => AssertPieReleases();
    [Fact] public void PolarLineSeries_Releases() => AssertPolarReleases();

    // --- Indexed-values path ---------------------------------------------------------

    // Exercises DataFactory.EnumerateIndexedEntities and its _chartIndexEntityMap — the
    // path taken when Values is a collection of a reference type that does NOT implement
    // IChartEntity. The model instances are referenced via ChartPoint.Context.DataSource;
    // the test asserts they are released once Values is detached.
    [Fact]
    public void MappedModel_Releases_OnIndexedPath()
    {
        var series = new LineSeries<MappedModel>
        {
            Mapping = (model, index) => new Coordinate(index, model.Value)
        };
        var chart = new InMemoryCartesianChart
        {
            Width = 300,
            Height = 200,
            View =
            {
                Series = [series]
            }
        };
        var refs = WireAndDetachMapped(series, chart.View);
        AssertAllDead(refs, "mapped");
    }

    private sealed class MappedModel
    {
        public double Value { get; set; }
    }

    private static List<WeakReference> WireAndDetachMapped(
        LineSeries<MappedModel> series, ChartView chart)
    {
        var refs = new List<WeakReference>(PointCount);
        var values = new MappedModel[PointCount];
        for (var i = 0; i < PointCount; i++)
        {
            values[i] = new MappedModel { Value = i + 1 };
            refs.Add(new WeakReference(values[i]));
        }
        series.Values = values;

        _ = ChartDrawing.DrawChart(chart, animated: false);

        series.Values = Array.Empty<MappedModel>();

        _ = ChartDrawing.DrawChart(chart, animated: false);

        return refs;
    }

    // --- Assertion helpers -----------------------------------------------------------

    private static void AssertCartesianReleases(Func<ISeries> factory)
    {
        var series = factory();
        var chart = new InMemoryCartesianChart
        {
            Width = 300,
            Height = 200,
            View =
            {
                Series = [series]
            }
        };
        var refs = WireAndDetach(series, chart.View);
        AssertAllDead(refs, series.Name ?? series.GetType().Name);
    }

    private static void AssertPieReleases()
    {
        var series = new PieSeries<ObservableValue>();
        var chart = new InMemoryPieChart
        {
            Width = 300,
            Height = 200,
            View =
            {
                Series = [series]
            }
        };
        var refs = WireAndDetach(series, chart.View);
        AssertAllDead(refs, "pie");
    }

    private static void AssertPolarReleases()
    {
        var series = new PolarLineSeries<ObservableValue>();
        var chart = new InMemoryPolarChart
        {
            Width = 300,
            Height = 200,
            View =
            {
                Series = [series]
            }
        };
        var refs = WireAndDetach(series, chart.View);
        AssertAllDead(refs, "polar");
    }

    // Create N observables, render once so they become ChartPoints, then replace Values
    // with an empty collection and render again so the cleanup pass runs. Returns weak
    // references that should be dead after the caller forces GC. The local `values`
    // array is on the stack of this method and goes away when the method returns.
    private static List<WeakReference> WireAndDetach(ISeries series, ChartView chart)
    {
        var refs = new List<WeakReference>(PointCount);
        var values = new ObservableValue[PointCount];
        for (var i = 0; i < PointCount; i++)
        {
            values[i] = new ObservableValue(i + 1);
            refs.Add(new WeakReference(values[i]));
        }
        series.Values = values;

        // First draw: points get materialized into the chart's everFetched set.
        _ = ChartDrawing.DrawChart(chart, animated: false);

        // Detach — a fresh empty array so no hidden alias to the original.
        series.Values = Array.Empty<ObservableValue>();

        // Second draw: CollectPoints runs during Invalidate and prunes stale ChartPoints,
        // releasing their entity references.
        _ = ChartDrawing.DrawChart(chart, animated: false);

        return refs;
    }

    private static void AssertAllDead(List<WeakReference> refs, string label)
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        var alive = refs.Count(r => r.IsAlive);
        alive.ShouldBe(0, $"[{label}] {alive}/{refs.Count} point instances were not released after Values were replaced.");
    }
}
