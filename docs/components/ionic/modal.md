# Modal

A modal is a dialog that covers the app with its own content, typically a header and scrollable content like a page, until the user dismisses it.

`IonModal` is the declarative component. `IonModalController` creates a modal from code, with inline content or a component type. See [Overlays](./overlays.md) for where overlays render, what has to be in place, and how dismiss results work.

## Basic usage

Put the modal's content between the tags. The panel fills the screen on both modes, so lay it out like a page: an [`IonHeader`](./header.md) with a toolbar, then an [`IonContent`](./content.md) that scrolls. Close it by setting your `IsOpen` field to `false`. The system back button also closes it. A full-screen panel hides the backdrop, so a backdrop tap only works for a sheet or a panel you have resized. `BackdropDismiss="false"` turns off both the back button and the backdrop tap.

<!-- miko:example examples/modal/Basic.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Open modal</IonButton>

<IonModal @bind-IsOpen="_isOpen">
    <IonHeader>
        <IonToolbar>
            <ChildContent>
                <IonTitle>Settings</IonTitle>
            </ChildContent>
            <End>
                <IonButtons>
                    <IonButton OnClick="@(() => _isOpen = false)">Close</IonButton>
                </IonButtons>
            </End>
        </IonToolbar>
    </IonHeader>
    <IonContent Class="ion-padding">
        <p>The modal covers the screen; lay it out like a page, with a header and content.</p>
    </IonContent>
</IonModal>

@code {
    private bool _isOpen;
}
```
<!-- /miko:example -->

Ionic's smaller centered dialog on tablet-sized screens is not ported, and `KeepContentsMounted` has no effect.

## Sheet modal

Setting both `Breakpoints` (fractions of the screen height, from `0` to `1`) and `InitialBreakpoint` turns the modal into a bottom sheet that opens at the initial fraction. The sheet shows a drag handle unless `Handle="false"`. Dragging anywhere on the sheet moves it, and on release it snaps to the nearest breakpoint. When `0` is one of the breakpoints, dragging the sheet to the bottom closes it with role `"gesture"`. The backdrop covers the whole screen at every breakpoint; Ionic's `backdropBreakpoint` is not ported.

<!-- miko:example examples/modal/Sheet.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Open sheet</IonButton>

@* A sheet needs both Breakpoints and InitialBreakpoint. Dragging snaps to the nearest
   breakpoint; with 0 in the list, dragging to the bottom dismisses (role "gesture"). *@
<IonModal @bind-IsOpen="_isOpen"
          Breakpoints="@_breakpoints"
          InitialBreakpoint="0.5">
    <IonContent Class="ion-padding">
        <p>Drag the sheet up to expand it, or down to close it.</p>
    </IonContent>
</IonModal>

@code {
    private bool _isOpen;
    private readonly double[] _breakpoints = [0, 0.25, 0.5, 1];
}
```
<!-- /miko:example -->

When the component that declares the sheet re-renders, the sheet returns to `InitialBreakpoint`. `IonModal` has `SetCurrentBreakpointAsync` and `GetCurrentBreakpointAsync` methods, but markup cannot reach them because `@ref` is not supported.

## Card modal

Setting `Presenting` (any non-null value, usually `this`) makes it a card modal. In ios mode that rounds the panel's corners and gives it a shadow. Ionic's animation that scales the presenting page down behind the card is not implemented, and in md mode `Presenting` changes nothing.

<!-- miko:example examples/modal/Card.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Open card</IonButton>

@* Presenting marks a card modal: rounded corners and a shadow in ios mode, no change in md mode. *@
<IonModal @bind-IsOpen="_isOpen" Presenting="@this">
    <IonContent Class="ion-padding">
        <p>A card-style modal.</p>
    </IonContent>
</IonModal>

@code {
    private bool _isOpen;
}
```
<!-- /miko:example -->

## Using the controller

`IonModalController.CreateAsync` takes an `IonModalOptions`. Its `Content` is a `RenderFragment`, which can be written inline with Razor template syntax. Keep the `IonOverlayReference` to close the modal from that content, and `await OnDidDismissAsync()` when you need the result.

<!-- miko:example examples/modal/Controller.razor -->
```razor
@inject IonModalController ModalController

<IonButton OnClick="OpenAsync">Show terms</IonButton>

