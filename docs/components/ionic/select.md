# Select

Selects are form controls that let the user pick one or several values from a set of options; tapping the select opens an overlay listing the options.

`IonSelect` is the field; each `IonSelectOption` child declares one option. Options render nothing themselves — they only register their value and text with the parent select, which builds the overlay from them.

## Basic usage

Bind the selected value with `@bind-Value`. An option without `Value` uses its text as the value. The field shows the selected option's text, or `Placeholder` while nothing is selected.

<!-- miko:example examples/select/Basic.razor -->
```razor
<IonList>
    <IonItem>
        <IonSelect Label="Fruit" Placeholder="Select fruit" @bind-Value="_fruit">
            <IonSelectOption Value="apple">Apple</IonSelectOption>
            <IonSelectOption Value="banana">Banana</IonSelectOption>
            <IonSelectOption Value="orange">Orange</IonSelectOption>
        </IonSelect>
    </IonItem>
</IonList>

@code {
    private string? _fruit;
}
```
<!-- /miko:example -->

## Interfaces

`Interface` picks the overlay: `alert` (default, radio buttons with Cancel / OK), `action-sheet` (one button per option plus Cancel), `popover` (a radio list anchored to the tap), or `modal` (a full list with a close button). `InterfaceHeader`, `InterfaceSubHeader`, and `InterfaceMessage` fill the overlay's header; the alert falls back to `Label` when `InterfaceHeader` is not set.

The alert and action sheet apply the selection when they close; the popover and modal apply it as soon as an option is tapped. The overlay is declared by the select itself and is drawn in `IonApp`'s overlay layer — see [Overlays](./overlays.md).

<!-- miko:example examples/select/Interfaces.razor -->
```razor
<IonList>
    <IonItem>
        <IonSelect Label="Alert" Placeholder="Pick one" @bind-Value="_alert">
            <IonSelectOption Value="red">Red</IonSelectOption>
            <IonSelectOption Value="green">Green</IonSelectOption>
            <IonSelectOption Value="blue">Blue</IonSelectOption>
        </IonSelect>
    </IonItem>
    <IonItem>
        <IonSelect Label="Action sheet" Placeholder="Pick one" Interface="action-sheet"
                   InterfaceHeader="Colors" @bind-Value="_actionSheet">
            <IonSelectOption Value="red">Red</IonSelectOption>
            <IonSelectOption Value="green">Green</IonSelectOption>
            <IonSelectOption Value="blue">Blue</IonSelectOption>
        </IonSelect>
    </IonItem>
    <IonItem>
        <IonSelect Label="Popover" Placeholder="Pick one" Interface="popover" @bind-Value="_popover">
            <IonSelectOption Value="red">Red</IonSelectOption>
            <IonSelectOption Value="green">Green</IonSelectOption>
            <IonSelectOption Value="blue">Blue</IonSelectOption>
        </IonSelect>
    </IonItem>
    <IonItem>
        <IonSelect Label="Modal" Placeholder="Pick one" Interface="modal"
                   InterfaceHeader="Choose a color" @bind-Value="_modal">
            <IonSelectOption Value="red">Red</IonSelectOption>
            <IonSelectOption Value="green">Green</IonSelectOption>
            <IonSelectOption Value="blue">Blue</IonSelectOption>
        </IonSelect>
    </IonItem>
</IonList>

@code {
    private string? _alert;
    private string? _actionSheet;
    private string? _popover;
    private string? _modal;
}
```
<!-- /miko:example -->

## Multiple selection

With `Multiple="true"` bind `Values` (`IReadOnlyList<string>`) instead of `Value`. The alert, popover, and modal show checkboxes; the popover and modal stay open while options are ticked. `action-sheet` cannot select several values and falls back to `alert`. The field shows the selected options' text joined with `", "`. A `Disabled` option is shown but cannot be picked (the action sheet ignores `Disabled`, as in Ionic).

