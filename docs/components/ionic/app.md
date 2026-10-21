# App and Page

`IonApp` is the root container of an Ionic app, and `IonPage` is the root of each routed page; together they set up the screen structure every other Ionic component expects.

`IonApp` wraps all routed pages, once, in the app's layout. It is a positioned flex column that fills its parent, and it provides:

- **The overlay layer.** It renders the overlay host, painted above the page, the tab bar, and the menu. Overlays presented through a controller (`IonModalController`, `IonAlertController`, …) are rendered *only* there — without an `IonApp` they never appear. Declarative overlays (`IonModal`, `IonAlert`, …) declared on a routed page or in the layout are drawn there as well. See [Overlays](./overlays.md).
- **The menu area.** It is the positioned box an [`IonMenu`](./menu.md) drawer and its backdrop cover.

`IonApp` does not handle the system back button itself — the components that respond to it (overlays, the menu, [`IonBackButton`](./back-button.md)) register with the platform's back-button chain. Its services come from `builder.AddIonic()`.

## App shell

Put a single `IonApp` in the default layout, around `@Body`. Every app template (blank, list, sidemenu, tabs) does this in `MainLayout.razor`; the tabs template nests [`IonTabs`](./tabs.md) inside it. The pages themselves then start with `IonPage`, not another `IonApp`.

<!-- miko:example examples/app/MainLayout.razor -->
```razor
@inherits LayoutComponentBase

@* MainLayout.razor, registered with builder.UseDefaultLayout<MainLayout>(): one IonApp around
   every routed page. *@
<IonApp>
    @Body
</IonApp>
```
<!-- /miko:example -->

## Page structure

Each routed page starts with `IonPage`: a flex column that fills the space it is given (the `IonApp`, or the content area of `IonTabs`). Inside it, an optional [`IonHeader`](./header.md) stays at the top, an optional [`IonFooter`](./footer.md) at the bottom, and [`IonContent`](./content.md) grows to fill the rest and is the only part that scrolls.

Make `IonPage` the page's root element and put anything else, an `IonMenu` included, inside it, as the app templates do. A page may also have several root elements; they are placed in the `IonApp` side by side.

<!-- miko:example examples/app/Page.razor -->
```razor
@* A routed page (the real file starts with @page "/inbox"). IonContent fills the space between
   the header and the footer and is the only part that scrolls. *@
<IonPage>
    <IonHeader>
        <IonToolbar>
            <IonTitle>Inbox</IonTitle>
        </IonToolbar>
    </IonHeader>

    <IonContent>
        <IonList>
            <IonItem><IonLabel>Welcome to Miko</IonLabel></IonItem>
            <IonItem><IonLabel>Your weekly summary</IonLabel></IonItem>
        </IonList>
    </IonContent>

    <IonFooter>
        <IonToolbar>
            <IonTitle>2 messages</IonTitle>
        </IonToolbar>
    </IonFooter>
</IonPage>
```
<!-- /miko:example -->

## Navigating to a page

Pages are reached by route (`@page "/messages/{id}"`). Navigation goes through the app's `NavigationManager` and has a direction: **Forward** pushes a page onto the history stack, **Back** pops it, **Root** clears the stack. An [`IonItem`](./item.md) with `Href` navigates on tap: Forward with the mode's push transition by default, or as `RouterDirection` says (`"back"` — pop transition, `"root"` — no transition). `OnClick` runs first.

<!-- miko:example examples/app/ItemLinks.razor -->
```razor
<IonList>
    @foreach (var message in _messages)
    {
        @* A tap pushes /messages/{id} with the mode's page transition. *@
        <IonItem Href="@($"/messages/{message.Id}")">
            <IonLabel>@message.Subject</IonLabel>
        </IonItem>
    }

    @* "root" clears the history and switches without a transition. *@
    <IonItem Href="/" RouterDirection="root">
        <IonLabel>Start over</IonLabel>
    </IonItem>
</IonList>

@code {
    private readonly (int Id, string Subject)[] _messages =
    {
        (1, "Welcome to Miko"),
        (2, "Your weekly summary"),
    };
}
```
<!-- /miko:example -->

## Going back

An [`IonBackButton`](./back-button.md) in the header pops the history stack with the mode's pop transition. When there is nothing to pop (the app was opened on this page), it navigates back to `DefaultHref` instead. Without `DefaultHref` the button is hidden.

