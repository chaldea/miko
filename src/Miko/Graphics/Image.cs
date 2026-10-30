namespace Miko.Graphics;

/// <summary>
/// 一张可绘制的图像：位图、矢量图（SVG）或 GPU 纹理（如视频帧）。具体存储由渲染后端决定，
/// 对外只暴露尺寸（ISSUE-162）。
///
/// <para>通过 <see cref="IGraphicsFactory"/>（解码、像素）或 <see cref="GraphicsDevice"/>
/// （GPU 纹理、视频平面）创建；不能在核心库之外派生。</para>
///
/// <para><b>所有权</b>：自己创建的图像由自己释放。引擎交出的图像（<c>ImageElement.Image</c>、
/// 视频帧源返回的帧）归引擎或帧源所有，使用方不得释放。</para>
/// </summary>
public abstract class Image : IDisposable
{
    private protected Image()
    {
    }

    /// <summary>像素宽度；矢量图为其 <c>viewBox</c> 宽度。</summary>
    public abstract int Width { get; }

    /// <summary>像素高度；矢量图为其 <c>viewBox</c> 高度。</summary>
    public abstract int Height { get; }

    /// <summary>是否为矢量图（按任意尺寸绘制都清晰）。</summary>
    public abstract bool IsVector { get; }

    public abstract void Dispose();
}

/// <summary>像素数据的排列方式（每个通道 8 位）。</summary>
public enum ImagePixelFormat
{
    /// <summary>R、G、B、A 四字节，alpha 未预乘。</summary>
    Rgba8888,

    /// <summary>B、G、R、A 四字节，alpha 未预乘。</summary>
    Bgra8888,

    /// <summary>R、G、B 加一个被忽略的字节，图像不透明（多数视频解码器的 RGB 输出）。</summary>
    Rgbx8888,

    /// <summary>B、G、R 加一个被忽略的字节，图像不透明。</summary>
    Bgrx8888,

    /// <summary>单通道（如 NV12 的亮度平面），不透明。</summary>
    R8,

    /// <summary>双通道交错（如 NV12 的色度平面），不透明。</summary>
    Rg88,
}

/// <summary>位图缩放时的重采样方式。矢量图不受影响。</summary>
public enum ImageSampling
{
    /// <summary>
    /// 按实际的设备像素缩放比选择：1:1 逐像素拷贝，放大与轻度缩小用 Catmull-Rom 三次插值，
    /// 大幅缩小用 mipmap 抗走样（与引擎绘制 <c>&lt;img&gt;</c> 相同）。
    /// </summary>
    Auto,

    /// <summary>最近邻，不做插值。</summary>
    Nearest,

    /// <summary>双线性插值，不用 mipmap。</summary>
    Linear,

    /// <summary>Mitchell 三次插值，适合整幅画面的缩小合成。</summary>
    Cubic,
}
