using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Header styles.</summary>
public sealed class HeaderToken : IonicToken
{
    private List<BoxShadow> _boxShadow = new();
    public List<BoxShadow> BoxShadow
    {
        get { Mark(nameof(BoxShadow)); return _boxShadow; }
        set { _boxShadow = value; Mark(nameof(BoxShadow)); }
    }

    private Color _borderColor = default;
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Mark(nameof(BorderColor)); }
    }

    private float _borderWidth = default;
    public float BorderWidth
    {
        get => _borderWidth;
        set { _borderWidth = value; Mark(nameof(BorderWidth)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (HeaderToken)target;
        if (IsSpecified(nameof(BoxShadow))) token.BoxShadow = new(BoxShadow);
        if (IsSpecified(nameof(BorderColor))) token.BorderColor = BorderColor;
        if (IsSpecified(nameof(BorderWidth))) token.BorderWidth = BorderWidth;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, BorderColor);
        AppendValue(builder, BorderWidth);
        AppendValue(builder, BoxShadow);
    }
}
