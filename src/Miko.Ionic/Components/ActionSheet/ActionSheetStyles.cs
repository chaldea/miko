using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-action-sheet</c>. Ported from the Ionic source: <c>action-sheet.scss</c> /
/// <c>.md.scss</c> / <c>.ios.scss</c> (+ their <c>*.vars.scss</c>).
/// <para>
/// A bottom-anchored overlay: a fixed full-screen host holding a tappable backdrop and a wrapper
/// whose container bottom-aligns the button group(s). md fills to the bottom edge with a flat white
/// group and left-aligned buttons; ios floats a rounded group with side margins, centered buttons,
/// hairline dividers, and a separate rounded cancel group. Rules are scoped by the active mode class
/// (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class ActionSheetStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // Host — a fixed full-screen overlay above the page. Always mounted (NOT display:none
            // when closed) so the enter/leave animations can run: Miko detects a transition by
            // diffing against the PREVIOUS frame's computed style, and a display:none element has
            // no layout box to diff against — so a sheet that only appears on open would jump
            // straight to its end state. Same always-mounted approach as IonMenu.
            [$".ion-action-sheet.{mode}"] = new()
            {
                Position = Position.Fixed,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                ZIndex = 1000,
            },

            // Closed & settled: fully hidden and transparent to input, so the page below stays
            // interactive. Ionic's :host(.overlay-hidden) is display:none; here the host keeps its
            // box (see above) and instead goes pointer-events:none — Miko's hit test walks layout
            // boxes and honors pointer-events, and the property inherits, so the whole subtree
            // (backdrop, wrapper, buttons) stops catching taps and scrolls too.
            [$".ion-action-sheet.{mode}.overlay-hidden"] = new()
            {
                PointerEvents = PointerEvents.None,
            },

            // Open, or animating in/out: interactive, so the backdrop catches the dim-area tap
            // (including during the leave animation, matching Ionic).
            [$".ion-action-sheet.{mode}.action-sheet-mounted"] = new()
            {
                PointerEvents = PointerEvents.Auto,
            },

            // Backdrop — the tappable dim layer filling the host. Fades between 0 and the mode's
            // backdrop opacity ({md|ios}.{enter|leave}.ts backdropAnimation fromTo('opacity', …)).
            // Transparent at rest so a closed sheet shows nothing while it stays mounted.
            [$".ion-action-sheet.{mode} .action-sheet-backdrop"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                BackgroundColor = t.ActionSheetBackdropColor,
                Opacity = 0f,
                Cursor = Cursor.Pointer,
                // NOTE on which duration goes where: Miko picks the transition list from the
                // PREVIOUS frame's computed style (MikoEngine captures it before re-layout), unlike
                // CSS which reads the after-change style. So the list on a rule governs the
                // transition OUT of that state: the closed (base) rule drives the ENTER animation,
                // and the .action-sheet-open rule drives the LEAVE. Ionic's easing is shared —
                // cubic-bezier(.36,.66,.04,1) — with enter 400ms / leave 450ms.
                Transitions = new List<Transition>
                {
                    Transition.For(x => x.Opacity)
                        .Duration(t.ActionSheetEnterDuration)
                        .CubicBezier(0.36f, 0.66f, 0.04f, 1f),
                },
            },

            // Faded in while open. Its transition list is the one in effect when the sheet leaves
            // the open state — i.e. the dismiss animation (see the note above).
            [$".ion-action-sheet.{mode}.action-sheet-open .action-sheet-backdrop"] = new()
            {
                Opacity = t.ActionSheetBackdropOpacity,
                Transitions = new List<Transition>
                {
                    Transition.For(x => x.Opacity)
                        .Duration(t.ActionSheetLeaveDuration)
                        .CubicBezier(0.36f, 0.66f, 0.04f, 1f),
                },
            },

            // Wrapper — bottom-anchored, centered horizontally, capped width. Parked fully below
            // the bottom edge (translateY(100%)) so it slides up on open and back down on close
            // ({md|ios}.{enter|leave}.ts wrapperAnimation fromTo('transform', …)).
            [$".ion-action-sheet.{mode} .action-sheet-wrapper"] = new()
            {
                Position = Position.Absolute,
                Left = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
                Width = Length.Percent(100),
                MaxWidth = Length.Px(t.ActionSheetMaxWidth),
                Transform = new Transform(new TransformFunction.TranslateY(Length.Percent(100))),
                // Drives the ENTER slide-up (see the duration note on the backdrop rule).
                Transitions = new List<Transition>
                {
                    Transition.For(x => x.Transform)
                        .Duration(t.ActionSheetEnterDuration)
                        .CubicBezier(0.36f, 0.66f, 0.04f, 1f),
                },
            },

            // Slid into place while open; its list drives the LEAVE slide-down.
            [$".ion-action-sheet.{mode}.action-sheet-open .action-sheet-wrapper"] = new()
            {
                // translateY(0%) — the sheet resting at the bottom edge. A zero length carries no
                // unit (0% == 0px), and the transform interpolator treats it as compatible with the
                // parked translateY(100%), so the two states slide into each other.
                Transform = new Transform(new TransformFunction.TranslateY(Length.Px(0))),
                Transitions = new List<Transition>
                {
                    Transition.For(x => x.Transform)
                        .Duration(t.ActionSheetLeaveDuration)
                        .CubicBezier(0.36f, 0.66f, 0.04f, 1f),
                },
            },

            // Container — a column that pushes its groups to the bottom, with the ios side padding.
            [$".ion-action-sheet.{mode} .action-sheet-container"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.FlexEnd,
                PaddingLeft = Length.Px(t.ActionSheetContainerPaddingX),
                PaddingRight = Length.Px(t.ActionSheetContainerPaddingX),
            },

            // Group — the button surface. Rounded + margined on ios; flat on md.
            [$".ion-action-sheet.{mode} .action-sheet-group"] = new()
            {
                BackgroundColor = t.ActionSheetBackground,
                BorderRadius = new BorderRadius(Length.Px(t.ActionSheetBorderRadius)),
                MarginTop = Length.Px(t.ActionSheetGroupMarginTop),
                MarginBottom = Length.Px(t.ActionSheetGroupMarginBottom),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // Cancel group — sits below the main group as its own (ios: rounded, separated) surface.
            [$".ion-action-sheet.{mode} .action-sheet-group-cancel"] = new()
            {
                FlexShrink = 0,
            },

            // Title — the header row above the buttons.
            [$".ion-action-sheet.{mode} .action-sheet-title"] = new()
            {
                PaddingTop = Length.Px(t.ActionSheetTitlePaddingY),
                PaddingBottom = Length.Px(t.ActionSheetTitlePaddingY),
                PaddingLeft = Length.Px(t.ActionSheetTitlePaddingX),
                PaddingRight = Length.Px(t.ActionSheetTitlePaddingX),
                Color = t.ActionSheetTitleColor,
                FontSize = Length.Px(t.ActionSheetTitleFontSize),
                TextAlign = t.ActionSheetTextAlign,
            },

            // Sub-title — the secondary header line.
            [$".ion-action-sheet.{mode} .action-sheet-sub-title"] = new()
            {
                PaddingTop = Length.Px(6),
                FontSize = Length.Px(t.ActionSheetSubTitleFontSize),
            },

            // Button — a full-width tappable row.
            [$".ion-action-sheet.{mode} .action-sheet-button"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.ActionSheetButtonHeight),
                PaddingTop = Length.Px(t.ActionSheetButtonPaddingY),
                PaddingBottom = Length.Px(t.ActionSheetButtonPaddingY),
                PaddingLeft = Length.Px(t.ActionSheetButtonPaddingX),
                PaddingRight = Length.Px(t.ActionSheetButtonPaddingX),
                BorderWidth = Length.Px(0),
                BackgroundColor = Color.Transparent,
                Color = t.ActionSheetButtonColor,
                FontSize = Length.Px(t.ActionSheetButtonFontSize),
                TextAlign = t.ActionSheetTextAlign,
                Cursor = Cursor.Pointer,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // Disabled button — dimmed and non-interactive.
            [$".ion-action-sheet.{mode} .action-sheet-button.action-sheet-button-disabled"] = new()
            {
                Opacity = 0.4f,
                PointerEvents = PointerEvents.None,
            },

            // Button inner — centers/justifies the icon + label row per mode.
            [$".ion-action-sheet.{mode} .action-sheet-button-inner"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                JustifyContent = t.ActionSheetButtonJustify,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },

            // Button icon.
            [$".ion-action-sheet.{mode} .action-sheet-icon"] = new()
            {
                MarginRight = Length.Px(mode == "ios" ? 8 : 32),
                FontSize = Length.Px(t.ActionSheetIconFontSize),
                Color = t.ActionSheetButtonColor,
            },

            // Selected button — a bold label.
            [$".ion-action-sheet.{mode} .action-sheet-selected"] = new()
            {
                FontWeight = FontWeight.Bold,
            },

            // Destructive button — the danger color.
            [$".ion-action-sheet.{mode} .action-sheet-destructive"] = new()
            {
                Color = t.ActionSheetDestructiveColor,
            },
        };

        // Cancel button — per-mode font weight (ios 600).
        css[$".ion-action-sheet.{mode} .action-sheet-cancel"] = new()
        {
            FontWeight = t.ActionSheetCancelFontWeight,
        };

        // iOS draws a hairline top divider between stacked buttons (md has none).
        if (mode == "ios")
        {
            css[$".ion-action-sheet.{mode} .action-sheet-group .action-sheet-button"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0.55f), BorderStyle.Solid, t.ActionSheetButtonBorderColor),
            };
        }

        return css;
    }
}
