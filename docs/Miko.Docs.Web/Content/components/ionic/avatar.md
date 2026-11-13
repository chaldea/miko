# Avatar

An avatar is a circular image, usually a picture of a person or an object, shown on its own or inside a chip or an item.

`IonAvatar` is a square box rounded into a circle; put an `img` in it.

## Basic usage

On its own the avatar is 64px in md mode and 48px in ios mode. The image fills the box and is clipped to the circle. Use a square image: `object-fit` is not supported, so a non-square image is stretched to the box.

<!-- miko:example examples/avatar/Basic.razor -->
```razor
<IonAvatar>
    <img src="https://ionicframework.com/docs/img/demos/avatar.svg" />
</IonAvatar>
```
<!-- /miko:example -->

## In a chip

Inside an [`IonChip`](./chip.md) the avatar shrinks to 24px. As the chip's first or last child it hangs into the chip's padding.

<!-- miko:example examples/avatar/Chip.razor -->
```razor
@* Inside a chip the avatar shrinks to 24px and tucks into the chip's leading edge. *@
<IonChip>
    <IonAvatar>
        <img src="https://ionicframework.com/docs/img/demos/avatar.svg" />
    </IonAvatar>
    <IonLabel>Ada Lovelace</IonLabel>
</IonChip>
```
<!-- /miko:example -->

## In an item

In an [`IonItem`](./item.md)'s `Start` or `End` slot the avatar shrinks to 40px (md) / 36px (ios) and gets the item's media margins. These sizes and margins are `Item` theme tokens (`AvatarSize`, `Avatar*Margin*`), listed on the [Item](./item.md#theme-tokens) page.

<!-- miko:example examples/avatar/Item.razor -->
```razor
<IonList>
    <IonItem>
        <Start>
            <IonAvatar>
                <img src="https://ionicframework.com/docs/img/demos/avatar.svg" />
            </IonAvatar>
        </Start>
        <ChildContent>
            <IonLabel>Avatar in the Start slot</IonLabel>
        </ChildContent>
    </IonItem>
    <IonItem>
        <ChildContent>
            <IonLabel>Avatar in the End slot</IonLabel>
        </ChildContent>
        <End>
            <IonAvatar>
                <img src="https://ionicframework.com/docs/img/demos/avatar.svg" />
            </IonAvatar>
        </End>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

An [`IonSkeletonText`](./skeleton-text.md) inside an avatar fills it, for a loading placeholder.

## API

<!-- miko:api IonAvatar -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonAvatar

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Avatar -->
Theme token `IonicTheme.Avatar` (`AvatarToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Avatar = new AvatarToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Size` | `float` | `64` | `48` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-avatar` | The avatar host: size and circular radius. Also carries the mode (`md` / `ios`). |
| `.ion-avatar img` | The image: fills the host, rounded to match. |

```csharp
// GlobalStyles.cs — a rounded-square avatar: <IonAvatar Class="square">…</IonAvatar>
[".ion-avatar.square, .ion-avatar.square img"] = new()
{
    BorderRadius = new BorderRadius(8),
},
```
