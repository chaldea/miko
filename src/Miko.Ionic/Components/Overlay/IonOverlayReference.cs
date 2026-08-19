namespace Miko.Ionic.Components;

/// <summary>A handle returned by an Ionic overlay controller.</summary>
public sealed class IonOverlayReference
{
    private readonly Func<Task> _present;
    private readonly Func<object?, string?, Task<bool>> _dismiss;
    private readonly Task<IonOverlayDismissEventArgs> _result;

    internal IonOverlayReference(
        string id,
        Func<Task> present,
        Func<object?, string?, Task<bool>> dismiss,
        Task<IonOverlayDismissEventArgs> result)
    {
        Id = id;
        _present = present;
        _dismiss = dismiss;
        _result = result;
    }

    public string Id { get; }

    public Task PresentAsync() => _present();

    public Task<bool> DismissAsync(object? data = null, string? role = null)
        => _dismiss(data, role);

    public Task<IonOverlayDismissEventArgs> OnDidDismissAsync() => _result;
}
