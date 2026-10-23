# Menu

A menu is a navigation drawer that slides in from the side of the screen over the current page, with a dimmed backdrop behind it.

`IonMenu` is the drawer; `IonMenuButton` is the hamburger button that usually opens it from a toolbar. Both are **controlled**: the menu shows and hides only through its `IsOpen` parameter, and the button only raises `OnClick` — there is no menu controller, no `menuId` / `contentId` linking, and the button does not find or toggle a menu by itself.

## App layout

The drawer and its backdrop are absolutely positioned over their nearest positioned ancestor, which for a page is the layout's [`IonApp`](./app.md) — or, in a tabs layout, the tab content area. So the page that renders the menu must sit inside the app's `IonApp`.

<!-- miko:example examples/menu/MainLayout.razor -->
```razor
@inherits LayoutComponentBase

@* IonApp is the box the menu covers: the drawer and backdrop fill it. *@
<IonApp>
    @Body
</IonApp>
```
<!-- /miko:example -->

## Side menu

The page owns a `bool` for the open state: `IonMenuButton.OnClick` sets it, `IonMenu.OnClose` — raised by a tap on the backdrop and by the system back button while the menu is open — clears it. Make the `IonMenu` the last child of the page's `IonPage` (see [Page structure](./app.md#page-structure)); the drawer is a flex column, so an `IonHeader` / `IonContent` inside it lays out like a page. Handlers defined in the page re-render it when they return, so they only need to flip the flag.

<!-- miko:example examples/menu/Home.razor -->
```razor
@* The page (the real file starts with @page "/"). IonMenu is the last child of the IonPage, so
   the page keeps a single root. The page owns the open state: IonMenuButton opens the menu,
   IonMenu.OnClose (backdrop tap or system back) closes it. *@
<IonPage>
    <IonHeader>
        <IonToolbar>
            <Start>
                <IonButtons>
                    <IonMenuButton OnClick="OpenMenu" />
                </IonButtons>
            </Start>
            <ChildContent>
                <IonTitle>Inbox</IonTitle>
            </ChildContent>
        </IonToolbar>
    </IonHeader>

    <IonContent>
        <div class="ion-padding">
            <p>Tap the menu button to slide the drawer in.</p>
        </div>
    </IonContent>

    <IonMenu IsOpen="_menuOpen" Side="start" OnClose="CloseMenu">
        <IonContent>
            <IonList>
                <IonListHeader>Inbox</IonListHeader>
                @foreach (var item in _pages)
                {
                    <IonItem Lines="none">
                        <Start>
                            <IonIcon Icon="@item.Icon" />
                        </Start>
                        <ChildContent>
                            <IonLabel>@item.Title</IonLabel>
                        </ChildContent>
                    </IonItem>
                }
            </IonList>
        </IonContent>
    </IonMenu>
</IonPage>

@code {
    private bool _menuOpen;

    private readonly MenuPage[] _pages =
    {
        new("Inbox", Ionicons.MailOutline),
        new("Outbox", Ionicons.PaperPlaneOutline),
        new("Favorites", Ionicons.HeartOutline),
        new("Archived", Ionicons.ArchiveOutline),
        new("Trash", Ionicons.TrashOutline),
        new("Spam", Ionicons.WarningOutline),
    };

    // Defined in this page, so the EventCallbacks re-render the page when they return.
    private void OpenMenu() => _menuOpen = true;

    private void CloseMenu() => _menuOpen = false;

    private readonly record struct MenuPage(string Title, string Icon);
}
```
<!-- /miko:example -->

The sidemenu app template (`Pages/Home.sidemenu.razor`) is built the same way.

Opening and closing animate both ways: the drawer slides and the backdrop fades. As soon as `IsOpen` is `false` taps pass through to the page, also while the backdrop is still fading out.

## Side

`Side="start"` (default) anchors the drawer to the left edge, `Side="end"` to the right. `Type` accepts only `"overlay"` — the drawer slides over the page; Ionic's `"reveal"` and `"push"` are not implemented. The drawer content is padded for the top and bottom safe-area insets.

