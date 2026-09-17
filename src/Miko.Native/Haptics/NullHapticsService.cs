namespace Miko.Native.Haptics;

/// <summary>未注册平台实现时的 <see cref="IHapticsService"/> 占位实现，调用即抛。</summary>
public sealed class NullHapticsService : NativeServiceBase, IHapticsService
{
    public Task ImpactAsync(ImpactOptions? options = null) => UnsupportedAsync();
    public Task NotificationAsync(NotificationOptions? options = null) => UnsupportedAsync();
    public Task VibrateAsync(VibrateOptions? options = null) => UnsupportedAsync();
    public Task SelectionStartAsync() => UnsupportedAsync();
    public Task SelectionChangedAsync() => UnsupportedAsync();
    public Task SelectionEndAsync() => UnsupportedAsync();
}
