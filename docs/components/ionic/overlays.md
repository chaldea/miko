# Overlays

Overlays are views that open above the page: action sheets, alerts, loading indicators, modals, popovers, and toasts. All of them except the toast block the page until they close. Each one can be declared in markup and shown through its `IsOpen` parameter, or created from code through a controller service. Both ways report the same dismiss result. Overlays are drawn in the overlay layer that [`IonApp`](./app.md) hosts, whether they are declared on a routed page, in the layout, or presented through a controller.

| Overlay | Page | Controller | Backdrop | System back button |
| --- | --- | --- | --- | --- |
| `IonActionSheet` | [Action Sheet](./action-sheet.md) | `IonActionSheetController` | yes | closes it |
| `IonAlert` | [Alert](./alert.md) | `IonAlertController` | yes | closes it |
| `IonLoading` | [Loading](./loading.md) | `IonLoadingController` | yes (taps ignored by default) | closes it only with `BackdropDismiss` |
| `IonModal` | [Modal](./modal.md) | `IonModalController` | yes | closes it |
| `IonPopover` | [Popover](./popover.md) | `IonPopoverController` | yes | closes it |
| `IonToast` | [Toast](./toast.md) | `IonToastController` | none, the page stays usable | — |

As in Ionic, the system back button closes an open overlay when its `BackdropDismiss` allows it, with the dismiss role `"back"`; a toast never answers it.

[`IonSelect`](./select.md) opens its options in one of these overlays, and the same rules apply to it.

## The overlay layer

`builder.AddIonic()` registers the overlay services as singletons: the six controllers, the overlay registry, and `IonOverlayManager`. `IonApp` draws the layer after its content. The layer is a box that covers the `IonApp` (z-index 1000) and lets taps through except where an overlay is. Overlays in the layer are stacked in the order they were added, so the last one is on top and gets the taps. Put exactly one `IonApp` in the app's layout, around `@Body`, the way every app template does.

<!-- miko:example examples/overlays/AppLayout.razor -->
```razor
@inherits LayoutComponentBase

@* MainLayout.razor. IonApp renders the overlay layer after the page, so overlays drawn in it
   (every controller overlay) paint above the page, the tab bar, and the menu. *@
<IonApp>
    @Body
</IonApp>
```
<!-- /miko:example -->

- **Controller overlays render only in this layer.** Without an `IonApp`, `PresentAsync()` succeeds but nothing appears.
- **Use one `IonApp`, never nest them.** Each `IonApp` draws the whole layer, so an inner one shows every overlay twice.
- `IonOverlayManager` (inject it) gives a read-only view of the layer: `Count`, and `GetTopIdAsync()` for the `Id` of the topmost overlay.

- **Declarative overlays move into the layer too.** A routed page is built inside the layout, so an overlay declared anywhere on the page receives `IonApp`'s overlay context and is drawn in the layer, not where it is declared. Taps on it therefore do not bubble to the elements around its declaration. Without an `IonApp` (a component rendered on its own) a declarative overlay falls back to rendering inline.

## Declarative overlays

Declare the component in markup and bind `IsOpen` two-way with `@bind-IsOpen`. When the overlay closes itself (a button, the backdrop, the back button, a timeout, a sheet swipe), it sets `IsOpen` to `false`, raises `IsOpenChanged(false)`, and then raises `OnDidDismiss`. Without the binding the parent still holds `true`, so the overlay comes back as soon as the parent re-renders — immediately, if the parent handles `OnDidDismiss`. If you set `IsOpen` to `false` yourself, the overlay closes without raising `OnDidDismiss`.

<!-- miko:example examples/overlays/Declarative.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Delete draft</IonButton>

@* @bind-IsOpen keeps _isOpen in sync when the alert closes itself (button, backdrop). *@
<IonAlert @bind-IsOpen="_isOpen"
          Header="Delete draft?"
          Buttons="@_buttons"
          OnDidDismiss="OnDismissed" />

<p>Last result: @_result</p>

