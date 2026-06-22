using Miko.Common;
using SkiaSharp;

namespace Miko.Platform.Resources;

/// <summary>
/// 图片资源加载器：把 <see cref="MediaSource"/>（统一协议）解析并解码为 <see cref="SKBitmap"/>。
/// 由渲染引擎在 <c>&lt;img&gt;</c> 首次进入树时调用，加载在渲染线程之外进行，完成后引擎触发重绘。
/// <para>
/// 实现者必须保证 <see cref="LoadAsync"/> 不抛异常（失败返回 <c>null</c>，上层回退到占位图/背景），
/// 且回调可能在后台线程完成（引擎侧通过 <c>MikoEngine.PostInvalidate</c> 把失效转交主循环）。
/// </para>
/// </summary>
public interface IImageLoader
{
    /// <summary>异步加载并解码图片。失败返回 <c>null</c>，不得抛异常。</summary>
    Task<SKBitmap?> LoadAsync(MediaSource source, CancellationToken ct = default);
}
