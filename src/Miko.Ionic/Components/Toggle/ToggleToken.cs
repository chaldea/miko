using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Toggle styles.</summary>
public sealed class ToggleToken : IonicToken
{
    private float _trackWidth = 36f;
    public float TrackWidth
    {
        get => _trackWidth;
        set { _trackWidth = value; Mark(nameof(TrackWidth)); }
    }

    private float _trackHeight = 14f;
    public float TrackHeight
    {
        get => _trackHeight;
        set { _trackHeight = value; Mark(nameof(TrackHeight)); }
    }

    private Length _borderRadius = Length.Px(14);
    public Length BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Mark(nameof(BorderRadius)); }
    }

    private float _trackCheckedAlpha = 0.5f;
    public float TrackCheckedAlpha
    {
        get => _trackCheckedAlpha;
        set { _trackCheckedAlpha = value; Mark(nameof(TrackCheckedAlpha)); }
    }

    private float _handleWidth = 20f;
    public float HandleWidth
    {
        get => _handleWidth;
        set { _handleWidth = value; Mark(nameof(HandleWidth)); }
    }

    private float _handleHeight = 20f;
    public float HandleHeight
    {
        get => _handleHeight;
        set { _handleHeight = value; Mark(nameof(HandleHeight)); }
    }

    private Length _handleBorderRadius = Length.Px(10);
    public Length HandleBorderRadius
    {
        get => _handleBorderRadius;
        set { _handleBorderRadius = value; Mark(nameof(HandleBorderRadius)); }
    }

    private Color _handleBackground = Color.White;
    public Color HandleBackground
    {
        get => _handleBackground;
        set { _handleBackground = value; Mark(nameof(HandleBackground)); }
    }

    private Color _handleBackgroundChecked = Color.White;
    public Color HandleBackgroundChecked
    {
        get => _handleBackgroundChecked;
        set { _handleBackgroundChecked = value; Mark(nameof(HandleBackgroundChecked)); }
    }

    private List<BoxShadow> _handleBoxShadow = new();
    public List<BoxShadow> HandleBoxShadow
    {
        get { Mark(nameof(HandleBoxShadow)); return _handleBoxShadow; }
        set { _handleBoxShadow = value; Mark(nameof(HandleBoxShadow)); }
    }

    private float _handleTravel = 16f;
    public float HandleTravel
    {
        get => _handleTravel;
        set { _handleTravel = value; Mark(nameof(HandleTravel)); }
    }

    private float _transitionDuration = 0.16f;
    public float TransitionDuration
    {
        get => _transitionDuration;
        set { _transitionDuration = value; Mark(nameof(TransitionDuration)); }
    }

    private float _disabledOpacity = 0.38f;
    public float DisabledOpacity
    {
        get => _disabledOpacity;
        set { _disabledOpacity = value; Mark(nameof(DisabledOpacity)); }
    }

    private Color _trackBackgroundOff = default;
    public Color TrackBackgroundOff
    {
        get => _trackBackgroundOff;
        set { _trackBackgroundOff = value; Mark(nameof(TrackBackgroundOff)); }
    }

    private Color _trackBackgroundOn = default;
    public Color TrackBackgroundOn
    {
        get => _trackBackgroundOn;
        set { _trackBackgroundOn = value; Mark(nameof(TrackBackgroundOn)); }
    }

    private float _handleSpacing = default;
    public float HandleSpacing
    {
        get => _handleSpacing;
        set { _handleSpacing = value; Mark(nameof(HandleSpacing)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (ToggleToken)target;
        if (IsSpecified(nameof(TrackWidth))) token.TrackWidth = TrackWidth;
        if (IsSpecified(nameof(TrackHeight))) token.TrackHeight = TrackHeight;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(TrackCheckedAlpha))) token.TrackCheckedAlpha = TrackCheckedAlpha;
        if (IsSpecified(nameof(HandleWidth))) token.HandleWidth = HandleWidth;
        if (IsSpecified(nameof(HandleHeight))) token.HandleHeight = HandleHeight;
        if (IsSpecified(nameof(HandleBorderRadius))) token.HandleBorderRadius = HandleBorderRadius;
        if (IsSpecified(nameof(HandleBackground))) token.HandleBackground = HandleBackground;
        if (IsSpecified(nameof(HandleBackgroundChecked))) token.HandleBackgroundChecked = HandleBackgroundChecked;
        if (IsSpecified(nameof(HandleBoxShadow))) token.HandleBoxShadow = new(HandleBoxShadow);
        if (IsSpecified(nameof(HandleTravel))) token.HandleTravel = HandleTravel;
        if (IsSpecified(nameof(TransitionDuration))) token.TransitionDuration = TransitionDuration;
        if (IsSpecified(nameof(DisabledOpacity))) token.DisabledOpacity = DisabledOpacity;
        if (IsSpecified(nameof(TrackBackgroundOff))) token.TrackBackgroundOff = TrackBackgroundOff;
        if (IsSpecified(nameof(TrackBackgroundOn))) token.TrackBackgroundOn = TrackBackgroundOn;
        if (IsSpecified(nameof(HandleSpacing))) token.HandleSpacing = HandleSpacing;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, BorderRadius);
        AppendValue(builder, DisabledOpacity);
        AppendValue(builder, HandleBackground);
        AppendValue(builder, HandleBackgroundChecked);
        AppendValue(builder, HandleBorderRadius);
        AppendValue(builder, HandleBoxShadow);
        AppendValue(builder, HandleHeight);
        AppendValue(builder, HandleSpacing);
        AppendValue(builder, HandleTravel);
        AppendValue(builder, HandleWidth);
        AppendValue(builder, TrackBackgroundOff);
        AppendValue(builder, TrackBackgroundOn);
        AppendValue(builder, TrackCheckedAlpha);
        AppendValue(builder, TrackHeight);
        AppendValue(builder, TrackWidth);
        AppendValue(builder, TransitionDuration);
    }
}
