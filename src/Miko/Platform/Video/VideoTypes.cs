namespace Miko.Platform.Video;

/// <summary>视频源描述。<paramref name="Uri"/> 为本地路径或 http(s) URL；MIME 可选，用于后端选择解码器。</summary>
public sealed record VideoSourceDescriptor(string Uri, string? MimeType = null)
{
    public bool IsHls =>
        string.Equals(MimeType, "application/vnd.apple.mpegurl", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(MimeType, "application/x-mpegURL", StringComparison.OrdinalIgnoreCase) ||
        (System.Uri.TryCreate(Uri, UriKind.Absolute, out var parsed)
            ? parsed.AbsolutePath : Uri.Split('?', '#')[0]).EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);

    /// <summary>是否为网络源（http/https）。</summary>
    public bool IsNetwork =>
        Uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        Uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 把源解析为后端可直接打开的形式：网络 URL 原样返回，本地相对路径解析为绝对路径。
    ///
    /// <para>
    /// **为什么必须做这一步**：系统解码器（Media Foundation / GStreamer / AVFoundation）
    /// 把相对路径解析到**进程当前工作目录**，而 Miko 的资源约定与
    /// <c>ResourceManager.ResolveFilePath</c> 一致 —— 相对路径基于
    /// <see cref="AppContext.BaseDirectory"/>。两者在「从 IDE 或其它目录启动」时并不相同，
    /// 不归一化就会出现 <c>&lt;img&gt;</c> 能加载而同目录的 <c>&lt;video&gt;</c> 报找不到文件。
    /// </para>
    ///
    /// <para>文件不存在时返回原串，让后端给出自己的错误信息（可能是它支持的自定义 scheme）。</para>
    /// </summary>
    public string ResolveForBackend()
    {
        if (IsNetwork) return Uri;

        // 去掉 file:// 前缀（含三斜杠形式的盘符路径）。
        string path = Uri;
        if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            path = path["file://".Length..];
            if (path.Length >= 3 && path[0] == '/' && path[2] == ':')
                path = path[1..];
        }

        if (Path.IsPathRooted(path))
            return path;

        // 其它自定义 scheme（含 "://"）交给后端自行处理。
        if (path.Contains("://", StringComparison.Ordinal))
            return Uri;

        string basePath = Path.Combine(AppContext.BaseDirectory, path);
        return File.Exists(basePath) ? basePath : path;
    }
}

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
    public sealed record Buffering(bool IsBuffering) : VideoSessionEvent;
    /// <summary>媒体已加载，已知内禀尺寸与时长。引擎据此写入元素内禀尺寸并触发重排。</summary>
    public sealed record Loaded(int Width, int Height, TimeSpan Duration) : VideoSessionEvent;

    /// <summary>新解码帧可供显示（PTS 已到呈现时机）。引擎据此把对应元素标脏。</summary>
    public sealed record FrameAvailable(TimeSpan Pts) : VideoSessionEvent;

    /// <summary>播放结束。</summary>
    public sealed record Ended : VideoSessionEvent;

    /// <summary>发生错误（打开失败 / 解码错误等）。</summary>
    public sealed record Error(string Message, Exception? Cause) : VideoSessionEvent;
}

public readonly record struct VideoTimeRange(TimeSpan Start, TimeSpan End);