<!-- miko:example examples/select/Multiple.razor -->
```razor
@* Multi-value selects bind Values (a list), not Value. *@
<IonList>
    <IonItem>
        <IonSelect Label="Toppings" Placeholder="Select toppings" Multiple="true" @bind-Values="_toppings">
            <IonSelectOption Value="bacon">Bacon</IonSelectOption>
            <IonSelectOption Value="olives">Black Olives</IonSelectOption>
            <IonSelectOption Value="mushrooms">Mushrooms</IonSelectOption>
            <IonSelectOption Value="pineapple" Disabled="true">Pineapple</IonSelectOption>
        </IonSelect>
    </IonItem>
</IonList>

<p>Selected: @string.Join(", ", _toppings)</p>

@code {
    private IReadOnlyList<string> _toppings = new[] { "bacon" };
}
```
<!-- /miko:example -->

## Label placement

`LabelPlacement` positions `Label`: `start` (default), `end`, `fixed` (a 100px column), `stacked` (always above the value), or `floating` (inside the field until it has a value or is open, then above it). `Justify` (`start`, `end`, `space-between`) packs label and value on one line and is ignored for `stacked` / `floating`. For a label with markup, use the `LabelSlot` fragment instead of `Label`.

<!-- miko:example examples/select/LabelPlacement.razor -->
```razor
<IonList>
    <IonItem>
        <IonSelect Label="Start" Placeholder="Pick one">
            <IonSelectOption Value="a">Option A</IonSelectOption>
            <IonSelectOption Value="b">Option B</IonSelectOption>
        </IonSelect>
    </IonItem>
    <IonItem>
        <IonSelect Label="Fixed" LabelPlacement="fixed" Placeholder="Pick one">
            <IonSelectOption Value="a">Option A</IonSelectOption>
            <IonSelectOption Value="b">Option B</IonSelectOption>
        </IonSelect>
    </IonItem>
    <IonItem>
        <IonSelect Label="Stacked" LabelPlacement="stacked" Placeholder="Pick one">
            <IonSelectOption Value="a">Option A</IonSelectOption>
            <IonSelectOption Value="b">Option B</IonSelectOption>
        </IonSelect>
    </IonItem>
    <IonItem>
        <IonSelect Label="Floating" LabelPlacement="floating" Placeholder="Pick one">
            <IonSelectOption Value="a">Option A</IonSelectOption>
            <IonSelectOption Value="b">Option B</IonSelectOption>
        </IonSelect>
    </IonItem>
    <IonItem>
        <IonSelect Label="Space between" Justify="space-between" Placeholder="Pick one">
            <IonSelectOption Value="a">Option A</IonSelectOption>
            <IonSelectOption Value="b">Option B</IonSelectOption>
        </IonSelect>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Fill, shape, and helper text

`Fill="solid"` gives the value area a background and `Fill="outline"` a border around the field (the outline is drawn in md mode only; in ios mode it renders no border). Fills are meant for standalone selects, not selects inside an `IonItem`. `Shape="round"` rounds the solid background's corners (it does not change the outline's corners). `HelperText` and `ErrorText` render below the field; both show whenever they are set, so toggle `ErrorText` yourself when validating. In md mode a standalone select without `Fill="outline"` also draws a 2px highlight line under the field.

<!-- miko:example examples/select/Fill.razor -->
```razor
@* Standalone selects (not inside an IonItem). The outline is drawn in md mode only. *@
<IonSelect Label="Solid" LabelPlacement="floating" Fill="solid" Placeholder="Pick one"
           HelperText="Used for shipping">
    <IonSelectOption Value="standard">Standard</IonSelectOption>
    <IonSelectOption Value="express">Express</IonSelectOption>
</IonSelect>

<IonSelect Label="Outline" LabelPlacement="floating" Fill="outline" Placeholder="Pick one"
           ErrorText="A shipping method is required">
    <IonSelectOption Value="standard">Standard</IonSelectOption>
    <IonSelectOption Value="express">Express</IonSelectOption>
</IonSelect>

<IonSelect Label="Round" Fill="solid" Shape="round" Placeholder="Pick one">
    <IonSelectOption Value="standard">Standard</IonSelectOption>
    <IonSelectOption Value="express">Express</IonSelectOption>
