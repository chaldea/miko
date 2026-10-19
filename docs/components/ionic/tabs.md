# Tabs

Tabs are a top-level navigation pattern: a tab bar of buttons at the bottom (or top) of the screen switches between independent root pages.

`IonTabs` is the container: a content area plus `Top` and `Bottom` slots for the bar. `IonTabBar` is the bar, and each `IonTabButton` in it navigates to a route. There is no `ion-tab` component and no tab outlet: each tab is its own routed page, and the tabs container lives in the app layout so it stays on screen while the pages change.

## Tab layout

Put `IonTabs` in the default layout inside [`IonApp`](./app.md), with `@Body` in the `Content` slot and the `IonTabBar` in `Bottom` (or `Top`). The **slot** decides where the bar goes; `IonTabBar`'s `Slot` parameter only picks the border side. A tap on a button performs a root navigation to its `Href`: the history is cleared, no transition plays, and each tab page's scroll position is restored when you switch back to it. `OnClick` runs before the navigation.

`Selected` is not derived from the route — set it yourself, typically by comparing the layout's `NavigationManager.CurrentPath` with each tab's path (as the tabs app template does). Navigation re-renders the layout, so `Selected` follows the current page. `Tab` is only an identifier kept for Ionic parity; nothing reads it.

<!-- miko:example examples/tabs/TabsLayout.razor -->
```razor
@inherits LayoutComponentBase

@* MainLayout.razor of the tabs template. The routed page goes in Content; the tab bar in
   Bottom. Selected is not automatic: compare each tab with the current path. *@
<IonApp>
    <IonTabs>
        <Content>@Body</Content>
        <Bottom>
            <IonTabBar Slot="bottom">
                <IonTabButton Tab="tab1" Href="/tab1" Selected="@IsActive("/tab1", "/")">
                    <IonIcon Icon="@Ionicons.Triangle" />
                    <IonLabel>Tab 1</IonLabel>
                </IonTabButton>
                <IonTabButton Tab="tab2" Href="/tab2" Selected="@IsActive("/tab2")">
                    <IonIcon Icon="@Ionicons.Ellipse" />
                    <IonLabel>Tab 2</IonLabel>
                </IonTabButton>
                <IonTabButton Tab="tab3" Href="/tab3" Selected="@IsActive("/tab3")">
                    <IonIcon Icon="@Ionicons.Square" />
                    <IonLabel>Tab 3</IonLabel>
                </IonTabButton>
            </IonTabBar>
        </Bottom>
    </IonTabs>
</IonApp>

@code {
    private string CurrentPath => NavigationManager?.CurrentPath ?? "/";

    private bool IsActive(params string[] paths) =>
        paths.Any(p => string.Equals(p, CurrentPath, StringComparison.OrdinalIgnoreCase));
}
```
<!-- /miko:example -->

## Tab pages

