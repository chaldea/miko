using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-content</c>. Ported from the Ionic source: <c>content.scss</c>.
/// <para>
/// The host is a block region that fills the remaining page height below the header. Inside it,
/// <c>#background-content</c> paints the background as an absolutely-positioned layer, and
/// <c>.inner-scroll</c> is the scrollable container holding the default slot. <c>scroll-x</c> /
/// <c>scroll-y</c> marker classes gate the two scroll axes (Ionic's <c>scrollX</c> / <c>scrollY</c>
/// props), mirroring content.scss's <c>.scroll-y { overflow-y: var(--overflow) }</c>.
/// </para>
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/>
/// for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class ContentStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // :host — a block region filling the remaining page height below the header.
            // content.scss's :host is `display:block; position:relative; flex:1; width/height:100%`
            // with margin/padding forced to 0. The flex triple keeps the content from resolving a
            // percentage height against a zero basis when it sits in the ion-page flex column.
            [$".ion-content.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Width = Length.Percent(100),
                BackgroundColor = t.ContentBackground,
                Color = t.ContentColor,
                // content.scss: margin/padding are !important-zeroed on the host — the padding
                // belongs to .inner-scroll (--padding-*), never to the host itself.
                MarginTop = Length.Px(0),
                MarginRight = Length.Px(0),
                MarginBottom = Length.Px(0),
                MarginLeft = Length.Px(0),
                PaddingTop = Length.Px(0),
                PaddingRight = Length.Px(0),
                PaddingBottom = Length.Px(0),
                PaddingLeft = Length.Px(0),
                // NOTE: no overflow on the host. content.scss's :host declares none — all clipping
                // and scrolling belongs to .inner-scroll (`overflow: hidden`, plus the scroll-y/x
                // rules below). Giving the host `overflow-y: auto` made it a clipping box, which
                // cut off anything the fixed slot placed outside the content area: an edge IonFab
                // is meant to hang half over the header, and the half above y=0 was clipped away
                // (issues/ion-fab.md problem 3). Fixed-slot content is a sibling of .inner-scroll
                // precisely so it escapes the scroller's clip.
            },

            // #background-content — the background layer (content.scss `#background-content`):
            // absolutely positioned over the host, inset by the (negated) offsets. Ionic paints
            // --background here rather than on the host so a translucent header can show through.
            [$".ion-content.{mode} .background-content"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                BackgroundColor = t.ContentBackground,
            },

            // .inner-scroll — the scroll container holding the default slot. content.scss makes it
            // `position: absolute` with inset 0 (via the --offset-* vars, which are 0 unless
            // fullscreen measuring kicks in) and border-box sizing. Keeping it absolute matters
            // beyond sizing: it takes the scroll container out of flow, so inset-less fixed-slot
            // content keeps its static position at the top of the content area instead of being
            // pushed below the scroll container.
            [$".ion-content.{mode} .inner-scroll"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Color = t.ContentColor,
                BoxSizing = BoxSizing.BorderBox,
                // content.scss: `.inner-scroll { overflow: hidden }` — neither axis scrolls until
                // the corresponding marker class is present.
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // .scroll-y — `overflow-y: var(--overflow)` (auto). Stamped when scrollY is true
            // (Ionic's default), so the common case scrolls vertically only.
            [$".ion-content.{mode} .inner-scroll.scroll-y"] = new()
            {
                OverflowY = Overflow.Auto,
            },

            // .scroll-x — `overflow-x: var(--overflow)`. Stamped when scrollX is true (off by
            // default in Ionic).
            [$".ion-content.{mode} .inner-scroll.scroll-x"] = new()
            {
                OverflowX = Overflow.Auto,
            },

            // Fixed slot content (content.scss `::slotted([slot="fixed"])`): taken out of the
            // scroll flow so it stays put while the content scrolls. The marker class is stamped
            // by IonContent.Build() onto the fixed elements THEMSELVES (Ionic's ::slotted targets
            // the projected elements, not a wrapper).
            //
            // Only `position: absolute`, exactly like Ionic (which adds just a translateZ
            // compositing hint on top). No insets: the slotted element picks its own — an IonFab
            // sets bottom/right and shrink-wraps, while inset-less content falls back to its static
            // position (the top of the content area, since .inner-scroll is absolute and so does
            // not advance the flow cursor). Setting top/left here would instead hand a
            // bottom/right-positioned element all four insets and stretch it across the content.
            [$".ion-content.{mode} .ion-slot-fixed"] = new()
            {
                Position = Position.Absolute,
            },

            // Popover sizing (content.scss `:host(.content-sizing)`): a flex column that may
            // shrink below its content height so long content scrolls inside the popover.
            [$".ion-content.{mode}.content-sizing"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                MinHeight = Length.Px(0),
            },
            [$".ion-content.{mode}.content-sizing .inner-scroll"] = new()
            {
                Position = Position.Relative,
                Top = Length.Px(0),
                Bottom = Length.Px(0),
            },

            // .transition-effect — the iOS page-push shadow layers (content.scss). Rendered only in
            // ios mode and `display:none` at rest: Ionic's page transition animates its opacity,
            // which this port's navigation transitions do not drive, so it stays hidden. Ported for
            // DOM/style parity with content.tsx's transitionShadow branch.
            [$".ion-content.{mode} .transition-effect"] = new()
            {
                Display = Display.None,
                Position = Position.Absolute,
                Width = Length.Percent(100),
                Opacity = 0f,
                PointerEvents = PointerEvents.None,
            },
            // :host(.content-ltr) .transition-effect { left: -100% }
            [$".ion-content.{mode}.content-ltr .transition-effect"] = new()
            {
                Left = Length.Percent(-100),
            },
            [$".ion-content.{mode} .transition-cover"] = new()
            {
                Position = Position.Absolute,
                Right = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BackgroundColor = Color.Black,
                Opacity = 0.1f,
            },
            [$".ion-content.{mode} .transition-shadow"] = new()
            {
                Display = Display.Block,
                Position = Position.Absolute,
                Right = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },
        };

        // Named palette colors (content.scss `:host(.ion-color) .inner-scroll`): the fill and text
        // color both move onto .inner-scroll. The background layer is tinted too so the color
        // covers the whole region (Ionic's --background feeds #background-content).
        AddColor(css, mode, "primary", t.Primary, Color.FromHex("ffffff"));
        AddColor(css, mode, "secondary", t.Secondary, Color.FromHex("ffffff"));
        AddColor(css, mode, "tertiary", t.Tertiary, Color.FromHex("ffffff"));
        AddColor(css, mode, "success", t.Success, Color.FromHex("000000"));
        AddColor(css, mode, "warning", t.Warning, Color.FromHex("000000"));
        AddColor(css, mode, "danger", t.Danger, Color.FromHex("ffffff"));
        AddColor(css, mode, "light", t.Light, Color.FromHex("000000"));
        AddColor(css, mode, "medium", t.Medium, Color.FromHex("ffffff"));
        AddColor(css, mode, "dark", t.Dark, Color.FromHex("ffffff"));

        return css;
    }

    /// <summary>
    /// One named-color variant: <c>:host(.ion-color) .inner-scroll { background: base; color:
    /// contrast }</c>. The background layer takes the same fill so the tint covers the region even
    /// where the scroll container does not paint.
    /// </summary>
    private static void AddColor(CssObject css, string mode, string name, Color baseColor, Color contrast)
    {
        css[$".ion-content.{mode}.ion-color-{name} .inner-scroll"] = new()
        {
            BackgroundColor = baseColor,
            Color = contrast,
        };
        css[$".ion-content.{mode}.ion-color-{name} .background-content"] = new()
        {
            BackgroundColor = baseColor,
        };
    }
}
