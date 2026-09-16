namespace Miko.Native.Clipboard;

/// <summary>
/// 写入剪贴板的内容。<see cref="String"/>、<see cref="Image"/>、<see cref="Url"/>
/// 三者按该优先级取第一个非空值写入。
/// </summary>
public sealed record ClipboardWriteOptions
{
    /// <summary>要写入的纯文本。</summary>
    public string? String { get; init; }

    /// <summary>要写入的图片，Data URL 形式（<c>data:image/png;base64,...</c>）。</summary>
    public string? Image { get; init; }

    /// <summary>要写入的 URL。</summary>
    public string? Url { get; init; }

    /// <summary>剪贴项标签（Android）。</summary>
    public string? Label { get; init; }
}

/// <summary>剪贴板读取结果。</summary>
/// <param name="Value">剪贴板内容；图片为 Data URL。</param>
/// <param name="Type">内容 MIME 类型，如 <c>text/plain</c>、<c>image/png</c>。</param>
public sealed record ClipboardReadResult(string Value, string Type);
