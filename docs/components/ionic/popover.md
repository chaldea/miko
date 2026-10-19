# Popover

A popover is a small floating panel that opens next to the element that triggered it. It is typically used for overflow menus and short contextual content.

`IonPopover` is the declarative component. `IonPopoverController` creates a popover from code. See [Overlays](./overlays.md) for where overlays render, what has to be in place, and how dismiss results work.

## Basic usage

Put the content between the tags; it goes into a box 250 px wide on md and 200 px on ios, at most 90% of the screen height, scrolling if needed. To anchor the popover to the button that opened it, pass the button's click event as `Event`. `IonButton`'s `OnClick` handler can take a `Miko.Events.MouseEventArgs`. Without an `Event`, the popover is centered on the screen. Tapping the backdrop closes it with role `"backdrop"`, unless `BackdropDismiss` (or `Dismissable`) is `false`. The system back button does not close a popover in this port.

<!-- miko:example examples/popover/Basic.razor -->
```razor
<IonButton OnClick="Open">More</IonButton>

@* Event anchors the popover to the tapped element; without it the popover is centered. *@
<IonPopover @bind-IsOpen="_isOpen" Event="_event">
    <IonList>
        <IonItem Button="true" OnClick="@(() => _isOpen = false)">Rename</IonItem>
        <IonItem Button="true" OnClick="@(() => _isOpen = false)">Duplicate</IonItem>
        <IonItem Button="true" Lines="none" OnClick="@(() => _isOpen = false)">Move to trash</IonItem>
    </IonList>
</IonPopover>

@code {
    private bool _isOpen;
    private Miko.Events.MouseEventArgs? _event;

    private void Open(Miko.Events.MouseEventArgs e)
    {
        _event = e;
        _isOpen = true;
    }
}
```
<!-- /miko:example -->

`Trigger` and `TriggerAction` are kept for API parity only: nothing opens the popover except `IsOpen` or a controller.

## Position

With an `Event`, `Side` chooses which edge of the trigger the popover opens on: `bottom` (default), `top`, `left`, `right`, `start` (left), or `end` (right). It flips to the opposite side when that side has more room (for `left` / `right`, only when the content does not fit). `Alignment` lines it up with the trigger's `start`, `center`, or `end`; the default is `start` on md and `center` on ios. On the top and bottom sides, the popover is also kept 8 px inside the left and right screen edges. `Arrow` (default `true`) adds a `.popover-arrow` element, but this port does not place or rotate it toward the trigger, so `Arrow="false"` usually looks cleaner.

<!-- miko:example examples/popover/Position.razor -->
```razor
<IonButton OnClick="Open">Info</IonButton>

@* Side picks the edge of the trigger (flipped when the viewport has no room on that side);
   Alignment lines the popover up with the trigger's start, center or end. *@
<IonPopover @bind-IsOpen="_isOpen"
            Event="_event"
            Side="top"
            Alignment="center"
            Arrow="false">
    <div class="ion-padding">Last synced 2 minutes ago.</div>
</IonPopover>

@code {
    private bool _isOpen;
    private Miko.Events.MouseEventArgs? _event;

    private void Open(Miko.Events.MouseEventArgs e)
    {
        _event = e;
        _isOpen = true;
    }
}
```
<!-- /miko:example -->

## Using the controller

`IonPopoverController.CreateAsync` takes an `IonPopoverOptions`, where `Content` is a `RenderFragment` and `Event` anchors the popover. To return a choice, call `DismissAsync(data)` on the reference from the content, then read `Data` from `OnDidDismissAsync()`.

