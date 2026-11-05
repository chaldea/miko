// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Rewritten to draw through Miko.Graphics instead of SkiaSharp.

using Miko.Graphics;

namespace LiveChartsCore.MikoView.Painting.Effects;

/// <summary>Strokes with alternating drawn and skipped lengths.</summary>
/// <param name="dashArray">The on/off lengths, an even count of two at least.</param>
/// <param name="phase">The offset into the pattern.</param>
public class DashEffect(float[] dashArray, float phase = 0) : PathEffect(s_key)
{
    internal static object s_key = new();

    private float[] DashArray { get; set; } = dashArray;

    private float Phase { get; set; } = phase;

    /// <inheritdoc />
    public override PathEffect Clone() => new DashEffect((float[])DashArray.Clone(), Phase);

    /// <inheritdoc />
    public override void Apply(GraphicsPaint paint) => paint.SetDash(DashArray, Phase);

    /// <inheritdoc />
    public override PathEffect? Transitionate(float progress, PathEffect? target)
    {
        if (target is not DashEffect dashEffect) return target;

        if (DashArray.Length != dashEffect.DashArray.Length)
            throw new InvalidOperationException("The dash arrays must have the same length.");

        var clone = (DashEffect)Clone();
        for (var i = 0; i < DashArray.Length; i++)
            clone.DashArray[i] = DashArray[i] + (dashEffect.DashArray[i] - DashArray[i]) * progress;

        clone.Phase = Phase + (dashEffect.Phase - Phase) * progress;
        return clone;
    }
}
