# Progress Bar

A progress bar is a thin horizontal bar that shows how far an operation has progressed, or — when the amount is unknown — that it is still running.

`IonProgressBar` fills its container's width and is 4px tall (the `Height` token); in `ios` mode its ends are fully rounded. For a compact loading indicator without a track, see [Spinner](./spinner.md).

## Determinate

The default type. `Value` (0–1, clamped) sets how much of the bar is filled; the unfilled part shows the track. The fill jumps to a new value — changes are not animated.

<!-- miko:example examples/progress-bar/Determinate.razor -->
```razor
@* Value runs from 0 to 1 (clamped). *@
<IonProgressBar Value="@_value" />
<IonButton Size="small" OnClick="Advance">Add 10%</IonButton>

@code {
    private double _value = 0.2;

    private void Advance() => _value = Math.Min(1, _value + 0.1);
}
```
<!-- /miko:example -->

## Buffer

`Buffer` (0–1, default 1) marks how much has been loaded ahead of `Value`, for example while streaming media. With `Buffer` below 1 the track only extends to the buffer point and an animated dotted stream runs past it.

<!-- miko:example examples/progress-bar/Buffer.razor -->
```razor
@* Buffer < 1 shows the buffered range behind the value, with the dotted stream past it. *@
<IonProgressBar Value="0.25" Buffer="0.5" />
```
<!-- /miko:example -->

## Indeterminate

`Type="indeterminate"` ignores `Value` and `Buffer` and runs two stripes along the bar in a loop. `Reversed` mirrors the bar so it fills (or animates) from right to left; it applies to the determinate type as well.

<!-- miko:example examples/progress-bar/Indeterminate.razor -->
```razor
<IonProgressBar Type="indeterminate" />
<IonProgressBar Type="indeterminate" Reversed="true" />
```
<!-- /miko:example -->

## Colors

`Color` sets the fill and stripes to a palette color and the track to the same color at 30% opacity. See [Theming](./theming.md#palette).

<!-- miko:example examples/progress-bar/Colors.razor -->
```razor
<IonProgressBar Value="0.5" Color="secondary" />
<IonProgressBar Value="0.25" Buffer="0.5" Color="danger" />
<IonProgressBar Type="indeterminate" Color="success" />
```
<!-- /miko:example -->

## In a toolbar

A progress bar placed in an [`IonToolbar`](./toolbar.md)'s default content is pinned along the toolbar's bottom edge, across its full width.

<!-- miko:example examples/progress-bar/Toolbar.razor -->
```razor
@* Placed in the toolbar's default content, the bar is pinned along the toolbar's bottom edge. *@
<IonToolbar>
    <IonTitle>Uploading</IonTitle>
    <IonProgressBar Type="indeterminate" />
</IonToolbar>
```
<!-- /miko:example -->

## API

<!-- miko:api IonProgressBar -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonProgressBar

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Buffer` | `double` | `1` | The buffered fraction of a determinate bar, clamped to [0, 1]. Below 1 the track ends at this point and an animated dotted stream shows past it. |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class the stylesheet maps to the fill / track color. |
| `Reversed` | `bool` | `false` | If true, the bar is mirrored so it fills / animates from right to left (Ionic `reversed`). |
| `Type` | `string` | `"determinate"` | Bar state (Ionic `type`): `"determinate"` (default, shows [`Value`](#ionprogressbar) and [`Buffer`](#ionprogressbar)) or `"indeterminate"` (two stripes loop along the bar; [`Value`](#ionprogressbar) and [`Buffer`](#ionprogressbar) are ignored). |
| `Value` | `double` | `0` | The filled fraction of a determinate bar, clamped to [0, 1]. Changes are not animated. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens ProgressBar -->
Theme token `IonicTheme.ProgressBar` (`ProgressBarToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.ProgressBar = new ProgressBarToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `rgba(0, 84, 233, 0.3)` | `rgba(0, 84, 233, 0.3)` |
| `BorderRadius` | `float` | `0` | `9999` |
| `Height` | `float` | `4` | `4` |
| `ProgressBackground` | `Color` | `#0054e9` | `#0054e9` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-progress-bar` | The host. Also carries the mode (`md` / `ios`) and the classes below. |
| `.progress-bar-determinate`, `.progress-bar-indeterminate` | `Type`. |
| `.progress-bar-reversed` | `Reversed`. |
| `.progress-bar-solid` | `Buffer` is 1 (no buffer stream). |
| `.ion-progress-bar .progress` | The value fill (determinate). |
| `.ion-progress-bar .progress-buffer-bar` | The track. |
| `.ion-progress-bar .buffer-circles` | The dotted buffer stream (determinate, `Buffer` below 1). |
| `.ion-progress-bar .progress-indeterminate` | The two moving stripes (indeterminate). |
| `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |

```csharp
// GlobalStyles.cs — a thicker, rounded bar: <IonProgressBar Class="thick" Value="0.4" />
[".ion-progress-bar.thick"] = new()
{
    Height = Length.Px(8),
    BorderRadius = new BorderRadius(Length.Px(4)),
},
```
