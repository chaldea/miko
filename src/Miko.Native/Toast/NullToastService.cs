namespace Miko.Native.Toast;

/// <summary>未注册平台实现时的 <see cref="IToastService"/> 占位实现，调用即抛。</summary>
public sealed class NullToastService : NativeServiceBase, IToastService
{
    public Task ShowAsync(ToastOptions options) => UnsupportedAsync();
}