<!-- miko:example examples/app/DetailPage.razor -->
```razor
@* The /messages/{id} page. The back button pops back to the list; when the app was opened
   directly on this page (nothing to pop), it navigates back to DefaultHref instead. *@
<IonPage>
    <IonHeader>
        <IonToolbar>
            <Start>
                <IonBackButton DefaultHref="/" />
            </Start>
            <ChildContent>
                <IonTitle>Message</IonTitle>
            </ChildContent>
        </IonToolbar>
    </IonHeader>

    <IonContent>
        <div class="ion-padding">
            <p>Thanks for trying Miko.</p>
        </div>
    </IonContent>
</IonPage>
```
<!-- /miko:example -->

## Tabs

Tab switching is root navigation: an [`IonTabButton`](./tabs.md) inside `IonTabs` calls `NavigateTo(Href, NavigationDirection.Root)` — the history is cleared and no transition plays, so a tab page has nothing to go back to. Each root page's scroll position is restored when you switch back to it.

## Navigating from code

Inject `Miko.Routing.NavigationManager` and call `NavigateTo(path, direction, transition)`, or `NavigateBack(transition)`, which returns `false` when there is nothing to pop. Routed pages and layouts also have a `NavigationManager` property, but nested components need `@inject`. `NavigateTo(path)` alone is a Forward push *without* a transition; pass `IonicPageTransitions.Push(mode)` / `Pop(mode)` to get the Ionic animation. `IonicModeResolver.ResolveClass` returns the mode the host platform implies; if the app forces a mode with `AddIonic(o => o.Platform = …)`, pass that mode instead.

<!-- miko:example examples/app/CodeNavigation.razor -->
```razor
@inject Miko.Routing.NavigationManager Navigation
@inject Miko.Platform.IPlatformInfo Platform

<IonButton Expand="block" OnClick="OpenSettings">Settings</IonButton>
<IonButton Expand="block" Fill="outline" OnClick="SignOut">Sign out</IonButton>

@code {
    // "ios" on iOS hosts, "md" elsewhere — the mode the Ionic components pick by default.
    private string Mode => IonicModeResolver.ResolveClass(Platform);

    // Forward: push onto the history stack, with Ionic's push transition for the mode.
    private void OpenSettings() =>
        Navigation.NavigateTo("/settings", Miko.Routing.NavigationDirection.Forward, IonicPageTransitions.Push(Mode));

    // Root: clear the history and switch without a transition.
    private void SignOut() =>
        Navigation.NavigateTo("/login", Miko.Routing.NavigationDirection.Root);
}
```
<!-- /miko:example -->

## Page transitions

`IonicPageTransitions` ports Ionic's page animations. The components use them automatically (`IonItem` `Href`, `IonBackButton`); code navigation passes them explicitly. Each transition moves and fades the leaving and entering pages as whole layers — it does not re-run layout — and Ionic's decorative iOS edge shadow is omitted. Navigating to the current path is a no-op.

| Transition | Duration | Easing | Effect |
| --- | --- | --- | --- |
| `Push("ios")` | 540 ms | `cubic-bezier(0.32, 0.72, 0, 1)` | The new page slides in from the right over the old one, which shifts 30% to the left. |
| `Pop("ios")` | 540 ms | `cubic-bezier(0.32, 0.72, 0, 1)` | The old page slides out to the right above the new one, which returns from 30% left. |
| `Push("md")` | 280 ms | `cubic-bezier(0.36, 0.66, 0.04, 1)` | The new page rises 40px and fades in over the old one, which stays still. |
| `Pop("md")` | 200 ms | `cubic-bezier(0.47, 0, 0.745, 0.715)` | The old page fades out above the new one. |
| Root (tabs) | — | — | No transition. |

Going back restores the scroll position of the page you return to; a Forward visit starts at the top.

## API

<!-- miko:api IonApp IonPage -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonApp

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonPage

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

`IonApp`'s background is the `AppBackground` value of the [Menu token](./menu.md#theme-tokens). `IonPage` has no token.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-app` | The `IonApp` host: a positioned flex column, 100% wide and high. Also carries the mode (`md` / `ios`). |
| `.ion-app .ion-overlay-host` | The overlay layer inside `IonApp` (absolutely positioned over it; pointer events pass through except on overlays). |
| `.ion-page` | The `IonPage` host: a flex column that fills its parent. |
