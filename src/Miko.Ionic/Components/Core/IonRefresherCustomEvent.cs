namespace Miko.Ionic.Components;

/// <summary>
/// Payload for <see cref="IonRefresher.OnRefresh"/>. Mirrors Ionic's <c>RefresherCustomEvent</c>,
/// where the handler ends the refresh with <c>event.detail.complete()</c> (or
/// <c>event.target.complete()</c>) once its async work is done.
/// <para>
/// <see cref="Complete"/> is forwarded to <see cref="Target"/> so handlers do not need an
/// element reference (<c>@ref</c>) on the component:
/// <code>
/// OnRefresh="@(async e => { await Reload(); await e.Complete(); })"
/// </code>
/// </para>
/// </summary>
public sealed class IonRefresherCustomEvent
{
    /// <summary>The refresher that raised the event (Ionic's <c>event.target</c>).</summary>
    public IonRefresher Target { get; }

    public IonRefresherCustomEvent(IonRefresher target)
    {
        Target = target;
    }

    /// <summary>
    /// Signals that the refresh has finished: the refresher shows its completing state and the
    /// content slides back to the top. Shorthand for <c>Target.Complete()</c>.
    /// </summary>
    public Task Complete() => Target.Complete();
}
