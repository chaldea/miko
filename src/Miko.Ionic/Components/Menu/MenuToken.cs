using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Menu styles.</summary>
public sealed class MenuToken : IonicToken
{
    private float _width = 304f;
    public float Width
    {
        get => _width;
        set { _width = value; Mark(nameof(Width)); }
    }

    private List<BoxShadow> _boxShadow = new();
    public List<BoxShadow> BoxShadow
    {
        get { Mark(nameof(BoxShadow)); return _boxShadow; }
        set { _boxShadow = value; Mark(nameof(BoxShadow)); }
    }

    private float _animDuration = 0.28f;
    public float AnimDuration
    {
        get => _animDuration;
        set { _animDuration = value; Mark(nameof(AnimDuration)); }
    }

    private Color _appBackground = default;
    public Color AppBackground
    {
        get => _appBackground;
        set { _appBackground = value; Mark(nameof(AppBackground)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
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
        var token = (MenuToken)target;
        if (IsSpecified(nameof(Width))) token.Width = Width;
        if (IsSpecified(nameof(BoxShadow))) token.BoxShadow = new(BoxShadow);
        if (IsSpecified(nameof(AnimDuration))) token.AnimDuration = AnimDuration;
        if (IsSpecified(nameof(AppBackground))) token.AppBackground = AppBackground;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(BorderColor))) token.BorderColor = BorderColor;
        if (IsSpecified(nameof(BorderWidth))) token.BorderWidth = BorderWidth;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, AnimDuration);
        AppendValue(builder, AppBackground);
        AppendValue(builder, Background);
        AppendValue(builder, BorderColor);
        AppendValue(builder, BorderWidth);
        AppendValue(builder, BoxShadow);
        AppendValue(builder, Width);
    }
}
