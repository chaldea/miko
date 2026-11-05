// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Rewritten to configure the Miko render layer instead of SkiaSharp.

using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.Motion;
using LiveChartsCore.Painting;
using LiveChartsCore.MikoView.TypeConverters;
using Miko.Graphics;

namespace LiveChartsCore.MikoView;

/// <summary>
/// Configures LiveCharts to draw through Miko.Graphics.
/// </summary>
public static class LiveChartsMiko
{
    internal static MotionCanvasComposer.MotionCanvasRenderingFactoryDelegate MotionCanvasRenderingFactory { get; set; } =
        (settings) => throw new NotImplementedException(
            "No motion canvas rendering factory has been set, please use the method 'HasMotionCanvasRenderingFactory' to set one.");

    internal static TextSettings DefaultTextSettings { get; set; } = new();

    private static IGraphicsFactory? s_graphics;

    /// <summary>
    /// Gets the factory that lays out the text of the labels. A label is measured while the chart is
    /// measured, before there is a drawing context, so the factory is process-wide. Miko's factory keeps
    /// no per-engine state, so any engine's <c>Graphics</c> is equivalent; the chart components set it.
    /// </summary>
    internal static IGraphicsFactory Graphics =>
        s_graphics ?? throw new InvalidOperationException(
            "No graphics factory has been set. Charts get theirs from the Miko engine when a chart component " +
            "is created; when building a chart outside a Miko app, call LiveChartsMiko.UseGraphics(engine.Graphics).");

