using Miko.Common;
using Miko.Graphics;

namespace Miko.Core.DomElements;

/// <summary>
/// Custom content painted on the render thread inside the element's content box.
/// The engine owns the drawing context; callbacks must not retain it. Resources for drawing
/// (text, images) come from <see cref="IDrawingContext.Graphics"/>.
/// </summary>
public class CanvasElement : Element
{
    public override string TagName => "canvas";

    /// <summary>
    /// Paints the element. Receives the drawing context, clipped to the content box, and the
    /// content box itself (in the context's coordinates).
    /// </summary>
    public Action<IDrawingContext, RectF>? Paint { get; set; }
}
