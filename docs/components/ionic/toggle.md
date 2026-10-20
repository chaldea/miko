# Toggle

Toggles are switches that turn a single option on or off. `IonToggle` draws a track with a sliding knob and takes its label as child content; a tap anywhere on the label or the switch flips it.

## Basic usage

The child content is the label; without it only the switch is drawn. The knob slides with a short transition. The switch responds to taps only — it cannot be dragged — and Ionic's on/off labels (`enableOnOffLabels`) are not implemented. `Disabled` blocks taps and dims the toggle.

<!-- miko:example examples/toggle/Basic.razor -->
```razor
<IonToggle>Default toggle</IonToggle>
<IonToggle Checked="true">Checked toggle</IonToggle>
<IonToggle Disabled="true">Disabled toggle</IonToggle>
<IonToggle Checked="true" Disabled="true">Disabled checked toggle</IonToggle>
```
<!-- /miko:example -->

## Binding the checked state

`@bind-Checked` keeps a `bool` in sync. On every tap `CheckedChanged` and then `IonChange` are raised with the new state — the change event is named `IonChange`, not `OnChange`. Neither is raised when `Checked` is changed from code. An unbound toggle still flips itself, but it is reset to the `Checked` value its parent passes whenever the parent re-renders, so bind `Checked` or update it from `IonChange`.

<!-- miko:example examples/toggle/Binding.razor -->
```razor
<IonList>
    <IonItem>
        <IonToggle @bind-Checked="_wifi">Wi-Fi</IonToggle>
    </IonItem>
    <IonItem>
        <IonToggle Checked="@_bluetooth" IonChange="SetBluetooth">Bluetooth</IonToggle>
    </IonItem>
</IonList>
<p>Wi-Fi is @(_wifi ? "on" : "off"), Bluetooth is @(_bluetooth ? "on" : "off").</p>

@code {
    private bool _wifi = true;
    private bool _bluetooth;

    // IonChange carries the new state; it is raised only by a user tap.
    private void SetBluetooth(bool on) => _bluetooth = on;
}
```
<!-- /miko:example -->

## Label placement

`LabelPlacement` positions the label: `start` (default, before the switch), `end` (after the switch), `fixed` (before the switch, 100px wide), or `stacked` (above the switch). The label is a single line and is clipped when it does not fit.

<!-- miko:example examples/toggle/LabelPlacement.razor -->
```razor
<IonToggle LabelPlacement="start">Label at the start</IonToggle>
<IonToggle LabelPlacement="end">Label at the end</IonToggle>
<IonToggle LabelPlacement="fixed">Fixed-width label</IonToggle>
<IonToggle LabelPlacement="stacked">Stacked label</IonToggle>
```
<!-- /miko:example -->

## Justify and alignment

`Justify` packs the label and switch along the line: `start`, `end`, or `space-between`. `Alignment` aligns them on the cross axis: `start` or `center`. Setting either turns the inline-block host into a block, so it spans its container. Inside an [`IonItem`](./item.md) the toggle always stretches to fill the row; without `Justify` the label then sits at the start and the switch at the far end.

With `LabelPlacement="end"` on a full-width toggle (in an item, or with `Justify`), the switch and label are packed at the start; an explicit `Justify` still applies.

<!-- miko:example examples/toggle/Justify.razor -->
```razor
@* Inside an IonItem the toggle fills the row, so Justify has room to work. *@
<IonList>
    <IonItem>
        <IonToggle Justify="start">Packed at the start</IonToggle>
    </IonItem>
    <IonItem>
        <IonToggle Justify="end">Packed at the end</IonToggle>
    </IonItem>
    <IonItem>
        <IonToggle Justify="space-between">Space between</IonToggle>
    </IonItem>
    <IonItem>
        <IonToggle LabelPlacement="stacked" Alignment="start">Stacked, aligned to the start</IonToggle>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Helper and error text

`HelperText` and `ErrorText` render under the label. There is no validity tracking, so both are shown whenever they are set — decide in your code which one to show.

<!-- miko:example examples/toggle/HelperError.razor -->
```razor
@* Both texts render when both are set, so show one or the other. *@
<IonToggle @bind-Checked="_backup"
           HelperText="@(_backup ? "Photos are backed up nightly" : null)"
           ErrorText="@(_backup ? null : "Turn on backup to keep your photos safe")">
    Cloud backup
</IonToggle>

