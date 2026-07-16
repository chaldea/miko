using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-radio</c>. Ported from Ionic's source: <c>radio.scss</c> / <c>radio.md.scss</c>
/// / <c>radio.ios.scss</c> and their <c>*.vars.scss</c>.
/// <para>
/// The visible control is <c>.radio-icon</c> inside <c>.native-wrapper</c>, with a nested
/// <c>.radio-inner</c>:
/// <list type="bullet">
///   <item><b>md</b> — <c>.radio-icon</c> is a bordered ring (circle) and <c>.radio-inner</c> a
///   filled dot revealed (scaled 0→1 via opacity here, since Miko has no <c>transform: scale</c>
///   reveal) on the <c>radio-checked</c> state.</item>
///   <item><b>ios</b> — <c>.radio-icon</c> has no ring; <c>.radio-inner</c> shows a checkmark glyph
///   (a template-tinted background image supplied by the component) faded in from opacity 0→1 on
///   the <c>radio-checked</c> state, exactly like <see cref="CheckboxStyles"/>' mark.</item>
/// </list>
/// The hidden native <c>&lt;input&gt;</c> is kept for form parity (<c>display:none</c>). The label
/// sits in <c>.label-text-wrapper</c>; <c>labelPlacement</c> flips the wrapper's flex direction and
/// margins. Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>).
/// </para>
/// <para>
/// Hardcoded from vars (kept off the shared <c>IonicTheme</c>): md icon 20px / ios icon 15×24px
/// (<c>$radio-md-icon-width|height</c>, <c>$radio-ios-icon-width|height</c>), md label margin 16px
/// (<c>$form-control-label-margin</c>), md disabled label 0.38 / icon 0.63, ios disabled host 0.3.
/// </para>
/// </summary>
internal static class RadioStyles
{
    // $radio-md-icon-width/height = 20px; $radio-ios-icon-width = 15px, height = 24px.
    private const float MdIconSize = 20f;
    private const float IosIconWidth = 15f;
    private const float IosIconHeight = 24f;
    // $radio-md-icon-border-width = 2px.
    private const float MdBorderWidth = 2f;
    // $form-control-label-margin = 16px.
    private const float LabelMargin = 16f;

    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-radio — the host. Ionic's :host: inline-block, relative, pointer cursor,
            // no text selection.
            [$".ion-radio.{mode}"] = new()
            {
                Display = Display.InlineBlock,
                Position = Position.Relative,
                Cursor = Cursor.Pointer,
                UserSelect = UserSelect.None,
                Color = t.ItemColor,
                FontSize = Length.Px(t.SelectFontSize),
                BoxSizing = BoxSizing.BorderBox,
            },

            // .radio-wrapper — the flex click surface. Grows to fill the host, centers the box and
            // label on the cross axis, and pushes them apart (space-between) by default.
            [$".ion-radio.{mode} .radio-wrapper"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Cursor = Cursor.Pointer,
            },

            // The hidden native input (kept for form semantics). Ionic renders it visually hidden.
            [$".ion-radio.{mode} .radio-native"] = new()
            {
                Display = Display.None,
            },

            // .label-text-wrapper — the slotted label. One nowrap line, clipped.
            [$".ion-radio.{mode} .label-text-wrapper"] = new()
            {
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },
            [$".ion-radio.{mode} .label-text-wrapper-hidden"] = new()
            {
                Display = Display.None,
            },

            // .native-wrapper — wraps the visual box; centers it on the cross axis.
            [$".ion-radio.{mode} .native-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
            },

            // .radio-icon — the visual box. Centers the inner mark.
            [$".ion-radio.{mode} .radio-icon"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                BoxSizing = BoxSizing.BorderBox,
            },

            // .radio-inner — the mark. Sizing / reveal differ per mode below.
            [$".ion-radio.{mode} .radio-inner"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
                PointerEvents = PointerEvents.None,
            },

            // Justify / alignment — setting either switches the host to block (Ionic's rule).
            [$".ion-radio.{mode}.radio-justify-space-between .radio-wrapper"] = new()
            {
                JustifyContent = JustifyContent.SpaceBetween,
            },
            [$".ion-radio.{mode}.radio-justify-start .radio-wrapper"] = new()
            {
                JustifyContent = JustifyContent.FlexStart,
            },
            [$".ion-radio.{mode}.radio-justify-end .radio-wrapper"] = new()
            {
                JustifyContent = JustifyContent.FlexEnd,
            },
            [$".ion-radio.{mode}.radio-alignment-start .radio-wrapper"] = new()
            {
                AlignItems = AlignItems.FlexStart,
            },
            [$".ion-radio.{mode}.radio-alignment-center .radio-wrapper"] = new()
            {
                AlignItems = AlignItems.Center,
            },
            [$".ion-radio.{mode}.radio-justify-space-between"] = new() { Display = Display.Block },
            [$".ion-radio.{mode}.radio-justify-start"] = new() { Display = Display.Block },
            [$".ion-radio.{mode}.radio-justify-end"] = new() { Display = Display.Block },
            [$".ion-radio.{mode}.radio-alignment-start"] = new() { Display = Display.Block },
            [$".ion-radio.{mode}.radio-alignment-center"] = new() { Display = Display.Block },

