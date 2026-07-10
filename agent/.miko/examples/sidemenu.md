# Example: Side-menu layout (Ionic)

An Ionic slide-in drawer over a page, with open/close state and animation. This is what `dotnet new miko-razor --layout sidemenu` scaffolds. Requires `builder.AddIonic()` and the Ionic `@using`s.

## Key idea

`<IonApp>` lays out the `<IonPage>` and the `<IonMenu>` as siblings. Author the menu **after** the page so hit-testing reaches the full-screen menu host first. The page owns the `_menuOpen` flag; toggling it (and calling `StateHasChanged()`) drives the drawer. `IonMenu` owns its own backdrop and unmount timing. Because nested components don't get DI, the page injects `AnimationManager` and passes it to `IonMenu`.

## `Pages/Home.razor`

```razor
@page "/"
@namespace MyApp.Pages
@inject Miko.Animation.AnimationManager Animations

<IonApp>
    <IonPage>
        <IonHeader>
            <IonToolbar>
                <IonButtons Slot="start">
                    <IonMenuButton OnClick="OpenMenu" />
                </IonButtons>
                <IonTitle>Inbox</IonTitle>
            </IonToolbar>
        </IonHeader>

        <IonContent Fullscreen="true">
            <div class="demo-content">
                <p>Tap the menu button to slide the drawer in. Tap the backdrop to close.</p>
            </div>
        </IonContent>
    </IonPage>

    <IonMenu IsOpen="_menuOpen" Side="start" Type="overlay" OnClose="CloseMenu" Animations="Animations">
        <IonContent>
            <IonList Id="inbox-list">
                <IonListHeader>Inbox</IonListHeader>
                @foreach (var item in _pages)
                {
                    <IonItem Lines="none">
                        <IonIcon Icon="@item.Icon" />
                        <IonLabel>@item.Title</IonLabel>
                    </IonItem>
                }
            </IonList>
        </IonContent>
    </IonMenu>
</IonApp>

@code {
    private bool _menuOpen;

    private readonly MenuPage[] _pages =
    {
        new("Inbox",     Ionicons.MailOutline),
        new("Outbox",    Ionicons.PaperPlaneOutline),
        new("Favorites", Ionicons.HeartOutline),
        new("Archived",  Ionicons.ArchiveOutline),
        new("Trash",     Ionicons.TrashOutline),
    };

    // This page owns _menuOpen and renders IonMenu, so it must call StateHasChanged()
    // when the child EventCallbacks fire (they re-render the children, not this page).
    private void OpenMenu()  { _menuOpen = true;  StateHasChanged(); }
    private void CloseMenu() { _menuOpen = false; StateHasChanged(); }

    private readonly record struct MenuPage(string Title, string Icon);
}
```

## `MainLayout.razor`

The shell lives in the page (so menu state + button + drawer share one component). The layout just hosts the routed page full-size:

```razor
@inherits Miko.Components.LayoutComponentBase
@namespace MyApp

<div class="app-root">
    @Body
</div>
```
