namespace Miko.Native.Haptics;

/// <summary>
/// 触觉与震动反馈。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/v8/native/haptics#api">Haptics API</see>。
/// 支持平台：Android、iOS。
/// </summary>
public interface IHapticsService
{
    /// <summary>碰撞触觉。</summary>
    Task ImpactAsync(ImpactOptions? options = null);

    /// <summary>通知触觉。</summary>
    Task NotificationAsync(NotificationOptions? options = null);

    /// <summary>振动。</summary>
    Task VibrateAsync(VibrateOptions? options = null);

    /// <summary>选择控件开始反馈。</summary>
    Task SelectionStartAsync();

    /// <summary>选择项变化反馈。</summary>
    Task SelectionChangedAsync();

    /// <summary>选择控件结束反馈。</summary>
    Task SelectionEndAsync();
}
