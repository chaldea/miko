namespace Miko.Native.Clipboard;

/// <summary>未注册平台实现时的 <see cref="IClipboardService"/> 占位实现，调用即抛。</summary>
public sealed class NullClipboardService : NativeServiceBase, IClipboardService
{
    public Task WriteAsync(ClipboardWriteOptions options) => UnsupportedAsync();
    public Task<ClipboardReadResult> ReadAsync() => UnsupportedAsync<ClipboardReadResult>();
}
