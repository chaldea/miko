using System.ComponentModel;
using ModelContextProtocol.Server;
using Miko.Simulator;

namespace Miko.McpServer.Tools;

/// <summary>
/// 模拟器 MCP 工具集。通过 <see cref="ISimulatorService"/> 暴露设备切换、朝向、
/// 安全区与截屏能力。工具由官方 ModelContextProtocol SDK 反射发现并纳入 tools/list。
/// </summary>
[McpServerToolType]
public sealed class SimulatorTools
{
    private readonly ISimulatorService _simulator;

    public SimulatorTools(ISimulatorService simulator)
    {
        _simulator = simulator;
    }

    [McpServerTool(Name = "simulator_get_current_device", ReadOnly = true)]
    [Description("获取模拟器当前模拟的设备信息（名称、逻辑分辨率、像素密度、类别、平台）。")]
    public DeviceInfo GetCurrentDevice() => _simulator.GetCurrentDevice();

    [McpServerTool(Name = "simulator_list_devices", ReadOnly = true)]
    [Description("列出所有可切换的模拟设备。")]
    public IReadOnlyList<DeviceInfo> ListDevices() => _simulator.GetAvailableDevices();

    [McpServerTool(Name = "simulator_select_device")]
    [Description("按设备名称切换模拟设备（会同步切换平台，使依赖平台的 UI 如 Ionic md/ios 模式随之变化）。")]
    public string SelectDevice(
        [Description("设备名称，需与 simulator_list_devices 返回的 Name 完全一致，例如 'iPhone 15 Pro'。")] string deviceName)
    {
        var ok = _simulator.SelectDevice(deviceName);
        return ok ? $"已切换到设备: {deviceName}" : $"未找到设备: {deviceName}";
    }

    [McpServerTool(Name = "simulator_get_orientation", ReadOnly = true)]
    [Description("获取当前屏幕方向（Portrait 竖屏 / Landscape 横屏）。")]
    public string GetOrientation() => _simulator.GetOrientation().ToString();

    [McpServerTool(Name = "simulator_set_orientation")]
    [Description("设置屏幕方向。")]
    public string SetOrientation(
        [Description("目标方向：'Portrait'（竖屏）或 'Landscape'（横屏）。")] string orientation)
    {
        if (!Enum.TryParse<Orientation>(orientation, ignoreCase: true, out var value))
            return $"无效的方向: {orientation}（应为 Portrait 或 Landscape）";
        _simulator.SetOrientation(value);
        return $"方向已设置为: {value}";
    }

    [McpServerTool(Name = "simulator_get_safe_area", ReadOnly = true)]
    [Description("获取安全区（刘海/状态栏/Home 指示条内缩）是否启用。")]
    public bool GetSafeAreaEnabled() => _simulator.GetSafeAreaEnabled();

    [McpServerTool(Name = "simulator_toggle_safe_area")]
    [Description("启用或禁用安全区内缩。")]
    public string ToggleSafeArea(
        [Description("true 启用安全区，false 禁用。")] bool enabled)
    {
        _simulator.ToggleSafeArea(enabled);
        return enabled ? "安全区已启用" : "安全区已禁用";
    }

    [McpServerTool(Name = "simulator_screenshot", ReadOnly = true)]
    [Description("截取当前设备画面，返回 PNG 图片（base64 data URI）。")]
    public string Screenshot()
    {
        var png = _simulator.CaptureScreenshot();
        if (png == null || png.Length == 0)
            return "截屏失败：设备画面尚未就绪。";
        return $"data:image/png;base64,{Convert.ToBase64String(png)}";
    }
}
