# Radio

Radios let the user pick exactly one option from a set. `IonRadioGroup` owns the selected value and `IonRadio` is one option inside it, identified by its `Value`.

A radio needs an enclosing `IonRadioGroup`: it reads the selection from the group, so on its own it is never checked and tapping it does nothing. In md mode a radio is a ring with a dot when checked; in ios mode it shows only a checkmark, and only when checked.

## Basic usage

Bind the group's `Value` with `@bind-Value`; the radio whose `Value` matches is checked. Tapping an unchecked radio selects it. Values are strings (Ionic's non-string values and `compareWith` are not supported). An unbound group still updates its own selection, but it is reset to the `Value` its parent passes whenever the parent re-renders.

<!-- miko:example examples/radio/Basic.razor -->
```razor
<IonRadioGroup @bind-Value="_fruit">
    <IonRadio Value="apple">Apple</IonRadio>
    <IonRadio Value="grape">Grape</IonRadio>
    <IonRadio Value="cherry">Cherry</IonRadio>
</IonRadioGroup>
<p>Selected: @_fruit</p>

@code {
    private string? _fruit = "grape";
}
```
<!-- /miko:example -->

## Radios in a list

The group can wrap any markup — list headers, items, other components — and every `IonRadio` below it joins the group. Inside an [`IonItem`](./item.md) a radio stretches to fill the row, with the label at the start and the radio at the far end.

<!-- miko:example examples/radio/InList.razor -->
```razor
@* The group can wrap any markup; every IonRadio below it joins the group. *@
<IonList>
    <IonRadioGroup @bind-Value="_shipping">
        <IonListHeader>
            <IonLabel>Shipping</IonLabel>
        </IonListHeader>
        <IonItem>
            <IonRadio Value="standard">Standard (3-5 days)</IonRadio>
        </IonItem>
        <IonItem>
            <IonRadio Value="express">Express (1-2 days)</IonRadio>
        </IonItem>
        <IonItem>
            <IonRadio Value="pickup">Store pickup</IonRadio>
        </IonItem>
    </IonRadioGroup>
</IonList>

@code {
    private string? _shipping = "standard";
}
```
<!-- /miko:example -->

## Reacting to changes

`ValueChanged` is the group's only change notification (there is no `ionChange` equivalent); it is raised when a tap changes the selection, not when `Value` is changed from code. Pair `Value` with a `ValueChanged` handler when you need to act on the change.

<!-- miko:example examples/radio/Changed.razor -->
```razor
<IonRadioGroup Value="@_theme" ValueChanged="ApplyTheme">
    <IonRadio Value="light">Light</IonRadio>
    <IonRadio Value="dark">Dark</IonRadio>
    <IonRadio Value="system">System</IonRadio>
</IonRadioGroup>
<p>@_message</p>

@code {
    private string? _theme = "system";
    private string _message = string.Empty;

    // ValueChanged is the group's only change notification.
    private void ApplyTheme(string? theme)
    {
        _theme = theme;
        _message = $"Theme set to {theme}";
    }
}
```
<!-- /miko:example -->

## Allowing an empty selection

By default tapping the checked radio does nothing. With `AllowEmptySelection` it clears the selection and `ValueChanged` is raised with `null`.

<!-- miko:example examples/radio/AllowEmpty.razor -->
```razor
@* Tapping the checked radio again clears the selection (Value becomes null). *@
<IonRadioGroup AllowEmptySelection="true" @bind-Value="_topping">
    <IonRadio Value="cheese">Extra cheese</IonRadio>
    <IonRadio Value="basil">Basil</IonRadio>
</IonRadioGroup>
<p>Topping: @(_topping ?? "none")</p>

@code {
    private string? _topping = "cheese";
}
```
<!-- /miko:example -->

## Label placement

`LabelPlacement` positions each radio's label: `start` (default, before the radio), `end` (after it), `fixed` (before it, 100px wide), or `stacked` (above it). The label is a single line, truncated with an ellipsis when it does not fit.

<!-- miko:example examples/radio/LabelPlacement.razor -->
```razor
<IonRadioGroup Value="start">
    <IonRadio Value="start" LabelPlacement="start">Label at the start</IonRadio>
    <IonRadio Value="end" LabelPlacement="end">Label at the end</IonRadio>
    <IonRadio Value="fixed" LabelPlacement="fixed">Fixed-width label</IonRadio>
    <IonRadio Value="stacked" LabelPlacement="stacked">Stacked label</IonRadio>
</IonRadioGroup>
```
<!-- /miko:example -->

## Justify and alignment

`Justify` packs the label and radio along the line: `start`, `end`, or `space-between`. `Alignment` aligns them on the cross axis: `start` or `center`. Setting either turns the inline-block host into a block. In a full-width radio (for example in an item), `LabelPlacement="end"` alone puts the radio at the start and the label at the far end; add `Justify="start"` to keep the label next to the radio.

