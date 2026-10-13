using System.Runtime.ExceptionServices;

namespace Miko.Windowing.Common;

/// <summary>
/// 挂在桌面事件循环上的附加窗口（例如 DevTools 窗口）。所有成员都由事件循环线程调用。
/// <para>窗口自己的渲染线程照常由它的 <see cref="SilkWindowThreadRunner"/> 持有：
/// <see cref="Create"/> 里 <see cref="SilkWindowThreadRunner.Start"/>，
/// <see cref="PumpEvents"/> 里 <see cref="SilkWindowThreadRunner.PumpEvents"/>，
/// <see cref="Destroy"/> 里 <see cref="SilkWindowThreadRunner.Stop"/>。</para>
/// </summary>
public interface ISilkEventLoopWindow
{
    /// <summary>创建并初始化原生窗口（通常随即启动它的渲染线程）。</summary>
    void Create();

    /// <summary>泵一次本窗口的事件。窗口进入关闭状态后返回 <c>false</c>，随后会调用 <see cref="Destroy"/>。</summary>
    bool PumpEvents();

    /// <summary>
    /// 停止渲染并销毁原生窗口。每个挂上的窗口恰好调用一次：<see cref="PumpEvents"/> 返回 <c>false</c> 之后、
    /// <see cref="Create"/>/<see cref="PumpEvents"/> 抛出之后，或事件循环本身结束时——
    /// 后者包括 <see cref="Create"/> 还没来得及执行的窗口，实现须容忍窗口从未创建。
    /// </summary>
    void Destroy();
}

/// <summary>
/// 进程级的桌面事件循环：负责创建 Silk 窗口并泵其原生事件的那个线程。
///
/// <para><b>为什么需要</b>：macOS（AppKit）与 GLFW 只允许在主线程上创建窗口、处理窗口事件。
/// 宿主运行之后才从别的线程打开的窗口（DevTools 由 F12 在渲染线程上打开）因此不能自起一个循环——
/// 在 macOS 上那样的窗口会永远卡死（ISSUE-148）。它改为挂到宿主已在主线程上运行的循环里：
/// 循环替它创建窗口、每轮泵它的事件、并在它关闭（或循环结束）时销毁它。</para>
///
/// <para>GLFW 的事件轮询本就是进程级的（<c>glfwPollEvents</c> 一次处理所有窗口），
/// 所以「一个线程泵所有窗口」与 GLFW 的模型一致，Windows/Linux 上也同样适用。</para>
///
/// <para>进程里第一个进入 <see cref="SilkWindowThreadRunner.Run"/> 的循环成为事件循环
/// （桌面宿主与模拟器都经由它），直到该循环结束。</para>
/// </summary>
public static class SilkEventLoop
{
    private static readonly object s_gate = new();

    // 当前充当事件循环的所有者（由 s_gate 保护）。
    private static object? s_owner;

    // 已挂上、等待事件循环创建的窗口（由 s_gate 保护）。
    private static readonly Queue<ISilkEventLoopWindow> s_pending = new();

    // 已创建、由事件循环泵事件的窗口。只由事件循环线程访问。
    private static readonly List<ISilkEventLoopWindow> s_active = new();

    /// <summary>
    /// 把 <paramref name="window"/> 挂到正在运行的事件循环上，可从任意线程调用。
    /// 事件循环在下一轮于自己的线程上调用 <see cref="ISilkEventLoopWindow.Create"/>。
    /// </summary>
    /// <returns>没有正在运行的事件循环时为 <c>false</c>（例如移动端宿主、单元测试），窗口不会被创建。</returns>
    public static bool TryAttach(ISilkEventLoopWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        lock (s_gate)
        {
            if (s_owner == null) return false;
            s_pending.Enqueue(window);
            return true;
        }
    }

    /// <summary>让 <paramref name="owner"/> 成为事件循环。已有事件循环在运行时返回 <c>false</c>。</summary>
    internal static bool TryEnter(object owner)
    {
        lock (s_gate)
        {
            if (s_owner != null) return false;
            s_owner = owner;
            return true;
        }
    }

    /// <summary>事件循环线程每轮调用：创建新挂上的窗口，泵已创建窗口的事件，销毁已关闭的窗口。</summary>
    internal static void PumpAttachedWindows()
    {
        while (TryTakePending(out var window))
        {
            // 先登记再创建：创建抛出时，循环结束会照常销毁它（见 Exit）。
            s_active.Add(window);
            window.Create();
        }

        for (int i = s_active.Count - 1; i >= 0; i--)
        {
            var window = s_active[i];
            if (window.PumpEvents()) continue;

            // 先移出再销毁，保证销毁抛出时也不会被 Exit 再销毁一次。
            s_active.RemoveAt(i);
            window.Destroy();
        }
    }

    /// <summary>
    /// 事件循环结束：销毁所有挂着的窗口（含尚未创建的），之后 <see cref="TryAttach"/> 返回 <c>false</c>。
    /// 某个窗口销毁抛出时仍会销毁其余窗口，最后重新抛出第一个异常。
    /// </summary>
    internal static void Exit(object owner)
    {
        List<ISilkEventLoopWindow> remaining;
        lock (s_gate)
        {
            if (!ReferenceEquals(s_owner, owner)) return;
            s_owner = null;
            remaining = [.. s_active, .. s_pending];
            s_pending.Clear();
        }
        s_active.Clear();

        Exception? first = null;
        foreach (var window in remaining)
        {
            try
            {
                window.Destroy();
            }
            catch (Exception ex)
            {
                first ??= ex;
            }
        }

        if (first != null)
            ExceptionDispatchInfo.Throw(first);
    }

    private static bool TryTakePending(out ISilkEventLoopWindow window)
    {
        lock (s_gate)
            return s_pending.TryDequeue(out window!);
    }
}
