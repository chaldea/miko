# Picker

Pickers display one or more scrollable wheels of options, each with a single selected value, inline in the page.

`IonPicker` is the frame: it lays its columns out side by side and draws the selection band (filled in ios mode only). Each `IonPickerColumn` is one wheel with its own value, and each `IonPickerColumnOption` is one row of a column. An option only works inside a column — it reads the selected value from it and asks it to select on tap.

## Basic usage

Bind each column's value with `@bind-Value`. The user selects by scrolling the wheel (mouse wheel or fling — the nearest option snaps to the center), by dragging it with a finger (the wheel follows the finger and `ValueChanged` fires once, on release), or by tapping an option (it scrolls to the center). On first render the wheel is scrolled so that `Value` is centered. The picker is a fixed 200px tall, with 34px rows.

<!-- miko:example examples/picker/Basic.razor -->
```razor
<IonPicker>
    <IonPickerColumn @bind-Value="_language">
        <IonPickerColumnOption Value="csharp">C#</IonPickerColumnOption>
        <IonPickerColumnOption Value="fsharp">F#</IonPickerColumnOption>
        <IonPickerColumnOption Value="go">Go</IonPickerColumnOption>
        <IonPickerColumnOption Value="rust">Rust</IonPickerColumnOption>
        <IonPickerColumnOption Value="typescript">TypeScript</IonPickerColumnOption>
    </IonPickerColumn>
</IonPicker>

<p>Selected: @_language</p>

@code {
    private string? _language = "csharp";
}
```
<!-- /miko:example -->

## Multiple columns

Put several columns in one picker. Columns are independent — combine their values yourself. Option values are strings.

<!-- miko:example examples/picker/Columns.razor -->
```razor
@* Each column has its own value. Values are strings, so format numbers yourself. *@
<IonPicker>
    <IonPickerColumn @bind-Value="_hour">
        @for (var h = 0; h < 24; h++)
        {
            var value = h.ToString("00");
            <IonPickerColumnOption Value="@value">@value</IonPickerColumnOption>
        }
    </IonPickerColumn>
    <IonPickerColumn @bind-Value="_minute">
        @foreach (var m in new[] { 0, 15, 30, 45 })
        {
            var value = m.ToString("00");
            <IonPickerColumnOption Value="@value">@value</IonPickerColumnOption>
        }
    </IonPickerColumn>
</IonPicker>

<p>Time: @_hour:@_minute</p>

@code {
    private string? _hour = "09";
    private string? _minute = "30";
}
```
<!-- /miko:example -->

## Disabled options and columns

A `Disabled` option is dimmed and cannot be tapped; scrolling onto it selects the nearest enabled option instead. A `Disabled` column is dimmed and ignores taps and scrolling.

<!-- miko:example examples/picker/Disabled.razor -->
```razor
<IonPicker>
    <IonPickerColumn @bind-Value="_size">
        <IonPickerColumnOption Value="s">Small</IonPickerColumnOption>
        <IonPickerColumnOption Value="m">Medium</IonPickerColumnOption>
        <IonPickerColumnOption Value="l" Disabled="true">Large (sold out)</IonPickerColumnOption>
        <IonPickerColumnOption Value="xl">X-Large</IonPickerColumnOption>
    </IonPickerColumn>
    <IonPickerColumn Value="cotton" Disabled="true">
        <IonPickerColumnOption Value="cotton">Cotton</IonPickerColumnOption>
    </IonPickerColumn>
</IonPicker>

@code {
    private string? _size = "m";
}
```
<!-- /miko:example -->

## Picker in a modal

Ionic's standalone picker overlay is not ported; to show a picker over the page, put an `IonPicker` in an [`IonModal`](./modal.md) (or an [`IonPopover`](./popover.md)) with your own buttons. Bind the columns to a draft value so Cancel can discard it.

<!-- miko:example examples/picker/Modal.razor -->
```razor
@* The picker edits a draft; Done copies it to the confirmed value, Cancel discards it. *@
<IonButton OnClick="Open">Pet: @_pet</IonButton>

<IonModal @bind-IsOpen="_isOpen">
    <IonToolbar>
        <Start>
            <IonButtons>
                <IonButton OnClick="@(() => _isOpen = false)">Cancel</IonButton>
            </IonButtons>
        </Start>
        <End>
            <IonButtons>
                <IonButton Strong="true" OnClick="Confirm">Done</IonButton>
            </IonButtons>
        </End>
    </IonToolbar>
    <IonPicker>
        <IonPickerColumn @bind-Value="_draft">
            @foreach (var pet in _pets)
            {
                <IonPickerColumnOption Value="@pet">@pet</IonPickerColumnOption>
            }
        </IonPickerColumn>
    </IonPicker>
</IonModal>

@code {
    private readonly string[] _pets = { "Dog", "Cat", "Bird", "Lizard", "Chinchilla" };
    private string _pet = "Dog";
    private string? _draft;
    private bool _isOpen;

    private void Open()
    {
        _draft = _pet;
        _isOpen = true;
    }

    private void Confirm()
    {
        _pet = _draft ?? _pet;
        _isOpen = false;
    }
}
```
<!-- /miko:example -->