A tab page is an ordinary routed page with its own [`IonPage`](./app.md#page-structure), header, and content; it fills the area above the tab bar. Give the first tab the `"/"` route as well, so the app starts on it — and include `"/"` in that tab's `Selected` check.

<!-- miko:example examples/tabs/TabPage.razor -->
```razor
@* Pages/Tab1Page.razor — the real file starts with @page "/" and @page "/tab1". A tab page is
   an ordinary page; the layout supplies the tab bar. *@
<IonPage>
    <IonHeader>
        <IonToolbar>
            <IonTitle>Tab 1</IonTitle>
        </IonToolbar>
    </IonHeader>
    <IonContent>
        <div class="ion-padding">
            <p>This is the first tab page.</p>
        </div>
    </IonContent>
</IonPage>
```
<!-- /miko:example -->

A page pushed from a tab (for example through an [`IonItem`](./item.md) `Href`) renders in the same content area, so the tab bar stays visible. Unlike Ionic there is a single history stack, not one per tab: a pushed page can go back to its tab page, but a tab switch clears the stack, so the back button cannot undo it and the pushed page is not remembered when you return to that tab.

## Badges

An [`IonBadge`](./badge.md) inside a tab button floats over the top-right corner of the icon instead of flowing under the label. An empty badge is drawn as an 8px dot in md mode and stays hidden in ios mode.

<!-- miko:example examples/tabs/Badges.razor -->
```razor
@* A badge floats over the top-right of the icon. An empty badge is an 8px dot in md mode and
   hidden in ios mode. *@
<IonTabBar>
    <IonTabButton Tab="inbox" Href="/inbox" Selected="true">
        <IonIcon Icon="@Ionicons.MailOutline" />
        <IonLabel>Inbox</IonLabel>
        <IonBadge Color="danger">3</IonBadge>
    </IonTabButton>
    <IonTabButton Tab="calendar" Href="/calendar">
        <IonIcon Icon="@Ionicons.Calendar" />
        <IonLabel>Calendar</IonLabel>
        <IonBadge Color="primary"></IonBadge>
    </IonTabButton>
</IonTabBar>
```
<!-- /miko:example -->

## Top tab bar

Put the bar in the `Top` slot to place it above the content; `Slot="top"` on the bar moves its border to the bottom edge. A tab bar is always padded for the *bottom* safe-area inset and never for the top one, even at the top of the screen — adjust the padding of `.ion-tab-bar-top` in an app style if your target devices have insets.

<!-- miko:example examples/tabs/TopBar.razor -->
```razor
@* The IonTabs slot decides the position; Slot="top" only moves the border to the bottom edge. *@
<IonTabs>
    <Top>
        <IonTabBar Slot="top">
            <IonTabButton Tab="music" Href="/music" Selected="true">
                <IonLabel>Music</IonLabel>
            </IonTabButton>
            <IonTabButton Tab="movies" Href="/movies">
                <IonLabel>Movies</IonLabel>
            </IonTabButton>
        </IonTabBar>
    </Top>
    <Content>
        <IonPage>
            <IonContent>
                <div class="ion-padding">
                    <p>Music</p>
                </div>
            </IonContent>
        </IonPage>
    </Content>
</IonTabs>
```
<!-- /miko:example -->

## Requirements

- An `IonTabButton` navigates only inside an `IonTabs` (it receives the container as a cascading value). Outside it — for example in an `IonTabBar` that is not in an `IonTabs` slot — it only raises `OnClick`.
- `Href` must be a route the app defines; without `Href` a tap does not navigate.
- A tab bar in a layout is built once per navigation, so `Selected` must be computed from the current path rather than from a field set in `OnClick`.

## API

<!-- miko:api IonTabs IonTabBar IonTabButton -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonTabs

| Slot | Type | Description |
| --- | --- | --- |
| `Bottom` | `RenderFragment?` | Content rendered after the main tabs area, corresponding to Ionic's `slot="bottom"`. |
| `Content` | `RenderFragment?` | The routed page content (in a tabs layout, `@Body`), rendered in the growing `tabs-inner` area between the [`Top`](#iontabs) and [`Bottom`](#iontabs) slots. |
| `Top` | `RenderFragment?` | Content rendered before the main tabs area, corresponding to Ionic's `slot="top"`. |

### IonTabBar

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Slot` | `string` | `"bottom"` | Ionic's `slot` attribute on `ion-tab-bar`: `"bottom"` (default) or `"top"`, stamped as `ion-tab-bar-bottom` / `ion-tab-bar-top`. It only picks the border side (top border for a bottom bar, bottom border for a top bar); the [`IonTabs`](#iontabs) slot the bar is placed in (`Bottom` / `Top`) decides the position. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonTabButton

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Href` | `string?` | `null` | Navigation target for the tab button. When the button sits inside an [`IonTabs`](#iontabs), clicking it performs a root-level navigation to this target (history cleared, no page transition), mirroring Ionic's tab switching. Outside an [`IonTabs`](#iontabs) the button does not navigate. |
| `Selected` | `bool` | `false` | Whether this tab button is the currently selected one (stamps `tab-selected`). Not derived from the route — the app sets it, e.g. by comparing `NavigationManager.CurrentPath` with [`Href`](#iontabbutton). |
| `Tab` | `string?` | `null` | Logical tab identifier (e.g. `"tab1"`, Ionic `tab`). Kept for API parity: it is not used for selection or navigation (there is no `ion-tab` component). |

| Event | Type | Description |
| --- | --- | --- |
| `OnClick` | `EventCallback` | Invoked when the tab button is clicked. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

`Bar…` values style `IonTabBar`; `Button…` values style `IonTabButton` and the badge inside it. `ButtonIconSize` sizes an [`IonIcon`](./icon.md) inside a tab button, and an [`IonLabel`](./label.md) there takes the button's font size.

<!-- miko:tokens Tab -->
Theme token `IonicTheme.Tab` (`TabToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Tab = new TabToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `BarBackground` | `Color` | `#ffffff` | `#f7f7f7` |
| `BarBorderColor` | `Color` | `rgba(0, 0, 0, 0.07)` | `rgba(0, 0, 0, 0.2)` |
| `BarBorderWidth` | `float` | `1` | `1` |
| `BarColor` | `Color` | `#595959` | `#737373` |
| `BarColorSelected` | `Color` | `#0054e9` | `#0054e9` |
| `BarHeight` | `float` | `56` | `50` |
| `ButtonBadgeBorderRadius` | `float` | `8` | `10` |
| `ButtonBadgeFontSize` | `float` | `8` | `12` |
| `ButtonBadgeMinWidth` | `float` | `12` | `10` |
| `ButtonBadgePaddingBottom` | `float` | `2` | `1` |
| `ButtonBadgePaddingEnd` | `float` | `2` | `6` |
| `ButtonBadgePaddingStart` | `float` | `2` | `6` |
| `ButtonBadgePaddingTop` | `float` | `3` | `1` |
| `ButtonBadgeSizeEmpty` | `float` | `8` | `8` |
| `ButtonFontSize` | `float` | `12` | `10` |
| `ButtonIconSize` | `float` | `22` | `24` |
| `ButtonMaxWidth` | `float` | `168` | `240` |
| `ButtonPaddingX` | `float` | `12` | `2` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-tabs` | The `IonTabs` host: a flex column filling its parent. Also carries the mode (`md` / `ios`). |
| `.ion-tabs .tabs-inner` | The content area (`Content` slot), which grows to push the bar to the edge. |
| `.ion-tab-bar` | The bar: height, background, color, safe-area padding. |
| `.ion-tab-bar-bottom`, `.ion-tab-bar-top` | `IonTabBar.Slot`: border on the top or bottom edge. |
| `.ion-tab-button` | A tab button: icon stacked above the label. |
| `.ion-tab-button.tab-selected` | The selected button (selected color). |
| `.ion-tab-button .ion-badge` | A badge in a tab button. |

```csharp
// GlobalStyles.cs — bold label on the selected tab.
[".ion-tab-button.tab-selected .ion-label"] = new() { FontWeight = FontWeight.Bold },
```
