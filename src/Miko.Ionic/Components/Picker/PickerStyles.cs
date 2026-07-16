using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the Picker components (<c>ion-picker</c>, <c>ion-picker-column</c>,
/// <c>ion-picker-column-option</c>). Ported from Ionic's source: <c>picker.scss</c> /
/// <c>picker.md.scss</c> / <c>picker.ios.scss</c> (+ <c>picker.vars.scss</c>),
/// <c>picker-column.scss</c>, and <c>picker-column-option.scss</c> / <c>.md.scss</c> / <c>.ios.scss</c>.
/// <para>
/// The picker is a fixed-height (200px) flex row that lays its columns out side by side and paints a
/// centered horizontal <b>highlight band</b> (<c>.picker-highlight</c>) behind the selected row. Each
/// column is a centered vertical list of option buttons; the active option (its value matches the
/// column's selected value) is tinted the color base and shown at full opacity, while the others are
/// faded. Ionic's real wheel picker fades non-selected rows with the top/bottom gradient overlays
/// (<c>.picker-before</c> / <c>.picker-after</c>); with no scroll wheel here we instead fade the
/// inactive options directly so the selected option reads as picked.
/// </para>
/// <para>
/// There are no picker theme tokens (see <see cref="IonicTheme"/>); the picker-specific dimensions
/// are hardcoded from <c>picker.vars.scss</c> / the component SCSS with <c>// $picker-... = ...</c>
/// comments. Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see
/// <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class PickerStyles
{
    // picker.scss row height / highlight band ------------------------------------------------
    private const float PickerHeight = 200f;            // :host height: 200px
    private const float HighlightHeight = 34f;          // .picker-highlight height: 34px
    private const float HighlightBorderRadius = 8f;     // --highlight-border-radius default: 8px
    private const float HighlightInsetX = 16f;          // .picker-highlight width: calc(100% - 16px) → 8px each side

    // picker-column.scss ----------------------------------------------------------------------
    private const float ColumnFontSize = 22f;           // :host font-size: 22px
    private const float OptsPaddingX = 16f;             // .picker-opts padding: 0 16px
    private const float OptsMinWidth = 26f;             // .picker-opts min-width: 26px

    // picker-column-option.scss ---------------------------------------------------------------
    private const float OptionHeight = 34f;             // .picker-column-option-button height: 34px
    private const float DisabledOpacity = 0.4f;         // :host(.option-disabled) / disabled column opacity: 0.4
    private const float InactiveOpacity = 0.45f;        // non-selected options read faded (gradient stand-in)

    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        // Highlight band background — Ionic's ios default is --ion-color-step-150 (#eeeeef); md has
        // no default fill, but a subtle band reads better in a static (non-wheel) picker, so we use
        // the same light step-150 tint for both modes.
        var highlightBackground = Color.FromHex("eeeeef");

        var css = new CssObject
        {
            // ion-picker — a fixed-height flex row, centered, that clips its columns. z-index:0 keeps
            // the (negatively-stacked) highlight band from showing through when used inline.
            [$".ion-picker.{mode}"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Px(PickerHeight),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                ZIndex = 0,
            },

            // .picker-highlight — the center selection band. Absolutely positioned, vertically
            // centered, inset 8px on each side (width: calc(100% - 16px)), behind the columns.
            [$".ion-picker.{mode} .picker-highlight"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Percent(50),
                Left = Length.Px(HighlightInsetX / 2f),
                Right = Length.Px(HighlightInsetX / 2f),
                Height = Length.Px(HighlightHeight),
                MarginTop = Length.Px(-HighlightHeight / 2f),      // stand-in for transform: translateY(-50%)
                BackgroundColor = highlightBackground,
                BorderRadius = new BorderRadius(Length.Px(HighlightBorderRadius)),
                ZIndex = -1,
            },

            // .picker-before / .picker-after — the top/bottom fade gradient markers. Miko has no
            // gradient fill here; they are kept as absolutely-positioned non-interactive spacers so
            // the DOM contract matches (their fade is emulated by the inactive-option opacity below).
            [$".ion-picker.{mode} .picker-before"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Px(83),                            // .picker-before height: 83px
                PointerEvents = PointerEvents.None,
                ZIndex = 1,
            },
            [$".ion-picker.{mode} .picker-after"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(116),                              // .picker-after top: 116px
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Px(84),                            // .picker-after height: 84px
                PointerEvents = PointerEvents.None,
                ZIndex = 1,
            },

            // ion-picker-column — a centered vertical column, full picker height, 22px text.
            [$".ion-picker-column.{mode}"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                MaxWidth = Length.Percent(100),
                Height = Length.Px(PickerHeight),
                FontSize = Length.Px(ColumnFontSize),
                TextAlign = TextAlign.Center,
            },

            // A disabled column dims and blocks interaction (picker-column.scss
            // :host(.picker-column-disabled) ::slotted(...) opacity: 0.4 / pointer-events: none).
            [$".ion-picker-column.{mode}.picker-column-disabled"] = new()
            {
                Opacity = DisabledOpacity,
                PointerEvents = PointerEvents.None,
            },

            // .picker-opts — the option list. Side padding keeps the focus highlight from being
            // overly narrow; a min-width avoids layout shift between columns.
            [$".ion-picker-column.{mode} .picker-opts"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Stretch,
                JustifyContent = JustifyContent.Center,
                PaddingLeft = Length.Px(OptsPaddingX),
                PaddingRight = Length.Px(OptsPaddingX),
                MinWidth = Length.Px(OptsMinWidth),
                MaxHeight = Length.Px(PickerHeight),
                TextAlign = TextAlign.Center,
            },

            // ion-picker-column-option — the option button (host). Full width, 34px tall centered
            // label. Non-selected options read faded (the gradient stand-in); background transparent,
            // no border. Text uses the column's inherited 22px font.
            [$".ion-picker-column-option.{mode}"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                Height = Length.Px(OptionHeight),
                LineHeight = Length.Px(OptionHeight),
                BackgroundColor = Color.Transparent,
                Color = t.TextColor,
                FontSize = Length.Px(ColumnFontSize),
                TextAlign = TextAlign.Center,
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                Cursor = Cursor.Pointer,
                Opacity = InactiveOpacity,
            },

            // .option-active — the selected option. Full opacity, colored the primary base, bold so
            // it reads as the picked row (picker-column-option.md/ios.scss :host(.option-active) →
            // color: current-color(base)).
            [$".ion-picker-column-option.{mode}.option-active"] = new()
            {
                Opacity = 1f,
                Color = t.Primary,
                FontWeight = FontWeight.SemiBold,
            },

            // .option-disabled — a dimmed, non-interactive option.
            [$".ion-picker-column-option.{mode}.option-disabled"] = new()
            {
                Opacity = DisabledOpacity,
                Cursor = Cursor.Default,
                PointerEvents = PointerEvents.None,
            },
        };

        // Per-color active option tint (ion-color-*). Mirrors createColorClasses on the option: an
        // active option colored with a non-primary palette color takes that color's base.
        AddActiveColor(css, mode, "primary", t.Primary);
        AddActiveColor(css, mode, "secondary", t.Secondary);
        AddActiveColor(css, mode, "tertiary", t.Tertiary);
        AddActiveColor(css, mode, "success", t.Success);
        AddActiveColor(css, mode, "warning", t.Warning);
        AddActiveColor(css, mode, "danger", t.Danger);
        AddActiveColor(css, mode, "light", t.Light);
        AddActiveColor(css, mode, "medium", t.Medium);
        AddActiveColor(css, mode, "dark", t.Dark);

        return css;
    }

    // An active option carrying a palette color takes that color's base as its text tint.
    private static void AddActiveColor(CssObject css, string mode, string name, Color color)
    {
        css[$".ion-picker-column-option.{mode}.ion-color-{name}.option-active"] = new()
        {
            Color = color,
        };
    }
}
