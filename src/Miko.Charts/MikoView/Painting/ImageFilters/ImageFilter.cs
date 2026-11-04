// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Rewritten to draw through Miko.Graphics instead of SkiaSharp.

using Miko.Graphics;

namespace LiveChartsCore.MikoView.Painting.ImageFilters;

/// <summary>A filter applied to everything a paint draws.</summary>
/// <param name="key">Identifies the kind of filter, to find the neutral filter a transition starts from.</param>
public abstract class ImageFilter(object key)
{
    private readonly object _key = key;

    private static readonly Dictionary<object, ImageFilter> s_defaultFilters = new()
    {
        { DropShadow.s_key, new DropShadow(0, 0, 0, 0, new(0, 0, 0, 0)) },
        { Blur.s_key, new Blur(0, 0) }
    };

    /// <summary>Applies the filter to a graphics paint.</summary>
    public abstract void Apply(GraphicsPaint paint);

    /// <summary>Creates a copy.</summary>
    public abstract ImageFilter Clone();

    /// <summary>Gets the filter between this one and <paramref name="target"/>.</summary>
    protected abstract ImageFilter Transitionate(float progress, ImageFilter target);

    internal static ImageFilter? Transitionate(ImageFilter? from, ImageFilter? to, float progress)
    {
        if (from is null && to is null) return null;

        var key = (from ?? to)!._key;

        // a transition to or from nothing runs against the neutral filter (a transparent shadow, no blur)
        from ??= s_defaultFilters[key];
        to ??= s_defaultFilters[key];

        return from.Transitionate(progress, to);
    }
}
