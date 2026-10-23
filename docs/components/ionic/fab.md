# Floating Action Button

A floating action button (FAB) is a circular button that floats above the content at a corner, edge, or the center of the screen, for the screen's primary action; it can also open lists of related actions.

`IonFab` is the positioned container; it holds one main `IonFabButton` and optionally one or more `IonFabList`s, each a row or column of smaller `IonFabButton`s revealed when the fab is activated.

## Basic usage

`IonFab` is absolutely positioned. Put it in [`IonContent`](./content.md)'s `Fixed` slot, which places it outside the scrolling area so it stays put while the content scrolls, positioned against the content box. Anywhere else it is positioned against its nearest positioned ancestor.

<!-- miko:example examples/fab/Basic.razor -->
```razor
@* The fab goes in IonContent's Fixed slot, so it stays put while the content scrolls. *@
<IonContent>
    <Fixed>
        <IonFab Vertical="bottom" Horizontal="end">
            <IonFabButton OnClick="Compose">
                <IonIcon Icon="@Ionicons.Add" />
            </IonFabButton>
        </IonFab>
    </Fixed>
    <ChildContent>
        <p class="ion-padding">Drafts: @_drafts</p>
    </ChildContent>
</IonContent>

@code {
    private int _drafts;

    private void Compose() => _drafts++;
}
```
<!-- /miko:example -->

## Position

`Horizontal` (`start`, `center`, `end`) and `Vertical` (`top`, `center`, `bottom`) place the fab; `start`/`end`/`top`/`bottom` keep a 10px gap (the `ContentMargin` token) to that edge. An axis left unset gets no inset, so on that axis the fab stays where it would sit in normal flow.

<!-- miko:example examples/fab/Position.razor -->
```razor
<IonContent>
    <Fixed>
        <IonFab Vertical="top" Horizontal="start">
            <IonFabButton Color="secondary"><IonIcon Icon="@Ionicons.Add" /></IonFabButton>
        </IonFab>
        <IonFab Vertical="center" Horizontal="center">
            <IonFabButton><IonIcon Icon="@Ionicons.Add" /></IonFabButton>
        </IonFab>
        <IonFab Vertical="bottom" Horizontal="end">
            <IonFabButton Color="danger"><IonIcon Icon="@Ionicons.Add" /></IonFabButton>
        </IonFab>
    </Fixed>
</IonContent>
```
<!-- /miko:example -->

## Edge

`Edge="true"` with `Vertical="top"` (or `"bottom"`) shifts the main button by half its height so it straddles the line between the content and the header (or footer). The fab is painted and hit-tested over the header.

<!-- miko:example examples/fab/Edge.razor -->
```razor
@* Edge pulls the button up by half its height so it straddles the header line. *@
<IonPage>
    <IonHeader>
        <IonToolbar>
            <IonTitle>Inbox</IonTitle>
        </IonToolbar>
    </IonHeader>
    <IonContent>
        <Fixed>
            <IonFab Vertical="top" Horizontal="end" Edge="true">
                <IonFabButton>
                    <IonIcon Icon="@Ionicons.Pencil" />
                </IonFabButton>
            </IonFab>
        </Fixed>
    </IonContent>
</IonPage>
```
<!-- /miko:example -->

## Fab lists

An `IonFabList` is hidden until the fab is activated. `Side` puts it above (`top`), below (`bottom`, the default), before (`start`), or after (`end`) the main button; a fab can hold one list per side. Buttons inside a list are drawn at the 40px small size with the list-button colors (`ListButtonBackground` / `ListButtonColor` tokens) unless they set `Color`.

Tapping any button in the fab toggles `Activated` — but only when the fab contains a list. When activated, the main button cross-fades to a close icon (`CloseIcon`, default `Ionicons.Close`) and the list buttons scale in. A tapped list button runs its own `OnClick` first, then the fab closes. `@bind-Activated` lets the page observe or set the state; Ionic staggers the buttons' entrance, Miko animates them together.

