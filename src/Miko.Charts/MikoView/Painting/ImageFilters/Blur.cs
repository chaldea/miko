// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Rewritten to draw through Miko.Graphics instead of SkiaSharp.

using Miko.Graphics;

namespace LiveChartsCore.MikoView.Painting.ImageFilters;

/// <summary>Blurs what the paint draws.</summary>
public class Blur(float sigmaX, float sigmaY) : ImageFilter(s_key)
{
    internal static object s_key = new();

    private float SigmaX { get; set; } = sigmaX;

    private float SigmaY { get; set; } = sigmaY;

    /// <inheritdoc />
    public override ImageFilter Clone() => new Blur(SigmaX, SigmaY);

    /// <inheritdoc />
    public override void Apply(GraphicsPaint paint) => paint.SetBlur(SigmaX, SigmaY);

    /// <inheritdoc />
    protected override ImageFilter Transitionate(float progress, ImageFilter target)
    {
        var blur = (Blur)target;
        var clone = (Blur)Clone();
        clone.SigmaX = SigmaX + (blur.SigmaX - SigmaX) * progress;
        clone.SigmaY = SigmaY + (blur.SigmaY - SigmaY) * progress;
        return clone;
    }
}
