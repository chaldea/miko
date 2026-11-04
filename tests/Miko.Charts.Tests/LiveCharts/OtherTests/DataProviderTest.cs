// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class DataProviderTest
{
    public DataProviderTest() => CoreTestsSetup.Ensure();

    [Fact]
    public void Fetch()
    {
        static void testType<T>(T[] values, Action<ChartPoint[]>? test = null)
        {
            var sutSeries = new ColumnSeries<T>
            {
                Values = values
            };

            var chart = new InMemoryCartesianChart
            {
                Width = 100,
                Height = 100,
                View =
                {
                    Series = [sutSeries]
                }
            };

            chart.GetImage().Dispose();

            var datafactory = sutSeries.DataFactory;
            var points = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();

            if (test is not null)
            {
                test(points);
                return;
            }

            for (var i = 0; i < points.Length; i++)
            {
                var point = points[i];
                var c = point.Coordinate;
                (c.SecondaryValue == i && c.PrimaryValue == 1).ShouldBeTrue();
            }
        }

        testType(new short[] { 1, 1, 1, 1, 1 });
        testType([1, 1, 1, 1, 1]);
        testType(new long[] { 1, 1, 1, 1, 1 });
        testType(new float[] { 1, 1, 1, 1, 1 });
        testType(new double[] { 1, 1, 1, 1, 1 });
        testType(new decimal[] { 1, 1, 1, 1, 1 });
        testType(new short?[] { 1, 1, 1, 1, 1 });
        testType(new int?[] { 1, 1, 1, 1, 1 });
        testType(new long?[] { 1, 1, 1, 1, 1 });
        testType(new float?[] { 1, 1, 1, 1, 1 });
        testType(new double?[] { 1, 1, 1, 1, 1 });
        testType(new decimal?[] { 1, 1, 1, 1, 1 });

        testType(new ObservableValue[] { new(1), new(1), new(1), new(1), new(1) });
        testType(new ObservablePoint[] { new(0, 1), new(1, 1), new(2, 1), new(3, 1), new(4, 1) });

        testType(
            new WeightedPoint[] { new(0, 1, 0), new(1, 1, 10), new(2, 1, 20), new(3, 1, 30), new(4, 1, 40) },
            points =>
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    var c = point.Coordinate;
                    (c.SecondaryValue == i && c.PrimaryValue == 1 && c.TertiaryValue == i * 10).ShouldBeTrue();
                }
            });

        var now = DateTime.Now;
        testType(
            new DateTimePoint[] { new(now.AddDays(0), 1), new(now.AddDays(1), 1), new(now.AddDays(2), 1) },
            points =>
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    var c = point.Coordinate;
                    (c.SecondaryValue == now.AddDays(i).Ticks &&
                        c.PrimaryValue == 1).ShouldBeTrue();
                }
            });

        testType(
            new TimeSpanPoint[] {
                new(TimeSpan.FromMilliseconds(0), 1),
                new(TimeSpan.FromMilliseconds(1), 1),
                new(TimeSpan.FromMilliseconds(2), 1)
            },
            points =>
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    var c = point.Coordinate;
                    (c.SecondaryValue == TimeSpan.FromMilliseconds(i).Ticks &&
                        c.PrimaryValue == 1).ShouldBeTrue();
                }
            });

        testType(new ObservablePolarPoint[] { new(0, 1), new(1, 1), new(2, 1), new(3, 1), new(4, 1) });

        testType(
            new FinancialPoint[] {
                new(now.AddDays(0), 1, 2, 3, 4),
                new(now.AddDays(1), 1, 2, 3, 4),
                new(now.AddDays(2), 1, 2, 3, 4)
            },
            points =>
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    var c = point.Coordinate;
                    (c.SecondaryValue == now.AddDays(i).Ticks &&
                        c.PrimaryValue == 1 && c.TertiaryValue == 2 &&
                        c.QuaternaryValue == 3 && c.QuinaryValue == 4).ShouldBeTrue();
                }
            });

        testType(
             new FinancialPoint[] {
                new(now.AddDays(0), 1, 2, 3, 4),
                new(now.AddDays(1), 1, 2, 3, 4),
                new(now.AddDays(2), 1, 2, 3, 4)
             },
             points =>
             {
                 for (var i = 0; i < points.Length; i++)
                 {
                     var point = points[i];
                     var c = point.Coordinate;
                     (c.SecondaryValue == now.AddDays(i).Ticks &&
                         c.PrimaryValue == 1 && c.TertiaryValue == 2 &&
                         c.QuaternaryValue == 3 && c.QuinaryValue == 4).ShouldBeTrue();
                 }
             });

        testType(
            new FinancialPointI[] {
                new(1, 2, 3, 4),
                new(1, 2, 3, 4),
                new(1, 2, 3, 4)
            },
            points =>
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    var c = point.Coordinate;
                    (c.SecondaryValue == i &&
                        c.PrimaryValue == 1 && c.TertiaryValue == 2 &&
                        c.QuaternaryValue == 3 && c.QuinaryValue == 4).ShouldBeTrue();
                }
            });
    }

    [Fact]
    public void FetchCustomType()
    {
        // finally lets test a mapper
        var sutSeries = new ColumnSeries<City>
        {
            Values = [new(1), new(1), new(1), new(1), new(1)],
            Mapping = (city, index) => new(index, city.Population!.Value)
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 100,
            Height = 100,
            View =
            {
                Series = [sutSeries]
            }
        };

        chart.GetImage().Dispose();

        var datafactory = sutSeries.DataFactory;
        var points = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();

        for (var i = 0; i < points.Length; i++)
        {
            var point = points[i];
            var c = point.Coordinate;
            (c.SecondaryValue == i && c.PrimaryValue == 1).ShouldBeTrue();
        }
    }

    [Fact]
    public void FetchCoordinateEmpty()
    {
        // finally lets test a mapper
        var sutSeries = new ColumnSeries<City>
        {
            Values = [new(1), new(null), new(1), new(null), new(1)],
            Mapping = (city, index) =>
                city.Population is null
                    ? Coordinate.Empty
                    : new(index, city.Population.Value)
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 100,
            Height = 100,
            View =
            {
                Series = [sutSeries]
            }
        };

        chart.GetImage().Dispose();

        var datafactory = sutSeries.DataFactory;
        var points = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();

        var emptyCount = 0;
        for (var i = 0; i < points.Length; i++)
        {
            var point = sutSeries.ConvertToTypedChartPoint(points[i]);
            var c = point.Coordinate;

            if (c.IsEmpty)
            {
                (point.Visual is null).ShouldBeTrue();
                emptyCount++;
            }
            else
            {
                (c.SecondaryValue == i && c.PrimaryValue == 1).ShouldBeTrue();
            }
        }

        (emptyCount == 2).ShouldBeTrue();
    }

    [Fact]
    public void FetchNull()
    {
        // finally lets test a mapper
        var sutSeries = new ColumnSeries<City?>
        {
            Values = [new(1), null, new(1), null, new(1)],
            Mapping = (city, index) => new(index, city!.Population!.Value)
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 100,
            Height = 100,
            View =
            {
                Series = [sutSeries]
            }
        };

        chart.GetImage().Dispose();

        var datafactory = sutSeries.DataFactory;
        var points = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();

        var emptyCount = 0;
        for (var i = 0; i < points.Length; i++)
        {
            var point = sutSeries.ConvertToTypedChartPoint(points[i]);
            var c = point.Coordinate;

            if (point.Model is null)
            {
                (point.Visual is null && c.IsEmpty).ShouldBeTrue();
                emptyCount++;
            }
            else
            {
                (c.SecondaryValue == i && c.PrimaryValue == 1).ShouldBeTrue();
            }
        }

        (emptyCount == 2).ShouldBeTrue();
    }

    [Fact]
    public void FetchReturnEqualityOnValueTypes()
    {
        int[] values = [1, 2, 3];

        var sutSeries = new ColumnSeries<int>
        {
            Values = values
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 100,
            Height = 100,
            View =
            {
                FindingStrategy = LiveChartsCore.Measure.FindingStrategy.ExactMatch,
                Series = [sutSeries]
            }
        };

        chart.GetImage().Dispose();

        var datafactory = sutSeries.DataFactory;

        var fetch0 = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();
        var fetch1 = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();

        (fetch0.Length == fetch1.Length).ShouldBeTrue();

        for (var i = 0; i < fetch0.Length; i++)
        {
            (fetch0[i] == fetch1[i]).ShouldBeTrue();
        }
    }

    public class City(double? population)
    {
        public double? Population { get; set; } = population;
    }
}
