# Reorder

Reorder lets the user drag items of a list into a new order by a handle.

`IonReorderGroup` wraps the items and runs the drag gesture; `IonReorder` is the drag handle placed inside each item. A handle in an item's `Start` or `End` slot only appears inside an enabled `IonReorderGroup` — without one it is hidden.

## Basic usage

Put one child per data item in the group, each containing an `IonReorder` (usually in the item's `End` slot). Reordering is off by default (`Disabled` defaults to `true`, as in Ionic), so set `Disabled="false"`.

When the user drops an item, `OnItemReorder` receives `(From, To)` — the item's old and new index. **The group does not move anything itself**: apply the move to your collection in the handler, and the re-render shows the new order. The dragged item stays where it was dropped until the handler returns. There is no `complete()` call as in Ionic. `OnItemReorder` is raised on every drop, including one at the original index (`From == To`).

<!-- miko:example examples/reorder/Basic.razor -->
```razor
@* The group reports the move; the app applies it to its own list. *@
<IonList>
    <IonReorderGroup Disabled="false" OnItemReorder="Move">
        @foreach (var task in _tasks)
        {
            <IonItem>
                <ChildContent>
                    <IonLabel>@task</IonLabel>
                </ChildContent>
                <End>
                    <IonReorder />
                </End>
            </IonItem>
        }
    </IonReorderGroup>
</IonList>

@code {
    private readonly List<string> _tasks = new() { "Buy milk", "Walk the dog", "Pay rent", "Call mom" };

    private void Move((int From, int To) move)
    {
        var task = _tasks[move.From];
        _tasks.RemoveAt(move.From);
        _tasks.Insert(move.To, task);
    }
}
```
<!-- /miko:example -->

Indexes count only the group's children that contain an `IonReorder`, in render order, so keep exactly one child per data item and do not mix in children without a handle (headers, dividers) — use a separate group for each section instead.

## Toggling reordering

Bind `Disabled` to switch reordering on and off, for example from an Edit button. While disabled every slotted handle is hidden and presses do nothing.

<!-- miko:example examples/reorder/Toggle.razor -->
```razor
@* Disabled defaults to true: the handles stay hidden until reordering is switched on. *@
<IonButton OnClick="@(() => _editing = !_editing)">@(_editing ? "Done" : "Edit")</IonButton>

<IonList>
    <IonReorderGroup Disabled="@(!_editing)" OnItemReorder="Move">
        @foreach (var city in _cities)
        {
            <IonItem>
                <ChildContent>
                    <IonLabel>@city</IonLabel>
                </ChildContent>
                <End>
                    <IonReorder />
                </End>
            </IonItem>
        }
    </IonReorderGroup>
</IonList>

@code {
    private readonly List<string> _cities = new() { "Lisbon", "Oslo", "Kyoto", "Lima" };
    private bool _editing;

    private void Move((int From, int To) move)
    {
        var city = _cities[move.From];
        _cities.RemoveAt(move.From);
        _cities.Insert(move.To, city);
    }
}
```
<!-- /miko:example -->

## Custom handle

An `IonReorder` with content shows that content as the handle instead of the default reorder icon.

<!-- miko:example examples/reorder/CustomHandle.razor -->
```razor
<IonList>
    <IonReorderGroup Disabled="false" OnItemReorder="Move">
        @foreach (var song in _songs)
        {
            <IonItem>
                <ChildContent>
                    <IonLabel>@song</IonLabel>
                </ChildContent>
                <End>
                    <IonReorder>
                        <IonIcon Icon="@Ionicons.SwapVertical" />
                    </IonReorder>
                </End>
            </IonItem>
        }
    </IonReorderGroup>
</IonList>

@code {
    private readonly List<string> _songs = new() { "Intro", "Verse", "Chorus", "Outro" };

    private void Move((int From, int To) move)
    {
        var song = _songs[move.From];
        _songs.RemoveAt(move.From);
        _songs.Insert(move.To, song);
    }
}
```
<!-- /miko:example -->

## Dragging the whole item

Wrap the item in `IonReorder` to make the whole row the handle. While the group is disabled the wrapped items stay visible and simply cannot be dragged.

<!-- miko:example examples/reorder/WholeItem.razor -->
```razor
@* Wrapping the item makes the whole row the drag handle. *@
<IonList>
    <IonReorderGroup Disabled="false" OnItemReorder="Move">
        @foreach (var photo in _photos)
        {
            <IonReorder>
                <IonItem>
                    <IonLabel>@photo</IonLabel>
                </IonItem>
            </IonReorder>
        }
    </IonReorderGroup>
</IonList>

@code {
    private readonly List<string> _photos = new() { "Beach", "Mountains", "City", "Forest" };

    private void Move((int From, int To) move)
    {
        var photo = _photos[move.From];
        _photos.RemoveAt(move.From);
        _photos.Insert(move.To, photo);
    }
}
```
<!-- /miko:example -->

## Haptics

Picking up an item gives a light impact, each time the drop position changes the selection ticks, and the selection ends on release — mirroring Ionic. The feedback is real only when the app registers its platform's native services (`UseAndroidNative()`, `UseIosNative()`); see [Picker](./picker.md#haptics).

## API

<!-- miko:api IonReorder IonReorderGroup -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonReorder

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonReorderGroup

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `true` | If true (default, matching Ionic), the reorder handles are hidden and the group is non-interactive. Set to false to reveal the handles. |

| Event | Type | Description |
| --- | --- | --- |
| `OnItemReorder` | `EventCallback<ValueTuple<int, int>>` | Raised when a drag gesture completes, with the original and destination child indexes. The callback is raised even when the item is released at its original index, matching Ionic's `ionReorderEnd` contract. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

Reorder has no theme tokens; the default icon is 31px at 30% opacity in md mode and 34px at 40% in ios mode.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-reorder` | The handle. Also carries the mode (`md` / `ios`). |
| `.reorder-enabled`, `.reorder-hidden` | On a handle: its group is enabled (shown) / missing or disabled (hidden). |
| `.ion-reorder .reorder-icon` | The default handle icon. |
| `.ion-reorder-group` | The group. |
| `.reorder-enabled`, `.reorder-group-disabled` | On the group: `Disabled` is false / true. |
| `.reorder-list-active` | On the group while a drag is in progress. |
| `.reorder-selected` | The item being dragged (dimmed, with a shadow). |
| `.reorder-item-shift-up`, `.reorder-item-shift-down` | Items moved aside to make room during a drag. |

```csharp
// GlobalStyles.cs — a stronger lift for the dragged item
[".ion-reorder-group.reorder-list-active .reorder-selected"] = new()
{
    Opacity = 1f,
    BoxShadow = new List<BoxShadow> { new(0, 4, 16, 0, Color.Rgba(0, 0, 0, 0.3f)) },
},
```
