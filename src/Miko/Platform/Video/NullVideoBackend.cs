namespace Miko.Platform.Video;

/// <summary>
/// 核心库内置的空视频后端（ISSUE-129）。未注册任何平台后端时作为默认实现，
/// 使 <see cref="IVideoBackend"/> 可以和 <c>IImageLoader</c> / <c>ISyntaxHighlighter</c> 一样
/// 走构造器注入，而不必让引擎依赖 <c>IServiceProvider</c> 做可选解析。
///
/// <para>行为等价于「没有后端」：不创建任何会话，<c>&lt;video&gt;</c> 元素只显示背景/poster。
/// 平台宿主（桌面 FFmpeg、Android MediaCodec、iOS AVFoundation）注册自己的实现后即被覆盖。</para>
/// </summary>
public sealed class NullVideoBackend : IVideoBackend
{
    /// <summary>无解码能力：既不支持硬件解码/HDR，也不支持任何 MIME 类型。</summary>
    public VideoBackendCapabilities Capabilities { get; } =
        new(HardwareDecode: false, Hdr: false, SupportedMimeTypes: Array.Empty<string>());

    /// <summary>
    /// 本后端不提供播放能力。引擎在创建会话前会先通过 <see cref="IsNull"/> 判定并跳过，
    /// 因此正常路径不会走到这里；直接调用则明确失败而不是返回一个假装能播的会话。
    /// </summary>
    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        => throw new NotSupportedException(
            "No video backend is registered. Register a platform backend (e.g. FFmpegVideoBackend " +
            "from Miko.Windowing) to enable <video> playback.");
}