## Colors

The selected option is tinted with the primary color. To use another palette color, set `Color` on the options — `Color` on `IonPickerColumn` only adds a class to the column and does not change the options. See [Theming](./theming.md#palette).

<!-- miko:example examples/picker/Color.razor -->
```razor
@* The active-option tint comes from each option's own Color. *@
<IonPicker>
    <IonPickerColumn @bind-Value="_level">
        <IonPickerColumnOption Value="low" Color="success">Low</IonPickerColumnOption>
        <IonPickerColumnOption Value="medium" Color="warning">Medium</IonPickerColumnOption>
        <IonPickerColumnOption Value="high" Color="danger">High</IonPickerColumnOption>
    </IonPickerColumn>
</IonPicker>

@code {
    private string? _level = "medium";
}
```
<!-- /miko:example -->

## Haptics

A column gives the same haptic feedback as Ionic: a selection starts with the first scroll of a gesture, every change of the centered option ticks, and the selection ends 250ms after the wheel stops; a tap ticks once. Feedback goes through `IPlatformService.Haptics`, which is real only when the app registers its platform's native services (`UseAndroidNative()`, `UseIosNative()`); otherwise — and always on the desktop — it does nothing. The simulator (`UseSimulatorNative()`) logs and counts the calls.

## API

`SlotPrefix` and `SlotSuffix` currently stack above and below the wheel (shortening it) instead of sitting beside it as in Ionic; to label a column, put the label outside the picker or in a separate column.

<!-- miko:api IonPicker IonPickerColumn IonPickerColumnOption -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonPicker

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonPickerColumn

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`). Stamped as `ion-color-*` classes on the column only; the active-option tint is read from each [`IonPickerColumnOption`](#ionpickercolumnoption)'s own `Color` (primary when unset), so this does not recolor the options. |
| `Disabled` | `bool` | `false` | If true, the whole column is dimmed and non-interactive. |
| `Value` | `string?` | `null` | The column's selected option value. Bind two-way with [`ValueChanged`](#ionpickercolumn). Two-way bindable: `@bind-Value`. |

| Event | Type | Description |
| --- | --- | --- |
| `ValueChanged` | `EventCallback<string>` | Raised with the new value when an option is tapped or the wheel is scrolled. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
| `SlotPrefix` | `RenderFragment?` | Content shown on the left of the options (Ionic `slot="prefix"`). |
| `SlotSuffix` | `RenderFragment?` | Content shown on the right of the options (Ionic `slot="suffix"`). |

### IonPickerColumnOption

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`, defaults to `"primary"`): tints the option when active. |
| `Disabled` | `bool` | `false` | If true, this option cannot be selected (dimmed, non-interactive). |
| `Value` | `string` | `""` | The option's value; becomes the column's selected value when tapped, and identifies the option in the column's active-state check. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

The picker has no theme tokens; its sizes are fixed (200px picker, 34px rows, 22px text).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-picker` | The picker frame. Also carries the mode (`md` / `ios`). |
| `.picker-has-multiple-columns` | The picker holds more than one column. |
| `.ion-picker .picker-highlight` | The selection band behind the centered row (transparent in md mode). |
| `.ion-picker .picker-before`, `.ion-picker .picker-after` | Top / bottom overlay areas (kept for structure; they paint nothing). |
| `.ion-picker-column` | One column; `.picker-column-disabled` when `Disabled`. |
| `.ion-picker-column .picker-opts` | The scrolling wheel. |
| `.ion-picker-column .picker-item-empty` | The spacer rows above and below the options. |
| `.ion-picker-column-option` | One option (a button). Unselected options are faded. |
| `.option-active`, `.option-disabled` | The selected option, a disabled option. |
| `.ion-color-{name}` | `Color` on an option (or the column). |

```csharp
// GlobalStyles.cs — a smaller wheel font
[".ion-picker-column-option"] = new()
{
    FontSize = Length.Px(18),
},
```
