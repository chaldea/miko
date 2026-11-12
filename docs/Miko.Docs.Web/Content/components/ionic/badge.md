# Badge

A badge is a small pill of bold text, usually a count or a short status, shown next to another element to flag new or pending content.

`IonBadge` is an inline-block span that wraps its child content.

## Basic usage

A badge does not wrap its text and is filled with the primary color by default. A badge with no content is hidden.

<!-- miko:example examples/badge/Basic.razor -->
```razor
<IonBadge>99+</IonBadge>
<IonBadge>New</IonBadge>
```
<!-- /miko:example -->

## In an item

Put a badge in an [`IonItem`](./item.md)'s `End` slot for a trailing count, or in the `Start` slot to lead the row. A badge after the label in the default slot also ends up at the trailing edge, because the label grows to fill the row.

<!-- miko:example examples/badge/Item.razor -->
```razor
<IonList>
    <IonItem>
        <ChildContent>
            <IonLabel>Inbox</IonLabel>
        </ChildContent>
        <End>
            <IonBadge>@_unread</IonBadge>
        </End>
    </IonItem>
    <IonItem>
        <Start>
            <IonBadge Color="success">New</IonBadge>
        </Start>
        <ChildContent>
            <IonLabel>Release notes</IonLabel>
        </ChildContent>
    </IonItem>
</IonList>

@code {
    private int _unread = 12;
}
```
<!-- /miko:example -->

## Colors

`Color` fills the badge with a palette color and uses the matching contrast text (dark text on `light`, `success`, and `warning`) — see [Theming](./theming.md#palette).

<!-- miko:example examples/badge/Colors.razor -->
```razor
<IonBadge Color="primary">11</IonBadge>
<IonBadge Color="secondary">22</IonBadge>
<IonBadge Color="tertiary">33</IonBadge>
<IonBadge Color="success">44</IonBadge>
<IonBadge Color="warning">55</IonBadge>
<IonBadge Color="danger">66</IonBadge>
<IonBadge Color="light">77</IonBadge>
<IonBadge Color="medium">88</IonBadge>
<IonBadge Color="dark">99</IonBadge>
```
<!-- /miko:example -->

## In tab buttons

A badge inside a tab button floats over the top corner of the icon with smaller, tab-specific metrics, and in md mode an empty badge shows as a dot instead of being hidden. See [Tabs](./tabs.md).

## API

<!-- miko:api IonBadge -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonBadge

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, .... |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Badge -->
Theme token `IonicTheme.Badge` (`BadgeToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Badge = new BadgeToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `#0054e9` | `#0054e9` |
| `BorderRadius` | `float` | `4` | `10` |
| `Color` | `Color` | `#ffffff` | `#ffffff` |
| `FontSize` | `float` | `13` | `13` |
| `MinWidth` | `float` | `10` | `10` |
| `PaddingBottom` | `float` | `4` | `3` |
| `PaddingEnd` | `float` | `4` | `8` |
| `PaddingStart` | `float` | `4` | `8` |
| `PaddingTop` | `float` | `3` | `3` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-badge` | The badge host. Also carries the mode (`md` / `ios`). |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |

```csharp
// GlobalStyles.cs — pill-shaped badges app-wide
[".ion-badge"] = new()
{
    BorderRadius = new BorderRadius(999),
},
```
