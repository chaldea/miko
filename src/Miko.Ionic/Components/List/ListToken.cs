using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for List styles.</summary>
public sealed class ListToken : IonicToken
{
    private float _headerFontSize = 16f;
    public float HeaderFontSize
    {
        get => _headerFontSize;
        set { _headerFontSize = value; Mark(nameof(HeaderFontSize)); }
    }

    private float _insetMargin = 16f;
    public float InsetMargin
    {
        get => _insetMargin;
        set { _insetMargin = value; Mark(nameof(InsetMargin)); }
    }

    private float _insetBorderRadius = 2f;
    public float InsetBorderRadius
    {
        get => _insetBorderRadius;
        set { _insetBorderRadius = value; Mark(nameof(InsetBorderRadius)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    private Color _headerColor = default;
    public Color HeaderColor
    {
        get => _headerColor;
        set { _headerColor = value; Mark(nameof(HeaderColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (ListToken)target;
        if (IsSpecified(nameof(HeaderFontSize))) token.HeaderFontSize = HeaderFontSize;
        if (IsSpecified(nameof(InsetMargin))) token.InsetMargin = InsetMargin;
        if (IsSpecified(nameof(InsetBorderRadius))) token.InsetBorderRadius = InsetBorderRadius;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(HeaderColor))) token.HeaderColor = HeaderColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, HeaderColor);
        AppendValue(builder, HeaderFontSize);
        AppendValue(builder, InsetBorderRadius);
        AppendValue(builder, InsetMargin);
    }
}
