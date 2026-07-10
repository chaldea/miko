namespace Miko.Simulator;

/// <summary>
/// 模拟器宿主生命周期观察者。由 DI 容器解析并在 <see cref="SimulatorHost"/> 创建后回调，
/// 使外部模块（如 Miko.McpServer）能拿到宿主引用而无需 Miko.Simulator 反向依赖它们。
/// </summary>
public interface ISimulatorHostObserver
{
    /// <summary>模拟器宿主已创建（尚未进入渲染循环）时调用。</summary>
    void OnHostStarted(SimulatorHost host);
}
