namespace Miko.Native.StatusBar;

/// <summary>未注册平台实现时的 <see cref="IStatusBarService"/> 占位实现，调用即抛。</summary>
public sealed class NullStatusBarService : NativeServiceBase, IStatusBarService
{
    public Task SetStyleAsync(StatusBarStyleOptions options) => UnsupportedAsync();
    public Task SetBackgroundColorAsync(BackgroundColorOptions options) => UnsupportedAsync();
    public Task ShowAsync(AnimationOptions? options = null) => UnsupportedAsync();
    public Task HideAsync(AnimationOptions? options = null) => UnsupportedAsync();
    public Task<StatusBarInfo> GetInfoAsync() => UnsupportedAsync<StatusBarInfo>();
    public Task SetOverlaysWebViewAsync(SetOverlaysWebViewOptions options) => UnsupportedAsync();

    public event Action<StatusBarInfo>? OnVisibilityChanged { add { } remove { } }
    public event Action<StatusBarInfo>? OnOverlayChanged { add { } remove { } }
}
