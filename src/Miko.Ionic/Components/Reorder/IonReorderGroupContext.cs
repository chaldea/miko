namespace Miko.Ionic.Components;

/// <summary>
/// Behavior state cascaded from an <see cref="IonReorderGroup"/> down to the
/// <see cref="IonReorder"/> drag-handles nested inside its list items. Mirrors how Ionic's
/// <c>ion-reorder-group</c> owns the <c>disabled</c> flag and, via the <c>reorder-enabled</c> host
/// class, decides whether the reorder handles are shown and interactive.
/// <para>
/// In Ionic the group toggles a <c>reorder-enabled</c> class on its host and the descendant
/// <c>ion-reorder</c> elements are revealed by a <c>.reorder-enabled ion-reorder { display: block }</c>
/// rule (they are <c>display:none</c> otherwise). Miko has no ancestor-class descendant selector that
/// a child can react to at build time, so instead each <see cref="IonReorder"/> reads this context
/// (matched by type through a <see cref="Microsoft.AspNetCore.Components.CascadingValue{TValue}"/>)
/// and stamps its own enabled/disabled class.
/// </para>
/// <para>
/// This is a structural / visual port: the live drag-to-reorder gesture is NOT implemented (Miko has
/// no gesture engine here). The context models only the disabled cascade.
/// </para>
/// </summary>
public sealed class IonReorderGroupContext
{
    /// <summary>Whether the group is disabled. When true the reorder handles are hidden and
    /// non-interactive; when false they are shown (Ionic's <c>reorder-enabled</c> state).</summary>
    public bool Disabled { get; init; } = true;
}
