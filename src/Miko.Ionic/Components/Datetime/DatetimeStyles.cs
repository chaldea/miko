using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-datetime</c> / <c>ion-datetime-button</c>. Ported from the Ionic source:
/// <c>datetime.scss</c> / <c>.md.scss</c> / <c>.ios.scss</c> and
/// <c>datetime-button.scss</c> / <c>.md.scss</c> / <c>.ios.scss</c> (+ their <c>*.vars.scss</c>).
/// <para>
/// This covers the default calendar (date) view: a header (title + selected date), a calendar
/// (month/year toggle + a 7-column weekday row and month grid of round day buttons), and a footer.
/// Ionic uses CSS Grid for the 7 columns; Miko has no grid display, so the weekday row and month
/// grid are flex-wrap rows of seven <c>~14.2857%</c>-wide cells. Rules are scoped by the active mode
/// class (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class DatetimeStyles
{
    // 100% / 7 columns.
    private const float ColumnWidth = 100f / 7f;

    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // Host — the datetime surface.
            [$".ion-datetime.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Percent(100),
                BackgroundColor = t.DatetimeBackground,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            [$".ion-datetime.{mode}.datetime-disabled"] = new()
            {
                Opacity = 0.4f,
                PointerEvents = PointerEvents.None,
            },

            // Header — title + selected date. md fills it with the brand color.
            [$".ion-datetime.{mode} .datetime-header"] = new()
            {
                PaddingTop = Length.Px(16),
                PaddingBottom = Length.Px(16),
                PaddingLeft = Length.Px(16),
                PaddingRight = Length.Px(16),
                BackgroundColor = t.DatetimeHeaderBackground,
                Color = t.DatetimeHeaderColor,
            },
            [$".ion-datetime.{mode} .datetime-title"] = new()
            {
                Color = t.DatetimeHeaderColor,
                FontSize = Length.Px(t.DatetimeTitleFontSize),
            },
            [$".ion-datetime.{mode} .datetime-selected-date"] = new()
            {
                Color = t.DatetimeHeaderColor,
                FontSize = Length.Px(t.DatetimeSelectedDateFontSize),
            },

            // Calendar header — the month/year toggle row and nav buttons.
            [$".ion-datetime.{mode} .calendar-action-buttons"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                PaddingLeft = Length.Px(16),
                PaddingRight = Length.Px(8),
            },
            [$".ion-datetime.{mode} .calendar-month-year-toggle"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                MinHeight = Length.Px(48),
                PaddingLeft = Length.Px(0),
                BackgroundColor = Color.Transparent,
                BorderWidth = Length.Px(0),
                Color = t.DatetimeMonthYearColor,
                FontWeight = FontWeight.SemiBold,
                Cursor = Cursor.Pointer,
            },
            [$".ion-datetime.{mode} .calendar-next-prev"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
            },
            [$".ion-datetime.{mode} .calendar-nav-button"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(40),
                Height = Length.Px(40),
                BackgroundColor = Color.Transparent,
                BorderWidth = Length.Px(0),
                Color = t.DatetimeMonthYearColor,
                Cursor = Cursor.Pointer,
            },

            // Weekday row — seven equal-width, centered cells.
            [$".ion-datetime.{mode} .calendar-days-of-week"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                Color = t.DatetimeDayOfWeekColor,
                FontSize = Length.Px(t.DatetimeDayOfWeekFontSize),
            },
            [$".ion-datetime.{mode} .calendar-days-of-week .day-of-week"] = new()
            {
                Width = Length.Percent(ColumnWidth),
                TextAlign = TextAlign.Center,
            },

            // Calendar body / month grid — seven equal-width day cells, wrapping into weeks.
            [$".ion-datetime.{mode} .calendar-body"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
            },
            [$".ion-datetime.{mode} .calendar-month"] = new()
            {
                Width = Length.Percent(100),
            },
            [$".ion-datetime.{mode} .calendar-month-grid"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                FlexWrap = FlexWrap.Wrap,
                Width = Length.Percent(100),
            },
            [$".ion-datetime.{mode} .calendar-day-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(ColumnWidth),
            },

            // Day button — a round, centered cell.
            [$".ion-datetime.{mode} .calendar-day"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(t.DatetimeDaySize),
                Height = Length.Px(t.DatetimeDaySize),
                MarginTop = Length.Px(2),
                MarginBottom = Length.Px(2),
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
                BorderWidth = Length.Px(0),
                BorderRadius = new BorderRadius(Length.Percent(50)),
                BackgroundColor = Color.Transparent,
                Color = t.DatetimeDayColor,
                FontSize = Length.Px(t.DatetimeDayFontSize),
                Cursor = Cursor.Pointer,
            },

            // Padding cells are invisible placeholders before the 1st.
            [$".ion-datetime.{mode} .calendar-day-padding"] = new()
            {
                BackgroundColor = Color.Transparent,
                PointerEvents = PointerEvents.None,
            },

            // Today — a primary ring + primary text.
            [$".ion-datetime.{mode} .calendar-day.calendar-day-today"] = new()
            {
                BorderWidth = Length.Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = t.DatetimeTodayColor,
                Color = t.DatetimeTodayColor,
            },

            // Active (selected) — a filled (md) / translucent (ios) circle.
            [$".ion-datetime.{mode} .calendar-day.calendar-day-active"] = new()
            {
                BackgroundColor = t.DatetimeDayActiveBackground,
                Color = t.DatetimeDayActiveColor,
                FontWeight = FontWeight.SemiBold,
            },

            // Footer — the action buttons row.
            [$".ion-datetime.{mode} .datetime-footer"] = new()
            {
                Display = Display.Flex,
                Width = Length.Percent(100),
            },
            [$".ion-datetime.{mode} .datetime-buttons"] = new()
            {
                Display = Display.Flex,
                Width = Length.Percent(100),
                PaddingTop = Length.Px(8),
                PaddingBottom = Length.Px(8),
                PaddingLeft = Length.Px(8),
                PaddingRight = Length.Px(8),
            },
            [$".ion-datetime.{mode} .datetime-action-buttons"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Width = Length.Percent(100),
            },
            [$".ion-datetime.{mode} .datetime-action-button"] = new()
            {
                BackgroundColor = Color.Transparent,
                BorderWidth = Length.Px(0),
                Color = t.Primary,
                FontSize = Length.Px(14),
                Cursor = Cursor.Pointer,
                PaddingTop = Length.Px(8),
                PaddingBottom = Length.Px(8),
                PaddingLeft = Length.Px(12),
                PaddingRight = Length.Px(12),
            },

            // Datetime button — the date/time pill pair.
            [$".ion-datetime-button.{mode}"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Gap = Length.Px(8),
            },
            [$".ion-datetime-button.{mode} button"] = new()
            {
                PaddingTop = Length.Px(t.DatetimeButtonPaddingY),
                PaddingBottom = Length.Px(t.DatetimeButtonPaddingY),
                PaddingLeft = Length.Px(t.DatetimeButtonPaddingX),
                PaddingRight = Length.Px(t.DatetimeButtonPaddingX),
                BorderWidth = Length.Px(0),
                BorderRadius = new BorderRadius(Length.Px(t.DatetimeButtonBorderRadius)),
                BackgroundColor = t.DatetimeButtonBackground,
                Color = t.DatetimeButtonColor,
                FontSize = Length.Px(t.DatetimeButtonFontSize),
                Cursor = Cursor.Pointer,
            },
            // Active button turns the primary color (its associated datetime is open).
            [$".ion-datetime-button.{mode}.date-active #date-button"] = new()
            {
                Color = t.DatetimeButtonActiveColor,
            },
            [$".ion-datetime-button.{mode}.time-active #time-button"] = new()
            {
                Color = t.DatetimeButtonActiveColor,
            },
            [$".ion-datetime-button.{mode}.datetime-button-disabled"] = new()
            {
                Opacity = 0.4f,
                PointerEvents = PointerEvents.None,
            },
        };

        return css;
    }
}
