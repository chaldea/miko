# Back Button

A back button navigates to the previous page: it pops the navigation history when there is something to pop, and otherwise goes to a default page.

`IonBackButton` sits at the start of a page's header toolbar — in the [`IonToolbar`](./toolbar.md) `Start` slot, directly or inside `IonButtons`. It is its own component, not an `IonButton`.

## Basic usage

`DefaultHref` is required in practice: the button is **hidden unless `DefaultHref` is set**, even when there is history to go back to (Ionic also shows it whenever the stack can go back; Miko does not). On a tap it pops the history with the mode's pop transition; when the history cannot go back, it navigates to `DefaultHref` in the back direction with the same transition. When `DefaultHref` is already the current page, nothing happens. See [App and Page](./app.md#going-back).

<!-- miko:example examples/back-button/Basic.razor -->
```razor
@* DefaultHref is required: without it the button is hidden. *@
<IonToolbar>
    <Start>
        <IonBackButton DefaultHref="/" />
    </Start>
    <ChildContent>
        <IonTitle>Details</IonTitle>
    </ChildContent>
</IonToolbar>
```
<!-- /miko:example -->

## Text and icon

In ios mode the button shows a `chevron-back` icon and the text "Back"; in md mode only an `arrow-back-sharp` icon (as a 48×48 round icon-only button). `Text` replaces the label in both modes — so in md it adds one — and `Icon` replaces the icon.

<!-- miko:example examples/back-button/Custom.razor -->
```razor
<IonToolbar>
    <Start>
        @* A label in both modes (md shows none by default) and a different icon. *@
        <IonBackButton DefaultHref="/" Text="Inbox" Icon="@Ionicons.Close" />
    </Start>
    <ChildContent>
        <IonTitle>Message</IonTitle>
    </ChildContent>
</IonToolbar>
```
<!-- /miko:example -->

## Handling taps

`OnClick` runs before the default navigation, which follows once the handler (or its `Task`) completes. The handler cannot cancel the navigation. `Disabled` dims the button and skips both.

<!-- miko:example examples/back-button/Click.razor -->
```razor
<IonToolbar>
    <Start>
        <IonBackButton DefaultHref="/" OnClick="SaveDraft" />
    </Start>
    <ChildContent>
        <IonTitle>New message</IonTitle>
    </ChildContent>
</IonToolbar>

@code {
    private readonly List<string> _drafts = new();
    private string _text = "Hello";

    // Runs first; the back navigation follows once the handler (or its Task) completes.
    private void SaveDraft() => _drafts.Add(_text);
}
```
<!-- /miko:example -->

## System back button

While it is visible and enabled, the button also answers the system back button (Android back, back gesture, Escape on the desktop) with the same navigation as a tap — at the lowest priority, so an open overlay or menu takes the press first. If there is nowhere to go, the press is not consumed and the platform default (leaving the app) runs. `RespondToHardwareBack="false"` keeps the on-screen behavior but leaves the system back button to other handlers. `OnClick` is not raised for a system back press.

## API

<!-- miko:api IonBackButton -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonBackButton

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Only stamped as an `ion-color-*` class: the back-button stylesheet has no palette rules, so it does not change the color unless an app style targets that class. |
| `DefaultHref` | `string?` | `null` | The path to navigate back to when the history stack cannot go back. Also controls visibility: when null the button is hidden (no `show-back-button` class) even if there is history to pop. |
| `Disabled` | `bool` | `false` | When true, the user cannot interact with the button (dimmed, non-interactive). |
| `Icon` | `string?` | `null` | Override the default back button icon (an `Ionicons` name). When null, uses the mode default: `chevron-back` on iOS, `arrow-back-sharp` on MD. |
| `RespondToHardwareBack` | `bool` | `true` | Whether the system back button (hardware back / back gesture / Escape) navigation-pops the same way a tap on this button does. Defaults to `true`. Set to `false` when the page wants hardware back to behave differently from tapping the on-screen button. |
| `Text` | `string?` | `null` | Override the default back button text. When null, uses the mode default ("Back" for iOS, null for MD). |

| Event | Type | Description |
| --- | --- | --- |
| `OnClick` | `EventCallback` | Raised when the button is clicked (skipped when disabled). The default behavior still runs afterwards: nav-pop (with the mode's back transition) when the nav stack can go back, else a back-direction navigation to [`DefaultHref`](#ionbackbutton). |
<!-- /miko:api -->

## Styling

`IonBackButton` has no theme token. Its color is the palette's `Primary` in ios mode and the palette's `TextColor` in md mode. The `Color` parameter only stamps an `ion-color-*` class — no Ionic rule styles it, so target the class from an app stylesheet if you use it.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-back-button` | The host: hidden unless `.show-back-button` is present. Also carries the mode (`md` / `ios`) and `.button`. |
| `.show-back-button` | `DefaultHref` is set (the button is displayed). |
| `.back-button-disabled` | `Disabled`. |
| `.back-button-has-icon-only` | No text (md default): a round 48×48 button. |
| `.ion-back-button .button-native` | The clickable surface. |
| `.ion-back-button .button-inner` | The row holding the icon and the text. |
| `.ion-back-button .back-button-icon` | The icon. |
| `.ion-back-button .button-text` | The text. |
| `.ion-color-{name}` | `Color` (unstyled by Ionic). |

```csharp
// GlobalStyles.cs — a white back button on a colored toolbar.
[".ion-back-button"] = new() { Color = Color.FromHex("#ffffff") },
```
