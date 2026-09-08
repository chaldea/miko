using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for SkeletonText styles.</summary>
public sealed class SkeletonTextToken : IonicToken
{
    private Color _background = new Color(0, 0, 0, 17);
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    private Color _backgroundAnimated = new Color(0, 0, 0, 34);
    public Color BackgroundAnimated
    {
        get => _backgroundAnimated;
        set { _backgroundAnimated = value; Mark(nameof(BackgroundAnimated)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (SkeletonTextToken)target;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(BackgroundAnimated))) token.BackgroundAnimated = BackgroundAnimated;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, BackgroundAnimated);
    }
}
