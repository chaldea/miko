namespace Miko.Simulator;

/// <summary>
/// 模拟器配置。控制可选设备列表、初始设备与朝向、窗口标题。
/// </summary>
public sealed class SimulatorOptions
{
    /// <summary>模拟器窗口标题。null 时默认使用 "<应用标题> — Simulator"。</summary>
    public string? Title { get; set; }

    /// <summary>可选设备列表。默认使用 <see cref="DeviceProfile.Defaults"/>。</summary>
    public IReadOnlyList<DeviceProfile> Devices { get; set; } = DeviceProfile.Defaults;

    /// <summary>初始选中设备。null 时使用 <see cref="Devices"/> 的第一个。</summary>
    public DeviceProfile? InitialDevice { get; set; }

    /// <summary>初始朝向。</summary>
    public Orientation InitialOrientation { get; set; } = Orientation.Portrait;
}
