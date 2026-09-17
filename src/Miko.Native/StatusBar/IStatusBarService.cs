namespace Miko.Native.StatusBar;

/// <summary>
/// 状态栏。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/native/status-bar#api">Status Bar API</see>。
/// 支持平台：Android、iOS。
/// </summary>
public interface IStatusBarService
{
    /// <summary>设置状态栏文字样式。</summary>
    Task SetStyleAsync(StatusBarStyleOptions options);

    /// <summary>设置状态栏背景色。Android 15+ 无效。</summary>
    Task SetBackgroundColorAsync(BackgroundColorOptions options);

    /// <summary>显示状态栏。</summary>
    Task ShowAsync(AnimationOptions? options = null);

    /// <summary>隐藏状态栏。</summary>
    Task HideAsync(AnimationOptions? options = null);

    /// <summary>获取状态栏当前状态。</summary>
    Task<StatusBarInfo> GetInfoAsync();

    /// <summary>设置状态栏是否覆盖内容。Android 15+ 无效。</summary>
    Task SetOverlaysWebViewAsync(SetOverlaysWebViewOptions options);

    /// <summary>状态栏可见性变化。</summary>
    event Action<StatusBarInfo>? OnVisibilityChanged;

    /// <summary>状态栏覆盖状态变化。</summary>
    event Action<StatusBarInfo>? OnOverlayChanged;
}
