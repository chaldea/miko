using Miko.Common;
using Miko.Styling;
using static Miko.Ionic.Styles.IonicMixins;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-datetime</c> / <c>ion-datetime-button</c>. Ported from the Ionic source:
/// <c>datetime.scss</c> / <c>.md.scss</c> / <c>.ios.scss</c> and
/// <c>datetime-button.scss</c> / <c>.md.scss</c> / <c>.ios.scss</c> (+ their <c>*.vars.scss</c>).
/// <para>
/// Every view is covered: the calendar (header, 7-column CSS grids for the weekdays and the days),
/// the month/year wheel that replaces it, the time row with its button, the wheel views and the
/// footer. The wheels themselves are <see cref="IonPicker"/>s styled by <see cref="PickerStyles"/>;
/// the navigation and footer buttons are <see cref="IonButton"/>s inside <see cref="IonButtons"/>.
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/>
/// for the mode-scoping rationale.
/// </para>
/// <para>
/// Left out: the <c>datetime-ready</c> fade-in (Miko lays the datetime out before its first
/// paint), focus rings, and the iOS Dynamic Type <c>mod()</c> tricks (the day cells keep their
/// round shape at the default font size).
/// </para>
/// </summary>
internal static class DatetimeStyles
{
    private const float DisabledOpacity = 0.4f;               // .calendar-day[disabled], disabled datetime parts

    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var host = $".ion-datetime.{mode}";
        var ios = mode == "ios";

