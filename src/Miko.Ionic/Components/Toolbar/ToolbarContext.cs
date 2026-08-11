namespace Miko.Ionic.Components;

/// <summary>
/// Toolbar context cascaded from <see cref="IonToolbar"/> down to nested <see cref="IonButton"/>s.
/// Mirrors how button.tsx resolves three ancestor-dependent settings via <c>hostContext</c> and
/// <c>closest('ion-buttons')</c> at render time (which Miko cannot do inline, because components
/// build subtrees detached). A button that receives this context applies the
/// <c>in-toolbar</c> / <c>in-buttons</c> styling and the clear-fill default automatically.
/// <para>
/// The context is matched by type via <see cref="CascadingValue{TValue}"/> — the same pattern as
/// <see cref="IonItemContext"/>, <see cref="IonAccordionGroupContext"/>, and <see cref="FabContext"/>.
/// </para>
/// </summary>
public sealed class ToolbarContext
{
    /// <summary>
    /// Whether the button is nested inside an <see cref="IonButtons"/> group (matches
    /// <c>closest('ion-buttons')</c>). A true value adds the <c>in-buttons</c> class, which applies
    /// the denser toolbar-button metrics ported from <c>buttons.{md,ios}.scss</c>'s
    /// <c>::slotted(*) ion-button</c> block, and flips the button's default fill to clear.
    /// </summary>
    public bool InButtons { get; init; }
}
