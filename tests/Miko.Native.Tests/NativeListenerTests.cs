using Shouldly;

namespace Miko.Native.Tests;

/// <summary>
/// <see cref="INativeListener"/> 的退订语义：<c>RemoveAsync</c> 之后必须真正解除原生订阅
/// （<c>issues/feat-platform.md</c> 平台实现要求第 2 条），且重复调用幂等。
/// </summary>
public class NativeListenerTests
{
    [Fact]
    public async Task RemoveAsync_should_run_the_unsubscribe_delegate()
    {
        var removed = false;
        var listener = new DelegateNativeListener(() => removed = true);

        await listener.RemoveAsync();

        removed.ShouldBeTrue();
        listener.IsRemoved.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveAsync_should_be_idempotent()
    {
        // 页面 Dispose 与显式退订可能都会调用一次；第二次不该重复退原生订阅。
        var calls = 0;
        var listener = new DelegateNativeListener(() => calls++);

        await listener.RemoveAsync();
        await listener.RemoveAsync();

        calls.ShouldBe(1);
    }

    [Fact]
    public async Task Callbacks_should_stop_after_RemoveAsync()
    {
        // 模拟一个原生订阅源：退订后再「触发」不应再进入回调。
        var received = new List<int>();
        Action<int>? sink = received.Add;
        var listener = new DelegateNativeListener(() => sink = null);

        sink?.Invoke(1);
        await listener.RemoveAsync();
        sink?.Invoke(2);

        received.ShouldBe([1]);
    }

    [Fact]
    public async Task Async_unsubscribe_delegate_should_be_awaited()
    {
        var removed = false;
        var listener = new DelegateNativeListener(async () =>
        {
            await Task.Yield();
            removed = true;
        });

        await listener.RemoveAsync();

        removed.ShouldBeTrue();
    }

    [Fact]
    public void IsRemoved_should_be_false_before_removal()
    {
        new DelegateNativeListener(() => { }).IsRemoved.ShouldBeFalse();
    }
}
