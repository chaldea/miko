// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Rewritten to draw through Miko.Graphics instead of SkiaSharp.

using LiveChartsCore.Drawing;
using Miko.Graphics;

namespace LiveChartsCore.MikoView.Painting.ImageFilters;

/// <summary>Draws a blurred, offset shadow beneath what the paint draws.</summary>
public class DropShadow(float dx, float dy, float sigmaX, float sigmaY, LvcColor color) : ImageFilter(s_key)
{
    internal static object s_key = new();

    private float Dx { get; set; } = dx;

    private float Dy { get; set; } = dy;

    private float SigmaX { get; set; } = sigmaX;

    private float SigmaY { get; set; } = sigmaY;

    private LvcColor Color { get; set; } = color;

    /// <inheritdoc />
    public override ImageFilter Clone() => new DropShadow(Dx, Dy, SigmaX, SigmaY, Color);

    /// <inheritdoc />
    public override void Apply(GraphicsPaint paint) =>
        paint.SetDropShadow(Dx, Dy, SigmaX, SigmaY, new Miko.Common.Color(Color.R, Color.G, Color.B, Color.A));

    /// <inheritdoc />
    protected override ImageFilter Transitionate(float progress, ImageFilter target)
    {
        var shadow = (DropShadow)target;
        var clone = (DropShadow)Clone();
        clone.Dx = Dx + (shadow.Dx - Dx) * progress;
        clone.Dy = Dy + (shadow.Dy - Dy) * progress;
        clone.SigmaX = SigmaX + (shadow.SigmaX - SigmaX) * progress;
        clone.SigmaY = SigmaY + (shadow.SigmaY - SigmaY) * progress;
        clone.Color = new LvcColor(
            (byte)(Color.R + (shadow.Color.R - Color.R) * progress),
            (byte)(Color.G + (shadow.Color.G - Color.G) * progress),
            (byte)(Color.B + (shadow.Color.B - Color.B) * progress),
            (byte)(Color.A + (shadow.Color.A - Color.A) * progress));
        return clone;
    }
}
