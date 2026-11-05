// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).

namespace LiveChartsCore.MikoView;

/// <summary>
/// Defines the text settings of the charts.
/// </summary>
public class TextSettings
{
    /// <summary>
    /// Gets or sets the font family used by a paint that does not set one: a comma separated fallback
    /// chain, as in CSS. Mixed scripts fall back per character through Miko's font pipeline, so a single
    /// family is normally enough.
    /// </summary>
    public string DefaultFontFamily { get; set; } = "Arial, sans-serif";

    /// <summary>
    /// Gets a value indicating whether the layout is right to left.
    /// </summary>
    public bool IsRTL => LiveCharts.DefaultSettings.IsRightToLeft;
}
