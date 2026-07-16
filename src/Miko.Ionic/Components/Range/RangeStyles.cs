using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-range</c>. Ported from Ionic's source: <c>range.scss</c> / <c>range.md.scss</c>
/// / <c>range.ios.scss</c> and their <c>*.vars.scss</c>.
/// <para>
/// The host is a flex row (<c>.range-wrapper</c> → <c>.native-wrapper</c> → <c>.range-slider</c>).
/// The slider layers a <c>.range-bar-container</c> holding the inactive <c>.range-bar</c> (full
/// width) and the <c>.range-bar-active</c> (whose <c>right</c> offset is set inline to the value
/// ratio), plus a <c>.range-knob-handle</c> (absolutely positioned by an inline <c>left</c>
/// percentage, self-centered by a negative half-width margin) carrying the <c>.range-knob</c> and an
/// optional <c>.range-pin</c>. Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>).
/// </para>
/// <para>
/// Hardcoded from vars (kept off the shared <c>IonicTheme</c>): md bar-height 2px, knob 18px, slider
/// 42px, pin 28px; ios bar-height 4px, knob 26px, slider 42px, bar radius 2px, knob box-shadow. Bar
/// active / knob / pin use the primary color (theme token). The bar's inactive track uses a light
/// neutral (md a translucent primary, ios a grey step) — both hardcoded here. Disabled: md dims the
/// label 0.38; ios dims the whole host 0.3.
/// </para>
/// </summary>
internal static class RangeStyles
{
    // Shared handle size derives from the knob size (--knob-handle-size = knob-size * 2).
    private const float MdKnobSize = 18f;   // $range md --knob-size
    private const float IosKnobSize = 26f;  // $range-ios-knob-width
    private const float MdBarHeight = 2f;   // $range-md-bar-height
    private const float IosBarHeight = 4f;  // $range-ios-bar-height
    private const float SliderHeight = 42f; // $range-*-slider-height
    private const float MdPinDimension = 28f; // $range-md-pin-dimension
    private const float LabelMargin = 16f;  // $form-control-label-margin

    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var knobSize = mode == "md" ? MdKnobSize : IosKnobSize;
        var barHeight = mode == "md" ? MdBarHeight : IosBarHeight;
        var handleSize = knobSize * 2f;

        // $range-ios-bar-border-radius = 2px; md bar-border-radius = 0.
        var barRadius = mode == "md" ? new BorderRadius(Length.Px(0)) : new BorderRadius(Length.Px(2));

        var css = new CssObject
        {
            // ion-range — the host: flex row, relative, no text selection.
            [$".ion-range.{mode}"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
                UserSelect = UserSelect.None,
                Color = t.ItemColor,
            },
            [$".ion-range.{mode}.range-disabled"] = new()
            {
                PointerEvents = PointerEvents.None,
            },

            // .range-wrapper — the flex surface holding the label + native wrapper.
            [$".ion-range.{mode} .range-wrapper"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
            },

            // .label-text-wrapper — the label. Clipped nowrap line.
            [$".ion-range.{mode} .label-text-wrapper"] = new()
            {
                MaxWidth = Length.Px(200),
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },
            [$".ion-range.{mode} .label-text-wrapper-hidden"] = new()
            {
                Display = Display.None,
            },

            // .native-wrapper — grows to fill, centers the slider on the cross axis.
            [$".ion-range.{mode} .native-wrapper"] = new()
            {
                Display = Display.Flex,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
            },

            // .range-slider — the interactive track region. Full width, fixed height, relative so the
            // absolutely positioned bar container / knob handle anchor to it.
            [$".ion-range.{mode} .range-slider"] = new()
            {
                Position = Position.Relative,
                FlexGrow = 1,
                Width = Length.Percent(100),
                Height = Length.Px(SliderHeight),
                Cursor = Cursor.Pointer,
            },

            // .range-bar-container — vertically centered strip holding the two bars.
            [$".ion-range.{mode} .range-bar-container"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px((SliderHeight - barHeight) / 2f),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Px(barHeight),
                BorderRadius = barRadius,
            },

            // .range-bar — the inactive track (full width).
            [$".ion-range.{mode} .range-bar"] = new()
            {
                Position = Position.Absolute,
                Width = Length.Percent(100),
                Height = Length.Px(barHeight),
                BorderRadius = barRadius,
                BackgroundColor = mode == "md"
                    ? new Color(t.Primary.R, t.Primary.G, t.Primary.B, 66) // md --bar-background: primary @ .26 (66/255)
                    : Color.FromHex("e6e6e6"),   // ios --bar-background: step-900 grey
                PointerEvents = PointerEvents.None,
            },

            // .range-bar-active — the filled portion. Its right offset is set inline to the value
            // ratio (left anchored at 0). Uses the checked/active color.
            [$".ion-range.{mode} .range-bar-active"] = new()
            {
                Bottom = Length.Px(0),
                BackgroundColor = t.Primary, // --bar-background-active
            },

            // .range-knob-handle — absolutely positioned by an inline left%; self-centered by a
            // negative half-width margin. Holds the knob and pin.
            [$".ion-range.{mode} .range-knob-handle"] = new()
            {
                Position = Position.Absolute,
                Display = Display.Flex,
                JustifyContent = JustifyContent.Center,
                Top = Length.Px((SliderHeight - handleSize) / 2f),
                MarginLeft = Length.Px(-handleSize / 2f),
                Width = Length.Px(handleSize),
                Height = Length.Px(handleSize),
                TextAlign = TextAlign.Center,
            },

            // .range-knob — the draggable thumb. Centered within the handle, circular, on top.
            [$".ion-range.{mode} .range-knob"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px((handleSize - knobSize) / 2f),
                Left = Length.Px((handleSize - knobSize) / 2f),
                Width = Length.Px(knobSize),
                Height = Length.Px(knobSize),
                BorderRadius = new BorderRadius(Length.Percent(50)),
                PointerEvents = PointerEvents.None,
            },

            // .range-pin — the value bubble above the knob (only rendered when Pin is set).
            [$".ion-range.{mode} .range-pin"] = new()
            {
                Position = Position.Absolute,
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                TextAlign = TextAlign.Center,
                BoxSizing = BoxSizing.BorderBox,
            },

            // Label placement — start (default): label left, slider right.
            [$".ion-range.{mode}.range-label-placement-start .range-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-range.{mode}.range-label-placement-start .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(LabelMargin),
                MarginLeft = Length.Px(0),
            },

            // Label placement — end: slider left, label right (row-reverse).
            [$".ion-range.{mode}.range-label-placement-end .range-wrapper"] = new()
            {
                FlexDirection = FlexDirection.RowReverse,
            },
            [$".ion-range.{mode}.range-label-placement-end .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(0),
                MarginLeft = Length.Px(LabelMargin),
            },

            // Label placement — fixed: like start but the label has a fixed 100px width.
            [$".ion-range.{mode}.range-label-placement-fixed .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(LabelMargin),
                MarginLeft = Length.Px(0),
                FlexGrow = 0,
                FlexShrink = 0,
                FlexBasis = Length.Px(100),
                Width = Length.Px(100),
                MinWidth = Length.Px(100),
                MaxWidth = Length.Px(200),
            },

            // Label placement — stacked: label above the slider (column), stretched.
            [$".ion-range.{mode}.range-label-placement-stacked .range-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Stretch,
            },
            [$".ion-range.{mode}.range-label-placement-stacked .label-text-wrapper"] = new()
            {
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(LabelMargin),
                MaxWidth = Length.Percent(100),
            },
        };

