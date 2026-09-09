using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Label styles.</summary>
public sealed class LabelToken : IonicToken
{
    private float _textWrapLineHeight = 1.5f;
    public float TextWrapLineHeight
    {
        get => _textWrapLineHeight;
        set { _textWrapLineHeight = value; Mark(nameof(TextWrapLineHeight)); }
    }

    private float? _textWrapFontSize;
    public float? TextWrapFontSize
    {
        get => _textWrapFontSize;
        set { _textWrapFontSize = value; Mark(nameof(TextWrapFontSize)); }
    }

    private float _stackedMarginBottom = default;
    public float StackedMarginBottom
    {
        get => _stackedMarginBottom;
        set { _stackedMarginBottom = value; Mark(nameof(StackedMarginBottom)); }
    }

    private float? _stackedFontSize;
    public float? StackedFontSize
    {
        get => _stackedFontSize;
        set { _stackedFontSize = value; Mark(nameof(StackedFontSize)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (LabelToken)target;
        if (IsSpecified(nameof(TextWrapLineHeight))) token.TextWrapLineHeight = TextWrapLineHeight;
        if (IsSpecified(nameof(TextWrapFontSize))) token.TextWrapFontSize = TextWrapFontSize;
        if (IsSpecified(nameof(StackedMarginBottom))) token.StackedMarginBottom = StackedMarginBottom;
        if (IsSpecified(nameof(StackedFontSize))) token.StackedFontSize = StackedFontSize;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, StackedFontSize);
        AppendValue(builder, StackedMarginBottom);
        AppendValue(builder, TextWrapFontSize);
        AppendValue(builder, TextWrapLineHeight);
    }
}
