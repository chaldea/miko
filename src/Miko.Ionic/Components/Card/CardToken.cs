using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Card styles.</summary>
public sealed class CardToken : IonicToken
{
    private float _marginTop = 10f;
    public float MarginTop
    {
        get => _marginTop;
        set { _marginTop = value; Mark(nameof(MarginTop)); }
    }

    private float _marginEnd = 10f;
    public float MarginEnd
    {
        get => _marginEnd;
        set { _marginEnd = value; Mark(nameof(MarginEnd)); }
    }

    private float _marginBottom = 10f;
    public float MarginBottom
    {
        get => _marginBottom;
        set { _marginBottom = value; Mark(nameof(MarginBottom)); }
    }

    private float _marginStart = 10f;
    public float MarginStart
    {
        get => _marginStart;
        set { _marginStart = value; Mark(nameof(MarginStart)); }
    }

    private float _borderRadius = 4f;
    public float BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Mark(nameof(BorderRadius)); }
    }

    private float _fontSize = 14f;
    public float FontSize
    {
        get => _fontSize;
        set { _fontSize = value; Mark(nameof(FontSize)); }
    }

    private Length _lineHeight = Length.Number(1.5f);
    public Length LineHeight
    {
        get => _lineHeight;
        set { _lineHeight = value; Mark(nameof(LineHeight)); }
    }

    private List<BoxShadow> _boxShadow = new();
    public List<BoxShadow> BoxShadow
    {
        get { Mark(nameof(BoxShadow)); return _boxShadow; }
        set { _boxShadow = value; Mark(nameof(BoxShadow)); }
    }

    private float _headerPaddingTop = 16f;
    public float HeaderPaddingTop
    {
        get => _headerPaddingTop;
        set { _headerPaddingTop = value; Mark(nameof(HeaderPaddingTop)); }
    }

    private float _headerPaddingEnd = 16f;
    public float HeaderPaddingEnd
    {
        get => _headerPaddingEnd;
        set { _headerPaddingEnd = value; Mark(nameof(HeaderPaddingEnd)); }
    }

    private float _headerPaddingBottom = 16f;
    public float HeaderPaddingBottom
    {
        get => _headerPaddingBottom;
        set { _headerPaddingBottom = value; Mark(nameof(HeaderPaddingBottom)); }
    }

    private float _headerPaddingStart = 16f;
    public float HeaderPaddingStart
    {
        get => _headerPaddingStart;
        set { _headerPaddingStart = value; Mark(nameof(HeaderPaddingStart)); }
    }

    private float _contentPaddingTop = 13f;
    public float ContentPaddingTop
    {
        get => _contentPaddingTop;
        set { _contentPaddingTop = value; Mark(nameof(ContentPaddingTop)); }
    }

    private float _contentPaddingEnd = 16f;
    public float ContentPaddingEnd
    {
        get => _contentPaddingEnd;
        set { _contentPaddingEnd = value; Mark(nameof(ContentPaddingEnd)); }
    }

    private float _contentPaddingBottom = 13f;
    public float ContentPaddingBottom
    {
        get => _contentPaddingBottom;
        set { _contentPaddingBottom = value; Mark(nameof(ContentPaddingBottom)); }
    }

    private float _contentPaddingStart = 16f;
    public float ContentPaddingStart
    {
        get => _contentPaddingStart;
        set { _contentPaddingStart = value; Mark(nameof(ContentPaddingStart)); }
    }

    private float _contentFontSize = 14f;
    public float ContentFontSize
    {
        get => _contentFontSize;
        set { _contentFontSize = value; Mark(nameof(ContentFontSize)); }
    }

    private Length _contentLineHeight = Length.Number(1.5f);
    public Length ContentLineHeight
    {
        get => _contentLineHeight;
        set { _contentLineHeight = value; Mark(nameof(ContentLineHeight)); }
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
        var token = (CardToken)target;
        if (IsSpecified(nameof(MarginTop))) token.MarginTop = MarginTop;
        if (IsSpecified(nameof(MarginEnd))) token.MarginEnd = MarginEnd;
        if (IsSpecified(nameof(MarginBottom))) token.MarginBottom = MarginBottom;
        if (IsSpecified(nameof(MarginStart))) token.MarginStart = MarginStart;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(FontSize))) token.FontSize = FontSize;
        if (IsSpecified(nameof(LineHeight))) token.LineHeight = LineHeight;
        if (IsSpecified(nameof(BoxShadow))) token.BoxShadow = new(BoxShadow);
        if (IsSpecified(nameof(HeaderPaddingTop))) token.HeaderPaddingTop = HeaderPaddingTop;
        if (IsSpecified(nameof(HeaderPaddingEnd))) token.HeaderPaddingEnd = HeaderPaddingEnd;
        if (IsSpecified(nameof(HeaderPaddingBottom))) token.HeaderPaddingBottom = HeaderPaddingBottom;
        if (IsSpecified(nameof(HeaderPaddingStart))) token.HeaderPaddingStart = HeaderPaddingStart;
        if (IsSpecified(nameof(ContentPaddingTop))) token.ContentPaddingTop = ContentPaddingTop;
        if (IsSpecified(nameof(ContentPaddingEnd))) token.ContentPaddingEnd = ContentPaddingEnd;
        if (IsSpecified(nameof(ContentPaddingBottom))) token.ContentPaddingBottom = ContentPaddingBottom;
        if (IsSpecified(nameof(ContentPaddingStart))) token.ContentPaddingStart = ContentPaddingStart;
        if (IsSpecified(nameof(ContentFontSize))) token.ContentFontSize = ContentFontSize;
        if (IsSpecified(nameof(ContentLineHeight))) token.ContentLineHeight = ContentLineHeight;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(Color))) token.Color = Color;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, BorderRadius);
        AppendValue(builder, BoxShadow);
        AppendValue(builder, Color);
        AppendValue(builder, ContentFontSize);
        AppendValue(builder, ContentLineHeight);
        AppendValue(builder, ContentPaddingBottom);
        AppendValue(builder, ContentPaddingEnd);
        AppendValue(builder, ContentPaddingStart);
        AppendValue(builder, ContentPaddingTop);
        AppendValue(builder, FontSize);
        AppendValue(builder, HeaderPaddingBottom);
        AppendValue(builder, HeaderPaddingEnd);
        AppendValue(builder, HeaderPaddingStart);
        AppendValue(builder, HeaderPaddingTop);
        AppendValue(builder, LineHeight);
        AppendValue(builder, MarginBottom);
        AppendValue(builder, MarginEnd);
        AppendValue(builder, MarginStart);
        AppendValue(builder, MarginTop);
    }
}
