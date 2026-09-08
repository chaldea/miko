using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Checkbox styles.</summary>
public sealed class CheckboxToken : IonicToken
{
    private float _size = 18f;
    public float Size
    {
        get => _size;
        set { _size = value; Mark(nameof(Size)); }
    }

    private float _borderWidth = 2f;
    public float BorderWidth
    {
        get => _borderWidth;
        set { _borderWidth = value; Mark(nameof(BorderWidth)); }
    }

    private Length _borderRadius = Length.Px(2.25f);
    public Length BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Mark(nameof(BorderRadius)); }
    }

    private float _disabledOpacity = 0.38f;
    public float DisabledOpacity
    {
        get => _disabledOpacity;
        set { _disabledOpacity = value; Mark(nameof(DisabledOpacity)); }
    }

    private Color _borderColorOff = default;
    public Color BorderColorOff
    {
        get => _borderColorOff;
        set { _borderColorOff = value; Mark(nameof(BorderColorOff)); }
    }

    private Color _backgroundOff = default;
    public Color BackgroundOff
    {
        get => _backgroundOff;
        set { _backgroundOff = value; Mark(nameof(BackgroundOff)); }
    }

    private Color _backgroundChecked = default;
    public Color BackgroundChecked
    {
        get => _backgroundChecked;
        set { _backgroundChecked = value; Mark(nameof(BackgroundChecked)); }
    }

    private Color _checkmarkColor = default;
    public Color CheckmarkColor
    {
        get => _checkmarkColor;
        set { _checkmarkColor = value; Mark(nameof(CheckmarkColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (CheckboxToken)target;
        if (IsSpecified(nameof(Size))) token.Size = Size;
        if (IsSpecified(nameof(BorderWidth))) token.BorderWidth = BorderWidth;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(DisabledOpacity))) token.DisabledOpacity = DisabledOpacity;
        if (IsSpecified(nameof(BorderColorOff))) token.BorderColorOff = BorderColorOff;
        if (IsSpecified(nameof(BackgroundOff))) token.BackgroundOff = BackgroundOff;
        if (IsSpecified(nameof(BackgroundChecked))) token.BackgroundChecked = BackgroundChecked;
        if (IsSpecified(nameof(CheckmarkColor))) token.CheckmarkColor = CheckmarkColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, BackgroundChecked);
        AppendValue(builder, BackgroundOff);
        AppendValue(builder, BorderColorOff);
        AppendValue(builder, BorderRadius);
        AppendValue(builder, BorderWidth);
        AppendValue(builder, CheckmarkColor);
        AppendValue(builder, DisabledOpacity);
        AppendValue(builder, Size);
    }
}
