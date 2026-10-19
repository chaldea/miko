# Note

A note is a short piece of muted, smaller text for metadata, such as a count, a size, or a timestamp beside a list item.

`IonNote` is an inline-block span that wraps its child content.

## Basic usage

<!-- miko:example examples/note/Basic.razor -->
```razor
<IonNote>Last updated 5 minutes ago</IonNote>
```
<!-- /miko:example -->

## In an item

Put the note in an [`IonItem`](./item.md)'s `End` slot to show metadata at the trailing edge of the row.

<!-- miko:example examples/note/Item.razor -->
```razor
@* Metadata at the trailing edge of a row: put the note in the End slot. *@
<IonList>
    <IonItem>
        <ChildContent>
            <IonLabel>Storage used</IonLabel>
        </ChildContent>
        <End>
            <IonNote>12.4 GB</IonNote>
        </End>
    </IonItem>
    <IonItem>
        <ChildContent>
            <IonLabel>Version</IonLabel>
        </ChildContent>
        <End>
            <IonNote>2.1.0</IonNote>
        </End>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Colors

`Color` sets the text to a palette color — see [Theming](./theming.md#palette).

<!-- miko:example examples/note/Colors.razor -->
```razor
<IonNote Color="primary">Primary</IonNote>
<IonNote Color="secondary">Secondary</IonNote>
<IonNote Color="tertiary">Tertiary</IonNote>
<IonNote Color="success">Success</IonNote>
<IonNote Color="warning">Warning</IonNote>
<IonNote Color="danger">Danger</IonNote>
<IonNote Color="medium">Medium</IonNote>
<IonNote Color="dark">Dark</IonNote>
```
<!-- /miko:example -->

## API

<!-- miko:api IonNote -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonNote

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class the stylesheet maps to the text color. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Note -->
Theme token `IonicTheme.Note` (`NoteToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Note = new NoteToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Color` | `Color` | `#666666` | `#a6a6a6` |
| `FontSize` | `float` | `14` | `14` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-note` | The note host. Also carries the mode (`md` / `ios`). |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-success`). |
