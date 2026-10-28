using Miko.Common;
using Miko.Core;
using Miko.Layout;

namespace Miko.Components.Virtualization;

/// <summary>
/// Renders only the items of a list that can be seen, plus a few on either side, and stands in for the
/// rest with two spacers sized to them (ISSUE-151). A list of ten thousand rows then builds, styles and
/// lays out about as many elements as the screen holds, and scrolling it costs the same as scrolling
/// a list of twenty. Same API as Blazor's <c>Virtualize&lt;TItem&gt;</c>, plus <see cref="ItemKey"/>.
///
/// <code>
/// &lt;IonContent&gt;
///     &lt;IonList&gt;
///         &lt;Virtualize Items="@_users" ItemKey="@(u =&gt; u.Id)" ItemSize="72" Context="user"&gt;
///             &lt;IonItem&gt;&lt;IonLabel&gt;@user.Name&lt;/IonLabel&gt;&lt;/IonItem&gt;
///         &lt;/Virtualize&gt;
///     &lt;/IonList&gt;
/// &lt;/IonContent&gt;
/// </code>
///
/// <para><b>Items may have any height.</b> <see cref="ItemSize"/> is only the estimate used before
/// anything has been measured. Every item that is rendered is measured after layout, and items that
/// have not been rendered count as the average of the measured ones. When a measurement changes the
/// height of content above what is on screen (scrolling up into rows taller than estimated), the list
/// adjusts the scroll offset by the difference, so what the user is looking at does not move.</para>
///
/// <para><b>How it hooks into the engine.</b> The list renders a layout-transparent element that the
/// engine notifies, after every layout and every scroll and before the frame is painted, of the part
/// of the viewport it can be seen through. When the visible items are no longer covered the list
/// re-renders right there, so a frame never shows blank space where rows should be.</para>
///
/// <para><b>Items keep their elements while they stay in view.</b> Moving the window builds only the
/// items that scroll in; the others keep their element and component instances. The item template
/// runs again for every item when the list's parent re-renders or <see cref="RefreshDataAsync"/> is
/// called, so call one of them after changing the data.</para>
///
/// <para>The list is vertical. It must be inside an element that scrolls (<c>IonContent</c>, or any
/// box with <c>overflow-y: auto</c>); otherwise all of it is "visible" and it renders up to
/// <see cref="MaxItemCount"/> items.</para>
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public class Virtualize<TItem> : ComponentBase
{
    // ---------------------------------------------------------------- parameters

    /// <summary>
    /// The template for each item; <c>context</c> (or the name given with <c>Context="…"</c>) is the
    /// item. Write the template directly between the tags, or in an <see cref="ItemContent"/> element.
    /// </summary>
    [Parameter] public new RenderFragment<TItem>? ChildContent { get; set; }

    /// <summary>The template for each item, when it is written as an <c>&lt;ItemContent&gt;</c> element.</summary>
    [Parameter] public RenderFragment<TItem>? ItemContent { get; set; }

    /// <summary>
    /// Shown for an item that <see cref="ItemsProvider"/> is still loading. Defaults to an empty element
    /// of the estimated item height.
    /// </summary>
    [Parameter] public RenderFragment<PlaceholderContext>? Placeholder { get; set; }

    /// <summary>Shown instead of the items when there are none.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>
    /// The estimated height of an item, in pixels (default 50). Only used until items have been
    /// measured; a value close to the real height makes the first frame and the scrollbar more accurate.
    /// </summary>
    [Parameter] public float ItemSize { get; set; } = 50f;

    /// <summary>The items, when they are all in memory. Use either this or <see cref="ItemsProvider"/>.</summary>
    [Parameter] public ICollection<TItem>? Items { get; set; }

    /// <summary>
    /// Loads items on demand, for lists too long or too remote to hold in memory: called with the range
    /// that is about to be shown. Use either this or <see cref="Items"/>.
    /// </summary>
    [Parameter] public ItemsProviderDelegate<TItem>? ItemsProvider { get; set; }

    /// <summary>How many items to render above and below the visible ones (default 3).</summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>
    /// The most items rendered at once (default 100), whatever the viewport or the measured heights.
    /// </summary>
    [Parameter] public int MaxItemCount { get; set; } = 100;

    /// <summary>
    /// The tag of the two spacer elements that stand in for the items not rendered (default
    /// <c>"div"</c>). Use <c>"tr"</c> when the items are table rows.
    /// </summary>
    [Parameter] public string SpacerElement { get; set; } = "div";

    /// <summary>
    /// A stable identity for each item (Miko extension). With it, an item keeps its elements, component
    /// state and measured height when items are inserted or removed before it, and inserting items
    /// above the visible ones does not move what is on screen. Without it, items are identified by their
    /// index. Keys must be unique.
    /// </summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    // ---------------------------------------------------------------- state

    private readonly ItemSizeIndex _sizes = new(50f);

    // Bumped on every parent render and RefreshDataAsync: slots built in an older generation run their
    // template again (see VirtualizeSlot).
    private int _generation;
    private bool _selfRender;

    // The rendered window [_windowStart, _windowEnd). Changed only right before a render, so between
    // renders it is what the DOM shows.
    private int _windowStart;
    private int _windowEnd;

    // Items mode: the collection the item count and keys were last taken from.
    private ICollection<TItem>? _keyedItems;
    private int _itemCount;

    // ItemKey: where each key is (and the reverse), and the measured height of each key (survives the
    // item moving).
    private Dictionary<object, int>? _keyToIndex;
    private List<object>? _indexToKey;
    private Dictionary<object, float>? _sizeByKey;

    // ItemsProvider mode.
    private ItemsProviderDelegate<TItem>? _provider;
    private bool _countKnown;
    private List<TItem>? _loadedItems;
    private int _loadedStart;
    private (int Start, int End)? _pendingRange;
    private CancellationTokenSource? _pendingCts;

    // What the last render produced, in window order (slot i is item _windowStart + i).
    private readonly List<VirtualizeSlot> _rendered = new();

    // The measured start of each rendered slot, in layout coordinates, plus one entry for the end of the
    // last; empty until the layout the last render produced has been measured.
    private readonly List<float> _slotStarts = new();
    private LayoutBox? _measuredLayout;

    // Scroll anchoring: the first item on screen and where it was, to keep it still when a new layout
    // moves it (items above it measured taller or shorter than estimated, items inserted above it).
    private object? _anchorKey;
    private int _anchorIndex = -1;
    private float _anchorY;

    // The scroll container's box when the anchor was recorded. After a layout it is the previous
    // layout's box, which still holds the offset before the engine clamped it to the new range.
    private LayoutBox? _anchorScroller;

    private int ItemCount => ItemsProvider is null ? _itemCount : (_countKnown ? _itemCount : 0);

    private RenderFragment<TItem>? ItemTemplate => ItemContent ?? ChildContent;

    // ---------------------------------------------------------------- lifecycle

    protected override void OnParametersSet()
    {
        // A render the list asked for itself (the window moved) keeps the slots built so far; any other
        // render comes from the parent, whose data may have changed.
        if (_selfRender) return;

        Validate();
        _generation++;
        _sizes.DefaultSize = ItemSize;

        if (ItemsProvider is not null) SyncProvider();
        else SyncItems(force: false);
    }

    protected override void OnDispose()
    {
        _pendingCts?.Cancel();
        _pendingCts = null;
    }

    /// <summary>
    /// Runs the item template again for every rendered item and, with an <see cref="ItemsProvider"/>,
    /// asks it again for the items on screen (the items shown so far stay until the new ones arrive).
    /// Call it after changing the data the list shows without re-rendering the list's parent.
    /// </summary>
    public async Task RefreshDataAsync()
    {
        _generation++;
        if (ItemsProvider is null)
        {
            SyncItems(force: true);
            RenderSelf();
            return;
        }

        // The total may have changed too: an empty window asks again, like the first time.
        if (_windowEnd <= _windowStart)
        {
            _countKnown = false;
            _loadedItems = null;
            _pendingRange = null;
            RenderSelf();
            return;
        }

        var task = Request(_windowStart, _windowEnd - _windowStart);
        if (task is not null) await task;
        else RenderSelf();
    }

    private void Validate()
    {
        if (Items is not null && ItemsProvider is not null)
            throw new InvalidOperationException(
                $"{nameof(Virtualize<TItem>)} takes either {nameof(Items)} or {nameof(ItemsProvider)}, not both.");
        if (Items is null && ItemsProvider is null)
            throw new InvalidOperationException(
                $"{nameof(Virtualize<TItem>)} needs {nameof(Items)} or {nameof(ItemsProvider)}.");
        if (ItemTemplate is null)
            throw new InvalidOperationException(
                $"{nameof(Virtualize<TItem>)} needs an item template: write it between the tags or in an <{nameof(ItemContent)}> element.");
        if (!(ItemSize > 0f))
            throw new InvalidOperationException($"{nameof(Virtualize<TItem>)}.{nameof(ItemSize)} must be greater than 0.");
        if (OverscanCount < 0)
            throw new InvalidOperationException($"{nameof(Virtualize<TItem>)}.{nameof(OverscanCount)} must not be negative.");
        if (MaxItemCount < 1)
            throw new InvalidOperationException($"{nameof(Virtualize<TItem>)}.{nameof(MaxItemCount)} must be at least 1.");
    }

    /// <summary>Re-renders the list itself: the slots of items still in the window keep their elements.</summary>
    private void RenderSelf()
    {
        _selfRender = true;
        try
        {
            RenderNow();
        }
        finally
        {
            _selfRender = false;
        }
    }

    // ---------------------------------------------------------------- data sources

    /// <summary>
    /// Takes the item count from <see cref="Items"/> and, with <see cref="ItemKey"/>, moves each measured
    /// height to the item's current index. Keys are recomputed when the collection or its count changed
    /// (or <paramref name="force"/>), or when an item in the window is no longer where it was — so an
    /// insertion before the window is noticed without walking every item on every parent render.
    /// </summary>
    private void SyncItems(bool force)
    {
        if (_provider is not null) ResetProviderState();

        var items = Items!;
        int count = items.Count;

        if (ItemKey is null)
        {
            _keyToIndex = null;
            _indexToKey = null;
            _sizeByKey = null;
            _sizes.Resize(count);
        }
        else if (force || _keyToIndex is null || !ReferenceEquals(items, _keyedItems) || !WindowKeysUnchanged(items))
        {
            Rekey(items, count);
        }
        else if (count != _itemCount && !TryRekeyTail(items, count))
        {
            Rekey(items, count);
        }

        _keyedItems = items;
        _itemCount = count;
        ClampWindow();
    }

    /// <summary>Whether every item in the rendered window still has the key it had at its index.</summary>
    private bool WindowKeysUnchanged(ICollection<TItem> items)
    {
        if (_keyToIndex is null) return false;
        for (int i = _windowStart; i < _windowEnd && i < items.Count; i++)
        {
            if (!TryGetFromItems(items, i, out var item)) return false;
            if (!_keyToIndex.TryGetValue(KeyOf(item, i), out var at) || at != i) return false;
        }
        return true;
    }

    private void Rekey(ICollection<TItem> items, int count)
    {
        var keyToIndex = new Dictionary<object, int>(count);
        var indexToKey = new List<object>(count);
        _sizeByKey ??= new Dictionary<object, float>();
        var sizes = new float[count];
        int i = 0;
        foreach (var item in items)
        {
            if (i >= count) break;
            var key = KeyOf(item, i);
            keyToIndex[key] = i;
            indexToKey.Add(key);
            sizes[i] = _sizeByKey.TryGetValue(key, out var size) ? size : float.NaN;
            i++;
        }
        for (; i < count; i++) sizes[i] = float.NaN;
        _sizes.Reset(sizes);
        _indexToKey = indexToKey;

        // Items were inserted or removed before the item on screen: move the window with it, so the
        // same items stay rendered and the anchor (applied after the next layout) keeps them still.
        if (_anchorKey is not null && _anchorIndex >= 0)
        {
            if (!keyToIndex.TryGetValue(_anchorKey, out var moved))
            {
                ClearAnchor();
            }
            else if (moved != _anchorIndex)
            {
                int delta = moved - _anchorIndex;
                _windowStart += delta;
                _windowEnd += delta;
                _anchorIndex = moved;
            }
        }

        _keyToIndex = keyToIndex;

        // Forget the heights of items that are gone, once there are many of them.
        if (_sizeByKey.Count > 2 * count + 64)
        {
            var stale = new List<object>();
            foreach (var key in _sizeByKey.Keys)
            {
                if (!keyToIndex.ContainsKey(key)) stale.Add(key);
            }
            foreach (var key in stale) _sizeByKey.Remove(key);
        }
    }

    /// <summary>
    /// The keys of a collection that only grew or shrank at its end — what loading another page does —
    /// in O(change) instead of O(count): the rendered items are where they were (checked by the caller)
    /// and so is the last item both versions have. Returns false when that does not hold; the caller then
    /// recomputes every key.
    /// </summary>
    private bool TryRekeyTail(ICollection<TItem> items, int count)
    {
        var keyToIndex = _keyToIndex!;
        var indexToKey = _indexToKey;
        int old = _itemCount;
        if (indexToKey is null || indexToKey.Count != old || items is not (IList<TItem> or IReadOnlyList<TItem>))
            return false;

        int last = Math.Min(old, count) - 1;
        if (last >= 0 && (!TryGetFromItems(items, last, out var lastItem) || !Equals(KeyOf(lastItem, last), indexToKey[last])))
            return false;

        if (count > old)
        {
            for (int i = old; i < count; i++)
            {
                TryGetFromItems(items, i, out var item);
                var key = KeyOf(item, i);
                // An appended item that was already in the list moved: recompute everything.
                if (!keyToIndex.TryAdd(key, i)) return false;
                indexToKey.Add(key);
            }
            _sizes.Resize(count);
            for (int i = old; i < count; i++)
            {
                if (_sizeByKey!.TryGetValue(indexToKey[i], out var size)) _sizes.Set(i, size);
            }
        }
        else
        {
            for (int i = count; i < old; i++) keyToIndex.Remove(indexToKey[i]);
            indexToKey.RemoveRange(count, old - count);
            _sizes.Resize(count);
        }
        return true;
    }

    private void SyncProvider()
    {
        if (_keyedItems is not null)
        {
            _keyedItems = null;
            _keyToIndex = null;
            _indexToKey = null;
            _sizeByKey = null;
        }

        // A different provider is a different list. (A method group converts to an equal delegate on
        // every render, so this is not triggered by the parent re-rendering.)
        if (_provider is not null && !Equals(_provider, ItemsProvider))
            ResetProviderState();
        _provider = ItemsProvider;
        ClampWindow();
    }

    private void ResetProviderState()
    {
        _pendingCts?.Cancel();
        _pendingCts = null;
        _pendingRange = null;
        _provider = null;
        _countKnown = false;
        _loadedItems = null;
        _loadedStart = 0;
        _itemCount = 0;
        _windowStart = _windowEnd = 0;
        ClearAnchor();
        _anchorKey = null;
        _sizes.Resize(0);
    }

    /// <summary>
    /// Asks the provider for <paramref name="count"/> items from <paramref name="start"/> unless they are
    /// loaded or already requested. Returns null when the request completed synchronously (its items
    /// are already applied; the caller renders) or was not needed; otherwise the task that applies the
    /// result and re-renders once it arrives.
    /// </summary>
    private Task? Request(int start, int count)
    {
        if (count <= 0) return null;
        if (_pendingRange is { } pending && pending.Start == start && pending.End == start + count) return _pendingTask;

        _pendingCts?.Cancel();
        var cts = new CancellationTokenSource();
        _pendingCts = cts;
        _pendingRange = (start, start + count);

        ValueTask<ItemsProviderResult<TItem>> call;
        try
        {
            call = ItemsProvider!(new ItemsProviderRequest(start, count, cts.Token));
        }
        catch (Exception ex)
        {
            Fail(cts, ex);
            return null;
        }

        if (call.IsCompletedSuccessfully)
        {
            Apply(cts, start, call.Result);
            return null;
        }

        _pendingTask = AwaitRequest(call, cts, start);
        return _pendingTask;
    }

    private Task? _pendingTask;

    // The token sources are cancelled but never disposed: a provider may still hold the token of a
    // request that was superseded (and register on it), and a source without a timer holds nothing
    // that needs releasing.

    private async Task AwaitRequest(ValueTask<ItemsProviderResult<TItem>> call, CancellationTokenSource cts, int start)
    {
        ItemsProviderResult<TItem> result;
        try
        {
            result = await call;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            Fail(cts, ex);
            return;
        }

        if (!Apply(cts, start, result)) return;
        RenderSelf();
    }

    /// <summary>Stores a provider result, unless a later request superseded it. Returns whether it was stored.</summary>
    private bool Apply(CancellationTokenSource cts, int start, ItemsProviderResult<TItem> result)
    {
        if (!ReferenceEquals(cts, _pendingCts) || cts.IsCancellationRequested) return false;

        _pendingCts = null;
        _pendingRange = null;
        _pendingTask = null;

        _loadedItems = result.Items is null ? new List<TItem>() : new List<TItem>(result.Items);
        _loadedStart = start;
        _itemCount = Math.Max(0, result.TotalItemCount);
        _countKnown = true;
        _sizes.Resize(_itemCount);
        ClampWindow();
        return true;
    }

    private void Fail(CancellationTokenSource cts, Exception ex)
    {
        if (ReferenceEquals(cts, _pendingCts))
        {
            _pendingCts = null;
            _pendingRange = null;
            _pendingTask = null;
        }
        Console.Error.WriteLine($"[Virtualize] {nameof(ItemsProvider)} failed: {ex}");
    }

    private bool IsLoaded(int index)
        => _loadedItems is { } loaded && index >= _loadedStart && index < _loadedStart + loaded.Count;

    private void ClampWindow()
    {
        int count = ItemCount;
        _windowEnd = Math.Clamp(_windowEnd, 0, count);
        _windowStart = Math.Clamp(_windowStart, 0, _windowEnd);
    }

    private object KeyOf(TItem item, int index)
        => ItemKey is { } itemKey ? itemKey(item) ?? index : index;

    private static bool TryGetFromItems(ICollection<TItem> items, int index, out TItem item)
    {
        switch (items)
        {
            case IList<TItem> list when index < list.Count:
                item = list[index];
                return true;
            case IReadOnlyList<TItem> list when index < list.Count:
                item = list[index];
                return true;
        }
        item = default!;
        return false;
    }

    // ---------------------------------------------------------------- rendering

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var host = builder.OpenElement<VirtualizeHostElement>();
        host.Handler = OnViewportChanged;

        // The layout this render produces has not been measured yet.
        _rendered.Clear();
        _slotStarts.Clear();
        _measuredLayout = null;

        RenderSpacer(builder, after: false, (float)_sizes.Prefix(_windowStart));

        if (ItemCount == 0)
        {
            // Before an ItemsProvider has answered there is nothing to say yet.
            if (EmptyContent is not null && (ItemsProvider is null || _countKnown))
                builder.AddContent(1, EmptyContent);
        }
        else if (_windowEnd > _windowStart)
        {
            var template = ItemTemplate!;
            // A collection without an indexer is enumerated once per render, for the window only.
            List<TItem>? window = ItemsProvider is null && Items is not (IList<TItem> or IReadOnlyList<TItem>)
                ? Items!.Skip(_windowStart).Take(_windowEnd - _windowStart).ToList()
                : null;

            for (int index = _windowStart; index < _windowEnd; index++)
            {
                if (window is not null)
                {
                    int at = index - _windowStart;
                    if (at < window.Count) RenderItem(builder, template, index, window[at]);
                    else RenderPlaceholder(builder, index);
                }
                else if (TryGetItem(index, out var item))
                {
                    RenderItem(builder, template, index, item);
                }
                else
                {
                    RenderPlaceholder(builder, index);
                }
            }
        }

        RenderSpacer(builder, after: true, (float)(_sizes.Total - _sizes.Prefix(_windowEnd)));
        builder.CloseElement();
    }

    private void RenderItem(RenderTreeBuilder builder, RenderFragment<TItem> template, int index, TItem item)
        => RenderSlot(builder, new VirtualizeSlotKey(KeyOf(item, index), Placeholder: false), template(item));

    private void RenderPlaceholder(RenderTreeBuilder builder, int index)
    {
        float size = _sizes.SizeOf(index);
        RenderFragment content = Placeholder is { } placeholder
            ? placeholder(new PlaceholderContext(index, size))
            : b => RenderBlock(b, SpacerElement, size, key: null);
        RenderSlot(builder, new VirtualizeSlotKey(index, Placeholder: true), content);
    }

    private void RenderSlot(RenderTreeBuilder builder, VirtualizeSlotKey key, RenderFragment content)
    {
        object boxedKey = key;
        var slot = builder.OpenComponent<VirtualizeSlot>(-1, boxedKey);
        slot.Content = content;
        slot.Generation = _generation;
        slot.SlotKey = boxedKey;
        builder.CloseComponent();
        _rendered.Add(slot);
    }

    private void RenderSpacer(RenderTreeBuilder builder, bool after, float height)
    {
        RenderBlock(builder, SpacerElement, height, new VirtualizeSpacerKey(after));
        if (after) _afterHeight = height;
        else _beforeHeight = height;
    }

    /// <summary>
    /// An empty element of <paramref name="height"/> pixels (see <see cref="SpacerStyle"/>).
    /// </summary>
    private static void RenderBlock(RenderTreeBuilder builder, string tag, float height, object? key)
    {
        builder.OpenElement(0, tag);
        var element = builder.CurrentElement!;
        element.Key = key;
        element.Style = SpacerStyle(height);
        builder.CloseElement();
    }

    /// <summary>
    /// <paramref name="height"/> pixels tall. The vertical margin, padding and border are pinned to zero
    /// so a stylesheet rule matching the tag cannot change what the element stands for, and it does not
    /// shrink in a flex column.
    /// </summary>
    private static Styling.Style SpacerStyle(float height) => new()
    {
        Height = Length.Px(Math.Max(0f, height)),
        FlexShrink = 0f,
        MarginTop = Length.Px(0),
        MarginBottom = Length.Px(0),
        PaddingTop = Length.Px(0),
        PaddingBottom = Length.Px(0),
        BorderTopWidth = Length.Px(0),
        BorderBottomWidth = Length.Px(0),
    };

    // The heights the spacers currently have (rendered, or rewritten by SyncSpacers).
    private float _beforeHeight;
    private float _afterHeight;

    /// <summary>
    /// Brings the two spacers to the heights the size index gives them now, in place. Measuring the rows
    /// of a render changes the estimate for every item that is not rendered, so the spacers that render
    /// produced no longer stand for the items they replace: the scroll height would be off by the change
    /// times the number of those items, leaving blank space at the end or ending the list early. Replacing
    /// a spacer's style restyles only the spacer (ISSUE-146 inline-style tier); the engine lays out again
    /// and the anchor keeps what is on screen still.
    /// </summary>
    private void SyncSpacers(VirtualizeHostElement host)
    {
        var children = host.Children;
        if (children.Count < 2
            || children[0] is not { Key: VirtualizeSpacerKey { After: false } } before
            || children[^1] is not { Key: VirtualizeSpacerKey { After: true } } after)
            return;

        float beforeHeight = Math.Max(0f, (float)_sizes.Prefix(_windowStart));
        float afterHeight = Math.Max(0f, (float)(_sizes.Total - _sizes.Prefix(_windowEnd)));
        if (Math.Abs(beforeHeight - _beforeHeight) >= 0.5f)
        {
            before.Style = SpacerStyle(beforeHeight);
            _beforeHeight = beforeHeight;
        }
        if (Math.Abs(afterHeight - _afterHeight) >= 0.5f)
        {
            after.Style = SpacerStyle(afterHeight);
            _afterHeight = afterHeight;
        }
    }

    private bool TryGetItem(int index, out TItem item)
    {
        if (ItemsProvider is not null)
        {
            if (IsLoaded(index))
            {
                item = _loadedItems![index - _loadedStart];
                return true;
            }
            item = default!;
            return false;
        }
        if (Items is { } items) return TryGetFromItems(items, index, out item);
        item = default!;
        return false;
    }

    // ---------------------------------------------------------------- viewport

    /// <summary>
    /// The engine's notification (see <see cref="IViewportObserver"/>): measures the items the last render
    /// produced once they are laid out, keeps the item on screen still when that layout moved it, and
    /// re-renders when the visible items are no longer all rendered.
    /// </summary>
    private bool OnViewportChanged(VirtualizeHostElement host, in ViewportObservation observation)
    {
        var current = observation;
        bool scrolled = false;
        bool measured = false;

        if (!ReferenceEquals(current.LayoutRoot, _measuredLayout))
        {
            _measuredLayout = current.LayoutRoot;
            Measure(host);
            measured = true;
            if (CompensateAnchor(host, ref current)) scrolled = true;
        }

        if (UpdateWindow(current))
        {
            RenderSelf();
            return scrolled;
        }

        // The window stays. New measurements changed the estimate: keep the spacers in step.
        if (measured) SyncSpacers(host);
        return scrolled;
    }

    /// <summary>
    /// Decides the window for what <paramref name="observation"/> shows; returns whether it changed (the
    /// caller re-renders). Also records the scroll anchor and, with an <see cref="ItemsProvider"/>,
    /// requests the items the window needs.
    /// </summary>
    private bool UpdateWindow(in ViewportObservation observation)
    {
        if (!observation.IsVisible || _slotStarts.Count == 0)
        {
            ClearAnchor();
            return false;
        }

        // Layout coordinates of item i's start: baseY + Prefix(i). Exact for the rendered items (they
        // were just measured), the current estimate elsewhere.
        double baseY = _slotStarts[0] - _sizes.Prefix(_windowStart);
        double top = observation.VisibleRect.Top - baseY;
        double bottom = observation.VisibleRect.Bottom - baseY;

        if (ItemsProvider is not null && !_countKnown)
        {
            // Nothing is known yet, not even how many items there are: ask for a screenful.
            ClearAnchor();
            if (bottom <= 0 || _pendingRange is not null) return false;
            int capacity = (int)Math.Ceiling(Math.Min(bottom, observation.VisibleRect.Height) / Math.Max(1f, _sizes.Estimate));
            capacity = Math.Clamp(capacity + OverscanCount, 1, MaxItemCount);
            return Request(0, capacity) is null && _countKnown;
        }

        int count = ItemCount;
        if (count == 0 || bottom <= 0 || top >= _sizes.Total)
        {
            // Empty, or scrolled entirely past: whatever is rendered stays.
            ClearAnchor();
            return false;
        }

        int first = Math.Min(_sizes.CountWithin(Math.Max(0, top)), count - 1);
        int lastExclusive = Math.Clamp(_sizes.CountWithin(bottom) + 1, first + 1, count);

        // The visible items themselves can outnumber MaxItemCount (tiny measured heights); the window
        // then follows the top of the screen.
        lastExclusive = Math.Min(lastExclusive, first + MaxItemCount);

        RecordAnchor(first, top, baseY, observation);

        bool covered = _windowStart <= first && _windowEnd >= lastExclusive;
        // A window much larger than needed (the viewport shrank, items turned out taller) is narrowed;
        // the slack keeps a visible count that flickers by one row from re-rendering on every frame.
        bool oversized = _windowEnd - _windowStart > (lastExclusive - first) + 2 * OverscanCount + 2;
        if (covered && !oversized)
        {
            // ItemsProvider: the window is in place but its items may have failed to load or been
            // cancelled — ask again (the placeholders stay until they arrive).
            if (ItemsProvider is not null && _pendingRange is null && !WindowLoaded())
                Request(_windowStart, _windowEnd - _windowStart);
            return false;
        }

        int start = Math.Max(0, first - OverscanCount);
        int end = Math.Min(count, lastExclusive + OverscanCount);
        int excess = end - start - MaxItemCount;
        if (excess > 0)
        {
            int trimBefore = Math.Min(first - start, (excess + 1) / 2);
            start += trimBefore;
            end -= Math.Min(end - lastExclusive, excess - trimBefore);
            if (end - start > MaxItemCount) end = start + MaxItemCount;
        }

        _windowStart = start;
        _windowEnd = end;
        if (ItemsProvider is not null && !WindowLoaded())
            Request(start, end - start);
        return true;
    }

    private bool WindowLoaded()
        => _windowEnd <= _windowStart || (IsLoaded(_windowStart) && IsLoaded(Math.Min(_windowEnd, ItemCount) - 1));

    /// <summary>
    /// Records where each slot of the layout the last render produced starts — slot 0 where the leading
    /// spacer ends, every later slot at the margin-box top of its first laid-out element — and where the
    /// last one ends (the trailing spacer's margin-box top). Each measured item's height is the distance
    /// to the next start, so margins and gaps between items count towards the item above them and the
    /// heights add up exactly to the span between the spacers. Placeholders are not measured: their
    /// height says nothing about the item's.
    /// </summary>
    private void Measure(VirtualizeHostElement host)
    {
        _slotStarts.Clear();
        var children = host.Children;
        if (children.Count < 2
            || children[0] is not { Key: VirtualizeSpacerKey { After: false } } before
            || children[^1] is not { Key: VirtualizeSpacerKey { After: true } } after
            || !IsLaidOut(before) || !IsLaidOut(after))
            return;

        int n = _rendered.Count;
        _slotStarts.Add(before.LayoutBox!.BoxModel.MarginBox.Bottom);
        for (int i = 1; i < n; i++)
            _slotStarts.Add(SlotTop(_rendered[i].CurrentRoot) ?? float.NaN);
        if (n > 0) _slotStarts.Add(after.LayoutBox!.BoxModel.MarginBox.Top);

        // A slot that laid out nothing (a template rendering nothing for this item, or only absolutely
        // positioned content) starts where the next one does: zero height.
        for (int i = n - 1; i >= 1; i--)
        {
            if (float.IsNaN(_slotStarts[i])) _slotStarts[i] = _slotStarts[i + 1];
        }

        for (int i = 0; i < n; i++)
        {
            var key = (VirtualizeSlotKey)_rendered[i].SlotKey!;
            if (key.Placeholder) continue;

            float size = Math.Max(0f, _slotStarts[i + 1] - _slotStarts[i]);
            _sizes.Set(_windowStart + i, size);
            if (_sizeByKey is not null && ItemsProvider is null) _sizeByKey[key.Key] = size;
        }
    }

    /// <summary>
    /// Notes the first item on screen and where it starts. That is the first <b>rendered</b> item that
    /// reaches into the visible rect, measured — what the user is looking at. Only when no rendered item
    /// is on screen (a fast scroll landed in a spacer) is it the item the size index puts at the top,
    /// where that spacer says it is: the render that fills the spacer must not then move the rows by
    /// however much their real heights differ from the estimate.
    /// </summary>
    private void RecordAnchor(int first, double top, double baseY, in ViewportObservation observation)
    {
        ClearAnchor();
        // The top of the screen is above the list (a header is showing): nothing of the list to keep
        // still — content above the list moving is not the list's doing.
        if (top < 0) return;

        float visibleTop = observation.VisibleRect.Top;
        float visibleBottom = observation.VisibleRect.Bottom;
        if (_slotStarts.Count == _rendered.Count + 1)
        {
            for (int slot = 0; slot < _rendered.Count; slot++)
            {
                if (_slotStarts[slot + 1] <= visibleTop) continue;
                if (_slotStarts[slot] >= visibleBottom) break;

                _anchorIndex = _windowStart + slot;
                _anchorKey = ((VirtualizeSlotKey)_rendered[slot].SlotKey!).Key;
                _anchorY = _slotStarts[slot];
                _anchorScroller = observation.ScrollContainer;
                return;
            }
        }

        _anchorIndex = first;
        _anchorKey = KeyAt(first);
        _anchorY = (float)(baseY + _sizes.Prefix(first));
        _anchorScroller = observation.ScrollContainer;
    }

    /// <summary>The slot key item <paramref name="index"/> is rendered with (its ItemKey, or its index).</summary>
    private object KeyAt(int index)
        => ItemKey is not null && TryGetItem(index, out var item) ? KeyOf(item, index) : index;

    private void ClearAnchor()
    {
        _anchorIndex = -1;
        _anchorScroller = null;
    }

    /// <summary>
    /// After a layout, scrolls by however far the anchor item moved (ISSUE-151 scroll anchoring): items
    /// above it measured taller or shorter than estimated, or items were inserted or removed above it
    /// (then <see cref="Rekey"/> moved the window and the anchor's index along). The visible rect is
    /// observed again so the window is computed for the corrected position.
    /// </summary>
    private bool CompensateAnchor(VirtualizeHostElement host, ref ViewportObservation observation)
    {
        int index = _anchorIndex;
        var previous = _anchorScroller;
        ClearAnchor();
        if (index < 0) return false;

        int slot = index - _windowStart;
        if (slot < 0 || slot >= _rendered.Count || _slotStarts.Count != _rendered.Count + 1) return false;
        // With ItemKey the anchor is an item (Rekey keeps its index current); the item now at that index
        // must still be it. Otherwise the anchor is the index itself.
        if (ItemKey is not null && ItemsProvider is null
            && !Equals(((VirtualizeSlotKey)_rendered[slot].SlotKey!).Key, _anchorKey)) return false;

        float shift = _slotStarts[slot] - _anchorY;
        if (observation.ScrollContainer is not { } scroller) return false;

        // Start from the offset the scroller had before this layout — the box of the previous layout
        // still holds it, including any scrolling since the anchor was recorded — when the engine clamped
        // it carrying it over: a spacer that shrank by more than the distance to the bottom would
        // otherwise lose the difference. An offset the engine reset (content judged replaced) stays reset.
        float from = previous is not null && ReferenceEquals(previous.Element, scroller.Element)
                     && scroller.ScrollTop > 0f && scroller.ScrollTop < previous.ScrollTop
            ? previous.ScrollTop
            : scroller.ScrollTop;

        float max = Math.Max(0f, scroller.ScrollableContentHeight - scroller.BoxModel.PaddingBox.Height
            + scroller.HorizontalScrollbarGutter);
        float target = Math.Clamp(from + shift, 0f, max);
        if (Math.Abs(target - scroller.ScrollTop) < 0.01f) return false;
        scroller.ScrollTop = target;

        observation = ViewportObservers.Observe(host, observation.LayoutRoot, observation.Viewport);
        return true;
    }

    private static bool IsLaidOut(Element element)
        => element.LayoutBox?.ComputedStyle is { } style && style.Display != Display.None;

    /// <summary>
    /// Where a slot's content starts: the margin-box top of the first element it laid out, looking
    /// through layout-transparent fragments. Out-of-flow content does not occupy the slot.
    /// </summary>
    private static float? SlotTop(Element? element)
    {
        if (element is null) return null;
        if (element is Core.DomElements.FragmentElement)
        {
            foreach (var child in element.Children)
            {
                if (SlotTop(child) is { } top) return top;
            }
            return null;
        }

        if (element.LayoutBox is not { ComputedStyle: { } style } box) return null;
        if (style.Display == Display.None) return null;
        if (style.Position is Position.Absolute or Position.Fixed) return null;
        return box.BoxModel.MarginBox.Top;
    }
}
