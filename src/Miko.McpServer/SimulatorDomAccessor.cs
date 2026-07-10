using Miko.Core;
using Miko.DevTools;
using Miko.Simulator;

namespace Miko.McpServer;

/// <summary>
/// 将 <see cref="SimulatorHost"/> 的渲染线程marshal能力适配为 DevTools 的
/// <see cref="IDomAccessor"/>，使 <see cref="DevToolsService"/> 能在渲染线程上
/// 安全地检查运行中的应用引擎。
/// </summary>
internal sealed class SimulatorDomAccessor : IDomAccessor
{
    private readonly SimulatorHost _host;

    public SimulatorDomAccessor(SimulatorHost host)
    {
        _host = host;
    }

    public T Invoke<T>(Func<MikoEngine, T> func)
        => _host.InvokeOnRenderThread(() => func(_host.AppController.Engine));

    public void Invoke(Action<MikoEngine> action)
        => _host.InvokeOnRenderThread(() => action(_host.AppController.Engine));
}
