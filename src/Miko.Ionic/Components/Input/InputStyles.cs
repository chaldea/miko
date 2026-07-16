using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-input</c>. Ported from Ionic's source: <c>input.scss</c> /
/// <c>input.md.scss</c> / <c>input.ios.scss</c> (plus the solid / outline fills) and their
/// <c>*.vars.scss</c>.
/// <para>
/// The host (<c>.ion-input</c>) is a full-width block. Inside, a <c>&lt;label&gt;.input-wrapper</c>
/// is a flex line holding <c>.label-text-wrapper</c> (the label) and <c>.native-wrapper</c>
/// (which wraps the native <c>&lt;input&gt;.native-input</c>, an optional clear button, and the
/// start/end slots). <c>labelPlacement</c> flips the wrapper's flex direction (start = row,
/// end = row-reverse, stacked/floating = column). An optional <c>.input-bottom</c> row carries
/// helper / error text and a character counter. In md mode a 2px <c>.input-highlight</c> bar sits
/// under the field; ios has no bar (<c>--highlight-height: 0</c>).
/// </para>
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>) exactly like the other
/// ported form controls; see <see cref="SelectStyles"/> for the same shape.
/// </para>
/// </summary>
internal static class InputStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-input — the host. Ionic's :host: display block, relative, full width, 44px
            // min-height, item text color, the input font size.
            [$".ion-input.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.InputMinHeight),
                Color = t.InputTextColor,
                FontSize = Length.Px(t.InputFontSize),
                BackgroundColor = t.InputBackground,
            },

            // Floating / stacked labels need a taller host so the label does not crowd the text.
            [$".ion-input.{mode}.input-label-placement-floating"] = new()
            {
                MinHeight = Length.Px(56),
            },
            [$".ion-input.{mode}.input-label-placement-stacked"] = new()
            {
                MinHeight = Length.Px(56),
            },

            // .input-wrapper — the flex click surface. Grows to fill the host, stretches its
            // children on the cross axis, carries the field padding + background.
            [$".ion-input.{mode} .input-wrapper"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexGrow = 1,
                AlignItems = AlignItems.Stretch,
                Height = Length.Percent(100),
                MinHeight = Length.Px(t.InputMinHeight),
                PaddingTop = Length.Px(0),
                PaddingRight = Length.Px(t.InputPaddingEnd),
                PaddingBottom = Length.Px(0),
                PaddingLeft = Length.Px(t.InputPaddingStart),
                BackgroundColor = t.InputBackground,
                BoxSizing = BoxSizing.BorderBox,
            },

            // .native-input — the actual text field. Fills the wrapper, transparent, borderless.
            [$".ion-input.{mode} .native-input"] = new()
            {
                Display = Display.InlineBlock,
                Position = Position.Relative,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Percent(0),
                Width = Length.Percent(100),
                MaxWidth = Length.Percent(100),
                Height = Length.Percent(100),
                BorderWidth = Length.Px(0),
                BackgroundColor = Color.Transparent,
                Color = t.InputTextColor,
                FontSize = Length.Px(t.InputFontSize),
                BoxSizing = BoxSizing.BorderBox,
                ZIndex = 1,
            },

            // .native-wrapper — wraps the field + clear button + start/end slots, centered.
            [$".ion-input.{mode} .native-wrapper"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
                Width = Length.Percent(100),
            },

            // .label-text-wrapper — centers the label vertically; caps its width so it never eats
            // the whole line. (Ionic clips overflow; Miko has no text-overflow so overflow hides it.)
            [$".ion-input.{mode} .label-text-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                MaxWidth = Length.Px(200),
            },
            [$".ion-input.{mode} .label-text"] = new()
            {
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                Color = t.InputLabelColor,
            },

            // Empty label — Ionic hides the wrapper so it adds no margin.
            [$".ion-input.{mode} .label-text-wrapper-hidden"] = new()
            {
                Display = Display.None,
            },

            // .input-bottom — helper / error / counter row below the field, with a top border.
            [$".ion-input.{mode} .input-bottom"] = new()
            {
                Display = Display.Flex,
                JustifyContent = JustifyContent.SpaceBetween,
                PaddingTop = Length.Px(5),
                PaddingRight = Length.Px(t.InputPaddingEnd),
                PaddingLeft = Length.Px(t.InputPaddingStart),
                BorderTopWidth = Length.Px(1),
                BorderTopStyle = BorderStyle.Solid,
                BorderTopColor = t.InputBorderColor,
                FontSize = Length.Px(12),
                WhiteSpace = WhiteSpace.Normal,
            },
            [$".ion-input.{mode} .input-bottom .helper-text"] = new()
            {
                Color = t.InputHelperColor,
            },
            [$".ion-input.{mode} .input-bottom .error-text"] = new()
            {
                Color = t.InputErrorColor,
            },
            [$".ion-input.{mode} .input-bottom .counter"] = new()
            {
                Color = t.InputHelperColor,
                WhiteSpace = WhiteSpace.Nowrap,
                MarginLeft = Length.Px(16),
            },

            // .input-clear-icon — the trailing clear button, hidden until there is a value.
            [$".ion-input.{mode} .input-clear-icon"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(30),
                Height = Length.Px(30),
                BorderWidth = Length.Px(0),
                BackgroundColor = Color.Transparent,
                Color = t.InputClearIconColor,
                Visibility = Visibility.Hidden,
            },
            [$".ion-input.{mode}.has-value .input-clear-icon"] = new()
            {
                Visibility = Visibility.Visible,
            },

            // Label placement — start (default): label left, field right; margin on the label end.
            [$".ion-input.{mode}.input-label-placement-start .input-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-input.{mode}.input-label-placement-start .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(16),
                MarginLeft = Length.Px(0),
            },

            // Label placement — end: field left, label right (row-reverse); margin on the label start.
            [$".ion-input.{mode}.input-label-placement-end .input-wrapper"] = new()
            {
                FlexDirection = FlexDirection.RowReverse,
            },
            [$".ion-input.{mode}.input-label-placement-end .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(0),
                MarginLeft = Length.Px(16),
            },

            // Label placement — fixed: like start but the label has a fixed 100px width.
            [$".ion-input.{mode}.input-label-placement-fixed .input-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-input.{mode}.input-label-placement-fixed .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(16),
                MarginLeft = Length.Px(0),
            },
            [$".ion-input.{mode}.input-label-placement-fixed .label-text"] = new()
            {
                FlexGrow = 0,
                FlexShrink = 0,
                FlexBasis = Length.Px(100),
                Width = Length.Px(100),
                MinWidth = Length.Px(100),
                MaxWidth = Length.Px(200),
            },

            // Label placement — stacked / floating: label above the field (column, packed to start).
            [$".ion-input.{mode}.input-label-placement-stacked .input-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.FlexStart,
            },
            [$".ion-input.{mode}.input-label-placement-floating .input-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.FlexStart,
            },
            [$".ion-input.{mode}.input-label-placement-stacked .label-text-wrapper"] = new()
            {
                MaxWidth = Length.Percent(100),
            },
            [$".ion-input.{mode}.input-label-placement-floating .label-text-wrapper"] = new()
            {
                MaxWidth = Length.Percent(100),
            },
        };

        // Fill: solid — a filled field with a bottom border and 16px side padding (md only really,
        // but Ionic emits it in the shared fill files; we scope by mode for consistency).
        css[$".ion-input.{mode}.input-fill-solid .input-wrapper"] = new()
        {
            BackgroundColor = new Color(0, 0, 0, 10),
            PaddingLeft = Length.Px(16),
            PaddingRight = Length.Px(16),
            BorderBottomWidth = Length.Px(1),
            BorderBottomStyle = BorderStyle.Solid,
            BorderBottomColor = t.InputBorderColor,
            MinHeight = Length.Px(56),
        };
        css[$".ion-input.{mode}.input-fill-solid"] = new()
        {
            MinHeight = Length.Px(56),
        };
        // Solid already has a bottom border, so the bottom-content border is dropped.
        css[$".ion-input.{mode}.input-fill-solid .input-bottom"] = new()
        {
            BorderTopWidth = Length.Px(0),
        };

        // Fill: outline — a bordered transparent field with rounded corners and 16px side padding.
        css[$".ion-input.{mode}.input-fill-outline .input-wrapper"] = new()
        {
            BorderWidth = Length.Px(1),
            BorderStyle = BorderStyle.Solid,
            BorderColor = t.InputBorderColor,
            BorderRadius = new BorderRadius(Length.Px(t.InputBorderRadius)),
            PaddingLeft = Length.Px(16),
            PaddingRight = Length.Px(16),
            MinHeight = Length.Px(56),
        };
        css[$".ion-input.{mode}.input-fill-outline"] = new()
        {
            MinHeight = Length.Px(56),
        };
        css[$".ion-input.{mode}.input-fill-outline .input-bottom"] = new()
        {
            BorderTopWidth = Length.Px(0),
        };

        // Shape: round — a large corner radius (Ionic uses 16px).
        css[$".ion-input.{mode}.input-shape-round .input-wrapper"] = new()
        {
            BorderRadius = new BorderRadius(Length.Px(16)),
        };

        // md-only: the 2px highlight bar under the field. ios has no bar (height 0), so we only
        // emit the rule for md; the element is not rendered on ios anyway.
        if (mode == "md")
        {
            css[$".ion-input.{mode} .input-highlight"] = new()
            {
                Position = Position.Absolute,
                Left = Length.Px(0),
                Bottom = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Px(t.InputHighlightHeight),
                BackgroundColor = t.InputHighlightColor,
            };
        }

        // Readonly / disabled — Ionic dims the disabled host and blocks pointer interaction.
        css[$".ion-input.{mode}.input-disabled"] = new()
        {
            Opacity = t.InputDisabledOpacity,
            PointerEvents = PointerEvents.None,
        };
        css[$".ion-input.{mode}.input-readonly"] = new()
        {
            PointerEvents = PointerEvents.None,
        };

        AddColor(css, mode, "primary", t.Primary);
        AddColor(css, mode, "secondary", t.Secondary);
        AddColor(css, mode, "tertiary", t.Tertiary);
        AddColor(css, mode, "success", t.Success);
        AddColor(css, mode, "warning", t.Warning);
        AddColor(css, mode, "danger", t.Danger);
        AddColor(css, mode, "light", t.Light);
        AddColor(css, mode, "medium", t.Medium);
        AddColor(css, mode, "dark", t.Dark);

        return css;
    }

    // A named color tints the focus highlight bar (Ionic's --highlight-color-focused: current-color).
    private static void AddColor(CssObject css, string mode, string name, Color color)
    {
        css[$".ion-input.{mode}.ion-color-{name} .input-highlight"] = new()
        {
            BackgroundColor = color,
        };
    }
}
