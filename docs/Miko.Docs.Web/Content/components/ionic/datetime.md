# Datetime

Datetime lets the user pick a date, a time, or both, from a calendar or from scrolling wheels.

`IonDatetime` is the picker; `IonDatetimeButton` is a compact button that shows a formatted date (and optionally a time) and asks the app to open a datetime.

## Basic usage

Bind the value with `@bind-Value`. The value is an ISO 8601 string — `"2026-07-15"`, `"2026-07-15T10:30:00"`, `"2026-07"`, `"10:30"` — and is taken as written: there is no time-zone conversion. A pick writes back the same form, as Ionic does: the picked date plus the time of the current value (or of now, when there is none). Without buttons every pick changes the value right away.

The default presentation, `date-time`, is a month calendar with a **Time** row below it. The month/year label in the calendar header opens month and year wheels; the arrows page between months, and so does a horizontal swipe on the days. The time button opens the hour, minute and AM/PM wheels in a popover. Today is marked, the selected day is filled.

<!-- miko:example examples/datetime/Basic.razor -->
```razor
<IonDatetime @bind-Value="_value" />

<p>Selected: @(_value ?? "nothing yet")</p>

@code {
    // An ISO 8601 string. A pick writes back the same form: here "yyyy-MM-ddTHH:mm:ss".
    private string? _value = "2026-07-15T10:30:00";
}
```
<!-- /miko:example -->

## Presentations

`Presentation` decides what is picked:

| Presentation | Calendar (grid) | Wheels |
| --- | --- | --- |
| `date` | yes | with `PreferWheel` |
| `date-time` (default) | yes, with a time button | with `PreferWheel` |
| `time-date` | yes, time button first | with `PreferWheel` |
| `time` | — | always |
| `month`, `month-year`, `year` | — | always |

<!-- miko:example examples/datetime/Presentations.razor -->
```razor
@* A calendar only, the time wheels only, and month / year wheels. *@
<IonDatetime Presentation="date" @bind-Value="_date" />
<IonDatetime Presentation="time" @bind-Value="_time" />
<IonDatetime Presentation="month-year" @bind-Value="_month" />

@code {
    private string? _date = "2026-07-15";
    private string? _time = "10:30";
    private string? _month = "2026-07";
}
```
<!-- /miko:example -->

`PreferWheel="true"` replaces the calendar with wheels: month, day and year columns for `date` (ordered as the locale writes dates), and a single date wheel next to the time wheels for `date-time` / `time-date`, which shows three months around the current one and reads **Today** for today.

<!-- miko:example examples/datetime/Wheels.razor -->
```razor
@* PreferWheel swaps the calendar for wheels: month / day / year for "date", one date wheel
   next to the time wheels for "date-time". *@
<IonDatetime Presentation="date" PreferWheel="true" @bind-Value="_date" />
<IonDatetime PreferWheel="true" @bind-Value="_dateTime" />

@code {
    private string? _date = "2026-07-15";
    private string? _dateTime = "2026-07-15T10:30:00";
}
```
<!-- /miko:example -->

`Size="cover"` makes the datetime as wide as its container; the default, `fixed`, keeps the calendar at most 350px wide and the wheels at least 350px.

## Title and buttons

`ShowDefaultTitle="true"` adds a header with the title "Select Date" and the selected date; for wheels it shows the title only. `ShowDefaultButtons="true"` adds Cancel and Done, `ShowClearButton="true"` a Clear button (`CancelText`, `DoneText`, `ClearText` change the labels).

With buttons a pick no longer changes the value: the calendar shows it, and **Done** commits it (raising `ValueChanged` and `OnConfirm`). **Cancel** raises `OnCancel` and leaves the value as it was; **Clear** empties it. When the datetime sits in an [`IonModal`](./modal.md) or [`IonPopover`](./popover.md), Done and Cancel also close it, with the role `"datetime-confirm"` or `"datetime-cancel"`.

