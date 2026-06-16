namespace Miko.Platform.Video;

/// <summary>视频源描述。<paramref name="Uri"/> 为本地路径或 http(s) URL；MIME 可选，用于后端选择解码器。</summary>
public sealed record VideoSourceDescriptor(string Uri, string? MimeType = null);

/// <summary>会话创建选项，对应 <c>&lt;video&gt;</c> 的 autoplay / muted / loop 等属性。</summary>
public sealed record VideoSessionOptions(
    bool AutoPlay = false,
    bool Muted = false,
    bool Loop = false,
    float InitialVolume = 1.0f,
    bool PreferHardwareDecode = true);

/// <summary>后端能力。上层据此决定是否降级（如不支持硬解时仍可软解，或不支持某 MIME 时回退占位）。</summary>
public sealed record VideoBackendCapabilities(
    bool HardwareDecode,
    bool Hdr,
    IReadOnlyList<string> SupportedMimeTypes);

/// <summary>会话状态机。</summary>
public enum VideoSessionState
{
    /// <summary>初始态，尚未开始加载。</summary>
    Idle,
    /// <summary>正在解复用/打开解码器/缓冲首帧。</summary>
    Loading,
    /// <summary>已就绪（已知时长与尺寸），等待播放。</summary>
    Ready,
    Playing,
    Paused,
    /// <summary>播放至结尾（未开启 loop）。</summary>
    Ended,
    Error,
}

/// <summary>会话事件。可能在后端解码线程触发，订阅方需注意线程边界。</summary>
public abstract record VideoSessionEvent
{
    /// <summary>媒体已加载，已知内禀尺寸与时长。引擎据此写入元素内禀尺寸并触发重排。</summary>
    public sealed record Loaded(int Width, int Height, TimeSpan Duration) : VideoSessionEvent;

    /// <summary>新解码帧可供显示（PTS 已到呈现时机）。引擎据此把对应元素标脏。</summary>
    public sealed record FrameAvailable(TimeSpan Pts) : VideoSessionEvent;

    /// <summary>播放结束。</summary>
    public sealed record Ended : VideoSessionEvent;

    /// <summary>发生错误（打开失败 / 解码错误等）。</summary>
    public sealed record Error(string Message, Exception? Cause) : VideoSessionEvent;
}
