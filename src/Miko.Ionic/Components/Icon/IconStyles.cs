using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-icon</c>. The SVG itself is supplied per-instance as a background
/// image (see <see cref="IonIcon"/>); these rules control the icon box size. Within a
/// tab button the icon uses the active mode's tab-button icon size.
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see
/// <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class IconStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            [$".ion-icon.{mode}"] = new()
            {
                Display = Display.Block,
                Width = Length.Px(t.TabButtonIconSize),
                Height = Length.Px(t.TabButtonIconSize),
            },

            // Tab button icon: explicit mode size + small bottom gap before the label.
            [$".ion-tab-button.{mode} .ion-icon"] = new()
            {
                Width = Length.Px(t.TabButtonIconSize),
                Height = Length.Px(t.TabButtonIconSize),
                MarginBottom = Length.Px(2),
            },
        };
    }
}
