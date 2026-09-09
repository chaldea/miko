using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Title styles.</summary>
public sealed class TitleToken : IonicToken
{
    private float _fontSize = 20f;
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

    private float _paddingX = 20f;
    public float PaddingX
    {
        get => _paddingX;
        set { _paddingX = value; Mark(nameof(PaddingX)); }
    }

    private TextAlign _textAlign = TextAlign.Left;
    public TextAlign TextAlign
    {
        get => _textAlign;
        set { _textAlign = value; Mark(nameof(TextAlign)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (TitleToken)target;
        if (IsSpecified(nameof(FontSize))) token.FontSize = FontSize;
        if (IsSpecified(nameof(FontWeight))) token.FontWeight = FontWeight;
        if (IsSpecified(nameof(PaddingX))) token.PaddingX = PaddingX;
        if (IsSpecified(nameof(TextAlign))) token.TextAlign = TextAlign;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, FontSize);
        AppendValue(builder, FontWeight);
        AppendValue(builder, PaddingX);
        AppendValue(builder, TextAlign);
    }
}
