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

            // .label-text-wrapper — the slotted label. One nowrap line, clipped, with an
            // ellipsis when the text overflows (checkbox.scss: text-overflow: ellipsis;
            // white-space: nowrap; overflow: hidden).
            [$".ion-checkbox.{mode} .label-text-wrapper"] = new()
            {
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                TextOverflow = TextOverflow.Ellipsis,
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
            // checkbox.scss writes `justify-content: start` here — the ABSOLUTE keyword. Under
            // row-reverse, flex-start would mean the RIGHT edge and push the pair over; `start`
            // keeps it on the left (LTR). See JustifyContent.Start.
            [$".ion-checkbox.{mode}.checkbox-label-placement-end .checkbox-wrapper"] = new()
            {
                FlexDirection = FlexDirection.RowReverse,
                JustifyContent = JustifyContent.Start,
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

            // Justify / alignment. These are declared AFTER the label-placement rules on purpose:
            // checkbox.scss puts them last (lines 312/316 vs 229), and both selectors have the same
            // specificity, so source order is what lets an explicit `justify` override the
            // `justify-content` that label-placement-end sets for itself. Declaring them earlier
            // (as this port originally did) meant Justify was silently ignored whenever
            // LabelPlacement="end" was also set (ISSUE-116 problem 4).
            //
            // Keyword family matters too: Ionic writes `justify-content: start` / `end` — the
            // ABSOLUTE (writing-mode relative) keywords, not flex-start/flex-end. They do not flip
            // under row-reverse, so Justify="start" + LabelPlacement="end" keeps the pair on the
            // left while the label still follows the box. See JustifyContent.Start.
            [$".ion-checkbox.{mode}.checkbox-justify-space-between .checkbox-wrapper"] = new()
            {
                JustifyContent = JustifyContent.SpaceBetween,
            },
            [$".ion-checkbox.{mode}.checkbox-justify-start .checkbox-wrapper"] = new()
            {
                JustifyContent = JustifyContent.Start,
            },
            [$".ion-checkbox.{mode}.checkbox-justify-end .checkbox-wrapper"] = new()
            {
                JustifyContent = JustifyContent.End,
            },
            [$".ion-checkbox.{mode}.checkbox-alignment-start .checkbox-wrapper"] = new()
            {
                AlignItems = AlignItems.FlexStart,
            },
            [$".ion-checkbox.{mode}.checkbox-alignment-center .checkbox-wrapper"] = new()
            {
                AlignItems = AlignItems.Center,
            },

            // Setting either justify or alignment switches the host to block, so the wrapper has
            // free main-axis space to distribute (an inline-block host shrink-wraps its content).
            [$".ion-checkbox.{mode}.checkbox-justify-space-between"] = new() { Display = Display.Block },
            [$".ion-checkbox.{mode}.checkbox-justify-start"] = new() { Display = Display.Block },
            [$".ion-checkbox.{mode}.checkbox-justify-end"] = new() { Display = Display.Block },
            [$".ion-checkbox.{mode}.checkbox-alignment-start"] = new() { Display = Display.Block },
            [$".ion-checkbox.{mode}.checkbox-alignment-center"] = new() { Display = Display.Block },

            // In-item (checkbox.scss `:host(.in-item)`, mirroring `hostContext('ion-item')`): the
            // host stretches to fill the item's content area so justify / alignment have free space
            // to work with. The class is stamped by the component via CascadingParameter IonItemContext.
            // Slotted start/end checkboxes reset to content size (`:host([slot="start"]/["end"])`);
            // in Miko those are placed in .ion-slot-start / .ion-slot-end spans.
            [$".ion-checkbox.{mode}.in-item"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },
            [$".ion-slot-start .ion-checkbox.{mode}.in-item"] = new()
            {
                FlexGrow = 0,
                FlexBasis = Length.Auto,
                Width = Length.Auto,
            },
            [$".ion-slot-end .ion-checkbox.{mode}.in-item"] = new()
            {
                FlexGrow = 0,
                FlexBasis = Length.Auto,
                Width = Length.Auto,
            },

            // In-item vertical rhythm (`$checkbox-item-label-margin-top/bottom` = 10px): the label
            // and the box get 10px top/bottom margins; stacked swaps the label's bottom margin for
            // `$form-control-label-margin` (16px) and drops the box's top margin.
            [$".ion-checkbox.{mode}.in-item .label-text-wrapper"] = new()
            {
                MarginTop = Length.Px(10),
                MarginBottom = Length.Px(10),
            },
            [$".ion-checkbox.{mode}.in-item .native-wrapper"] = new()
            {
                MarginTop = Length.Px(10),
                MarginBottom = Length.Px(10),
            },
            [$".ion-checkbox.{mode}.in-item.checkbox-label-placement-stacked .label-text-wrapper"] = new()
            {
                MarginTop = Length.Px(10),
                MarginBottom = Length.Px(16),
            },
            [$".ion-checkbox.{mode}.in-item.checkbox-label-placement-stacked .native-wrapper"] = new()
            {
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(10),
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

        // Named palette colors (checkbox.scss `:host(.ion-color)`), which redefine three custom
        // properties: --checkbox-background-checked and --border-color-checked to current-color(base),
        // and --checkmark-color to current-color(contrast). Miko has no CSS custom properties here,
        // so the vars are resolved at authoring time into the rules that consume them — the checked/
        // indeterminate fill + border, and the checkmark tint. Without these the `color` attribute
        // stamped an ion-color-* class that no rule matched, making Color a no-op (ISSUE-116
        // problem 5).
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
    /// Emits one named-color variant: the checked/indeterminate box takes the palette base as its
    /// fill and border, and the checkmark takes the palette contrast. Mirrors the three vars Ionic
    /// overrides in <c>:host(.ion-color)</c>.
    /// <para>
    /// The selectors carry the state class (<c>.checkbox-checked</c> / <c>.checkbox-indeterminate</c>)
    /// so they out-specify the unchecked defaults AND the equally-shaped uncolored checked rules
    /// above — an <c>.ion-color-*</c> compound is one class more specific, so it wins regardless of
    /// source order.
    /// </para>
    /// </summary>
    private static void AddColor(CssObject css, string mode, string name, Color baseColor, Color contrast)
    {
        css[$".ion-checkbox.{mode}.ion-color-{name}.checkbox-checked .checkbox-icon"] = new()
        {
            BorderColor = baseColor,
            BackgroundColor = baseColor,
        };
        css[$".ion-checkbox.{mode}.ion-color-{name}.checkbox-indeterminate .checkbox-icon"] = new()
        {
            BorderColor = baseColor,
            BackgroundColor = baseColor,
        };
        // --checkmark-color: the glyph is a template background image tinted by the computed Color.
        css[$".ion-checkbox.{mode}.ion-color-{name} .checkbox-icon-mark"] = new()
        {
            Color = contrast,
        };
    }
}
