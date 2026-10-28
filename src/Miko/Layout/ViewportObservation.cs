using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.Layout;

/// <summary>
/// An element that wants to know, after every layout and every scroll, which part of the viewport
/// it can be seen through (ISSUE-151). <c>Virtualize&lt;TItem&gt;</c> is the consumer: it renders only
/// the items that fall inside the visible region, so the work a frame does is bounded by the screen,
/// not by the length of the list.
///
/// <para>The engine collects observers while it builds the layout tree (an observer under a
/// <c>display: none</c> ancestor is never reached, so it is not notified until it is shown) and calls
/// them before the frame is painted — see <see cref="MikoEngine"/>'s settle pass. An observer may
/// re-render (the engine then lays the tree out again and notifies once more, still before painting,
/// so the frame shows the new content and never the blank area it replaced) or adjust a scroll
/// container's offset (scroll anchoring).</para>
/// </summary>
internal interface IViewportObserver
{
    /// <summary>
    /// Reports where the element can currently be seen. Called on the render thread, at least once
    /// after every layout and whenever a frame is about to be painted; the observer is expected to
    /// return quickly when nothing it depends on has changed.
    /// </summary>
    /// <returns><c>true</c> when the observer changed a scroll offset (the other observers are then
    /// notified again with the new geometry).</returns>
    bool OnViewportChanged(in ViewportObservation observation);
}

/// <summary>
/// Where an <see cref="IViewportObserver"/> can be seen, in <b>layout coordinates</b> — the space its
/// own boxes are laid out in, which does not include any scrolling.
/// </summary>
/// <param name="LayoutRoot">The layout tree the geometry belongs to. A different instance than at the
/// previous call means the tree was laid out again and box geometry must be re-read.</param>
/// <param name="VisibleRect">The viewport, clipped by every ancestor that clips (any <c>overflow</c>
/// other than <c>visible</c>) and moved by their scroll offsets. Empty when nothing of the area the
/// element could occupy is on screen. It is not intersected with the element's own boxes: an observer
/// above the visible region sees a rect below itself.</param>
/// <param name="ScrollContainer">The nearest ancestor that scrolls vertically (<c>overflow-y</c> auto or
/// scroll), or <c>null</c> when none does (the root does not scroll either).</param>
/// <param name="Viewport">The layout viewport, to observe again after changing a scroll offset
/// (<see cref="ViewportObservers.Observe"/>).</param>
internal readonly record struct ViewportObservation(LayoutBox LayoutRoot, RectF VisibleRect, LayoutBox? ScrollContainer, RectF Viewport)
{
    public bool IsVisible => VisibleRect.Width > 0f && VisibleRect.Height > 0f;
}

/// <summary>Computes <see cref="ViewportObservation"/>s for the observers registered on a layout root.</summary>
internal static class ViewportObservers
{
    /// <summary>The observers of one layout tree (document order) and its viewport rectangle.</summary>
    internal sealed class Set(List<(Element Element, IViewportObserver Observer)> observers, RectF viewport)
    {
        public List<(Element Element, IViewportObserver Observer)> Observers { get; } = observers;

        /// <summary>The layout viewport: the area the root was laid out into.</summary>
        public RectF Viewport { get; } = viewport;
    }

    /// <summary>
    /// Where <paramref name="element"/> can be seen in the tree laid out as <paramref name="layoutRoot"/>.
    ///
    /// <para>Walks the element's ancestors (in the DOM: the observer itself may be a layout-transparent
    /// fragment with no box of its own). Every ancestor that clips its content intersects the region
    /// with its padding box — minus the Classic scrollbar gutter, exactly the rectangle the renderer
    /// clips to — and the region is moved into the content's coordinates by that ancestor's scroll
    /// offset and elastic overscroll, which is the translation the renderer applies. A <c>fixed</c>
    /// ancestor is not moved by the scroll containers above it, so the walk stops there.</para>
    ///
    /// <para>Transforms and the page-transition layer offsets are paint-time effects and are not
    /// applied: an observer inside a sliding modal sees the region it will occupy once at rest.</para>
    /// </summary>
    public static ViewportObservation Observe(Element element, LayoutBox layoutRoot, RectF viewport)
    {
        // The region in the coordinates of the box being examined, starting from the observer and
        // widening outwards. Clips are collected innermost first; each is expressed in the observer's
        // coordinates by adding the scroll offsets of every clipping box from the observer up to and
        // including itself — the content of a box is drawn translated by its own scroll and by the
        // scroll of every clipping ancestor, so undoing that translation maps a clip back.
        float scrollX = 0f, scrollY = 0f;
        RectF? clip = null;
        LayoutBox? scrollContainer = null;

        for (var current = element.Parent; current != null; current = current.Parent)
        {
            if (current is FragmentElement) continue;
            var box = current.LayoutBox;
            if (box == null) continue;

            var style = box.ComputedStyle;
            bool clips = style.OverflowX != Overflow.Visible || style.OverflowY != Overflow.Visible;
            if (clips)
            {
                scrollX += box.ScrollLeft + box.OverscrollX;
                scrollY += box.ScrollTop + box.OverscrollY;

                var pb = box.BoxModel.PaddingBox;
                var rect = new RectF(
                    pb.X + scrollX,
                    pb.Y + scrollY,
                    Math.Max(0f, pb.Width - box.VerticalScrollbarGutter),
                    Math.Max(0f, pb.Height - box.HorizontalScrollbarGutter));
                clip = clip is { } inner ? RectF.Intersect(inner, rect) : rect;

                if (scrollContainer == null && style.OverflowY is Overflow.Auto or Overflow.Scroll)
                    scrollContainer = box;
            }

            if (style.Position == Position.Fixed) break;
        }

        // The viewport itself, moved by the total scroll. A fixed ancestor is positioned against the
        // viewport and drawn without the scroll of the boxes above it — which the walk never added.
        var view = new RectF(viewport.X + scrollX, viewport.Y + scrollY, viewport.Width, viewport.Height);
        var visible = clip is { } c ? RectF.Intersect(c, view) : view;
        return new ViewportObservation(layoutRoot, visible, scrollContainer, viewport);
    }
}
