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

/// <summary>
/// 矢量源（SVG）解码后的位图分辨率，与它的 CSS 内禀尺寸<b>可以不同</b>。
///
/// <para>SVG 没有固有的像素分辨率，只有一个 <c>viewBox</c> 逻辑尺寸。把它栅格化成
/// <c>viewBox</c> 大小的位图，在高密度屏上会被画布放大插值，边缘发虚（ISSUE-137）。
/// 因此加载器可以过采样栅格化（分辨率 &gt; viewBox），同时用本接口告知引擎真正的
/// <b>逻辑</b>尺寸，使 <c>&lt;img&gt;</c> 的内禀尺寸（布局输入）仍取 <c>viewBox</c>，
/// 布局不受过采样影响。</para>
///
/// <para><see cref="IImageLoader"/> 的实现可选择性实现本接口；未实现时引擎按位图像素尺寸
/// 作为内禀尺寸（位图源的正确行为）。</para>
/// </summary>
public interface IImageIntrinsicSizeProvider
{
    /// <summary>
    /// 取给定源已解码位图的 CSS 内禀尺寸。返回 <c>null</c> 表示"就用位图像素尺寸"
    /// （非矢量源，或该源未经过采样）。必须在对应 <see cref="IImageLoader.LoadAsync"/>
    /// 完成之后调用才有结果。
    /// </summary>
    (int Width, int Height)? GetIntrinsicSize(MediaSource source);
}
