using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Toast styles.</summary>
public sealed class ToastToken : IonicToken
{
    private float _edgeOffset = 8f;
    public float EdgeOffset
    {
        get => _edgeOffset;
        set { _edgeOffset = value; Mark(nameof(EdgeOffset)); }
    }

    private float _enterDuration = 0.4f;
    public float EnterDuration
    {
        get => _enterDuration;
        set { _enterDuration = value; Mark(nameof(EnterDuration)); }
    }

    private float _leaveDuration = 0.3f;
    public float LeaveDuration
    {
        get => _leaveDuration;
        set { _leaveDuration = value; Mark(nameof(LeaveDuration)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (ToastToken)target;
        if (IsSpecified(nameof(EdgeOffset))) token.EdgeOffset = EdgeOffset;
        if (IsSpecified(nameof(EnterDuration))) token.EnterDuration = EnterDuration;
        if (IsSpecified(nameof(LeaveDuration))) token.LeaveDuration = LeaveDuration;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, EdgeOffset);
        AppendValue(builder, EnterDuration);
        AppendValue(builder, LeaveDuration);
    }
}
