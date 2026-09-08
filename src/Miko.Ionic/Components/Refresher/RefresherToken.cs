using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Refresher styles.</summary>
public sealed class RefresherToken : IonicToken
{
    private float _height = 60f;
    public float Height
    {
        get => _height;
        set { _height = value; Mark(nameof(Height)); }
    }

    private float _iconFontSize = 30f;
    public float IconFontSize
    {
        get => _iconFontSize;
        set { _iconFontSize = value; Mark(nameof(IconFontSize)); }
    }

    private float _textFontSize = 16f;
    public float TextFontSize
    {
        get => _textFontSize;
        set { _textFontSize = value; Mark(nameof(TextFontSize)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (RefresherToken)target;
        if (IsSpecified(nameof(Height))) token.Height = Height;
        if (IsSpecified(nameof(IconFontSize))) token.IconFontSize = IconFontSize;
        if (IsSpecified(nameof(TextFontSize))) token.TextFontSize = TextFontSize;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Height);
        AppendValue(builder, IconFontSize);
        AppendValue(builder, TextFontSize);
    }
}
