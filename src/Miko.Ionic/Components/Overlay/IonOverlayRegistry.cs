using Miko.Components;

namespace Miko.Ionic.Components;

/// <summary>Application-scoped registry rendered by the single <see cref="IonOverlayHost"/>.</summary>
public sealed class IonOverlayRegistry
{
    private readonly object _sync = new();
    private readonly List<IonOverlayEntry> _entries = new();
    private long _nextVersion;

    internal event Action? Changed;

    internal IReadOnlyList<IonOverlayEntry> Snapshot()
    {
        lock (_sync)
            return _entries.ToArray();
    }

    internal long Register(string id, RenderFragment content, bool controllerOwned = false)
    {
        long version;
        lock (_sync)
        {
            version = ++_nextVersion;
            var index = _entries.FindIndex(entry => entry.Id == id);
            var next = new IonOverlayEntry(id, version, content, controllerOwned);
            if (index >= 0)
                _entries[index] = next;
            else
                _entries.Add(next);
        }
        Changed?.Invoke();
        return version;
    }

    internal bool Remove(string id, long? version = null)
    {
        bool removed;
        lock (_sync)
        {
            var index = _entries.FindIndex(entry =>
                entry.Id == id && (version is null || entry.Version == version));
            removed = index >= 0;
            if (removed) _entries.RemoveAt(index);
        }
        if (removed) Changed?.Invoke();
        return removed;
    }

    internal IonOverlayEntry? GetTop()
    {
        lock (_sync)
            return _entries.Count == 0 ? null : _entries[^1];
    }
}
