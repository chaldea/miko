namespace Miko.Native.SplashScreen;

/// <summary>未注册平台实现时的 <see cref="ISplashScreenService"/> 占位实现，调用即抛。</summary>
public sealed class NullSplashScreenService : NativeServiceBase, ISplashScreenService
{
    public Task ShowAsync(ShowSplashOptions? options = null) => UnsupportedAsync();
    public Task HideAsync(HideSplashOptions? options = null) => UnsupportedAsync();
}
