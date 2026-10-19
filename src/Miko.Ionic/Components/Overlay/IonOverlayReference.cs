namespace Miko.Ionic.Components;

/// <summary>
/// A handle to one overlay created by a controller's <c>CreateAsync</c> (the element Ionic's
/// <c>create()</c> resolves to). It is single-use: once dismissed, it cannot be presented again.
/// </summary>
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

    /// <summary>The overlay's id in the overlay layer (what <c>IonOverlayManager.GetTopIdAsync</c>
    /// returns).</summary>
    public string Id { get; }

    /// <summary>Shows the overlay in the overlay layer of <see cref="IonApp"/>. Does nothing when it
    /// is already presented or was dismissed. Nothing appears when the app has no
    /// <see cref="IonApp"/>.</summary>
    public Task PresentAsync() => _present();

    /// <summary>Closes the overlay and completes <see cref="OnDidDismissAsync"/> with
    /// <paramref name="role"/> and <paramref name="data"/>. Returns false when the overlay is not
    /// presented or is already closing or dismissed.</summary>
    public Task<bool> DismissAsync(object? data = null, string? role = null)
        => _dismiss(data, role);

    /// <summary>Completes once the overlay is dismissed, by the user or by
    /// <see cref="DismissAsync"/>, with the role and data it was dismissed with.</summary>
    public Task<IonOverlayDismissEventArgs> OnDidDismissAsync() => _result;
}
