using SkiaSharp;

namespace Miko.Core.DomElements;

/// <summary>
/// 图片元素
/// </summary>
public class ImageElement : Element
{
    public override string TagName => "img";
    public string? Source { get; set; }
    public SKBitmap? Bitmap { get; internal set; }
}
