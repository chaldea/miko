namespace Miko.Native;

/// <summary>
/// 原生订阅句柄。所有以回调/事件形式暴露的原生能力（位置监听、传感器、通知点击等）
/// 都通过它解除订阅。
/// <para>
/// 接口层以 C# <c>event</c> 暴露 <c>On...</c> 通知，底层原生订阅则统一实现为本接口：
/// 调用 <see cref="RemoveAsync"/> 后必须真正退掉原生回调，否则页面反复进出会重复订阅并泄漏
/// （见 <c>issues/feat-platform.md</c> 平台实现要求第 2 条）。
/// </para>
/// </summary>
public interface INativeListener
{
    /// <summary>解除该订阅。重复调用应当是幂等的。</summary>
    Task RemoveAsync();
}

/// <summary>
/// 以委托退订的 <see cref="INativeListener"/> 实现，供平台实现复用。
/// 退订委托只会被执行一次，重复 <see cref="RemoveAsync"/> 为空操作。
/// </summary>
public sealed class DelegateNativeListener : INativeListener
{
    private Func<Task>? _remove;

    public DelegateNativeListener(Func<Task> remove) => _remove = remove;

    public DelegateNativeListener(Action remove)
        : this(() => { remove(); return Task.CompletedTask; })
    {
    }

    /// <summary>该订阅是否已被解除。</summary>
    public bool IsRemoved => _remove is null;

    public Task RemoveAsync()
    {
        // Interlocked：原生回调可能在任意线程触发退订，保证退订委托只跑一次。
        var remove = Interlocked.Exchange(ref _remove, null);
        return remove?.Invoke() ?? Task.CompletedTask;
    }
}
