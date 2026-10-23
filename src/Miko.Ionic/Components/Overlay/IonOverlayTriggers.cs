using Miko.Events;

namespace Miko.Ionic.Components;

/// <summary>
/// The declarative overlays that open when an element with a given id is clicked — Ionic's
/// <c>trigger</c> property (overlays.ts <c>createTriggerController</c>). Owned by
/// <see cref="IonApp"/> through <see cref="IonOverlayContext"/>.
/// <para>
/// Ionic finds the trigger with <c>document.getElementById</c> and adds a listener to it. A Miko
/// component has no host element to listen on, and the trigger may be rebuilt by any re-render, so
/// the listener sits on <see cref="IonApp"/>'s root instead: every click inside the app bubbles
/// there, and the overlays whose trigger id is on the clicked element or one of its ancestors open.
/// </para>
/// </summary>
internal sealed class IonOverlayTriggers
{
    private readonly List<Binding> _bindings = new();

    private sealed record Binding(object Owner, string ElementId, bool OnLongPress, Func<MouseEventArgs, Task> Open);

    /// <summary>
    /// Makes <paramref name="owner"/> open through <paramref name="open"/> when the element with id
    /// <paramref name="elementId"/> is clicked (or long-pressed, with <paramref name="onLongPress"/>).
    /// Calling it again replaces the owner's binding; a null or empty id removes it. Overlays call it
    /// from <c>OnParametersSet</c>, so it must be idempotent.
    /// </summary>
    public void Set(object owner, string? elementId, Func<MouseEventArgs, Task> open, bool onLongPress = false)
    {
        Remove(owner);
        if (!string.IsNullOrEmpty(elementId))
            _bindings.Add(new Binding(owner, elementId, onLongPress, open));
    }

    /// <summary>Drops <paramref name="owner"/>'s binding (an overlay that is disposed or lost its trigger).</summary>
    public void Remove(object owner)
    {
        for (var i = _bindings.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_bindings[i].Owner, owner))
                _bindings.RemoveAt(i);
        }
    }

    /// <summary>Number of bound overlays (for tests).</summary>
    internal int Count => _bindings.Count;

    /// <summary>
    /// Opens the overlays triggered by the element <paramref name="args"/> hit: the target itself or
    /// any ancestor carrying a bound id. Called by <see cref="IonApp"/> for clicks and long presses.
    /// </summary>
    public void Dispatch(MouseEventArgs args, bool longPress)
    {
        if (_bindings.Count == 0) return;

        List<Binding>? matched = null;
        for (var node = args.Target; node is not null; node = node.Parent)
        {
            if (string.IsNullOrEmpty(node.Id)) continue;
            foreach (var binding in _bindings)
            {
                if (binding.OnLongPress == longPress && binding.ElementId == node.Id
                    && (matched is null || !matched.Contains(binding)))
                {
                    (matched ??= new()).Add(binding);
                }
            }
        }
        if (matched is null) return;

        // Snapshot taken: opening an overlay re-renders it, which rebinds (Set) while we iterate.
        foreach (var binding in matched)
            _ = binding.Open(args);
    }
}
