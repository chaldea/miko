namespace Miko.Native.Clipboard;

/// <summary>
/// 系统剪贴板。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/v8/native/clipboard#api">Clipboard API</see>。
/// 支持平台：Android、iOS、Windowing。
/// </summary>
public interface IClipboardService
{
    /// <summary>写入文本、图片 Data URL 或 URL。</summary>
    Task WriteAsync(ClipboardWriteOptions options);

    /// <summary>读取剪贴板内容。</summary>
    Task<ClipboardReadResult> ReadAsync();
}
