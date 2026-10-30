using Miko.Common;

namespace Miko.Graphics;

/// <summary>
/// 创建可绘制资源（文本、图像、离屏表面）的服务（ISSUE-162）。
///
/// <para>由 <c>AddMikoEngine</c> 注册到容器（一个容器一份，与引擎相同），组件可以
/// <c>[Inject]</c> 取得；绘制回调里也可以直接用 <see cref="IDrawingContext.Graphics"/>。
/// 返回的对象都由渲染后端实现，调用方只看到这里的抽象类型。</para>
///
/// <para>线程安全：可在任意线程创建资源；绘制仍只在渲染线程进行。</para>
/// </summary>
public interface IGraphicsFactory
{
    /// <summary>
    /// 排版一行文本：字体回退、双向重排与整形都已完成，结果可反复绘制。
    /// 同样的文本与样式返回的结果共享缓存，调用方无需也不得释放。
    /// </summary>
    TextBlock CreateText(string text, TextStyle style);

    /// <summary>
    /// 解码图像（PNG / JPEG / WebP / GIF 首帧 / SVG）。SVG 解码为矢量图像，按绘制尺寸清晰呈现。
    /// 数据无法识别时返回 <c>null</c>。调用方拥有返回值，不再使用时释放。
    /// </summary>
    Image? DecodeImage(Stream stream);

    /// <inheritdoc cref="DecodeImage(Stream)"/>
    Image? DecodeImage(ReadOnlySpan<byte> data);

    /// <summary>
    /// 以像素数据创建图像（拷贝一份）。<paramref name="rowBytes"/> 为 0 表示紧凑排列。
    /// 尺寸非正或数据不足时返回 <c>null</c>。调用方拥有返回值。
    /// </summary>
    Image? CreateImage(int width, int height, ImagePixelFormat format, ReadOnlySpan<byte> pixels, int rowBytes = 0);

    /// <summary>
    /// 创建一张 CPU 离屏表面（像素尺寸），用于无窗口渲染、截图与测试。调用方拥有返回值。
    /// </summary>
    RenderSurface CreateSurface(int width, int height);
}

/// <summary>文本的排版样式。</summary>
/// <param name="FontFamily">字体族（可为逗号分隔的回退链，与 CSS <c>font-family</c> 相同）。</param>
/// <param name="FontSize">字号（逻辑像素）。</param>
/// <param name="FontWeight">字重。</param>
/// <param name="LetterSpacing">字间距（逻辑像素）。</param>
/// <param name="Direction">段落方向；<c>null</c> 表示按第一个强方向字符判定。</param>
public readonly record struct TextStyle(
    string FontFamily,
    float FontSize,
    FontWeight FontWeight = FontWeight.Normal,
    float LetterSpacing = 0,
    Direction? Direction = null);
