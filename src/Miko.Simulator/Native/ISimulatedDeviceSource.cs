namespace Miko.Simulator.Native;

/// <summary>
/// 向模拟器的 Native 服务提供「当前模拟设备」。
/// <para>
/// 服务需要在**调用时**读取当前设备（用户随时可以在面板上切换设备），因此不能在构造时
/// 把 <see cref="DeviceProfile"/> 拷走。<see cref="SimulatorHost"/> 实现本接口，
/// 通过 <c>INativeHostContext</c> 交给各服务。
/// </para>
/// </summary>
public interface ISimulatedDeviceSource
{
    /// <summary>当前选中的模拟设备。</summary>
    DeviceProfile CurrentDevice { get; }
}
