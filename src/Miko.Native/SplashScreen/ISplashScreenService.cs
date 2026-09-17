namespace Miko.Native.SplashScreen;

/// <summary>
/// 启动画面。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/native/splash-screen#api">Splash Screen API</see>。
/// 支持平台：Android、iOS。
/// <para>
/// 启动阶段的配置项（<c>LaunchShowDuration</c>、<c>LaunchAutoHide</c>、<c>BackgroundColor</c>、
/// <c>ShowSpinner</c> 等）属于各平台的清单/资源配置，不在本接口内暴露。
/// </para>
/// </summary>
public interface ISplashScreenService
{
    /// <summary>显示启动画面。</summary>
    Task ShowAsync(ShowSplashOptions? options = null);

    /// <summary>隐藏启动画面。</summary>
    Task HideAsync(HideSplashOptions? options = null);
}
