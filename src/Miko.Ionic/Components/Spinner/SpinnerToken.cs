using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Spinner styles.</summary>
public sealed class SpinnerToken : IonicToken
{
    private float _size = 28f;
    public float Size
    {
        get => _size;
        set { _size = value; Mark(nameof(Size)); }
    }

    private float _smallSize = 16f;
    public float SmallSize
    {
        get => _smallSize;
        set { _smallSize = value; Mark(nameof(SmallSize)); }
    }

    private Color _color = default;
    public Color Color
    {
        get => _color;
        set { _color = value; Mark(nameof(Color)); }
    }

    private Color _trackColor = default;
    public Color TrackColor
    {
        get => _trackColor;
        set { _trackColor = value; Mark(nameof(TrackColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (SpinnerToken)target;
        if (IsSpecified(nameof(Size))) token.Size = Size;
        if (IsSpecified(nameof(SmallSize))) token.SmallSize = SmallSize;
        if (IsSpecified(nameof(Color))) token.Color = Color;
        if (IsSpecified(nameof(TrackColor))) token.TrackColor = TrackColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Color);
        AppendValue(builder, Size);
        AppendValue(builder, SmallSize);
        AppendValue(builder, TrackColor);
    }
}