<!-- miko:example examples/radio/Justify.razor -->
```razor
@* Inside an IonItem the radio fills the row, so Justify has room to work. *@
<IonList>
    <IonRadioGroup Value="start">
        <IonItem>
            <IonRadio Value="start" Justify="start">Packed at the start</IonRadio>
        </IonItem>
        <IonItem>
            <IonRadio Value="end" Justify="end">Packed at the end</IonRadio>
        </IonItem>
        <IonItem>
            <IonRadio Value="between" Justify="space-between">Space between</IonRadio>
        </IonItem>
        <IonItem>
            <IonRadio Value="box-first" LabelPlacement="end" Justify="start">Radio first, packed at the start</IonRadio>
        </IonItem>
    </IonRadioGroup>
</IonList>
```
<!-- /miko:example -->

## Disabled

`Disabled` on an `IonRadio` blocks that option; `Disabled` on the group blocks all of its radios. Disabled radios are dimmed.

<!-- miko:example examples/radio/Disabled.razor -->
```razor
@* One disabled option. *@
<IonRadioGroup Value="card">
    <IonRadio Value="card">Card</IonRadio>
    <IonRadio Value="paypal" Disabled="true">PayPal (unavailable)</IonRadio>
</IonRadioGroup>

@* The whole group. *@
<IonRadioGroup Value="card" Disabled="true">
    <IonRadio Value="card">Card</IonRadio>
    <IonRadio Value="paypal">PayPal</IonRadio>
</IonRadioGroup>
```
<!-- /miko:example -->

## Colors

`Color` takes a palette name and tints the radio's checked state: the ring and dot in md mode, the checkmark in ios mode. See [Theming](./theming.md#palette).

<!-- miko:example examples/radio/Colors.razor -->
```razor
@* The color shows on the checked radio: tap the others to see theirs. *@
<IonRadioGroup @bind-Value="_status">
    <IonRadio Value="ok" Color="success">Healthy</IonRadio>
    <IonRadio Value="warn" Color="warning">Degraded</IonRadio>
    <IonRadio Value="down" Color="danger">Down</IonRadio>
</IonRadioGroup>

@code {
    private string? _status = "ok";
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonRadioGroup IonRadio -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonRadioGroup

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `AllowEmptySelection` | `bool` | `false` | If true, a checked radio can be tapped again to clear the selection. |
| `Disabled` | `bool` | `false` | If true, the radios in the group are non-interactive. |
| `Value` | `string?` | `null` | The selected radio value. Bind two-way with [`ValueChanged`](#ionradiogroup). Two-way bindable: `@bind-Value`. |

| Event | Type | Description |
| --- | --- | --- |
| `ValueChanged` | `EventCallback<string?>` | Raised with the new selected value when it changes (null when cleared). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonRadio

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Alignment` | `string?` | `null` | Cross-axis alignment of the label and radio: `"start"` or `"center"`. Setting this changes the host `display` to block. |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class the stylesheet maps to the checked ring / dot. |
| `Disabled` | `bool` | `false` | If true, the user cannot interact with the radio. |
| `Justify` | `string?` | `null` | How to pack the label and radio within a line: `"start"`, `"end"`, or `"space-between"`. Setting this changes the host `display` to block. |
| `LabelPlacement` | `string` | `"start"` | Where the label sits relative to the radio: `"start"` (default), `"end"`, `"fixed"`, or `"stacked"`. |
| `Value` | `string` | `""` | This radio's value — its identity within a group. Required: the group's selected value is compared against this to derive the checked state. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

Radios have no token of their own: they reuse `IonicTheme.Checkbox` — `BorderColorOff` for the md ring and `BackgroundChecked` for the checked color (see [Checkbox](./checkbox.md#theme-tokens)). The label's font size comes from `IonicTheme.Select.FontSize` (see [Select](./select.md)) and its color from `IonicTheme.Item.Color` (see [Item](./item.md)).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-radio-group` | The `IonRadioGroup` container. |
| `.ion-radio` | A radio's host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-radio .radio-wrapper` | The clickable row holding the label and the radio. |
| `.ion-radio .label-text-wrapper` | The label (also `.label-text-wrapper-hidden` when there is none). |
| `.ion-radio .native-wrapper` | The wrapper around the radio. |
| `.ion-radio .radio-icon` | The md ring; in ios mode the box holding the checkmark. |
| `.ion-radio .radio-inner` | The md dot; in ios mode the checkmark glyph (a tinted image whose `Color` is the tint). |
| `.radio-label-placement-start`, `-end`, `-fixed`, `-stacked` | `LabelPlacement`. |
| `.radio-justify-start`, `-end`, `-space-between`, `.radio-alignment-start`, `-center` | `Justify`, `Alignment`. |
| `.radio-checked`, `.radio-disabled` | The radio is selected; it (or its group) is disabled. |
| `.in-item` | The radio sits in an `IonItem`. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-success`). |

```csharp
// GlobalStyles.cs — a thicker md ring for radios with Class="bold".
[".ion-radio.md.bold .radio-icon"] = new()
{
    BorderWidth = Length.Px(3),
},
```
