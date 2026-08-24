using Miko.Animation;
using Miko.Common;
using Miko.Ionic.Styles;
using Miko.Styling;
using static Miko.Ionic.Styles.IonicMixins;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the Segment components (<c>ion-segment</c>, <c>ion-segment-button</c>,
/// <c>ion-segment-view</c>, <c>ion-segment-content</c>). Ported from Ionic's source:
/// <c>segment.scss</c>, <c>segment-button.scss</c> / <c>segment-button.md.scss</c> /
/// <c>segment-button.ios.scss</c>.
/// <para>
/// The checked button is shown by an <b>indicator</b> overlay (Ionic's
/// <c>.segment-button-indicator</c>), not by filling the whole button:
/// <list type="bullet">
///   <item><b>md</b>: a 2px underline bar pinned to the button's bottom in the primary color;
///   the checked label turns primary, the button background stays transparent.</item>
///   <item><b>ios</b>: a full-height light rounded pill (with a soft shadow) behind the label;
///   the label color stays the default dark text.</item>
/// </list>
/// The indicator is an absolutely-positioned child kept at opacity 0 and faded to 1 when the
/// button is checked — matching <c>:host(.segment-button-checked) .segment-button-indicator</c>.
/// </para>
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see
/// <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class SegmentStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // Miko does not yet model CSS minmax() grid tracks, so flex-grow emulates Ionic's
            // equal grid-auto-columns while preserving the mode-specific host appearance.
            [$".ion-segment.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Stretch,
                Width = Length.Percent(100),
                BackgroundColor = t.SegmentBackground,
                BorderRadius = UniformRadius(t.SegmentBorderRadius),
                TextAlign = TextAlign.Center,
                PaddingTop = Length.Px(0),
                PaddingBottom = Length.Px(0),
                PaddingLeft = Length.Px(0),
                PaddingRight = Length.Px(0),
            },

            // segment-disabled — reduced opacity, pointer-events none.
            [$".ion-segment.{mode}.segment-disabled"] = new()
            {
                Opacity = 0.4f,
                PointerEvents = PointerEvents.None,
            },

            // segment.scss :host(.segment-scrollable). Keep the segment viewport at its
            // containing width while allowing the no-shrink buttons to overflow horizontally.
            [$".ion-segment.{mode}.segment-scrollable"] = new()
            {
                Width = Length.Auto,
                JustifyContent = JustifyContent.FlexStart,
                OverflowX = Overflow.Auto,
                ScrollbarWidth = ScrollbarWidth.None,
            },
            [$".ion-segment.{mode}.segment-scrollable > .ion-segment-button"] = new()
            {
                FlexGrow = 0,
                FlexShrink = 0,
                FlexBasis = Length.Auto,
                MinWidth = mode == "md" ? Length.Auto : Length.Px(t.SegmentButtonMinWidth),
            },
            // ion-segment-button — the host (a <div> wrapper). It is the flex item inside the
            // segment row and the positioned containing block for the
            // indicator (position:relative). All host classes (checked/label-only/etc.) live here.
            // Per Ionic's :host: transparent background, no border; the checked state is the
            // indicator overlay (a sibling of .button-native), NOT a fill.
            [$".ion-segment-button.{mode}"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Display = Display.Flex,
                FlexDirection = mode == "ios" ? FlexDirection.Row : FlexDirection.Column,
                Position = Position.Relative,
                MinWidth = Length.Px(t.SegmentButtonMinWidth),
                MinHeight = Length.Px(t.SegmentButtonMinHeight),
                MarginTop = Length.Px(t.SegmentButtonMarginY),
                MarginBottom = Length.Px(t.SegmentButtonMarginY),
                BackgroundColor = Color.Transparent,
                BorderRadius = mode == "ios" ? UniformRadius(t.SegmentIndicatorBorderRadius) : UniformRadius(0),
                Color = t.SegmentButtonColor,
                FontSize = Length.Px(t.SegmentButtonFontSize),
                FontWeight = FontWeight.Medium,
                LineHeight = Length.Px(t.SegmentButtonLineHeight),
                LetterSpacing = t.SegmentButtonLetterSpacing,
                TextTransform = mode == "md" ? TextTransform.Uppercase : TextTransform.None,
                WhiteSpace = WhiteSpace.Nowrap,
                TextDecoration = TextDecoration.None,
                BorderWidth = Length.Px(0),
                Cursor = Cursor.Pointer,
                Transitions = new List<Transition>
                {
                    new Transition(nameof(Style.Color), 0.15f, TimingFunction.Linear),
                },
            },

            // .button-native — the clickable <button> inside the host. Flex-grows to fill the host
            // and centers its content (the .button-inner → label). Inherits the host's padding
            // (Ionic's --padding-* map here) so the label clears the edges. pointer-events:none on
            // the NATIVE element is intentional in Ionic (the host handles activation); Miko keeps
            // it clickable by routing @onclick here, so we leave default pointer events.
            [$".ion-segment-button.{mode} .button-native"] = new()
            {
                // segment-button.scss applies text-inherit() here. This is required because the
                // normalized native <button> style explicitly resets text-transform to none.
                ["..."] = TextInherit(),
                Position = Position.Relative,
                Display = Display.Flex,
                FlexDirection = mode == "ios" ? FlexDirection.Row : FlexDirection.Column,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.SegmentButtonMinHeight),
                PaddingTop = Length.Px(t.SegmentButtonPaddingY),
                PaddingBottom = Length.Px(t.SegmentButtonPaddingY),
                PaddingLeft = Length.Px(t.SegmentButtonPaddingX),
                PaddingRight = Length.Px(t.SegmentButtonPaddingX),
                BackgroundColor = Color.Transparent,
                BorderWidth = Length.Px(0),
                Cursor = Cursor.Pointer,
                ZIndex = 1,
            },

            // .button-inner — centers the slotted label/icon (Ionic's .button-inner).
            [$".ion-segment-button.{mode} .button-native .button-inner"] = new()
            {
                Display = Display.Flex,
                FlexDirection = mode == "ios" ? FlexDirection.Row : FlexDirection.Column,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                ZIndex = 1,
            },

            // segment-button-checked — only the text color changes (md → primary, ios → stays
            // dark). The visual selection is the indicator overlay below.
            [$".ion-segment-button.{mode}.segment-button-checked"] = new()
            {
                Color = t.SegmentButtonCheckedColor,
            },

            // segment-button-disabled — individual button disabled state.
            [$".ion-segment-button.{mode}.segment-button-disabled"] = new()
            {
                Opacity = 0.3f,
                PointerEvents = PointerEvents.None,
            },
            // segment-button-indicator — the checked overlay, a SIBLING of .button-native (not
            // inside it). Always present (so it can fade), transparent until checked, and never
            // intercepts taps. position:absolute anchors it to the host.
            [$".ion-segment-button.{mode} .segment-button-indicator"] = IndicatorContainer(mode),

            // A segment color is propagated from IonSegment to its buttons (Ionic's
            // `in-segment-color` host-context marker). The palette-specific rules below provide
            // the actual base color and selected indicator surface.
            [$".ion-segment-button.{mode}.in-segment-color"] = new()
            {
                BackgroundColor = Color.Transparent,
                Color = t.SegmentButtonColor,
            },

            // segment-button-indicator-background — the painted surface inside the indicator.
            [$".ion-segment-button.{mode} .segment-button-indicator-background"] = IndicatorBackground(t),

            // Checked: fade the indicator in (matches :host(.segment-button-checked) .indicator).
            [$".ion-segment-button.{mode}.segment-button-checked .segment-button-indicator"] = new()
            {
                Opacity = 1f,
            },

            // ion-label inside a segment button — base segment-button.scss `::slotted(ion-label)`.
            // The label is a block, centered on the flex cross axis, clipped to one nowrap line
            // (22px), and non-interactive so taps fall through to the button. (text-overflow:
            // ellipsis is not modeled — the engine has no text-overflow property; overflow:hidden
            // still clips the overflowing text.) Applies in both modes (base rule).
            [$".ion-segment-button.{mode} .ion-label"] = new()
            {
                Display = Display.Block,
                AlignSelf = AlignSelf.Center,
                MaxWidth = Length.Percent(100),
                LineHeight = Length.Px(22),
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                BoxSizing = BoxSizing.BorderBox,
                PointerEvents = PointerEvents.None,
            },

            // ion-segment-view — the swappable content container. Ported faithfully from Ionic's
            // segment-view.scss (:host): a flex container that fills its parent's height with
            // horizontal scroll for the snap-paged content. The `height: 100%` resolves against
            // the enclosing ion-content's inner-scroll, which has a definite height (it grows to
            // fill the flex-column page below the header — see PageStyles).
            //
            // IMPORTANT: the content (below) must NOT also pin `height: 100%`. Ionic's
            // segment-content.scss uses `min-height: 1px` + `flex-shrink: 0`, NOT `height: 100%`.
            // Pinning both view and content to `height: 100%` creates a circular dependency that
            // collapses the subtree to 0 in Miko's block layout (the content's percentage height
            // resolves against the view's still-zero height).
            [$".ion-segment-view.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                OverflowX = Overflow.Hidden,
            },

            // ion-segment-content — one page of content inside the view. Ported from Ionic's
            // segment-content.scss (:host): full width, does not shrink, with a 1px min-height
            // floor. Height is auto so it sizes to its own children (no `height: 100%`).
            [$".ion-segment-content.{mode}"] = new()
            {
                Display = Display.Block,
                FlexShrink = 0,
                Width = Length.Percent(100),
                MinHeight = Length.Px(1),
            },

            // segment-content-hidden — the inactive content is display:none.
            [$".ion-segment-content.{mode}.segment-content-hidden"] = new()
            {
                Display = Display.None,
            },
        };

        if (mode == "ios")
        {
            var segment = css[$".ion-segment.{mode}"];
            segment.OverflowX = Overflow.Hidden;
            segment.OverflowY = Overflow.Hidden;

            // Ionic uses translate3d(0, 0, 0) here to establish a per-button stacking context.
            // Miko's positioned z-index:0 is the equivalent boundary and avoids lifting the
            // native content/indicator layers out of this button during segment scrolling.
            css[$".ion-segment-button.{mode}"].ZIndex = 0;

            css[$".ion-toolbar.{mode} .ion-segment.{mode}"] = new()
            {
                Width = Length.Auto,
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(0),
            };

            css[$".ion-segment.{mode}.in-toolbar"] = new()
            {
                Width = Length.Auto,
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(0),
            };
        }

        AddSegmentColors(css, mode, t);
        AddButtonContentStyles(css, mode);

        // Segment in a toolbar (md only). Ionic's segment.md.scss `:host(.in-toolbar)` makes the
        // segment inherit the toolbar's height via `min-height: var(--min-height)` (the
        // --min-height token is set on ion-toolbar). Modeled here as a descendant rule matching a
        // segment inside an ion-toolbar — mirroring `hostContext('ion-toolbar')`. iOS's
        // `.in-toolbar` rule is different (margin/width/background, no min-height) so this is
        // md-scoped only.
        if (mode == "md")
        {
            css[$".ion-toolbar.{mode} .ion-segment.{mode}"] = new()
            {
                MinHeight = Length.Px(t.ToolbarMinHeight),
            };

            css[$".ion-segment.{mode}.in-toolbar"] = new()
            {
                MinHeight = Length.Px(t.ToolbarMinHeight),
            };
        }

        return css;
    }

    private static BorderRadius UniformRadius(float value)
    {
        var radius = Length.Px(value);
        return new BorderRadius(radius, radius, radius, radius);
    }

    private static void AddSegmentColors(CssObject css, string mode, IonicTheme t)
    {
        AddSegmentColor(css, mode, "primary", t.Primary);
        AddSegmentColor(css, mode, "secondary", t.Secondary);
        AddSegmentColor(css, mode, "tertiary", t.Tertiary);
        AddSegmentColor(css, mode, "success", t.Success);
        AddSegmentColor(css, mode, "warning", t.Warning);
        AddSegmentColor(css, mode, "danger", t.Danger);
        AddSegmentColor(css, mode, "light", t.Light);
        AddSegmentColor(css, mode, "medium", t.Medium);
        AddSegmentColor(css, mode, "dark", t.Dark);
    }

    private static void AddSegmentColor(CssObject css, string mode, string name, Color color)
    {
        if (mode == "ios")
        {
            css[$".ion-segment.{mode}.ion-color-{name}"] = new()
            {
                BackgroundColor = new Color(color.R, color.G, color.B, 17),
            };

            // iOS keeps the segment-button text and indicator on their normal iOS colors when a
            // segment palette is supplied. Only the track receives the translucent palette fill.
            return;
        }

        // MD follows the palette for the button text and checked indicator.
        css[$".ion-segment-button.{mode}.in-segment-color.ion-color-{name}"] = new()
        {
            Color = color,
        };
        css[$".ion-segment-button.{mode}.in-segment-color.ion-color-{name} .segment-button-indicator-background"] = new()
        {
            BackgroundColor = color,
        };
    }

    private static void AddButtonContentStyles(CssObject css, string mode)
    {
        css[$".ion-segment-button.{mode} .ion-icon"] = new()
        {
            FlexShrink = 0,
            Order = -1,
            FontSize = Length.Px(24),
            PointerEvents = PointerEvents.None,
        };

        css[$".ion-segment-button.{mode}.segment-button-layout-icon-top .button-native"] = new()
        {
            FlexDirection = FlexDirection.Column,
        };
        css[$".ion-segment-button.{mode}.segment-button-layout-icon-top .button-inner"] = new()
        {
            FlexDirection = FlexDirection.Column,
        };
        css[$".ion-segment-button.{mode}.segment-button-layout-icon-start .button-native"] = new()
        {
            FlexDirection = FlexDirection.Row,
        };
        css[$".ion-segment-button.{mode}.segment-button-layout-icon-start .button-inner"] = new()
        {
            FlexDirection = FlexDirection.Row,
        };
        css[$".ion-segment-button.{mode}.segment-button-layout-icon-end .button-native"] = new()
        {
            FlexDirection = FlexDirection.RowReverse,
        };
        css[$".ion-segment-button.{mode}.segment-button-layout-icon-end .button-inner"] = new()
        {
            FlexDirection = FlexDirection.RowReverse,
        };
        css[$".ion-segment-button.{mode}.segment-button-layout-icon-bottom .button-native"] = new()
        {
            FlexDirection = FlexDirection.ColumnReverse,
        };
        css[$".ion-segment-button.{mode}.segment-button-layout-icon-bottom .button-inner"] = new()
        {
            FlexDirection = FlexDirection.ColumnReverse,
        };
        css[$".ion-segment-button.{mode}.segment-button-layout-icon-hide .ion-icon"] = new()
        {
            Display = Display.None,
        };
        css[$".ion-segment-button.{mode}.segment-button-layout-label-hide .ion-label"] = new()
        {
            Display = Display.None,
        };

        if (mode == "md")
        {
            css[$".ion-segment-button.{mode} .ion-icon"].MarginTop = Length.Px(12);
            css[$".ion-segment-button.{mode} .ion-icon"].MarginBottom = Length.Px(12);
            css[$".ion-segment-button.{mode} .ion-label"].MarginTop = Length.Px(12);
            css[$".ion-segment-button.{mode} .ion-label"].MarginBottom = Length.Px(12);

            // Keep the base 12px label/icon breathing room used by the existing segment
            // metrics. Layout changes the flex direction; it does not change those margins.
            css[$".ion-segment-button.{mode}.segment-button-layout-icon-start .ion-label"] = new()
            {
                MarginLeft = Length.Px(8),
                MarginRight = Length.Px(0),
            };
            css[$".ion-segment-button.{mode}.segment-button-layout-icon-end .ion-label"] = new()
            {
                MarginLeft = Length.Px(0),
                MarginRight = Length.Px(8),
            };
            return;
        }

        css[$".ion-segment-button.{mode}.segment-button-layout-icon-start .ion-label"] = new()
        {
            MarginLeft = Length.Px(2),
            MarginRight = Length.Px(0),
        };
        css[$".ion-segment-button.{mode}.segment-button-layout-icon-end .ion-label"] = new()
        {
            MarginLeft = Length.Px(0),
            MarginRight = Length.Px(2),
        };
    }

    // The positioned indicator container. md pins a thin bar to the bottom edge; ios covers the
    // full button height as a pill. Kept at opacity 0 until the button is checked.
    private static CssObject IndicatorContainer(string mode)
    {
        if (mode == "ios")
        {
            // Full-cover pill: anchored top-left, fills the button (height:100% resolves against
            // the button's definite min-height), inset 2px horizontally like Ionic's margin.
            return new CssObject
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Auto,
                // Stretch between top and bottom instead of resolving 100% against the
                // button's min-height. This covers icon + label layouts whose content is taller.
                Height = Length.Auto,
                PaddingLeft = Length.Px(2),
                PaddingRight = Length.Px(2),
                // Keep both positioned siblings in the same positive stacking pass. Miko's
                // negative-z pass is intentionally painted below normal-flow content and can
                // therefore disappear behind the host background; z=0 is the indicator layer,
                // z=1 is the native button/content layer.
                ZIndex = 0,
                TransformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50)),
                Opacity = 0f,
                PointerEvents = PointerEvents.None,
                Transitions = new List<Transition>
                {
                    new Transition(nameof(Style.Opacity), 0.26f, TimingFunction.EaseInOut),
                    new Transition(nameof(Style.Transform), 0.26f, TimingFunction.CubicBezier)
                    {
                        CubicBezier = new CubicBezierParams(0.4f, 0f, 0.2f, 1f),
                    },
                },
            };
        }

        // md: a 2px underline bar pinned to the bottom, spanning the button width.
        return new CssObject
        {
            Position = Position.Absolute,
            Left = Length.Px(0),
            Right = Length.Px(0),
            Bottom = Length.Px(0),
            Width = Length.Percent(100),
            // Keep the underline below the native button/content in the positive stacking pass.
            ZIndex = 0,
            TransformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50)),
            Opacity = 0f,
            PointerEvents = PointerEvents.None,
            Transitions = new List<Transition>
            {
                new Transition(nameof(Style.Opacity), 0.25f, TimingFunction.EaseInOut),
                new Transition(nameof(Style.Transform), 0.25f, TimingFunction.CubicBezier)
                {
                    CubicBezier = new CubicBezierParams(0.4f, 0f, 0.2f, 1f),
                },
            },
        };
    }

    // The painted surface inside the indicator: height/radius/shadow/color come from the per-mode
    // theme tokens (md → 2px square primary bar; ios → full-height white rounded pill w/ shadow).
    private static CssObject IndicatorBackground(IonicTheme t)
    {
        var bg = new CssObject
        {
            Width = Length.Percent(100),
            Height = t.SegmentIndicatorHeight,
            BackgroundColor = t.SegmentIndicatorColor,
            PointerEvents = PointerEvents.None,
        };

        if (t.SegmentIndicatorBorderRadius > 0)
        {
            var r = Length.Px(t.SegmentIndicatorBorderRadius);
            bg.BorderRadius = new BorderRadius(r, r, r, r);
        }

        if (t.SegmentIndicatorBoxShadow.Count > 0)
            bg.BoxShadow = t.SegmentIndicatorBoxShadow;

        return bg;
    }
}
