namespace Miko.Charts;

/// <summary>Options of the chart components, registered by <see cref="ChartsServiceExtensions.AddMikoCharts"/>.</summary>
public sealed class MikoChartsOptions
{
    /// <summary>
    /// How long chart transitions take when a chart does not set <c>AnimationsSpeed</c>; LiveCharts' default
    /// (800 ms) when null.
    /// </summary>
    public TimeSpan? AnimationsSpeed { get; set; }

    /// <summary>
    /// The font family of the chart text (axis labels, legends, tooltips) when a paint does not set one: a
    /// comma separated fallback chain, as in CSS. Register custom fonts with <c>FontManager</c> first.
    /// </summary>
    public string FontFamily { get; set; } = "Arial, sans-serif";

    /// <summary>Starts with the dark theme instead of the light one.</summary>
    public bool DarkTheme { get; set; }
}
