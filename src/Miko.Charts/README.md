# Miko.Charts

Animated charts for Miko, ported from [LiveCharts2](https://livecharts.dev) (MIT). The chart engine is
LiveCharts' own; drawing goes through Miko's rendering abstractions instead of SkiaSharp, so the charts run
on every Miko platform and use Miko's text pipeline.

```csharp
using Miko.Charts;

builder.Services.AddMikoCharts();
```

```razor
@using Miko.Charts
@using LiveChartsCore
@using LiveChartsCore.MikoView

<CartesianChart Series="@_series" />

@code {
    private readonly ISeries[] _series = [new LineSeries<double>([1, 5, 3, 8, 4])];
}
```

Components: `CartesianChart`, `PieChart`, `PolarChart`, `GeoMap`. See the
[chart documentation](https://github.com/chaldea/miko/blob/main/docs/components/charts.md).
