# Example: Tabs layout (Ionic)

An Ionic bottom tab bar with three routed tab pages sharing one layout. This is what `dotnet new miko-razor --layout tabs` scaffolds. Requires `builder.AddIonic()` and the Ionic `@using`s (see below).

## `_Imports.razor`

```razor
@using Miko.Components
@using Miko.Core
@using Miko.Core.DomElements
@using Miko.Common
@using Miko.Styling
@using Miko.Styling.Selectors
@using Miko.Ionic
@using Miko.Ionic.Components
```

## `MainLayout.razor` — the tab shell

The layout renders `<IonTabs>`: `Content` hosts the routed page (`@Body`); `Bar` is the tab bar. Each `IonTabButton` navigates to its route and marks itself selected on the current path.

```razor
@inherits Miko.Components.LayoutComponentBase
@namespace MyApp

<IonTabs>
    <Content>@Body</Content>
    <Bar>
        <IonTabBar Slot="bottom">
            <IonTabButton Tab="tab1" Href="/tab1" Selected="@IsActive("/tab1", "/")" OnClick="@(() => NavigateTo("/tab1"))">
                <IonIcon Icon="@Ionicons.Triangle" />
                <IonLabel>Tab 1</IonLabel>
            </IonTabButton>
            <IonTabButton Tab="tab2" Href="/tab2" Selected="@IsActive("/tab2")" OnClick="@(() => NavigateTo("/tab2"))">
                <IonIcon Icon="@Ionicons.Ellipse" />
                <IonLabel>Tab 2</IonLabel>
            </IonTabButton>
            <IonTabButton Tab="tab3" Href="/tab3" Selected="@IsActive("/tab3")" OnClick="@(() => NavigateTo("/tab3"))">
                <IonIcon Icon="@Ionicons.Square" />
                <IonLabel>Tab 3</IonLabel>
            </IonTabButton>
        </IonTabBar>
    </Bar>
</IonTabs>

@code {
    private string CurrentPath => NavigationManager?.CurrentPath ?? "/";

    private bool IsActive(params string[] paths) =>
        paths.Any(p => string.Equals(p, CurrentPath, StringComparison.OrdinalIgnoreCase));

    private void NavigateTo(string path) => NavigationManager?.NavigateTo(path);
}
```

`NavigationManager` is available on `LayoutComponentBase`; each tab swaps `@Body` while the bar stays put.

## A tab page — `Pages/Tab1Page.razor`

```razor
@page "/"
@page "/tab1"
@namespace MyApp.Pages

<IonPage>
    <IonHeader>
        <IonToolbar>
            <IonTitle>Tab 1</IonTitle>
        </IonToolbar>
    </IonHeader>
    <IonContent Fullscreen="true">
        <div class="demo-content">
            <p>This is the first tab page, served at <strong>/tab1</strong>.</p>
        </div>
    </IonContent>
</IonPage>
```

Add `Tab2Page.razor` (`@page "/tab2"`) and `Tab3Page.razor` (`@page "/tab3"`) the same way. `App.cs` calls `builder.AddIonic();` and typically a phone-portrait `builder.UseSize(390, 844);`.
