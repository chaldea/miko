using SkiaSharp;

namespace Miko.Platform.Video;

/// <summary>
/// 渲染线程对视频帧的视图：提供"当前应显示的一帧"作为 GPU 资源。
/// 不暴露 CPU 像素，从契约上引导零拷贝实现。
/// <para>
/// 重要约束：Skia 的 <see cref="GRContext"/> 非线程安全，所有 <c>SKImage.FromTexture</c> 等
/// GPU 资源包装必须在渲染线程进行。因此后端解码线程只负责把 GPU 资源句柄
/// （D3D11 共享句柄 / DMA-BUF FD / CVPixelBuffer）放入内部队列，<see cref="AcquireCurrentFrame"/>
/// 在渲染线程被调用时才真正包装成 <see cref="SKImage"/>。软解后端则在此处一次性上传纹理。
/// </para>
/// </summary>
public interface IVideoFrameSource
{
    /// <summary>
    /// 获取当前帧。返回的 <see cref="SKImage"/> 由帧源拥有并管理生命周期，调用方不得 Dispose。
    /// 尚无可用帧时（首帧前 / seek 中）返回 <c>null</c>，由上层回退到 poster 或背景。
    /// <para>
    /// 仅在渲染线程调用。<paramref name="grContext"/> 为当前渲染所用的 GPU 上下文；
    /// 离屏/软件渲染（无 GPU 上下文）时为 <c>null</c>，实现应回退到 CPU 光栅图像。
    /// </para>
    /// </summary>
    SKImage? AcquireCurrentFrame(GRContext? grContext);

    /// <summary>
    /// 通知后端当前帧已被本帧渲染消费，可用于解码节流与 GPU 资源引用计数。
    /// 与 <see cref="AcquireCurrentFrame"/> 成对调用。
    /// </summary>
    void ReleaseCurrentFrame();
}
