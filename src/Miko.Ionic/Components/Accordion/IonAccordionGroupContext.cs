using Miko.Components;

namespace Miko.Ionic.Components;

/// <summary>
/// Selection/behavior state cascaded from an <see cref="IonAccordionGroup"/> down to its
/// <see cref="IonAccordion"/> children. Mirrors how Ionic's <c>ion-accordion-group</c> owns the
/// expanded <c>value</c> (a string, or an array when <c>multiple</c>) and, via
/// <c>requestAccordionToggle</c>, decides whether an accordion may expand.
/// <para>
/// The group and its accordions share a subtree, so this travels via a
/// <see cref="CascadingValue{TValue}"/> (matched by type), exactly like
/// <see cref="IonSegmentContext"/> and <see cref="FabContext"/>. Ionic wires them with runtime DOM
/// events (<c>ionValueChange</c>) and imperative methods; Miko has neither, so each accordion reads
/// this context to derive its own expanded/disabled state and calls <see cref="RequestToggle"/> to
/// ask the group to change the value.
/// </para>
/// </summary>
public sealed class IonAccordionGroupContext
{
    /// <summary>The set of currently expanded accordion values.</summary>
    public IReadOnlyList<string> Values { get; init; } = Array.Empty<string>();

    /// <summary>Whether more than one accordion may be expanded at once.</summary>
    public bool Multiple { get; init; }

    /// <summary>Whether the whole group is disabled (dims and blocks interaction).</summary>
    public bool Disabled { get; init; }

    /// <summary>Whether the whole group is read-only (blocks interaction without dimming).</summary>
    public bool Readonly { get; init; }

    /// <summary>Whether accordions animate on expand/collapse (marker only in this base port).</summary>
    public bool Animated { get; init; }

    /// <summary>Expansion behavior: <c>"compact"</c> (default) or <c>"inset"</c>.</summary>
    public string Expand { get; init; } = "compact";

    /// <summary>The toggle icon slot inside the header item: <c>"start"</c> or <c>"end"</c>.</summary>
    public string ToggleIconSlot { get; init; } = "end";

    /// <summary>Invoked by an accordion (its own value + whether it wants to expand) to ask the
    /// group to update the expanded set. Mirrors Ionic's <c>requestAccordionToggle</c>.</summary>
    public EventCallback<(string Value, bool Expand)> RequestToggle { get; init; }

    /// <summary>True when <paramref name="value"/> is in the expanded set.</summary>
    public bool IsExpanded(string value) => Values.Contains(value);
}
