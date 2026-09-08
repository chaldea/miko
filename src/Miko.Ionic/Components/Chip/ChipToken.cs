using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Chip styles.</summary>
public sealed class ChipToken : IonicToken
{
    private float _fontSize = 14f;
    public float FontSize
    {
        get => _fontSize;
        set { _fontSize = value; Mark(nameof(FontSize)); }
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
        var token = (ChipToken)target;
        if (IsSpecified(nameof(FontSize))) token.FontSize = FontSize;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(Color))) token.Color = Color;
        if (IsSpecified(nameof(BorderColor))) token.BorderColor = BorderColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, BorderColor);
        AppendValue(builder, Color);
        AppendValue(builder, FontSize);
    }
}
