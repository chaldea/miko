# Header

A header is the top region of a page: a wrapper for one or more toolbars that stays above the scrolling content.

`IonHeader` goes first in an [`IonPage`](./app.md#page-structure), before [`IonContent`](./content.md). It is a positioned block (z-index 10) so its shadow paints over the content below it, and it clears the device's status bar: the first [`IonToolbar`](./toolbar.md) inside it is padded by the top safe-area inset.

## Basic usage

In md mode the header casts an elevation shadow; in ios mode it has a hairline bottom border instead (`Header` token).

<!-- miko:example examples/header/Basic.razor -->
```razor
<IonHeader>
    <IonToolbar>
        <Start>
            <IonBackButton DefaultHref="/" />
        </Start>
        <ChildContent>
            <IonTitle>Settings</IonTitle>
        </ChildContent>
    </IonToolbar>
</IonHeader>
```
<!-- /miko:example -->

## Multiple toolbars

Toolbars stack top to bottom — a title toolbar followed by a [searchbar](./searchbar.md) or [segment](./segment.md) toolbar is the usual pattern. Only the first toolbar gets the top safe-area padding; every toolbar is padded for left and right insets.

<!-- miko:example examples/header/MultipleToolbars.razor -->
```razor
@* Toolbars stack top to bottom. Only the first one is padded for the status bar. *@
<IonHeader>
    <IonToolbar>
        <IonTitle>Contacts</IonTitle>
    </IonToolbar>
    <IonToolbar>
        <IonSearchbar @bind-Value="_query" Placeholder="Search contacts" />
    </IonToolbar>
</IonHeader>

@code {
    private string? _query;
}
```
<!-- /miko:example -->

## Differences from Ionic

`IonHeader` has no parameters besides `Class` and `Style`: Ionic's `translucent` and `collapse` (`"condense"` large titles, `"fade"`) are not implemented, and the `ion-no-border` class has no effect on a header — remove the shadow or border with an app style (see [CSS classes](#css-classes)).

## API

<!-- miko:api IonHeader -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonHeader

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

The `Header` token's `BorderColor` also colors the iOS hairline above an [`IonFooter`](./footer.md).

<!-- miko:tokens Header -->
Theme token `IonicTheme.Header` (`HeaderToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Header = new HeaderToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `BorderColor` | `Color` | `transparent` | `rgba(0, 0, 0, 0.2)` |
| `BorderWidth` | `float` | `0` | `0.55` |
| `BoxShadow` | `List<BoxShadow>` | `0 2px 4px -1px rgba(0, 0, 0, 0.2), 0 4px 5px 0 rgba(0, 0, 0, 0.14), 0 1px 10px 0 rgba(0, 0, 0, 0.12)` | `none` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-header` | The host: shadow (md) or bottom border (ios). Also carries the mode (`md` / `ios`). |
| `.ion-header .ion-toolbar:first-of-type` | The toolbar that receives the top safe-area padding. |

```csharp
// GlobalStyles.cs — a flat header: <IonHeader Class="flat">…</IonHeader>
[".ion-header.flat"] = new()
{
    BoxShadow = new List<BoxShadow>(),
    BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
},
```
