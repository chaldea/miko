using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-toggle</c>. Ported from Ionic's source: <c>toggle.scss</c> /
/// <c>toggle.md.scss</c> / <c>toggle.ios.scss</c> and their <c>*.vars.scss</c>.
/// <para>
/// The visible control is a track (<c>.toggle-icon</c>) holding a sliding knob
/// (<c>.toggle-inner</c>). The knob is absolutely positioned at the start of a full-width
/// <c>.toggle-icon-wrapper</c>; on the <c>toggle-checked</c> host state the wrapper translates
/// right by <see cref="IonicTheme.ToggleHandleTravel"/> (= track-width − handle-width), sliding the
/// knob to the end, while the track fills with the checked color. A transition on the wrapper (and
/// the track background) animates the change. The hidden native <c>&lt;input&gt;</c> is kept for
/// form parity (<c>display:none</c>).
/// </para>
/// <para>
/// The label sits in <c>.label-text-wrapper</c>; <c>labelPlacement</c> flips the wrapper's flex
/// direction and margins (mirrors the checkbox). Rules are scoped by the active mode class
/// (<c>md</c> / <c>ios</c>).
/// </para>
/// </summary>
internal static class ToggleStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var trackRadius = new BorderRadius(t.ToggleBorderRadius);
        var r = t.ToggleHandleBorderRadius;
        var handleRadius = new BorderRadius(r, r, r, r);
        var slideTransition = new List<Transition>
        {
            new Transition(nameof(Style.Transform), t.ToggleTransitionDuration, TimingFunction.EaseInOut),
        };

        var css = new CssObject
        {
            // ion-toggle — the host. Ionic's :host: inline-block, relative, pointer cursor,
            // no text selection. The checked/disabled/placement classes are all stamped here.
            [$".ion-toggle.{mode}"] = new()
            {
                Display = Display.InlineBlock,
                Position = Position.Relative,
                MaxWidth = Length.Percent(100),
                Cursor = Cursor.Pointer,
                UserSelect = UserSelect.None,
                Color = t.ItemColor,
                FontSize = Length.Px(t.SelectFontSize),
            },

            // .toggle-wrapper — the flex click surface. Grows to fill the host, centers the switch
            // and label on the cross axis, and pushes them apart (space-between) by default.
            [$".ion-toggle.{mode} .toggle-wrapper"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Cursor = Cursor.Pointer,
            },

            // The hidden native input (kept for form semantics). Ionic renders it display:none.
            [$".ion-toggle.{mode} .toggle-native"] = new()
            {
                Display = Display.None,
            },

            // .label-text-wrapper — the slotted label. One nowrap line, clipped.
            [$".ion-toggle.{mode} .label-text-wrapper"] = new()
            {
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // Empty label — Ionic hides the wrapper entirely so it adds no margin.
            [$".ion-toggle.{mode} .label-text-wrapper-hidden"] = new()
            {
                Display = Display.None,
            },

            // .native-wrapper — wraps the visual switch; centers it on the cross axis.
            [$".ion-toggle.{mode} .native-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
            },

            // .toggle-icon — the background track. Fixed size from the per-mode tokens, off-state
            // background, full-pill radius, clips the overhanging knob shadow.
            [$".ion-toggle.{mode} .toggle-icon"] = new()
            {
                Position = Position.Relative,
                Display = Display.Block,
                Width = Length.Px(t.ToggleTrackWidth),
                Height = Length.Px(t.ToggleTrackHeight),
                BackgroundColor = t.ToggleTrackBackgroundOff,
                BorderRadius = trackRadius,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                Transitions = new List<Transition>
                {
                    new Transition(nameof(Style.BackgroundColor), t.ToggleTransitionDuration, TimingFunction.Linear),
                },
            },

            // .toggle-icon-wrapper — full-width carrier of the knob. Translating this on the checked
            // state slides the knob to the end; the transition animates the slide.
            [$".ion-toggle.{mode} .toggle-icon-wrapper"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                AlignItems = AlignItems.Center,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                Transitions = slideTransition,
            },

            // .toggle-inner — the knob. Absolutely positioned at the start (inset by handle-spacing),
            // fixed handle size, round, with the handle background + elevation shadow.
            [$".ion-toggle.{mode} .toggle-inner"] = new()
            {
                Position = Position.Absolute,
                Left = Length.Px(t.ToggleHandleSpacing),
                Width = Length.Px(t.ToggleHandleWidth),
                Height = Length.Px(t.ToggleHandleHeight),
                BackgroundColor = t.ToggleHandleBackground,
                BorderRadius = handleRadius,
                BoxShadow = t.ToggleHandleBoxShadow.Count > 0
                    ? (StyleProperty<List<BoxShadow>>?)t.ToggleHandleBoxShadow
                    : null,
            },

            // Checked — fill the track with the checked color and slide the knob to the end.
            [$".ion-toggle.{mode}.toggle-checked .toggle-icon"] = new()
            {
                BackgroundColor = t.ToggleTrackBackgroundOn,
            },
            [$".ion-toggle.{mode}.toggle-checked .toggle-icon-wrapper"] = new()
            {
                // Ionic: translate3d(calc(100% - var(--handle-width)), 0, 0). The wrapper is 100% of
                // the track width, so (track-width - handle-width) px slides the knob flush to the end.
                Transform = new Transform(new TransformFunction.TranslateX(Length.Px(t.ToggleHandleTravel))),
            },

            // Toggle bottom (helper / error text). Small text row below the label.
            [$".ion-toggle.{mode} .toggle-bottom"] = new()
            {
                Display = Display.Flex,
                JustifyContent = JustifyContent.SpaceBetween,
                PaddingTop = Length.Px(4),
                FontSize = Length.Px(12),
                WhiteSpace = WhiteSpace.Normal,
            },
            [$".ion-toggle.{mode} .toggle-bottom .helper-text"] = new()
            {
                Color = t.SelectHelperColor,
            },
            [$".ion-toggle.{mode} .toggle-bottom .error-text"] = new()
            {
                Color = t.SelectErrorColor,
            },

            // Justify / alignment — setting either switches the host to block (Ionic's rule).
            [$".ion-toggle.{mode}.toggle-justify-space-between .toggle-wrapper"] = new()
            {
                JustifyContent = JustifyContent.SpaceBetween,
            },
            [$".ion-toggle.{mode}.toggle-justify-start .toggle-wrapper"] = new()
            {
                JustifyContent = JustifyContent.FlexStart,
            },
            [$".ion-toggle.{mode}.toggle-justify-end .toggle-wrapper"] = new()
            {
                JustifyContent = JustifyContent.FlexEnd,
            },
            [$".ion-toggle.{mode}.toggle-alignment-start .toggle-wrapper"] = new()
            {
                AlignItems = AlignItems.FlexStart,
            },
            [$".ion-toggle.{mode}.toggle-alignment-center .toggle-wrapper"] = new()
            {
                AlignItems = AlignItems.Center,
            },
            [$".ion-toggle.{mode}.toggle-justify-space-between"] = new() { Display = Display.Block },
            [$".ion-toggle.{mode}.toggle-justify-start"] = new() { Display = Display.Block },
            [$".ion-toggle.{mode}.toggle-justify-end"] = new() { Display = Display.Block },
            [$".ion-toggle.{mode}.toggle-alignment-start"] = new() { Display = Display.Block },
            [$".ion-toggle.{mode}.toggle-alignment-center"] = new() { Display = Display.Block },

            // Label placement — start (default): label left, switch right, margin on the label end.
            [$".ion-toggle.{mode}.toggle-label-placement-start .toggle-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-toggle.{mode}.toggle-label-placement-start .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(16),
                MarginLeft = Length.Px(0),
            },

            // Label placement — end: switch left, label right (row-reverse), packed to the start.
            [$".ion-toggle.{mode}.toggle-label-placement-end .toggle-wrapper"] = new()
            {
                FlexDirection = FlexDirection.RowReverse,
                JustifyContent = JustifyContent.FlexStart,
            },
            [$".ion-toggle.{mode}.toggle-label-placement-end .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(0),
                MarginLeft = Length.Px(16),
            },

            // Label placement — fixed: like start but the label has a fixed 100px width.
            [$".ion-toggle.{mode}.toggle-label-placement-fixed .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(16),
                MarginLeft = Length.Px(0),
                FlexGrow = 0,
                FlexShrink = 0,
                FlexBasis = Length.Px(100),
                Width = Length.Px(100),
                MinWidth = Length.Px(100),
                MaxWidth = Length.Px(200),
            },

            // Label placement — stacked: label above the switch (column), centered, bottom margin.
            [$".ion-toggle.{mode}.toggle-label-placement-stacked .toggle-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Column,
                TextAlign = TextAlign.Center,
            },
            [$".ion-toggle.{mode}.toggle-label-placement-stacked .label-text-wrapper"] = new()
            {
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(16),
                MaxWidth = Length.Percent(100),
            },
        };

        // Disabled: both modes dim the whole host and block pointer events (Ionic's :host rule).
        css[$".ion-toggle.{mode}.toggle-disabled"] = new()
        {
            Opacity = t.ToggleDisabledOpacity,
            PointerEvents = PointerEvents.None,
        };

        return css;
    }
}
