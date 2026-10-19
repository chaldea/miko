# List

A list is a vertical container of rows, usually [items](./item.md), for content such as settings, contacts, or search results.

`IonList` is the container and sets the divider style and inset look for all of its items. `IonListHeader` is a section heading row with an optional action button.

## Basic usage

<!-- miko:example examples/list/Basic.razor -->
```razor
<IonList>
    <IonItem>
        <IonLabel>Pokémon Yellow</IonLabel>
    </IonItem>
    <IonItem>
        <IonLabel>Mega Man X</IonLabel>
    </IonItem>
    <IonItem>
        <IonLabel>The Legend of Zelda</IonLabel>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Inset

`Inset="true"` adds a margin around the list, rounds its corners, and clips its content. The last `IonItem` in the list loses its divider, and two inset lists in a row share one margin between them.

<!-- miko:example examples/list/Inset.razor -->
```razor
@* Margin and rounded corners around the list; the last item loses its divider. *@
<IonList Inset="true">
    <IonItem>
        <IonLabel>Pokémon Yellow</IonLabel>
    </IonItem>
    <IonItem>
        <IonLabel>Mega Man X</IonLabel>
    </IonItem>
    <IonItem>
        <IonLabel>The Legend of Zelda</IonLabel>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Lines

`Lines` sets the divider of every item in the list: `"full"` (across the whole row), `"inset"` (starting after the leading padding, the default look), or `"none"`. It only applies to items that leave their own `Lines` unset; an item's value always wins.

<!-- miko:example examples/list/Lines.razor -->
```razor
<IonList Lines="full">
    <IonItem>
        <IonLabel>Full lines</IonLabel>
    </IonItem>
    @* An item's own Lines wins over the list's. *@
    <IonItem Lines="none">
        <IonLabel>No line on this item</IonLabel>
    </IonItem>
    <IonItem>
        <IonLabel>Full lines</IonLabel>
    </IonItem>
</IonList>

<IonList Lines="none">
    <IonItem>
        <IonLabel>No lines</IonLabel>
    </IonItem>
    <IonItem>
        <IonLabel>No lines</IonLabel>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## List header

`IonListHeader` is a heading row: a label in its default slot fills the row, and an [`IonButton`](./button.md) after it defaults to the clear fill. It has no divider unless you set `Lines`. `Color` fills the header with a palette color and uses the matching contrast text. A header also works outside a list, as a heading above any content.

<!-- miko:example examples/list/Header.razor -->
```razor
<IonList>
    @* A button in a list header defaults to the clear fill. *@
    <IonListHeader>
        <IonLabel>Recently played</IonLabel>
        <IonButton OnClick="ShowAll">See all</IonButton>
    </IonListHeader>
    <IonItem>
        <IonLabel>Super Mario World</IonLabel>
    </IonItem>
    <IonItem>
        <IonLabel>Pac-Man</IonLabel>
    </IonItem>
</IonList>

@code {
    private void ShowAll()
    {
        // Navigate to the full list, open a modal, …
    }
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonList IonListHeader -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonList

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string?` | `null` | Optional DOM id for the list (Ionic's `id` attribute, e.g. `"inbox-list"`). |
| `Inset` | `bool` | `false` | When true, the list gets margin around it and rounded corners, and the last item's divider is removed (Ionic `inset`). |
| `Lines` | `string?` | `null` | How the bottom border is displayed on all items (Ionic `lines`): `"full"`, `"inset"`, or `"none"`. Applies only to items that do not set their own `lines` — an item-level value takes priority (Ionic targets `.item-lines-default` only). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonListHeader

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, etc. |
| `Lines` | `string?` | `null` | How the bottom border is displayed (Ionic `lines`): `"full"`, `"inset"`, or `"none"`. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens List -->
Theme token `IonicTheme.List` (`ListToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.List = new ListToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `#ffffff` | `#ffffff` |
| `HeaderColor` | `Color` | `#595959` | `#737373` |
| `HeaderFontSize` | `float` | `16` | `17` |
| `InsetBorderRadius` | `float` | `2` | `10` |
| `InsetMargin` | `float` | `16` | `16` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-list` | The list host. Also carries the mode (`md` / `ios`) and `list-md` / `list-ios`. |
| `.list-inset` | `Inset="true"`. |
| `.list-lines-full`, `.list-lines-inset`, `.list-lines-none` | `Lines` (also stamped mode-qualified, e.g. `.list-md-lines-full`). |
| `.ion-list-header` | The header host. |
| `.ion-list-header .list-header-inner` | The header's inner row (draws the `"inset"` line). |
| `.list-header-lines-full`, `.list-header-lines-inset`, `.list-header-lines-none` | `Lines` on the header. |
| `.ion-list-header.ion-color-{name}` | `Color` on the header (e.g. `.ion-color-primary`). |

```csharp
// GlobalStyles.cs — a grouped-settings look: <IonList Class="settings" Inset="true">…</IonList>
[".ion-list.settings"] = new()
{
    BackgroundColor = Color.FromHex("#f4f5f8"),
    BorderRadius = new BorderRadius(12),
},
```
