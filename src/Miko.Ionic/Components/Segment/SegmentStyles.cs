using Miko.Animation;
using Miko.Common;
using Miko.Styling;

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
            // ion-segment — the selector row. Flex row, centered, full width, translucent
            // background track with rounded corners (4px radius matches Ionic MD).
            [$".ion-segment.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Stretch,
                Width = Length.Percent(100),
                BackgroundColor = t.SegmentBackground,
                BorderRadius = new BorderRadius(Length.Px(4), Length.Px(4), Length.Px(4), Length.Px(4)),
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
            // ion-segment-button — the host (a <div> wrapper). It is the flex item inside the
            // segment row, a flex COLUMN itself, and the positioned containing block for the
            // indicator (position:relative). All host classes (checked/label-only/etc.) live here.
            // Per Ionic's :host: transparent background, no border; the checked state is the
            // indicator overlay (a sibling of .button-native), NOT a fill.
            [$".ion-segment-button.{mode}"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Position = Position.Relative,
                MaxWidth = Length.Px(t.SegmentButtonMaxWidth),
                MinHeight = Length.Px(t.SegmentButtonMinHeight),
                BackgroundColor = Color.Transparent,
                Color = t.SegmentButtonColor,
                FontSize = Length.Px(t.SegmentButtonFontSize),
                FontWeight = FontWeight.Medium,
                TextTransform = TextTransform.Uppercase,
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
                Position = Position.Relative,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
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
                ZIndex = 2,
            },

            // .button-inner — centers the slotted label/icon (Ionic's .button-inner).
            [$".ion-segment-button.{mode} .button-native .button-inner"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
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
                Opacity = 0.4f,
                PointerEvents = PointerEvents.None,
            },
            // segment-button-indicator — the checked overlay, a SIBLING of .button-native (not
            // inside it). Always present (so it can fade), transparent until checked, and never
            // intercepts taps. position:absolute anchors it to the host.
            [$".ion-segment-button.{mode} .segment-button-indicator"] = IndicatorContainer(mode),

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

            // A label-only segment button gives its label 12px vertical breathing room
            // (segment-button.md.scss `:host(.segment-button-has-label-only) ::slotted(ion-label)`).
            // The marker is stamped on the host by IonSegmentButton.Build when it sees an
            // ion-label child with no ion-icon sibling. iOS has no equivalent rule.
            css[$".ion-segment-button.{mode}.segment-button-has-label-only .ion-label"] = new()
            {
                MarginTop = Length.Px(12),
                MarginBottom = Length.Px(12),
            };
        }

        return css;
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
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                PaddingLeft = Length.Px(2),
                PaddingRight = Length.Px(2),
                Opacity = 0f,
                PointerEvents = PointerEvents.None,
                Transitions = new List<Transition>
                {
                    new Transition(nameof(Style.Opacity), 0.26f, TimingFunction.EaseInOut),
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
            Opacity = 0f,
            PointerEvents = PointerEvents.None,
            Transitions = new List<Transition>
            {
                new Transition(nameof(Style.Opacity), 0.25f, TimingFunction.EaseInOut),
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
