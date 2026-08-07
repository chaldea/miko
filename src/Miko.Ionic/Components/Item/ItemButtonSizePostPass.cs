using Miko.Core;

namespace Miko.Ionic.Components;

/// <summary>
/// Post-pass shared by <see cref="IonItem"/> and <see cref="IonItemDivider"/>: stamps
/// <c>button-small</c> on every slotted <see cref="IonButton"/> that did not set an explicit
/// <c>size</c> — mirroring button.tsx's <c>finalSize = size === undefined &amp;&amp; this.inItem
/// ? 'small' : size</c> (where <c>inItem</c> is <c>closest('ion-item, ion-item-divider')</c>).
/// A Miko component builds its subtree detached, so a button cannot see its item ancestor in
/// its own <c>Build()</c>; the item stamps the class here instead, the same post-pass pattern
/// as <see cref="IonList"/>'s <c>item-last-in-list</c>. Stamping the class (rather than adding
/// item-scoped style rules) lets every existing <c>button-small</c> rule apply unchanged.
/// </summary>
internal static class ItemButtonSizePostPass
{
    internal static void StampSmallButtons(Element root)
    {
        foreach (var button in root.FindByClass("ion-button"))
        {
            // IonButton stamps button-{size} only when Size is explicitly set ("default"
            // included) — any of those classes means the button keeps its own size.
            if (button.HasClass("button-small") ||
                button.HasClass("button-default") ||
                button.HasClass("button-large"))
            {
                continue;
            }

            button.Class += " button-small";
        }
    }
}
