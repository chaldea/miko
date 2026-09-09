using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Button styles.</summary>
public sealed class ButtonToken : IonicToken
{
    private float _borderRadius = 4f;
    public float BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Mark(nameof(BorderRadius)); }
    }

    private float _roundBorderRadius = 999f;
    public float RoundBorderRadius
    {
        get => _roundBorderRadius;
        set { _roundBorderRadius = value; Mark(nameof(RoundBorderRadius)); }
    }

    private Length _minHeight = Length.Px(36);
    public Length MinHeight
    {
        get => _minHeight;
        set { _minHeight = value; Mark(nameof(MinHeight)); }
    }

    private Length _paddingTop = Length.Px(8);
    public Length PaddingTop
    {
        get => _paddingTop;
        set { _paddingTop = value; Mark(nameof(PaddingTop)); }
    }

    private Length _paddingBottom = Length.Px(8);
    public Length PaddingBottom
    {
        get => _paddingBottom;
        set { _paddingBottom = value; Mark(nameof(PaddingBottom)); }
    }

    private Length _paddingStart = Length.Em(1.1f);
    public Length PaddingStart
    {
        get => _paddingStart;
        set { _paddingStart = value; Mark(nameof(PaddingStart)); }
    }

    private Length _paddingEnd = Length.Em(1.1f);
    public Length PaddingEnd
    {
        get => _paddingEnd;
        set { _paddingEnd = value; Mark(nameof(PaddingEnd)); }
    }

    private float _fontSize = 14f;
    public float FontSize
    {
        get => _fontSize;
        set { _fontSize = value; Mark(nameof(FontSize)); }
    }

    private FontWeight _fontWeight = FontWeight.Medium;
    public FontWeight FontWeight
    {
        get => _fontWeight;
        set { _fontWeight = value; Mark(nameof(FontWeight)); }
    }

    private TextTransform _textTransform = TextTransform.None;
    public TextTransform TextTransform
    {
        get => _textTransform;
        set { _textTransform = value; Mark(nameof(TextTransform)); }
    }

    private Length _letterSpacing = Length.Px(0);
    public Length LetterSpacing
    {
        get => _letterSpacing;
        set { _letterSpacing = value; Mark(nameof(LetterSpacing)); }
    }

    private FontWeight _strongFontWeight = FontWeight.Bold;
    public FontWeight StrongFontWeight
    {
        get => _strongFontWeight;
        set { _strongFontWeight = value; Mark(nameof(StrongFontWeight)); }
    }

    private List<BoxShadow> _solidBoxShadow = new();
    public List<BoxShadow> SolidBoxShadow
    {
        get { Mark(nameof(SolidBoxShadow)); return _solidBoxShadow; }
        set { _solidBoxShadow = value; Mark(nameof(SolidBoxShadow)); }
    }

    private Length _outlineBorderWidth = Length.Px(1);
    public Length OutlineBorderWidth
    {
        get => _outlineBorderWidth;
        set { _outlineBorderWidth = value; Mark(nameof(OutlineBorderWidth)); }
    }

    private Length _largeMinHeight = Length.Em(2.8f);
    public Length LargeMinHeight
    {
        get => _largeMinHeight;
        set { _largeMinHeight = value; Mark(nameof(LargeMinHeight)); }
    }

    private Length _largePaddingTop = Length.Px(14);
    public Length LargePaddingTop
    {
        get => _largePaddingTop;
        set { _largePaddingTop = value; Mark(nameof(LargePaddingTop)); }
    }

    private Length _largePaddingBottom = Length.Px(14);
    public Length LargePaddingBottom
    {
        get => _largePaddingBottom;
        set { _largePaddingBottom = value; Mark(nameof(LargePaddingBottom)); }
    }

    private Length _largePaddingX = Length.Em(1f);
    public Length LargePaddingX
    {
        get => _largePaddingX;
        set { _largePaddingX = value; Mark(nameof(LargePaddingX)); }
    }

    private float _largeFontSize = 20f;
    public float LargeFontSize
    {
        get => _largeFontSize;
        set { _largeFontSize = value; Mark(nameof(LargeFontSize)); }
    }

    private float _largeBorderRadius = 4f;
    public float LargeBorderRadius
    {
        get => _largeBorderRadius;
        set { _largeBorderRadius = value; Mark(nameof(LargeBorderRadius)); }
    }

    private Length _smallMinHeight = Length.Em(2.1f);
    public Length SmallMinHeight
    {
        get => _smallMinHeight;
        set { _smallMinHeight = value; Mark(nameof(SmallMinHeight)); }
    }

    private Length _smallPaddingTop = Length.Px(4);
    public Length SmallPaddingTop
    {
        get => _smallPaddingTop;
        set { _smallPaddingTop = value; Mark(nameof(SmallPaddingTop)); }
    }

    private Length _smallPaddingBottom = Length.Px(4);
    public Length SmallPaddingBottom
    {
        get => _smallPaddingBottom;
        set { _smallPaddingBottom = value; Mark(nameof(SmallPaddingBottom)); }
    }

    private Length _smallPaddingX = Length.Em(0.9f);
    public Length SmallPaddingX
    {
        get => _smallPaddingX;
        set { _smallPaddingX = value; Mark(nameof(SmallPaddingX)); }
    }

    private float _smallFontSize = 13f;
    public float SmallFontSize
    {
        get => _smallFontSize;
        set { _smallFontSize = value; Mark(nameof(SmallFontSize)); }
    }

    private float _smallBorderRadius = 4f;
    public float SmallBorderRadius
    {
        get => _smallBorderRadius;
        set { _smallBorderRadius = value; Mark(nameof(SmallBorderRadius)); }
    }

    private float _iconOnlyMinSize = 40f;
    public float IconOnlyMinSize
    {
        get => _iconOnlyMinSize;
        set { _iconOnlyMinSize = value; Mark(nameof(IconOnlyMinSize)); }
    }

    private float _iconOnlyIconSize = 22.4f;
    public float IconOnlyIconSize
    {
        get => _iconOnlyIconSize;
        set { _iconOnlyIconSize = value; Mark(nameof(IconOnlyIconSize)); }
    }

    private float _smallIconOnlyMinSize = 28f;
    public float SmallIconOnlyMinSize
    {
        get => _smallIconOnlyMinSize;
        set { _smallIconOnlyMinSize = value; Mark(nameof(SmallIconOnlyMinSize)); }
    }

    private float _smallIconOnlyIconSize = 16f;
    public float SmallIconOnlyIconSize
    {
        get => _smallIconOnlyIconSize;
        set { _smallIconOnlyIconSize = value; Mark(nameof(SmallIconOnlyIconSize)); }
    }

    private float _largeIconOnlyMinSize = 50f;
    public float LargeIconOnlyMinSize
    {
        get => _largeIconOnlyMinSize;
        set { _largeIconOnlyMinSize = value; Mark(nameof(LargeIconOnlyMinSize)); }
    }

    private float _largeIconOnlyIconSize = 28f;
    public float LargeIconOnlyIconSize
    {
        get => _largeIconOnlyIconSize;
        set { _largeIconOnlyIconSize = value; Mark(nameof(LargeIconOnlyIconSize)); }
    }

    /// <summary>Convenience alias for the default solid button color.</summary>
    public Color Color { get => SolidBackground; set => SolidBackground = value; }

    private Color _solidBackground = default;
    public Color SolidBackground
    {
        get => _solidBackground;
        set { _solidBackground = value; Mark(nameof(SolidBackground)); }
    }

    private Color _solidColor = default;
    public Color SolidColor
    {
        get => _solidColor;
        set { _solidColor = value; Mark(nameof(SolidColor)); }
    }

    private Color _textColor = default;
    public Color TextColor
    {
        get => _textColor;
        set { _textColor = value; Mark(nameof(TextColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (ButtonToken)target;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(RoundBorderRadius))) token.RoundBorderRadius = RoundBorderRadius;
        if (IsSpecified(nameof(MinHeight))) token.MinHeight = MinHeight;
        if (IsSpecified(nameof(PaddingTop))) token.PaddingTop = PaddingTop;
        if (IsSpecified(nameof(PaddingBottom))) token.PaddingBottom = PaddingBottom;
        if (IsSpecified(nameof(PaddingStart))) token.PaddingStart = PaddingStart;
        if (IsSpecified(nameof(PaddingEnd))) token.PaddingEnd = PaddingEnd;
        if (IsSpecified(nameof(FontSize))) token.FontSize = FontSize;
        if (IsSpecified(nameof(FontWeight))) token.FontWeight = FontWeight;
        if (IsSpecified(nameof(TextTransform))) token.TextTransform = TextTransform;
        if (IsSpecified(nameof(LetterSpacing))) token.LetterSpacing = LetterSpacing;
        if (IsSpecified(nameof(StrongFontWeight))) token.StrongFontWeight = StrongFontWeight;
        if (IsSpecified(nameof(SolidBoxShadow))) token.SolidBoxShadow = new(SolidBoxShadow);
        if (IsSpecified(nameof(OutlineBorderWidth))) token.OutlineBorderWidth = OutlineBorderWidth;
        if (IsSpecified(nameof(LargeMinHeight))) token.LargeMinHeight = LargeMinHeight;
        if (IsSpecified(nameof(LargePaddingTop))) token.LargePaddingTop = LargePaddingTop;
        if (IsSpecified(nameof(LargePaddingBottom))) token.LargePaddingBottom = LargePaddingBottom;
        if (IsSpecified(nameof(LargePaddingX))) token.LargePaddingX = LargePaddingX;
        if (IsSpecified(nameof(LargeFontSize))) token.LargeFontSize = LargeFontSize;
        if (IsSpecified(nameof(LargeBorderRadius))) token.LargeBorderRadius = LargeBorderRadius;
        if (IsSpecified(nameof(SmallMinHeight))) token.SmallMinHeight = SmallMinHeight;
        if (IsSpecified(nameof(SmallPaddingTop))) token.SmallPaddingTop = SmallPaddingTop;
        if (IsSpecified(nameof(SmallPaddingBottom))) token.SmallPaddingBottom = SmallPaddingBottom;
        if (IsSpecified(nameof(SmallPaddingX))) token.SmallPaddingX = SmallPaddingX;
        if (IsSpecified(nameof(SmallFontSize))) token.SmallFontSize = SmallFontSize;
        if (IsSpecified(nameof(SmallBorderRadius))) token.SmallBorderRadius = SmallBorderRadius;
        if (IsSpecified(nameof(IconOnlyMinSize))) token.IconOnlyMinSize = IconOnlyMinSize;
        if (IsSpecified(nameof(IconOnlyIconSize))) token.IconOnlyIconSize = IconOnlyIconSize;
        if (IsSpecified(nameof(SmallIconOnlyMinSize))) token.SmallIconOnlyMinSize = SmallIconOnlyMinSize;
        if (IsSpecified(nameof(SmallIconOnlyIconSize))) token.SmallIconOnlyIconSize = SmallIconOnlyIconSize;
        if (IsSpecified(nameof(LargeIconOnlyMinSize))) token.LargeIconOnlyMinSize = LargeIconOnlyMinSize;
        if (IsSpecified(nameof(LargeIconOnlyIconSize))) token.LargeIconOnlyIconSize = LargeIconOnlyIconSize;
        if (IsSpecified(nameof(Color))) token.Color = Color;
        if (IsSpecified(nameof(SolidBackground))) token.SolidBackground = SolidBackground;
        if (IsSpecified(nameof(SolidColor))) token.SolidColor = SolidColor;
        if (IsSpecified(nameof(TextColor))) token.TextColor = TextColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, BorderRadius);
        AppendValue(builder, Color);
        AppendValue(builder, FontSize);
        AppendValue(builder, FontWeight);
        AppendValue(builder, IconOnlyIconSize);
        AppendValue(builder, IconOnlyMinSize);
        AppendValue(builder, LargeBorderRadius);
        AppendValue(builder, LargeFontSize);
        AppendValue(builder, LargeIconOnlyIconSize);
        AppendValue(builder, LargeIconOnlyMinSize);
        AppendValue(builder, LargeMinHeight);
        AppendValue(builder, LargePaddingBottom);
        AppendValue(builder, LargePaddingTop);
        AppendValue(builder, LargePaddingX);
        AppendValue(builder, LetterSpacing);
        AppendValue(builder, MinHeight);
        AppendValue(builder, OutlineBorderWidth);
        AppendValue(builder, PaddingBottom);
        AppendValue(builder, PaddingEnd);
        AppendValue(builder, PaddingStart);
        AppendValue(builder, PaddingTop);
        AppendValue(builder, RoundBorderRadius);
        AppendValue(builder, SmallBorderRadius);
        AppendValue(builder, SmallFontSize);
        AppendValue(builder, SmallIconOnlyIconSize);
        AppendValue(builder, SmallIconOnlyMinSize);
        AppendValue(builder, SmallMinHeight);
        AppendValue(builder, SmallPaddingBottom);
        AppendValue(builder, SmallPaddingTop);
        AppendValue(builder, SmallPaddingX);
        AppendValue(builder, SolidBackground);
        AppendValue(builder, SolidBoxShadow);
        AppendValue(builder, SolidColor);
        AppendValue(builder, StrongFontWeight);
        AppendValue(builder, TextColor);
        AppendValue(builder, TextTransform);
    }
}
