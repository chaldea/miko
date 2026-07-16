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
            // Host — a fixed full-screen overlay above the page.
            [$".ion-action-sheet.{mode}"] = new()
            {
                Position = Position.Fixed,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                ZIndex = 1000,
            },

            // Closed sheet is fully hidden.
            [$".ion-action-sheet.{mode}.overlay-hidden"] = new()
            {
                Display = Display.None,
            },

            // Backdrop — the tappable dim layer filling the host.
            [$".ion-action-sheet.{mode} .action-sheet-backdrop"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                BackgroundColor = t.ActionSheetBackdropColor,
                Opacity = t.ActionSheetBackdropOpacity,
                Cursor = Cursor.Pointer,
            },

            // Wrapper — bottom-anchored, centered horizontally, capped width.
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
