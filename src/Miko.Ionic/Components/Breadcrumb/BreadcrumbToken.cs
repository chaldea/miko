using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Breadcrumb styles.</summary>
public sealed class BreadcrumbToken : IonicToken
{
    private float _fontSize = 16f;
    public float FontSize
    {
        get => _fontSize;
        set { _fontSize = value; Mark(nameof(FontSize)); }
    }

    private FontWeight _activeFontWeight = FontWeight.Medium;
    public FontWeight ActiveFontWeight
    {
        get => _activeFontWeight;
        set { _activeFontWeight = value; Mark(nameof(ActiveFontWeight)); }
    }

    private float _paddingY = 6f;
    public float PaddingY
    {
        get => _paddingY;
        set { _paddingY = value; Mark(nameof(PaddingY)); }
    }

    private float _paddingX = 12f;
    public float PaddingX
    {
        get => _paddingX;
        set { _paddingX = value; Mark(nameof(PaddingX)); }
    }

    private float _separatorMarginX = 10f;
    public float SeparatorMarginX
    {
        get => _separatorMarginX;
        set { _separatorMarginX = value; Mark(nameof(SeparatorMarginX)); }
    }

    private float _iconFontSize = 18f;
    public float IconFontSize
    {
        get => _iconFontSize;
        set { _iconFontSize = value; Mark(nameof(IconFontSize)); }
    }

    private float _iconSlotMargin = 8f;
    public float IconSlotMargin
    {
        get => _iconSlotMargin;
        set { _iconSlotMargin = value; Mark(nameof(IconSlotMargin)); }
    }

    private float _indicatorWidth = 32f;
    public float IndicatorWidth
    {
        get => _indicatorWidth;
        set { _indicatorWidth = value; Mark(nameof(IndicatorWidth)); }
    }

    private float _indicatorHeight = 18f;
    public float IndicatorHeight
    {
        get => _indicatorHeight;
        set { _indicatorHeight = value; Mark(nameof(IndicatorHeight)); }
    }

    private float _indicatorMarginX = 14f;
    public float IndicatorMarginX
    {
        get => _indicatorMarginX;
        set { _indicatorMarginX = value; Mark(nameof(IndicatorMarginX)); }
    }

    private float _indicatorBorderRadius = 2f;
    public float IndicatorBorderRadius
    {
        get => _indicatorBorderRadius;
        set { _indicatorBorderRadius = value; Mark(nameof(IndicatorBorderRadius)); }
    }

    private float _indicatorIconSize = 22f;
    public float IndicatorIconSize
    {
        get => _indicatorIconSize;
        set { _indicatorIconSize = value; Mark(nameof(IndicatorIconSize)); }
    }

    private Color _color = default;
    public Color Color
    {
        get => _color;
        set { _color = value; Mark(nameof(Color)); }
    }

    private Color _colorActive = default;
    public Color ColorActive
    {
        get => _colorActive;
        set { _colorActive = value; Mark(nameof(ColorActive)); }
    }

    private float _borderRadius = default;
    public float BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Mark(nameof(BorderRadius)); }
    }

    private Color _separatorColor = default;
    public Color SeparatorColor
    {
        get => _separatorColor;
        set { _separatorColor = value; Mark(nameof(SeparatorColor)); }
    }

    private Color _iconColor = default;
    public Color IconColor
    {
        get => _iconColor;
        set { _iconColor = value; Mark(nameof(IconColor)); }
    }

    private Color _iconColorActive = default;
    public Color IconColorActive
    {
        get => _iconColorActive;
        set { _iconColorActive = value; Mark(nameof(IconColorActive)); }
    }

    private Color _indicatorBackground = default;
    public Color IndicatorBackground
    {
        get => _indicatorBackground;
        set { _indicatorBackground = value; Mark(nameof(IndicatorBackground)); }
    }

    private Color _indicatorColor = default;
    public Color IndicatorColor
    {
        get => _indicatorColor;
        set { _indicatorColor = value; Mark(nameof(IndicatorColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (BreadcrumbToken)target;
        if (IsSpecified(nameof(FontSize))) token.FontSize = FontSize;
        if (IsSpecified(nameof(ActiveFontWeight))) token.ActiveFontWeight = ActiveFontWeight;
        if (IsSpecified(nameof(PaddingY))) token.PaddingY = PaddingY;
        if (IsSpecified(nameof(PaddingX))) token.PaddingX = PaddingX;
        if (IsSpecified(nameof(SeparatorMarginX))) token.SeparatorMarginX = SeparatorMarginX;
        if (IsSpecified(nameof(IconFontSize))) token.IconFontSize = IconFontSize;
        if (IsSpecified(nameof(IconSlotMargin))) token.IconSlotMargin = IconSlotMargin;
        if (IsSpecified(nameof(IndicatorWidth))) token.IndicatorWidth = IndicatorWidth;
        if (IsSpecified(nameof(IndicatorHeight))) token.IndicatorHeight = IndicatorHeight;
        if (IsSpecified(nameof(IndicatorMarginX))) token.IndicatorMarginX = IndicatorMarginX;
        if (IsSpecified(nameof(IndicatorBorderRadius))) token.IndicatorBorderRadius = IndicatorBorderRadius;
        if (IsSpecified(nameof(IndicatorIconSize))) token.IndicatorIconSize = IndicatorIconSize;
        if (IsSpecified(nameof(Color))) token.Color = Color;
        if (IsSpecified(nameof(ColorActive))) token.ColorActive = ColorActive;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(SeparatorColor))) token.SeparatorColor = SeparatorColor;
        if (IsSpecified(nameof(IconColor))) token.IconColor = IconColor;
        if (IsSpecified(nameof(IconColorActive))) token.IconColorActive = IconColorActive;
        if (IsSpecified(nameof(IndicatorBackground))) token.IndicatorBackground = IndicatorBackground;
        if (IsSpecified(nameof(IndicatorColor))) token.IndicatorColor = IndicatorColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, ActiveFontWeight);
        AppendValue(builder, BorderRadius);
        AppendValue(builder, Color);
        AppendValue(builder, ColorActive);
        AppendValue(builder, FontSize);
        AppendValue(builder, IconColor);
        AppendValue(builder, IconColorActive);
        AppendValue(builder, IconFontSize);
        AppendValue(builder, IconSlotMargin);
        AppendValue(builder, IndicatorBackground);
        AppendValue(builder, IndicatorBorderRadius);
        AppendValue(builder, IndicatorColor);
        AppendValue(builder, IndicatorHeight);
        AppendValue(builder, IndicatorIconSize);
        AppendValue(builder, IndicatorMarginX);
        AppendValue(builder, IndicatorWidth);
        AppendValue(builder, PaddingX);
        AppendValue(builder, PaddingY);
        AppendValue(builder, SeparatorColor);
        AppendValue(builder, SeparatorMarginX);
    }
}
