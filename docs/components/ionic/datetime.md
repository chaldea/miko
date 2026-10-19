# Datetime

Datetime is an inline month calendar for picking a single date.

`IonDatetime` is the calendar; `IonDatetimeButton` is a compact button that shows a formatted date (and optionally a time) and asks the app to open a datetime.

The Miko port implements only Ionic's calendar (`date`) view. There is no time wheel, no month/year wheel (the month/year label in the calendar header is not tappable), no `min` / `max` / disabled dates, no multiple selection, and no locale formatting — labels use the invariant culture (English).

## Basic usage

Bind the selected date with `@bind-Value`. The value is an ISO date string (`yyyy-MM-dd`); a full ISO date-time is accepted on input, but only its date part is used and a selection always writes back `yyyy-MM-dd`. Tapping a day selects it immediately. The calendar opens on the month of `Value`, or the current month when `Value` is empty; the arrows in the header page between months. Today is outlined; the selected day is filled.

<!-- miko:example examples/datetime/Basic.razor -->
```razor
<IonDatetime @bind-Value="_date" />

<p>Selected: @(_date ?? "nothing yet")</p>

@code {
    // ISO date string, "yyyy-MM-dd".
    private string? _date = "2026-07-15";
}
```
<!-- /miko:example -->

## Title and buttons

The header with the title ("Select Date", not configurable) and the selected date (`ShowDefaultTitle`) is shown by default. `ShowDefaultButtons="true"` adds a Cancel / Done row (`CancelText`, `DoneText`). The buttons do not hold back the selection — `Value` has already changed when a day was tapped. `OnConfirm` receives the current value and `OnCancel` nothing, so to support Cancel keep the confirmed value yourself, as below.

<!-- miko:example examples/datetime/Buttons.razor -->
```razor
@* The value changes as soon as a day is tapped; Done / Cancel only raise events. *@
<IonDatetime @bind-Value="_draft"
             ShowDefaultTitle="false"
             ShowDefaultButtons="true"
             DoneText="Save"
             OnConfirm="Save"
             OnCancel="Revert" />

<p>Saved: @(_saved ?? "nothing yet")</p>

@code {
    private string? _saved;
    private string? _draft;

    private void Save(string? value) => _saved = value;

    private void Revert() => _draft = _saved;
}
```
<!-- /miko:example -->

## First day of the week

`FirstDayOfWeek` sets the first column of the grid: `0` = Sunday (default), `1` = Monday, and so on.

<!-- miko:example examples/datetime/FirstDayOfWeek.razor -->
```razor
@* 0 = Sunday (default), 1 = Monday, … *@
<IonDatetime FirstDayOfWeek="1" @bind-Value="_date" />

@code {
    private string? _date;
}
```
<!-- /miko:example -->

## Datetime button

In Ionic, `ion-datetime-button` finds its datetime by id, formats its value, and opens it. The Miko port has no such link: pass the text to show in `DateText` (and `TimeText`; each button renders only when its text is set) and open your own `IonDatetime` from `OnDateClick` / `OnTimeClick`, typically in an [`IonModal`](./modal.md) or [`IonPopover`](./popover.md). `DatetimeActive` highlights the button while it is open (`SelectedButton` picks the date or the time button). The `Datetime` parameter is kept only for API parity.

<!-- miko:example examples/datetime/DatetimeButton.razor -->
```razor
@* IonDatetimeButton only renders the formatted value and raises OnDateClick; the app opens its
   own IonDatetime, here in a modal, and closes it again. *@
<IonDatetimeButton DateText="@FormatDate(_date)" DatetimeActive="_isOpen"
                   OnDateClick="@(() => _isOpen = true)" />

<IonModal @bind-IsOpen="_isOpen">
    <IonDatetime @bind-Value="_date"
                 ShowDefaultButtons="true"
                 OnConfirm="@(_ => _isOpen = false)"
                 OnCancel="@(() => _isOpen = false)" />
</IonModal>

@code {
    private string? _date = "2026-07-15";
    private bool _isOpen;

    private static string FormatDate(string? value) =>
        DateTime.TryParse(value, out var date) ? date.ToString("MMM d, yyyy") : "Select date";
}
```
<!-- /miko:example -->

## Disabled and read-only

`Disabled` dims the calendar and blocks every tap. `Readonly` blocks taps — day selection, month paging, and the footer buttons — but keeps the normal look. `IonDatetimeButton` also has `Disabled`.

## API