@code {
    private bool _isOpen;
    private string? _result;

    private readonly IReadOnlyList<IonAlertButton> _buttons =
    [
        new() { Text = "Cancel", Role = "cancel" },
        new() { Text = "Delete", Role = "destructive" },
    ];

    private void OnDismissed(IonOverlayDismissEventArgs e)
        => _result = e.IsCancel ? "kept" : "deleted";
}
```
<!-- /miko:example -->

The public methods some overlays expose (for example `IonModal.DismissAsync`) can be called through `@ref`: declare a field of the component type and write `@ref="_modal"` on the component. The field always refers to the current instance; Miko creates a new one each time the declaring component re-renders. When two overlays declared by the same component are open at once, the one declared later in the markup is on top. The exception is overlays in the layer that each have a stable `OverlayId`: those keep the order in which they were opened.

## Controllers

Inject a controller and call `CreateAsync(options)` to get an [`IonOverlayReference`](#ionoverlayreference). Then:

- `PresentAsync()` shows the overlay.
- `await OnDidDismissAsync()` returns the result once it closes.
- `DismissAsync(data, role)` closes it from code.

The options classes mirror the component parameters, and `CssClass` maps to the component's `Class`. Nothing has to be declared in the page. A reference is single-use: after it has been dismissed, `PresentAsync()` does nothing and `DismissAsync()` returns `false`, so create a new overlay to show it again. `GetTopAsync()` returns the topmost overlay created by that controller (other controllers' overlays do not count). Content inside the overlay can use it to close itself, like Ionic's `modalController.dismiss()` — see [Modal](./modal.md#presenting-a-component).

<!-- miko:example examples/overlays/Controller.razor -->
```razor
@inject IonAlertController AlertController

<IonButton OnClick="ConfirmAsync">Delete draft</IonButton>

<p>Last result: @_result</p>

@code {
    private string? _result;

    private async Task ConfirmAsync()
    {
        var alert = await AlertController.CreateAsync(new IonAlertOptions
        {
            Header = "Delete draft?",
            Buttons =
            [
                new() { Text = "Cancel", Role = "cancel" },
                new() { Text = "Delete", Role = "destructive" },
            ],
        });
        await alert.PresentAsync();

        // Completes when the alert is dismissed — by a button, the backdrop, or alert.DismissAsync().
        var result = await alert.OnDidDismissAsync();
        _result = result.IsCancel ? "kept" : "deleted";
    }
}
```
<!-- /miko:example -->

The layer re-renders an overlay's content whenever the stack changes, and every re-render creates fresh instances of the components inside. Keep state that must survive in the presenting component or in parameters, not in private fields of the content component.

## Dismiss results

Every overlay reports how it closed with an [`IonOverlayDismissEventArgs`](#ionoverlaydismisseventargs). Declarative overlays pass it to `OnDidDismiss`; controllers return it from `OnDidDismissAsync()`. `IonModal` and `IonPopover` also raise `OnWillDismiss` with the same value just before they close.

| `Role` | When |
| --- | --- |
| the button's `Role` | A button was tapped (alert, action sheet, toast). `null` for a button without a role. |
| `"backdrop"` | The backdrop was tapped (only when `BackdropDismiss` is true). |
| `"back"` | The system back button closed a modal or action sheet. |
| `"gesture"` | A sheet modal was dragged down to the `0` breakpoint. |
| `"timeout"` | A toast's `Duration` ran out (a loading indicator's `Duration` reports `null`). |
| any | Whatever was passed to `IonOverlayReference.DismissAsync(data, role)`. |

`Data` holds the tapped action-sheet button's `Data`, an alert's input values, or whatever was passed to `DismissAsync`. Toasts and loading indicators never attach data themselves. `IsCancel` is true only for the `cancel` and `backdrop` roles. A `Handler` on an alert, action-sheet, or toast button is a plain `Action` that runs before the overlay closes. Unlike in Ionic, it cannot keep the overlay open.

`ShowBackdrop="false"` (on loading, modal, and popover) removes the backdrop element, and with it the tap target that `BackdropDismiss` relies on.

## Back button

While an `IonModal` or `IonActionSheet` is open and its `BackdropDismiss` is true, it handles the system back button: the Android back button, the iOS edge-swipe back gesture, Escape on desktop, and the simulator's back button. It closes with role `"back"`. Overlays take the back action before an open [`IonMenu`](./menu.md) and before page navigation ([`IonBackButton`](./back-button.md)). Among open overlays, the most recently registered one goes first. `IonAlert`, `IonLoading`, `IonPopover`, and `IonToast` do not respond to the back button in this port, whereas Ionic closes them too.

## Types

<!-- miko:type IonOverlayReference IonOverlayDismissEventArgs IonOverlayControllerBase -->
### IonOverlayReference

A handle to one overlay created by a controller's `CreateAsync` (the element Ionic's `create()` resolves to). It is single-use: once dismissed, it cannot be presented again.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string` | — | The overlay's id in the overlay layer (what `IonOverlayManager.GetTopIdAsync` returns). (read-only) |

