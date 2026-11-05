namespace Miko.Charts.Tests.LiveCharts;

using LiveChartsCore.Drawing;

/// <summary>
/// The SkiaSharp named colors the LiveCharts tests used (<c>SKColors.X</c>), with SkiaSharp's RGB values;
/// <see cref="LvcColor"/> has no named colors.
/// </summary>
internal static class TestColors
{
    public static LvcColor Black => new(0, 0, 0);
    public static LvcColor White => new(255, 255, 255);
    public static LvcColor Red => new(255, 0, 0);
    public static LvcColor Green => new(0, 128, 0);
    public static LvcColor Blue => new(0, 0, 255);
    public static LvcColor Yellow => new(255, 255, 0);
    public static LvcColor Purple => new(128, 0, 128);
    public static LvcColor LightGray => new(211, 211, 211);
    public static LvcColor DarkSlateGray => new(47, 79, 79);

    /// <summary><c>SKColor.Empty</c>: transparent black (not <see cref="LvcColor.Empty"/>, which means "the default").</summary>
    public static LvcColor Transparent => new(0, 0, 0, 0);
}
