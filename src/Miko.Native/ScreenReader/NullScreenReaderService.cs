namespace Miko.Native.ScreenReader;

/// <summary>未注册平台实现时的 <see cref="IScreenReaderService"/> 占位实现，调用即抛。</summary>
public sealed class NullScreenReaderService : NativeServiceBase, IScreenReaderService
{
    public Task<ScreenReaderState> IsEnabledAsync() => UnsupportedAsync<ScreenReaderState>();
    public Task SpeakAsync(SpeakOptions options) => UnsupportedAsync();

    public event Action<ScreenReaderState>? OnStateChange { add { } remove { } }
}