@code {
    private bool _backup = true;
}
```
<!-- /miko:example -->

## Colors

`Color` takes a palette name and tints the checked track (at half opacity in md mode, where the knob takes the solid color; solid in ios mode, where the knob stays white). See [Theming](./theming.md#palette).

<!-- miko:example examples/toggle/Colors.razor -->
```razor
<IonToggle Checked="true" Color="primary">Primary</IonToggle>
<IonToggle Checked="true" Color="secondary">Secondary</IonToggle>
<IonToggle Checked="true" Color="success">Success</IonToggle>
<IonToggle Checked="true" Color="warning">Warning</IonToggle>
<IonToggle Checked="true" Color="danger">Danger</IonToggle>
<IonToggle Checked="true" Color="dark">Dark</IonToggle>
```
<!-- /miko:example -->

## API

<!-- miko:api IonToggle -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonToggle

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Alignment` | `string?` | `null` | Cross-axis alignment of the label and switch: `"start"` or `"center"`. Setting this changes the host `display` to block. |
| `Checked` | `bool` | `false` | If true, the toggle is selected (on). Two-way bindable: `@bind-Checked`. |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class the stylesheet maps to the checked track fill. |
| `Disabled` | `bool` | `false` | If true, the user cannot interact with the toggle. |
| `ErrorText` | `string?` | `null` | Error text displayed below the label whenever it is set (Miko does not track validity). |
| `HelperText` | `string?` | `null` | Helper text displayed below the label. Shown even when [`ErrorText`](#iontoggle) is also set. |
| `Justify` | `string?` | `null` | How to pack the label and switch within a line: `"start"`, `"end"`, or `"space-between"`. Setting this changes the host `display` to block. |
| `LabelPlacement` | `string` | `"start"` | Where the label sits relative to the switch: `"start"` (default), `"end"`, `"fixed"`, or `"stacked"`. |
| `Name` | `string?` | `null` | Form field name marker. |
| `Value` | `string` | `"on"` | Ionic `value` (the form value when checked). Kept for API parity; Miko has no form submission. |

| Event | Type | Description |
| --- | --- | --- |
| `CheckedChanged` | `EventCallback<bool>` | Raised (with the new checked state) when the user toggles the switch. |
| `IonChange` | `EventCallback<bool>` | Raised when the checked state changes as a result of a user action (Ionic `ionChange`). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Toggle -->
Theme token `IonicTheme.Toggle` (`ToggleToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Toggle = new ToggleToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `BorderRadius` | `Length` | `14px` | `15.5px` |
| `DisabledOpacity` | `float` | `0.38` | `0.3` |
| `HandleBackground` | `Color` | `#ffffff` | `#ffffff` |
| `HandleBackgroundChecked` | `Color` | `#0054e9` | `#ffffff` |
| `HandleBorderRadius` | `Length` | `10px` | `25.5px` |
| `HandleBoxShadow` | `List<BoxShadow>` | `0 3px 1px -2px rgba(0, 0, 0, 0.2), 0 2px 2px 0 rgba(0, 0, 0, 0.14), 0 1px 5px 0 rgba(0, 0, 0, 0.12)` | `0 3px 4px 0 rgba(0, 0, 0, 0.06), 0 3px 8px 0 rgba(0, 0, 0, 0.06)` |
| `HandleHeight` | `float` | `20` | `27` |
| `HandleSpacing` | `float` | `0` | `2` |
| `HandleTravel` | `float` | `16` | `24` |
| `HandleWidth` | `float` | `20` | `27` |
| `TrackBackgroundOff` | `Color` | `rgba(0, 0, 0, 0.39)` | `rgba(0, 0, 0, 0.09)` |
| `TrackBackgroundOn` | `Color` | `rgba(0, 84, 233, 0.5)` | `#0054e9` |
| `TrackCheckedAlpha` | `float` | `0.5` | `1` |
| `TrackHeight` | `float` | `14` | `31` |
| `TrackWidth` | `float` | `36` | `51` |
| `TransitionDuration` | `float` | `0.16` | `0.3` |
<!-- /miko:tokens -->

The label's font size and the helper / error text colors come from `IonicTheme.Select` (`FontSize`, `HelperColor`, `ErrorColor`; see [Select](./select.md)), and the label color from `IonicTheme.Item.Color` (see [Item](./item.md)).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-toggle` | The host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-toggle .toggle-wrapper` | The clickable row holding the label and the switch. |
| `.ion-toggle .label-text-wrapper` | The label (also `.label-text-wrapper-hidden` when there is none). |
| `.ion-toggle .native-wrapper` | The wrapper around the switch. |
| `.ion-toggle .toggle-icon` | The track. |
| `.ion-toggle .toggle-icon-wrapper` | The full-width carrier that slides (a `Transform`) when checked. |
| `.ion-toggle .toggle-inner` | The knob. |
| `.ion-toggle .toggle-bottom` | The row under the label, holding `.helper-text` and `.error-text`. |
| `.toggle-label-placement-start`, `-end`, `-fixed`, `-stacked` | `LabelPlacement`. |
| `.toggle-justify-start`, `-end`, `-space-between`, `.toggle-alignment-start`, `-center` | `Justify`, `Alignment`. |
| `.toggle-checked`, `.toggle-disabled` | `Checked`, `Disabled`. |
| `.in-item` | The toggle sits in an `IonItem`. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-success`). |

```csharp
// GlobalStyles.cs — a brand color for every checked toggle.
[".ion-toggle.toggle-checked .toggle-icon"] = new()
{
    BackgroundColor = Color.FromHex("#0e7490"),
},
```
