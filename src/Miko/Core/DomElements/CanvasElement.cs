using Miko.Common;
using SkiaSharp;

namespace Miko.Core.DomElements;

/// <summary>
/// Custom content painted on the render thread inside the element's content box.
/// The engine owns the canvas; callbacks must not retain or dispose it.
/// </summary>
public class CanvasElement : Element
{
    public override string TagName => "canvas";
    public Action<SKCanvas, RectF>? Paint { get; set; }
}