        var css = new Rules
        {
            // :host — a column of header / calendar / time / footer.
            [host] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                BackgroundColor = t.DatetimeBackground,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // :host(.datetime-size-fixed) — as wide as its content, at most 350px for the calendar;
            // the wheels need at least 350px so their text is never cut.
            [$"{host}.datetime-size-fixed"] = new() { Width = Length.Auto, Height = Length.Auto },
            [$"{host}.datetime-size-fixed:not(.datetime-prefer-wheel)"] = new() { MaxWidth = Length.Px(350) },
            [$"{host}.datetime-size-fixed.datetime-prefer-wheel"] = new() { MinWidth = Length.Px(350) },
            [$"{host}.datetime-size-cover"] = new() { Width = Length.Percent(100) },

            // The month/year wheel of the calendar is hidden until its toggle opens it; then the
            // calendar's paging, weekdays, days and the time give way to it.
            [$"{host} .datetime-calendar, {host} .datetime-year"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Auto,
                FlexDirection = FlexDirection.Column,
            },
            [$"{host} .datetime-calendar"] = new() { Display = Display.Flex },
            [$"{host} .datetime-year"] = new() { Display = Display.None },
            [$"{host}.show-month-and-year .datetime-year"] = new() { Display = Display.Flex },
            [$"{host}.show-month-and-year .calendar-next-prev, {host}.show-month-and-year .calendar-days-of-week, {host}.show-month-and-year .calendar-body, {host}.show-month-and-year .datetime-time"] = new()
            {
                Display = Display.None,
            },
            [$"{host}.month-year-picker-open .datetime-footer"] = new() { Display = Display.None },

            // Wheel order: year-first locales put the year column first. Columns are reordered
            // with `order` rather than in the DOM, and their text aligned towards each other.
            [$"{host} .wheel-order-year-first .day-column"] = new() { Order = 3, TextAlign = TextAlign.End },
            [$"{host} .wheel-order-year-first .month-column"] = new() { Order = 2, TextAlign = TextAlign.End },
            [$"{host} .wheel-order-year-first .year-column"] = new() { Order = 1, TextAlign = TextAlign.Start },

            // :host(.datetime-disabled) — nothing reacts; weekdays and time are dimmed (the days
            // and buttons dim themselves as disabled controls).
            [$"{host}.datetime-disabled"] = new() { PointerEvents = PointerEvents.None },
            [$"{host}.datetime-disabled .calendar-days-of-week, {host}.datetime-disabled .datetime-time"] = new()
            {
                Opacity = DisabledOpacity,
            },

            // :host(.datetime-readonly) — the months can still be browsed, nothing selected; days
            // disabled only by readonly keep full opacity.
            [$"{host}.datetime-readonly"] = new() { PointerEvents = PointerEvents.None },
            [$"{host}.datetime-readonly .calendar-action-buttons, {host}.datetime-readonly .calendar-body, {host}.datetime-readonly .datetime-year"] = new()
            {
                PointerEvents = PointerEvents.Auto,
            },
            [$"{host}.datetime-readonly .calendar-day:disabled:not(.calendar-day-constrained), {host}.datetime-readonly .datetime-action-buttons .ion-button.button-disabled"] = new()
            {
                Opacity = 1f,
            },

            // Header.
            [$"{host} .datetime-header"] = new()
            {
                BackgroundColor = t.DatetimeHeaderBackground,
                Color = t.DatetimeHeaderColor,
            },
            [$"{host} .datetime-header .datetime-title"] = new()
            {
                Color = t.DatetimeTitleColor,
                FontSize = Length.Px(t.DatetimeTitleFontSize),
                TextOverflow = TextOverflow.Ellipsis,
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },
            [$"{host} .datetime-header .datetime-selected-date"] = new()
            {
                FontSize = Length.Px(t.DatetimeSelectedDateFontSize),
            },

            // Footer: Cancel at the start, Clear / Done grouped at the end.
            [$"{host} .datetime-action-buttons.has-clear-button"] = new() { Width = Length.Percent(100) },
            [$"{host} .datetime-action-buttons .ion-buttons"] = new()
            {
                Display = Display.Flex,
                JustifyContent = JustifyContent.SpaceBetween,
            },
            [$"{host} .datetime-action-buttons .datetime-action-buttons-container"] = new() { Display = Display.Flex },

            // Calendar header: the month/year toggle opposite the previous / next buttons.
            [$"{host} .calendar-action-buttons"] = new()
            {
                Display = Display.Flex,
                JustifyContent = JustifyContent.SpaceBetween,
            },
            [$"{host} .calendar-month-year"] = new() { MinWidth = Length.Px(0) },
            [$"{host} .calendar-month-year-toggle"] = WithTextInherit(new()
            {
                Position = Position.Relative,
                BorderWidth = Length.Px(0),
                BackgroundColor = Color.Transparent,
                Cursor = Cursor.Pointer,
                ZIndex = 1,
            }),
            [$"{host} .calendar-month-year-toggle:disabled"] = new()
            {
                Opacity = 0.3f,
                PointerEvents = PointerEvents.None,
            },
            [$"{host} .calendar-month-year-toggle .ion-icon"] = new()
            {
                PaddingLeft = Length.Px(4),
                FlexShrink = 0,
            },
            [$"{host} .calendar-month-year-toggle #toggle-wrapper"] = new()
            {
                Display = Display.InlineFlex,
                AlignItems = AlignItems.Center,
            },

            // Weekdays and days: seven equal columns.
            [$"{host} .calendar-days-of-week"] = new()
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Repeat(7, GridTrackSize.Fr(1)) },
                TextAlign = TextAlign.Center,
                Color = t.DatetimeDayOfWeekColor,
                FontSize = Length.Px(t.DatetimeDayOfWeekFontSize),
            },
            [$"{host} .calendar-days-of-week .day-of-week"] = new()
            {
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
            },

            // The body shows one month (Ionic scroll-snaps three side by side).
            [$"{host} .calendar-body"] = new()
            {
                Display = Display.Flex,
                FlexGrow = 1,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },
            [$"{host} .calendar-body .calendar-month"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                FlexShrink = 0,
                Width = Length.Percent(100),
            },
            [$"{host} .calendar-body .calendar-month-grid"] = new()
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Repeat(7, GridTrackSize.Fr(1)) },
            },
            [$"{host} .calendar-day-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                MinWidth = Length.Px(0),
                MinHeight = Length.Px(0),
            },

            // A day: a round button centered in its cell.
            [$"{host} .calendar-day"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(t.DatetimeDaySize),
                MinWidth = Length.Px(t.DatetimeDaySize),
                Height = Length.Px(t.DatetimeDaySize),
                PaddingTop = Length.Px(0),
                PaddingBottom = Length.Px(0),
                PaddingLeft = Length.Px(0),
                PaddingRight = Length.Px(0),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(0),
                MarginLeft = Length.Px(0),
                MarginRight = Length.Px(0),
                BorderWidth = Length.Px(0),
                BorderRadius = new BorderRadius(Length.Percent(50)),
                BackgroundColor = Color.Transparent,
                Color = t.DatetimeDayColor,
                FontSize = Length.Px(t.DatetimeDayFontSize),
                Cursor = Cursor.Pointer,
                ZIndex = 0,
            },
            [$"{host} .calendar-day:disabled"] = new()
            {
                Opacity = DisabledOpacity,
                PointerEvents = PointerEvents.None,
            },
            // A filler cell holds its place but shows nothing.
            [$"{host} .calendar-day-padding"] = new()
            {
                BackgroundColor = Color.Transparent,
                Opacity = 0f,
            },
            [$"{host} .calendar-day.calendar-day-adjacent-day"] = new() { Color = t.DatetimeAdjacentDayColor },

            // Time row: the label and the button showing the time.
            [$"{host} .datetime-time"] = new()
            {
                Display = Display.Flex,
                JustifyContent = JustifyContent.SpaceBetween,
            },
            [$"{host}.datetime-presentation-time .datetime-time"] = new()
            {
                PaddingTop = Length.Px(0),
                PaddingBottom = Length.Px(0),
                PaddingLeft = Length.Px(0),
                PaddingRight = Length.Px(0),
            },
            [$"{host} .time-header"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Color = t.DatetimeTimeHeaderColor,
            },
            [$"{host} .time-body"] = new()
            {
                Display = Display.Flex,
                PaddingTop = Length.Px(6),
                PaddingBottom = Length.Px(6),
                PaddingLeft = Length.Px(12),
                PaddingRight = Length.Px(12),
                BorderWidth = Length.Px(0),
                BorderRadius = new BorderRadius(Length.Px(8)),
                BackgroundColor = t.DatetimeButtonBackground,       // step-300, #edeef0
                Color = t.DatetimeButtonColor,                      // $text-color
                FontFamily = Css.Inherit,
                FontSize = Css.Inherit,
                Cursor = Cursor.Pointer,
            },
            [$"{host} .time-body-active"] = new() { Color = t.Primary },

            // The open month/year wheel tints its toggle.
            [$"{host}.show-month-and-year .calendar-action-buttons .calendar-month-year-toggle"] = new()
            {
                Color = t.Primary,
            },

            // Today and the selected day.
            [$"{host} .calendar-day.calendar-day-today"] = new() { Color = t.DatetimeTodayColor },
            [$"{host} .calendar-day.calendar-day-active, {host} .calendar-day.calendar-day-adjacent-day.calendar-day-active"] = new()
            {
                BackgroundColor = t.DatetimeDayActiveBackground,
                Color = t.DatetimeDayActiveColor,
            },
        };

        if (ios) AddIos(css, host, t);
        else AddMd(css, host, t);

        AddPaletteColors(css, mode, host, t);
        AddDatetimeButton(css, mode, t);
        return css.ToCssObject();
    }

    // text-inherit(): the rule's own declarations win over the mixin's, as after an SCSS @include.
    private static CssObject WithTextInherit(CssObject rule)
    {
        rule.Merge(TextInherit());
        return rule;
    }

    /// <summary>
    /// The rules being generated. Like SCSS sources compiled one after the other, a mode file may
    /// repeat a selector of the shared file: its declarations then extend the earlier rule and win
    /// where both set a property (<see cref="CssObject"/>'s own indexer would replace the rule).
    /// </summary>
    private sealed class Rules
    {
        private readonly Dictionary<string, CssObject> _rules = new(StringComparer.Ordinal);

        public CssObject this[string selector]
        {
            set
            {
                if (_rules.TryGetValue(selector, out var earlier)) value.Merge(earlier);
                _rules[selector] = value;
            }
        }

        public CssObject ToCssObject()
        {
            var css = new CssObject();
            foreach (var (selector, rule) in _rules) css[selector] = rule;
            return css;
        }
    }

    // datetime.md.scss
    private static void AddMd(Rules css, string host, IonicTheme t)
    {
        css[$"{host} .datetime-header"] = new()
        {
            PaddingTop = Length.Px(20),                         // $datetime-md-header-padding
            PaddingBottom = Length.Px(20),
            PaddingLeft = Length.Px(20),
            PaddingRight = Length.Px(20),
        };
        css[$"{host} .datetime-header .datetime-title"] = new() { TextTransform = TextTransform.Uppercase };
        css[$"{host} .datetime-header .datetime-selected-date"] = new() { MarginTop = Length.Px(30) };

        // The paging buttons and the toggle are step-350 gray.
        css[$"{host} .calendar-action-buttons .ion-button .button-native"] = new() { Color = t.DatetimeMonthYearColor };
        css[$"{host} .calendar-month-year-toggle"] = new()
        {
            PaddingTop = Length.Px(12),
            PaddingBottom = Length.Px(12),
            PaddingLeft = Length.Px(20),
            PaddingRight = Length.Px(16),
            MinHeight = Length.Px(48),
            Color = t.DatetimeMonthYearColor,
        };

        css[$"{host} .calendar-days-of-week"] = new()
        {
            PaddingLeft = Length.Px(10),
            PaddingRight = Length.Px(10),
            LineHeight = Length.Px(36),
        };

        // MD always shows six week rows, an empty one when the month needs only five.
        css[$"{host} .calendar-body .calendar-month .calendar-month-grid"] = new()
        {
            PaddingTop = Length.Px(4),
            PaddingBottom = Length.Px(4),
            PaddingLeft = Length.Px(10),
            PaddingRight = Length.Px(10),
            GridTemplateRows = new List<GridTrackSize> { GridTrackSize.Repeat(6, GridTrackSize.Fr(1)) },
        };

        // Today has a primary ring; the selected day is a filled primary circle.
        css[$"{host} .calendar-day.calendar-day-today"] = new()
        {
            BorderWidth = Length.Px(1),
            BorderStyle = BorderStyle.Solid,
            BorderColor = t.DatetimeTodayColor,
        };
        css[$"{host} .calendar-day.calendar-day-active, {host} .calendar-day.calendar-day-adjacent-day.calendar-day-active"] = new()
        {
            BorderWidth = Length.Px(1),
            BorderStyle = BorderStyle.Solid,
            BorderColor = t.DatetimeDayActiveBackground,
        };

        css[$"{host} .datetime-time"] = new()
        {
            PaddingTop = Length.Px(8),                          // $datetime-md-padding * 0.5
            PaddingBottom = Length.Px(8),
            PaddingLeft = Length.Px(16),
            PaddingRight = Length.Px(16),
        };

        css[$"{host}.datetime-presentation-month .datetime-year, {host}.datetime-presentation-year .datetime-year, {host}.datetime-presentation-month-year .datetime-year"] = new()
        {
            MarginTop = Length.Px(20),
            MarginBottom = Length.Px(20),
        };

        css[$"{host} .datetime-buttons"] = new()
        {
            Display = Display.Flex,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.FlexEnd,
            PaddingTop = Length.Px(10),
            PaddingBottom = Length.Px(10),
            PaddingLeft = Length.Px(10),
            PaddingRight = Length.Px(10),
        };
    }

    // datetime.ios.scss
    private static void AddIos(Rules css, string host, IonicTheme t)
    {
        const float padding = 16f;                              // $datetime-ios-padding
        var divider = Length.Px(0.55f);                         // $datetime-ios-border-color width

        css[$"{host}.datetime-presentation-date-time:not(.datetime-prefer-wheel), {host}.datetime-presentation-time-date:not(.datetime-prefer-wheel), {host}.datetime-presentation-date:not(.datetime-prefer-wheel)"] = new()
        {
            MinHeight = Length.Px(350),
        };

        css[$"{host} .datetime-header"] = new()
        {
            PaddingTop = Length.Px(padding),
            PaddingBottom = Length.Px(padding),
            PaddingLeft = Length.Px(padding),
            PaddingRight = Length.Px(padding),
            BorderBottomWidth = divider,
            BorderBottomStyle = BorderStyle.Solid,
            BorderBottomColor = t.DatetimeBorderColor,
            FontSize = Length.Px(t.DatetimeTitleFontSize),
        };
        css[$"{host} .datetime-header .datetime-selected-date"] = new() { MarginTop = Length.Px(10) };

        css[$"{host} .calendar-month-year-toggle"] = new()
        {
            PaddingLeft = Length.Px(padding),
            PaddingRight = Length.Px(16),
            MinHeight = Length.Px(44),
            FontSize = Length.Px(16),
            FontWeight = FontWeight.SemiBold,
            Color = t.DatetimeMonthYearColor,
        };
        css[$"{host} .calendar-month-year-toggle #toggle-wrapper"] = new()
        {
            MarginTop = Length.Px(10),
            MarginBottom = Length.Px(10),
            MarginRight = Length.Px(8),
        };

        // The toggle's chevron and the paging buttons take the color.
        css[$"{host} .calendar-action-buttons .calendar-month-year-toggle .ion-icon"] = new() { Color = t.Primary };
        css[$"{host} .calendar-action-buttons .ion-buttons .ion-button .button-native"] = new() { Color = t.Primary };
        css[$"{host} .calendar-action-buttons .ion-buttons"] = new()
        {
            AlignItems = AlignItems.Stretch,
            Height = Length.Percent(100),
        };
        css[$"{host} .calendar-action-buttons .ion-buttons .ion-button"] = new()
        {
            MarginTop = Length.Px(0),
            MarginBottom = Length.Px(0),
            MarginLeft = Length.Px(0),
            MarginRight = Length.Px(0),
        };

        css[$"{host} .calendar-days-of-week"] = new()
        {
            PaddingLeft = Length.Px(padding * 0.5f),
            PaddingRight = Length.Px(padding * 0.5f),
            FontWeight = FontWeight.SemiBold,
            LineHeight = Length.Px(24),
            TextTransform = TextTransform.Uppercase,
        };

        css[$"{host} .calendar-body .calendar-month .calendar-month-grid"] = new()
        {
            PaddingTop = Length.Px(padding * 0.5f),
            PaddingBottom = Length.Px(padding * 0.5f),
            PaddingLeft = Length.Px(padding * 0.5f),
            PaddingRight = Length.Px(padding * 0.5f),
            AlignItems = AlignItems.Center,
        };
        css[$"{host} .calendar-day-wrapper"] = new()
        {
            PaddingTop = Length.Px(4),
            PaddingBottom = Length.Px(4),
            PaddingLeft = Length.Px(4),
            PaddingRight = Length.Px(4),
        };

        // Today and the selected day show primary text; the selection adds a translucent circle,
        // and today when selected becomes a filled circle.
        css[$"{host} .calendar-day.calendar-day-active, {host} .calendar-day.calendar-day-adjacent-day.calendar-day-active"] = new()
        {
            FontSize = Length.Px(22),
            FontWeight = FontWeight.SemiBold,
        };
        css[$"{host} .calendar-day.calendar-day-today.calendar-day-active"] = new()
        {
            BackgroundColor = t.Primary,
            Color = ItemStyles.PaletteContrast("primary"),
        };

        css[$"{host} .datetime-time"] = new()
        {
            PaddingTop = Length.Px(padding * 0.5f),
            PaddingBottom = Length.Px(padding),
            PaddingLeft = Length.Px(padding),
            PaddingRight = Length.Px(padding),
            FontSize = Length.Px(16),
        };
        css[$"{host} .datetime-time .time-header"] = new() { FontWeight = FontWeight.SemiBold };

        css[$"{host} .datetime-buttons"] = new()
        {
            PaddingTop = Length.Px(padding * 0.5f),
            PaddingBottom = Length.Px(padding * 0.5f),
            PaddingLeft = Length.Px(padding * 0.5f),
            PaddingRight = Length.Px(padding * 0.5f),
            BorderTopWidth = divider,
            BorderTopStyle = BorderStyle.Solid,
            BorderTopColor = t.DatetimeBorderColor,
        };
        css[$"{host} .datetime-buttons .ion-buttons"] = new()
        {
            Display = Display.Flex,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.SpaceBetween,
        };
        css[$"{host} .datetime-action-buttons"] = new() { Width = Length.Percent(100) };
    }

    // :host(.ion-color) — the palette color replaces primary wherever the datetime uses it (the
    // footer buttons and wheel options receive the color as a parameter).
    private static void AddPaletteColors(Rules css, string mode, string host, IonicTheme t)
    {
        foreach (var (name, color) in new[]
        {
            ("primary", t.Primary), ("secondary", t.Secondary), ("tertiary", t.Tertiary),
            ("success", t.Success), ("warning", t.Warning), ("danger", t.Danger),
            ("light", t.Light), ("medium", t.Medium), ("dark", t.Dark),
        })
        {
            var contrast = ItemStyles.PaletteContrast(name);
            var colored = $"{host}.ion-color-{name}";
            css[$"{colored} .time-body-active"] = new() { Color = color };
            css[$"{colored}.show-month-and-year .calendar-action-buttons .calendar-month-year-toggle"] = new() { Color = color };

            if (mode == "ios")
            {
                css[$"{colored} .calendar-day.calendar-day-today"] = new() { Color = color };
                css[$"{colored} .calendar-day.calendar-day-active, {colored} .calendar-day.calendar-day-adjacent-day.calendar-day-active"] = new()
                {
                    BackgroundColor = new Color(color.R, color.G, color.B, 51),
                    Color = color,
                };
                css[$"{colored} .calendar-day.calendar-day-today.calendar-day-active"] = new() { BackgroundColor = color, Color = contrast };
                css[$"{colored} .calendar-action-buttons .calendar-month-year-toggle .ion-icon"] = new() { Color = color };
                css[$"{colored} .calendar-action-buttons .ion-buttons .ion-button .button-native"] = new() { Color = color };
            }
            else
            {
                css[$"{colored} .calendar-day.calendar-day-today"] = new() { BorderColor = color, Color = color };
                css[$"{colored} .calendar-day.calendar-day-active, {colored} .calendar-day.calendar-day-adjacent-day.calendar-day-active"] = new()
                {
                    BackgroundColor = color,
                    BorderColor = color,
                    Color = contrast,
                };
                css[$"{colored} .datetime-header"] = new() { BackgroundColor = color, Color = contrast };
                css[$"{colored} .datetime-header .datetime-title"] = new() { Color = contrast };
            }
        }
    }

    // datetime-button.scss / .md.scss / .ios.scss
    private static void AddDatetimeButton(Rules css, string mode, IonicTheme t)
    {
        var host = $".ion-datetime-button.{mode}";
        css[host] = new()
        {
            Display = Display.Flex,
            FlexWrap = FlexWrap.Wrap,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
        };
        css[$"{host} button"] = new()
        {
            Position = Position.Relative,
            MarginTop = Length.Px(0),
            MarginBottom = Length.Px(0),
            MarginLeft = Length.Px(2),
            MarginRight = Length.Px(2),
            PaddingTop = Length.Px(t.DatetimeButtonPaddingY),
            PaddingBottom = Length.Px(t.DatetimeButtonPaddingY),
            PaddingLeft = Length.Px(t.DatetimeButtonPaddingX),
            PaddingRight = Length.Px(t.DatetimeButtonPaddingX),
            BorderWidth = Length.Px(0),
            BorderRadius = new BorderRadius(Length.Px(t.DatetimeButtonBorderRadius)),
            BackgroundColor = t.DatetimeButtonBackground,
            Color = t.DatetimeButtonColor,
            FontFamily = Css.Inherit,
            FontSize = Length.Px(t.DatetimeButtonFontSize),
            Cursor = Cursor.Pointer,
            OverflowX = Overflow.Hidden,
            OverflowY = Overflow.Hidden,
        };
        // The button of the open datetime takes the color.
        css[$"{host}.date-active #date-button"] = new() { Color = t.DatetimeButtonActiveColor };
        css[$"{host}.time-active #time-button"] = new() { Color = t.DatetimeButtonActiveColor };
        css[$"{host}.datetime-button-disabled"] = new() { PointerEvents = PointerEvents.None };
        css[$"{host}.datetime-button-disabled button"] = new() { Opacity = DisabledOpacity };

        foreach (var (name, color) in new[]
        {
            ("primary", t.Primary), ("secondary", t.Secondary), ("tertiary", t.Tertiary),
            ("success", t.Success), ("warning", t.Warning), ("danger", t.Danger),
            ("light", t.Light), ("medium", t.Medium), ("dark", t.Dark),
        })
        {
            var button = $"{host}.ion-color-{name}";
            css[$"{button}.date-active #date-button"] = new() { Color = color };
            css[$"{button}.time-active #time-button"] = new() { Color = color };
        }
    }
}
