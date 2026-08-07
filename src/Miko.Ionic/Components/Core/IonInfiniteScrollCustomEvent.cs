namespace Miko.Ionic.Components;

/// <summary>
/// Payload for <see cref="IonInfiniteScroll.OnInfinite"/>. Mirrors Ionic's
/// <c>IonInfiniteScrollCustomEvent</c>, where the handler reaches the emitting component through
/// <c>event.target</c> and signals the end of the async work with <c>event.target.complete()</c>.
/// <para>
/// <see cref="Complete"/> is forwarded to <see cref="Target"/> so handlers do not need an
/// element reference (<c>@ref</c>) on the component:
/// <code>
/// OnInfinite="@(async e => { await LoadMore(); await e.Complete(); })"
/// </code>
/// </para>
/// </summary>
public sealed class IonInfiniteScrollCustomEvent
{
    /// <summary>The infinite scroll that raised the event (Ionic's <c>event.target</c>).</summary>
    public IonInfiniteScroll Target { get; }

    public IonInfiniteScrollCustomEvent(IonInfiniteScroll target)
    {
        Target = target;
    }

    /// <summary>
    /// Signals that the async work started by this event has finished, clearing the loading state
    /// and re-arming the infinite scroll for the next threshold crossing. Shorthand for
    /// <c>Target.Complete()</c>.
    /// </summary>
    public Task Complete() => Target.Complete();
}