<!-- miko:example examples/fab/List.razor -->
```razor
@* Tapping the main button opens the list (it turns into a close icon); tapping any button closes it again. *@
<IonContent>
    <Fixed>
        <IonFab Vertical="bottom" Horizontal="end" @bind-Activated="_open">
            <IonFabButton>
                <IonIcon Icon="@Ionicons.Share" />
            </IonFabButton>
            <IonFabList Side="top">
                <IonFabButton OnClick="@(() => ShareTo("Twitter"))">
                    <IonIcon Icon="@Ionicons.LogoTwitter" />
                </IonFabButton>
                <IonFabButton OnClick="@(() => ShareTo("Facebook"))">
                    <IonIcon Icon="@Ionicons.LogoFacebook" />
                </IonFabButton>
            </IonFabList>
            <IonFabList Side="start">
                <IonFabButton OnClick="@(() => ShareTo("Mail"))">
                    <IonIcon Icon="@Ionicons.Mail" />
                </IonFabButton>
            </IonFabList>
        </IonFab>
    </Fixed>
    <ChildContent>
        <p class="ion-padding">@(_open ? "Choose a target" : $"Last shared to: {_target}")</p>
    </ChildContent>
</IonContent>

@code {
    private bool _open;
    private string _target = "nothing yet";

    private void ShareTo(string target) => _target = target;
}
```
<!-- /miko:example -->

## Button variants

`Size="small"` gives a 40px mini button, `Color` takes a palette color, and `Disabled` dims the button and blocks taps. `IonFabButton` also renders on its own, outside an `IonFab`.

<!-- miko:example examples/fab/Buttons.razor -->
```razor
@* A fab button works on its own too: small size, colors, disabled. *@
<IonFabButton Size="small">
    <IonIcon Icon="@Ionicons.Add" />
</IonFabButton>
<IonFabButton Color="success">
    <IonIcon Icon="@Ionicons.Star" />
</IonFabButton>
<IonFabButton Disabled="true">
    <IonIcon Icon="@Ionicons.Trash" />
</IonFabButton>
```
<!-- /miko:example -->

## API

