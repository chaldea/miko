# Infinite Scroll

Infinite scroll calls the app to load more data when the user scrolls near the end of a scrolling list.

`IonInfiniteScroll` watches the scroll position and raises `OnInfinite`; `IonInfiniteScrollContent` inside it is the loading indicator (a spinner and optional text) shown while the app loads.

## Basic usage

Place `IonInfiniteScroll` inside the scrolling content of an [`IonContent`](./content.md), after the list. It listens to the scroll events of the nearest scrolling ancestor, so it must not sit inside a nested scroll container of its own. It only checks while the user scrolls: if the first page of data does not fill the viewport, nothing can scroll and `OnInfinite` never fires — load enough rows up front.

When the remaining distance to the end drops below `Threshold`, the infinite scroll enters its loading state and raises `OnInfinite` once. **The handler must call `await e.Complete()`** when its work is done; until then the loading indicator stays visible and no further `OnInfinite` is raised. Render the new rows first (`StateHasChanged()`), then complete. When there is no more data, set `Disabled="true"`: the component is hidden and stops raising events.

<!-- miko:example examples/infinite-scroll/Basic.razor -->
```razor
@* The infinite scroll goes at the end of IonContent's scrolling content, after the list. *@
<IonContent>
    <IonList>
        @foreach (var item in _items)
        {
            <IonItem>
                <IonLabel>@item</IonLabel>
            </IonItem>
        }
    </IonList>

    <IonInfiniteScroll Disabled="@(!_hasMore)" OnInfinite="LoadMore">
        <IonInfiniteScrollContent LoadingSpinner="circular" />
    </IonInfiniteScroll>
</IonContent>

@code {
    private readonly List<string> _items = Enumerable.Range(1, 30).Select(i => $"Item {i}").ToList();
    private bool _hasMore = true;

    private async Task LoadMore(IonInfiniteScrollCustomEvent e)
    {
        await Task.Delay(500); // stand-in for a network request

        var start = _items.Count;
        _items.AddRange(Enumerable.Range(start + 1, 20).Select(i => $"Item {i}"));
        _hasMore = _items.Count < 100;

        // Render the new rows first, then re-arm the infinite scroll.
        StateHasChanged();
        await e.Complete();
    }
}
```
<!-- /miko:example -->

## Threshold and loading content

`Threshold` is either a percentage of the visible height (`"15%"`, the default) or a pixel distance (`"100px"`). `IonInfiniteScrollContent` shows `LoadingSpinner` (an [`IonSpinner`](./spinner.md) name) and `LoadingText` only while loading. Without `LoadingSpinner` the mode's default spinner is shown, as in Ionic (`lines` on ios, `crescent` on md); `LoadingSpinner=""` shows none. The content always reserves its minimum height (84px) at the end of the list while the infinite scroll is enabled.

<!-- miko:example examples/infinite-scroll/Content.razor -->
```razor
<IonContent>
    <IonList>
        @foreach (var item in _items)
        {
            <IonItem>
                <IonLabel>@item</IonLabel>
            </IonItem>
        }
    </IonList>

    @* Fire 100px before the end instead of the default 15% of the viewport height. *@
    <IonInfiniteScroll Threshold="100px" OnInfinite="LoadMore">
        <IonInfiniteScrollContent LoadingSpinner="bubbles" LoadingText="Loading more data..." />
    </IonInfiniteScroll>
</IonContent>

@code {
    private readonly List<string> _items = Enumerable.Range(1, 30).Select(i => $"Item {i}").ToList();

    private async Task LoadMore(IonInfiniteScrollCustomEvent e)
    {
        await Task.Delay(500);
        _items.AddRange(Enumerable.Range(_items.Count + 1, 20).Select(i => $"Item {i}"));
        StateHasChanged();
        await e.Complete();
    }
}
```
<!-- /miko:example -->

## Loading at the top

With `Position="top"`, place the infinite scroll before the content; it fires when the user scrolls to within `Threshold` of the top, for example to load older chat messages. Unlike Ionic, the scroll position is not adjusted after rows are prepended, and `IonContent` has no scroll-to-bottom call to start the user at the end.

