using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;

namespace Miko.Components.Virtualization;

/// <summary>
/// One rendered item (or placeholder) of a <see cref="Virtualize{TItem}"/> (ISSUE-151).
///
/// <para>Each item is rendered through its own slot, keyed by the item, so that when the window moves
/// the items that stay in it keep their elements and component instances: <see cref="Build"/> hands
/// back the element it produced last time instead of running the item template again. Only the items
/// scrolling in are built — on a range change, that is a few rows rather than the whole window, and an
/// item's runtime state (an image already decoded, a running transition, focus) survives scrolling.
/// The list's <see cref="Generation"/> changes on every render of the list's parent and on
/// <c>RefreshDataAsync</c>, and the template then runs again for every item: the data may have
/// changed.</para>
///
/// <para>The element is handed back as it is now, not as it was built: a component inside the item
/// that re-rendered itself since (a toggled checkbox) replaced its own element, and
/// <see cref="ComponentBase.CurrentRoot"/> follows it.</para>
/// </summary>
internal sealed class VirtualizeSlot : ComponentBase
{
    /// <summary>The item's content, or the placeholder's.</summary>
    public RenderFragment? Content { get; set; }

    /// <summary>The list's render generation; the slot rebuilds when it differs from the one it was built in.</summary>
    public int Generation { get; set; }

    /// <summary>The key the slot was opened with, written onto its element (see <see cref="Build"/>).</summary>
    public object? SlotKey { get; set; }

    private int _builtGeneration = -1;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Content is not null) builder.AddContent(0, Content);
    }

    public override Element Build()
    {
        Element root;
        if (_builtGeneration == Generation && CurrentRoot is { } retained)
        {
            root = retained;
        }
        else
        {
            root = base.Build();
            _builtGeneration = Generation;
        }

        // The list's host pairs its children by key when it re-renders, so the layout box and the
        // runtime state of an item that left the window are never carried over to another item.
        // A key the template put on the element itself is left alone.
        root.Key ??= SlotKey;
        return root;
    }
}

/// <summary>
/// The element a <see cref="Virtualize{TItem}"/> renders: a layout-transparent fragment (the items
/// lay out as children of the element around the list, as in Blazor, so <c>IonList</c> styles them)
/// that observes where it can be seen.
/// </summary>
internal sealed class VirtualizeHostElement : FragmentElement, IViewportObserver
{
    internal delegate bool ViewportHandler(VirtualizeHostElement host, in ViewportObservation observation);

    internal ViewportHandler? Handler;

    bool IViewportObserver.OnViewportChanged(in ViewportObservation observation)
        => Handler is { } handler && handler(this, observation);
}

/// <summary>Keys of the elements a <see cref="Virtualize{TItem}"/> renders, kept apart from any key a template uses.</summary>
internal readonly record struct VirtualizeSpacerKey(bool After);

/// <summary>The key of one item slot: the item's <c>ItemKey</c> (or index), or the index of a placeholder.</summary>
internal readonly record struct VirtualizeSlotKey(object Key, bool Placeholder);
