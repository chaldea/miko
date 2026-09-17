using Shouldly;

namespace Miko.Native.Tests;

/// <summary>
/// <see cref="NativeHostContext"/> 的延迟注入语义。这是整个 Native 层的接线关键：
/// 服务容器在平台宿主存在之前就已构建，服务只能在调用时才拿到宿主。
/// </summary>
public class NativeHostContextTests
{
    private sealed class FakeHost;

    private sealed class OtherHost;

    [Fact]
    public void Host_should_be_null_before_attach()
    {
        new NativeHostContext().Host.ShouldBeNull();
    }

    [Fact]
    public void Attach_should_publish_the_host()
    {
        var context = new NativeHostContext();
        var host = new FakeHost();

        context.Attach(host);

        context.Host.ShouldBeSameAs(host);
    }

    [Fact]
    public void Attach_should_raise_HostAttached_for_earlier_subscribers()
    {
        var context = new NativeHostContext();
        var host = new FakeHost();
        object? observed = null;

        context.HostAttached += h => observed = h;
        context.Attach(host);

        observed.ShouldBeSameAs(host);
    }

    [Fact]
    public void Subscribing_after_attach_should_fire_immediately()
    {
        // 宿主只 Attach 一次。晚订阅的服务若收不到通知就永远拿不到宿主——
        // 服务是懒解析的，几乎必然晚于宿主创建。
        var context = new NativeHostContext();
        var host = new FakeHost();
        context.Attach(host);

        object? observed = null;
        context.HostAttached += h => observed = h;

        observed.ShouldBeSameAs(host);
    }

    [Fact]
    public void Removed_handler_should_not_be_called()
    {
        var context = new NativeHostContext();
        var calls = 0;
        Action<object> handler = _ => calls++;

        context.HostAttached += handler;
        context.HostAttached -= handler;
        context.Attach(new FakeHost());

        calls.ShouldBe(0);
    }

    [Fact]
    public void Attach_should_reject_null()
    {
        Should.Throw<ArgumentNullException>(() => new NativeHostContext().Attach(null!));
    }

    [Fact]
    public void RequireHost_should_return_the_typed_host()
    {
        var context = new NativeHostContext();
        var host = new FakeHost();
        context.Attach(host);

        context.RequireHost<FakeHost>().ShouldBeSameAs(host);
    }

    [Fact]
    public void RequireHost_before_attach_should_throw_InvalidOperation()
    {
        // 不是 PlatformNotSupportedException：这表示平台包忘了 Attach（接线 bug），
        // 而非「本平台不支持该能力」。两者必须能区分开。
        var ex = Should.Throw<InvalidOperationException>(
            () => new NativeHostContext().RequireHost<FakeHost>());

        ex.Message.ShouldContain(nameof(INativeHostContext.Attach));
    }

    [Fact]
    public void RequireHost_with_mismatched_type_should_throw_InvalidOperation()
    {
        var context = new NativeHostContext();
        context.Attach(new OtherHost());

        var ex = Should.Throw<InvalidOperationException>(() => context.RequireHost<FakeHost>());

        ex.Message.ShouldContain(nameof(OtherHost));
        ex.Message.ShouldContain(nameof(FakeHost));
    }
}
