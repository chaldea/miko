using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-icon</c>. The SVG itself is supplied per-instance as a background
/// image (see <see cref="IonIcon"/>); these rules control the icon box size. Within a
/// tab button the icon uses the Material Design size (22px) from
/// <c>tab-button.md.vars.scss</c>.
/// </summary>
internal static class IconStyles
{
    internal static CssObject GenStyle(IonicTheme t)
    {
        return new CssObject
        {
            [".ion-icon"] = new()
            {
                Display = Display.Block,
                Width = Length.Px(t.TabButtonIconSize),
                Height = Length.Px(t.TabButtonIconSize),
            },

            // Tab button icon: explicit MD size + small bottom gap before the label.
            [".ion-tab-button .ion-icon"] = new()
            {
                Width = Length.Px(t.TabButtonIconSize),
                Height = Length.Px(t.TabButtonIconSize),
                MarginBottom = Length.Px(2),
            },
        };
    }
}
