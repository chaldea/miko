# Checkbox

Checkboxes let the user select any number of options from a set, or switch a single option on or off. `IonCheckbox` draws the box — a rounded square in md mode, a circle in ios mode — and takes its label as child content; a tap anywhere on the label or the box toggles it.

## Basic usage

The child content is the label; without it only the box is drawn. `Disabled` blocks taps and dims the checkbox.

<!-- miko:example examples/checkbox/Basic.razor -->
```razor
<IonCheckbox>I agree to the terms and conditions</IonCheckbox>
<IonCheckbox Checked="true">Checked</IonCheckbox>
<IonCheckbox Disabled="true">Disabled</IonCheckbox>
```
<!-- /miko:example -->

## Binding the checked state

`@bind-Checked` keeps a `bool` in sync. On every tap `CheckedChanged` and then `IonChange` are raised with the new state — note the change event is named `IonChange`, not `OnChange`. Neither is raised when `Checked` is changed from code. An unbound checkbox still toggles itself, but it is reset to the `Checked` value its parent passes whenever the parent re-renders, so bind `Checked` or update it from `IonChange`.

<!-- miko:example examples/checkbox/Binding.razor -->
```razor
<IonCheckbox @bind-Checked="_subscribe">Subscribe to the newsletter</IonCheckbox>
<IonButton Disabled="@(!_subscribe)">Continue</IonButton>

@code {
    private bool _subscribe;
}
```
<!-- /miko:example -->

## Label placement

`LabelPlacement` positions the label: `start` (default, before the box), `end` (after the box), `fixed` (before the box, 100px wide), or `stacked` (above the box). The label is a single line, truncated with an ellipsis when it does not fit.

<!-- miko:example examples/checkbox/LabelPlacement.razor -->
```razor
<IonCheckbox LabelPlacement="start">Label at the start</IonCheckbox>
<IonCheckbox LabelPlacement="end">Label at the end</IonCheckbox>
<IonCheckbox LabelPlacement="fixed">Fixed-width label</IonCheckbox>
<IonCheckbox LabelPlacement="stacked">Stacked label</IonCheckbox>
```
<!-- /miko:example -->

## Justify and alignment

`Justify` packs the label and box along the line: `start`, `end`, or `space-between`. `Alignment` aligns them on the cross axis: `start` or `center`. Setting either turns the inline-block host into a block, so it spans its container. Inside an [`IonItem`](./item.md) the checkbox always stretches to fill the row; without `Justify` the label then sits at the start and the box at the far end (except with `LabelPlacement="end"`, which packs both at the start).

<!-- miko:example examples/checkbox/Justify.razor -->
```razor
@* Inside an IonItem the checkbox fills the row, so Justify has room to work. *@
<IonList>
    <IonItem>
        <IonCheckbox Justify="start">Packed at the start</IonCheckbox>
    </IonItem>
    <IonItem>
        <IonCheckbox Justify="end">Packed at the end</IonCheckbox>
    </IonItem>
    <IonItem>
        <IonCheckbox Justify="space-between">Space between</IonCheckbox>
    </IonItem>
    <IonItem>
        <IonCheckbox LabelPlacement="end" Justify="start">Box first, packed at the start</IonCheckbox>
    </IonItem>
    <IonItem>
        <IonCheckbox LabelPlacement="stacked" Alignment="start">Stacked, aligned to the start</IonCheckbox>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Indeterminate

`Indeterminate` shows a minus mark instead of the checkmark, typically on a "select all" box whose options are only partly selected. A tap clears it and raises `IndeterminateChanged(false)`, so it can be bound with `@bind-Indeterminate`; for a "select all" box it is usually simpler to derive it from your data, as below.

<!-- miko:example examples/checkbox/Indeterminate.razor -->
```razor
<IonList>
    <IonItem>
        <IonCheckbox Checked="@AllChecked" Indeterminate="@SomeChecked" IonChange="CheckAll">
            Select all
        </IonCheckbox>
    </IonItem>
    @foreach (var topping in _toppings)
    {
        <IonItem>
            <IonCheckbox @bind-Checked="topping.Checked">@topping.Name</IonCheckbox>
        </IonItem>
    }
</IonList>

@code {
    private sealed class Topping
    {
        public required string Name { get; init; }
        public bool Checked { get; set; }
    }

    private readonly List<Topping> _toppings =
    [
        new() { Name = "Mushrooms", Checked = true },
        new() { Name = "Olives" },
        new() { Name = "Peppers" },
    ];

    private bool AllChecked => _toppings.All(t => t.Checked);
    private bool SomeChecked => !AllChecked && _toppings.Any(t => t.Checked);

    // Indeterminate is derived state: the app decides it, the checkbox only draws it.
    private void CheckAll(bool isChecked)
    {
        foreach (var topping in _toppings)
            topping.Checked = isChecked;
    }
}
```
<!-- /miko:example -->

## Helper and error text

`HelperText` and `ErrorText` render under the label. There is no validity tracking, so both are shown whenever they are set — decide in your code which one to show.

<!-- miko:example examples/checkbox/HelperError.razor -->
```razor
@* Both texts render when both are set, so show one or the other. *@
<IonCheckbox @bind-Checked="_accepted"
             HelperText="@(Error is null ? "Required to create an account" : null)"
             ErrorText="@Error">
    I agree to the terms
</IonCheckbox>
<IonButton OnClick="() => _submitted = true">Create account</IonButton>