<!-- miko:api IonFab IonFabButton IonFabList -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonFab

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Activated` | `bool` | `false` | If true, the main button shows the close icon and all fab-lists are visible. Tapping any button in the fab toggles it, but only when the fab contains an [`IonFabList`](#ionfablist). Two-way bindable: `@bind-Activated`. |
| `Edge` | `bool` | `false` | If true, the fab sits on the edge of the header (`vertical="top"`) or footer (`vertical="bottom"`). Stamped as `fab-edge`. |
| `Horizontal` | `string?` | `null` | Where to align the fab horizontally: `"start"`, `"end"`, or `"center"`. Stamped as `fab-horizontal-*`. |
| `Vertical` | `string?` | `null` | Where to align the fab vertically: `"top"`, `"bottom"`, or `"center"`. Stamped as `fab-vertical-*`. |

| Event | Type | Description |
| --- | --- | --- |
| `ActivatedChanged` | `EventCallback<bool>` | Raised with the new activated state when the fab is toggled. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonFabButton

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Activated` | `bool` | `false` | If true, the button shows the close icon (Ionic `activated`). Normally derived from the enclosing fab, but can be set directly for a standalone button. |
| `CloseIcon` | `string` | `"close"` | Icon name shown when the button is the active close button (Ionic `closeIcon`). Defaults to the Ionicons `close` glyph. |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as `ion-color` + `ion-color-*`. |
| `Disabled` | `bool` | `false` | If true, the user cannot interact with the button (Ionic `disabled`). |
| `Href` | `string?` | `null` | Route to navigate to (Ionic `href`). When set, the native surface is an `a`, and a tap raises [`OnClick`](#ionfabbutton), toggles the enclosing fab and then navigates through the app's `NavigationManager` with the mode's page transition. |
| `RouterDirection` | `string?` | `null` | Navigation direction when [`Href`](#ionfabbutton) is clicked (mirrors Ionic's `routerDirection`): `"forward"` (default — push with transition), `"back"` (pop-style reverse transition), or `"root"` (clear the history stack, no transition). |
| `Show` | `bool` | `false` | If true, a list button is shown (Ionic `show`). Normally derived from the enclosing fab-list's activated state. |
| `Size` | `string?` | `null` | Size (Ionic `size`): `"small"` for a mini fab button. Stamped as `fab-button-small`. |

| Event | Type | Description |
| --- | --- | --- |
| `OnClick` | `EventCallback` | Raised when the button is tapped (skipped when disabled). Fires before the fab toggle. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonFabList

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Activated` | `bool` | `false` | If true, the list shows its buttons (Ionic `activated`). Normally derived from the enclosing fab; can be set directly for a standalone list. |
| `Side` | `string` | `"bottom"` | The side the list shows on relative to the main button (Ionic `side`): `"start"`, `"end"`, `"top"`, or `"bottom"` (default). Stamped as `fab-list-side-*`. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

`IonFabButton.Activated` and `IonFabList.Activated` are normally derived from the enclosing `IonFab`; set them directly only for a standalone button or list. With `Href`, a tap on `IonFabButton` navigates like [`IonItem`](./item.md) (after `OnClick` and the fab toggle); `RouterDirection` picks the direction.

## Styling

### Theme tokens

<!-- miko:tokens Fab -->
Theme token `IonicTheme.Fab` (`FabToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Fab = new FabToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `#0054e9` | `#0054e9` |
| `BoxShadow` | `List<BoxShadow>` | `0 3px 5px -1px rgba(0, 0, 0, 0.2), 0 6px 10px 0 rgba(0, 0, 0, 0.14), 0 1px 18px 0 rgba(0, 0, 0, 0.12)` | `0 4px 16px 0 rgba(0, 0, 0, 0.12)` |
| `ButtonSmallMargin` | `float` | `8` | `8` |
| `Color` | `Color` | `#ffffff` | `#ffffff` |
| `ContentMargin` | `float` | `10` | `10` |
| `HoverOpacity` | `float` | `0.08` | `0.16` |
| `IconFontSize` | `float` | `24` | `28` |
| `ListButtonBackground` | `Color` | `#f4f5f8` | `#f4f5f8` |
| `ListButtonColor` | `Color` | `#000000` | `#000000` |
| `ListButtonIconSize` | `float` | `18` | `18` |
| `ListMargin` | `float` | `10` | `10` |
| `Size` | `float` | `56` | `56` |
| `SmallSize` | `float` | `40` | `40` |
| `TransitionDuration` | `float` | `0.3` | `0.3` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-fab` | The positioned container. Also carries the mode (`md` / `ios`) and the classes below. |
| `.fab-horizontal-{value}`, `.fab-vertical-{value}` | `Horizontal`, `Vertical` (e.g. `.fab-horizontal-end`). |
| `.fab-edge` | `Edge`. |
| `.ion-fab-button` | A fab button host (56px, or 40px when small / in a list). |
| `.ion-fab-button .button-native` | The painted round surface: background, color, shadow. |
| `.ion-fab-button .button-inner` | Centers the slotted icon. |
| `.ion-fab-button .close-icon` | The close icon shown while the fab is activated. |
| `.fab-button-small`, `.fab-button-disabled` | `Size="small"`, `Disabled`. |
| `.fab-button-close-active` | The main button while the fab is activated. |
| `.fab-button-in-list`, `.fab-button-show` | A button inside an `IonFabList`; shown while the list is open. |
| `.ion-color-{name}` | `Color` on a fab button (e.g. `.ion-color-danger`). |
| `.ion-fab-list` | A fab list (`display: none` until active). |
| `.fab-list-side-{side}`, `.fab-list-active` | `Side` (e.g. `.fab-list-side-top`); the list is open. |

```csharp
// GlobalStyles.cs — a brand-colored fab button: <IonFabButton Class="compose">…</IonFabButton>
[".ion-fab-button.compose .button-native"] = new()
{
    BackgroundColor = Color.FromHex("#0e7490"),
},
```
