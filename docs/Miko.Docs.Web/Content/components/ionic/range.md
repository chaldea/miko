# Range

Ranges are sliders for picking a number between a minimum and a maximum by dragging a knob along a bar. `IonRange` has a single knob, an optional value pin, a label, and start/end slots for icons.

Ionic's dual knobs (`dualKnobs`), tick marks (`ticks`), `pinFormatter`, and the `ionChange` / `ionKnobMoveStart` / `ionKnobMoveEnd` events are not implemented.

## Basic usage

`Value` is a `double` (default 0) between `Min` (default 0) and `Max` (default 100). `Disabled` blocks input; ios mode dims the whole range, md mode dims the label and turns the bars and knob grey (the knob also shrinks), as in Ionic.

<!-- miko:example examples/range/Basic.razor -->
```razor
<IonRange Label="Default" />
<IonRange Label="With value" Value="40" />
<IonRange Label="Disabled" Value="60" Disabled="true" />
```
<!-- /miko:example -->

## Binding the value

`@bind-Value` keeps a `double` in sync. Pressing the bar moves the knob to that point, and dragging moves it continuously — the drag keeps tracking outside the bar until the pointer is released. `ValueChanged` is raised on the press and on every drag step that changes the value; it is the only change event, so there is no separate notification on release.

<!-- miko:example examples/range/Binding.razor -->
```razor
<IonRange Label="Volume" @bind-Value="_volume" />
<p>Volume: @_volume</p>

@code {
    // ValueChanged fires on press and on every drag step that changes the value.
    private double _volume = 30;
}
```
<!-- /miko:example -->

## Min, max, and step

A value picked by press or drag is rounded to the nearest multiple of `Step` (default 1) counted from `Min`, then clamped to `Min`–`Max`. The knob always lands on a step, as with Ionic's `snaps`; tick marks are not drawn.

<!-- miko:example examples/range/MinMaxStep.razor -->
```razor
<IonRange Label="Temperature" Min="16" Max="30" Step="0.5" Pin="true" @bind-Value="_temperature" />
<IonRange Label="Price" Min="0" Max="1000" Step="50" Pin="true" @bind-Value="_price" />

@code {
    private double _temperature = 21.5;
    private double _price = 250;
}
```
<!-- /miko:example -->

## Pin

`Pin` shows the current value in a bubble above the knob (rounded to two decimals; whole numbers have none) and adds space above the bar for it.

<!-- miko:example examples/range/Pin.razor -->
```razor
<IonRange Label="Brush size" Pin="true" Value="33" />
```
<!-- /miko:example -->

## Start and end slots

`StartSlot` and `EndSlot` place content before and after the bar, typically icons that describe the two ends of the scale.

<!-- miko:example examples/range/Slots.razor -->
```razor
<IonRange @bind-Value="_brightness">
    <StartSlot>
        <IonIcon Icon="@Ionicons.SunnyOutline" Size="small" />
    </StartSlot>
    <EndSlot>
        <IonIcon Icon="@Ionicons.Sunny" />
    </EndSlot>
</IonRange>

@code {
    private double _brightness = 70;
}
```
<!-- /miko:example -->

## Label placement

`Label` sets a text label; for a label with markup use the `LabelSlot` slot, which is rendered only when `Label` is not set. `LabelPlacement` positions it: `start` (default, before the bar), `end` (after it), `fixed` (before it, 100px wide), or `stacked` (above it).

<!-- miko:example examples/range/LabelPlacement.razor -->
```razor
<IonRange Label="Start (default)" Value="20" />
<IonRange Label="End" LabelPlacement="end" Value="40" />
<IonRange Label="Fixed" LabelPlacement="fixed" Value="60" />
<IonRange Label="Stacked" LabelPlacement="stacked" Value="80" />

@* Rich label: LabelSlot is used when Label is not set. *@
<IonRange LabelPlacement="stacked" Value="50">
    <LabelSlot>
        Opacity <IonText Color="medium">(%)</IonText>
    </LabelSlot>
</IonRange>
```
<!-- /miko:example -->

## Ranges in a list

Inside an [`IonItem`](./item.md) the range fills the item's content area.

