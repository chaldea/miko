# Content

Content is the scrollable main area of a page, between the header and the footer.

`IonContent` grows to fill the height its parent leaves — the [`IonPage`](./app.md#page-structure) flex column, an [`IonMenu`](./menu.md) drawer, or a modal — and scrolls its children inside. It has no height of its own: outside such a parent (or a box with a definite height) it collapses to zero height. Use one `IonContent` per page.

## Basic usage

Only the inner scroll container scrolls; the header and footer stay put. Vertical scrolling is on by default; `ScrollX="true"` adds horizontal scrolling and `ScrollY="false"` turns vertical scrolling off.

<!-- miko:example examples/content/Basic.razor -->
```razor
@* IonContent needs a parent that gives it a height — IonPage (or a menu drawer / modal). *@
<IonPage>
    <IonHeader>
        <IonToolbar>
            <IonTitle>Games</IonTitle>
        </IonToolbar>
    </IonHeader>

    <IonContent>
        <IonList>
            @foreach (var game in _games)
            {
                <IonItem><IonLabel>@game</IonLabel></IonItem>
            }
        </IonList>
    </IonContent>
</IonPage>

@code {
    private readonly string[] _games =
    {
        "Pokémon Yellow", "Mega Man X", "The Legend of Zelda", "Pac-Man", "Super Mario World",
        "Street Fighter II", "Chrono Trigger", "Secret of Mana", "Final Fantasy VI",
    };
}
```
<!-- /miko:example -->

## Padding

`Class="ion-padding"` on `IonContent` insets the scrolled content by 16px, as in Ionic; `ion-padding-top`, `-bottom`, `-start`, `-end`, `-vertical` and `-horizontal` pad single sides. The host itself keeps no padding — the scroll container inside it is padded. A wrapper with the class works too.

<!-- miko:example examples/content/Padding.razor -->
```razor
@* ion-padding on IonContent insets the scrolled content by 16px on every side. *@
<IonPage>
    <IonContent Class="ion-padding">
        <h2>Welcome</h2>
        <p>The content is inset by 16px on every side.</p>
    </IonContent>
</IonPage>
```
<!-- /miko:example -->

## Colors

`Color` fills the content area with a palette color and switches the text to its contrast color. Colors come from the theme's palette — see [Theming](./theming.md#palette).

<!-- miko:example examples/content/Colors.razor -->
```razor
<IonPage>
    <IonContent Color="light">
        <div class="ion-padding">
            <p>The background and text color come from the "light" palette entry.</p>
        </div>
    </IonContent>
</IonPage>
```
<!-- /miko:example -->

## Fixed content

The `Fixed` slot renders outside the scroll container, so its content stays in place while the rest scrolls — typically an [`IonFab`](./fab.md). Each top-level element of the slot is absolutely positioned and picks its own offsets. `FixedSlotPlacement="before"` renders the slot before the scroll container instead of after it.

<!-- miko:example examples/content/Fixed.razor -->
```razor
@* Fixed content sits outside the scroll container, so the fab stays put while the list scrolls. *@
<IonPage>
    <IonContent>
        <Fixed>
            <IonFab Vertical="bottom" Horizontal="end">
                <IonFabButton>
                    <IonIcon Icon="@Ionicons.Add" />
                </IonFabButton>
            </IonFab>
        </Fixed>
        <ChildContent>
            <IonList>
                @for (var i = 1; i <= 30; i++)
                {
                    <IonItem><IonLabel>Note @i</IonLabel></IonItem>
                }
            </IonList>
        </ChildContent>
    </IonContent>
</IonPage>
```
<!-- /miko:example -->

## Elastic overscroll

With `ForceOverscroll`, dragging past the top or bottom edge stretches the content and springs it back on release, even when the content is shorter than the screen. It defaults to on in ios mode and off in md mode. The bounce needs the platform's elastic scroll behavior, which the Android, iOS, and simulator hosts install and a plain desktop host does not; it follows touch drags, not the mouse wheel.

<!-- miko:example examples/content/Overscroll.razor -->
```razor
@* Rubber-band bounce at both edges, also in md mode (it is on by default only on iOS). *@
<IonPage>
    <IonContent ForceOverscroll="true">
        <IonList>
            <IonItem><IonLabel>Drag past the top edge…</IonLabel></IonItem>
            <IonItem><IonLabel>…or past the bottom edge</IonLabel></IonItem>
        </IonList>
    </IonContent>
</IonPage>
```
<!-- /miko:example -->

## Scroll events and methods

`IonContent` raises no scroll events and has no scroll methods: Ionic's `scrollEvents` / `ionScroll` events and `scrollToTop()` / `scrollToBottom()` are not available. For scroll-driven loading and pull-to-refresh use [`IonInfiniteScroll`](./infinite-scroll.md) and [`IonRefresher`](./refresher.md), which work inside `IonContent`. Ionic's `fullscreen` is not ported either: the content never scrolls behind the header or footer.

## API

<!-- miko:api IonContent -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonContent

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as `ion-color` + `ion-color-*` classes the stylesheet maps to the scroll container's fill and text color. |
| `FixedSlotPlacement` | `string` | `"after"` | Where the fixed slot sits relative to the scrollable content in the DOM (Ionic `fixedSlotPlacement`): `"after"` (default) or `"before"`. Ionic uses this to control keyboard focus order. |
| `ForceOverscroll` | `bool?` | `null` | If true, scrolling bounces past the bounds even when the content does not overflow (Ionic `forceOverscroll`). When null, Ionic defaults it to true on iOS. Stamps the `overscroll` marker class, which `ContentStyles` maps to `overscroll-effect: elastic`. The bounce additionally requires an `ElasticScrollBehavior` on the platform host — the Android / iOS / Simulator hosts install one by default, a plain desktop host does not. |
| `ScrollX` | `bool` | `false` | If true, the content scrolls on the X axis (Ionic `scrollX`, default false). |
| `ScrollY` | `bool` | `true` | If false, vertical scrolling is disabled (Ionic `scrollY`, default true). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
| `Fixed` | `RenderFragment?` | Content for Ionic's `fixed` slot — placed outside the scroll container so it stays put while the main content scrolls (typically an `IonFab`). |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Content -->
Theme token `IonicTheme.Content` (`ContentToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Content = new ContentToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `#ffffff` | `#ffffff` |
| `Color` | `Color` | `#000000` | `#000000` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-content` | The host: fills the remaining height. Also carries the mode (`md` / `ios`) and `.content-ltr`. |
| `.ion-content .background-content` | The background layer behind the scroll container. |
| `.ion-content .inner-scroll` | The scroll container holding the default slot. |
| `.inner-scroll.scroll-y`, `.inner-scroll.scroll-x` | `ScrollY` / `ScrollX`: the axes that scroll. |
| `.overscroll` | `ForceOverscroll` in effect (on the host and on `.inner-scroll`). |
| `.ion-slot-fixed` | Each top-level element of the `Fixed` slot. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-light`). |

```csharp
// GlobalStyles.cs — a tinted page background: <IonContent Class="tinted">…</IonContent>
[".ion-content.tinted .background-content"] = new() { BackgroundColor = Color.FromHex("#f4f5f8") },
[".ion-content.tinted .inner-scroll"] = new() { BackgroundColor = Color.FromHex("#f4f5f8") },
```
