namespace Miko.Native.Device;

/// <summary>未注册平台实现时的 <see cref="IDeviceService"/> 占位实现，调用即抛。</summary>
public sealed class NullDeviceService : NativeServiceBase, IDeviceService
{
    public Task<DeviceId> GetIdAsync() => UnsupportedAsync<DeviceId>();
    public Task<DeviceInfo> GetInfoAsync() => UnsupportedAsync<DeviceInfo>();
    public Task<BatteryInfo> GetBatteryInfoAsync() => UnsupportedAsync<BatteryInfo>();
    public Task<LanguageCode> GetLanguageCodeAsync() => UnsupportedAsync<LanguageCode>();
    public Task<LanguageTag> GetLanguageTagAsync() => UnsupportedAsync<LanguageTag>();
}
