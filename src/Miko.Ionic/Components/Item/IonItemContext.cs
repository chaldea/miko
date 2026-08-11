namespace Miko.Ionic.Components;

/// <summary>
/// Presence marker cascaded from an <see cref="IonItem"/> (or <see cref="IonItemDivider"/>) down to
/// its child form controls. Mirrors how Ionic's form controls (input, select, checkbox, …) use
/// <c>hostContext('ion-item', this.el)</c> to detect whether they are hosted inside an item and
/// adjust their rendering accordingly — e.g., <see cref="IonInput"/> suppresses the
/// <c>.input-highlight</c> bar when inside an item because the item already has its own bottom
/// border.
/// <para>
/// The context is matched by type via <see cref="CascadingValue{TValue}"/>, exactly like
/// <see cref="IonAccordionGroupContext"/> and <see cref="FabContext"/>. An item provides this
/// marker; form controls receive it (nullable) and gate in-item-specific behavior on
/// <c>ItemContext is not null</c>.
/// </para>
/// </summary>
public sealed class IonItemContext
{
    /// <summary>Singleton marker instance. Items provide this; no per-item state is needed.</summary>
    public static readonly IonItemContext Instance = new();

    private IonItemContext() { }
}
