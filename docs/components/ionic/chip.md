# Chip

A chip is a compact, pill-shaped element for a tag, a filter, or a contact, holding text plus an optional icon or avatar.

`IonChip` is an inline-flex pill that lays out its child content in a row.

## Basic usage

`Outline="true"` swaps the gray fill for a border; `Disabled="true"` dims the chip and blocks input.

<!-- miko:example examples/chip/Basic.razor -->
```razor
<IonChip>Default</IonChip>
<IonChip Outline="true">Outline</IonChip>
<IonChip Disabled="true">Disabled</IonChip>
```
<!-- /miko:example -->

## Icons and avatars

Put an [`IonLabel`](./label.md), [`IonIcon`](./icon.md)s, or an [`IonAvatar`](./avatar.md) in the chip. Icons get a 20px box and the avatar shrinks to 24px; as the chip's first or last child they hang into the chip's padding, with an 8px gap toward the label.

<!-- miko:example examples/chip/Content.razor -->
```razor
@* A leading or trailing icon / avatar (first or last child) hangs into the chip's padding. *@
<IonChip>
    <IonAvatar>
        <img src="https://ionicframework.com/docs/img/demos/avatar.svg" />
    </IonAvatar>
    <IonLabel>Avatar chip</IonLabel>
    <IonIcon Icon="@Ionicons.CloseCircle" />
</IonChip>

<IonChip>
    <IonIcon Icon="@Ionicons.Pin" Color="primary" />
    <IonLabel>Icon chip</IonLabel>
    <IonIcon Icon="@Ionicons.Close" />
</IonChip>
```
<!-- /miko:example -->

## Colors

`Color` tints the chip with a light wash of the palette color and colors its text; on an outline chip it colors the border and text instead — see [Theming](./theming.md#palette).

<!-- miko:example examples/chip/Colors.razor -->
```razor
<IonChip Color="primary">Primary</IonChip>
<IonChip Color="secondary">Secondary</IonChip>
<IonChip Color="success">Success</IonChip>
<IonChip Color="danger">Danger</IonChip>

<IonChip Outline="true" Color="primary">Primary</IonChip>
<IonChip Outline="true" Color="secondary">Secondary</IonChip>
<IonChip Outline="true" Color="success">Success</IonChip>
<IonChip Outline="true" Color="danger">Danger</IonChip>
```
<!-- /miko:example -->

## Handling taps

`IonChip` has no click event, and an `@onclick` written on the `<IonChip>` tag itself compiles but is ignored. Wrap the chip in an element and handle the tap there. The chip shows a pointer cursor and a hover wash on pointer devices.

<!-- miko:example examples/chip/Taps.razor -->
```razor
@* IonChip has no click event: handle the tap on a wrapping element. *@
@foreach (var tag in _tags)
{
    <span @onclick="() => Toggle(tag)">
        <IonChip Color="@(_selected.Contains(tag) ? "primary" : null)">
            <IonLabel>@tag</IonLabel>
        </IonChip>
    </span>
}

@code {
    private readonly string[] _tags = ["Music", "Sports", "Travel"];
    private readonly HashSet<string> _selected = ["Music"];

    private void Toggle(string tag)
    {
        if (!_selected.Remove(tag))
            _selected.Add(tag);
    }
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonChip -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonChip

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, .... |
| `Disabled` | `bool` | `false` | When true, the chip is dimmed and non-interactive. |
| `Outline` | `bool` | `false` | When true, renders the outline chip variant. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Chip -->
Theme token `IonicTheme.Chip` (`ChipToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Chip = new ChipToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `rgba(0, 0, 0, 0.12)` | `rgba(0, 0, 0, 0.12)` |
| `BorderColor` | `Color` | `rgba(0, 0, 0, 0.32)` | `rgba(0, 0, 0, 0.32)` |
| `Color` | `Color` | `rgba(0, 0, 0, 0.87)` | `rgba(0, 0, 0, 0.87)` |
| `FontSize` | `float` | `14` | `14` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-chip` | The chip host. Also carries the mode (`md` / `ios`) and `ion-activatable`. |
| `.chip-outline`, `.chip-disabled` | `Outline`, `Disabled`. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-primary`). |
| `.ion-chip .ion-icon`, `.ion-chip .ion-avatar` | Icons and avatars inside the chip. |

```csharp
// GlobalStyles.cs — a "selected" state for filter chips: <IonChip Class="selected">…</IonChip>
[".ion-chip.selected"] = new()
{
    BackgroundColor = Color.FromHex("#0054e9"),
    Color = Color.White,
},
```
