using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Note styles.</summary>
public sealed class NoteToken : IonicToken
{
    private Color _color = Color.FromHex("666666");
    public Color Color
    {
        get => _color;
        set { _color = value; Mark(nameof(Color)); }
    }

    private float _fontSize = 14f;
    public float FontSize
    {
        get => _fontSize;
        set { _fontSize = value; Mark(nameof(FontSize)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (NoteToken)target;
        if (IsSpecified(nameof(Color))) token.Color = Color;
        if (IsSpecified(nameof(FontSize))) token.FontSize = FontSize;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Color);
        AppendValue(builder, FontSize);
    }
}
