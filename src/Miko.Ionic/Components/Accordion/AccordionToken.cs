using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Accordion styles.</summary>
public sealed class AccordionToken : IonicToken
{
    private float _disabledOpacity = 0.4f;
    public float DisabledOpacity
    {
        get => _disabledOpacity;
        set { _disabledOpacity = value; Mark(nameof(DisabledOpacity)); }
    }

    private float _insetMargin = 16f;
    public float InsetMargin
    {
        get => _insetMargin;
        set { _insetMargin = value; Mark(nameof(InsetMargin)); }
    }

    private float _insetBorderRadius = 6f;
    public float InsetBorderRadius
    {
        get => _insetBorderRadius;
        set { _insetBorderRadius = value; Mark(nameof(InsetBorderRadius)); }
    }

    private List<BoxShadow> _insetBoxShadow = new();
    public List<BoxShadow> InsetBoxShadow
    {
        get { Mark(nameof(InsetBoxShadow)); return _insetBoxShadow; }
        set { _insetBoxShadow = value; Mark(nameof(InsetBoxShadow)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (AccordionToken)target;
        if (IsSpecified(nameof(DisabledOpacity))) token.DisabledOpacity = DisabledOpacity;
        if (IsSpecified(nameof(InsetMargin))) token.InsetMargin = InsetMargin;
        if (IsSpecified(nameof(InsetBorderRadius))) token.InsetBorderRadius = InsetBorderRadius;
        if (IsSpecified(nameof(InsetBoxShadow))) token.InsetBoxShadow = new(InsetBoxShadow);
        if (IsSpecified(nameof(Background))) token.Background = Background;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, DisabledOpacity);
        AppendValue(builder, InsetBorderRadius);
        AppendValue(builder, InsetBoxShadow);
        AppendValue(builder, InsetMargin);
    }
}
