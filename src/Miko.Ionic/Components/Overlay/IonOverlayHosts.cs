using System.Runtime.CompilerServices;
using Miko.Core;

namespace Miko.Ionic.Components;

/// <summary>
/// The host elements of the presented <see cref="IonModal"/>s and <see cref="IonPopover"/>s, so
/// content can close the overlay it sits in — Ionic's
/// <c>el.closest('ion-modal, ion-popover').dismiss(data, role)</c>, as <c>ion-datetime</c> does
/// from its Done / Cancel buttons. The overlays register their host element whenever it is built
/// (inline, in <see cref="IonApp"/>'s overlay layer, or from a controller); the table holds the
/// elements weakly, so a discarded generation drops out by itself.
/// </summary>
internal static class IonOverlayHosts
{
    private static readonly ConditionalWeakTable<Element, Func<string?, object?, Task>> Hosts = new();

    /// <summary>Records <paramref name="host"/> as an overlay host dismissed by <paramref name="dismiss"/>.</summary>
    public static void Register(Element host, Func<string?, object?, Task> dismiss) => Hosts.AddOrUpdate(host, dismiss);

    /// <summary>
    /// The dismiss of the nearest overlay enclosing <paramref name="element"/>, or null when it is
    /// not inside one.
    /// </summary>
    public static Func<string?, object?, Task>? FindEnclosing(Element? element)
    {
        for (var node = element?.Parent; node is not null; node = node.Parent)
        {
            if (Hosts.TryGetValue(node, out var dismiss)) return dismiss;
        }
        return null;
    }
}
