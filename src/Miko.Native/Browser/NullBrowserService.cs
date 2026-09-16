namespace Miko.Native.Browser;

/// <summary>未注册平台实现时的 <see cref="IBrowserService"/> 占位实现，调用即抛。</summary>
public sealed class NullBrowserService : NativeServiceBase, IBrowserService
{
    public Task OpenAsync(OpenOptions options) => UnsupportedAsync();
    public Task CloseAsync() => UnsupportedAsync();

    public event Action? OnBrowserFinished { add { } remove { } }
    public event Action? OnBrowserPageLoaded { add { } remove { } }
}
