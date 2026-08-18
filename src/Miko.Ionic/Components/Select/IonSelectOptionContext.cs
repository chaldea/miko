namespace Miko.Ionic.Components;

/// <summary>
/// Registration channel cascaded from an <see cref="IonSelect"/> down to its
/// <see cref="IonSelectOption"/> children.
/// <para>
/// Ionic reads its options imperatively — <c>this.el.querySelectorAll('ion-select-option')</c>
/// (<c>select.tsx</c> <c>childOpts</c>) — and re-reads them through a <c>MutationObserver</c>
/// whenever the option elements change. Miko has neither a live DOM registry nor mutation
/// observers, so the flow is inverted: the select cascades this context, and each option registers
/// itself while the select's child content is being built. The select then owns the full option set
/// and uses it to build the overlay contents and the displayed text.
/// </para>
/// <para>
/// This travels via a <see cref="Miko.Components.CascadingValue{TValue}"/> (matched by type), like
/// <see cref="IonPickerColumnContext"/> and <see cref="IonSegmentContext"/>. Unlike those, the
/// callback flows child → parent as a registration rather than a request to change state, because
/// the parent needs the <em>whole</em> child set before it can render its overlay.
/// </para>
/// </summary>
public sealed class IonSelectOptionContext
{
    /// <summary>Invoked by each option as it builds, handing its data to the owning select.</summary>
    public Action<IonSelectOptionData>? Register { get; init; }
}
