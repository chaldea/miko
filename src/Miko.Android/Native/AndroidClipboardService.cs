using Android.Content;
using Miko.Native;
using Miko.Native.Clipboard;

namespace Miko.Android.Native;

/// <summary>Android 剪贴板，基于 <see cref="ClipboardManager"/>。</summary>
internal sealed class AndroidClipboardService : NativeServiceBase, IClipboardService
{
    private readonly INativeHostContext _hostContext;

    public AndroidClipboardService(INativeHostContext hostContext) => _hostContext = hostContext;

    private ClipboardManager Manager =>
        _hostContext.RequireHost<AndroidNativeHost>().Context
            .GetSystemService(Context.ClipboardService) as ClipboardManager
        ?? throw new InvalidOperationException("ClipboardManager is unavailable.");

    public Task WriteAsync(ClipboardWriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // 图片需要经 ContentProvider 暴露 URI 才能放进剪贴板；仅凭 Data URL 做不到，
        // 明确不支持而不是悄悄写一段 base64 文本。
        if (options.Image is not null)
            throw new PlatformNotSupportedException(
                $"{nameof(AndroidClipboardService)} cannot write images; only text and URLs are supported.");

        var label = options.Label ?? "Miko";

        var clip = options.Url is not null && options.String is null
            ? ClipData.NewRawUri(label, global::Android.Net.Uri.Parse(options.Url))
            : ClipData.NewPlainText(label, options.String ?? options.Url
                ?? throw new ArgumentException("ClipboardWriteOptions must set String or Url.", nameof(options)));

        Manager.PrimaryClip = clip;
        return Task.CompletedTask;
    }

    public Task<ClipboardReadResult> ReadAsync()
    {
        var clip = Manager.PrimaryClip;
        if (clip is null || clip.ItemCount == 0)
            return Task.FromResult(new ClipboardReadResult(string.Empty, "text/plain"));

        var item = clip.GetItemAt(0);

        if (item?.Uri is not null)
            return Task.FromResult(new ClipboardReadResult(item.Uri.ToString() ?? string.Empty, "text/uri-list"));

        var text = item?.Text ?? string.Empty;
        return Task.FromResult(new ClipboardReadResult(text, "text/plain"));
    }
}
