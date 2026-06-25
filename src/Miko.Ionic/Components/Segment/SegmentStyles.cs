using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the Segment components (<c>ion-segment</c>, <c>ion-segment-button</c>,
/// <c>ion-segment-view</c>, <c>ion-segment-content</c>). Ported from Ionic's Material Design
/// source: <c>segment.scss</c>, <c>segment-button.md.scss</c>.
/// <para>
/// The selected indicator is the MD 7+ solid-pill style: the checked button fills with the
/// primary color and its text flips to white, matching the current Ionic Material theme.
/// </para>
/// </summary>
internal static class SegmentStyles
{
    internal static CssObject GenStyle(IonicTheme t)
    {
        return new CssObject
        {
            // ion-segment — the selector row. Flex row, centered, full width, translucent
            // background track with rounded corners (4px radius matches Ionic MD).
            [".ion-segment"] = new()
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
            [".ion-segment.segment-disabled"] = new()
            {
                Opacity = 0.4f,
                PointerEvents = PointerEvents.None,
            },

            // ion-segment-button — each button inside the segment. Flex child that grows/shrinks
            // equally; transparent background by default; uppercase MD typography; no default
            // button border/outline; pointer cursor.
            [".ion-segment-button"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                MaxWidth = Length.Px(t.SegmentButtonMaxWidth),
                MinHeight = Length.Px(t.SegmentButtonMinHeight),
                PaddingTop = Length.Px(t.SegmentButtonPaddingY),
                PaddingBottom = Length.Px(t.SegmentButtonPaddingY),
                PaddingLeft = Length.Px(t.SegmentButtonPaddingX),
                PaddingRight = Length.Px(t.SegmentButtonPaddingX),
                BackgroundColor = Color.Transparent,
                Color = t.SegmentButtonColor,
                FontSize = Length.Px(t.SegmentButtonFontSize),
                FontWeight = FontWeight.Medium,
                TextTransform = TextTransform.Uppercase,
                TextDecoration = TextDecoration.None,
                BorderWidth = Length.Px(0),
                BorderRadius = new BorderRadius(Length.Px(4), Length.Px(4), Length.Px(4), Length.Px(4)),
                Cursor = Cursor.Pointer,
                Transitions = new List<Transition>
                {
                    new Transition(nameof(Style.BackgroundColor), 0.15f, TimingFunction.Ease),
                },
            },

            // segment-button-checked — MD 7+ solid-pill indicator. The whole checked button
            // fills with the primary color (background) and its text/label flips to white.
            [".ion-segment-button.segment-button-checked"] = new()
            {
                BackgroundColor = t.SegmentButtonCheckedBackground,
                Color = t.SegmentButtonCheckedColor,
            },

            // segment-button-disabled — individual button disabled state.
            [".ion-segment-button.segment-button-disabled"] = new()
            {
                Opacity = 0.4f,
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
            [".ion-segment-view"] = new()
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
            [".ion-segment-content"] = new()
            {
                Display = Display.Block,
                FlexShrink = 0,
                Width = Length.Percent(100),
                MinHeight = Length.Px(1),
            },

            // segment-content-hidden — the inactive content is display:none.
            [".ion-segment-content.segment-content-hidden"] = new()
            {
                Display = Display.None,
            },
        };
    }
}
