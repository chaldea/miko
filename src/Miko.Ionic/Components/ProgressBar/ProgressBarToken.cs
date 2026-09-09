using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for ProgressBar styles.</summary>
public sealed class ProgressBarToken : IonicToken
{
    private float _height = 4f;
    public float Height
    {
        get => _height;
        set { _height = value; Mark(nameof(Height)); }
    }

    private float _borderRadius = default;
    public float BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Mark(nameof(BorderRadius)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    private Color _progressBackground = default;
    public Color ProgressBackground
    {
        get => _progressBackground;
        set { _progressBackground = value; Mark(nameof(ProgressBackground)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (ProgressBarToken)target;
        if (IsSpecified(nameof(Height))) token.Height = Height;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(ProgressBackground))) token.ProgressBackground = ProgressBackground;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, BorderRadius);
        AppendValue(builder, Height);
        AppendValue(builder, ProgressBackground);
    }
}
