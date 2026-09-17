namespace Miko.Native.ScreenReader;

/// <summary>
/// 屏幕阅读器与朗读。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/native/screen-reader#api">Screen Reader API</see>。
/// 支持平台：Android、iOS、Windowing。
/// </summary>
public interface IScreenReaderService
{
    /// <summary>查询屏幕阅读器是否启用。</summary>
    Task<ScreenReaderState> IsEnabledAsync();

    /// <summary>朗读一段文本。</summary>
    Task SpeakAsync(SpeakOptions options);

    /// <summary>屏幕阅读器开关状态变化。</summary>
    event Action<ScreenReaderState>? OnStateChange;
}