    /// <summary>
    /// Sets the factory that lays out the text of the labels, normally <c>MikoEngine.Graphics</c>.
    /// </summary>
    public static void UseGraphics(IGraphicsFactory graphics)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        s_graphics = graphics;
    }

    /// <summary>
    /// Registers the Miko defaults (provider, theme, mappers) the first time a chart type is used.
    /// </summary>
    internal static LiveChartsSettings EnsureInitialized()
    {
        LiveCharts.Configure(settings => settings.UseDefaults());
        return LiveCharts.DefaultSettings;
    }

    /// <summary>
    /// Adds the Miko provider, the default theme and the default mappers to any of them not defined yet.
    /// </summary>
    public static LiveChartsSettings UseDefaults(this LiveChartsSettings settings)
    {
        if (!LiveCharts.DefaultSettings.HasBackedDefined)
            _ = settings.AddMiko();

        if (!LiveCharts.DefaultSettings.HasThemeDefined)
            _ = settings.AddDefaultTheme();

        if (!LiveCharts.DefaultSettings.HasMappersDefined)
            _ = settings.AddDefaultMappers();

        return settings;
    }

    /// <summary>
    /// Registers the Miko provider and the parsers that turn strings into colors, margins and so on.
    /// </summary>
    public static LiveChartsSettings AddMiko(this LiveChartsSettings settings)
    {
        PropertyDefinition.Parsers[typeof(Paint)] = HexToPaintTypeConverter.Parse;
        PropertyDefinition.Parsers[typeof(LvcColor)] = HexToLvcColorTypeConverter.Parse;
        PropertyDefinition.Parsers[typeof(Margin)] = MarginTypeConverter.ParseMargin;
        PropertyDefinition.Parsers[typeof(Padding)] = PaddingTypeConverter.ParsePadding;
        PropertyDefinition.Parsers[typeof(LvcPointD)] = PointDTypeConverter.ParsePoint;
        PropertyDefinition.Parsers[typeof(LvcPoint)] = PointTypeConverter.ParsePoint;

        return settings.HasProvider(new ChartProvider());
    }

    /// <summary>
    /// Configures the text settings, for example the default font family.
    /// </summary>
    public static LiveChartsSettings HasTextSettings(
        this LiveChartsSettings settings, TextSettings textSettings)
    {
        DefaultTextSettings = textSettings;
        return settings;
    }

    /// <summary>
    /// Sets the rendering factory, used by hosts that build their own motion canvas.
    /// </summary>
    public static LiveChartsSettings HasRenderingFactory(
        this LiveChartsSettings settings, MotionCanvasComposer.MotionCanvasRenderingFactoryDelegate factory)
    {
        MotionCanvasRenderingFactory = factory;
        return settings;
    }

    /// <summary>
    /// Returns the color with a different alpha.
    /// </summary>
    public static LvcColor WithOpacity(this LvcColor color, byte opacity) =>
        LvcColor.FromArgb(opacity, color);

    /// <summary>
    /// Returns the color with a different alpha (as <c>SKColor.WithAlpha</c> did in the SkiaSharp view).
    /// </summary>
    public static LvcColor WithAlpha(this LvcColor color, byte alpha) =>
        LvcColor.FromArgb(alpha, color);

    /// <summary>
    /// Calculates the distance in pixels from the target <see cref="ChartPoint"/> to the given location in the UI.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="location">The location.</param>
    /// <returns>The distance in pixels.</returns>
    public static double GetDistanceTo(this ChartPoint target, LvcPoint location)
    {
        LvcPointD dataCoordinates;
        double x, y;

        if (target.Context is ICartesianChartView cartesianChart)
        {
            dataCoordinates = cartesianChart.ScalePixelsToData(new LvcPointD(location));

            var cartesianSeries = (ICartesianSeries)target.Context.Series;

            if (target.Context.Series.SeriesProperties.HasFlag(SeriesProperties.PrimaryAxisHorizontalOrientation))
            {
                var primaryAxis = cartesianChart.Core.YAxes[cartesianSeries.ScalesYAt];
                var secondaryAxis = cartesianChart.Core.XAxes[cartesianSeries.ScalesXAt];

                var drawLocation = cartesianChart.Core.DrawMarginLocation;
                var drawMarginSize = cartesianChart.Core.DrawMarginSize;
                var secondaryScale = new Scaler(drawLocation, drawMarginSize, primaryAxis);
                var primaryScale = new Scaler(drawLocation, drawMarginSize, secondaryAxis);

                var coordinate = target.Coordinate;

                x = secondaryScale.ToPixels(coordinate.SecondaryValue);
                y = primaryScale.ToPixels(coordinate.PrimaryValue);
            }
            else
            {
                var primaryAxis = cartesianChart.Core.YAxes[cartesianSeries.ScalesXAt];
                var secondaryAxis = cartesianChart.Core.XAxes[cartesianSeries.ScalesYAt];

                var drawLocation = cartesianChart.Core.DrawMarginLocation;
                var drawMarginSize = cartesianChart.Core.DrawMarginSize;

                var secondaryScale = new Scaler(drawLocation, drawMarginSize, secondaryAxis);
                var primaryScale = new Scaler(drawLocation, drawMarginSize, primaryAxis);

                var coordinate = target.Coordinate;

                x = secondaryScale.ToPixels(coordinate.SecondaryValue);
                y = primaryScale.ToPixels(coordinate.PrimaryValue);
            }
        }
        else if (target.Context is IPolarChartView polarChart)
        {
            dataCoordinates = polarChart.ScalePixelsToData(new LvcPointD(location));

            var polarSeries = (IPolarSeries)target.Context.Series;

            var angleAxis = polarChart.Core.AngleAxes[polarSeries.ScalesAngleAt];
            var radiusAxis = polarChart.Core.RadiusAxes[polarSeries.ScalesRadiusAt];

            var drawLocation = polarChart.Core.DrawMarginLocation;
            var drawMarginSize = polarChart.Core.DrawMarginSize;

            var scaler = new PolarScaler(
                drawLocation, drawMarginSize, angleAxis, radiusAxis,
                polarChart.Core.InnerRadius, polarChart.Core.InitialRotation, polarChart.Core.TotalAnge);

            var scaled = scaler.ToPixels(target);
            x = scaled.X;
            y = scaled.Y;
        }
        else
        {
            throw new NotImplementedException();
        }

        // calculate the distance
        var dx = dataCoordinates.X - x;
        var dy = dataCoordinates.Y - y;

        var distance = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));

        return distance;
    }
}
