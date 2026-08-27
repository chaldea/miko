using Miko.Common;
using Miko.Core;
using Miko.Styling;

namespace Miko.Animation;

/// <summary>
/// Per-element animated paint values. These values are consumed only by the renderer, so updating
/// them does not invalidate computed styles or layout boxes.
/// </summary>
public sealed class AnimatedStyleOverlay
{
    private readonly Dictionary<Element, AnimatedPaintValues> _values = new();

    public bool TryGet(Element element, out AnimatedPaintValues values)
        => _values.TryGetValue(element, out values!);

    internal AnimatedPaintValues GetOrCreate(Element element)
    {
        if (!_values.TryGetValue(element, out var values))
        {
            values = new AnimatedPaintValues();
            _values.Add(element, values);
        }
        return values;
    }

    internal void SetOpacity(Element element, float value)
    {
        // Preserve the standalone AnimationManager contract used without an engine/layout. Attached
        // engine elements never take this branch, so their declared Style remains stable.
        if (element.Owner == null && element.LayoutBox == null)
        {
            element.Style ??= new Style();
            element.Style.Opacity = value;
        }
        var values = GetOrCreate(element);
        values.Prepare(element);
        values.Opacity = value;
    }

    internal void SetTransform(Element element, Transform value)
    {
        if (element.Owner == null && element.LayoutBox == null)
        {
            element.Style ??= new Style();
            element.Style.Transform = value;
        }
        var values = GetOrCreate(element);
        values.Prepare(element);
        values.Transform = value;
    }

    internal void SetColor(Element element, string property, Color value)
    {
        if (element.Owner == null && element.LayoutBox == null)
        {
            element.Style ??= new Style();
            switch (property)
            {
                case nameof(Style.Color): element.Style.Color = value; break;
                case nameof(Style.BackgroundColor): element.Style.BackgroundColor = value; break;
                case nameof(Style.BorderColor): element.Style.BorderColor = value; break;
            }
        }
        var values = GetOrCreate(element);
        values.Prepare(element);
        switch (property)
        {
            case nameof(Style.Color): values.Color = value; break;
            case nameof(Style.BackgroundColor): values.BackgroundColor = value; break;
            case nameof(Style.BorderColor):
                values.BorderTopColor = value;
                values.BorderRightColor = value;
                values.BorderBottomColor = value;
                values.BorderLeftColor = value;
                break;
            case nameof(Style.BorderTopColor): values.BorderTopColor = value; break;
            case nameof(Style.BorderRightColor): values.BorderRightColor = value; break;
            case nameof(Style.BorderBottomColor): values.BorderBottomColor = value; break;
            case nameof(Style.BorderLeftColor): values.BorderLeftColor = value; break;
        }
    }

    internal void Apply(Element element)
    {
        if (_values.TryGetValue(element, out var values)) values.Apply(element);
    }

    internal void Migrate(Element oldElement, Element newElement)
    {
        if (ReferenceEquals(oldElement, newElement) || !_values.Remove(oldElement, out var values)) return;
        _values[newElement] = values;
    }

    internal void Remove(Element element)
    {
        if (_values.Remove(element, out var values)) values.Restore();
    }

    internal void RemoveProperty(Element element, string property)
    {
        if (!_values.TryGetValue(element, out var values)) return;
        values.RestoreProperty(property);
        if (values.IsEmpty) _values.Remove(element);
    }

    internal void Clear()
    {
        foreach (var values in _values.Values) values.Restore();
        _values.Clear();
    }
}

public sealed class AnimatedPaintValues
{
    public float? Opacity { get; internal set; }
    public Transform? Transform { get; internal set; }
    public Color? Color { get; internal set; }
    public Color? BackgroundColor { get; internal set; }
    public Color? BorderTopColor { get; internal set; }
    public Color? BorderRightColor { get; internal set; }
    public Color? BorderBottomColor { get; internal set; }
    public Color? BorderLeftColor { get; internal set; }

    internal bool IsEmpty => Opacity == null && Transform == null && Color == null &&
        BackgroundColor == null && BorderTopColor == null && BorderRightColor == null &&
        BorderBottomColor == null && BorderLeftColor == null;

    private ComputedStyle? _appliedStyle;
    private float _baseOpacity;
    private Transform _baseTransform = Transform.None;
    private Color _baseColor;
    private Color _baseBackgroundColor;
    private Color _baseBorderTopColor;
    private Color _baseBorderRightColor;
    private Color _baseBorderBottomColor;
    private Color _baseBorderLeftColor;

