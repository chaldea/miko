using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Native;

namespace Miko.Simulator.Native;

/// <summary>
/// 模拟器 Native 服务的公共基类：提供当前模拟设备的读取与统一的调用日志。
/// <para>
/// 模拟器的定位是**让开发者在桌面上跑通调用链**：能力返回可预期的仿真数据并记录日志，
/// 而不是抛 <see cref="PlatformNotSupportedException"/>——那样开发者在模拟器里就无法走通
/// 「点按钮 → 调相机 → 拿到结果 → 渲染」这条路径，等于把调试推迟到真机。
/// </para>
/// <para>
/// 仿真数据必须**可辨识**（URI 带 <c>miko-simulator</c> 前缀、坐标为固定样本），
/// 使其不会被误当成真实设备数据。
/// </para>
/// </summary>
public abstract class SimulatorNativeServiceBase
{
    private readonly INativeHostContext _hostContext;

    /// <summary>日志器；模拟器的每次能力调用都记一条 Information，便于观察调用链。</summary>
    protected ILogger Logger { get; }

    protected SimulatorNativeServiceBase(INativeHostContext hostContext, ILogger? logger)
    {
        _hostContext = hostContext;
        Logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// 当前模拟设备。宿主尚未附加时回退到默认预设——服务可能在首帧之前就被调用，
    /// 那时报错没有意义（模拟器本就不该失败）。
    /// </summary>
    protected DeviceProfile Device =>
        (_hostContext.Host as ISimulatedDeviceSource)?.CurrentDevice ?? DeviceProfile.IPhone15Pro;

    /// <summary>记录一次能力调用。</summary>
    protected void Log(string member, string? detail = null)
        => Logger.LogInformation(
            "[Simulator.Native] {Service}.{Member} {Detail}", GetType().Name, member, detail ?? string.Empty);
}
