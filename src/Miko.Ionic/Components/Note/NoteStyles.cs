using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Generates stylesheet rules for the Ionic note component.
/// Ports <c>note.scss</c> + the per-mode <c>note.md.scss</c> / <c>note.ios.scss</c> and their vars.
/// <para>
/// A note is an inline muted-gray label (metadata beside a list item). The base rule sets the mode
/// gray text color and font size (both 14px; md uses <c>text-color-step-400</c>, ios the lighter
/// <c>text-color-step-650</c>). When a palette <c>color</c> is supplied, the <c>ion-color-*</c> rule
/// overrides the text color with that palette base color.
/// </para>
/// </summary>
internal static class NoteStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            [$".ion-note.{mode}"] = new()
            {
                Display = Display.InlineBlock,
                Color = t.NoteColor,
                FontSize = Length.Px(t.NoteFontSize),
                BoxSizing = BoxSizing.BorderBox,
            },
        };

        // ion-color sets the note text to the named palette base color.
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
        css[$".ion-note.{mode}.ion-color-{name}"] = new()
        {
            Color = color,
        };
    }
}
