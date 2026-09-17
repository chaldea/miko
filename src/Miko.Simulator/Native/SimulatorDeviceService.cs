using System.Globalization;
using Microsoft.Extensions.Logging;
using Miko.Native;
using Miko.Native.Device;
using Miko.Platform;
using DeviceOperatingSystem = Miko.Native.Device.OperatingSystem;
using NativeDeviceInfo = Miko.Native.Device.DeviceInfo;

namespace Miko.Simulator.Native;

/// <summary>
/// 模拟器设备信息：直接映射当前选中的 <see cref="DeviceProfile"/>，
/// 使 <c>IDeviceService</c> 的返回值随面板上的设备切换而变化——这正是模拟器存在的意义。
/// </summary>
internal sealed class SimulatorDeviceService : SimulatorNativeServiceBase, IDeviceService
{
    public SimulatorDeviceService(INativeHostContext hostContext, ILogger<SimulatorDeviceService>? logger)
        : base(hostContext, logger) { }

    public Task<DeviceId> GetIdAsync()
    {
        var device = Device;
        Log(nameof(GetIdAsync), device.Name);

        // 按设备名派生，切换设备即换 ID——与真机「不同设备不同标识」一致；
        // 前缀让它一眼可辨为仿真值。
        return Task.FromResult(new DeviceId($"miko-simulator-{device.Name.ToLowerInvariant().Replace(' ', '-')}"));
    }

    public Task<NativeDeviceInfo> GetInfoAsync()
    {
        var device = Device;
        Log(nameof(GetInfoAsync), device.Name);

        var platform = device.Platform switch
        {
            HostPlatform.Ios => DevicePlatform.Ios,
            HostPlatform.Android => DevicePlatform.Android,
            _ => DevicePlatform.Web,
        };

        var os = device.Platform switch
        {
            HostPlatform.Ios => DeviceOperatingSystem.Ios,
            HostPlatform.Android => DeviceOperatingSystem.Android,
            HostPlatform.Windows => DeviceOperatingSystem.Windows,
            HostPlatform.MacOS => DeviceOperatingSystem.Mac,
            _ => DeviceOperatingSystem.Unknown,
        };

        return Task.FromResult(new NativeDeviceInfo(
            Name: device.Name,
            Model: device.Name,
            Platform: platform,
            OperatingSystem: os,
            OsVersion: "0.0",
            // 模拟器不声称某个具体系统版本；两者留空避免代码按版本号分支时被误导。
            IosVersion: null,
            AndroidSdkVersion: null,
            Manufacturer: "Miko Simulator",
            // 关键：模拟设备必须自报为虚拟设备。
            IsVirtual: true,
            MemUsed: Environment.WorkingSet,
            WebViewVersion: $"Miko.Simulator/{device.LogicalWidth}x{device.LogicalHeight}@{device.Scale:0.#}x"));
    }

    public Task<BatteryInfo> GetBatteryInfoAsync()
    {
        Log(nameof(GetBatteryInfoAsync));
        // 固定样本：非满非空且不在充电，便于验证「低电量提示」之类的 UI 分支。
        return Task.FromResult(new BatteryInfo(BatteryLevel: 0.78, IsCharging: false));
    }

    public Task<LanguageCode> GetLanguageCodeAsync()
        => Task.FromResult(new LanguageCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

    public Task<LanguageTag> GetLanguageTagAsync()
        => Task.FromResult(new LanguageTag(CultureInfo.CurrentUICulture.IetfLanguageTag));
}
