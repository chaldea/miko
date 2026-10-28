namespace Miko.Components.Virtualization;

/// <summary>
/// Supplies the items of a <see cref="Virtualize{TItem}"/> on demand: called with the range the list
/// is about to show, it returns those items and the total number of items. Same shape as Blazor's
/// <c>ItemsProviderDelegate&lt;TItem&gt;</c>.
/// </summary>
public delegate ValueTask<ItemsProviderResult<TItem>> ItemsProviderDelegate<TItem>(ItemsProviderRequest request);

/// <summary>The range of items a <see cref="Virtualize{TItem}"/> asks its <see cref="Virtualize{TItem}.ItemsProvider"/> for.</summary>
public readonly struct ItemsProviderRequest
{
    public ItemsProviderRequest(int startIndex, int count, CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        Count = count;
        CancellationToken = cancellationToken;
    }

    /// <summary>The index of the first item wanted.</summary>
    public int StartIndex { get; }

    /// <summary>
    /// How many items are wanted. Before the first result the list does not know the total, so this
    /// can run past the end; return what exists.
    /// </summary>
    public int Count { get; }

    /// <summary>Cancelled when the list scrolled on and asked for another range before this one arrived.</summary>
    public CancellationToken CancellationToken { get; }
}

/// <summary>The items an <see cref="ItemsProviderDelegate{TItem}"/> returns, and how many items there are in total.</summary>
public readonly struct ItemsProviderResult<TItem>
{
    public ItemsProviderResult(IEnumerable<TItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }

    /// <summary>The requested items, starting at <see cref="ItemsProviderRequest.StartIndex"/>.</summary>
    public IEnumerable<TItem> Items { get; }

    /// <summary>The number of items in the whole list.</summary>
    public int TotalItemCount { get; }
}

/// <summary>What a <see cref="Virtualize{TItem}.Placeholder"/> template is rendered for: an item that is still loading.</summary>
public readonly struct PlaceholderContext
{
    public PlaceholderContext(int index, float size = 0f)
    {
        Index = index;
        Size = size;
    }

    /// <summary>The index of the item being loaded.</summary>
    public int Index { get; }

    /// <summary>The size, in pixels, the list expects the item to take (its current estimate).</summary>
    public float Size { get; }
}