<!-- miko:example examples/datetime/Buttons.razor -->
```razor
@* With buttons a pick shows in the calendar, but the value only changes on Done (here "Save").
   Cancel leaves it as it was; Clear empties it. *@
<IonDatetime Presentation="date"
             @bind-Value="_value"
             ShowDefaultTitle="true"
             ShowDefaultButtons="true"
             ShowClearButton="true"
             DoneText="Save" />

<p>Saved: @(_value ?? "nothing yet")</p>

@code {
    private string? _value;
}
```
<!-- /miko:example -->

The `Title`, `TimeLabel` and `Buttons` fragments replace the title, the "Time" label and the footer buttons (Ionic's `title`, `time-label` and `buttons` slots). A `Title` shows the header even without `ShowDefaultTitle`; `ShowDefaultTimeLabel="false"` hides the label. Custom buttons confirm through the datetime's methods, called with an `@ref`:

| Method | Description |
| --- | --- |
| `ConfirmAsync(bool closeOverlay = false)` | Commits the selection to the value, as Done does. Nothing selected clears the value (with `PreferWheel`, the wheels' position is confirmed). |
| `CancelAsync(bool closeOverlay = false)` | Raises `OnCancel`, as Cancel does. |
| `ResetAsync(string? startDate = null)` | Shows `startDate`, or nothing selected, without changing the value. |

`closeOverlay` also closes the enclosing modal or popover.

<!-- miko:example examples/datetime/Slots.razor -->
```razor
<IonDatetime @bind-Value="_value">
    <Title>Choose a slot</Title>
    <TimeLabel>Starts at</TimeLabel>
</IonDatetime>

@code {
    private string? _value = "2026-07-15T10:30:00";
}
```
<!-- /miko:example -->

## Limiting the choice

`Min` and `Max` take ISO strings that may be partial: `Min="2026"` is January 1, 2026, `Max="2026"` is December 31, 2026 at 23:59. Days outside the range are disabled, the paging stops at their months, and the wheels only offer what is in range — on the min or max day, the time too. Without them, the year wheel spans the last 100 years up to the current one.

`YearValues`, `MonthValues`, `DayValues`, `HourValues` and `MinuteValues` list the allowed values, comma separated (`MinuteValues="0,15,30,45"`). Months are numbered from 1; hours follow the hour cycle (0 – 23, or 0 – 11 where 0 is 12). `IsDateEnabled` decides day by day: it receives each date as `"yyyy-MM-dd"` and returns false to disable it.

<!-- miko:example examples/datetime/Constraints.razor -->
```razor
@using System.Globalization
@* The next two weeks only, weekdays only, every quarter of an hour. *@
<IonDatetime Min="2026-07-10" Max="2026-07-24T18:00"
             MinuteValues="0,15,30,45"
             IsDateEnabled="IsWeekday"
             @bind-Value="_value" />

@code {
    private string? _value = "2026-07-15T10:30:00";

    private static bool IsWeekday(string date) =>
        DateTime.Parse(date, CultureInfo.InvariantCulture).DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
```
<!-- /miko:example -->

## Locale and time

`Locale` sets the culture for month and day names, the order of the date wheels and the clock: a culture name such as `"es-ES"`, `"zh-CN"` or `"de-DE"`, or `"default"` (the default) for the app's current culture. Unicode extensions work as in Ionic (`"en-GB-u-hc-h12"`). The texts come from .NET's culture data rather than the browser's `Intl`, so a few differ in detail; the weekday initials are derived from the short names, and the "Today" label is translated for about forty languages and falls back to English.

`HourCycle` overrides the locale's clock: `"h12"` (1 – 12 with AM/PM), `"h23"` (0 – 23), `"h11"` (0 – 11 with AM/PM) or `"h24"` (1 – 24). `FirstDayOfWeek` sets the first calendar column: `0` = Sunday (default), `1` = Monday, …

<!-- miko:example examples/datetime/Locale.razor -->
```razor
@* Spanish month and day names, weeks starting on Monday, a 24-hour clock. *@
<IonDatetime Locale="es-ES"
             FirstDayOfWeek="1"
             HourCycle="h23"
             ShowDefaultTitle="true"
             @bind-Value="_value" />

@code {
    private string? _value = "2026-07-15T18:30:00";
}
```
<!-- /miko:example -->

`FormatOptions` changes how the header date and the time button are written. Each part takes the fields to show — `Weekday`, `Year`, `Month`, `Day`, `Hour`, `Minute` — with the widths of JavaScript's `Intl.DateTimeFormat` (`"numeric"`, `"2-digit"`, `"short"`, `"long"`, `"narrow"`), arranged as the locale writes them; `Pattern` takes a .NET format string instead.

```razor
<IonDatetime ShowDefaultTitle="true"
             FormatOptions="@(new DatetimeFormatOptions
             {
                 Date = new DatetimeFormat { Weekday = "long", Month = "long", Day = "numeric" },
                 Time = new DatetimeFormat { Hour = "2-digit", Minute = "2-digit" },
             })" />
```

## Several dates

With `Multiple="true"` and `Presentation="date"`, each tap adds a date or removes it again. Bind the dates with `@bind-Values`; they are date-only strings (`"2026-07-15"`). The header shows the date when one is selected, otherwise "*n* days" — or what `TitleSelectedDatesFormatter` returns for the selected dates.

<!-- miko:example examples/datetime/Multiple.razor -->
```razor
<IonDatetime Presentation="date" Multiple="true" ShowDefaultTitle="true" @bind-Values="_dates" />

<p>@(_dates is null ? "No dates" : string.Join(", ", _dates))</p>

@code {
    private IReadOnlyList<string>? _dates = ["2026-07-03", "2026-07-15"];
}
```
<!-- /miko:example -->

## Highlighted and adjacent days

`HighlightedDates` gives single days their own text color, fill and border; `HighlightedDatesCallback` computes them per day instead. A selected day keeps the selection style. `ShowAdjacentDays="true"` fills the calendar to six weeks with the end of the previous month and the start of the next; those days can be picked too, which moves the calendar to their month.

<!-- miko:example examples/datetime/Highlights.razor -->
```razor
@* Two highlighted days, and the end / start of the neighbouring months in the grid. *@
<IonDatetime Presentation="date"
             Value="2026-07-15"
             ShowAdjacentDays="true"
             HighlightedDates="_highlights" />

@code {
    private readonly DatetimeHighlight[] _highlights =
    [
        new() { Date = "2026-07-03", TextColor = Color.FromHex("800080"), BackgroundColor = Color.FromHex("ffc0cb") },
        new() { Date = "2026-07-22", TextColor = Color.FromHex("09721b"), BackgroundColor = Color.FromHex("c8e5d0") },
    ];
}
```
<!-- /miko:example -->

## Datetime button

In Ionic, `ion-datetime-button` finds its datetime by id, formats its value and opens it. The Miko port has no such link: pass the text to show in `DateText` (and `TimeText`; each button renders only when its text is set) and open your own `IonDatetime` from `OnDateClick` / `OnTimeClick`, typically in an [`IonModal`](./modal.md) or [`IonPopover`](./popover.md). `DatetimeActive` highlights the button while the datetime is open, `SelectedButton` picks which one.

<!-- miko:example examples/datetime/DatetimeButton.razor -->
```razor
@* IonDatetimeButton shows the value; tapping it opens the app's IonDatetime, here in a modal,
   as a date or a time picker. Done and Cancel close the modal the datetime is in. *@
<IonDatetimeButton DateText="@FormatDate(_value)"
                   TimeText="@FormatTime(_value)"
                   SelectedButton="@_presentation"
                   DatetimeActive="_isOpen"
                   OnDateClick="@(() => Open("date"))"
                   OnTimeClick="@(() => Open("time"))" />

<IonModal @bind-IsOpen="_isOpen">
    <IonDatetime Presentation="@_presentation" ShowDefaultButtons="true" @bind-Value="_value" />
</IonModal>

@code {
    private string? _value = "2026-07-15T10:30:00";
    private string _presentation = "date";
    private bool _isOpen;

    private void Open(string presentation)
    {
        _presentation = presentation;
        _isOpen = true;
    }

    private static string FormatDate(string? value) =>
        DateTime.TryParse(value, out var date) ? date.ToString("MMM d, yyyy") : "Select date";

    private static string FormatTime(string? value) =>
        DateTime.TryParse(value, out var date) ? date.ToString("h:mm tt") : "Select time";
}
```
<!-- /miko:example -->

## Disabled and read-only

`Disabled` blocks every interaction and dims the weekdays and the time. `Readonly` keeps the normal look and still lets the user page through months, but nothing can be selected. `IonDatetimeButton` also has `Disabled`.

## Not ported

Ionic keeps the previous and next months beside the current one in a scroll-snapping strip; Miko shows the current month and pages without the slide animation. Arrow-key navigation between days is not available, because the engine sends key events to text fields only.

## API

<!-- miko:api IonDatetime IonDatetimeButton -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonDatetime

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `CancelText` | `string` | `"Cancel"` | Cancel button label. |
| `ClearText` | `string` | `"Clear"` | Clear button label. |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"` (the look when unset), `"danger"`, …. Tints today, the selected day, the md header, the footer buttons and the selected wheel options. |
| `DayValues` | `string?` | `null` | Only these days of the month can be picked, comma separated: `"1,15"`. Days a month does not have are skipped. |
| `Disabled` | `bool` | `false` | If true, the datetime cannot be interacted with: the weekdays and time are dimmed, every button is disabled and the wheels are locked. |
| `DoneText` | `string` | `"Done"` | Done button label. |
| `FirstDayOfWeek` | `int` | `0` | The first column of the calendar: 0 = Sunday (default), 1 = Monday, …. |
| `FormatOptions` | `DatetimeFormatOptions?` | `null` | Formats of the header date and the time button (Ionic `formatOptions`). |
| `HighlightedDates` | `IReadOnlyList<DatetimeHighlight>?` | `null` | Custom colors for given calendar days. A selected day keeps its own style. |
| `HighlightedDatesCallback` | `Func<string, DatetimeHighlightStyle?>?` | `null` | Custom colors per calendar day, computed: receives the day as an ISO date and returns its colors, or null for the normal style. Used when [`HighlightedDates`](#iondatetime) is null. |
| `HourCycle` | `string?` | `null` | The hour cycle: `"h12"` (1 – 12 with AM/PM), `"h23"` (0 – 23), `"h11"` (0 – 11 with AM/PM) or `"h24"` (1 – 24). Null follows [`Locale`](#iondatetime). |
| `HourValues` | `string?` | `null` | Only these hours can be picked, in the hour cycle's numbering (0 – 23 for 24-hour, 0 – 11 for 12-hour where 0 is 12), comma separated. |
| `IsDateEnabled` | `Func<string, bool>?` | `null` | Decides which calendar days can be picked: receives each day as an ISO date (`"2026-07-15"`) and returns false to disable it. Called for every rendered day, so keep it cheap; an exception counts as enabled. |
| `Locale` | `string` | `"default"` | The locale used for month and day names, number order and the hour cycle: a culture name such as `"es-ES"` or `"zh-CN"` (Unicode extensions like `"en-GB-u-hc-h12"` work), or `"default"` for the current culture. |
| `Max` | `string?` | `null` | The latest selectable value, as an ISO string that may be partial: `"2026"` means December 31, 2026 at 23:59. Defaults to the end of the current year for the year wheel. |
| `Min` | `string?` | `null` | The earliest selectable value, as an ISO string that may be partial: `"2026"` means January 1, 2026. Defaults to 100 years before the current year for the year wheel. |
| `MinuteValues` | `string?` | `null` | Only these minutes can be picked, comma separated: `"0,15,30,45"`. |
| `MonthValues` | `string?` | `null` | Only these months can be picked (1 = January), comma separated: `"6,7,8"`. |
| `Multiple` | `bool` | `false` | If true, several dates can be picked (only with `Presentation="date"` and a calendar). Bind [`Values`](#iondatetime). |
| `PreferWheel` | `bool` | `false` | If true, `"date"`, `"date-time"` and `"time-date"` show wheels instead of a calendar. The other presentations always use wheels. |
| `Presentation` | `string` | `"date-time"` | What to pick: `"date-time"` (default) a calendar plus a time button, `"time-date"` the same with the time first, `"date"` a calendar, `"time"` hour / minute / AM-PM wheels, `"month-year"`, `"month"` or `"year"` wheels. |
| `Readonly` | `bool` | `false` | If true, the datetime looks normal but its value cannot change. The calendar can still be browsed month by month. |
| `ShowAdjacentDays` | `bool` | `false` | If true, the calendar always shows six weeks, filled with the end of the previous month and the start of the next; those days can be picked too. |
| `ShowClearButton` | `bool` | `false` | If true, a Clear button is shown in the footer; it empties the value. |
| `ShowDefaultButtons` | `bool` | `false` | If true, Cancel and Done buttons are shown. A pick then changes [`Value`](#iondatetime) only on Done; Cancel raises [`OnCancel`](#iondatetime). Both close an enclosing [`IonModal`](./modal.md#ionmodal) or [`IonPopover`](./popover.md#ionpopover). |
| `ShowDefaultTimeLabel` | `bool` | `true` | If true (default), the time row of `"date-time"` / `"time-date"` is labelled "Time". A [`TimeLabel`](#iondatetime) replaces it. |
| `ShowDefaultTitle` | `bool` | `false` | If true, a header with the title ("Select Date") and the selected date is shown above the calendar. A [`Title`](#iondatetime) shows it too. |
| `Size` | `string` | `"fixed"` | `"fixed"` (default): at most 350px wide. `"cover"`: fills the width of its container. |
| `TitleSelectedDatesFormatter` | `Func<IReadOnlyList<string>, string>?` | `null` | Header text when [`Multiple`](#iondatetime) has zero or several dates selected; receives the selected ISO dates. Defaults to `"{count} days"`. |
| `Value` | `string?` | `null` | The value as an ISO 8601 string: `"2026-07-15"`, `"2026-07-15T13:30:00"`, `"2026-07"`, `"13:30"` …. Picking writes back the same form Ionic does: the date plus the time of the current value (or of now, when there is none). No time zone conversion happens. Bind two-way with [`ValueChanged`](#iondatetime); with [`Multiple`](#iondatetime) use [`Values`](#iondatetime). Two-way bindable: `@bind-Value`. |
| `Values` | `IReadOnlyList<string>?` | `null` | The selected dates when [`Multiple`](#iondatetime) is true (Ionic's array `value`), each an ISO date string. Bind two-way with [`ValuesChanged`](#iondatetime). Two-way bindable: `@bind-Values`. |
| `YearValues` | `string?` | `null` | Only these years can be picked, comma separated: `"2024,2028,2032"`. |

| Event | Type | Description |
| --- | --- | --- |
| `OnCancel` | `EventCallback` | Raised when the selection is cancelled — the Cancel button or [`CancelAsync`](#iondatetime) (Ionic `ionCancel`). |
| `OnConfirm` | `EventCallback<string?>` | Raised after [`ConfirmAsync`](#iondatetime) committed the value — the Done button, or a pick without buttons — with the confirmed value (the first date with [`Multiple`](#iondatetime)). |
| `ValueChanged` | `EventCallback<string?>` | Raised with the new value when the user changes it (Ionic `ionChange`): right away, or on Done when there are buttons. Null when the value was cleared. |
| `ValuesChanged` | `EventCallback<IReadOnlyList<string>?>` | Raised with the new dates when [`Multiple`](#iondatetime) is true; null when none is left. |

| Slot | Type | Description |
| --- | --- | --- |
| `Buttons` | `RenderFragment?` | Footer content replacing the default buttons (Ionic `slot="buttons"`). As with the default buttons, a pick then waits for [`ConfirmAsync`](#iondatetime), which these buttons call through an `@ref`. |
| `TimeLabel` | `RenderFragment?` | The label of the time row (Ionic `slot="time-label"`), replacing "Time". |
| `Title` | `RenderFragment?` | The header title (Ionic `slot="title"`), replacing "Select Date". Shows the header even without [`ShowDefaultTitle`](#iondatetime). |

### IonDatetimeButton

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. The text color of the active button (see [`DatetimeActive`](#iondatetimebutton)), primary by default. |
| `DateText` | `string?` | `null` | The formatted date label; when empty the date button is not rendered. |
| `DatetimeActive` | `bool` | `false` | Whether the associated datetime is currently open (stamps `{selected}-active`). |
| `Disabled` | `bool` | `false` | If true, the buttons are dimmed and non-interactive. |
| `SelectedButton` | `string` | `"date"` | Which button reads as active while the datetime is open: `"date"` or `"time"`. |
| `TimeText` | `string?` | `null` | The formatted time label; when empty the time button is not rendered. |

| Event | Type | Description |
| --- | --- | --- |
| `OnDateClick` | `EventCallback` | Raised when the date button is tapped. |
| `OnTimeClick` | `EventCallback` | Raised when the time button is tapped. |
<!-- /miko:api -->

## Types

<!-- miko:type DatetimeFormatOptions DatetimeFormat DatetimeHighlight DatetimeHighlightStyle -->
### DatetimeFormatOptions

How [`IonDatetime`](#iondatetime) formats the text it shows (Ionic `formatOptions`): the selected date in the header and the time on the time button. Ionic validates that the presentation has what it needs: `date`, `month`, `month-year` and `year` read [`Date`](#datetimeformatoptions), `time` reads [`Time`](#datetimeformatoptions), `date-time` / `time-date` read either.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Date` | `DatetimeFormat?` | `null` | Format of the selected date in the header, e.g. `new() { Weekday = "long", Month = "long", Day = "numeric" }`. |
| `Time` | `DatetimeFormat?` | `null` | Format of the time button, e.g. `new() { Hour = "2-digit", Minute = "2-digit" }`. |

### DatetimeFormat

The fields of a date or time to show and their width — the subset of JavaScript's `Intl.DateTimeFormatOptions` that Ionic's datetime uses. The text follows the datetime's locale: field order, separators and names come from its .NET `CultureInfo`. Time zones are not supported, as in Ionic (values never carry one).

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Weekday` | `string?` | `null` | Day of the week: `"long"` (Wednesday), `"short"` (Wed) or `"narrow"` (shown like short). |
| `Year` | `string?` | `null` | `"numeric"` (2026) or `"2-digit"` (26). |
| `Month` | `string?` | `null` | `"numeric"` (7), `"2-digit"` (07), `"long"` (July), `"short"` (Jul) or `"narrow"` (shown like short). Locales that write the month as a number next to a unit (`7月`) keep the number for the text widths. |
| `Day` | `string?` | `null` | `"numeric"` (5) or `"2-digit"` (05). |
| `Hour` | `string?` | `null` | `"numeric"` or `"2-digit"`. The hour cycle comes from the datetime's `HourCycle` or its locale. |
| `Minute` | `string?` | `null` | `"numeric"` or `"2-digit"`; minutes always show two digits after an hour. |
| `Pattern` | `string?` | `null` | A .NET custom date and time format string (`"dddd d MMMM"`) used instead of the fields above. Not available in Ionic. |

### DatetimeHighlight

A highlighted calendar day for [`IonDatetime.HighlightedDates`](#iondatetime) (Ionic `DatetimeHighlight`).

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Date` | `string` | `""` | The day as an ISO date, `yyyy-MM-dd`. |
| `TextColor` | `Color?` | `null` | The day number's color. |
| `BackgroundColor` | `Color?` | `null` | The day circle's fill. |
| `Border` | `Border?` | `null` | The day circle's border. |

### DatetimeHighlightStyle

Custom colors for one highlighted calendar day (Ionic `DatetimeHighlightStyle`), as returned by [`IonDatetime.HighlightedDatesCallback`](#iondatetime). Unset values keep the normal day style.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `TextColor` | `Color?` | `null` | The day number's color. |
| `BackgroundColor` | `Color?` | `null` | The day circle's fill. |
| `Border` | `Border?` | `null` | The day circle's border. |
<!-- /miko:type -->

## Styling

### Theme tokens

<!-- miko:tokens Datetime -->
Theme token `IonicTheme.Datetime` (`DatetimeToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Datetime = new DatetimeToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `AdjacentDayColor` | `Color` | `#808080` | `#b3b3b3` |
| `Background` | `Color` | `#ffffff` | `#f4f5f8` |
| `BorderColor` | `Color` | `transparent` | `#cccccc` |
| `ButtonActiveColor` | `Color` | `#0054e9` | `#0054e9` |
| `ButtonBackground` | `Color` | `#edeef0` | `#edeef0` |
| `ButtonBorderRadius` | `float` | `8` | `8` |
| `ButtonColor` | `Color` | `#000000` | `#000000` |
| `ButtonFontSize` | `float` | `16` | `16` |
| `ButtonPaddingX` | `float` | `12` | `13` |
| `ButtonPaddingY` | `float` | `6` | `7` |
| `DayActiveBackground` | `Color` | `#0054e9` | `rgba(0, 84, 233, 0.2)` |
| `DayActiveColor` | `Color` | `#ffffff` | `#0054e9` |
| `DayColor` | `Color` | `#000000` | `#000000` |
| `DayFontSize` | `float` | `14` | `20` |
| `DayOfWeekColor` | `Color` | `#808080` | `#b3b3b3` |
| `DayOfWeekFontSize` | `float` | `14` | `12` |
| `DaySize` | `float` | `42` | `40` |
| `HeaderBackground` | `Color` | `#0054e9` | `#f4f5f8` |
| `HeaderColor` | `Color` | `#ffffff` | `#000000` |
| `MonthYearColor` | `Color` | `#595959` | `#000000` |
| `SelectedDateFontSize` | `float` | `34` | `14` |
| `TimeHeaderColor` | `Color` | `#595959` | `#000000` |
| `TitleColor` | `Color` | `#ffffff` | `#666666` |
| `TitleFontSize` | `float` | `12` | `14` |
| `TodayColor` | `Color` | `#0054e9` | `#0054e9` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-datetime` | The host. Also carries the mode (`md` / `ios`) and the classes below. |
| `.datetime-presentation-{presentation}`, `.datetime-size-fixed`, `.datetime-size-cover` | `Presentation`, `Size`. |
| `.datetime-grid`, `.datetime-prefer-wheel` | A calendar is shown; wheels replace a calendar (`PreferWheel`). |
| `.show-month-and-year`, `.month-year-picker-open` | Month/year wheels are shown; they are open over the calendar. |
| `.datetime-disabled`, `.datetime-readonly` | `Disabled`, `Readonly`. |
| `.ion-datetime .datetime-header` | The header; holds `.datetime-title` and `.datetime-selected-date`. |
| `.ion-datetime .calendar-action-buttons` | The month/year toggle (`.calendar-month-year-toggle`) and the paging buttons (`.calendar-next-prev`). |
| `.ion-datetime .calendar-days-of-week .day-of-week` | One weekday label. |
| `.ion-datetime .calendar-month-grid` | The day grid (seven columns). |
| `.ion-datetime .calendar-day` | One day button: `.calendar-day-padding` for the blank cells before the 1st, `.calendar-day-adjacent-day` for days of the neighbouring months. |
| `.calendar-day-today`, `.calendar-day-active`, `.calendar-day-constrained` | Today, a selected day, a day ruled out by `Min` / `Max` / the value lists while the datetime is disabled or read-only. |
| `.ion-datetime .datetime-year` | The month/year wheels of the calendar. |
| `.wheel-order-month-first`, `.wheel-order-year-first`; `.month-column`, `.day-column`, `.year-column`, `.date-column` | The wheel view and its date columns. |
| `.ion-datetime .datetime-time`, `.time-header`, `.time-body` | The time row, its label and its button (`.time-body-active` while open). |
| `.ion-datetime .datetime-footer`, `.datetime-action-buttons`, `#cancel-button`, `#clear-button`, `#confirm-button` | The footer and its buttons. |
| `.ion-datetime-button` | The datetime button host; its buttons are `#date-button` and `#time-button`. |
| `.date-active`, `.time-active`, `.datetime-button-disabled` | `DatetimeActive` with `SelectedButton`, `Disabled`. |

```csharp
// GlobalStyles.cs — square day cells
[".ion-datetime .calendar-day"] = new()
{
    BorderRadius = new BorderRadius(8),
},
```
