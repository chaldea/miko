using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Toolbar styles.</summary>
public sealed class ToolbarToken : IonicToken
{
    private float _minHeight = 56f;
    public float MinHeight
    {
        get => _minHeight;
        set { _minHeight = value; Mark(nameof(MinHeight)); }
    }

    private Length _paddingTop = Length.Px(0);
    public Length PaddingTop
    {
        get => _paddingTop;
        set { _paddingTop = value; Mark(nameof(PaddingTop)); }
    }

    private Length _paddingBottom = Length.Px(0);
    public Length PaddingBottom
    {
        get => _paddingBottom;
        set { _paddingBottom = value; Mark(nameof(PaddingBottom)); }
    }

    private Length _paddingStart = Length.Px(0);
    public Length PaddingStart
    {
        get => _paddingStart;
        set { _paddingStart = value; Mark(nameof(PaddingStart)); }
    }

    private Length _paddingEnd = Length.Px(0);
    public Length PaddingEnd
    {
        get => _paddingEnd;
        set { _paddingEnd = value; Mark(nameof(PaddingEnd)); }
    }

    private Length _buttonMinHeight = Length.Px(32);
    public Length ButtonMinHeight
    {
        get => _buttonMinHeight;
        set { _buttonMinHeight = value; Mark(nameof(ButtonMinHeight)); }
    }

    private Length _buttonPaddingTop = Length.Px(3);
    public Length ButtonPaddingTop
    {
        get => _buttonPaddingTop;
        set { _buttonPaddingTop = value; Mark(nameof(ButtonPaddingTop)); }
    }

    private Length _buttonPaddingBottom = Length.Px(3);
    public Length ButtonPaddingBottom
    {
        get => _buttonPaddingBottom;
        set { _buttonPaddingBottom = value; Mark(nameof(ButtonPaddingBottom)); }
    }

    private Length _buttonPaddingStart = Length.Px(8);
    public Length ButtonPaddingStart
    {
        get => _buttonPaddingStart;
        set { _buttonPaddingStart = value; Mark(nameof(ButtonPaddingStart)); }
    }

    private Length _buttonPaddingEnd = Length.Px(8);
    public Length ButtonPaddingEnd
    {
        get => _buttonPaddingEnd;
        set { _buttonPaddingEnd = value; Mark(nameof(ButtonPaddingEnd)); }
    }

    private float _buttonMarginX = 2f;
    public float ButtonMarginX
    {
        get => _buttonMarginX;
        set { _buttonMarginX = value; Mark(nameof(ButtonMarginX)); }
    }

    private float _buttonBorderRadius = 2f;
    public float ButtonBorderRadius
    {
        get => _buttonBorderRadius;
        set { _buttonBorderRadius = value; Mark(nameof(ButtonBorderRadius)); }
    }

    private float _buttonIconFontSize = 1.4f;
    public float ButtonIconFontSize
    {
        get => _buttonIconFontSize;
        set { _buttonIconFontSize = value; Mark(nameof(ButtonIconFontSize)); }
    }

    private float _buttonIconOnlyFontSize = 1.8f;
    public float ButtonIconOnlyFontSize
    {
        get => _buttonIconOnlyFontSize;
        set { _buttonIconOnlyFontSize = value; Mark(nameof(ButtonIconOnlyFontSize)); }
    }

    private float _buttonIconOnlyClearSize = 48f;
    public float ButtonIconOnlyClearSize
    {
        get => _buttonIconOnlyClearSize;
        set { _buttonIconOnlyClearSize = value; Mark(nameof(ButtonIconOnlyClearSize)); }
    }

    private Length _buttonIconOnlyClearPadding = Length.Px(12);
    public Length ButtonIconOnlyClearPadding
    {
        get => _buttonIconOnlyClearPadding;
        set { _buttonIconOnlyClearPadding = value; Mark(nameof(ButtonIconOnlyClearPadding)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    private Color _color = default;
    public Color Color
    {
        get => _color;
        set { _color = value; Mark(nameof(Color)); }
    }

    private Color _borderColor = default;
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Mark(nameof(BorderColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (ToolbarToken)target;
        if (IsSpecified(nameof(MinHeight))) token.MinHeight = MinHeight;
        if (IsSpecified(nameof(PaddingTop))) token.PaddingTop = PaddingTop;
        if (IsSpecified(nameof(PaddingBottom))) token.PaddingBottom = PaddingBottom;
        if (IsSpecified(nameof(PaddingStart))) token.PaddingStart = PaddingStart;
        if (IsSpecified(nameof(PaddingEnd))) token.PaddingEnd = PaddingEnd;
        if (IsSpecified(nameof(ButtonMinHeight))) token.ButtonMinHeight = ButtonMinHeight;
        if (IsSpecified(nameof(ButtonPaddingTop))) token.ButtonPaddingTop = ButtonPaddingTop;
        if (IsSpecified(nameof(ButtonPaddingBottom))) token.ButtonPaddingBottom = ButtonPaddingBottom;
        if (IsSpecified(nameof(ButtonPaddingStart))) token.ButtonPaddingStart = ButtonPaddingStart;
        if (IsSpecified(nameof(ButtonPaddingEnd))) token.ButtonPaddingEnd = ButtonPaddingEnd;
        if (IsSpecified(nameof(ButtonMarginX))) token.ButtonMarginX = ButtonMarginX;
        if (IsSpecified(nameof(ButtonBorderRadius))) token.ButtonBorderRadius = ButtonBorderRadius;
        if (IsSpecified(nameof(ButtonIconFontSize))) token.ButtonIconFontSize = ButtonIconFontSize;
        if (IsSpecified(nameof(ButtonIconOnlyFontSize))) token.ButtonIconOnlyFontSize = ButtonIconOnlyFontSize;
        if (IsSpecified(nameof(ButtonIconOnlyClearSize))) token.ButtonIconOnlyClearSize = ButtonIconOnlyClearSize;
        if (IsSpecified(nameof(ButtonIconOnlyClearPadding))) token.ButtonIconOnlyClearPadding = ButtonIconOnlyClearPadding;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(Color))) token.Color = Color;
        if (IsSpecified(nameof(BorderColor))) token.BorderColor = BorderColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, BorderColor);
        AppendValue(builder, ButtonBorderRadius);
        AppendValue(builder, ButtonIconFontSize);
        AppendValue(builder, ButtonIconOnlyClearPadding);
        AppendValue(builder, ButtonIconOnlyClearSize);
        AppendValue(builder, ButtonIconOnlyFontSize);
        AppendValue(builder, ButtonMarginX);
        AppendValue(builder, ButtonMinHeight);
        AppendValue(builder, ButtonPaddingBottom);
        AppendValue(builder, ButtonPaddingEnd);
        AppendValue(builder, ButtonPaddingStart);
        AppendValue(builder, ButtonPaddingTop);
        AppendValue(builder, Color);
        AppendValue(builder, MinHeight);
        AppendValue(builder, PaddingBottom);
        AppendValue(builder, PaddingEnd);
        AppendValue(builder, PaddingStart);
        AppendValue(builder, PaddingTop);
    }
}