        if (mode == "md")
        {
            // md knob background = active bar color; no shadow. Pin is a filled primary circle.
            css[".ion-range.md .range-knob"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px((handleSize - knobSize) / 2f),
                Left = Length.Px((handleSize - knobSize) / 2f),
                Width = Length.Px(knobSize),
                Height = Length.Px(knobSize),
                BorderRadius = new BorderRadius(Length.Percent(50)),
                BackgroundColor = t.Primary,
                PointerEvents = PointerEvents.None,
            };
            css[".ion-range.md .range-pin"] = new()
            {
                Position = Position.Absolute,
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(MdPinDimension),
                Height = Length.Px(MdPinDimension),
                BorderRadius = new BorderRadius(Length.Percent(50)),
                BackgroundColor = t.Primary,      // --pin-background
                Color = Color.White,              // --pin-color (primary contrast)
                FontSize = Length.Px(12),         // $range-md-pin-font-size
                BoxSizing = BoxSizing.BorderBox,
            };

            // Disabled: label dims; the bars turn to a neutral grey (approximated with the track grey).
            css[".ion-range.md.range-disabled .label-text-wrapper"] = new()
            {
                Opacity = 0.38f, // $range-md-disabled-opacity
            };
        }
        else
        {
            // ios knob is a white circle with a soft shadow.
            css[".ion-range.ios .range-knob"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px((handleSize - knobSize) / 2f),
                Left = Length.Px((handleSize - knobSize) / 2f),
                Width = Length.Px(knobSize),
                Height = Length.Px(knobSize),
                BorderRadius = new BorderRadius(Length.Percent(50)),
                BackgroundColor = Color.White,    // $range-ios-knob-background-color
                // $range-ios-knob-box-shadow (second, more prominent layer).
                BoxShadow = new List<BoxShadow>
                {
                    new(Length.Px(0), Length.Px(6), Length.Px(13), Length.Px(0), new Color(0, 0, 0, 31)),
                },
                PointerEvents = PointerEvents.None,
            };
            // ios pin has a transparent background and dark text.
            css[".ion-range.ios .range-pin"] = new()
            {
                Position = Position.Absolute,
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                MinWidth = Length.Px(28),         // $range-ios pin min-width
                BackgroundColor = Color.Transparent, // $range-ios-pin-background-color
                Color = t.ItemColor,              // $range-ios-pin-color (text)
                FontSize = Length.Px(12),         // $range-ios-pin-font-size
                TextAlign = TextAlign.Center,
                BoxSizing = BoxSizing.BorderBox,
            };

            // Disabled: ios dims the whole host.
            css[".ion-range.ios.range-disabled"] = new()
            {
                Opacity = 0.3f, // $range-ios-disabled-opacity
                PointerEvents = PointerEvents.None,
            };
        }

        return css;
    }
}
