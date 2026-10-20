# Toolbar

A toolbar is a horizontal bar that holds a title, buttons, and other controls for the current screen, usually placed in an [`IonHeader`](./header.md) or [`IonFooter`](./footer.md).

`IonToolbar` arranges its content in a row: named slots for buttons at either edge, and the default slot — typically an [`IonTitle`](./title.md) — taking the remaining width. Group buttons with [`IonButtons`](./button.md#buttons-in-a-toolbar).

## Basic usage

Default-slot content goes into the flexible middle of the row. The toolbar is at least `MinHeight` tall (56px md, 44px ios) and grows with its content.

<!-- miko:example examples/toolbar/Basic.razor -->
```razor
<IonToolbar>
    <IonTitle>Toolbar</IonTitle>
</IonToolbar>
```
<!-- /miko:example -->

## Slots

`Start` and `End` pin content to the leading and trailing edge — usually an `IonBackButton` or `IonMenuButton` at the start and action buttons at the end. The slot decides the position; `IonButtons`' own `Slot` parameter does not move the group. When you fill a named slot, put the title in `<ChildContent>`.

<!-- miko:example examples/toolbar/Slots.razor -->
```razor
@* With a named slot in use, the title goes in <ChildContent>. *@
<IonToolbar>
    <Start>
        <IonBackButton DefaultHref="/" />
    </Start>
    <ChildContent>
        <IonTitle>Details</IonTitle>
    </ChildContent>
    <End>
        <IonButtons>
            <IonButton>
                <IconOnly>
                    <IonIcon Icon="@Ionicons.Share" />
                </IconOnly>
            </IonButton>
        </IonButtons>
    </End>
</IonToolbar>
```
<!-- /miko:example -->

## Primary and secondary

`Secondary` and `Primary` are Ionic's mode-aware button slots. Miko renders the slots in one fixed order in both modes — `Start`, `Secondary`, content, `Primary`, `End` — so unlike Ionic, Material Design does not move `Secondary` and `Primary` after the title. Use `Start` / `End` when the position matters.

<!-- miko:example examples/toolbar/PrimarySecondary.razor -->
```razor
@* Rendered left to right as Start, Secondary, title, Primary, End — in both modes. *@
<IonToolbar>
    <Secondary>
        <IonButtons>
            <IonButton>Reset</IonButton>
        </IonButtons>
    </Secondary>
    <ChildContent>
        <IonTitle>Filters</IonTitle>
    </ChildContent>
    <Primary>
        <IonButtons>
            <IonButton Strong="true">Apply</IonButton>
        </IonButtons>
    </Primary>
</IonToolbar>
```
<!-- /miko:example -->

## Buttons

Inside `IonButtons`, an `IonButton` without an explicit `Fill` is clear, uses the compact toolbar metrics (`Button…` tokens below), and takes the toolbar's text color. A toolbar button that sets `Color` keeps its palette color. [`IonMenuButton`](./menu.md) and [`IonBackButton`](./back-button.md) are separate components and can sit in a slot directly or inside `IonButtons`.

<!-- miko:example examples/toolbar/Buttons.razor -->
```razor
@* IonButtons gives its buttons the clear fill and the compact toolbar metrics. IonMenuButton
   only raises OnClick — wire it to your IonMenu (see the Menu page). *@
<IonToolbar>
    <Start>
        <IonButtons>
            <IonMenuButton />
        </IonButtons>
    </Start>
    <ChildContent>
        <IonTitle>Inbox</IonTitle>
    </ChildContent>
    <End>
        <IonButtons>
            <IonButton>
                <IconOnly>
                    <IonIcon Icon="@Ionicons.Search" />
                </IconOnly>
            </IonButton>
            <IonButton>
                <IconOnly>
                    <IonIcon Ios="@Ionicons.EllipsisHorizontal" Md="@Ionicons.EllipsisVertical" />
                </IconOnly>
            </IonButton>
        </IonButtons>
    </End>
</IonToolbar>
```
<!-- /miko:example -->

## Segments and searchbars

An [`IonSegment`](./segment.md) or [`IonSearchbar`](./searchbar.md) goes in the default slot, usually in a second toolbar under the title. In md mode a segment in a toolbar is as tall as the toolbar; in ios mode it shrinks to its content and is centered.

<!-- miko:example examples/toolbar/Controls.razor -->
```razor
@* A segment or searchbar goes in the default slot, usually in its own toolbar below the title. *@
<IonToolbar>
    <IonSegment @bind-Value="_filter">
        <IonSegmentButton Value="all">All</IonSegmentButton>
        <IonSegmentButton Value="favorites">Favorites</IonSegmentButton>
    </IonSegment>
</IonToolbar>

<IonToolbar>
    <IonSearchbar @bind-Value="_query" Placeholder="Search contacts" />
</IonToolbar>

@code {
    private string? _filter = "all";
    private string? _query;
}
```
<!-- /miko:example -->

## Progress bar

An [`IonProgressBar`](./progress-bar.md) placed directly in the default slot is pinned across the bottom edge of the toolbar instead of taking a row of its own.

<!-- miko:example examples/toolbar/Progress.razor -->
```razor
@* A progress bar directly in the default slot is pinned across the toolbar's bottom edge. *@
<IonToolbar>
    <IonTitle>Uploading</IonTitle>
    <IonProgressBar Value="0.4" />
</IonToolbar>
```
<!-- /miko:example -->

## Colors

`IonToolbar` has no `Color` parameter. The background and text color come from the `Toolbar` token, which the toolbar's title and buttons read too — so to color one toolbar, wrap it in a [`ConfigProvider`](./theming.md#theme-for-a-subtree-configprovider) with a partial theme; to color all of them, set the token app-wide.

<!-- miko:example examples/toolbar/Colors.razor -->
```razor
@* IonToolbar has no Color parameter. Theme the subtree instead: the toolbar background, its
   title and its buttons all read the Toolbar token. *@
<ConfigProvider IonicTheme="_brand">
    <IonToolbar>
        <Start>
            <IonButtons>
                <IonButton>Cancel</IonButton>
            </IonButtons>
        </Start>
        <ChildContent>
            <IonTitle>Brand</IonTitle>
        </ChildContent>
    </IonToolbar>
</ConfigProvider>

@code {
    private readonly IonicTheme _brand = new()
    {
        Toolbar = new ToolbarToken
        {
            Background = Color.FromHex("#0054e9"),
            Color = Color.FromHex("#ffffff"),
        },
    };
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonToolbar -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonToolbar

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
| `End` | `RenderFragment?` | Content pinned to the end (right in LTR) of the toolbar — Ionic's `slot="end"`. |
| `Primary` | `RenderFragment?` | Ionic's `slot="primary"`, rendered between the default content and [`End`](#iontoolbar). The order is the same in both modes (the toolbar does not reorder slots per mode). |
| `Secondary` | `RenderFragment?` | Ionic's `slot="secondary"`, rendered between [`Start`](#iontoolbar) and the default content. The order is the same in both modes — unlike Ionic, Material Design does not move it after the title. |
| `Start` | `RenderFragment?` | Content pinned to the start (left in LTR) of the toolbar — Ionic's `slot="start"`. Typically an `IonButtons` with a back/menu button. |
<!-- /miko:api -->

## Styling

### Theme tokens

The toolbar reads `Background`, `Color`, and `MinHeight` (an md-mode `IonSegment` in a toolbar also takes `MinHeight`). `Color` is also the text color of `IonTitle`, `IonMenuButton`, and every `IonButton` in the toolbar; the `Button…` values size the buttons inside `IonButtons`. `PaddingTop` / `PaddingBottom` / `PaddingStart` / `PaddingEnd` pad the row of slots and content (`.toolbar-container`): 0 in md, 3px / 4px in ios, as in Ionic. `BorderColor` lands on the background layer, whose border is 0 wide in both modes, so it currently has no visible effect.

<!-- miko:tokens Toolbar -->
Theme token `IonicTheme.Toolbar` (`ToolbarToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Toolbar = new ToolbarToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `#ffffff` | `#f7f7f7` |
| `BorderColor` | `Color` | `#b2b2b2` | `#c8c7cc` |
| `ButtonBorderRadius` | `float` | `2` | `4` |
| `ButtonIconFontSize` | `float` | `1.4` | `1.41` |
| `ButtonIconOnlyClearPadding` | `Length` | `12px` | `0px` |
| `ButtonIconOnlyClearSize` | `float` | `48` | `0` |
| `ButtonIconOnlyFontSize` | `float` | `1.8` | `1.65` |
| `ButtonMarginX` | `float` | `2` | `2` |
| `ButtonMinHeight` | `Length` | `32px` | `32px` |
| `ButtonPaddingBottom` | `Length` | `3px` | `3px` |
| `ButtonPaddingEnd` | `Length` | `8px` | `5px` |
| `ButtonPaddingStart` | `Length` | `8px` | `5px` |
| `ButtonPaddingTop` | `Length` | `3px` | `3px` |
| `Color` | `Color` | `#424242` | `#000000` |
| `MinHeight` | `float` | `56` | `44` |
| `PaddingBottom` | `Length` | `0px` | `3px` |
| `PaddingEnd` | `Length` | `0px` | `4px` |
| `PaddingStart` | `Length` | `0px` | `4px` |
| `PaddingTop` | `Length` | `0px` | `3px` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-toolbar` | The host: background, text color, minimum height, side safe-area padding. Also carries the mode (`md` / `ios`). |
| `.ion-toolbar .toolbar-background` | The painted backdrop behind the content. It repeats the background color, so a background override must target it too. |
| `.ion-toolbar .toolbar-container` | The flex row holding the slots and the content area. |
| `.ion-toolbar .toolbar-content` | The flexible wrapper around the default slot. |
| `.toolbar-segment` | Added to the host when the default slot contains an `IonSegment`. |
| `.ion-buttons` | An `IonButtons` group (also `.buttons-start` / `.buttons-end` from its `Slot`). |

```csharp
// GlobalStyles.cs — a see-through toolbar: <IonToolbar Class="clear">…</IonToolbar>
[".ion-toolbar.clear"] = new() { BackgroundColor = Color.Transparent },
[".ion-toolbar.clear .toolbar-background"] = new() { BackgroundColor = Color.Transparent },
```
