using Miko.Core;

namespace Miko.DevTools;

/// <summary>
/// DOM 访问抽象。DevTools 调试能力需要读写运行中的 <see cref="MikoEngine"/>，
/// 而引擎的 DOM/布局只能在渲染线程上被安全触碰。宿主（如模拟器）实现本接口，
/// 负责把操作marshal到渲染线程执行，使 <see cref="DevToolsService"/> 与线程模型解耦。
/// </summary>
public interface IDomAccessor
{
    /// <summary>在渲染线程上执行 <paramref name="func"/> 并返回结果（阻塞等待）。</summary>
    T Invoke<T>(Func<MikoEngine, T> func);

    /// <summary>在渲染线程上执行 <paramref name="action"/>（阻塞等待）。</summary>
    void Invoke(Action<MikoEngine> action);
}
