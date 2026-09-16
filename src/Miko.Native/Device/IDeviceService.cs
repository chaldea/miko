namespace Miko.Native.Device;

/// <summary>
/// 设备、系统与电池信息。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/v8/native/device#api">Device API</see>。
/// 支持平台：Android、iOS、Windowing。
/// </summary>
public interface IDeviceService
{
    /// <summary>获取应用可用的设备标识。</summary>
    Task<DeviceId> GetIdAsync();

    /// <summary>获取设备、系统和 WebView 信息。</summary>
    Task<DeviceInfo> GetInfoAsync();

    /// <summary>获取电池电量与充电状态。</summary>
    Task<BatteryInfo> GetBatteryInfoAsync();

    /// <summary>获取两位语言代码。</summary>
    Task<LanguageCode> GetLanguageCodeAsync();

    /// <summary>获取 BCP 47 语言标签。</summary>
    Task<LanguageTag> GetLanguageTagAsync();
}
