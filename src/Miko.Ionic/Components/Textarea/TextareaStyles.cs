using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-textarea</c>. Ported from Ionic's source: <c>textarea.scss</c> /
/// <c>textarea.md.scss</c> / <c>textarea.ios.scss</c> (plus the solid / outline fills) and their
/// <c>*.vars.scss</c>.
/// <para>
/// The textarea is the multi-line sibling of <c>ion-input</c> and shares its theming: it reuses the
/// existing <c>Input*</c> theme tokens rather than introducing textarea-specific ones. The host
/// (<c>.ion-textarea</c>) is a full-width block. Inside, a <c>&lt;label&gt;.textarea-wrapper</c> is a
/// flex line holding <c>.label-text-wrapper</c> (the label) and <c>.textarea-wrapper-inner</c>
/// (which wraps the start/end slots and a <c>.native-wrapper</c> around the native
/// <c>&lt;textarea&gt;.native-textarea</c>). <c>labelPlacement</c> flips the wrapper's flex direction
/// (start = row, end = row-reverse, stacked/floating = column). An optional <c>.textarea-bottom</c>
/// row carries helper / error text and a character counter. In md mode a 2px <c>.textarea-highlight</c>
/// bar sits under the field; ios has no bar.
/// </para>
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>) exactly like the other ported
/// form controls; see <see cref="InputStyles"/> for the same shape.
/// </para>
/// </summary>
internal static class TextareaStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-textarea — the host. Ionic's :host: display block, relative, full width, 44px
            // min-height, item text color, the input font size (shared with ion-input).
            [$".ion-textarea.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.InputMinHeight),
                Color = t.InputTextColor,
                FontSize = Length.Px(t.InputFontSize),
                BackgroundColor = t.InputBackground,
                BoxSizing = BoxSizing.BorderBox,
            },

            // Floating / stacked labels need a taller host so the label does not crowd the text.
            [$".ion-textarea.{mode}.textarea-label-placement-floating"] = new()
            {
                MinHeight = Length.Px(56),
            },
            [$".ion-textarea.{mode}.textarea-label-placement-stacked"] = new()
            {
                MinHeight = Length.Px(56),
            },

            // .textarea-wrapper — the flex click surface. Grows to fill the host, aligns children to
            // the top on the cross axis (multi-line text sits top-aligned), carries the field padding.
            [$".ion-textarea.{mode} .textarea-wrapper"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexGrow = 1,
                AlignItems = AlignItems.FlexStart,
                Height = Length.Percent(100),
                MinHeight = Length.Px(t.InputMinHeight),
                PaddingTop = Length.Px(0),
                PaddingRight = Length.Px(t.InputPaddingEnd),
                PaddingBottom = Length.Px(8),           // $textarea-padding-bottom
                PaddingLeft = Length.Px(t.InputPaddingStart),
                BackgroundColor = t.InputBackground,
                BoxSizing = BoxSizing.BorderBox,
            },

            // .textarea-wrapper-inner — wraps the native field plus the start/end slots, filling width.
            [$".ion-textarea.{mode} .textarea-wrapper-inner"] = new()
            {
                Display = Display.Flex,
                Width = Length.Percent(100),
                MinHeight = Length.Percent(100),
            },

            // .native-wrapper — wraps the native textarea, filling the line.
            [$".ion-textarea.{mode} .native-wrapper"] = new()
            {
                Position = Position.Relative,
                FlexGrow = 1,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },

            // .native-textarea — the actual multi-line field. Fills the wrapper, transparent, borderless.
            [$".ion-textarea.{mode} .native-textarea"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                FlexGrow = 1,
                Width = Length.Percent(100),
                MaxWidth = Length.Percent(100),
                MaxHeight = Length.Percent(100),
                BorderWidth = Length.Px(0),
                BackgroundColor = Color.Transparent,
                Color = t.InputTextColor,
                FontSize = Length.Px(t.InputFontSize),
                WhiteSpace = WhiteSpace.PreWrap,
                BoxSizing = BoxSizing.BorderBox,
                ZIndex = 1,
            },

            // .label-text-wrapper — centers the label vertically; caps its width so it never eats the
            // whole line. (Ionic clips overflow; Miko has no text-overflow so overflow hides it.)
            [$".ion-textarea.{mode} .label-text-wrapper"] = new()
            {
                Display = Display.Flex,
                MaxWidth = Length.Px(200),
            },
            [$".ion-textarea.{mode} .label-text"] = new()
            {
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                Color = t.InputLabelColor,
            },

            // Empty label — Ionic hides the wrapper so it adds no margin.
            [$".ion-textarea.{mode} .label-text-wrapper-hidden"] = new()
            {
                Display = Display.None,
            },

            // .textarea-bottom — helper / error / counter row below the field, with a top border.
            [$".ion-textarea.{mode} .textarea-bottom"] = new()
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
            [$".ion-textarea.{mode} .textarea-bottom .helper-text"] = new()
            {
                Color = t.InputHelperColor,
            },
            [$".ion-textarea.{mode} .textarea-bottom .error-text"] = new()
            {
                Color = t.InputErrorColor,
            },
            [$".ion-textarea.{mode} .textarea-bottom .counter"] = new()
            {
                Color = t.InputHelperColor,
                WhiteSpace = WhiteSpace.Nowrap,
                MarginLeft = Length.Px(16),
            },

            // Label placement — start (default): label left, field right; margin on the label end.
            [$".ion-textarea.{mode}.textarea-label-placement-start .textarea-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-textarea.{mode}.textarea-label-placement-start .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(16),
                MarginLeft = Length.Px(0),
            },

            // Label placement — end: field left, label right (row-reverse); margin on the label start.
            [$".ion-textarea.{mode}.textarea-label-placement-end .textarea-wrapper"] = new()
            {
                FlexDirection = FlexDirection.RowReverse,
            },
            [$".ion-textarea.{mode}.textarea-label-placement-end .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(0),
                MarginLeft = Length.Px(16),
            },

            // Label placement — fixed: like start but the label has a fixed 100px width.
            [$".ion-textarea.{mode}.textarea-label-placement-fixed .textarea-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-textarea.{mode}.textarea-label-placement-fixed .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(16),
                MarginLeft = Length.Px(0),
            },
            [$".ion-textarea.{mode}.textarea-label-placement-fixed .label-text"] = new()
            {
                FlexGrow = 0,
                FlexShrink = 0,
                FlexBasis = Length.Px(100),
                Width = Length.Px(100),
                MinWidth = Length.Px(100),
                MaxWidth = Length.Px(200),
            },

            // Label placement — stacked / floating: label above the field (column, packed to start).
            [$".ion-textarea.{mode}.textarea-label-placement-stacked .textarea-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.FlexStart,
            },
            [$".ion-textarea.{mode}.textarea-label-placement-floating .textarea-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.FlexStart,
            },
            [$".ion-textarea.{mode}.textarea-label-placement-stacked .label-text-wrapper"] = new()
            {
                MaxWidth = Length.Percent(100),
            },
            [$".ion-textarea.{mode}.textarea-label-placement-floating .label-text-wrapper"] = new()
            {
                MaxWidth = Length.Percent(100),
            },
        };

        // Fill: solid — a filled field with a bottom border and 16px side padding (matches ion-input).
        css[$".ion-textarea.{mode}.textarea-fill-solid .textarea-wrapper"] = new()
        {
            BackgroundColor = new Color(0, 0, 0, 10),
            PaddingLeft = Length.Px(16),
            PaddingRight = Length.Px(16),
            BorderBottomWidth = Length.Px(1),
            BorderBottomStyle = BorderStyle.Solid,
            BorderBottomColor = t.InputBorderColor,
            MinHeight = Length.Px(56),
        };
        css[$".ion-textarea.{mode}.textarea-fill-solid"] = new()
        {
            MinHeight = Length.Px(56),
        };
        // Solid already has a bottom border, so the bottom-content border is dropped.
        css[$".ion-textarea.{mode}.textarea-fill-solid .textarea-bottom"] = new()
        {
            BorderTopWidth = Length.Px(0),
        };

        // Fill: outline — a bordered transparent field with rounded corners and 16px side padding.
        css[$".ion-textarea.{mode}.textarea-fill-outline .textarea-wrapper"] = new()
        {
            BorderWidth = Length.Px(1),
            BorderStyle = BorderStyle.Solid,
            BorderColor = t.InputBorderColor,
            BorderRadius = new BorderRadius(Length.Px(t.InputBorderRadius)),
            PaddingLeft = Length.Px(16),
            PaddingRight = Length.Px(16),
            MinHeight = Length.Px(56),
        };
        css[$".ion-textarea.{mode}.textarea-fill-outline"] = new()
        {
            MinHeight = Length.Px(56),
        };
        css[$".ion-textarea.{mode}.textarea-fill-outline .textarea-bottom"] = new()
        {
            BorderTopWidth = Length.Px(0),
        };

        // Shape: round — a large corner radius (Ionic uses 16px).
        css[$".ion-textarea.{mode}.textarea-shape-round .textarea-wrapper"] = new()
        {
            BorderRadius = new BorderRadius(Length.Px(16)),
        };

        // md-only: the 2px highlight bar under the field. ios has no bar (height 0), so we only emit
        // the rule for md; the element is not rendered on ios anyway.
        if (mode == "md")
        {
            css[$".ion-textarea.{mode} .textarea-highlight"] = new()
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
        css[$".ion-textarea.{mode}.textarea-disabled"] = new()
        {
            Opacity = t.InputDisabledOpacity,
            PointerEvents = PointerEvents.None,
        };
        css[$".ion-textarea.{mode}.textarea-readonly"] = new()
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
        css[$".ion-textarea.{mode}.ion-color-{name} .textarea-highlight"] = new()
        {
            BackgroundColor = color,
        };
    }
}
