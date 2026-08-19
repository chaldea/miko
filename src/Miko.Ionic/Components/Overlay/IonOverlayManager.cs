namespace Miko.Ionic.Components;

/// <summary>Read-only application view of the unified Ionic overlay stack.</summary>
public sealed class IonOverlayManager
{
    private readonly IonOverlayRegistry _registry;

    public IonOverlayManager(IonOverlayRegistry registry)
    {
        _registry = registry;
    }

    public int Count => _registry.Snapshot().Count;

    public Task<string?> GetTopIdAsync()
        => Task.FromResult(_registry.GetTop()?.Id);
}
