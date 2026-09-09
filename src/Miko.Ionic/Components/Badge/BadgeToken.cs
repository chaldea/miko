using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Badge styles.</summary>
public sealed class BadgeToken : IonicToken
{
    private float _borderRadius = 4f;
    public float BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Mark(nameof(BorderRadius)); }
    }

    private float _fontSize = 13f;
    public float FontSize
    {
        get => _fontSize;
        set { _fontSize = value; Mark(nameof(FontSize)); }
    }

    private float _paddingTop = 3f;
    public float PaddingTop
    {
        get => _paddingTop;
        set { _paddingTop = value; Mark(nameof(PaddingTop)); }
    }

    private float _paddingEnd = 4f;
    public float PaddingEnd
    {
        get => _paddingEnd;
        set { _paddingEnd = value; Mark(nameof(PaddingEnd)); }
    }

    private float _paddingBottom = 4f;
    public float PaddingBottom
    {
        get => _paddingBottom;
        set { _paddingBottom = value; Mark(nameof(PaddingBottom)); }
    }

    private float _paddingStart = 4f;
    public float PaddingStart
    {
        get => _paddingStart;
        set { _paddingStart = value; Mark(nameof(PaddingStart)); }
    }

    private float _minWidth = 10f;
    public float MinWidth
    {
        get => _minWidth;
        set { _minWidth = value; Mark(nameof(MinWidth)); }
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

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (BadgeToken)target;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(FontSize))) token.FontSize = FontSize;
        if (IsSpecified(nameof(PaddingTop))) token.PaddingTop = PaddingTop;
        if (IsSpecified(nameof(PaddingEnd))) token.PaddingEnd = PaddingEnd;
        if (IsSpecified(nameof(PaddingBottom))) token.PaddingBottom = PaddingBottom;
        if (IsSpecified(nameof(PaddingStart))) token.PaddingStart = PaddingStart;
        if (IsSpecified(nameof(MinWidth))) token.MinWidth = MinWidth;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(Color))) token.Color = Color;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, BorderRadius);
        AppendValue(builder, Color);
        AppendValue(builder, FontSize);
        AppendValue(builder, MinWidth);
        AppendValue(builder, PaddingBottom);
        AppendValue(builder, PaddingEnd);
        AppendValue(builder, PaddingStart);
        AppendValue(builder, PaddingTop);
    }
}
