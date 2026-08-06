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

        // fab-button.scss: `transition: all ease-in-out 300ms; transition-property: transform, opacity`
        // — the curve behind both fab reveals: the main button's close-icon/inner cross-fade, and a
        // list button scaling up from 0. A fresh list per rule: Transition instances are mutable, so
        // sharing one list across rules would let a later edit leak into earlier rules.
        List<Transition> RevealTransition() =>
        [
            new Transition(nameof(Style.Transform), t.FabTransitionDuration, TimingFunction.EaseInOut),
            new Transition(nameof(Style.Opacity), t.FabTransitionDuration, TimingFunction.EaseInOut),
        ];

        var css = new CssObject
        {
            // --- ion-fab (container) --------------------------------------------------------------
            // fab.scss :host — absolute, high z-index, width/height fit-content.
            // fit-content (not auto) is load-bearing here: the centering rules below pin BOTH edges
            // of an axis (left:0;right:0) and let auto margins take the leftover. With width:auto
            // CSS would instead solve the size from those insets and stretch the host across the
            // whole containing block — swallowing every pointer event at z-index 1000 and parking
            // its button in the corner. fit-content keeps the host shrink-wrapped so the auto
            // margins actually have leftover space to center it with.
            [$".ion-fab.{mode}"] = new()
            {
                Position = Position.Absolute,
                Width = Length.FitContent,
                Height = Length.FitContent,
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

            // Edge offsets (fab.scss). The fab button is pulled up (top edge) or down (bottom edge)
            // by half its own height so it straddles the header/footer line instead of sitting
            // wholly inside the content. Percentage margins resolve against the containing block's
            // WIDTH (CSS) — the fab host is fit-content, i.e. exactly as wide as the round button,
            // so -50% is half the button's size on both axes. `.fab-slotted` restricts these to the
            // fab's own children (Ionic's ::slotted), keeping a fab-list's buttons out of it.
            [$".ion-fab.{mode}.fab-vertical-top.fab-edge .fab-slotted.ion-fab-button"] = new()
            {
                MarginTop = Length.Percent(-50),
            },
            [$".ion-fab.{mode}.fab-vertical-bottom.fab-edge .fab-slotted.ion-fab-button"] = new()
            {
                MarginBottom = Length.Percent(-50),
            },
            // A small main button already carries 8px top/bottom margin; the edge offset overrides
            // margin-top outright, so fold that margin back in: (-100% + 2*small-margin) / 2.
            [$".ion-fab.{mode}.fab-vertical-top.fab-edge .fab-slotted.ion-fab-button.fab-button-small"] = new()
            {
                MarginTop = (Length.Percent(-100) + smallMargin * 2f) / 2f,
            },
            [$".ion-fab.{mode}.fab-vertical-bottom.fab-edge .fab-slotted.ion-fab-button.fab-button-small"] = new()
            {
                MarginBottom = (Length.Percent(-100) + smallMargin * 2f) / 2f,
            },
            // The sibling lists must follow the button, or a gap opens between them. Horizontal
            // lists shift by the same -50%; vertical lists keep their calc(100% + list-margin)
            // offset but measured from the moved button, i.e. 50% + list-margin.
            [$".ion-fab.{mode}.fab-vertical-top.fab-edge .fab-slotted.ion-fab-list.fab-list-side-start"] = new()
            {
                MarginTop = Length.Percent(-50),
            },
            [$".ion-fab.{mode}.fab-vertical-top.fab-edge .fab-slotted.ion-fab-list.fab-list-side-end"] = new()
            {
                MarginTop = Length.Percent(-50),
            },
            [$".ion-fab.{mode}.fab-vertical-top.fab-edge .fab-slotted.ion-fab-list.fab-list-side-top"] = new()
            {
                MarginTop = Calc(Length.Percent(50), listMargin),
            },
            [$".ion-fab.{mode}.fab-vertical-top.fab-edge .fab-slotted.ion-fab-list.fab-list-side-bottom"] = new()
            {
                MarginTop = Calc(Length.Percent(50), listMargin),
            },
            [$".ion-fab.{mode}.fab-vertical-bottom.fab-edge .fab-slotted.ion-fab-list.fab-list-side-start"] = new()
            {
                MarginBottom = Length.Percent(-50),
            },
            [$".ion-fab.{mode}.fab-vertical-bottom.fab-edge .fab-slotted.ion-fab-list.fab-list-side-end"] = new()
            {
                MarginBottom = Length.Percent(-50),
            },
            [$".ion-fab.{mode}.fab-vertical-bottom.fab-edge .fab-slotted.ion-fab-list.fab-list-side-top"] = new()
            {
                MarginBottom = Calc(Length.Percent(50), listMargin),
            },
            [$".ion-fab.{mode}.fab-vertical-bottom.fab-edge .fab-slotted.ion-fab-list.fab-list-side-bottom"] = new()
            {
                MarginBottom = Calc(Length.Percent(50), listMargin),
            },

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
                Transitions = RevealTransition(),
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
                Transitions = RevealTransition(),
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
                // Scale/fade in when the fab opens (and back out when it closes) rather than
                // popping — Ionic gets the same effect from the button's own transform/opacity
                // transition. Ionic additionally staggers the buttons with a per-index 30ms
                // setTimeout; Miko has no such timer, so they animate together.
                Transitions = RevealTransition(),
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
        // (fab-list.scss :host(.fab-list-side-start|end) ::slotted(.fab-button-in-list)) — margins
        // only; the hidden opacity/transform and the reveal transition come from the base
        // `.fab-button-in-list` rule above and are not re-declared here (which would also mean
        // out-specifying `.fab-button-show` and pinning these buttons hidden forever).
        css[$".ion-fab-list.{mode}.fab-list-side-start .fab-button-in-list"] = new()
        {
            MarginTop = Length.Px(0),
            MarginBottom = Length.Px(0),
            MarginLeft = Length.Px(5),
            MarginRight = Length.Px(5),
        };
        css[$".ion-fab-list.{mode}.fab-list-side-end .fab-button-in-list"] = new()
        {
            MarginTop = Length.Px(0),
            MarginBottom = Length.Px(0),
            MarginLeft = Length.Px(5),
            MarginRight = Length.Px(5),
        };

        // --- Hover ---------------------------------------------------------------------------
        // Ionic paints hover as an overlay on `.button-native::after` (--background-hover at
        // --background-hover-opacity), behind an `@media (any-hover: hover)` guard. Miko has no
        // ::after opacity layer and no pointer-capability media query (touch devices simply never
        // hover), so — exactly as ButtonStyles does — the wash is composited onto the resolved fill
        // and exposed as a plain `:hover` rule.
        //
        // md's overlay is `currentColor`, i.e. the button's own label color, at 8%: a white-labelled
        // primary fab lightens, a dark-labelled light fab darkens. The rules anchor on the host
        // `:hover` (hover propagates up the hit chain, so hovering the native surface flags the host)
        // and target `.button-native`, out-specifying the equal-structure fill rules above.
        css[$".ion-fab-button.{mode}:hover .button-native"] = new()
        {
            BackgroundColor = Composite(t.FabBackground, t.FabColor, t.FabHoverOpacity),
        };
        // A button inside a fab-list hovers toward `light tint` (both modes) — the same wash with
        // that surface's own dark label.
        css[$".ion-fab-button.{mode}.fab-button-in-list:hover .button-native"] = new()
        {
            BackgroundColor = Composite(t.FabListButtonBackground, t.FabListButtonColor, t.FabHoverOpacity),
        };
        // Disabled buttons never show a hover response (they are pointer-events:none, but a rule
        // matching on :hover would still win if the state were ever set by other means).
        css[$".ion-fab-button.{mode}.fab-button-disabled:hover .button-native"] = new()
        {
            BackgroundColor = t.FabBackground,
        };

        // --- Named color fills (Ionic --ion-color-* palette) -------------------------------------
        // A fab button with a color fills with that base and uses its contrast label.
        AddColorFill(css, mode, "primary", t.Primary, Color.FromHex("ffffff"), t.FabHoverOpacity);
        AddColorFill(css, mode, "secondary", t.Secondary, Color.FromHex("ffffff"), t.FabHoverOpacity);
        AddColorFill(css, mode, "tertiary", t.Tertiary, Color.FromHex("ffffff"), t.FabHoverOpacity);
        AddColorFill(css, mode, "success", t.Success, Color.FromHex("000000"), t.FabHoverOpacity);
        AddColorFill(css, mode, "warning", t.Warning, Color.FromHex("000000"), t.FabHoverOpacity);
        AddColorFill(css, mode, "danger", t.Danger, Color.FromHex("ffffff"), t.FabHoverOpacity);
        AddColorFill(css, mode, "light", t.Light, Color.FromHex("000000"), t.FabHoverOpacity);
        AddColorFill(css, mode, "medium", t.Medium, Color.FromHex("ffffff"), t.FabHoverOpacity);
        AddColorFill(css, mode, "dark", t.Dark, Color.FromHex("ffffff"), t.FabHoverOpacity);

        return css;
    }

    // margin: calc(100% + list-margin) — an absolute box centered relative to the main button. Miko
    // Length addition combines percent + px into a calc-style length.
    private static Length Calc(Length percent, Length px) => percent + px;

    // Base fill + contrast label on the native surface for a named palette color, plus the hover
    // wash for that fill. Ionic's md rule for a colored fab is
    // `:host(.ion-color:hover) .button-native::after { background: current-color(contrast) }` —
    // i.e. the same currentColor overlay, resolved against this palette entry's contrast.
    private static void AddColorFill(CssObject css, string mode, string name, Color baseColor, Color contrast, float hoverOpacity)
    {
        css[$".ion-fab-button.{mode}.ion-color-{name} .button-native"] = new()
        {
            BackgroundColor = baseColor,
            Color = contrast,
        };
        // Must out-specify the plain `.ion-fab-button.{mode}:hover .button-native` rule above, which
        // it does on class count (host carries both the mode and the ion-color-* class).
        css[$".ion-fab-button.{mode}.ion-color-{name}:hover .button-native"] = new()
        {
            BackgroundColor = Composite(baseColor, contrast, hoverOpacity),
        };
    }

    /// <summary>
    /// Composites <paramref name="overlay"/> at <paramref name="overlayOpacity"/> over the opaque
    /// <paramref name="baseColor"/> (source-over), yielding an opaque result. Mirrors Ionic's
    /// <c>.button-native::after</c> hover overlay, which Miko cannot express as a separate layer
    /// (same approach as <see cref="ButtonStyles"/>).
    /// </summary>
    private static Color Composite(Color baseColor, Color overlay, float overlayOpacity)
    {
        float a = Math.Clamp(overlayOpacity, 0f, 1f);
        byte Blend(byte b, byte o) => (byte)Math.Round(b * (1 - a) + o * a);
        return new Color(Blend(baseColor.R, overlay.R), Blend(baseColor.G, overlay.G), Blend(baseColor.B, overlay.B));
    }
}
