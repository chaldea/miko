using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-icon</c>. The SVG itself is supplied per-instance as a background
/// image (see <see cref="IonIcon"/>); these rules control the icon box size and tint.
/// <para>
/// Mirroring Ionic's <c>icon.css</c>, the host box is 1em × 1em so the icon scales with the
/// computed font size: <c>icon-small</c>/<c>icon-large</c> merely set the font size
/// (1.125rem / 2rem), and a tab button sizes its icon via font-size too. The glyph is a
/// monochrome template tinted with the element's <c>color</c> (CSS <c>fill: currentColor</c>),
/// so <c>ion-color-*</c> maps to <see cref="Style.Color"/>.
/// </para>
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see
/// <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class IconStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // :host { display: inline-block; width: 1em; height: 1em; }
            [$".ion-icon.{mode}"] = new()
            {
                Display = Display.InlineBlock,
                Width = Length.Em(1),
                Height = Length.Em(1),
                BoxSizing = BoxSizing.ContentBox,
            },

            // :host(.icon-small) { font-size: 1.125rem } → 18px box.
            [$".ion-icon.{mode}.icon-small"] = new()
            {
                FontSize = Length.Rem(1.125f),
            },

            // :host(.icon-large) { font-size: 2rem } → 32px box.
            [$".ion-icon.{mode}.icon-large"] = new()
            {
                FontSize = Length.Rem(2),
            },

            // Tab button icon: the tab-button font-size drives the 1em box; small bottom gap
            // before the label.
            [$".ion-tab-button.{mode} .ion-icon"] = new()
            {
                FontSize = Length.Px(t.TabButtonIconSize),
                MarginBottom = Length.Px(2),
            },
        };

        // :host(.ion-color-{name}) { --ion-color-base: ... } → color: base (fill: currentColor).
        AddColor(css, mode, "primary", t.Primary);
        AddColor(css, mode, "secondary", t.Secondary);
        AddColor(css, mode, "tertiary", t.Tertiary);
        AddColor(css, mode, "success", t.Success);
        AddColor(css, mode, "warning", t.Warning);
        AddColor(css, mode, "danger", t.Danger);
        AddColor(css, mode, "light", t.Light);
        AddColor(css, mode, "medium", t.Medium);
        AddColor(css, mode, "dark", t.Dark);

        return css;
    }

    private static void AddColor(CssObject css, string mode, string name, Color color)
    {
        css[$".ion-icon.{mode}.ion-color-{name}"] = new()
        {
            Color = color,
        };
    }
}
