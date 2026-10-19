# Refresher

The refresher provides pull-to-refresh on a scrolling content area: pulling down from the top reveals an indicator, and releasing far enough starts a refresh.

`IonRefresher` handles the gesture and the refresh state; `IonRefresherContent` is the indicator shown inside it (an icon and text while pulling, a spinner and text while refreshing). The refresher has no default visuals — without an `IonRefresherContent` the pull reveals an empty strip.

## Basic usage

Place the `IonRefresher` in the `Fixed` slot of an [`IonContent`](./content.md) and the scrolling content in `ChildContent`. This placement is required: `IonContent` owns the pointer handling and forwards pulls to the refresher in its `Fixed` slot, and the pull moves the content's scroll area, not the refresher. Outside an `IonContent` the refresher never receives a gesture.

A pull can only start while the content is scrolled to the very top. Releasing after pulling at least `PullMin` (default 60px) starts a refresh, and pulling past `PullMax` (default `PullMin + 60`) starts it without waiting for the release; a shorter pull slides back. `OnRefresh` then fires with an `IonRefresherCustomEvent`. **The handler must call `await e.Complete()`** when its work is done — until then the refresher stays in the refreshing state with the content held down, and no new pull can start.

<!-- miko:example examples/refresher/Basic.razor -->
```razor
@* The refresher goes in IonContent's Fixed slot; the list goes in ChildContent. *@
<IonContent>
    <Fixed>
        <IonRefresher OnRefresh="Refresh">
            <IonRefresherContent />
        </IonRefresher>
    </Fixed>
    <ChildContent>
        <IonList>
            @foreach (var message in _messages)
            {
                <IonItem>
                    <IonLabel>@message</IonLabel>
                </IonItem>
            }
        </IonList>
    </ChildContent>
</IonContent>

@code {
    private readonly List<string> _messages = new() { "Welcome", "Your order shipped" };

    private async Task Refresh(IonRefresherCustomEvent e)
    {
        await Task.Delay(1000); // stand-in for a network request
        _messages.Insert(0, $"New message at {DateTime.Now:t}");

        // Required: until Complete() is called the refresher keeps spinning.
        await e.Complete();
    }
}
```
<!-- /miko:example -->

## Indicator content

`IonRefresherContent` shows `PullingIcon` (default `arrow-down` in md, `refresh` in ios; it flips when a release would refresh) and `PullingText` while pulling, then `RefreshingSpinner` (default `circular` in md, `lines` in ios — any [`IonSpinner`](./spinner.md) name) and `RefreshingText` while refreshing.

<!-- miko:example examples/refresher/Content.razor -->
```razor
<IonContent>
    <Fixed>
        <IonRefresher PullMin="80" OnRefresh="Refresh">
            <IonRefresherContent PullingIcon="@Ionicons.ChevronDownCircleOutline"
                                 PullingText="Pull to refresh"
                                 RefreshingSpinner="dots"
                                 RefreshingText="Refreshing..." />
        </IonRefresher>
    </Fixed>
    <ChildContent>
        <p class="ion-padding">Pull down from the top.</p>
    </ChildContent>
</IonContent>

@code {
    private async Task Refresh(IonRefresherCustomEvent e)
    {
        await Task.Delay(1000);
        await e.Complete();
    }
}
```
<!-- /miko:example -->

## Cancelling a refresh

`e.Target` is the `IonRefresher` itself. Besides `Complete()`, it has `Cancel()` (end the pull or refresh without the completing state), `BeginRefresh()` (start a refresh as if the user had pulled), and `GetProgress()` (the pull distance relative to `PullMin`). Call `Cancel()` when the work fails, so the refresher still closes.

<!-- miko:example examples/refresher/Cancel.razor -->
```razor
<IonContent>
    <Fixed>
        <IonRefresher OnRefresh="Refresh">
            <IonRefresherContent />
        </IonRefresher>
    </Fixed>
    <ChildContent>
        <p class="ion-padding">@_status</p>
    </ChildContent>
</IonContent>

@code {
    private string _status = "Pull down to sync.";

    private async Task Refresh(IonRefresherCustomEvent e)
    {
        try
        {
            await SyncAsync();
            _status = $"Synced at {DateTime.Now:t}";
            await e.Complete();
        }
        catch (Exception ex)
        {
            _status = $"Sync failed: {ex.Message}";
            // Cancel() slides the content back without the "completing" state.
            await e.Target.Cancel();
        }
    }

    private static Task SyncAsync() => Task.Delay(500);
}
```
<!-- /miko:example -->

