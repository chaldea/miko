using Miko.Graphics;

namespace Miko.Platform.Video;

/// <summary>
/// 渲染线程对视频帧的视图：提供"当前应显示的一帧"作为可绘制的 <see cref="Image"/>。
/// 不暴露 CPU 像素，从契约上引导零拷贝实现。
/// <para>
/// 重要约束：GPU 上下文非线程安全，所有纹理包装（<see cref="GraphicsDevice.WrapGlTexture"/>、
/// <see cref="GraphicsDevice.WrapMetalTexture"/>）必须在渲染线程进行。因此后端解码线程只负责把 GPU 资源句柄
/// （D3D11 共享句柄 / DMA-BUF FD / CVPixelBuffer）放入内部队列，<see cref="AcquireCurrentFrame"/>
/// 在渲染线程被调用时才真正包装成图像。软解后端则在此处一次性上传像素。
/// </para>
/// </summary>
public interface IVideoFrameSource
{
    /// <summary>
    /// 获取当前帧。返回的图像由帧源拥有并管理生命周期，调用方不得释放。
    /// 尚无可用帧时（首帧前 / seek 中）返回 <c>null</c>，由上层回退到 poster 或背景。
    /// <para>
    /// 仅在渲染线程调用。<paramref name="device"/> 为当前渲染所用的设备；
    /// 离屏/软件渲染时为 <see cref="GraphicsDevice.Software"/>，实现应回退到 CPU 光栅图像
    /// （纹理包装在该设备上返回 <c>null</c>）。
    /// </para>
    /// </summary>
    Image? AcquireCurrentFrame(GraphicsDevice device);

    /// <summary>
    /// 通知后端当前帧已被本帧渲染消费，可用于解码节流与 GPU 资源引用计数。
    /// 与 <see cref="AcquireCurrentFrame"/> 成对调用。
    /// </summary>
    void ReleaseCurrentFrame();
}