<!-- miko:example examples/range/InItem.razor -->
```razor
<IonList>
    <IonListHeader>
        <IonLabel>Adjust display</IonLabel>
    </IonListHeader>
    <IonItem>
        <IonRange Label="Brightness" @bind-Value="_brightness" />
    </IonItem>
    <IonItem>
        <IonRange Label="Contrast" Min="-100" Max="100" Pin="true" @bind-Value="_contrast" />
    </IonItem>
</IonList>

@code {
    private double _brightness = 20;
    private double _contrast;
}
```
<!-- /miko:example -->

## Colors

`Color` takes a palette name. In md mode it colors the bar, the knob, and the pin; in ios mode only the filled part of the bar (the knob stays white). See [Theming](./theming.md#palette).

<!-- miko:example examples/range/Colors.razor -->
```razor
<IonRange Color="secondary" Value="30" />
<IonRange Color="success" Value="50" />
<IonRange Color="danger" Value="70" Pin="true" />
```
<!-- /miko:example -->

## API

<!-- miko:api IonRange -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonRange

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`). Stamped as an `ion-color-*` class. |
| `Disabled` | `bool` | `false` | If true, the range ignores input. ios dims the whole range; md dims only the label. |
| `Label` | `string?` | `null` | Inline label text. When set, rendered as `.label-text`; otherwise [`LabelSlot`](#ionrange) (Ionic's `slot="label"`) is used. |
| `LabelPlacement` | `string` | `"start"` | Where the label sits relative to the slider: `"start"` (default), `"end"`, `"fixed"`, or `"stacked"`. |
| `Max` | `double` | `100` | Maximum value (default 100). |
| `Min` | `double` | `0` | Minimum value (default 0). |
| `Pin` | `bool` | `false` | If true, a pin (value bubble) is shown above the knob. |
| `Step` | `double` | `1` | The quantized step between allowed values (default 1). The knob always lands on a step, as with Ionic's `snaps` (no tick marks are drawn). |
| `Value` | `double` | `0` | The current value. Bind two-way with [`ValueChanged`](#ionrange). Two-way bindable: `@bind-Value`. |

| Event | Type | Description |
| --- | --- | --- |
| `ValueChanged` | `EventCallback<double>` | Raised with the new value when a press or drag on the slider changes it (repeatedly while dragging). The only change event: there is no separate commit event on release. |

| Slot | Type | Description |
| --- | --- | --- |
| `EndSlot` | `RenderFragment?` | Content placed after the slider (Ionic `slot="end"`). |
| `LabelSlot` | `RenderFragment?` | The label content when no inline [`Label`](#ionrange) is set (Ionic `slot="label"`). |
| `StartSlot` | `RenderFragment?` | Content placed before the slider (Ionic `slot="start"`). |
<!-- /miko:api -->

## Styling

### Theme tokens

`IonRange` has no theme token: its sizes are fixed per mode, and its default colors come from the palette's primary color (see [Theming](./theming.md#palette)).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-range` | The host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-range .range-wrapper` | The row holding the label and the slider area. |
| `.ion-range .label-text-wrapper`, `.ion-range .label-text` | The label (the wrapper also gets `.label-text-wrapper-hidden` when there is none). |
| `.ion-range .native-wrapper` | The start slot, the slider, and the end slot. |
| `.ion-range .range-slider` | The 42px-high area that receives presses and drags. |
| `.ion-range .range-bar`, `.ion-range .range-bar-active` | The full track and its filled part. |
| `.ion-range .range-knob-handle`, `.ion-range .range-knob` | The positioned knob handle and the visible knob. |
| `.ion-range .range-pin`, `.ion-range .range-pin-arrow` | The value pin and its pointer (md). |
| `.range-label-placement-start`, `-end`, `-fixed`, `-stacked` | `LabelPlacement`. |
| `.range-has-pin`, `.range-disabled` | `Pin`, `Disabled`. |
| `.range-value-min`, `.range-value-max` | The value is at `Min` / at `Max`. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |

```csharp
// GlobalStyles.cs — a brand-colored slider: <IonRange Class="brand" … />
[".ion-range.brand .range-bar-active"] = new()
{
    BackgroundColor = Color.FromHex("#0e7490"),
},
[".ion-range.brand .range-knob"] = new()
{
    BackgroundColor = Color.FromHex("#0e7490"),
},
```
