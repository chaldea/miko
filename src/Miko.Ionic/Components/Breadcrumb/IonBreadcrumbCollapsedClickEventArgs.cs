using Miko.Core;

namespace Miko.Ionic.Components;

/// <summary>
/// Payload for <see cref="IonBreadcrumbs.OnCollapsedClick"/>, raised when the collapsed
/// indicator (the "…" button shown when <see cref="IonBreadcrumbs.MaxItems"/> hides the middle
/// crumbs) is clicked. Mirrors Ionic's <c>ionCollapsedClick</c> event detail
/// (<c>BreadcrumbCollapsedClickEventDetail</c>), which carries the collapsed breadcrumbs so the
/// app can e.g. open them in a popover.
/// </summary>
public sealed class IonBreadcrumbCollapsedClickEventArgs
{
    /// <summary>The breadcrumb host elements currently hidden by the collapse, in document
    /// order (Ionic passes the <c>HTMLIonBreadcrumbElement</c>s; Miko passes their built
    /// <see cref="Element"/> subtrees).</summary>
    public IReadOnlyList<Element> CollapsedBreadcrumbs { get; }

    public IonBreadcrumbCollapsedClickEventArgs(IReadOnlyList<Element> collapsedBreadcrumbs)
    {
        CollapsedBreadcrumbs = collapsedBreadcrumbs;
    }
}
