using Miko.Common;
using Miko.Platform;

namespace Miko.Simulator;

/// <summary>
/// 模拟器服务接口,对外暴露模拟器的调试和控制能力。
/// </summary>
public interface ISimulatorService
{
    /// <summary>获取当前模拟的设备信息。</summary>
    DeviceInfo GetCurrentDevice();

    /// <summary>获取所有可用的设备列表。</summary>
    IReadOnlyList<DeviceInfo> GetAvailableDevices();

    /// <summary>切换到指定设备。</summary>
    bool SelectDevice(string deviceName);

    /// <summary>获取当前屏幕方向。</summary>
    Orientation GetOrientation();

    /// <summary>设置屏幕方向。</summary>
    bool SetOrientation(Orientation orientation);

    /// <summary>获取安全区是否启用。</summary>
    bool GetSafeAreaEnabled();

    /// <summary>切换安全区启用状态。</summary>
    bool ToggleSafeArea(bool enabled);

    /// <summary>捕获当前设备画面的截图。</summary>
    byte[]? CaptureScreenshot();
}

/// <summary>设备信息（对外暴露的简化版本）。</summary>
public sealed record DeviceInfo(
    string Name,
    int LogicalWidth,
    int LogicalHeight,
    float Scale,
    string Kind,
    string Platform);
