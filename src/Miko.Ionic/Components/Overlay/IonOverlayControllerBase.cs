using Miko.Components;

namespace Miko.Ionic.Components;

public abstract class IonOverlayControllerBase
{
    private readonly IonOverlayRegistry _registry;
    private readonly object _sync = new();
    private readonly Dictionary<string, ControllerOverlay> _overlays = new();

    protected IonOverlayControllerBase(IonOverlayRegistry registry)
    {
        _registry = registry;
    }

    protected IonOverlayReference CreateReference(
        string prefix,
        Func<ControllerOverlay, RenderFragment> render)
    {
        var id = $"{prefix}-{Guid.NewGuid():N}";
        var overlay = new ControllerOverlay(id);
        overlay.Render = render(overlay);

        lock (_sync) _overlays[id] = overlay;

        return new IonOverlayReference(
            id,
            () => PresentAsync(overlay),
            (data, role) => DismissAsync(overlay, data, role),
            overlay.Result.Task);
    }

    public Task<IonOverlayReference?> GetTopAsync()
    {
        lock (_sync)
        {
            var overlay = _registry.Snapshot()
                .Reverse()
                .Select(entry => _overlays.GetValueOrDefault(entry.Id))
                .FirstOrDefault(candidate => candidate is not null);
            if (overlay is null)
                return Task.FromResult<IonOverlayReference?>(null);

            return Task.FromResult<IonOverlayReference?>(new IonOverlayReference(
                overlay.Id,
                () => PresentAsync(overlay),
                (data, role) => DismissAsync(overlay, data, role),
                overlay.Result.Task));
        }
    }

    private Task PresentAsync(ControllerOverlay overlay)
    {
        lock (_sync)
        {
            if (overlay.Dismissed || overlay.Presented) return Task.CompletedTask;
            overlay.Presented = true;
            overlay.IsOpen = true;
        }

        _registry.Register(overlay.Id, overlay.Render, controllerOwned: true);
        return Task.CompletedTask;
    }

    private async Task<bool> DismissAsync(ControllerOverlay overlay, object? data, string? role)
    {
        lock (_sync)
        {
            if (overlay.Dismissed || !overlay.Presented || overlay.Dismissing)
                return false;

            overlay.Dismissing = true;
            overlay.IsOpen = false;
            overlay.PendingData = data;
            overlay.PendingRole = role;
        }

        // Re-registering keeps the entry in the same stack position and renders its closed state.
        _registry.Register(overlay.Id, overlay.Render, controllerOwned: true);
        await CompleteDismissAsync(overlay, new IonOverlayDismissEventArgs(role, data));
        return true;
    }

    protected async Task CompleteDismissAsync(
        ControllerOverlay overlay,
        IonOverlayDismissEventArgs componentResult)
    {
        IonOverlayDismissEventArgs result;
        lock (_sync)
        {
            if (overlay.Dismissed) return;
            overlay.Dismissed = true;
            overlay.Dismissing = false;
            overlay.Presented = false;

            result = new IonOverlayDismissEventArgs(
                componentResult.Role ?? overlay.PendingRole,
                componentResult.Data ?? overlay.PendingData);
        }

        _registry.Remove(overlay.Id);
        overlay.Result.TrySetResult(result);
        lock (_sync) _overlays.Remove(overlay.Id);
        await Task.CompletedTask;
    }

    protected sealed class ControllerOverlay
    {
        internal ControllerOverlay(string id) => Id = id;

        internal string Id { get; }
        internal bool IsOpen { get; set; }
        internal bool Presented { get; set; }
        internal bool Dismissing { get; set; }
        internal bool Dismissed { get; set; }
        internal object? PendingData { get; set; }
        internal string? PendingRole { get; set; }
        internal RenderFragment Render { get; set; } = _ => { };
        internal TaskCompletionSource<IonOverlayDismissEventArgs> Result { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