<!-- miko:example examples/popover/Controller.razor -->
```razor
@inject IonPopoverController PopoverController

<IonButton OnClick="ShowAsync">Sort: @_sort</IonButton>

@code {
    private string _sort = "Newest";
    private IonOverlayReference? _popover;

    private async Task ShowAsync(Miko.Events.MouseEventArgs e)
    {
        _popover = await PopoverController.CreateAsync(new IonPopoverOptions
        {
            Event = e, // anchor to the tapped button
            Content = Options,
        });
        await _popover.PresentAsync();

        var result = await _popover.OnDidDismissAsync();
        if (result.Data is string sort)
            _sort = sort;
    }

    private Task PickAsync(string sort) => _popover?.DismissAsync(sort) ?? Task.CompletedTask;

    private RenderFragment Options =>
        @<IonList>
            <IonItem Button="true" OnClick="@(() => PickAsync("Newest"))">Newest</IonItem>
            <IonItem Button="true" OnClick="@(() => PickAsync("Oldest"))">Oldest</IonItem>
            <IonItem Button="true" Lines="none" OnClick="@(() => PickAsync("Title"))">Title</IonItem>
        </IonList>;
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonPopover -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonPopover

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Alignment` | `string?` | `null` | How the popover lines up with the [`Event`](#ionpopover) target: `"start"` / `"center"` / `"end"` (Ionic `alignment`). Null uses the mode default (`"start"` on md, `"center"` on ios). |
| `Arrow` | `bool` | `true` | If true (default), a `.popover-arrow` element is rendered (Ionic `arrow`). This port does not position or rotate it toward the trigger. |
| `BackdropDismiss` | `bool` | `true` | If true (default), tapping the backdrop dismisses the popover. |
| `Dismissable` | `bool` | `true` | Kept for API parity. When false, a backdrop tap does not dismiss the popover, the same as [`BackdropDismiss`](#ionpopover) = false. |
| `Event` | `MouseEventArgs?` | `null` | The click/pointer event used to anchor the popover. Controllers pass Ionic's `event` option here; declarative callers can bind the event received by their trigger. |
| `IsOpen` | `bool` | `false` | Whether the popover is presented. Bind two-way with [`IsOpenChanged`](#ionpopover). Two-way bindable: `@bind-IsOpen`. |
| `OverlayId` | `string?` | `null` | Optional stable identifier used by the root overlay registry. |
| `ShowBackdrop` | `bool` | `true` | If true (default), the backdrop is rendered. When false the backdrop element is omitted. |
| `Side` | `string` | `"bottom"` | Which side of the [`Event`](#ionpopover) target the popover opens on: `"top"` / `"right"` / `"bottom"` (default) / `"left"` / `"start"` (left) / `"end"` (right). It flips to the opposite side when that side has more room. The side used is stamped as `popover-side-{side}`. Without an [`Event`](#ionpopover) the popover is centered. |
| `Translucent` | `bool` | `false` | If true, the ios popover uses a translucent (blurred) surface (Ionic `translucent`). Stamps `popover-translucent`; a marker in this port. |
| `Trigger` | `string?` | `null` | Id of the element that opens the popover (Ionic `trigger`). Kept for API parity; the host drives [`IsOpen`](#ionpopover) in this port. |
| `TriggerAction` | `string` | `"click"` | How the trigger opens the popover: `"click"` (default), `"context-menu"`, or `"hover"` (Ionic `triggerAction`). Stored for API parity; there is no runtime trigger wiring in this static port. |

| Event | Type | Description |
| --- | --- | --- |
| `IsOpenChanged` | `EventCallback<bool>` | Raised with the new open state when the popover opens/closes. |
| `OnDidDismiss` | `EventCallback<IonOverlayDismissEventArgs>` | Raised after the popover dismisses (Ionic `ionPopoverDidDismiss`), with the role (`"backdrop"` on a backdrop tap, otherwise the caller's role). |
| `OnWillDismiss` | `EventCallback<IonOverlayDismissEventArgs>` | Raised before the popover dismisses (Ionic `ionPopoverWillDismiss`). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Types

<!-- miko:type IonPopoverOptions IonPopoverController -->
### IonPopoverOptions

Options for [`IonPopoverController.CreateAsync`](#ionpopovercontroller). Each property maps to the [`IonPopover`](#ionpopover) parameter of the same name.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Content` | `RenderFragment?` | `null` | The popover content. |
| `Event` | `MouseEventArgs?` | `null` | Pointer event used to anchor the popover to its presenting trigger. |
| `BackdropDismiss` | `bool` | `true` | If true (default), tapping the backdrop dismisses the popover. |
| `ShowBackdrop` | `bool` | `true` | If true (default), the backdrop is rendered. |
| `Side` | `string` | `"bottom"` | Side of the trigger to open on (see [`IonPopover.Side`](#ionpopover)). |
| `Alignment` | `string?` | `null` | Alignment with the trigger (see [`IonPopover.Alignment`](#ionpopover)). |
| `Arrow` | `bool` | `true` | If true (default), renders the arrow element. |
| `Translucent` | `bool` | `false` | Stamps `popover-translucent`; a marker only. |
| `CssClass` | `string?` | `null` | Extra CSS class(es) for the popover host (its `Class`). |

### IonPopoverController

Creates popovers from code (Ionic's `popoverController`). Inject it; `AddIonic()` registers it.

| Method | Returns | Description |
| --- | --- | --- |
| `CreateAsync(IonPopoverOptions options)` | `Task<IonOverlayReference>` | Creates a popover from `options`. Call [`IonOverlayReference.PresentAsync`](./overlays.md#ionoverlayreference) on the result to show it. |
<!-- /miko:type -->

## Styling

`IonPopover` has no theme token of its own. The content box uses the theme's `BackgroundColor` and `TextColor`, and the backdrop color comes from `IonicTheme.Alert.BackdropColor` (see [Alert](./alert.md#theme-tokens)). The backdrop is much lighter on ios (opacity 0.08) than on md (0.32).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-popover` | The full-screen host. Also carries the mode (`md` / `ios`); `.overlay-hidden` while closed. |
| `.popover-side-{side}` | The side actually used after flipping: `top`, `bottom`, `left`, or `right`. |
| `.popover-backdrop` | The dim layer (also `.ion-backdrop`); omitted when `ShowBackdrop="false"`. |
| `.popover-wrapper` | The positioned box that holds the arrow and the content. |
| `.popover-content` | The visible panel holding your content: width, background, radius, shadow. |
| `.popover-arrow` | The arrow element (`Arrow="true"`). |
| `.popover-translucent` | `Translucent="true"`. Only a marker; no translucent style is applied. |

```csharp
// GlobalStyles.cs — a wider menu: <IonPopover Class="wide" …>
[".ion-popover.wide .popover-content"] = new()
{
    Width = Length.Px(320),
},
```

When an `Event` is set, the position calculation assumes the default content width (250 / 200 px), so a much wider panel near the right edge can overflow the screen.