@code {
    private IonOverlayReference? _modal;

    private async Task OpenAsync()
    {
        _modal = await ModalController.CreateAsync(new IonModalOptions { Content = Terms });
        await _modal.PresentAsync();
    }

    private Task CloseAsync() => _modal?.DismissAsync() ?? Task.CompletedTask;

    // Inline content for the modal (IonModalOptions.Content).
    private RenderFragment Terms =>
        @<IonContent Class="ion-padding">
            <h2>Terms of use</h2>
            <p>By using this app you agree to…</p>
            <IonButton Expand="block" OnClick="CloseAsync">Accept</IonButton>
        </IonContent>;
}
```
<!-- /miko:example -->

## Presenting a component

`CreateAsync<TComponent>(parameters)` presents a component as the modal's content. Each parameter is assigned by name to a public property of the component, so use `nameof(...)` for the keys. The component can close its own modal: inject the controller and call `DismissAsync(data, role)` on `GetTopAsync()`, the equivalent of Ionic's `modalController.dismiss(data, role)`. The overlay layer re-creates the content component whenever the stack changes, for example when a toast appears. So the component should edit an object passed in as a parameter rather than a private field.

<!-- miko:example examples/modal/EditNameModal.razor -->
```razor
@* Content component for a controller-created modal. It closes itself through the controller,
   like Ionic's modalController.dismiss(data, role). *@
@inject IonModalController ModalController

<IonHeader>
    <IonToolbar>
        <Start>
            <IonButtons>
                <IonButton OnClick="@(() => CloseAsync("cancel"))">Cancel</IonButton>
            </IonButtons>
        </Start>
        <ChildContent>
            <IonTitle>Edit name</IonTitle>
        </ChildContent>
        <End>
            <IonButtons>
                <IonButton Strong="true" OnClick="@(() => CloseAsync("confirm"))">Save</IonButton>
            </IonButtons>
        </End>
    </IonToolbar>
</IonHeader>
<IonContent Class="ion-padding">
    <IonInput Label="Name" LabelPlacement="stacked" @bind-Value="Draft.Name" />
</IonContent>

@code {
    // Edit a caller-owned object, not a private field: the overlay layer re-creates this
    // component whenever it re-renders (e.g. another overlay opens); parameters survive that.
    [Parameter] public NameDraft Draft { get; set; } = new();

    public sealed class NameDraft
    {
        public string? Name { get; set; }
    }

    private async Task CloseAsync(string role)
    {
        // The top controller-created modal is the one showing this component.
        var modal = await ModalController.GetTopAsync();
        if (modal is not null)
            await modal.DismissAsync(Draft.Name, role);
    }
}
```
<!-- /miko:example -->

<!-- miko:example examples/modal/ControllerComponent.razor -->
```razor
@inject IonModalController ModalController

<IonButton OnClick="EditAsync">Name: @_name</IonButton>

