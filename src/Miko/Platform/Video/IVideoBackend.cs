namespace Miko.Platform.Video;

/// <summary>
/// 视频后端工厂。由各平台宿主在启动时注册到 DI 容器（<c>IServiceCollection</c>）。
/// 渲染引擎只通过本接口创建/销毁播放会话，自身不感知具体平台的解复用与编解码实现。
/// <para>
/// 桌面（Miko.Windowing）以 FFmpeg 实现；移动端（未来 Miko.Android / Miko.iOS）
/// 分别以 MediaCodec / AVFoundation 实现。三者对外形态一致（见 <see cref="IVideoFrameSource"/>），
/// 区别仅在解码与 GPU 资源来源。
/// </para>
/// </summary>
public interface IVideoBackend
{
    /// <summary>
    /// 为一个 <c>VideoElement</c> 创建播放会话。<paramref name="source"/> 可以是本地文件路径、
    /// http(s) URL 或自定义 scheme。实现者负责解复用、解码与 GPU 资源分配。
    /// 会话创建后处于 <see cref="VideoSessionState.Loading"/>，加载完成通过事件通知。
    /// </summary>
    IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options);

    /// <summary>后端能力查询（硬件解码、HDR、受支持的 MIME 类型等），用于上层降级策略。</summary>
    VideoBackendCapabilities Capabilities { get; }
}
