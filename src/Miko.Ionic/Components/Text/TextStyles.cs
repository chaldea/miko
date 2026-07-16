using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Generates stylesheet rules for the Ionic text component.
/// Ports <c>text.scss</c>, which is intentionally minimal: the only rule is
/// <c>:host(.ion-color) { color: current-color(base); }</c>. <c>ion-text</c> has no own display or
/// typography — it simply tints its slotted content with the named palette color. There is no
/// mode-specific difference (a single shared <c>text.scss</c>), but rules are still scoped per mode
/// for registration parity with the rest of the suite.
/// </summary>
internal static class TextStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject();

        // ion-color tints the slotted content with the named palette base color.
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
        css[$".ion-text.{mode}.ion-color-{name}"] = new()
        {
            Color = color,
        };
    }
}
