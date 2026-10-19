# Thumbnail

A thumbnail is a square preview image, typically shown at the start or end of a list item.

`IonThumbnail` is a fixed-size square box; put an `img` in it.

## Basic usage

On its own the thumbnail is 48px square in both modes, with square corners. The image fills the box. Use a square image: `object-fit` is not supported, so a non-square image is stretched to the box rather than cropped.

<!-- miko:example examples/thumbnail/Basic.razor -->
```razor
<IonThumbnail>
    <img src="https://ionicframework.com/docs/img/demos/thumbnail.svg" />
</IonThumbnail>
```
<!-- /miko:example -->

## In an item

In an [`IonItem`](./item.md)'s `Start` or `End` slot the thumbnail grows to 56px and gets the item's media margins. The in-item size is the `ThumbnailSize` token of the [Item](./item.md#theme-tokens) theme.

<!-- miko:example examples/thumbnail/Item.razor -->
```razor
<IonList>
    <IonItem>
        <Start>
            <IonThumbnail>
                <img src="https://ionicframework.com/docs/img/demos/thumbnail.svg" />
            </IonThumbnail>
        </Start>
        <ChildContent>
            <IonLabel>Thumbnail in the Start slot</IonLabel>
        </ChildContent>
    </IonItem>
    <IonItem>
        <ChildContent>
            <IonLabel>Thumbnail in the End slot</IonLabel>
        </ChildContent>
        <End>
            <IonThumbnail>
                <img src="https://ionicframework.com/docs/img/demos/thumbnail.svg" />
            </IonThumbnail>
        </End>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

An [`IonSkeletonText`](./skeleton-text.md) inside a thumbnail fills it, for a loading placeholder.

## API

<!-- miko:api IonThumbnail -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonThumbnail

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Thumbnail -->
Theme token `IonicTheme.Thumbnail` (`ThumbnailToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Thumbnail = new ThumbnailToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Size` | `float` | `48` | `48` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-thumbnail` | The thumbnail host: size and corner radius. Also carries the mode (`md` / `ios`). |
| `.ion-thumbnail img` | The image: fills the host. |

```csharp
// GlobalStyles.cs — rounded thumbnails app-wide
[".ion-thumbnail, .ion-thumbnail img"] = new()
{
    BorderRadius = new BorderRadius(8),
},
```
