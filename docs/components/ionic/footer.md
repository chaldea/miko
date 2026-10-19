# Footer

A footer is the bottom region of a page: a wrapper for one or more toolbars that stays below the scrolling content.

`IonFooter` goes last in an [`IonPage`](./app.md#page-structure), after [`IonContent`](./content.md). Like [`IonHeader`](./header.md) it is a positioned block (z-index 10); its toolbars are ordinary [`IonToolbar`](./toolbar.md)s.

## Basic usage

In md mode the footer casts a shadow upward onto the content; in ios mode its first toolbar has a hairline top border (colored by the [Header token](./header.md#theme-tokens)'s `BorderColor`).

<!-- miko:example examples/footer/Basic.razor -->
```razor
<IonFooter>
    <IonToolbar>
        <ChildContent>
            <IonTitle>3 items selected</IonTitle>
        </ChildContent>
        <End>
            <IonButtons>
                <IonButton Strong="true">Delete</IonButton>
            </IonButtons>
        </End>
    </IonToolbar>
</IonFooter>
```
<!-- /miko:example -->

## Without a border

The `ion-no-border` class removes the ios hairline. In md mode it currently does **not** remove the shadow — override `BoxShadow` with an app style for that (see [CSS classes](#css-classes)).

<!-- miko:example examples/footer/NoBorder.razor -->
```razor
@* ion-no-border removes the ios hairline; the md shadow needs an app style (see CSS classes). *@
<IonFooter Class="ion-no-border">
    <IonToolbar>
        <IonTitle>Footer</IonTitle>
    </IonToolbar>
</IonFooter>
```
<!-- /miko:example -->

## Safe area

The footer does not pad its last toolbar for the bottom safe-area inset (the home indicator / gesture bar), so on an edge-to-edge device the toolbar content can sit under it. Add the padding from the app stylesheet — the snippet under [CSS classes](#css-classes) does this.

## Translucent and collapse

These Ionic options are only partially ported. `Translucent` has an effect only in ios mode, and not Ionic's: there is no backdrop blur and the content does not scroll behind the footer — the footer's toolbars are drawn at 80% opacity (and the first one loses its hairline border). `Collapse` only stamps a `footer-collapse-{value}` class.

## API

<!-- miko:api IonFooter -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonFooter

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Collapse` | `string?` | `null` | Ionic's `collapse` (`"fade"`). Kept as a marker for API parity: it stamps `footer-collapse-{value}`, but no scroll-driven effect is implemented. |
| `Translucent` | `bool` | `false` | Ionic's `translucent`. Has an effect only in iOS mode, and only a partial one: there is no backdrop blur and content does not scroll behind the footer — the toolbars inside are drawn at 80% opacity. Stamps `footer-translucent` / `footer-translucent-{mode}`. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

`IonFooter` has no theme token of its own: the toolbars use the [Toolbar token](./toolbar.md#theme-tokens) and the ios hairline the [Header token](./header.md#theme-tokens)'s `BorderColor`.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-footer` | The host. Also carries the mode (`md` / `ios`) and `.footer-md` / `.footer-ios`. |
| `.ion-footer .ion-toolbar:first-of-type` | The toolbar that gets the ios hairline top border. |
| `.ion-footer .ion-toolbar:last-of-type` | The bottom-most toolbar (the one to pad for the safe area). |
| `.ion-no-border` | Set on the host to remove the ios hairline. |
| `.footer-translucent`, `.footer-translucent-ios` | `Translucent` (the second one in ios mode). |
| `.ion-footer .footer-background` | The empty background layer rendered for `Translucent` in ios mode. |
| `.footer-collapse-{value}` | `Collapse` (e.g. `.footer-collapse-fade`). |
| `.footer-toolbar-padding` | Always present on the host. |

```csharp
// GlobalStyles.cs — clear the home indicator, and drop the md shadow with ion-no-border.
[".ion-footer .ion-toolbar:last-of-type"] = new()
{
    PaddingBottom = Length.SafeAreaInsetBottom,
},
[".ion-footer.ion-no-border"] = new()
{
    BoxShadow = new List<BoxShadow>(),
},
```