@code {
    private string _name = "Ada";

    private async Task EditAsync()
    {
        // Keys are the content component's [Parameter] property names.
        var modal = await ModalController.CreateAsync<EditNameModal>(new Dictionary<string, object?>
        {
            [nameof(EditNameModal.Draft)] = new EditNameModal.NameDraft { Name = _name },
        });
        await modal.PresentAsync();

        var result = await modal.OnDidDismissAsync();
        if (result.Role == "confirm" && result.Data is string name)
            _name = name;
    }
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonModal -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonModal

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `BackdropDismiss` | `bool` | `true` | If true (default), tapping the backdrop or pressing the system back button dismisses the modal. |
| `Breakpoints` | `double[]?` | `null` | Sheet-modal snap points. Values outside the inclusive range 0..1 are ignored. |
| `Handle` | `bool?` | `null` | Controls the sheet drag handle. It is shown by default for sheet modals. |
| `InitialBreakpoint` | `double?` | `null` | Initial visible fraction of a sheet modal. If it is not present in [`Breakpoints`](#ionmodal), the nearest configured breakpoint is used. |
| `IsOpen` | `bool` | `false` | Whether the modal is presented. Bind two-way with [`IsOpenChanged`](#ionmodal). Two-way bindable: `@bind-IsOpen`. |
| `KeepContentsMounted` | `bool` | `false` | Ionic's `keepContentsMounted`. Kept for API parity; it has no effect in this port. |
| `OverlayId` | `string?` | `null` | Optional stable identifier used by the root overlay registry. |
| `Presenting` | `object?` | `null` | The element the modal is presented from. Any non-null value makes it a card modal (`modal-card`) in ios mode; it has no effect in md mode. The stacked card animation of the presenting page is not implemented. |
| `ShowBackdrop` | `bool` | `true` | If true (default), the backdrop is rendered behind the modal. |
| `Trigger` | `string?` | `null` | Id of the element that opens the modal. The host drives [`IsOpen`](#ionmodal) in this port, so this is retained for API parity. |

| Event | Type | Description |
| --- | --- | --- |
| `IsOpenChanged` | `EventCallback<bool>` | Raised with the new open state when the modal opens/closes. |
| `OnDidDismiss` | `EventCallback<IonOverlayDismissEventArgs>` | Raised after the modal dismisses, with role `"backdrop"`, `"back"` (system back button), `"gesture"` (sheet dragged to 0), or the role passed to [`DismissAsync`](#ionmodal). |
| `OnWillDismiss` | `EventCallback<IonOverlayDismissEventArgs>` | Raised before the modal dismisses, with the same role and data as [`OnDidDismiss`](#ionmodal). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Types

<!-- miko:type IonModalOptions IonModalController -->
### IonModalOptions

Options for [`IonModalController.CreateAsync`](#ionmodalcontroller). Each property maps to the [`IonModal`](#ionmodal) parameter of the same name.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Component` | `Type?` | `null` | A component type to render as the modal content (used when [`Content`](#ionmodaloptions) is null), with [`ComponentParameters`](#ionmodaloptions) as its parameters. Prefer [`IonModalController.CreateAsync`](#ionmodalcontroller), which sets it from a type argument. When assigning it directly in a trimmed (AOT) app, the source type must carry the same `DynamicallyAccessedMembers` annotation, or the parameters are trimmed away and silently keep their defaults. |
| `ComponentParameters` | `IReadOnlyDictionary<string, object?>` | `[]` | Parameters for [`Component`](#ionmodaloptions), keyed by its public property names. |
| `Content` | `RenderFragment?` | `null` | The modal content. Takes precedence over [`Component`](#ionmodaloptions). |
| `BackdropDismiss` | `bool` | `true` | If true (default), a backdrop tap or the system back button dismisses the modal. |
| `ShowBackdrop` | `bool` | `true` | If true (default), the backdrop is rendered. |
| `CssClass` | `string?` | `null` | Extra CSS class(es) for the modal host (its `Class`). |
| `Presenting` | `object?` | `null` | Any non-null value makes it a card modal in ios mode (see [`IonModal.Presenting`](#ionmodal)). |
| `Breakpoints` | `double[]?` | `null` | Sheet snap points, 0..1 (see [`IonModal.Breakpoints`](#ionmodal)). |
| `InitialBreakpoint` | `double?` | `null` | Initial sheet fraction; together with [`Breakpoints`](#ionmodaloptions) it makes a sheet modal. |
| `Handle` | `bool?` | `null` | Shows or hides the sheet's drag handle (shown by default for sheets). |

### IonModalController

Creates modals from code (Ionic's `modalController`). Inject it; `AddIonic()` registers it.

| Method | Returns | Description |
| --- | --- | --- |
| `CreateAsync(IonModalOptions options)` | `Task<IonOverlayReference>` | Creates a modal from `options`. Call [`IonOverlayReference.PresentAsync`](./overlays.md#ionoverlayreference) on the result to show it. |
| `CreateAsync<TComponent>(IReadOnlyDictionary<string, object?>? componentParameters = null, Action<IonModalOptions>? configure = null)` | `Task<IonOverlayReference>` | Creates a modal whose content is a `TComponent`, with `componentParameters` assigned to its public properties by name; `configure` sets the remaining [`IonModalOptions`](#ionmodaloptions). The type argument also tells the trimmer to keep the component's parameter properties, so parameters survive AOT. |
<!-- /miko:type -->

## Styling

`IonModal` has no theme token of its own. The panel uses the theme's `BackgroundColor` and `TextColor`, and its backdrop color comes from `IonicTheme.Alert.BackdropColor` (see [Alert](./alert.md#theme-tokens)).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-modal` | The full-screen host. Also carries the mode (`md` / `ios`); `.overlay-hidden` while closed. |
| `.modal-default`, `.modal-sheet`, `.modal-card` | The kind of modal: regular, sheet (`Breakpoints` + `InitialBreakpoint`), or card (`Presenting` in ios mode). |
| `.modal-backdrop` | The dim layer behind the panel (also `.ion-backdrop`); omitted when `ShowBackdrop="false"`. |
| `.modal-wrapper` | The panel holding your content (`width` / `height` 100% by default). |
| `.modal-shadow` | A transparent layer behind the panel, rendered in ios mode only. |
| `.modal-handle` | The sheet's drag handle. |

The host centers the panel, so giving `.modal-wrapper` a size turns the modal into a centered dialog:

```csharp
// GlobalStyles.cs — <IonModal Class="dialog" …>
[".ion-modal.dialog .modal-wrapper"] = new()
{
    Width = Length.Px(320),
    Height = Length.Px(240),
    BorderRadius = new BorderRadius(12),
},
```