            // Label placement — start (default): label left, radio right, margin on the label end.
            [$".ion-radio.{mode}.radio-label-placement-start .radio-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-radio.{mode}.radio-label-placement-start .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(LabelMargin),
                MarginLeft = Length.Px(0),
            },

            // Label placement — end: radio left, label right (row-reverse), packed to the start.
            [$".ion-radio.{mode}.radio-label-placement-end .radio-wrapper"] = new()
            {
                FlexDirection = FlexDirection.RowReverse,
                JustifyContent = JustifyContent.FlexStart,
            },
            [$".ion-radio.{mode}.radio-label-placement-end .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(0),
                MarginLeft = Length.Px(LabelMargin),
            },

            // Label placement — fixed: like start but the label has a fixed 100px width.
            [$".ion-radio.{mode}.radio-label-placement-fixed .radio-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-radio.{mode}.radio-label-placement-fixed .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(LabelMargin),
                MarginLeft = Length.Px(0),
                FlexGrow = 0,
                FlexShrink = 0,
                FlexBasis = Length.Px(100),
                Width = Length.Px(100),
                MinWidth = Length.Px(100),
            },

            // Label placement — stacked: label above the radio (column), bottom margin.
            [$".ion-radio.{mode}.radio-label-placement-stacked .radio-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Column,
                TextAlign = TextAlign.Center,
            },
            [$".ion-radio.{mode}.radio-label-placement-stacked .label-text-wrapper"] = new()
            {
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(LabelMargin),
                MaxWidth = Length.Percent(100),
            },
        };

        if (mode == "md")
        {
            GenMd(css, t);
        }
        else
        {
            GenIos(css, t);
        }

        return css;
    }

    // Material Design: outer ring + inner filled circle.
    private static void GenMd(CssObject css, IonicTheme t)
    {
        var circle = new BorderRadius(Length.Percent(50));

        // Outer ring: fixed size, off-color border, circular.
        css[".ion-radio.md .radio-icon"] = new()
        {
            Position = Position.Relative,
            Display = Display.Flex,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            BoxSizing = BoxSizing.BorderBox,
            Width = Length.Px(MdIconSize),
            Height = Length.Px(MdIconSize),
            BorderWidth = Length.Px(MdBorderWidth),
            BorderStyle = BorderStyle.Solid,
            BorderColor = t.CheckboxBorderColorOff, // --color (rgba text .60)
            BorderRadius = circle,
        };

        // Inner dot: half+border size, circular, filled with the checked color, hidden until checked.
        // Ionic scales it 0→1 via transform; Miko reveals it with opacity (no transform-scale reveal).
        css[".ion-radio.md .radio-inner"] = new()
        {
            BoxSizing = BoxSizing.BorderBox,
            PointerEvents = PointerEvents.None,
            Width = Length.Px(MdIconSize / 2f + MdBorderWidth),
            Height = Length.Px(MdIconSize / 2f + MdBorderWidth),
            BorderRadius = circle,
            BackgroundColor = t.CheckboxBackgroundChecked, // --color-checked (= primary)
            Opacity = 0f,
        };

        // Checked: ring turns to the checked color and the dot is revealed.
        css[".ion-radio.md.radio-checked .radio-icon"] = new()
        {
            BorderColor = t.CheckboxBackgroundChecked,
        };
        css[".ion-radio.md.radio-checked .radio-inner"] = new()
        {
            Opacity = 1f,
        };

        // Disabled: label matches other form controls (0.38); the icon uses 0.63 (see md vars note).
        css[".ion-radio.md.radio-disabled"] = new()
        {
            PointerEvents = PointerEvents.None,
        };
        css[".ion-radio.md.radio-disabled .label-text-wrapper"] = new()
        {
            Opacity = 0.38f, // $radio-md-disabled-opacity
        };
        css[".ion-radio.md.radio-disabled .native-wrapper"] = new()
        {
            Opacity = 0.63f, // $radio-md-icon-disabled-opacity
        };
    }

    // iOS: no ring; a checkmark glyph revealed on check (template background image, tinted).
    private static void GenIos(CssObject css, IonicTheme t)
    {
        // Box has no border; sized to the ios icon dimensions.
        css[".ion-radio.ios .radio-icon"] = new()
        {
            Position = Position.Relative,
            Display = Display.Flex,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            BoxSizing = BoxSizing.BorderBox,
            Width = Length.Px(IosIconWidth),
            Height = Length.Px(IosIconHeight),
        };

        // Inner checkmark: fills the box, tinted by the checked color, hidden until checked.
        css[".ion-radio.ios .radio-inner"] = new()
        {
            BoxSizing = BoxSizing.BorderBox,
            PointerEvents = PointerEvents.None,
            Width = Length.Percent(100),
            Height = Length.Percent(100),
            Color = t.CheckboxBackgroundChecked, // --color-checked tints the template glyph
            Opacity = 0f,
        };

        css[".ion-radio.ios.radio-checked .radio-inner"] = new()
        {
            Opacity = 1f,
        };

        // Disabled: ios dims the whole host.
        css[".ion-radio.ios.radio-disabled"] = new()
        {
            Opacity = 0.3f, // $radio-ios-disabled-opacity (form-control-ios-disabled-opacity)
            PointerEvents = PointerEvents.None,
        };
    }
}
