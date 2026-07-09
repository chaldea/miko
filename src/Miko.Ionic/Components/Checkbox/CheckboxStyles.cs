using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-checkbox</c>. Ported from Ionic's source: <c>checkbox.scss</c> /
/// <c>checkbox.md.scss</c> / <c>checkbox.ios.scss</c> and their <c>*.vars.scss</c>.
/// <para>
/// The visible control is <c>.checkbox-icon</c> — a bordered box (md a small rounded square,
/// ios a circle) inside <c>.native-wrapper</c>. It fills with the checked color and fades the
/// checkmark (<c>.checkbox-icon-mark</c>, a template-tinted glyph) in from opacity 0→1 on the
/// <c>checkbox-checked</c> / <c>checkbox-indeterminate</c> host state. The hidden native
/// <c>&lt;input&gt;</c> is kept for form parity (<c>display:none</c>).
/// </para>
/// <para>
/// The label sits in <c>.label-text-wrapper</c>; <c>labelPlacement</c> flips the wrapper's flex
/// direction (start = row, end = row-reverse, stacked = column) and margins. Rules are scoped by
/// the active mode class (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/> for the rationale.
/// </para>
/// </summary>
internal static class CheckboxStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var r = t.CheckboxBorderRadius;
        var radius = new BorderRadius(r, r, r, r);

        var css = new CssObject
        {
            // ion-checkbox — the host. Ionic's :host: inline-block, relative, pointer cursor,
            // no text selection. The checked/disabled/placement classes are all stamped here.
            [$".ion-checkbox.{mode}"] = new()
            {
                Display = Display.InlineBlock,
                Position = Position.Relative,
                Cursor = Cursor.Pointer,
                UserSelect = UserSelect.None,
                Color = t.ItemColor,
                FontSize = Length.Px(t.SelectFontSize),
            },

            // .checkbox-wrapper — the flex click surface. Grows to fill the host, centers the box
            // and label on the cross axis, and pushes them apart (space-between) by default.
            [$".ion-checkbox.{mode} .checkbox-wrapper"] = new()
            {
                Display = Display.Flex,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Cursor = Cursor.Pointer,
            },

            // The hidden native input (kept for form semantics). Ionic renders it display:none.
            [$".ion-checkbox.{mode} .checkbox-native"] = new()
            {
                Display = Display.None,
            },

            // .label-text-wrapper — the slotted label. One nowrap line, clipped (Miko has no
            // text-overflow so overflow:hidden does the clipping).
            [$".ion-checkbox.{mode} .label-text-wrapper"] = new()
            {
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // Empty label — Ionic hides the wrapper entirely so it adds no margin.
            [$".ion-checkbox.{mode} .label-text-wrapper-hidden"] = new()
            {
                Display = Display.None,
            },

            // .native-wrapper — wraps the visual box; centers it on the cross axis.
            [$".ion-checkbox.{mode} .native-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
            },

            // .checkbox-icon — the visible bordered box. Fixed size, border + background from the
            // per-mode tokens, border-box sizing so the border sits inside the size.
            [$".ion-checkbox.{mode} .checkbox-icon"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(t.CheckboxSize),
                Height = Length.Px(t.CheckboxSize),
                BorderWidth = Length.Px(t.CheckboxBorderWidth),
                BorderStyle = BorderStyle.Solid,
                BorderColor = t.CheckboxBorderColorOff,
                BackgroundColor = t.CheckboxBackgroundOff,
                BorderRadius = radius,
                BoxSizing = BoxSizing.BorderBox,
            },

            // .checkbox-icon-mark — the checkmark glyph, tinted by Color (the template image tint
            // source). Hidden until checked/indeterminate. Sized to the inner box.
            [$".ion-checkbox.{mode} .checkbox-icon-mark"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                Color = t.CheckboxCheckmarkColor,
                Opacity = 0f,
                PointerEvents = PointerEvents.None,
            },

            // Checked / indeterminate — fill the box with the checked color and reveal the mark.
            [$".ion-checkbox.{mode}.checkbox-checked .checkbox-icon"] = new()
            {
                BorderColor = t.CheckboxBackgroundChecked,
                BackgroundColor = t.CheckboxBackgroundChecked,
            },
            [$".ion-checkbox.{mode}.checkbox-indeterminate .checkbox-icon"] = new()
            {
                BorderColor = t.CheckboxBackgroundChecked,
                BackgroundColor = t.CheckboxBackgroundChecked,
            },
            [$".ion-checkbox.{mode}.checkbox-checked .checkbox-icon-mark"] = new()
            {
                Opacity = 1f,
            },
            [$".ion-checkbox.{mode}.checkbox-indeterminate .checkbox-icon-mark"] = new()
            {
                Opacity = 1f,
            },

            // Checkbox bottom (helper / error text). Small text row below the label.
            [$".ion-checkbox.{mode} .checkbox-bottom"] = new()
            {
                Display = Display.Flex,
                JustifyContent = JustifyContent.SpaceBetween,
                PaddingTop = Length.Px(4),
                FontSize = Length.Px(12),
                WhiteSpace = WhiteSpace.Normal,
            },
            [$".ion-checkbox.{mode} .checkbox-bottom .helper-text"] = new()
            {
                Color = t.SelectHelperColor,
            },
            [$".ion-checkbox.{mode} .checkbox-bottom .error-text"] = new()
            {
                Color = t.SelectErrorColor,
            },

            // Justify / alignment — setting either switches the host to block (Ionic's rule).
            [$".ion-checkbox.{mode}.checkbox-justify-space-between .checkbox-wrapper"] = new()
            {
                JustifyContent = JustifyContent.SpaceBetween,
            },
            [$".ion-checkbox.{mode}.checkbox-justify-start .checkbox-wrapper"] = new()
            {
                JustifyContent = JustifyContent.FlexStart,
            },
            [$".ion-checkbox.{mode}.checkbox-justify-end .checkbox-wrapper"] = new()
            {
                JustifyContent = JustifyContent.FlexEnd,
            },
            [$".ion-checkbox.{mode}.checkbox-alignment-start .checkbox-wrapper"] = new()
            {
                AlignItems = AlignItems.FlexStart,
            },
            [$".ion-checkbox.{mode}.checkbox-alignment-center .checkbox-wrapper"] = new()
            {
                AlignItems = AlignItems.Center,
            },
            [$".ion-checkbox.{mode}.checkbox-justify-space-between"] = new() { Display = Display.Block },
            [$".ion-checkbox.{mode}.checkbox-justify-start"] = new() { Display = Display.Block },
            [$".ion-checkbox.{mode}.checkbox-justify-end"] = new() { Display = Display.Block },
            [$".ion-checkbox.{mode}.checkbox-alignment-start"] = new() { Display = Display.Block },
            [$".ion-checkbox.{mode}.checkbox-alignment-center"] = new() { Display = Display.Block },

            // Label placement — start (default): label left, box right, margin on the label end.
            [$".ion-checkbox.{mode}.checkbox-label-placement-start .checkbox-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-checkbox.{mode}.checkbox-label-placement-start .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(16),
                MarginLeft = Length.Px(0),
            },

            // Label placement — end: box left, label right (row-reverse), packed to the start.
            [$".ion-checkbox.{mode}.checkbox-label-placement-end .checkbox-wrapper"] = new()
            {
                FlexDirection = FlexDirection.RowReverse,
                JustifyContent = JustifyContent.FlexStart,
            },
            [$".ion-checkbox.{mode}.checkbox-label-placement-end .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(0),
                MarginLeft = Length.Px(16),
            },

            // Label placement — fixed: like start but the label has a fixed 100px width.
            [$".ion-checkbox.{mode}.checkbox-label-placement-fixed .checkbox-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-checkbox.{mode}.checkbox-label-placement-fixed .label-text-wrapper"] = new()
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

            // Label placement — stacked: label above the box (column), centered, bottom margin.
            [$".ion-checkbox.{mode}.checkbox-label-placement-stacked .checkbox-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Column,
                TextAlign = TextAlign.Center,
            },
            [$".ion-checkbox.{mode}.checkbox-label-placement-stacked .label-text-wrapper"] = new()
            {
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(16),
                MaxWidth = Length.Percent(100),
            },
        };

        // Disabled: ios dims the whole host; md dims only the label + box wrappers.
        if (mode == "ios")
        {
            css[$".ion-checkbox.{mode}.checkbox-disabled"] = new()
            {
                Opacity = t.CheckboxDisabledOpacity,
                PointerEvents = PointerEvents.None,
            };
        }
        else
        {
            css[$".ion-checkbox.{mode}.checkbox-disabled"] = new()
            {
                PointerEvents = PointerEvents.None,
            };
            css[$".ion-checkbox.{mode}.checkbox-disabled .label-text-wrapper"] = new()
            {
                Opacity = t.CheckboxDisabledOpacity,
            };
            css[$".ion-checkbox.{mode}.checkbox-disabled .native-wrapper"] = new()
            {
                Opacity = 0.63f,   // $checkbox-md-icon-disabled-opacity
            };
        }

        return css;
    }
}
