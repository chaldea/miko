# Charts

`Miko.Charts` draws animated charts in a Miko app: line, area, column, row, scatter, stacked,
step, financial (candlesticks), box, heat, pie, doughnut, gauge, polar (radar) and geographic
heat maps. It is a port of [LiveCharts2](https://livecharts.dev) (MIT): the chart engine is
LiveCharts' own, and the drawing goes through Miko's rendering abstractions
([Custom Drawing](../engine/drawing.md)) instead of SkiaSharp, so charts render on every Miko
platform and use Miko's text pipeline (font fallback, right-to-left text, complex scripts).

## Setup

Reference the package and register the charts on the app builder:

```bash
dotnet add package Miko.Charts --prerelease
```

```csharp
using Miko.Charts;

var builder = MikoAppBuilder.CreateDefault();
builder.Services.AddMikoCharts();
```

Then import the namespaces in `_Imports.razor`:

```razor
@using Miko.Charts
@using LiveChartsCore
@using LiveChartsCore.Defaults
@using LiveChartsCore.Measure
@using LiveChartsCore.MikoView
@using LiveChartsCore.MikoView.Painting
```

`AddMikoCharts` takes options:

```csharp
builder.Services.AddMikoCharts(options =>
{
    options.DarkTheme = true;                       // dark axis, legend and tooltip colors
    options.FontFamily = "Inter, Arial, sans-serif"; // text of axes, legends, tooltips
    options.AnimationsSpeed = TimeSpan.FromMilliseconds(400);
});
```

LiveCharts keeps its settings per process, so the theme and font apply to every chart. Register
custom fonts with `FontManager` before the first chart renders ([Fonts & Text](../guide/fonts.md)).

## Components

| Component | Draws | Main parameters |
| --- | --- | --- |
| `CartesianChart` | Series on x and y axes | `Series`, `XAxes`, `YAxes`, `Sections`, `ZoomMode`, `FindingStrategy` |
| `PieChart` | Pie, doughnut and gauge series | `Series`, `InitialRotation`, `MaxAngle`, `MinValue`, `MaxValue`, `IsClockwise` |
| `PolarChart` | Series on an angle and a radius axis | `Series`, `AngleAxes`, `RadiusAxes`, `TotalAngle`, `InnerRadius`, `InitialRotation` |
| `GeoMap` | A world map with lands colored by value | `Series`, `ActiveMap`, `MapProjection`, `Stroke`, `Fill` |

Every chart (not `GeoMap`) also takes `Title`, `VisualElements`, `LegendPosition`,
`TooltipPosition`, `Legend`, `Tooltip`, the legend and tooltip paints, `DrawMargin`,
`AnimationsSpeed`, `EasingFunction`, `ChartTheme` and `Background`, and raises
`DataPointerDown`, `HoveredPointsChanged` and `UpdateFinished`.

A chart fills the width of its parent and is 300 pixels high. Size it with `Style` or `Class`:

```razor
<CartesianChart Series="@_series" Style="@(new Style { Height = Length.Px(220) })" />
```

## A line chart

```razor
<CartesianChart Series="@_series" XAxes="@_xAxes" LegendPosition="LegendPosition.Top" />

@code {
    private readonly ISeries[] _series =
    [
        new ColumnSeries<double>([42, 55, 61, 48, 70, 82]) { Name = "Revenue" },
        new LineSeries<double>([38, 41, 52, 50, 60, 74]) { Name = "Target", Fill = null },
    ];

    private readonly Axis[] _xAxes = [new Axis { Labels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"] }];
}
```

Series and axes are LiveCharts objects: `LineSeries<T>`, `ColumnSeries<T>`, `RowSeries<T>`,
`ScatterSeries<T>`, `StepLineSeries<T>`, `StackedAreaSeries<T>`, `StackedColumnSeries<T>`,
`StackedRowSeries<T>`, `StackedStepAreaSeries<T>`, `CandlesticksSeries<T>`, `BoxSeries<T>`,
`HeatSeries<T>`, `PieSeries<T>`, `PolarLineSeries<T>`, `HeatLandSeries`, and `Axis`,
`DateTimeAxis`, `TimeSpanAxis`, `LogaritmicAxis`, `PolarAxis`. Their properties are the ones
documented by LiveCharts, so the [LiveCharts samples](https://livecharts.dev/docs/blazor/2.0.0/samples.lines.basic)
apply: replace `LiveChartsCore.SkiaSharpView` with `LiveChartsCore.MikoView` and `SKColor` with
`LvcColor`.

## Pie, doughnut and gauge

```razor
<PieChart Series="@_slices" LegendPosition="LegendPosition.Right" />

@code {
    private readonly ISeries[] _slices =
    [
        new PieSeries<double>([42]) { Name = "Desktop", InnerRadius = 50 },
        new PieSeries<double>([33]) { Name = "Android", InnerRadius = 50 },
        new PieSeries<double>([25]) { Name = "iOS", InnerRadius = 50 },
    ];
}
```

`InnerRadius` turns a pie into a doughnut. A gauge is a pie of one value over a maximum;
`GaugeGenerator` (in `LiveChartsCore.MikoView.Extensions`) builds the value and the background
series:

```razor
<PieChart Series="@_gauge" InitialRotation="135" MaxAngle="270" MinValue="0" MaxValue="100" />

@code {
    private readonly ObservableValue _load = new(64);
    private PieSeries<ObservableValue>[] _gauge = null!;

    protected override void OnInitialized()
    {
        _gauge = GaugeGenerator.BuildSolidGauge(
            new GaugeItem(_load, s => { s.InnerRadius = 60; s.DataLabelsPosition = PolarLabelsPosition.ChartCenter; }),
            new GaugeItem(GaugeItem.Background, s => s.InnerRadius = 60));
    }
}
```

## Updating data

Charts follow their data:

- Assigning a new collection to a parameter replaces the series or axes.
- A collection that raises change notifications, such as `ObservableCollection<T>`, updates the
  chart when items are added, removed or replaced.
- Values that raise property changes (`ObservableValue`, `ObservablePoint`, `DateTimePoint`,
  `FinancialPoint` …) update the chart when they change.

Changes animate from the old values to the new ones. The chart redraws only while something moves,
so an idle chart costs no frames.

Change the data on the UI thread, which in Miko is the render thread: event handlers and
component methods already run there. From a timer or a background task, post the change through
the engine's dispatcher:

```csharp
[Inject] public MikoEngine? Engine { get; set; }

private readonly ObservableCollection<ObservableValue> _values = [];

private void OnTimer(object? _) => Engine?.Dispatcher.Post(() => _values.Add(new ObservableValue(Read())));
```

## Paints

A paint fills or strokes a series, an axis or a label:

| Paint | Draws with |
| --- | --- |
| `SolidColorPaint(LvcColor color, float strokeWidth = 1)` | One color. |
| `LinearGradientPaint(LvcColor[] stops, LvcPoint start, LvcPoint end)` | A linear gradient; points are fractions of the area drawn. |
| `RadialGradientPaint(LvcColor[] stops, LvcPoint? center, float radius)` | A radial gradient. |

Every paint also has `StrokeCap`, `StrokeJoin`, `PathEffect` (`DashEffect` for dashed lines),
`ImageFilter` (`DropShadow`, `Blur`), and for text `FontFamily` and `FontWeight`:

```csharp
new LineSeries<double>(values)
{
    Stroke = new SolidColorPaint(new LvcColor(33, 150, 243), 3) { PathEffect = new DashEffect([8, 4]) },
    Fill = new LinearGradientPaint(new LvcColor(33, 150, 243, 90), new LvcColor(33, 150, 243, 0),
        new LvcPoint(0.5f, 0), new LvcPoint(0.5f, 1)),
};

new Axis { LabelsPaint = new SolidColorPaint(new LvcColor(90, 90, 90)) { FontFamily = "Courier New" } };
```

## Interaction

- **Tooltips** show the points under the pointer, on hover with a mouse and on press with touch.
  `TooltipPosition="TooltipPosition.Hidden"` turns them off; `FindingStrategy` chooses which
  points a cartesian chart reports.
- **Clicks**: `DataPointerDown` receives the points under the pointer.
- **Panning**: with a `ZoomMode` that includes panning (`ZoomAndPanMode.X`, `Y`, `Both`), a drag
  pans the axes. A drag with the secondary mouse button draws a zoom section.
- **Zooming**: Miko has no wheel event yet, so the wheel does not zoom. Call
  `CartesianChart.Zoom(ZoomDirection.ZoomIn)` from a button, through an `@ref`.

```razor
<CartesianChart @ref="_chart" Series="@_series" ZoomMode="ZoomAndPanMode.X" />
<button @onclick="() => _chart?.Zoom(ZoomDirection.ZoomIn)">Zoom in</button>

@code {
    private CartesianChart? _chart;
}
```

## Maps

```razor
<GeoMap Series="@_heat" />

@code {
    private readonly HeatLandSeries[] _heat =
    [
        new HeatLandSeries
        {
            Lands = [new HeatLand { Name = "chn", Value = 12 }, new HeatLand { Name = "usa", Value = 9 }],
        },
    ];
}
```

Lands are named by their ISO 3166-1 alpha-3 code. The world map is bundled; `Maps.GetMapFromStreamReader`
loads another GeoJSON map. A drag pans the map.

## Rendering a chart to an image

`InMemoryCartesianChart`, `InMemoryPieChart` and `InMemoryPolarChart` (in
`LiveChartsCore.MikoView.Charts`) render a chart without a window, for reports and tests. The
chart's settings are on its `View`:

```csharp
var engine = new MikoEngineBuilder().Build();
var chart = new InMemoryCartesianChart(engine.Graphics) { Width = 800, Height = 400 };
chart.View.Series = [new LineSeries<double>([1, 5, 3, 8])];

chart.SaveImage("chart.png");                // or: using var surface = chart.GetImage();
```

## Differences from LiveCharts2

- Namespaces: `LiveChartsCore.SkiaSharpView` is `LiveChartsCore.MikoView`. The in-memory charts
  replace `SKCartesianChart`, `SKPieChart` and `SKPolarChart`, and the default tooltip and legend
  are `DefaultTooltip` and `DefaultLegend` in `LiveChartsCore.MikoView.Tooltips`.
- Colors are `LvcColor`, not `SKColor`. Fonts are `FontFamily` / `FontWeight` on a paint, not an
  `SKTypeface`; fallback for missing characters is automatic.
- `SVGPathGeometry` and `VariableSVGPathGeometry` take SVG path data as before; code that built
  an `SKPath` uses `Miko.Graphics.GraphicsPath` instead.
- There is no XAML: the chart properties are component parameters, and LiveCharts' commands
  (`DataPointerDownCommand` …) are `EventCallback` parameters.
- The wheel does not zoom (see [Interaction](#interaction)).

`Miko.Charts` is a preview package: its API may change before 1.0.
