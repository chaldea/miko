# Title

A title is the text label of a toolbar, naming the current screen or section.

`IonTitle` goes in the default slot of an [`IonToolbar`](./toolbar.md) — directly, or in `<ChildContent>` when the toolbar's named slots are used. It grows to fill the space the slots leave and takes its color from the `Toolbar` token.

## Basic usage

In md mode the title is left-aligned at 20px medium weight; in ios mode it is centered at 17px semibold (`Title` token).

<!-- miko:example examples/title/Basic.razor -->
```razor
<IonToolbar>
    <IonTitle>Inbox</IonTitle>
</IonToolbar>
```
<!-- /miko:example -->

## With toolbar buttons

The title occupies the width between the `Start` and `End` slots. In ios mode it is centered within that width — not across the whole toolbar as in Ionic — so with buttons of different widths on each side it sits off the toolbar's center.

<!-- miko:example examples/title/WithButtons.razor -->
```razor
@* The title takes the space left between the Start and End slots; on iOS it is centered in
   that space, not across the whole toolbar. *@
<IonToolbar>
    <Start>
        <IonBackButton DefaultHref="/" />
    </Start>
    <ChildContent>
        <IonTitle>Edit profile</IonTitle>
    </ChildContent>
    <End>
        <IonButtons>
            <IonButton Strong="true">Save</IonButton>
        </IonButtons>
    </End>
</IonToolbar>
```
<!-- /miko:example -->

## Long titles

The title stays on one line; text that does not fit is clipped at the edge (no ellipsis, no wrapping). Ionic's `size="large"` and collapsible large titles are not implemented — `IonTitle` has no parameters besides `Class` and `Style`. Several titles in one toolbar stack vertically.

## API

<!-- miko:api IonTitle -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonTitle

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

The text color is the [Toolbar token](./toolbar.md#theme-tokens)'s `Color`. `TextAlign` aligns the text within the title's width.

<!-- miko:tokens Title -->
Theme token `IonicTheme.Title` (`TitleToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Title = new TitleToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `FontSize` | `float` | `20` | `17` |
| `FontWeight` | `FontWeight` | `FontWeight.Medium` | `FontWeight.SemiBold` |
| `PaddingX` | `float` | `20` | `16` |
| `TextAlign` | `TextAlign` | `TextAlign.Left` | `TextAlign.Center` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-title` | The host: a flex row with the horizontal padding, font size, weight, and color. Also carries the mode (`md` / `ios`). |
| `.ion-title .toolbar-title` | The inner text box: one line, clipped, `text-align` from the token. |

```csharp
// GlobalStyles.cs — center titles in md mode too.
[".md.ion-title .toolbar-title"] = new() { TextAlign = TextAlign.Center },
```
