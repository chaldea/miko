# Toast

A toast is a short, non-blocking notification that appears at the top, middle, or bottom of the screen, typically to confirm an action or report a status.

`IonToast` is the declarative component. `IonToastController` creates the same toast from code. See [Overlays](./overlays.md) for where overlays render, what has to be in place, and how dismiss results work.

## Basic usage

Show the toast with `IsOpen`. When `Duration` (milliseconds) is greater than zero, the toast hides itself after that time with role `"timeout"`, so bind `IsOpen` with `@bind-IsOpen` to reset your flag. Without a `Duration` it stays until a button or your code closes it. A toast has no backdrop: the rest of the screen stays usable, and only the toast card itself takes taps.

<!-- miko:example examples/toast/Basic.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Save</IonButton>

@* Duration hides the toast after 2 s (role "timeout"); @bind-IsOpen resets _isOpen. *@
<IonToast @bind-IsOpen="_isOpen"
          Message="Changes saved"
          Duration="2000" />

@code {
    private bool _isOpen;
}
```
<!-- /miko:example -->

The toast fades in and out on md, and slides in from the screen edge on ios (a `middle` toast fades in both modes). It rests `IonicTheme.Toast.EdgeOffset` away from the top or bottom edge, plus the safe-area inset.

## Position

`Position` is `"bottom"` (default), `"top"`, or `"middle"`.

<!-- miko:example examples/toast/Position.razor -->
```razor
<IonButton OnClick="@(() => Show("top"))">Top</IonButton>
<IonButton OnClick="@(() => Show("middle"))">Middle</IonButton>
<IonButton OnClick="@(() => Show("bottom"))">Bottom</IonButton>

<IonToast @bind-IsOpen="_isOpen"
          Position="@_position"
          Message="@($"Shown at the {_position}")"
          Duration="1500" />

@code {
    private bool _isOpen;
    private string _position = "bottom";

    private void Show(string position)
    {
        _position = position;
        _isOpen = true;
    }
}
```
<!-- /miko:example -->

## Buttons, header, icon, and color

`Header` adds a bold line above `Message`. `Icon` takes an Ionicons name and draws it before the text. `Color` takes a palette name and tints the card; the text and buttons then use the color's contrast color (see [Theming](./theming.md#palette)).

`Buttons` takes a list of `IonToastButton`. Buttons go at the end of the toast, or before the content with `Side = "start"`. A button with an `Icon` and no `Text` is drawn as an icon-only button. Every button closes the toast, and `OnDidDismiss` reports its `Role`. A button's `Handler` runs before the toast closes, except for `cancel`-role buttons, which never run their handler.

<!-- miko:example examples/toast/Buttons.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Archive</IonButton>
<p>@_status</p>

<IonToast @bind-IsOpen="_isOpen"
          Header="Archived"
          Message="1 conversation moved to the archive."
          Icon="@Ionicons.Archive"
          Color="dark"
          Buttons="@_buttons"
          OnDidDismiss="OnDismissed" />

@code {
    private bool _isOpen;
    private string _status = "";
    private IReadOnlyList<IonToastButton> _buttons = [];

    protected override void OnInitialized()
    {
        _buttons =
        [
            // Handler runs before the toast closes; a cancel-role button never runs its Handler.
            new() { Text = "Undo", Role = "undo", Handler = () => _status = "Restored" },
            new() { Icon = Ionicons.Close, Role = "cancel" }, // icon-only button
        ];
    }

    private void OnDismissed(IonOverlayDismissEventArgs e)
    {
        if (e.Role != "undo")
            _status = "Archived";
    }
}
```
<!-- /miko:example -->

## Using the controller

`IonToastController.CreateAsync` takes an `IonToastOptions` with the same message, position, icon, color, buttons, and duration settings. A presented toast with a `Duration` dismisses itself, so there is nothing else to do.

<!-- miko:example examples/toast/Controller.razor -->
```razor
@inject IonToastController ToastController

<IonButton OnClick="CopyAsync">Copy link</IonButton>

@code {
    private async Task CopyAsync()
    {
        var toast = await ToastController.CreateAsync(new IonToastOptions
        {
            Message = "Link copied",
            Icon = Ionicons.CheckmarkCircle,
            Position = "top",
            Duration = 1500,
        });
        await toast.PresentAsync();
    }
}
```
<!-- /miko:example -->

Each `PresentAsync()` adds another toast to the layer. Toasts are not queued, so two at the same position overlap.

## API

