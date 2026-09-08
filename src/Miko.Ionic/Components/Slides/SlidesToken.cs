using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Slides styles.</summary>
public sealed class SlidesToken : IonicToken
{
    private Color _bulletBackground = default;
    public Color BulletBackground
    {
        get => _bulletBackground;
        set { _bulletBackground = value; Mark(nameof(BulletBackground)); }
    }

    private Color _bulletBackgroundActive = default;
    public Color BulletBackgroundActive
    {
        get => _bulletBackgroundActive;
        set { _bulletBackgroundActive = value; Mark(nameof(BulletBackgroundActive)); }
    }

    private Color _scrollBarBackground = default;
    public Color ScrollBarBackground
    {
        get => _scrollBarBackground;
        set { _scrollBarBackground = value; Mark(nameof(ScrollBarBackground)); }
    }

    private Color _scrollBarBackgroundActive = default;
    public Color ScrollBarBackgroundActive
    {
        get => _scrollBarBackgroundActive;
        set { _scrollBarBackgroundActive = value; Mark(nameof(ScrollBarBackgroundActive)); }
    }

    private Color _navigationColor = default;
    public Color NavigationColor
    {
        get => _navigationColor;
        set { _navigationColor = value; Mark(nameof(NavigationColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (SlidesToken)target;
        if (IsSpecified(nameof(BulletBackground))) token.BulletBackground = BulletBackground;
        if (IsSpecified(nameof(BulletBackgroundActive))) token.BulletBackgroundActive = BulletBackgroundActive;
        if (IsSpecified(nameof(ScrollBarBackground))) token.ScrollBarBackground = ScrollBarBackground;
        if (IsSpecified(nameof(ScrollBarBackgroundActive))) token.ScrollBarBackgroundActive = ScrollBarBackgroundActive;
        if (IsSpecified(nameof(NavigationColor))) token.NavigationColor = NavigationColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, BulletBackground);
        AppendValue(builder, BulletBackgroundActive);
        AppendValue(builder, NavigationColor);
        AppendValue(builder, ScrollBarBackground);
        AppendValue(builder, ScrollBarBackgroundActive);
    }
}
