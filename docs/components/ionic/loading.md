# Loading

A loading indicator is an overlay that shows a spinner and an optional message, and blocks interaction with the app while work is in progress.

`IonLoading` is the declarative component. `IonLoadingController` creates the same indicator from code. See [Overlays](./overlays.md) for where overlays render and what has to be in place.

## Basic usage

Show the indicator with `IsOpen`. When `Duration` (milliseconds) is greater than zero, it closes itself after that time and reports a `null` role, so bind `IsOpen` with `@bind-IsOpen` to reset your flag. The backdrop blocks taps on the page, and by default tapping it does nothing (`BackdropDismiss` defaults to `false`).

<!-- miko:example examples/loading/Basic.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Show loading</IonButton>

@* Duration auto-dismisses after 3 s; @bind-IsOpen resets _isOpen when it does. *@
<IonLoading @bind-IsOpen="_isOpen"
            Message="Please wait…"
            Duration="3000" />

@code {
    private bool _isOpen;
}
```
<!-- /miko:example -->

## While work runs

More often, the app opens the indicator before an operation and closes it afterwards. An async `OnClick` handler re-renders the component when it reaches its first `await` and again when it completes, so a flag set at both ends is enough.

<!-- miko:example examples/loading/Work.razor -->
```razor
<IonButton OnClick="SaveAsync">Save</IonButton>

<IonLoading IsOpen="_saving" Message="Saving…" />

@code {
    private bool _saving;

    private async Task SaveAsync()
    {
        _saving = true;           // the component re-renders at the first await, showing the overlay
        await Task.Delay(1500);   // stand-in for the real work
        _saving = false;          // and again when the handler completes, hiding it
    }
}
```
<!-- /miko:example -->

## Spinner

`Spinner` takes an [`IonSpinner`](./spinner.md) name. When it is `null`, the mode's default is used: `crescent` on md, `lines` on ios. An empty string shows the message without a spinner. Without a `Message`, the indicator shows the spinner alone.

<!-- miko:example examples/loading/Spinner.razor -->
```razor
<IonButton OnClick="@(() => _dotsOpen = true)">Dots spinner</IonButton>
<IonButton OnClick="@(() => _textOpen = true)">Message only</IonButton>

<IonLoading @bind-IsOpen="_dotsOpen" Spinner="dots" Message="Syncing…" Duration="2000" />

@* An empty Spinner hides the spinner; null (the default) picks the mode's spinner. *@
<IonLoading @bind-IsOpen="_textOpen" Spinner="" Message="Connecting…" Duration="2000" />

@code {
    private bool _dotsOpen;
    private bool _textOpen;
}
```
<!-- /miko:example -->

## Using the controller

`IonLoadingController.CreateAsync` takes an `IonLoadingOptions`. Present the indicator, do the work, and call `DismissAsync()` on the reference, in a `finally` so a failure cannot leave the app blocked.

<!-- miko:example examples/loading/Controller.razor -->
```razor
@inject IonLoadingController LoadingController

<IonButton OnClick="RefreshAsync">Refresh</IonButton>

@code {
    private async Task RefreshAsync()
    {
        var loading = await LoadingController.CreateAsync(new IonLoadingOptions
        {
            Message = "Refreshing…",
        });
        await loading.PresentAsync();
        try
        {
            await Task.Delay(1500); // stand-in for the real work
        }
        finally
        {
            await loading.DismissAsync();
        }
    }
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonLoading -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonLoading

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `BackdropDismiss` | `bool` | `false` | If true, tapping the backdrop dismisses the indicator. Defaults to `false` (loading blocks interaction — loading.tsx `backdropDismiss = false`). |
| `Duration` | `int` | `0` | Number of milliseconds before auto-dismissing (Ionic `duration`). When greater than zero, the loading indicator dismisses automatically after the specified time. |
| `IsOpen` | `bool` | `false` | Whether the indicator is presented. Bind two-way with [`IsOpenChanged`](#ionloading). Two-way bindable: `@bind-IsOpen`. |
| `Message` | `string?` | `null` | Optional text content shown beside/under the spinner. |
| `OverlayId` | `string?` | `null` | Optional stable identifier used by the root overlay registry. |
| `ShowBackdrop` | `bool` | `true` | If true (default), a backdrop is shown behind the indicator (loading.tsx `showBackdrop = true`). |
| `Spinner` | `string?` | `null` | The spinner preset name (see [`IonSpinner`](./spinner.md#ionspinner)). When null it falls back to the per-mode default (`"lines"` on ios, `"crescent"` on md — loading.tsx `componentWillLoad`). Pass an empty string to hide the spinner entirely. |
| `Translucent` | `bool` | `false` | If true, the surface is translucent (ios blur). Marker only in this port. |
| `Trigger` | `string?` | `null` | Id of the element that opens the indicator (Ionic `trigger`). Kept for API parity; the host drives [`IsOpen`](#ionloading) in this port. |

| Event | Type | Description |
| --- | --- | --- |
| `IsOpenChanged` | `EventCallback<bool>` | Raised with the new open state when the indicator opens/closes. |
| `OnDidDismiss` | `EventCallback<IonOverlayDismissEventArgs>` | Raised when the indicator dismisses (null role on a normal dismiss, `"backdrop"` when the backdrop is tapped). |
<!-- /miko:api -->

## Types

<!-- miko:type IonLoadingOptions IonLoadingController -->
### IonLoadingOptions

Options for [`IonLoadingController.CreateAsync`](#ionloadingcontroller). Each property maps to the [`IonLoading`](#ionloading) parameter of the same name.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Message` | `string?` | `null` | Optional text shown with the spinner. |
| `Spinner` | `string?` | `null` | Spinner name; null uses the mode default, an empty string hides it. |
| `Duration` | `int` | `0` | Milliseconds before the indicator dismisses itself; 0 (default) never. |
| `BackdropDismiss` | `bool` | `false` | If true, tapping the backdrop dismisses the indicator (default false). |
| `ShowBackdrop` | `bool` | `true` | If true (default), the backdrop is rendered. |
| `CssClass` | `string?` | `null` | Extra CSS class(es) for the indicator host (its `Class`). |

### IonLoadingController

Creates loading indicators from code (Ionic's `loadingController`). Inject it; `AddIonic()` registers it.

| Method | Returns | Description |
| --- | --- | --- |
| `CreateAsync(IonLoadingOptions options)` | `Task<IonOverlayReference>` | Creates a loading indicator from `options`. Call [`IonOverlayReference.PresentAsync`](./overlays.md#ionoverlayreference) on the result to show it. |
<!-- /miko:type -->

## Styling

`IonLoading` has no theme token of its own. Its spinner uses the palette's primary color on md, and its backdrop color comes from `IonicTheme.ActionSheet.BackdropColor` (see [Action Sheet](./action-sheet.md#theme-tokens)).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-loading` | The full-screen host that centers the indicator. Also carries the mode (`md` / `ios`); `.overlay-hidden` while closed. |
| `.loading-backdrop` | The dim layer (also `.ion-backdrop`); omitted when `ShowBackdrop="false"`. |
| `.loading-wrapper` | The indicator's card. |
| `.loading-spinner` | The box around the `IonSpinner`. |
| `.loading-content` | `Message`. |
| `.loading-translucent` | `Translucent="true"`. Only a marker; no translucent style is applied. |

```csharp
// GlobalStyles.cs — <IonLoading Class="brand" …>
[".ion-loading.brand .loading-wrapper"] = new()
{
    BorderRadius = new BorderRadius(12),
},
```
