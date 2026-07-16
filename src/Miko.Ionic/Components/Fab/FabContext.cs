using Miko.Components;

namespace Miko.Ionic.Components;

/// <summary>
/// Activation state cascaded from an <see cref="IonFab"/> down to its descendant
/// <see cref="IonFabButton"/> and <see cref="IonFabList"/> components. Mirrors how Ionic's
/// <c>ion-fab</c> owns the <c>activated</c> flag and, on toggle, turns its main button into a
/// close icon and reveals its fab-lists.
/// <para>
/// The fab and its descendants share a subtree, so this travels via a
/// <see cref="CascadingValue{TValue}"/> (matched by type), exactly like
/// <see cref="IonSegmentContext"/>. Ionic wires the three components with runtime DOM queries and
/// <c>@Watch</c>; Miko has neither, so each descendant instead derives its own marker classes from
/// this context.
/// </para>
/// <para>
/// An <see cref="IonFabList"/> re-cascades this context to its own children with
/// <see cref="InList"/> set, so a button knows it is a list button (and, via
/// <see cref="Activated"/>, whether the enclosing fab is open and it should show).
/// </para>
/// </summary>
public sealed class FabContext
{
    /// <summary>True when the enclosing fab is activated (open): the main button shows the close
    /// icon and fab-lists are visible.</summary>
    public bool Activated { get; init; }

    /// <summary>True when this context has been re-cascaded by an <see cref="IonFabList"/>, marking
    /// the buttons that read it as list buttons (<c>fab-button-in-list</c>).</summary>
    public bool InList { get; init; }

    /// <summary>Invoked by the main <see cref="IonFabButton"/> on click to ask the fab to toggle its
    /// activated state (a no-op when the fab has no list, mirroring <c>fab.tsx</c> <c>toggle()</c>).</summary>
    public EventCallback Toggle { get; init; }
}
