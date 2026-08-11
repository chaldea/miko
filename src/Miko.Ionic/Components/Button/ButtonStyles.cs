using Miko.Animation;
using Miko.Common;
using Miko.Ionic.Styles;
using Miko.Styling;
using static Miko.Ionic.Styles.IonicMixins;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-button</c>. Ported from the Ionic source: <c>button.scss</c> (shared base) plus
/// <c>button.md.scss</c> / <c>button.ios.scss</c> (per-mode overrides) and their <c>*.vars.scss</c>.
/// <para>
/// DOM mirrors Ionic's host structure:
/// <code>
/// &lt;div class="ion-button ..."&gt;        &lt;!-- host: inline-block, the fill/size/shape/expand classes --&gt;
///   &lt;button class="button-native"&gt;      &lt;!-- the filled clickable surface (radius/border/bg/padding) --&gt;
///     &lt;span class="button-inner"&gt;        &lt;!-- row, centers the slotted content --&gt;
///       (content)
///     &lt;/span&gt;
///   &lt;/button&gt;
/// &lt;/div&gt;
/// </code>
/// The host carries <c>--background</c> / <c>--color</c> semantics through the fill classes
/// (<c>button-solid</c> / <c>button-outline</c> / <c>button-clear</c>); the <c>.button-native</c>
/// paints the actual surface. Size (<c>button-small</c> / <c>-large</c>), shape (<c>button-round</c>),
/// and expand (<c>button-block</c> / <c>-full</c>) tune padding / radius / width.
/// </para>
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/>
/// for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class ButtonStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-button — the host. inline-block so it sizes to its content and flows inline with
            // sibling buttons (Ionic's :host { display: inline-block; width: auto; }). Carries the
            // label color, font, transform, and tracking; the native button inherits them.
            [$".ion-button.{mode}"] = new()
            {
                Display = Display.InlineBlock,
                Color = t.ButtonTextColor,
                FontSize = Length.Px(t.ButtonFontSize),
                FontWeight = t.ButtonFontWeight,
                TextTransform = t.ButtonTextTransform,
                LetterSpacing = t.ButtonLetterSpacing,
                TextAlign = TextAlign.Center,
                TextDecoration = TextDecoration.None,
                MinHeight = t.ButtonMinHeight,
                MarginTop = Length.Px(4),
                MarginBottom = Length.Px(4),
                MarginLeft = Length.Px(2),
                MarginRight = Length.Px(2),
                VerticalAlign = VerticalAlign.Top,
            },

            // disabled host — dimmed and non-interactive (button.scss :host(.button-disabled)).
            [$".ion-button.{mode}.button-disabled"] = new()
            {
                Opacity = 0.5f,
                Cursor = Cursor.Default,
                PointerEvents = PointerEvents.None,
            },

            // .button-native — the painted surface. Flex row filling the host, centered, with the
            // host padding (Ionic's --padding-*), the resolved border radius, and pointer cursor.
            // Background/border/color come from the fill rules below; default here is the solid fill.
            [$".ion-button.{mode} .button-native"] = new()
            {
                // Ionic 的 .button-native { @include text-inherit(); } —— 从宿主继承字体/颜色/字距等。
                // 直接书写的属性（如下方的 Color/FontSize 覆盖，以及 MinHeight = Inherit）胜过混入。
                ["..."] = TextInherit(),
                Position = Position.Relative,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                // Ionic's .button-native uses `min-height: inherit` — the native surface takes
                // whatever min-height the host resolved. Miko has no `inherit` keyword, so we mirror
                // the host value here (and in every variant below that changes the host min-height).
                // Without this, changing the host min-height (icon-only / small / large) leaves the
                // native surface stuck at the default, so it no longer fills the host (ISSUE: #4).
                MinHeight = Inherit,
                PaddingTop = t.ButtonPaddingTop,
                PaddingBottom = t.ButtonPaddingBottom,
                PaddingLeft = t.ButtonPaddingStart,
                PaddingRight = t.ButtonPaddingEnd,
                BorderRadius = Radius(t.ButtonBorderRadius),
                BorderWidth = Length.Px(0),
                LineHeight = Length.Number(1),
                Cursor = Cursor.Pointer,
                BoxSizing = BoxSizing.BorderBox,
                ZIndex = 0,
            },

            // .button-inner — centers the label/icon row (button.scss .button-inner).
            [$".ion-button.{mode} .button-native .button-inner"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                FlexShrink = 0,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                ZIndex = 1,
            },

            // Slots — start/end marker spans don't shrink (button.scss ::slotted([slot=start|end])).
            [$".ion-button.{mode} .ion-slot-start"] = new() { Display = Display.Flex, FlexShrink = 0 },
            [$".ion-button.{mode} .ion-slot-end"] = new() { Display = Display.Flex, FlexShrink = 0 },
            [$".ion-button.{mode} .ion-slot-icon-only"] = new() { Display = Display.Flex, FlexShrink = 0 },

            // Slotted icons scale with the button font (button.scss ::slotted(ion-icon)
            // { font-size: 1.35em; pointer-events: none } + icon.scss :host { width/height: 1em }).
            // The 1.35em font-size resolves against the inherited button font — which the
            // button-small / button-large host rules change — so start/end icons follow Size
            // (md: 13/14/20px → 17.55/18.9/27px); width/height: 1em then turns that font-size
            // into the icon box. The icon-only rules below override the box with explicit px at
            // higher specificity, so they are unaffected (ISSUE: #6).
            [$".ion-button.{mode} .ion-icon"] = new()
            {
                FontSize = Length.Em(1.35f),
                Width = Length.Em(1f),
                Height = Length.Em(1f),
                PointerEvents = PointerEvents.None,
            },

            // Slotted icons carry a small side gap toward the label (button.scss ::slotted(ion-icon[slot=…])).
            // start icon: gap on its trailing (right) edge; end icon: gap on its leading (left) edge.
            // Like CSS, the em margin resolves against the icon's own font-size (1.35em above).
            [$".ion-button.{mode} .ion-slot-start .ion-icon"] = new() { MarginRight = Length.Em(0.3f) },
            [$".ion-button.{mode} .ion-slot-end .ion-icon"] = new() { MarginLeft = Length.Em(0.3f) },
        };

        // --- Fill variants -------------------------------------------------------------------
        // Solid: primary fill, contrast (white) label, mode elevation shadow.
        css[$".ion-button.{mode}.button-solid .button-native"] = new()
        {
            BackgroundColor = t.ButtonSolidBackground,
            Color = t.ButtonSolidColor,
            BoxShadow = t.ButtonSolidBoxShadow.Count > 0 ? (StyleProperty<List<BoxShadow>>?)t.ButtonSolidBoxShadow : null,
        };

        // Outline: transparent fill, primary border + label.
        css[$".ion-button.{mode}.button-outline .button-native"] = new()
        {
            BackgroundColor = Color.Transparent,
            Color = t.ButtonTextColor,
            BorderWidth = t.ButtonOutlineBorderWidth,
            BorderStyle = BorderStyle.Solid,
            BorderColor = t.ButtonTextColor,
        };

        // Clear: transparent fill, no border, primary label.
        css[$".ion-button.{mode}.button-clear .button-native"] = new()
        {
            BackgroundColor = Color.Transparent,
            Color = t.ButtonTextColor,
            BorderWidth = Length.Px(0),
        };

        // --- Hover ---------------------------------------------------------------------------
        // Ionic paints hover as a semi-transparent overlay on `.button-native::after`
        // (--background-hover at --background-hover-opacity) behind an `@media (any-hover: hover)`
        // guard. Miko has no `::after` opacity layer (and no pointer-capability media query — touch
        // devices simply never hover), so we composite that wash onto the resolved fill and expose
        // it as a plain `:hover` rule, mirroring how ChipStyles handles its hover shift.
        //
        // Values follow Ionic's MD overlay model (the one expressible with the palette we carry):
        // a solid button lightens with an 8% white overlay; outline/clear buttons get a 4% wash of
        // their base color over the transparent fill. The rules anchor on the host `:hover` (the
        // hover state propagates up the hit chain, so hovering the native surface flags the host
        // too) and target `.button-native`, so they outrank the equal-structure fill rules above.
        css[$".ion-button.{mode}.button-solid:hover .button-native"] = new()
        {
            BackgroundColor = Composite(t.ButtonSolidBackground, Color.White, 0.08f),
        };
        css[$".ion-button.{mode}.button-outline:hover .button-native"] = new()
        {
            BackgroundColor = WithAlpha(t.ButtonTextColor, 10), // base @ 0.04
        };
        css[$".ion-button.{mode}.button-clear:hover .button-native"] = new()
        {
            BackgroundColor = WithAlpha(t.ButtonTextColor, 10), // base @ 0.04
        };

        // --- Expand variants -----------------------------------------------------------------
        // block: full-width host, no horizontal margin on the native button.
        css[$".ion-button.{mode}.button-block"] = new() { Display = Display.Block };
        css[$".ion-button.{mode}.button-block .button-native"] = new()
        {
            MarginLeft = Length.Px(0),
            MarginRight = Length.Px(0),
            Width = Length.Percent(100),
        };

        // full: full-width host, square corners, no left/right border (button.scss).
        css[$".ion-button.{mode}.button-full"] = new() { Display = Display.Block };
        css[$".ion-button.{mode}.button-full .button-native"] = new()
        {
            MarginLeft = Length.Px(0),
            MarginRight = Length.Px(0),
            Width = Length.Percent(100),
        };
        css[$".ion-button.{mode}.button-full .button-native"].BorderRadius =
            new BorderRadius(Length.Px(0));

        // --- Size variants -------------------------------------------------------------------
        css[$".ion-button.{mode}.button-small"] = new()
        {
            MinHeight = t.ButtonSmallMinHeight,
            FontSize = Length.Px(t.ButtonSmallFontSize),
        };
        css[$".ion-button.{mode}.button-small .button-native"] = new()
        {
            MinHeight = t.ButtonSmallMinHeight,   // mirror host min-height (Ionic: min-height: inherit)
            PaddingTop = t.ButtonSmallPaddingTop,
            PaddingBottom = t.ButtonSmallPaddingBottom,
            PaddingLeft = t.ButtonSmallPaddingX,
            PaddingRight = t.ButtonSmallPaddingX,
            BorderRadius = Radius(t.ButtonSmallBorderRadius),
        };

        css[$".ion-button.{mode}.button-large"] = new()
        {
            MinHeight = t.ButtonLargeMinHeight,
            FontSize = Length.Px(t.ButtonLargeFontSize),
        };
        css[$".ion-button.{mode}.button-large .button-native"] = new()
        {
            MinHeight = t.ButtonLargeMinHeight,   // mirror host min-height (Ionic: min-height: inherit)
            PaddingTop = t.ButtonLargePaddingTop,
            PaddingBottom = t.ButtonLargePaddingBottom,
            PaddingLeft = t.ButtonLargePaddingX,
            PaddingRight = t.ButtonLargePaddingX,
            BorderRadius = Radius(t.ButtonLargeBorderRadius),
        };

        // --- Shape: round --------------------------------------------------------------------
        css[$".ion-button.{mode}.button-round .button-native"] = new()
        {
            PaddingTop = Length.Px(0),
            PaddingBottom = Length.Px(0),
            PaddingLeft = Length.Px(26),
            PaddingRight = Length.Px(26),
            BorderRadius = Radius(t.ButtonRoundBorderRadius),
        };

        // --- Icon-only -----------------------------------------------------------------------
        // Square button, symmetric zero padding (button.*.scss :host(.button-has-icon-only)).
        css[$".ion-button.{mode}.button-has-icon-only"] = new()
        {
            MinWidth = Length.Px(t.ButtonIconOnlyMinSize),
            MinHeight = Length.Px(t.ButtonIconOnlyMinSize),
        };
        css[$".ion-button.{mode}.button-has-icon-only .button-native"] = new()
        {
            // Mirror the icon-only host min-height (Ionic: min-height: inherit) so the square native
            // surface follows the host instead of staying at the default 36px (ISSUE: #4).
            MinHeight = Length.Px(t.ButtonIconOnlyMinSize),
            PaddingTop = Length.Px(0),
            PaddingBottom = Length.Px(0),
            PaddingLeft = Length.Px(0),
            PaddingRight = Length.Px(0),
        };
        // The icon-only icon is larger than a start/end icon (button.*.scss
        // ::slotted(ion-icon[slot="icon-only"])).
        css[$".ion-button.{mode}.button-has-icon-only .ion-slot-icon-only .ion-icon"] = new()
        {
            Width = Length.Px(t.ButtonIconOnlyIconSize),
            Height = Length.Px(t.ButtonIconOnlyIconSize),
        };

        // Small icon-only: a smaller square with a smaller icon
        // (:host(.button-small.button-has-icon-only) and its slotted icon).
        css[$".ion-button.{mode}.button-small.button-has-icon-only"] = new()
        {
            MinWidth = Length.Px(t.ButtonSmallIconOnlyMinSize),
            MinHeight = Length.Px(t.ButtonSmallIconOnlyMinSize),
        };
        // Mirror the small icon-only host min-height onto the native surface (Ionic: min-height:
        // inherit). Needs its own 3-class rule: `.button-small .button-native` and
        // `.button-has-icon-only .button-native` collide at equal specificity, so neither carries the
        // small-icon-only value (ISSUE: #4).
        css[$".ion-button.{mode}.button-small.button-has-icon-only .button-native"] = new()
        {
            MinHeight = Length.Px(t.ButtonSmallIconOnlyMinSize),
        };
        css[$".ion-button.{mode}.button-small.button-has-icon-only .ion-slot-icon-only .ion-icon"] = new()
        {
            Width = Length.Px(t.ButtonSmallIconOnlyIconSize),
            Height = Length.Px(t.ButtonSmallIconOnlyIconSize),
        };

        // Large icon-only: a larger square with a larger icon
        // (:host(.button-large.button-has-icon-only) and its slotted icon).
        css[$".ion-button.{mode}.button-large.button-has-icon-only"] = new()
        {
            MinWidth = Length.Px(t.ButtonLargeIconOnlyMinSize),
            MinHeight = Length.Px(t.ButtonLargeIconOnlyMinSize),
        };
        // Mirror the large icon-only host min-height onto the native surface (Ionic: min-height:
        // inherit); same equal-specificity collision as the small case above (ISSUE: #4).
        css[$".ion-button.{mode}.button-large.button-has-icon-only .button-native"] = new()
        {
            MinHeight = Length.Px(t.ButtonLargeIconOnlyMinSize),
        };
        css[$".ion-button.{mode}.button-large.button-has-icon-only .ion-slot-icon-only .ion-icon"] = new()
        {
            Width = Length.Px(t.ButtonLargeIconOnlyIconSize),
            Height = Length.Px(t.ButtonLargeIconOnlyIconSize),
        };

        // --- Strong --------------------------------------------------------------------------
        css[$".ion-button.{mode}.button-strong"] = new()
        {
            FontWeight = t.ButtonStrongFontWeight,
        };

        // --- In a toolbar --------------------------------------------------------------------
        AddInToolbar(css, mode, t);

        // --- Named color fills (Ionic --ion-color-* palette) ---------------------------------
        // A solid button with a color fills with that base and uses its contrast label.
        AddSolidColor(css, mode, "primary", t.Primary, Color.FromHex("ffffff"));
        AddSolidColor(css, mode, "secondary", t.Secondary, Color.FromHex("ffffff"));
        AddSolidColor(css, mode, "tertiary", t.Tertiary, Color.FromHex("ffffff"));
        AddSolidColor(css, mode, "success", t.Success, Color.FromHex("000000"));
        AddSolidColor(css, mode, "warning", t.Warning, Color.FromHex("000000"));
        AddSolidColor(css, mode, "danger", t.Danger, Color.FromHex("ffffff"));
        AddSolidColor(css, mode, "light", t.Light, Color.FromHex("000000"));
        AddSolidColor(css, mode, "medium", t.Medium, Color.FromHex("ffffff"));
        AddSolidColor(css, mode, "dark", t.Dark, Color.FromHex("ffffff"));

        // Outline/clear with a color use that base for the border + label, transparent fill.
        AddTextColor(css, mode, "primary", t.Primary);
        AddTextColor(css, mode, "secondary", t.Secondary);
        AddTextColor(css, mode, "tertiary", t.Tertiary);
        AddTextColor(css, mode, "success", t.Success);
        AddTextColor(css, mode, "warning", t.Warning);
        AddTextColor(css, mode, "danger", t.Danger);
        AddTextColor(css, mode, "light", t.Light);
        AddTextColor(css, mode, "medium", t.Medium);
        AddTextColor(css, mode, "dark", t.Dark);

        return css;
    }

    private static BorderRadius Radius(float px) => new BorderRadius(Length.Px(px));

    /// <summary>
    /// Rules for a button sitting inside an <c>ion-toolbar</c>. Ported from two Ionic sources:
    /// <list type="bullet">
    ///   <item><description><c>button.scss</c>'s <c>:host(.in-toolbar…)</c> block — the label,
    ///   border, and solid fill are pulled from the toolbar's own color/background so a button
    ///   reads against the bar it sits on instead of against the page.</description></item>
    ///   <item><description><c>buttons.md.scss</c> / <c>buttons.ios.scss</c>'s
    ///   <c>::slotted(*) ion-button</c> block — the denser metrics (32px min-height, tighter
    ///   padding, squarer radius, larger icons) that apply only to buttons slotted through an
    ///   <c>ion-buttons</c> group.</description></item>
    /// </list>
    /// The <c>in-toolbar</c> / <c>in-buttons</c> markers are durably stamped by
    /// <see cref="IonButton"/> from the cascaded <see cref="ToolbarContext"/> (Ionic resolves them
    /// via <c>hostContext</c> / <c>closest</c>, which a detached Miko build cannot do from the
    /// button itself). The cascading approach survives button re-renders, unlike a Build() post-pass.
    /// <para>
    /// The color rules carry Ionic's <c>:not(.ion-color)</c> guard so an explicitly colored button
    /// keeps its own palette. Ionic also guards on <c>:not(.in-toolbar-color)</c> (a toolbar with a
    /// <c>color</c> attribute); <c>IonToolbar</c> has no <c>Color</c> parameter in this port, so
    /// that class never appears and the guard is omitted rather than written as dead selector text.
    /// </para>
    /// </summary>
    private static void AddInToolbar(CssObject css, string mode, IonicTheme t)
    {
        // :host(.in-toolbar:not(.ion-color)) .button-native — the label takes the toolbar's color.
        css[$".ion-button.{mode}.in-toolbar:not(.ion-color) .button-native"] = new()
        {
            Color = t.ToolbarColor,
        };

        // :host(.button-outline.in-toolbar:not(.ion-color)) .button-native — so does the border.
        css[$".ion-button.{mode}.button-outline.in-toolbar:not(.ion-color) .button-native"] = new()
        {
            BorderColor = t.ToolbarColor,
        };

        // :host(.button-solid.in-toolbar:not(.ion-color)) .button-native — a solid button inverts:
        // it fills with the toolbar's color and labels with the toolbar's background.
        css[$".ion-button.{mode}.button-solid.in-toolbar:not(.ion-color) .button-native"] = new()
        {
            BackgroundColor = t.ToolbarColor,
            Color = t.ToolbarBackground,
        };
        // Its hover keeps the 8% white wash the standalone solid rule uses, recomputed against the
        // toolbar fill (Ionic layers --background-hover over --background; see the hover note above).
        css[$".ion-button.{mode}.button-solid.in-toolbar:not(.ion-color):hover .button-native"] = new()
        {
            BackgroundColor = Composite(t.ToolbarColor, Color.White, 0.08f),
        };

        // --- buttons.{md,ios}.scss `::slotted(*) ion-button` ----------------------------------
        // Only buttons slotted through an ion-buttons group are re-metered; a bare button dropped
        // in the toolbar's default slot keeps its standalone size, matching Ionic's ::slotted scope.
        css[$".ion-button.{mode}.in-buttons"] = new()
        {
            MinHeight = t.ToolbarButtonMinHeight,
            MarginLeft = Length.Px(t.ToolbarButtonMarginX),
            MarginRight = Length.Px(t.ToolbarButtonMarginX),
        };
        css[$".ion-button.{mode}.in-buttons .button-native"] = new()
        {
            // Mirror the host min-height (Ionic's `min-height: inherit` on .button-native).
            MinHeight = t.ToolbarButtonMinHeight,
            PaddingTop = t.ToolbarButtonPaddingTop,
            PaddingBottom = t.ToolbarButtonPaddingBottom,
            PaddingLeft = t.ToolbarButtonPaddingStart,
            PaddingRight = t.ToolbarButtonPaddingEnd,
            // `::slotted(*) ion-button { --box-shadow: none }` (md) — a toolbar button is flat.
            BoxShadow = new List<BoxShadow>(),
        };

        // `::slotted(*) ion-button:not(.button-round) { --border-radius: … }` — the squarer
        // toolbar radius, except on a pill-shaped button which keeps its own.
        css[$".ion-button.{mode}.in-buttons:not(.button-round) .button-native"] = new()
        {
            BorderRadius = Radius(t.ToolbarButtonBorderRadius),
        };

        // `::slotted(*) .button-has-icon-only { --padding-top: 0; --padding-bottom: 0 }`.
        css[$".ion-button.{mode}.in-buttons.button-has-icon-only .button-native"] = new()
        {
            MinHeight = t.ToolbarButtonMinHeight,
            PaddingTop = Length.Px(0),
            PaddingBottom = Length.Px(0),
        };

        // `::slotted(*) ion-icon[slot=start|end|icon-only]` — a toolbar button's icons are sized
        // against the button font rather than the 1.35em the standalone button uses, and the
        // horizontal gap toward the label is asymmetric (.3em leading, .4em trailing). Ionic zeroes
        // the icon's own margins first, so the gap is the only margin left.
        css[$".ion-button.{mode}.in-buttons .ion-slot-start .ion-icon"] = new()
        {
            FontSize = Length.Em(t.ToolbarButtonIconFontSize),
            MarginLeft = Length.Px(0),
            MarginRight = Length.Em(0.3f),
        };
        css[$".ion-button.{mode}.in-buttons .ion-slot-end .ion-icon"] = new()
        {
            FontSize = Length.Em(t.ToolbarButtonIconFontSize),
            MarginLeft = Length.Em(0.4f),
            MarginRight = Length.Px(0),
        };
        // The icon-only icon is sized purely by font-size here (width/height stay 1em from the base
        // rule), so it must beat the base .button-has-icon-only rule's explicit px box.
        css[$".ion-button.{mode}.in-buttons.button-has-icon-only .ion-slot-icon-only .ion-icon"] = new()
        {
            FontSize = Length.Em(t.ToolbarButtonIconOnlyFontSize),
            Width = Length.Em(1f),
            Height = Length.Em(1f),
            MarginLeft = Length.Px(0),
            MarginRight = Length.Px(0),
        };

        // MD only: a clear icon-only toolbar button becomes a 3rem circular tap target
        // (buttons.md.scss `::slotted(*) .button-has-icon-only.button-clear`). iOS has no such rule.
        if (t.ToolbarButtonIconOnlyClearSize > 0)
        {
            css[$".ion-button.{mode}.in-buttons.button-has-icon-only.button-clear"] = new()
            {
                Width = Length.Px(t.ToolbarButtonIconOnlyClearSize),
                Height = Length.Px(t.ToolbarButtonIconOnlyClearSize),
                MinWidth = Length.Px(t.ToolbarButtonIconOnlyClearSize),
                MinHeight = Length.Px(t.ToolbarButtonIconOnlyClearSize),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(0),
                MarginLeft = Length.Px(0),
                MarginRight = Length.Px(0),
            };
            css[$".ion-button.{mode}.in-buttons.button-has-icon-only.button-clear .button-native"] = new()
            {
                MinHeight = Length.Px(t.ToolbarButtonIconOnlyClearSize),
                PaddingTop = t.ToolbarButtonIconOnlyClearPadding,
                PaddingBottom = t.ToolbarButtonIconOnlyClearPadding,
                PaddingLeft = t.ToolbarButtonIconOnlyClearPadding,
                PaddingRight = t.ToolbarButtonIconOnlyClearPadding,
                BorderRadius = new BorderRadius(Length.Percent(50)),
            };
        }
    }

    // Solid color: base fill + contrast label on the native surface.
    private static void AddSolidColor(CssObject css, string mode, string name, Color baseColor, Color contrast)
    {
        css[$".ion-button.{mode}.button-solid.ion-color-{name} .button-native"] = new()
        {
            BackgroundColor = baseColor,
            Color = contrast,
        };
        // Colored solid hover: same 8% white overlay as the default solid (Ionic MD tints the
        // colored fill on hover). Higher specificity than the plain `.button-solid:hover` rule, so
        // it wins for colored buttons.
        css[$".ion-button.{mode}.button-solid.ion-color-{name}:hover .button-native"] = new()
        {
            BackgroundColor = Composite(baseColor, Color.White, 0.08f),
        };
    }

    // Outline + clear color: base for border/label, transparent fill.
    private static void AddTextColor(CssObject css, string mode, string name, Color baseColor)
    {
        css[$".ion-button.{mode}.button-outline.ion-color-{name} .button-native"] = new()
        {
            BackgroundColor = Color.Transparent,
            Color = baseColor,
            BorderColor = baseColor,
        };
        css[$".ion-button.{mode}.button-clear.ion-color-{name} .button-native"] = new()
        {
            BackgroundColor = Color.Transparent,
            Color = baseColor,
        };
        // Colored outline/clear hover: a 4% wash of the button's own color over the transparent
        // fill (Ionic --background-hover: current-color(base), --background-hover-opacity: .04).
        css[$".ion-button.{mode}.button-outline.ion-color-{name}:hover .button-native"] = new()
        {
            BackgroundColor = WithAlpha(baseColor, 10), // base @ 0.04
        };
        css[$".ion-button.{mode}.button-clear.ion-color-{name}:hover .button-native"] = new()
        {
            BackgroundColor = WithAlpha(baseColor, 10), // base @ 0.04
        };
    }

    /// <summary>Same RGB as <paramref name="c"/> with a replaced alpha (for translucent washes).</summary>
    private static Color WithAlpha(Color c, byte alpha) => new(c.R, c.G, c.B, alpha);

    /// <summary>
    /// Composites <paramref name="overlay"/> at <paramref name="overlayOpacity"/> over the opaque
    /// <paramref name="baseColor"/> (source-over), yielding an opaque result. Mirrors Ionic's
    /// <c>.button-native::after</c> hover overlay, which Miko can't express as a separate layer.
    /// </summary>
    private static Color Composite(Color baseColor, Color overlay, float overlayOpacity)
    {
        float a = Math.Clamp(overlayOpacity, 0f, 1f);
        byte Blend(byte b, byte o) => (byte)Math.Round(b * (1 - a) + o * a);
        return new Color(Blend(baseColor.R, overlay.R), Blend(baseColor.G, overlay.G), Blend(baseColor.B, overlay.B));
    }
}