</IonSelect>
```
<!-- /miko:example -->

## Events

`ValueChanged` / `ValuesChanged` carry the new selection and are what `@bind-*` uses. `OnChange` fires after them with a `Miko.Events.ChangeEventArgs` whose `NewValue` is the selected value (comma-joined when `Multiple`); neither fires when the selection did not change. `OnCancel` fires when the user taps the overlay's Cancel / close button (not on a backdrop tap), and `OnDismiss` whenever the overlay closes.

<!-- miko:example examples/select/Events.razor -->
```razor
<IonList>
    <IonItem>
        <IonSelect Label="Size" Placeholder="Select a size" Value="@_size"
                   ValueChanged="@((string? v) => _size = v)"
                   OnChange="HandleChange" OnCancel="@(() => _log = "Cancelled")">
            <IonSelectOption Value="s">Small</IonSelectOption>
            <IonSelectOption Value="m">Medium</IonSelectOption>
            <IonSelectOption Value="l">Large</IonSelectOption>
        </IonSelect>
    </IonItem>
</IonList>

<p>@_log</p>

@code {
    private string? _size;
    private string _log = "No change yet";

    // NewValue is the selected value (comma-joined for a multi-value select).
    private void HandleChange(Miko.Events.ChangeEventArgs e) => _log = $"Changed to {e.NewValue}";
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonSelect IonSelectOption -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonSelect

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `CancelText` | `string` | `"Cancel"` | The text on the cancel button (alert / action-sheet / modal). |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, .... |
| `Disabled` | `bool` | `false` | When true, the user cannot interact with the select. |
| `ErrorText` | `string?` | `null` | Error text displayed below the control. |
| `ExpandedIcon` | `string?` | `null` | Toggle icon shown while the select is open (defaults to [`ToggleIcon`](#ionselect)). When set, the host also gets the `has-expanded-icon` class. |
| `Fill` | `string?` | `null` | Fill marker: solid or outline (outline only styled in md). |
| `HelperText` | `string?` | `null` | Helper text displayed below the control. |
| `Interface` | `string` | `"alert"` | Which overlay the select opens: `"alert"` (default), `"action-sheet"`, `"popover"`, or `"modal"`. |
| `InterfaceHeader` | `string?` | `null` | Header text passed to the overlay (Ionic's `interfaceOptions.header`). The alert interface falls back to the label text when unset, mirroring `openAlert()`. |
| `InterfaceMessage` | `string?` | `null` | Message text passed to the alert / popover overlay. |
| `InterfaceSubHeader` | `string?` | `null` | Sub-header text passed to the overlay. |
| `Justify` | `string?` | `null` | Justify marker: start, end, or space-between. Ignored for floating/stacked labels, matching Ionic's `justifyEnabled`. |
| `Label` | `string?` | `null` | Visible label text associated with the select. Takes priority over [`LabelSlot`](#ionselect), matching Ionic's label prop-over-slot rule. |
| `LabelPlacement` | `string` | `"start"` | Label placement: start, end, floating, stacked, or fixed. |
| `Multiple` | `bool` | `false` | When true, the select accepts multiple values. |
| `Name` | `string?` | `null` | Form field name marker. |
| `OkText` | `string` | `"OK"` | The text on the alert's confirm button. |
| `Placeholder` | `string?` | `null` | Placeholder text shown when no value is selected. |
| `Required` | `bool` | `false` | Accessibility marker: announces the field as required. Does not block submission, matching Ionic. |
| `SelectedText` | `string?` | `null` | Optional selected-text override, shown instead of the selected options' text. |
| `Shape` | `string?` | `null` | Shape (Ionic `shape`): `"round"` gives the `solid` fill pill-shaped corners. It does not change the corners of the md outline. |
| `ToggleIcon` | `string?` | `null` | Toggle icon when closed. Defaults to chevron-expand on iOS and caret-down-sharp on md. |
| `Value` | `string?` | `null` | The selected value. With [`Multiple`](#ionselect) use [`Values`](#ionselect) instead. Two-way bindable: `@bind-Value`. |
| `Values` | `IReadOnlyList<string>?` | `null` | The selected values when [`Multiple`](#ionselect) is set. Two-way bindable: `@bind-Values`. |

| Event | Type | Description |
| --- | --- | --- |
| `OnBlur` | `EventCallback` | Raised when the select loses focus (Ionic's `ionBlur`). |
| `OnCancel` | `EventCallback` | Raised when the selection is cancelled (Ionic's `ionCancel`). |
| `OnChange` | `EventCallback<ChangeEventArgs>` | Raised when the value has changed, mirroring Ionic's `ionChange`. |
| `OnDismiss` | `EventCallback` | Raised when the overlay is dismissed (Ionic's `ionDismiss`). |
| `OnFocus` | `EventCallback` | Raised when the select gains focus (Ionic's `ionFocus`). |
| `ValueChanged` | `EventCallback<string?>` | Raised when the selected value changes (single-value selects). |
| `ValuesChanged` | `EventCallback<IReadOnlyList<string>>` | Raised when the selected values change (multi-value selects). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
| `EndSlot` | `RenderFragment?` | Content shown at the end side of the select (Ionic's `end` slot). |
| `LabelSlot` | `RenderFragment?` | Custom label content (Ionic's `label` slot), used when [`Label`](#ionselect) is not set. |
| `StartSlot` | `RenderFragment?` | Content shown at the start side of the select (Ionic's `start` slot). |

### IonSelectOption

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | If true, the user cannot select this option. Ionic notes this does not apply to `interface="action-sheet"`, since action-sheet buttons cannot be disabled. |
| `Value` | `string?` | `null` | The option's value. When null, Ionic falls back to the option's text content (`getOptionValue()`), so an option is always identifiable. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Select -->
Theme token `IonicTheme.Select` (`SelectToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Select = new SelectToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `transparent` | `transparent` |
| `BorderColor` | `Color` | `rgba(0, 0, 0, 0.24)` | `rgba(0, 0, 0, 0.2)` |
| `BorderRadius` | `float` | `4` | `10` |
| `ErrorColor` | `Color` | `#c5000f` | `#c5000f` |
| `FontSize` | `float` | `16` | `17` |
| `HelperColor` | `Color` | `#666666` | `#666666` |
| `HighlightColor` | `Color` | `#0054e9` | `#0054e9` |
| `LabelColor` | `Color` | `#666666` | `#666666` |
| `MinHeight` | `float` | `48` | `44` |
| `PaddingBottom` | `float` | `0` | `0` |
| `PaddingEnd` | `float` | `0` | `0` |
| `PaddingStart` | `float` | `0` | `0` |
| `PaddingTop` | `float` | `0` | `0` |
| `PlaceholderColor` | `Color` | `#666666` | `#666666` |
| `RoundBorderRadius` | `float` | `999` | `999` |
| `TextColor` | `Color` | `#000000` | `#000000` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-select` | The host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-select .select-wrapper` | The row holding the label and the value. |
| `.ion-select .label-text-wrapper`, `.ion-select .label-text` | The label. |
| `.ion-select .select-wrapper-inner` | The value area: start slot, text, end slot, icon. Carries the `solid` fill background. |
| `.ion-select .select-text`, `.ion-select .select-placeholder` | The displayed text; `.select-placeholder` while the placeholder is shown. |
| `.ion-select .select-icon` | The toggle icon. |
| `.ion-select .select-highlight` | The md underline (standalone selects without the outline fill). |
| `.ion-select .select-outline-container` | The md outline border (`Fill="outline"`). |
| `.ion-select .select-bottom`, `.helper-text`, `.error-text` | The helper / error text row. |
| `.select-label-placement-{placement}` | `LabelPlacement` (e.g. `.select-label-placement-stacked`). |
| `.label-floating` | A floating or stacked label is currently above the value. |
| `.select-fill-{fill}`, `.select-shape-round`, `.select-justify-{justify}` | `Fill`, `Shape`, `Justify` (e.g. `.select-fill-outline`). |
| `.has-value`, `.has-placeholder`, `.has-focus`, `.select-expanded`, `.select-disabled` | State: has selected text, has a placeholder, focused, overlay open, `Disabled`. |
| `.in-item` | The select sits inside an `IonItem`. |
| `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |
| `.select-alert`, `.select-popover`, `.select-modal` | The overlay opened by the alert, popover, and modal interfaces. |
| `.ion-select-popover`, `.ion-select-modal` | The option list inside the popover / modal. |
| `.select-interface-option` | One option inside any overlay; an option's own `Class` is added next to it. |

```csharp
// GlobalStyles.cs — a bolder selected value
[".ion-select .select-text"] = new()
{
    FontWeight = FontWeight.SemiBold,
},
```
