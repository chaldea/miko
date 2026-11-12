# Spinner

A spinner is an animated indicator that shows something is loading or in progress, with no measure of how far along it is.

`IonSpinner` draws one of nine preset animations in a 28px box (the `Size` token). For progress you can measure, use a [progress bar](./progress-bar.md); for a blocking overlay, see `IonLoading` ([Loading](./loading.md)), which shows a spinner itself.

## Basic usage

Without a `Name` the spinner follows the mode: `circular` in `md` mode, `lines` in `ios` mode.

<!-- miko:example examples/spinner/Basic.razor -->
```razor
@* Without a Name the spinner follows the mode: "circular" on md, "lines" on ios. *@
<IonSpinner />
```
<!-- /miko:example -->

## Names

`Name` picks one of the nine presets. The `-small` variants draw shorter lines in the same 28px box, so differently named spinners still line up.

<!-- miko:example examples/spinner/Names.razor -->
```razor
<IonSpinner Name="lines" />
<IonSpinner Name="lines-small" />
<IonSpinner Name="lines-sharp" />
<IonSpinner Name="lines-sharp-small" />
<IonSpinner Name="bubbles" />
<IonSpinner Name="circles" />
<IonSpinner Name="circular" />
<IonSpinner Name="crescent" />
<IonSpinner Name="dots" />
```
<!-- /miko:example -->

## Duration and paused

`Duration` sets the length of one animation cycle in milliseconds. `Paused` stops the animation and dims the spinner to 60% opacity.

<!-- miko:example examples/spinner/Paused.razor -->
```razor
@* Paused stops the animation and dims the spinner; Duration (ms) changes its speed. *@
<IonSpinner Name="crescent" Paused="@_paused" Duration="2000" />
<IonButton Size="small" OnClick="@(() => _paused = !_paused)">@(_paused ? "Resume" : "Pause")</IonButton>

@code {
    private bool _paused;
}
```
<!-- /miko:example -->

## Colors

`Color` takes a palette name and tints the lines, dots, or arc. See [Theming](./theming.md#palette).

<!-- miko:example examples/spinner/Colors.razor -->
```razor
<IonSpinner Color="primary" />
<IonSpinner Color="success" />
<IonSpinner Color="danger" />
<IonSpinner Name="dots" Color="medium" />
```
<!-- /miko:example -->

## In an item

A spinner in an [`IonItem`](./item.md)'s `Start` slot gets a 16px gap before the label, like an icon.

<!-- miko:example examples/spinner/Item.razor -->
```razor
@* In an item's Start slot the spinner gets the same 16px gap before the label as an icon. *@
<IonList>
    <IonItem>
        <Start>
            <IonSpinner Name="lines-small" />
        </Start>
        <ChildContent>
            <IonLabel>Syncing contacts…</IonLabel>
        </ChildContent>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## API

<!-- miko:api IonSpinner -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonSpinner

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, .... |
| `Duration` | `int?` | `null` | Duration of one animation cycle in milliseconds (Ionic `duration`). When null, uses Ionic's per-preset timing (750–1400 ms); 0 or less disables the animation. |
| `Name` | `string?` | `null` | Spinner preset name (Ionic `name`): `bubbles`, `circles`, `circular`, `crescent`, `dots`, `lines`, `lines-small`, `lines-sharp`, or `lines-sharp-small`. When null, `circular` in md mode and `lines` in ios mode; an unrecognized name falls back to `lines`. |
| `Paused` | `bool` | `false` | When true, the animation is paused and the spinner is dimmed (Ionic `paused`). |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Spinner -->
Theme token `IonicTheme.Spinner` (`SpinnerToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Spinner = new SpinnerToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Color` | `Color` | `#0054e9` | `#0054e9` |
| `Size` | `float` | `28` | `28` |
| `TrackColor` | `Color` | `rgba(0, 84, 233, 0.2)` | `rgba(0, 84, 233, 0.2)` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-spinner` | The host box (28px). Also carries the mode (`md` / `ios`). |
| `.spinner-{name}` | The resolved preset (e.g. `.spinner-crescent`). |
| `.spinner-paused` | `Paused`. |
| `.ion-spinner .spinner-line` | One line of a `lines*` preset (also `.spinner-line-{n}`). |
| `.ion-spinner .spinner-circle` | One dot of `bubbles` / `circles` / `dots`, or the arc of `circular` / `crescent` (also `.spinner-circle-{n}`). |
| `.ion-color-{name}` | `Color` (e.g. `.ion-color-primary`). |

The segments are not tinted by the text color: lines and dots are painted with `BackgroundColor`, and the `circular` / `crescent` arc with border colors (its `BackgroundColor` must stay transparent). For a palette color use `Color`; to change the default everywhere, set the `Color` token (`TrackColor` is the faint ring of `circular`). The `SmallSize` token is not used by any rule.

```csharp
// GlobalStyles.cs — white lines on a dark surface: <IonSpinner Name="lines" Class="on-dark" />
[".ion-spinner.on-dark .spinner-line"] = new() { BackgroundColor = Color.White },
```
