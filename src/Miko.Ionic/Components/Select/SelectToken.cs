using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Select styles.</summary>
public sealed class SelectToken : IonicToken
{
    private float _fontSize = 16f;
    public float FontSize
    {
        get => _fontSize;
        set { _fontSize = value; Mark(nameof(FontSize)); }
    }

    private float _minHeight = 48f;
    public float MinHeight
    {
        get => _minHeight;
        set { _minHeight = value; Mark(nameof(MinHeight)); }
    }

    private float _paddingTop = 0f;
    public float PaddingTop
    {
        get => _paddingTop;
        set { _paddingTop = value; Mark(nameof(PaddingTop)); }
    }

    private float _paddingEnd = 0f;
    public float PaddingEnd
    {
        get => _paddingEnd;
        set { _paddingEnd = value; Mark(nameof(PaddingEnd)); }
    }

    private float _paddingBottom = 0f;
    public float PaddingBottom
    {
        get => _paddingBottom;
        set { _paddingBottom = value; Mark(nameof(PaddingBottom)); }
    }

    private float _paddingStart = 0f;
    public float PaddingStart
    {
        get => _paddingStart;
        set { _paddingStart = value; Mark(nameof(PaddingStart)); }
    }

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

    private Color _textColor = default;
    public Color TextColor
    {
        get => _textColor;
        set { _textColor = value; Mark(nameof(TextColor)); }
    }

    private Color _placeholderColor = default;
    public Color PlaceholderColor
    {
        get => _placeholderColor;
        set { _placeholderColor = value; Mark(nameof(PlaceholderColor)); }
    }

    private Color _labelColor = default;
    public Color LabelColor
    {
        get => _labelColor;
        set { _labelColor = value; Mark(nameof(LabelColor)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    private Color _borderColor = default;
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Mark(nameof(BorderColor)); }
    }

    private Color _highlightColor = default;
    public Color HighlightColor
    {
        get => _highlightColor;
        set { _highlightColor = value; Mark(nameof(HighlightColor)); }
    }

    private Color _helperColor = default;
    public Color HelperColor
    {
        get => _helperColor;
        set { _helperColor = value; Mark(nameof(HelperColor)); }
    }

    private Color _errorColor = default;
    public Color ErrorColor
    {
        get => _errorColor;
        set { _errorColor = value; Mark(nameof(ErrorColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (SelectToken)target;
        if (IsSpecified(nameof(FontSize))) token.FontSize = FontSize;
        if (IsSpecified(nameof(MinHeight))) token.MinHeight = MinHeight;
        if (IsSpecified(nameof(PaddingTop))) token.PaddingTop = PaddingTop;
        if (IsSpecified(nameof(PaddingEnd))) token.PaddingEnd = PaddingEnd;
        if (IsSpecified(nameof(PaddingBottom))) token.PaddingBottom = PaddingBottom;
        if (IsSpecified(nameof(PaddingStart))) token.PaddingStart = PaddingStart;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(RoundBorderRadius))) token.RoundBorderRadius = RoundBorderRadius;
        if (IsSpecified(nameof(TextColor))) token.TextColor = TextColor;
        if (IsSpecified(nameof(PlaceholderColor))) token.PlaceholderColor = PlaceholderColor;
        if (IsSpecified(nameof(LabelColor))) token.LabelColor = LabelColor;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(BorderColor))) token.BorderColor = BorderColor;
        if (IsSpecified(nameof(HighlightColor))) token.HighlightColor = HighlightColor;
        if (IsSpecified(nameof(HelperColor))) token.HelperColor = HelperColor;
        if (IsSpecified(nameof(ErrorColor))) token.ErrorColor = ErrorColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, BorderColor);
        AppendValue(builder, BorderRadius);
        AppendValue(builder, ErrorColor);
        AppendValue(builder, FontSize);
        AppendValue(builder, HelperColor);
        AppendValue(builder, HighlightColor);
        AppendValue(builder, LabelColor);
        AppendValue(builder, MinHeight);
        AppendValue(builder, PaddingBottom);
        AppendValue(builder, PaddingEnd);
        AppendValue(builder, PaddingStart);
        AppendValue(builder, PaddingTop);
        AppendValue(builder, PlaceholderColor);
        AppendValue(builder, RoundBorderRadius);
        AppendValue(builder, TextColor);
    }
}