    internal void Prepare(Element element)
    {
        var style = element.LayoutBox?.ComputedStyle;
        if (style == null || ReferenceEquals(style, _appliedStyle)) return;

        _appliedStyle = style;
        _baseOpacity = style.Opacity;
        _baseTransform = style.Transform;
        _baseColor = style.Color;
        _baseBackgroundColor = style.BackgroundColor;
        _baseBorderTopColor = style.BorderTopColor;
        _baseBorderRightColor = style.BorderRightColor;
        _baseBorderBottomColor = style.BorderBottomColor;
        _baseBorderLeftColor = style.BorderLeftColor;
    }

    internal void Apply(Element element)
    {
        Prepare(element);
        if (_appliedStyle == null) return;
        if (Opacity is { } opacity) _appliedStyle.Opacity = opacity;
        if (Transform is { } transform) _appliedStyle.Transform = transform;
        if (Color is { } color) _appliedStyle.Color = color;
        if (BackgroundColor is { } background) _appliedStyle.BackgroundColor = background;
        if (BorderTopColor is { } top) _appliedStyle.BorderTopColor = top;
        if (BorderRightColor is { } right) _appliedStyle.BorderRightColor = right;
        if (BorderBottomColor is { } bottom) _appliedStyle.BorderBottomColor = bottom;
        if (BorderLeftColor is { } left) _appliedStyle.BorderLeftColor = left;
    }

    internal void Restore()
    {
        if (_appliedStyle == null) return;
        if (Opacity != null) _appliedStyle.Opacity = _baseOpacity;
        if (Transform != null) _appliedStyle.Transform = _baseTransform;
        if (Color != null) _appliedStyle.Color = _baseColor;
        if (BackgroundColor != null) _appliedStyle.BackgroundColor = _baseBackgroundColor;
        if (BorderTopColor != null) _appliedStyle.BorderTopColor = _baseBorderTopColor;
        if (BorderRightColor != null) _appliedStyle.BorderRightColor = _baseBorderRightColor;
        if (BorderBottomColor != null) _appliedStyle.BorderBottomColor = _baseBorderBottomColor;
        if (BorderLeftColor != null) _appliedStyle.BorderLeftColor = _baseBorderLeftColor;
    }

    internal void RestoreProperty(string property)
    {
        if (_appliedStyle == null)
        {
            switch (property)
            {
                case nameof(Style.Opacity): Opacity = null; break;
                case nameof(Style.Transform): Transform = null; break;
                case nameof(Style.Color): Color = null; break;
                case nameof(Style.BackgroundColor): BackgroundColor = null; break;
                case nameof(Style.BorderColor):
                    BorderTopColor = BorderRightColor = BorderBottomColor = BorderLeftColor = null;
                    break;
                case nameof(Style.BorderTopColor): BorderTopColor = null; break;
                case nameof(Style.BorderRightColor): BorderRightColor = null; break;
                case nameof(Style.BorderBottomColor): BorderBottomColor = null; break;
                case nameof(Style.BorderLeftColor): BorderLeftColor = null; break;
            }
            return;
        }
        switch (property)
        {
            case nameof(Style.Opacity) when Opacity != null: _appliedStyle.Opacity = _baseOpacity; Opacity = null; break;
            case nameof(Style.Transform) when Transform != null: _appliedStyle.Transform = _baseTransform; Transform = null; break;
            case nameof(Style.Color) when Color != null: _appliedStyle.Color = _baseColor; Color = null; break;
            case nameof(Style.BackgroundColor) when BackgroundColor != null: _appliedStyle.BackgroundColor = _baseBackgroundColor; BackgroundColor = null; break;
            case nameof(Style.BorderColor):
                RestoreProperty(nameof(Style.BorderTopColor));
                RestoreProperty(nameof(Style.BorderRightColor));
                RestoreProperty(nameof(Style.BorderBottomColor));
                RestoreProperty(nameof(Style.BorderLeftColor));
                break;
            case nameof(Style.BorderTopColor) when BorderTopColor != null: _appliedStyle.BorderTopColor = _baseBorderTopColor; BorderTopColor = null; break;
            case nameof(Style.BorderRightColor) when BorderRightColor != null: _appliedStyle.BorderRightColor = _baseBorderRightColor; BorderRightColor = null; break;
            case nameof(Style.BorderBottomColor) when BorderBottomColor != null: _appliedStyle.BorderBottomColor = _baseBorderBottomColor; BorderBottomColor = null; break;
            case nameof(Style.BorderLeftColor) when BorderLeftColor != null: _appliedStyle.BorderLeftColor = _baseBorderLeftColor; BorderLeftColor = null; break;
        }
    }
}
