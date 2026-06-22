namespace Miko.Common;

/// <summary>资源 URI 的协议（scheme）。</summary>
public enum MediaSourceScheme
{
    /// <summary>空源（未设置）。</summary>
    None,
    /// <summary>本地文件路径（<c>file://</c> 或裸路径）。</summary>
    File,
    /// <summary>嵌入资源（<c>res://</c>）。</summary>
    Resource,
    /// <summary>HTTP 网络资源。</summary>
    Http,
    /// <summary>HTTPS 网络资源。</summary>
    Https,
    /// <summary>内联 data URI（<c>data:[mime];base64,...</c>）。</summary>
    Data,
}

/// <summary>
/// 统一的媒体资源源。把 <c>&lt;img&gt;</c> / <c>&lt;video&gt;</c> 的 <c>src</c> 字符串解析为
/// 协议（<see cref="MediaSourceScheme"/>）+ 定位串（<see cref="Value"/>），由资源管理器据此选择加载方式。
/// <para>
/// 支持从字符串隐式转换（对齐 <see cref="Color"/> 的 <c>implicit operator</c> 用法），
/// 因此 <c>img.Source = "https://..."</c> 与 <c>img.Source = null</c> 均可直接赋值。
/// </para>
/// </summary>
public readonly struct MediaSource
{
    /// <summary>识别出的协议。</summary>
    public MediaSourceScheme Scheme { get; }

    /// <summary>原始书写串（保留协议前缀），如 <c>"res://Assets/a.png"</c>。<see cref="ToString"/> 返回它。</summary>
    public string Raw { get; }

    /// <summary>
    /// 去协议后的定位串：本地路径 / 资源名 / 完整 http(s) url / data 负载。
    /// 网络与文件源用于直接传给后端（FFmpeg / HttpClient）；资源与 data 源由资源管理器解析为字节。
    /// </summary>
    public string Value { get; }

    /// <summary>是否为空源（未设置 src）。</summary>
    public bool IsEmpty => string.IsNullOrEmpty(Raw);

    /// <summary>是否为网络源（http/https）。</summary>
    public bool IsNetwork => Scheme is MediaSourceScheme.Http or MediaSourceScheme.Https;

    private MediaSource(MediaSourceScheme scheme, string raw, string value)
    {
        Scheme = scheme;
        Raw = raw;
        Value = value;
    }

    /// <summary>空源单例。</summary>
    public static readonly MediaSource Empty = new(MediaSourceScheme.None, string.Empty, string.Empty);

    /// <summary>把书写串解析为 <see cref="MediaSource"/>。空串返回 <see cref="Empty"/>，裸路径视为本地文件。</summary>
    public static MediaSource Parse(string? source)
    {
        if (string.IsNullOrEmpty(source))
            return Empty;

        // 协议前缀大小写不敏感。
        if (StartsWith(source, "file://"))
        {
            // file:///C:/x → C:/x ；file://server/share → //server/share
            var rest = source.Substring("file://".Length);
            if (rest.Length >= 1 && rest[0] == '/' && rest.Length >= 3 && rest[2] == ':')
                rest = rest.Substring(1); // 三斜杠形式的本地盘符路径，去掉前导 '/'
            return new MediaSource(MediaSourceScheme.File, source, rest);
        }

        if (StartsWith(source, "res://"))
            return new MediaSource(MediaSourceScheme.Resource, source, source.Substring("res://".Length));

        if (StartsWith(source, "https://"))
            return new MediaSource(MediaSourceScheme.Https, source, source);

        if (StartsWith(source, "http://"))
            return new MediaSource(MediaSourceScheme.Http, source, source);

        if (StartsWith(source, "data:"))
            return new MediaSource(MediaSourceScheme.Data, source, source);

        // 其它（裸路径，如 "movie.mp4" / "C:\\imgs\\a.png" / "/assets/a.png"）按本地文件处理。
        return new MediaSource(MediaSourceScheme.File, source, source);
    }

    /// <summary>
    /// 返回可直接传给后端（FFmpeg / HttpClient）的 URI 串：网络与文件源即 <see cref="Value"/>。
    /// 资源与 data 源不应传给 FFmpeg，需经资源管理器解析为字节，此时回退到 <see cref="Value"/>。
    /// </summary>
    public string ToUri() => Value;

    /// <inheritdoc />
    public override string ToString() => Raw;

    public static implicit operator MediaSource(string? value) => Parse(value);

    private static bool StartsWith(string s, string prefix) =>
        s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