<!-- miko:api IonToast -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonToast

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Buttons` | `IReadOnlyList<IonToastButton>` | `[]` | The buttons to show. Split into start/end groups by [`IonToastButton.Side`](#iontoastbutton). |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, .... Stamps `ion-color ion-color-{color}` so the color-scoped rules tint the wrapper. |
| `Duration` | `int` | `0` | Number of milliseconds before auto-dismissing (Ionic `duration`). When greater than zero, the toast dismisses automatically after the specified time. |
| `Header` | `string?` | `null` | Optional bold header shown above the message. |
| `Icon` | `string?` | `null` | Optional leading icon (an Ionicons name) shown before the content. |
| `IsOpen` | `bool` | `false` | Whether the toast is presented. Bind two-way with [`IsOpenChanged`](#iontoast). Two-way bindable: `@bind-IsOpen`. |
| `Message` | `string?` | `null` | The body message. |
| `OverlayId` | `string?` | `null` | Optional stable identifier used by the root overlay registry. |
| `Position` | `string` | `"bottom"` | Where the toast sits: `"top"`, `"middle"`, or `"bottom"` (default). |
| `Trigger` | `string?` | `null` | Id of the element that opens the toast when clicked (Ionic `trigger`), e.g. `<IonButton Id="open-toast">`. Opening sets [`IsOpen`](#iontoast) and raises [`IsOpenChanged`](#iontoast). The page must be inside an [`IonApp`](./app.md#ionapp). |

| Event | Type | Description |
| --- | --- | --- |
| `IsOpenChanged` | `EventCallback<bool>` | Raised with the new open state when the toast opens/closes. |
| `OnDidDismiss` | `EventCallback<IonOverlayDismissEventArgs>` | Raised when the toast dismisses, with the tapped button's role, or `"timeout"` when [`Duration`](#iontoast) ran out. Data is always null. |
<!-- /miko:api -->

## Types

<!-- miko:type IonToastButton IonToastOptions IonToastController -->
### IonToastButton

A single toast button. Mirrors Ionic's `ToastButton` interface. [`Side`](#iontoastbutton) places the button in the start or end button group (default `"end"`). A [`Role`](#iontoastbutton) of `"cancel"` dismisses the toast without running the handler as a normal action; any other role runs the [`Handler`](#iontoastbutton) then dismisses. The role is also stamped as `toast-button-{role}`.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Text` | `string?` | `null` | The button label. |
| `Icon` | `string?` | `null` | Optional leading icon (an Ionicons name). |
| `Side` | `string?` | `null` | Which group the button belongs to: `"start"` or `"end"` (default). Anything other than `"start"` renders in the end group (matches toast.tsx). |
| `Role` | `string?` | `null` | Optional role: `"cancel"` or a custom string. Stamped as `toast-button-{role}`; a cancel role dismisses without running as a normal action. |
| `CssClass` | `string?` | `null` | Extra CSS class(es) to add to the button. |
| `Handler` | `Action?` | `null` | Optional handler run when a non-cancel button is tapped, before the toast dismisses. |
| `IsCancel` | `bool` | `false` | True when this button dismisses the toast without acting as a normal option. (read-only) |
| `IsStart` | `bool` | `false` | True when this button belongs to the start group. (read-only) |
| `IsIconOnly` | `bool` | `false` | True when the button shows only an icon (an icon but no text — toast.tsx `toast-button-icon-only`). (read-only) |

### IonToastOptions

Options for [`IonToastController.CreateAsync`](#iontoastcontroller). Each property maps to the [`IonToast`](#iontoast) parameter of the same name.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Header` | `string?` | `null` | Optional bold header above the message. |
| `Message` | `string?` | `null` | The body message. |
| `Position` | `string` | `"bottom"` | `"top"`, `"middle"`, or `"bottom"` (default). |
| `Icon` | `string?` | `null` | Optional leading icon (an Ionicons name). |
| `Color` | `string?` | `null` | Named palette color, e.g. `"success"`. |
| `Buttons` | `IReadOnlyList<IonToastButton>` | `[]` | The buttons to show. |
| `Duration` | `int` | `0` | Milliseconds before the toast dismisses itself (role `"timeout"`); 0 (default) never. |
| `CssClass` | `string?` | `null` | Extra CSS class(es) for the toast host (its `Class`). |

### IonToastController

Creates toasts from code (Ionic's `toastController`). Inject it; `AddIonic()` registers it.

| Method | Returns | Description |
| --- | --- | --- |
| `CreateAsync(IonToastOptions options)` | `Task<IonOverlayReference>` | Creates a toast from `options`. Call [`IonOverlayReference.PresentAsync`](./overlays.md#ionoverlayreference) on the result to show it. |
<!-- /miko:type -->

## Styling

### Theme tokens

<!-- miko:tokens Toast -->
Theme token `IonicTheme.Toast` (`ToastToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Toast = new ToastToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `EdgeOffset` | `float` | `8` | `10` |
| `EnterDuration` | `float` | `0.4` | `0.4` |
| `LeaveDuration` | `float` | `0.3` | `0.3` |
<!-- /miko:tokens -->

The card's colors, radius, and spacing are fixed per mode (md: a dark `#333333` card with light text; ios: a light `#f9f9f9` card with dark text). Change them with the classes below.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-toast` | The full-size host, transparent to taps. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.toast-open` | The toast is open (drives the enter animation's end state). |
| `.toast-mounted` | Open or still animating out: the card takes taps. |
| `.overlay-hidden` | Closed and settled. |
| `.toast-wrapper` | The card: background, radius, shadow. |
| `.toast-top`, `.toast-middle`, `.toast-bottom` | `Position`, on `.toast-wrapper`. |
| `.toast-container` | The row holding the icon, content, and button groups. |
| `.toast-icon` | `Icon`. |
| `.toast-content`, `.toast-header`, `.toast-message` | The text block, `Header`, and `Message`. |
| `.toast-button-group` | A button group; `.toast-button-group-start` / `.toast-button-group-end` by `Side`. |
| `.toast-button`, `.toast-button-inner`, `.toast-button-icon` | One button, its content row, and its icon. |
| `.toast-button-icon-only` | A button with an icon and no text. |
| `.toast-button-{role}` | A button's `Role`, e.g. `.toast-button-cancel`. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-success`). |

```csharp
// GlobalStyles.cs — <IonToast Class="pill" …>
[".ion-toast.pill .toast-wrapper"] = new()
{
    BorderRadius = new BorderRadius(999),
},
```
