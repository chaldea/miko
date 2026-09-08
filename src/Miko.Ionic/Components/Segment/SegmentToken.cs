using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Segment styles.</summary>
public sealed class SegmentToken : IonicToken
{
    private Length _indicatorHeight = Length.Px(2);
    public Length IndicatorHeight
    {
        get => _indicatorHeight;
        set { _indicatorHeight = value; Mark(nameof(IndicatorHeight)); }
    }

    private List<BoxShadow> _indicatorBoxShadow = new();
    public List<BoxShadow> IndicatorBoxShadow
    {
        get { Mark(nameof(IndicatorBoxShadow)); return _indicatorBoxShadow; }
        set { _indicatorBoxShadow = value; Mark(nameof(IndicatorBoxShadow)); }
    }

    private float _buttonFontSize = 14f;
    public float ButtonFontSize
    {
        get => _buttonFontSize;
        set { _buttonFontSize = value; Mark(nameof(ButtonFontSize)); }
    }

    private float _buttonMinWidth = 90f;
    public float ButtonMinWidth
    {
        get => _buttonMinWidth;
        set { _buttonMinWidth = value; Mark(nameof(ButtonMinWidth)); }
    }

    private float _buttonMinHeight = 48f;
    public float ButtonMinHeight
    {
        get => _buttonMinHeight;
        set { _buttonMinHeight = value; Mark(nameof(ButtonMinHeight)); }
    }

    private float _buttonLineHeight = 40f;
    public float ButtonLineHeight
    {
        get => _buttonLineHeight;
        set { _buttonLineHeight = value; Mark(nameof(ButtonLineHeight)); }
    }

    private Length _buttonLetterSpacing = Length.Em(0.06f);
    public Length ButtonLetterSpacing
    {
        get => _buttonLetterSpacing;
        set { _buttonLetterSpacing = value; Mark(nameof(ButtonLetterSpacing)); }
    }

    private float _buttonPaddingX = 16f;
    public float ButtonPaddingX
    {
        get => _buttonPaddingX;
        set { _buttonPaddingX = value; Mark(nameof(ButtonPaddingX)); }
    }

    private float _buttonPaddingY = 0f;
    public float ButtonPaddingY
    {
        get => _buttonPaddingY;
        set { _buttonPaddingY = value; Mark(nameof(ButtonPaddingY)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    private float _borderRadius = default;
    public float BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Mark(nameof(BorderRadius)); }
    }

    private Color _buttonColor = default;
    public Color ButtonColor
    {
        get => _buttonColor;
        set { _buttonColor = value; Mark(nameof(ButtonColor)); }
    }

    private Color _buttonCheckedColor = default;
    public Color ButtonCheckedColor
    {
        get => _buttonCheckedColor;
        set { _buttonCheckedColor = value; Mark(nameof(ButtonCheckedColor)); }
    }

    private Color _indicatorColor = default;
    public Color IndicatorColor
    {
        get => _indicatorColor;
        set { _indicatorColor = value; Mark(nameof(IndicatorColor)); }
    }

    private float _indicatorBorderRadius = default;
    public float IndicatorBorderRadius
    {
        get => _indicatorBorderRadius;
        set { _indicatorBorderRadius = value; Mark(nameof(IndicatorBorderRadius)); }
    }

    private float _buttonMarginY = default;
    public float ButtonMarginY
    {
        get => _buttonMarginY;
        set { _buttonMarginY = value; Mark(nameof(ButtonMarginY)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (SegmentToken)target;
        if (IsSpecified(nameof(IndicatorHeight))) token.IndicatorHeight = IndicatorHeight;
        if (IsSpecified(nameof(IndicatorBoxShadow))) token.IndicatorBoxShadow = new(IndicatorBoxShadow);
        if (IsSpecified(nameof(ButtonFontSize))) token.ButtonFontSize = ButtonFontSize;
        if (IsSpecified(nameof(ButtonMinWidth))) token.ButtonMinWidth = ButtonMinWidth;
        if (IsSpecified(nameof(ButtonMinHeight))) token.ButtonMinHeight = ButtonMinHeight;
        if (IsSpecified(nameof(ButtonLineHeight))) token.ButtonLineHeight = ButtonLineHeight;
        if (IsSpecified(nameof(ButtonLetterSpacing))) token.ButtonLetterSpacing = ButtonLetterSpacing;
        if (IsSpecified(nameof(ButtonPaddingX))) token.ButtonPaddingX = ButtonPaddingX;
        if (IsSpecified(nameof(ButtonPaddingY))) token.ButtonPaddingY = ButtonPaddingY;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(ButtonColor))) token.ButtonColor = ButtonColor;
        if (IsSpecified(nameof(ButtonCheckedColor))) token.ButtonCheckedColor = ButtonCheckedColor;
        if (IsSpecified(nameof(IndicatorColor))) token.IndicatorColor = IndicatorColor;
        if (IsSpecified(nameof(IndicatorBorderRadius))) token.IndicatorBorderRadius = IndicatorBorderRadius;
        if (IsSpecified(nameof(ButtonMarginY))) token.ButtonMarginY = ButtonMarginY;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, BorderRadius);
        AppendValue(builder, ButtonCheckedColor);
        AppendValue(builder, ButtonColor);
        AppendValue(builder, ButtonFontSize);
        AppendValue(builder, ButtonLetterSpacing);
        AppendValue(builder, ButtonLineHeight);
        AppendValue(builder, ButtonMarginY);
        AppendValue(builder, ButtonMinHeight);
        AppendValue(builder, ButtonMinWidth);
        AppendValue(builder, ButtonPaddingX);
        AppendValue(builder, ButtonPaddingY);
        AppendValue(builder, IndicatorBorderRadius);
        AppendValue(builder, IndicatorBoxShadow);
        AppendValue(builder, IndicatorColor);
        AppendValue(builder, IndicatorHeight);
    }
}