<!-- miko:example examples/menu/EndSide.razor -->
```razor
@* A drawer on the right edge, with its own header, opened from a toolbar button. *@
<IonPage>
    <IonHeader>
        <IonToolbar>
            <ChildContent>
                <IonTitle>Messages</IonTitle>
            </ChildContent>
            <End>
                <IonButtons>
                    <IonButton OnClick="@(() => _filtersOpen = true)">Filters</IonButton>
                </IonButtons>
            </End>
        </IonToolbar>
    </IonHeader>

    <IonContent>
        <div class="ion-padding">
            <p>@(_unreadOnly ? "Unread messages" : "All messages")</p>
        </div>
    </IonContent>

    <IonMenu Side="end" IsOpen="_filtersOpen" OnClose="@(() => _filtersOpen = false)">
        <IonHeader>
            <IonToolbar>
                <IonTitle>Filters</IonTitle>
            </IonToolbar>
        </IonHeader>
        <IonContent>
            <IonList>
                <IonItem>
                    <IonToggle @bind-Checked="_unreadOnly">Unread only</IonToggle>
                </IonItem>
            </IonList>
        </IonContent>
    </IonMenu>
</IonPage>

@code {
    private bool _filtersOpen;
    private bool _unreadOnly;
}
```
<!-- /miko:example -->

## Closing

The menu never closes itself: a backdrop tap and the system back button only raise `OnClose`, and the host sets `IsOpen` to `false`. The system back button is claimed only while the menu is open and `OnClose` has a handler (`BackButtonCloses="false"` opts out); an open overlay gets the back press first, page navigation (an [`IonBackButton`](./back-button.md)) after the menu. To close the menu after a tap on one of its items, clear the flag in the item's `OnClick`.

## API

<!-- miko:api IonMenu IonMenuButton -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonMenu

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `BackButtonCloses` | `bool` | `true` | Whether the system back button closes the open menu. Defaults to `true`; only takes effect while [`IsOpen`](#ionmenu) and when a host has subscribed to [`OnClose`](#ionmenu) — with nobody listening there is nothing to close. |
| `IsOpen` | `bool` | `false` | Whether the drawer is open (slid on-screen, backdrop dimmed). |
| `Side` | `string` | `"start"` | Edge the drawer is anchored to: `"start"` (default, left) or `"end"` (right). |

| Event | Type | Description |
| --- | --- | --- |
| `OnClose` | `EventCallback` | Invoked when the backdrop is tapped, or the system back button is pressed while the menu is open, asking the host to close the menu (set [`IsOpen`](#ionmenu) to false). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonMenuButton

| Event | Type | Description |
| --- | --- | --- |
| `OnClick` | `EventCallback` | Invoked when the hamburger button is clicked. The button does not find or toggle a menu by itself — open it here by setting the [`IonMenu`](#ionmenu)'s `IsOpen`. |
<!-- /miko:api -->

## Styling

### Theme tokens

`Width` is the drawer width, `AnimDuration` the slide and fade duration in seconds. `BorderWidth` / `BorderColor` draw the ios hairline on the drawer's edge that faces the page (the right edge for `Side="start"`, the left for `Side="end"`). `AppBackground` is the background of [`IonApp`](./app.md). The backdrop color is the palette's `BackdropColor` (see [Theming](./theming.md#palette)).

<!-- miko:tokens Menu -->
Theme token `IonicTheme.Menu` (`MenuToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Menu = new MenuToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `AnimDuration` | `float` | `0.28` | `0.28` |
| `AppBackground` | `Color` | `#ffffff` | `#ffffff` |
| `Background` | `Color` | `#ffffff` | `#ffffff` |
| `BorderColor` | `Color` | `transparent` | `rgba(0, 0, 0, 0.2)` |
| `BorderWidth` | `float` | `0` | `0.55` |
| `BoxShadow` | `List<BoxShadow>` | `0 2px 4px -1px rgba(0, 0, 0, 0.2), 0 4px 5px 0 rgba(0, 0, 0, 0.14), 0 1px 10px 0 rgba(0, 0, 0, 0.12)` | `none` |
| `Width` | `float` | `304` | `270` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-menu-host` | The full-size layer holding drawer and backdrop. Also carries the mode (`md` / `ios`), `.ion-menu-host-start` / `-end`, and the state: `.ion-menu-host-open`, or `-idle` (closed; taps pass through). |
| `.ion-menu-host .ion-menu-inner` | The drawer: width, background, shadow (md) or border (ios). Also `.ion-menu-inner-start` / `-end`, and `.ion-menu-inner-open` while open. |
| `.ion-menu-host .ion-menu-backdrop` | The dimmed backdrop; `.ion-menu-backdrop-open` while open. |
| `.ion-menu-button` | The `IonMenuButton` host: a 48×48 tap target in the toolbar's text color. |

```csharp
// GlobalStyles.cs — a dark drawer.
[".ion-menu-host .ion-menu-inner"] = new() { BackgroundColor = Color.FromHex("#1f1f1f") },
```
