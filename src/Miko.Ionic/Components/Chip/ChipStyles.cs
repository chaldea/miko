using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-chip</c>. Ports the chip styles from Ionic Framework
/// (<c>chip.scss</c> + <c>chip.vars.scss</c>).
/// <para>
/// The host is an <c>inline-flex</c> pill with centered items — slotted icons and avatars
/// hang off the pill edges via negative outer margins (<c>::slotted(…:first-child)</c> /
/// <c>::slotted(…:last-child)</c>). Icon/avatar sizes come from <c>chip.vars.scss</c>
/// (<c>$chip-icon-size</c> = 20em/14, <c>$chip-avatar-size</c> = 24em/14) evaluated against
/// the 14px chip font size, i.e. 20px / 24px boxes. The <c>@media (any-hover: hover)</c>
/// background shifts are ported as plain <c>:hover</c> rules (Miko has no pointer-capability
/// media queries; touch devices simply never hover). Focus/activated shifts are omitted:
/// Miko's Ionic port does not model those interaction states.
/// </para>
/// </summary>
internal static class ChipStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // :host — inline-flex pill, items vertically centered.
            [$".ion-chip.{mode}"] = new()
            {
                Display = Display.InlineFlex,
                AlignItems = AlignItems.Center,
                Position = Position.Relative,
                MinHeight = Length.Px(32),
                PaddingTop = Length.Px(6),
                PaddingRight = Length.Px(12),
                PaddingBottom = Length.Px(6),
                PaddingLeft = Length.Px(12),
                MarginTop = Length.Px(4),
                MarginRight = Length.Px(4),
                MarginBottom = Length.Px(4),
                MarginLeft = Length.Px(4),
                BackgroundColor = t.ChipBackground,
                Color = t.ChipColor,
                FontSize = Length.Px(t.ChipFontSize),
                BorderRadius = new BorderRadius(Length.Px(16)),
                Cursor = Cursor.Pointer,
                VerticalAlign = VerticalAlign.Middle,
                BoxSizing = BoxSizing.BorderBox,
                Overflow = Overflow.Hidden,
            },

            [$".ion-chip.{mode}.chip-disabled"] = new()
            {
                Opacity = 0.4f,
                Cursor = Cursor.Default,
                PointerEvents = PointerEvents.None,
            },

            [$".ion-chip.{mode}.chip-outline"] = new()
            {
                BackgroundColor = Color.Transparent,
                BorderWidth = Length.Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = t.ChipBorderColor,
            },

            // :host(:hover) — background: rgba($text-color-rgb, 0.16).
            [$".ion-chip.{mode}:hover"] = new()
            {
                BackgroundColor = WithAlpha(t.ChipBackground, 41), // 0.16 * 255
            },

            // :host(.chip-outline:not(.ion-color):hover) — background: rgba($text-color-rgb, 0.04).
            [$".ion-chip.{mode}.chip-outline:not(.ion-color):hover"] = new()
            {
                BackgroundColor = WithAlpha(t.ChipBackground, 10), // 0.04 * 255
            },

            // ::slotted(ion-icon) — Miko's IonIcon is an SVG background box, so the sass
            // `font-size: $chip-icon-size` becomes an explicit 20px box here.
            [$".ion-chip.{mode} .ion-icon"] = new()
            {
                Width = Length.Px(20),
                Height = Length.Px(20),
            },

            // ::slotted(ion-icon:first-child) — margin(-4px, 8px, -4px, -4px).
            [$".ion-chip.{mode} .ion-icon:first-child"] = new()
            {
                MarginTop = Length.Px(-4),
                MarginRight = Length.Px(8),
                MarginBottom = Length.Px(-4),
                MarginLeft = Length.Px(-4),
            },

            // ::slotted(ion-icon:last-child) — margin(-4px, -4px, -4px, 8px).
            [$".ion-chip.{mode} .ion-icon:last-child"] = new()
            {
                MarginTop = Length.Px(-4),
                MarginRight = Length.Px(-4),
                MarginBottom = Length.Px(-4),
                MarginLeft = Length.Px(8),
            },

            // ::slotted(ion-avatar) — flex-shrink: 0; $chip-avatar-size (24px) box.
            [$".ion-chip.{mode} .ion-avatar"] = new()
            {
                Width = Length.Px(24),
                Height = Length.Px(24),
                FlexShrink = 0,
            },

            // ::slotted(ion-avatar:first-child) — margin(-4px, 8px, -4px, -8px).
            [$".ion-chip.{mode} .ion-avatar:first-child"] = new()
            {
                MarginTop = Length.Px(-4),
                MarginRight = Length.Px(8),
                MarginBottom = Length.Px(-4),
                MarginLeft = Length.Px(-8),
            },

            // ::slotted(ion-avatar:last-child) — margin(-4px, -8px, -4px, 8px).
            [$".ion-chip.{mode} .ion-avatar:last-child"] = new()
            {
                MarginTop = Length.Px(-4),
                MarginRight = Length.Px(-8),
                MarginBottom = Length.Px(-4),
                MarginLeft = Length.Px(8),
            },
        };

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
        css[$".ion-chip.{mode}.ion-color-{name}"] = new()
        {
            BackgroundColor = new Color(color.R, color.G, color.B, 20),
            Color = color,
        };
        css[$".ion-chip.{mode}.chip-outline.ion-color-{name}"] = new()
        {
            BackgroundColor = Color.Transparent,
            BorderColor = new Color(color.R, color.G, color.B, 82),
            Color = color,
        };
        // :host(.ion-color:hover) — background: current-color(base, 0.12). Defined after the
        // outline-color rule (equal specificity, 40): on a colored outline chip the hover wash
        // must win over the transparent fill, so definition order breaks the tie.
        css[$".ion-chip.{mode}.ion-color-{name}:hover"] = new()
        {
            BackgroundColor = new Color(color.R, color.G, color.B, 31), // 0.12 * 255
        };
    }

    /// <summary>Same RGB as <paramref name="c"/> with a replaced alpha (text-color-derived rules).</summary>
    private static Color WithAlpha(Color c, byte alpha) => new(c.R, c.G, c.B, alpha);
}
