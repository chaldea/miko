// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.LayoutTests;

using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Drawing.Layouts;
using LiveChartsCore.MikoView.Painting;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class StackLayoutTest
{
    public StackLayoutTest() => CoreTestsSetup.Ensure();

    [Fact]
    public void StackLayoutHorizontalStart()
    {
        var stackPanel = new StackLayout
        {
            Padding = new Padding(10, 20),
            X = 100,
            Y = 200,
            VerticalAlignment = Align.Start,
            Orientation = ContainerOrientation.Horizontal
        };

        RectangleGeometry g1, g2;

        stackPanel.Children = [
            g1 = new RectangleGeometry
            {
                Width = 15,
                Height = 25,
                Fill = new SolidColorPaint(TestColors.Red)
            },
            g2 = new RectangleGeometry
            {
                Width = 15,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = [
                    new LineSeries<double>
                    {
                        Values = [1, 2, 3]
                    }
                ]
            }
        };

        chart.CoreCanvas.AddGeometry(stackPanel);

        chart.GetImage().Dispose();

        var panelSize = stackPanel.Measure();

        (
            // 100 left + 10 padding
            g1.X == 100 + 10 &&

            // 200 top + 20 padding
            g1.Y == 200 + 20 &&

            // 100 left + 10 padding + 15 previous width
            g2.X == 100 + 10 + 15 &&

            // preserve the previous y...
            g2.Y == 200 + 20 &&

            // 10 padding + 15 first shape + 15 second shape + 10 padding
            panelSize.Width == 10 + 15 + 15 + 10 &&
            panelSize.Height == 20 + 25 + 20).ShouldBeTrue();
    }

    [Fact]
    public void StackLayoutHorizontalMiddle()
    {
        var stackLayout = new StackLayout
        {
            Padding = new Padding(10, 20),
            X = 100,
            Y = 200,
            // also default should be middle...
            //VerticalAlignment = Drawing.Align.Middle,
            Orientation = ContainerOrientation.Horizontal
        };

        RectangleGeometry g1, g2;

        stackLayout.Children = [
            g1 = new RectangleGeometry
            {
                Width = 15,
                Height = 25,
                Fill = new SolidColorPaint(TestColors.Red)
            },
            g2 = new RectangleGeometry
            {
                Width = 15,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = [
                    new LineSeries<double>
                    {
                        Values = [1, 2, 3]
                    }
                ]
            }
        };

        chart.CoreCanvas.AddGeometry(stackLayout);
        chart.GetImage().Dispose();

        (
            // 100 left + 10 padding
            g1.X == 100 + 10 &&

            // 200 top + 20 padding
            g1.Y == 200 + 20 &&

            // 100 left + 10 padding + 15 previous width
            g2.X == 100 + 10 + 15 &&

            // preserve the previous y... + half of the difference between the heights
            g2.Y == 200 + 20 + 5).ShouldBeTrue();
    }

    [Fact]
    public void StackLayoutHorizontalEnd()
    {
        var stackLayout = new StackLayout
        {
            Padding = new Padding(10, 20),
            X = 100,
            Y = 200,
            VerticalAlignment = Align.End,
            Orientation = ContainerOrientation.Horizontal
        };

        RectangleGeometry g1, g2;

        stackLayout.Children = [
            g1 = new RectangleGeometry
            {
                Width = 15,
                Height = 25,
                Fill = new SolidColorPaint(TestColors.Red)
            },
            g2 = new RectangleGeometry
            {
                Width = 15,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series =
                [
                    new LineSeries<double>
                    {
                        Values = [1, 2, 3]
                    }
                ]
            }
        };

        chart.CoreCanvas.AddGeometry(stackLayout);
        chart.GetImage().Dispose();

        (
            // 100 left + 10 padding
            g1.X == 100 + 10 &&

            // 200 top + 20 padding
            g1.Y == 200 + 20 &&

            // 100 left + 10 padding + 15 previous width
            g2.X == 100 + 10 + 15 &&

            // preserve the previous y... + difference between the heights
            g2.Y == 200 + 20 + 10).ShouldBeTrue();
    }

    [Fact]
    public void StackLayoutVerticalStart()
    {
        var stackPanel = new StackLayout
        {
            Padding = new Padding(10, 20),
            X = 100,
            Y = 200,
            HorizontalAlignment = Align.Start,
            Orientation = ContainerOrientation.Vertical
        };

        RectangleGeometry g1, g2;

        stackPanel.Children = [
            g1 = new RectangleGeometry
            {
                Width = 10,
                Height = 25,
                Fill = new SolidColorPaint(TestColors.Red)
            },
            g2 = new RectangleGeometry
            {
                Width = 25,
                Height = 25,
                Fill = new SolidColorPaint(TestColors.Blue)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] { 1, 2, 3 }
                    }
                }
            }
        };

        chart.CoreCanvas.AddGeometry(stackPanel);
        chart.GetImage().Dispose();

        var panelSize = stackPanel.Measure();

        (
            // 100 left + 10 padding 
            g1.X == 100 + 10 &&

            // 200 top + 20 padding
            g1.Y == 200 + 20 &&

            // preserve the previous x...
            g2.X == 110 &&

            // 200 top + 20 padding + 25 previous height
            g2.Y == 200 + 20 + 25 &&

            panelSize.Width == 10 + 25 + 10 &&
            panelSize.Height == 20 + 25 + 25 + 20).ShouldBeTrue();
    }

    [Fact]
    public void StackLayoutVerticalMiddle()
    {
        var stackPanel = new StackLayout
        {
            Padding = new Padding(10, 20),
            X = 100,
            Y = 200,
            // also default should be middle...
            //HorizontalAlignment = Drawing.Align.Middle,
            Orientation = ContainerOrientation.Vertical
        };

        RectangleGeometry g1, g2;

        stackPanel.Children = [
            g1 = new RectangleGeometry
            {
                Width = 10,
                Height = 25,
                Fill = new SolidColorPaint(TestColors.Red)
            },
            g2 = new RectangleGeometry
            {
                Width = 25,
                Height = 25,
                Fill = new SolidColorPaint(TestColors.Blue)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] { 1, 2, 3 }
                    }
                }
            }
        };

        chart.CoreCanvas.AddGeometry(stackPanel);
        chart.GetImage().Dispose();

        (
            // 100 left + 10 padding + 7.5 half of the widths difference
            g1.X == 100 + 10 + 7.5 &&

            // 200 top + 20 padding
            g1.Y == 200 + 20 &&

            // preserve the previous x...
            g2.X == 110 &&

            // 200 top + 20 padding + 25 previous height
            g2.Y == 200 + 20 + 25).ShouldBeTrue();
    }

    [Fact]
    public void StackLayoutVerticalEnd()
    {
        var stackPanel = new StackLayout
        {
            Padding = new Padding(10, 20),
            X = 100,
            Y = 200,
            HorizontalAlignment = Align.End,
            Orientation = ContainerOrientation.Vertical
        };

        RectangleGeometry g1, g2;

        stackPanel.Children = [
            g1 = new RectangleGeometry
            {
                Width = 10,
                Height = 25,
                Fill = new SolidColorPaint(TestColors.Red)
            },
            g2 = new RectangleGeometry
            {
                Width = 25,
                Height = 25,
                Fill = new SolidColorPaint(TestColors.Blue)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] { 1, 2, 3 }
                    }
                }
            }
        };

        chart.CoreCanvas.AddGeometry(stackPanel);
        chart.GetImage().Dispose();

        (
            // 100 left + 10 padding + the widths difference
            g1.X == 100 + 10 + 15 &&

            // 200 top + 20 padding
            g1.Y == 200 + 20 &&

            // preserve the previous x...
            g2.X == 110 &&

            // 200 top + 20 padding + 25 previous height
            g2.Y == 200 + 20 + 25).ShouldBeTrue();
    }

    [Fact]
    public void StackLayoutHorizontalStartWrap()
    {
        var stackPanel = new StackLayout
        {
            Padding = new Padding(0),
            X = 100,
            Y = 200,
            MaxWidth = 12 * 3,
            VerticalAlignment = Align.Start,
            Orientation = ContainerOrientation.Horizontal
        };

        RectangleGeometry g0, g1, g2;

        stackPanel.Children = [
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            g0 = new RectangleGeometry
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 10,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 10,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g1 = new RectangleGeometry
            {
                Width = 10,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 12,
                Height = 20,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 12,
                Height = 20,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g2 = new RectangleGeometry
            {
                Width = 12,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] { 1, 2, 3 }
                    }
                }
            }
        };

        chart.CoreCanvas.AddGeometry(stackPanel);
        chart.GetImage().Dispose();

        var panelSize = stackPanel.Measure();

        (
            g0.X == 100 + 25 &&
            g0.Y == 200 &&
            g1.X == 100 + 20 &&
            g1.Y == 200 + 15 &&
            g2.X == 100 + 24 &&
            g2.Y == 200 + 15 + 15 &&
            panelSize.Width == 36 &&
            panelSize.Height == 50).ShouldBeTrue();
    }

    [Fact]
    public void StackPanelLayoutMiddleWrap()
    {
        var stackPanel = new StackLayout
        {
            Padding = new Padding(0),
            X = 100,
            Y = 200,
            MaxWidth = 12 * 3,
            VerticalAlignment = Align.Middle,
            Orientation = ContainerOrientation.Horizontal
        };

        RectangleGeometry g0, g1, g2;

        stackPanel.Children = [
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            g0 = new RectangleGeometry
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 10,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 10,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g1 = new RectangleGeometry
            {
                Width = 10,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 12,
                Height = 20,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 12,
                Height = 20,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g2 = new RectangleGeometry
            {
                Width = 12,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] { 1, 2, 3 }
                    }
                }
            }
        };

        chart.CoreCanvas.AddGeometry(stackPanel);
        chart.GetImage().Dispose();

        var panelSize = stackPanel.Measure();

        (
            g0.X == 100 + 25 &&
            g0.Y == 205 &&
            g1.X == 100 + 20 &&
            g1.Y == 220 &&
            g2.X == 100 + 24 &&
            g2.Y == 237.5 &&
            panelSize.Width == 36 &&
            panelSize.Height == 50).ShouldBeTrue();
    }

    [Fact]
    public void StackLayoutHorizontalEndWrap()
    {
        var stackPanel = new StackLayout
        {
            Padding = new Padding(0),
            X = 100,
            Y = 200,
            MaxWidth = 12 * 3,
            VerticalAlignment = Align.End,
            Orientation = ContainerOrientation.Horizontal
        };

        RectangleGeometry g0, g1, g2;

        stackPanel.Children = [
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 5,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            g0 = new RectangleGeometry
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 10,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 10,
                Height = 15,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g1 = new RectangleGeometry
            {
                Width = 10,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 12,
                Height = 20,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 12,
                Height = 20,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g2 = new RectangleGeometry
            {
                Width = 12,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] { 1, 2, 3 }
                    }
                }
            }
        };

        chart.CoreCanvas.AddGeometry(stackPanel);
        chart.GetImage().Dispose();

        var panelSize = stackPanel.Measure();

        (
            g0.X == 100 + 25 &&
            g0.Y == 210 &&
            g1.X == 100 + 20 &&
            g1.Y == 225 &&
            g2.X == 100 + 24 &&
            g2.Y == 245 &&
            panelSize.Width == 36 &&
            panelSize.Height == 50).ShouldBeTrue();
    }

    [Fact]
    public void StackPanelLayoutStartWrap()
    {
        var stackPanel = new StackLayout
        {
            Padding = new Padding(0),
            X = 200,
            Y = 100,
            MaxHeight = 12 * 3,
            HorizontalAlignment = Align.Start,
            Orientation = ContainerOrientation.Vertical
        };

        RectangleGeometry g0, g1, g2;

        stackPanel.Children = [
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            g0 = new RectangleGeometry
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 10,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 10,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g1 = new RectangleGeometry
            {
                Width = 5,
                Height = 10,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 20,
                Height = 12,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 20,
                Height = 12,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g2 = new RectangleGeometry
            {
                Width = 5,
                Height = 12,
                Fill = new SolidColorPaint(TestColors.Yellow)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] { 1, 2, 3 }
                    }
                }
            }
        };

        chart.CoreCanvas.AddGeometry(stackPanel);
        chart.GetImage().Dispose();

        var panelSize = stackPanel.Measure();

        (
            g0.X == 200 &&
            g0.Y == 125 &&
            g1.X == 215 &&
            g1.Y == 120 &&
            g2.X == 230 &&
            g2.Y == 124 &&
            panelSize.Width == 50 &&
            panelSize.Height == 36).ShouldBeTrue();
    }

    [Fact]
    public void StackLayoutVerticalMiddleWrap()
    {
        var stackPanel = new StackLayout
        {
            Padding = new Padding(0),
            X = 200,
            Y = 100,
            MaxHeight = 12 * 3,
            HorizontalAlignment = Align.Middle,
            Orientation = ContainerOrientation.Vertical
        };

        RectangleGeometry g0, g1, g2;

        stackPanel.Children = [
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            g0 = new RectangleGeometry
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 10,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 10,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g1 = new RectangleGeometry
            {
                Width = 5,
                Height = 10,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 20,
                Height = 12,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 20,
                Height = 12,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g2 = new RectangleGeometry
            {
                Width = 5,
                Height = 12,
                Fill = new SolidColorPaint(TestColors.Yellow)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] { 1, 2, 3 }
                    }
                }
            }
        };

        chart.CoreCanvas.AddGeometry(stackPanel);
        chart.GetImage().Dispose();

        var panelSize = stackPanel.Measure();

        (
            g0.X == 205 &&
            g0.Y == 125 &&
            g1.X == 220 &&
            g1.Y == 120 &&
            g2.X == 237.5 &&
            g2.Y == 124 &&
            panelSize.Width == 50 &&
            panelSize.Height == 36).ShouldBeTrue();
    }

    [Fact]
    public void StackPanelLayoutEndWrap()
    {
        var stackPanel = new StackLayout
        {
            Padding = new Padding(0),
            X = 200,
            Y = 100,
            MaxHeight = 12 * 3,
            HorizontalAlignment = Align.End,
            Orientation = ContainerOrientation.Vertical
        };

        RectangleGeometry g0, g1, g2;

        stackPanel.Children = [
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            g0 = new RectangleGeometry
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 10,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 15,
                Height = 10,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g1 = new RectangleGeometry
            {
                Width = 5,
                Height = 10,
                Fill = new SolidColorPaint(TestColors.Yellow)
            },
            new RectangleGeometry
            {
                Width = 20,
                Height = 12,
                Fill = new SolidColorPaint(TestColors.Green)
            },
            new RectangleGeometry
            {
                Width = 20,
                Height = 12,
                Fill = new SolidColorPaint(TestColors.Blue)
            },
            g2 = new RectangleGeometry
            {
                Width = 5,
                Height = 12,
                Fill = new SolidColorPaint(TestColors.Yellow)
            }
        ];

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] { 1, 2, 3 }
                    }
                }
            }
        };

        chart.CoreCanvas.AddGeometry(stackPanel);
        chart.GetImage().Dispose();

        var panelSize = stackPanel.Measure();

        (
            g0.X == 210 &&
            g0.Y == 125 &&
            g1.X == 225 &&
            g1.Y == 120 &&
            g2.X == 245 &&
            g2.Y == 124 &&
            panelSize.Width == 50 &&
            panelSize.Height == 36).ShouldBeTrue();
    }
}