<!-- miko:api IonDatetime IonDatetimeButton -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonDatetime

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `CancelText` | `string` | `"Cancel"` | Cancel button label. |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`). Stamped as `ion-color-*` classes for app styles to target; the built-in datetime styles do not read it, so on its own it changes nothing. |
| `Disabled` | `bool` | `false` | If true, the datetime is dimmed and non-interactive. |
| `DoneText` | `string` | `"Done"` | Confirm button label. |
| `FirstDayOfWeek` | `int` | `0` | The first day of the week (0 = Sunday, default). Ionic `firstDayOfWeek`. |
| `Presentation` | `string` | `"date"` | Which view to show. Only the default `"date"` calendar grid is rendered in this structural port; other values still stamp the presentation class for parity. |
| `Readonly` | `bool` | `false` | If true, the datetime is non-interactive but not dimmed. |
| `ShowDefaultButtons` | `bool` | `false` | If true, the footer Cancel / Done buttons are shown. They only raise [`OnCancel`](#iondatetime) / [`OnConfirm`](#iondatetime); the value already changed when the day was tapped. |
| `ShowDefaultTitle` | `bool` | `true` | If true (default), the header title/selected-date is shown. |
| `Size` | `string` | `"fixed"` | Sizing: `"fixed"` or `"cover"` (Ionic `size`). Stamped as a class. |
| `Value` | `string?` | `null` | The selected value as an ISO date string (`yyyy-MM-dd`). Bind two-way with [`ValueChanged`](#iondatetime). Two-way bindable: `@bind-Value`. |

| Event | Type | Description |
| --- | --- | --- |
| `OnCancel` | `EventCallback` | Raised when the cancel button is tapped. |
| `OnConfirm` | `EventCallback<string?>` | Raised when the confirm button is tapped, with the selected ISO date. |
| `ValueChanged` | `EventCallback<string?>` | Raised with the new ISO date string when a day is selected. |

### IonDatetimeButton

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`). Stamped as `ion-color-*` classes for app styles to target; the built-in datetime styles do not read it, so on its own it changes nothing. |
| `DateText` | `string?` | `null` | The formatted date label; when empty the date button is not rendered. |
| `Datetime` | `string?` | `null` | The id of the `ion-datetime` this button controls (Ionic `datetime`). Kept for API parity; the host wires the click callbacks in this port. |
| `DatetimeActive` | `bool` | `false` | Whether the associated datetime is currently open (stamps `{selected}-active`). |
| `Disabled` | `bool` | `false` | If true, the buttons are dimmed and non-interactive. |
| `SelectedButton` | `string` | `"date"` | Which button reads as active while the datetime is open: `"date"` or `"time"`. |
| `TimeText` | `string?` | `null` | The formatted time label; when empty the time button is not rendered. |

| Event | Type | Description |
| --- | --- | --- |
| `OnDateClick` | `EventCallback` | Raised when the date button is tapped. |
| `OnTimeClick` | `EventCallback` | Raised when the time button is tapped. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Datetime -->
Theme token `IonicTheme.Datetime` (`DatetimeToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Datetime = new DatetimeToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `#ffffff` | `#f4f5f8` |
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
| `DayOfWeekColor` | `Color` | `#808080` | `#808080` |
| `DayOfWeekFontSize` | `float` | `14` | `12` |
| `DaySize` | `float` | `42` | `40` |
| `HeaderBackground` | `Color` | `#0054e9` | `#f4f5f8` |
| `HeaderColor` | `Color` | `#ffffff` | `#000000` |
| `MonthYearColor` | `Color` | `#595959` | `#000000` |
| `SelectedDateFontSize` | `float` | `34` | `16` |
| `TitleFontSize` | `float` | `12` | `14` |
| `TodayColor` | `Color` | `#0054e9` | `#0054e9` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-datetime` | The host. Also carries the mode (`md` / `ios`) and the classes below. |
| `.datetime-presentation-{presentation}`, `.datetime-size-{size}` | `Presentation`, `Size` (markers only). |
| `.datetime-disabled`, `.datetime-readonly` | `Disabled`, `Readonly`. |
| `.ion-datetime .datetime-header` | The title area; holds `.datetime-title` and `.datetime-selected-date`. |
| `.ion-datetime .calendar-action-buttons` | The month/year label (`.calendar-month-year-toggle`) and the paging buttons (`.calendar-nav-button`). |
| `.ion-datetime .calendar-days-of-week .day-of-week` | One weekday label. |
| `.ion-datetime .calendar-month-grid` | The day grid (seven columns, wrapping). |
| `.ion-datetime .calendar-day` | One day button; `.calendar-day-padding` for the blank cells before the 1st. |
| `.calendar-day-today`, `.calendar-day-active` | Today, the selected day. |
| `.ion-datetime .datetime-footer`, `.datetime-action-button` | The Cancel / Done row and its buttons. |
| `.ion-datetime-button` | The datetime button host; its buttons are `#date-button` and `#time-button`. |
| `.date-active`, `.time-active`, `.datetime-button-disabled` | `DatetimeActive` with `SelectedButton`, `Disabled`. |

```csharp
// GlobalStyles.cs — square day cells
[".ion-datetime .calendar-day"] = new()
{
    BorderRadius = new BorderRadius(8),
},
```