<!-- miko:example examples/infinite-scroll/Top.razor -->
```razor
@* Position="top": place it before the content; it fires when the user scrolls near the top. *@
<IonContent>
    <IonInfiniteScroll Position="top" Disabled="@(_oldest == 1)" OnInfinite="LoadOlder">
        <IonInfiniteScrollContent LoadingSpinner="crescent" />
    </IonInfiniteScroll>

    <IonList>
        @foreach (var message in _messages)
        {
            <IonItem>
                <IonLabel>@message</IonLabel>
            </IonItem>
        }
    </IonList>
</IonContent>

@code {
    private int _oldest = 81;
    private readonly List<string> _messages = Enumerable.Range(81, 20).Select(i => $"Message {i}").ToList();

    private async Task LoadOlder(IonInfiniteScrollCustomEvent e)
    {
        await Task.Delay(500);

        var from = Math.Max(1, _oldest - 20);
        _messages.InsertRange(0, Enumerable.Range(from, _oldest - from).Select(i => $"Message {i}"));
        _oldest = from;

        StateHasChanged();
        await e.Complete();
    }
}
```
<!-- /miko:example -->

## Loading state from code

`Loading="true"` puts the infinite scroll into its loading state (indicator shown, `OnInfinite` blocked), and re-applies it on every render while it stays true. To end it, set `Loading` back to `false` and call `Complete()` on the component (`e.Target` in an `OnInfinite` handler).

## API

<!-- miko:api IonInfiniteScroll IonInfiniteScrollContent -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonInfiniteScroll

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | When true, the infinite scroll is hidden and does not emit events. |
| `Loading` | `bool` | `false` | Declarative loading state: while true, every render puts the infinite scroll into its loading state. Setting it back to false does not end a load — call [`Complete`](#ioninfinitescroll). |
| `Position` | `string` | `"bottom"` | Position of the infinite scroll marker: `"bottom"` (default) or `"top"`. |
| `Threshold` | `string` | `"15%"` | Threshold distance from the edge of the content at which [`OnInfinite`](#ioninfinitescroll) fires. Either a percentage of the viewport height (`"15%"`, the default) or a pixel distance (`"100px"`). |

| Event | Type | Description |
| --- | --- | --- |
| `OnInfinite` | `EventCallback<IonInfiniteScrollCustomEvent>` | Raised when the scroll position crosses the threshold. The handler must call [`IonInfiniteScrollCustomEvent.Complete`](#ioninfinitescrollcustomevent) once its async work is done, otherwise the infinite scroll stays in the loading state and never fires again. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonInfiniteScrollContent

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `LoadingSpinner` | `string?` | `null` | Spinner name (an [`IonSpinner`](./spinner.md#ionspinner) `Name`) to show while loading. When null, the mode default is used — `"lines"` on ios, `"crescent"` on md, as in Ionic; an empty string shows no spinner. |
| `LoadingText` | `string?` | `null` | Optional text to display while loading. |
<!-- /miko:api -->

## Types

<!-- miko:type IonInfiniteScrollCustomEvent -->
### IonInfiniteScrollCustomEvent

Payload for [`IonInfiniteScroll.OnInfinite`](#ioninfinitescroll). Mirrors Ionic's `IonInfiniteScrollCustomEvent`, where the handler reaches the emitting component through `event.target` and signals the end of the async work with `event.target.complete()`. [`Complete`](#ioninfinitescrollcustomevent) is forwarded to [`Target`](#ioninfinitescrollcustomevent) so handlers do not need an element reference (`@ref`) on the component: `OnInfinite="@(async e => { await LoadMore(); await e.Complete(); })"`

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Target` | `IonInfiniteScroll` | — | The infinite scroll that raised the event (Ionic's `event.target`). (read-only) |

| Method | Returns | Description |
| --- | --- | --- |
| `Complete()` | `Task` | Signals that the async work started by this event has finished, clearing the loading state and re-arming the infinite scroll for the next threshold crossing. Shorthand for `Target.Complete()`. |
<!-- /miko:type -->

## Styling

### Theme tokens

<!-- miko:tokens InfiniteScroll -->
Theme token `IonicTheme.InfiniteScroll` (`InfiniteScrollToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.InfiniteScroll = new InfiniteScrollToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `ContentMinHeight` | `float` | `84` | `84` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-infinite-scroll` | The host (hidden while `Disabled`). Also carries the mode (`md` / `ios`). |
| `.infinite-scroll-enabled`, `.infinite-scroll-loading`, `.infinite-scroll-top` | Not disabled, loading, `Position="top"`. |
| `.ion-infinite-scroll-content` | The indicator area. Also carries `.infinite-scroll-content-md` / `-ios`. |
| `.ion-infinite-scroll-content .infinite-loading` | The part shown while loading. |
| `.infinite-loading-spinner`, `.infinite-loading-spinner-icon` | The spinner wrapper and the spinner. |
| `.infinite-loading-text` | The loading text. |

```csharp
// GlobalStyles.cs — less empty space below the list
[".ion-infinite-scroll-content"] = new()
{
    MinHeight = Length.Px(48),
},
```