| Method | Returns | Description |
| --- | --- | --- |
| `DismissAsync(object? data = null, string? role = null)` | `Task<bool>` | Closes the overlay and completes [`OnDidDismissAsync`](#ionoverlayreference) with `role` and `data`. Returns false when the overlay is not presented or is already closing or dismissed. |
| `OnDidDismissAsync()` | `Task<IonOverlayDismissEventArgs>` | Completes once the overlay is dismissed, by the user or by [`DismissAsync`](#ionoverlayreference), with the role and data it was dismissed with. |
| `PresentAsync()` | `Task` | Shows the overlay in the overlay layer of [`IonApp`](./app.md#ionapp). Does nothing when it is already presented or was dismissed. Nothing appears when the app has no [`IonApp`](./app.md#ionapp). |

### IonOverlayDismissEventArgs

Payload for an overlay's dismiss event (`ion-action-sheet`, `ion-alert`, …). Mirrors Ionic's `OverlayEventDetail` (`{ role, data }`). [`Role`](#ionoverlaydismisseventargs) is the tapped button's role (`"cancel"`, `"destructive"`, a custom role), `"backdrop"` (backdrop tap), `"back"` (system back button), `"gesture"` (sheet modal dragged closed), `"timeout"` (toast duration), or the role passed to `DismissAsync`. [`Data`](#ionoverlaydismisseventargs) is the value attached to the tapped action-sheet button, an alert's input values, or the data passed to `DismissAsync`.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Role` | `string?` | — | How the overlay was dismissed: the tapped button's role, or one of the roles listed on the type. (read-only) |
| `Data` | `object?` | — | Data attached to the dismissing button, collected alert input values, or the data passed to `DismissAsync`. (read-only) |
| `IsCancel` | `bool` | — | True when [`Role`](#ionoverlaydismisseventargs) is `"cancel"` or `"backdrop"` (case-insensitive); false for every other role, including `"back"`. (read-only) |

### IonOverlayControllerBase

Base class of the overlay controllers ([`IonModalController`](./modal.md#ionmodalcontroller), [`IonAlertController`](./alert.md#ionalertcontroller), …). `AddIonic()` registers each controller as a singleton; overlays it creates render in the overlay layer of [`IonApp`](./app.md#ionapp).

| Method | Returns | Description |
| --- | --- | --- |
| `GetTopAsync()` | `Task<IonOverlayReference?>` | The topmost presented overlay created by this controller, or null. Overlays of other controllers and declarative overlays are not considered. |
<!-- /miko:type -->

## Styling

Each overlay page documents its own classes and theme tokens. These selectors are shared by all overlays:

| Selector | Element |
| --- | --- |
| `.ion-overlay-host` | The overlay layer that `IonApp` draws over its content. |
| `.ion-backdrop` | The dim, tappable layer behind an overlay (also `.alert-backdrop`, `.modal-backdrop`, …). |
| `.ion-overlay-wrapper` | The overlay's visible surface (the alert card, the sheet, the modal panel, …). |
| `.overlay-hidden` | An overlay host that is closed. Most hosts are `display: none` then; the action sheet and the toast stay laid out but ignore taps, so their leave animations can play. |

Loading, modal, and popover have no theme token of their own. Their backdrop color comes from `IonicTheme.ActionSheet.BackdropColor` (loading) and `IonicTheme.Alert.BackdropColor` (modal, popover); see [Action Sheet](./action-sheet.md#theme-tokens) and [Alert](./alert.md#theme-tokens).
