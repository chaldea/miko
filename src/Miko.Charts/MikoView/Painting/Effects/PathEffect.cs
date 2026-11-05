// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Rewritten to draw through Miko.Graphics instead of SkiaSharp.

using Miko.Graphics;

namespace LiveChartsCore.MikoView.Painting.Effects;

/// <summary>Changes the geometry of a stroke.</summary>
/// <param name="key">Identifies the kind of effect, to find the neutral effect a transition starts from.</param>
public abstract class PathEffect(object key)
{
    private readonly object _key = key;

    private static readonly Dictionary<object, PathEffect> s_defaultEffects = new()
    {
        { DashEffect.s_key, new DashEffect([1, 0], 0) }
    };

    /// <summary>Creates a copy.</summary>
    public abstract PathEffect Clone();

    /// <summary>Applies the effect to a graphics paint.</summary>
    public abstract void Apply(GraphicsPaint paint);

    /// <summary>Gets the effect between this one and <paramref name="target"/>.</summary>
    public abstract PathEffect? Transitionate(float progress, PathEffect? target);

    internal static PathEffect? Transitionate(PathEffect? from, PathEffect? to, float progress)
    {
        if (from is null && to is null) return null;

        var key = (from ?? to)!._key;

        // a transition to or from nothing runs against the neutral effect (a dash that never breaks)
        from ??= s_defaultEffects[key];
        to ??= s_defaultEffects[key];

        return from.Transitionate(progress, to);
    }
}