## Disabling and declarative state

`Disabled="true"` turns pull-to-refresh off. `State` sets the refresher's state declaratively (`inactive`, `pulling`, `ready`, `refreshing`, `cancelling`, `completing`); it is applied only when its value changes, and between changes the gesture drives the state.

## API

<!-- miko:api IonRefresher IonRefresherContent -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonRefresher

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | When true, the refresher is hidden and non-interactive. |
| `PullMax` | `int?` | `null` | Pull distance at which the refresh starts without waiting for the release. Defaults to `PullMin + 60`, as in Ionic. |
| `PullMin` | `int` | `60` | Minimum pull distance; releasing beyond it starts a refresh. |
| `State` | `string` | `"inactive"` | Declarative refresher state: inactive, pulling, ready, refreshing, cancelling or completing. Applied when the value changes; in between, the pull gesture drives the state, and a refresh stays in progress across re-renders until [`Complete`](#ionrefresher) is called. |

| Event | Type | Description |
| --- | --- | --- |
| `OnRefresh` | `EventCallback<IonRefresherCustomEvent>` | Raised when a refresh starts. The refresher keeps showing its refreshing state until the handler calls [`IonRefresherCustomEvent.Complete`](#ionrefreshercustomevent) (Ionic's `ionRefresh` and `complete()`). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonRefresherContent

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `PullingIcon` | `string?` | `null` | Icon to show while pulling. Defaults to arrow-down (md) / refresh (ios). |
| `PullingText` | `string?` | `null` | Text to show while pulling. |
| `RefreshingSpinner` | `string?` | `null` | Spinner name (an [`IonSpinner`](./spinner.md#ionspinner) `Name`) to show while refreshing. Defaults to `"circular"` (md) / `"lines"` (ios). |
| `RefreshingText` | `string?` | `null` | Text to show while refreshing. |
<!-- /miko:api -->

## Types

<!-- miko:type IonRefresherCustomEvent -->
### IonRefresherCustomEvent

Payload for [`IonRefresher.OnRefresh`](#ionrefresher). Mirrors Ionic's `RefresherCustomEvent`, where the handler ends the refresh with `event.detail.complete()` (or `event.target.complete()`) once its async work is done. [`Complete`](#ionrefreshercustomevent) is forwarded to [`Target`](#ionrefreshercustomevent) so handlers do not need an element reference (`@ref`) on the component: `OnRefresh="@(async e => { await Reload(); await e.Complete(); })"`

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Target` | `IonRefresher` | — | The refresher that raised the event (Ionic's `event.target`). (read-only) |

| Method | Returns | Description |
| --- | --- | --- |
| `Complete()` | `Task` | Signals that the refresh has finished: the refresher shows its completing state and the content slides back to the top. Shorthand for `Target.Complete()`. |
<!-- /miko:type -->

## Styling

### Theme tokens

<!-- miko:tokens Refresher -->
Theme token `IonicTheme.Refresher` (`RefresherToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Refresher = new RefresherToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Height` | `float` | `60` | `60` |
| `IconFontSize` | `float` | `30` | `30` |
| `TextFontSize` | `float` | `16` | `16` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-refresher` | The refresher strip above the content (hidden while inactive). Also carries the mode (`md` / `ios`) and `.refresher-md` / `.refresher-ios`. |
| `.refresher-active` | The refresher is not inactive (it is laid out and visible). |
| `.refresher-pulling`, `.refresher-ready`, `.refresher-refreshing`, `.refresher-cancelling`, `.refresher-completing` | On `.ion-refresher`: the current state. |
| `.refresher-disabled` | `Disabled`. |
| `.ion-refresher-content` | The indicator. |
| `.ion-refresher-content .refresher-pulling` | The pulling part: `.refresher-pulling-icon` and `.refresher-pulling-text`. |
| `.ion-refresher-content .refresher-refreshing` | The refreshing part: `.refresher-refreshing-icon` (the spinner) and `.refresher-refreshing-text`. |

```csharp
// GlobalStyles.cs — muted, smaller refreshing text
[".ion-refresher-content .refresher-refreshing-text"] = new()
{
    Color = Color.FromHex("#64748b"),
    FontSize = Length.Px(14),
},
```