@code {
    private bool _accepted;
    private bool _submitted;

    private string? Error => _submitted && !_accepted ? "You must accept the terms" : null;
}
```
<!-- /miko:example -->

## Colors

`Color` takes a palette name and tints the checked (and indeterminate) box; the mark uses the color's contrast. See [Theming](./theming.md#palette).

<!-- miko:example examples/checkbox/Colors.razor -->
```razor
<IonCheckbox Checked="true" Color="primary">Primary</IonCheckbox>
<IonCheckbox Checked="true" Color="secondary">Secondary</IonCheckbox>
<IonCheckbox Checked="true" Color="success">Success</IonCheckbox>
<IonCheckbox Checked="true" Color="warning">Warning</IonCheckbox>
<IonCheckbox Checked="true" Color="danger">Danger</IonCheckbox>
<IonCheckbox Checked="true" Color="dark">Dark</IonCheckbox>
```
<!-- /miko:example -->

## API

<!-- miko:api IonCheckbox -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonCheckbox

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Alignment` | `string?` | `null` | Cross-axis alignment of the label and box: `"start"` or `"center"`. Setting this changes the host `display` to block. |
| `Checked` | `bool` | `false` | If true, the checkbox is selected. Two-way bindable: `@bind-Checked`. |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class the stylesheet maps to the checked fill / border. |
| `Disabled` | `bool` | `false` | If true, the user cannot interact with the checkbox. |
| `ErrorText` | `string?` | `null` | Error text displayed below the label whenever it is set (Miko does not track validity). |
| `HelperText` | `string?` | `null` | Helper text displayed below the label. Shown even when [`ErrorText`](#ioncheckbox) is also set. |
| `Indeterminate` | `bool` | `false` | If true, the checkbox visually appears as indeterminate (a minus mark). A tap clears it and raises [`IndeterminateChanged`](#ioncheckbox); bind it with `@bind-Indeterminate`, or the parent's next render restores the minus mark from the value it still passes in. |
| `Justify` | `string?` | `null` | How to pack the label and box within a line: `"start"`, `"end"`, or `"space-between"`. Setting this changes the host `display` to block. |
| `LabelPlacement` | `string` | `"start"` | Where the label sits relative to the box: `"start"` (default), `"end"`, `"fixed"`, or `"stacked"`. |
| `Name` | `string?` | `null` | Form field name marker. |
| `Value` | `string` | `"on"` | Ionic `value` (the form value when checked). Kept for API parity; Miko has no form submission. |

| Event | Type | Description |
| --- | --- | --- |
| `CheckedChanged` | `EventCallback<bool>` | Raised (with the new checked state) when the user toggles the checkbox. |
| `IndeterminateChanged` | `EventCallback<bool>` | Raised with `false` when a tap clears [`Indeterminate`](#ioncheckbox), before [`CheckedChanged`](#ioncheckbox). Enables `@bind-Indeterminate`. Ionic clears the property silently; it has no event for this. |
| `IonChange` | `EventCallback<bool>` | Raised when the checked state changes as a result of a user action (Ionic `ionChange`). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Checkbox -->
Theme token `IonicTheme.Checkbox` (`CheckboxToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Checkbox = new CheckboxToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `BackgroundChecked` | `Color` | `#0054e9` | `#0054e9` |
| `BackgroundOff` | `Color` | `#ffffff` | `#ffffff` |
| `BorderColorOff` | `Color` | `rgba(0, 0, 0, 0.6)` | `rgba(0, 0, 0, 0.23)` |
| `BorderRadius` | `Length` | `2.25px` | `11px` |
| `BorderWidth` | `float` | `2` | `2` |
| `CheckmarkColor` | `Color` | `#ffffff` | `#ffffff` |
| `DisabledOpacity` | `float` | `0.38` | `0.3` |
| `Size` | `float` | `18` | `22` |
<!-- /miko:tokens -->

[`IonRadio`](./radio.md) reuses `BorderColorOff` (its unchecked ring) and `BackgroundChecked` (its checked color).

The label's font size and the helper / error text colors come from `IonicTheme.Select` (`FontSize`, `HelperColor`, `ErrorColor`; see [Select](./select.md)), and the label color from `IonicTheme.Item.Color` (see [Item](./item.md)).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-checkbox` | The host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-checkbox .checkbox-wrapper` | The clickable row holding the label and the box. |
| `.ion-checkbox .label-text-wrapper` | The label (also `.label-text-wrapper-hidden` when there is none). |
| `.ion-checkbox .native-wrapper` | The wrapper around the box. |
| `.ion-checkbox .checkbox-icon` | The box: border, background, radius, size. |
| `.ion-checkbox .checkbox-icon-mark` | The checkmark / minus glyph, a tinted image whose `Color` is the tint. |
| `.ion-checkbox .checkbox-bottom` | The row under the label, holding `.helper-text` and `.error-text`. |
| `.checkbox-label-placement-start`, `-end`, `-fixed`, `-stacked` | `LabelPlacement`. |
| `.checkbox-justify-start`, `-end`, `-space-between`, `.checkbox-alignment-start`, `-center` | `Justify`, `Alignment`. |
| `.checkbox-checked`, `.checkbox-indeterminate`, `.checkbox-disabled` | `Checked`, `Indeterminate`, `Disabled`. |
| `.in-item` | The checkbox sits in an `IonItem`. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |

```csharp
// GlobalStyles.cs — square boxes in both modes: <IonCheckbox Class="square">…</IonCheckbox>
[".ion-checkbox.square .checkbox-icon"] = new()
{
    BorderRadius = new BorderRadius(4),
},
```
