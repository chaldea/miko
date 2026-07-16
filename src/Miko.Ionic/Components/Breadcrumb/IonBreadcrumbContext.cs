using Miko.Components;

namespace Miko.Ionic.Components;

/// <summary>
/// Per-breadcrumb state cascaded from an <see cref="IonBreadcrumbs"/> down to its
/// <see cref="IonBreadcrumb"/> children. Mirrors how Ionic's <c>ion-breadcrumbs</c> walks its
/// slotted <c>ion-breadcrumb</c>s in <c>setBreadcrumbSeparator()</c> and stamps <c>last</c> /
/// <c>separator</c> / <c>active</c> onto each one.
/// <para>
/// Ionic does this imperatively via DOM queries after render; Miko has no live DOM to query, so
/// the container instead exposes a callback each breadcrumb calls once to register itself and read
/// back its resolved flags. The container assigns an incrementing index and, because the total
/// count is only known after every child has registered, defers the <c>last</c>/<c>active</c>
/// decision to <see cref="IonBreadcrumbs.Build"/> which post-processes the built subtree.
/// </para>
/// </summary>
public sealed class IonBreadcrumbContext
{
    /// <summary>The container's <c>color</c> (Ionic <c>ion-breadcrumbs[color]</c>), so a child can
    /// stamp the <c>in-breadcrumbs-color</c> marker that recolors it to the palette base.</summary>
    public string? Color { get; init; }
}
