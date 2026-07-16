using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the FAB family (<c>ion-fab</c>, <c>ion-fab-button</c>, <c>ion-fab-list</c>). Ported
/// from the Ionic source: <c>fab.scss</c>, <c>fab-button.scss</c> (+ <c>.md</c>/<c>.ios</c>), and
/// <c>fab-list.scss</c> plus their <c>*.vars.scss</c>.
/// <para>
/// DOM mirrors Ionic's host structure:
/// <code>
/// &lt;div class="ion-fab ..."&gt;              &lt;!-- absolute container, sized to content --&gt;
///   &lt;div class="ion-fab-button ..."&gt;      &lt;!-- the round host --&gt;
///     &lt;button class="button-native"&gt;      &lt;!-- painted surface: radius 50%, bg, shadow --&gt;
///       &lt;ion-icon class="close-icon" /&gt;   &lt;!-- close glyph, faded in when active --&gt;
///       &lt;span class="button-inner"&gt;...&lt;/span&gt;
///     &lt;/button&gt;
///   &lt;/div&gt;
///   &lt;div class="ion-fab-list ..."&gt;...&lt;/div&gt;   &lt;!-- optional mini-button lists --&gt;
/// &lt;/div&gt;
/// </code>
/// Ionic targets slotted descendants with <c>::slotted()</c>; Miko has no shadow DOM, so the rules
/// here target the built child classes directly. Rules are scoped by the active mode class
/// (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class FabStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var margin = Length.Px(t.FabContentMargin);
        var listMargin = Length.Px(t.FabListMargin);
        var smallMargin = Length.Px(t.FabButtonSmallMargin);

        var css = new CssObject
        {
            // --- ion-fab (container) --------------------------------------------------------------
            // fab.scss :host — absolute, high z-index, width/height fit-content. In Miko an absolute
            // box with no width constraint already shrink-wraps its content (BlockLayout), so we
            // leave width/height unset to match fit-content.
            [$".ion-fab.{mode}"] = new()
            {
                Position = Position.Absolute,
                ZIndex = 1000,   // $z-index-fixed-content
            },

            // Horizontal positioning (fab.scss). center: pinned left+right with auto side margins;
            // start/end: pinned to that edge by the content margin. Safe-area env() insets are
            // dropped per Miko's safe-area model (opt-in only).
            [$".ion-fab.{mode}.fab-horizontal-center"] = new()
            {
                Left = Length.Px(0),
                Right = Length.Px(0),
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
            },
            [$".ion-fab.{mode}.fab-horizontal-start"] = new() { Left = margin },
            [$".ion-fab.{mode}.fab-horizontal-end"] = new() { Right = margin },

            // Vertical positioning (fab.scss). center: pinned top+bottom with auto vertical margins.
            [$".ion-fab.{mode}.fab-vertical-top"] = new() { Top = margin },
            [$".ion-fab.{mode}.fab-vertical-bottom"] = new() { Bottom = margin },
            [$".ion-fab.{mode}.fab-vertical-center"] = new()
            {
                Top = Length.Px(0),
                Bottom = Length.Px(0),
                MarginTop = Length.Auto,
                MarginBottom = Length.Auto,
            },
            // Edge resets the pinned value (edge styling uses margin instead).
            [$".ion-fab.{mode}.fab-vertical-top.fab-edge"] = new() { Top = Length.Px(0) },
            [$".ion-fab.{mode}.fab-vertical-bottom.fab-edge"] = new() { Bottom = Length.Px(0) },

            // --- ion-fab-button (host) ------------------------------------------------------------
            // fab-button.scss :host — a fixed-size block, 14px font, centered text. margin:0.
            [$".ion-fab-button.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Px(t.FabSize),
                Height = Length.Px(t.FabSize),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(0),
                MarginLeft = Length.Px(0),
                MarginRight = Length.Px(0),
                FontSize = Length.Px(14),
                TextAlign = TextAlign.Center,
            },

            // .button-native — the painted round surface. Fills the host, radius 50% (a circle),
            // per-mode background + label color + elevation shadow, clipped content.
            [$".ion-fab-button.{mode} .button-native"] = new()
            {
                Position = Position.Relative,
                Display = Display.Block,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BorderRadius = new BorderRadius(Length.Percent(50)),
                BorderWidth = Length.Px(0),
                BackgroundColor = t.FabBackground,
                Color = t.FabColor,
                BoxShadow = t.FabBoxShadow.Count > 0 ? (StyleProperty<List<BoxShadow>>?)t.FabBoxShadow : null,
                Cursor = Cursor.Pointer,
                Overflow = Overflow.Hidden,
                BoxSizing = BoxSizing.BorderBox,
                ZIndex = 0,
            },

            // .button-inner — centers the slotted content (the icon), fills the surface, sits above
            // the (absolutely-positioned) close icon. Faded out when the close icon is active.
            [$".ion-fab-button.{mode} .button-inner"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                FlexShrink = 0,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Height = Length.Percent(100),
                Opacity = 1f,
                ZIndex = 1,
            },

            // Slotted icon size for the main button (per-mode font size).
            [$".ion-fab-button.{mode} .button-inner .ion-icon"] = new()
            {
                Width = Length.Px(t.FabIconFontSize),
                Height = Length.Px(t.FabIconFontSize),
            },

            // .close-icon — the close glyph, absolutely centered, hidden (opacity 0) and shrunk +
            // rotated until the button is close-active. z-index 1 like the inner content.
            [$".ion-fab-button.{mode} .close-icon"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
                Height = Length.Percent(100),
                Width = Length.Px(t.FabIconFontSize),
                Opacity = 0f,
                ZIndex = 1,
                Transform = new Transform(
                    new TransformFunction.Scale(0.4f, 0.4f),
                    new TransformFunction.Rotate(-45f)),
            },

            // Close-active swap: fade+un-rotate the close icon in, fade the inner content out.
            [$".ion-fab-button.{mode}.fab-button-close-active .close-icon"] = new()
            {
                Opacity = 1f,
                Transform = new Transform(
                    new TransformFunction.Scale(1f, 1f),
                    new TransformFunction.Rotate(0f)),
            },
            [$".ion-fab-button.{mode}.fab-button-close-active .button-inner"] = new()
            {
                Opacity = 0f,
                Transform = new Transform(
                    new TransformFunction.Scale(0.4f, 0.4f),
                    new TransformFunction.Rotate(45f)),
            },

            // Disabled host — dimmed and non-interactive (fab-button.scss :host(.fab-button-disabled)).
            [$".ion-fab-button.{mode}.fab-button-disabled"] = new()
            {
                Opacity = 0.5f,
                Cursor = Cursor.Default,
                PointerEvents = PointerEvents.None,
            },

            // Mini (small) button — smaller box with 8px margin (fab-button.scss :host(.fab-button-small)).
            [$".ion-fab-button.{mode}.fab-button-small"] = new()
            {
                Width = Length.Px(t.FabSmallSize),
                Height = Length.Px(t.FabSmallSize),
                MarginTop = smallMargin,
                MarginBottom = smallMargin,
                MarginLeft = smallMargin,
                MarginRight = smallMargin,
            },

            // In-list button — the light surface + dark label, smaller icon (fab-button.*.scss
            // :host(.fab-button-in-list)). Sized 40px like a mini button by the list rules below.
            [$".ion-fab-button.{mode}.fab-button-in-list .button-native"] = new()
            {
                BackgroundColor = t.FabListButtonBackground,
                Color = t.FabListButtonColor,
            },
            [$".ion-fab-button.{mode}.fab-button-in-list .button-inner .ion-icon"] = new()
            {
                Width = Length.Px(t.FabListButtonIconSize),
                Height = Length.Px(t.FabListButtonIconSize),
            },

            // --- ion-fab-list ---------------------------------------------------------------------
            // fab-list.scss :host — hidden until active; absolute column centered on the main button,
            // offset by 100% + list margin so it sits just past the button.
            [$".ion-fab-list.{mode}"] = new()
            {
                Display = Display.None,
                Position = Position.Absolute,
                Top = Length.Px(0),
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Center,
                MinWidth = Length.Px(t.FabSmallSize + t.FabButtonSmallMargin * 2),
                MinHeight = Length.Px(t.FabSmallSize + t.FabButtonSmallMargin * 2),
                MarginTop = Calc(Length.Percent(100), listMargin),
                MarginBottom = Calc(Length.Percent(100), listMargin),
            },

            // Active list — shown as a flex column/row.
            [$".ion-fab-list.{mode}.fab-list-active"] = new()
            {
                Display = Display.Flex,
            },

            // List buttons (fab-list.scss ::slotted(.fab-button-in-list)) — 40px, hidden until shown.
            [$".ion-fab-list.{mode} .fab-button-in-list"] = new()
            {
                Width = Length.Px(t.FabSmallSize),
                Height = Length.Px(t.FabSmallSize),
                MarginTop = smallMargin,
                MarginBottom = smallMargin,
                MarginLeft = Length.Px(0),
                MarginRight = Length.Px(0),
                Opacity = 0f,
                Transform = new Transform(new TransformFunction.Scale(0f, 0f)),
            },
            // Shown list button — full scale + opacity (::slotted(.fab-button-in-list.fab-button-show)).
            [$".ion-fab-list.{mode} .fab-button-in-list.fab-button-show"] = new()
            {
                Opacity = 1f,
                Transform = new Transform(new TransformFunction.Scale(1f, 1f)),
            },

            // Side placement (fab-list.scss). top: stack above the button (column-reverse, pinned to
            // the bottom). start/end: lay out horizontally to that side.
            [$".ion-fab-list.{mode}.fab-list-side-top"] = new()
            {
                Top = Length.Auto,
                Bottom = Length.Px(0),
                FlexDirection = FlexDirection.ColumnReverse,
            },
            [$".ion-fab-list.{mode}.fab-list-side-bottom"] = new()
            {
                FlexDirection = FlexDirection.Column,
            },
            [$".ion-fab-list.{mode}.fab-list-side-start"] = new()
            {
                FlexDirection = FlexDirection.RowReverse,
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(0),
                MarginRight = Calc(Length.Percent(100), listMargin),
                Right = Length.Px(0),
                Left = Length.Auto,
            },
            [$".ion-fab-list.{mode}.fab-list-side-end"] = new()
            {
                FlexDirection = FlexDirection.Row,
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(0),
                MarginLeft = Calc(Length.Percent(100), listMargin),
                Left = Length.Px(0),
            },
        };

        // Horizontal list side buttons use left/right margins instead of top/bottom
        // (fab-list.scss :host(.fab-list-side-start|end) ::slotted(.fab-button-in-list)).
        css[$".ion-fab-list.{mode}.fab-list-side-start .fab-button-in-list"] = new()
        {
            MarginTop = Length.Px(0),
            MarginBottom = Length.Px(0),
            MarginLeft = Length.Px(5),
            MarginRight = Length.Px(5),
            Opacity = 0f,
            Transform = new Transform(new TransformFunction.Scale(0f, 0f)),
        };
        css[$".ion-fab-list.{mode}.fab-list-side-end .fab-button-in-list"] = new()
        {
            MarginTop = Length.Px(0),
            MarginBottom = Length.Px(0),
            MarginLeft = Length.Px(5),
            MarginRight = Length.Px(5),
            Opacity = 0f,
            Transform = new Transform(new TransformFunction.Scale(0f, 0f)),
        };

        // --- Named color fills (Ionic --ion-color-* palette) -------------------------------------
        // A fab button with a color fills with that base and uses its contrast label.
        AddColorFill(css, mode, "primary", t.Primary, Color.FromHex("ffffff"));
        AddColorFill(css, mode, "secondary", t.Secondary, Color.FromHex("ffffff"));
        AddColorFill(css, mode, "tertiary", t.Tertiary, Color.FromHex("ffffff"));
        AddColorFill(css, mode, "success", t.Success, Color.FromHex("000000"));
        AddColorFill(css, mode, "warning", t.Warning, Color.FromHex("000000"));
        AddColorFill(css, mode, "danger", t.Danger, Color.FromHex("ffffff"));
        AddColorFill(css, mode, "light", t.Light, Color.FromHex("000000"));
        AddColorFill(css, mode, "medium", t.Medium, Color.FromHex("ffffff"));
        AddColorFill(css, mode, "dark", t.Dark, Color.FromHex("ffffff"));

        return css;
    }

    // margin: calc(100% + list-margin) — an absolute box centered relative to the main button. Miko
    // Length addition combines percent + px into a calc-style length.
    private static Length Calc(Length percent, Length px) => percent + px;

    // Base fill + contrast label on the native surface for a named palette color.
    private static void AddColorFill(CssObject css, string mode, string name, Color baseColor, Color contrast)
    {
        css[$".ion-fab-button.{mode}.ion-color-{name} .button-native"] = new()
        {
            BackgroundColor = baseColor,
            Color = contrast,
        };
    }
}
