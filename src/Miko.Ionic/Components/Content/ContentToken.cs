using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Content styles.</summary>
public sealed class ContentToken : IonicToken
{
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

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (ContentToken)target;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(Color))) token.Color = Color;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, Color);
    }
}
