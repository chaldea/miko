using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Miko.Native.Motion;
using Miko.Native.Toast;
using Shouldly;

namespace Miko.Native.Tests;

/// <summary>
/// <c>Null*</c> 默认实现的失败语义：调用任何方法都抛 <see cref="PlatformNotSupportedException"/>，
/// 而不是静默返回假数据（<c>issues/feat-platform.md</c> 平台实现要求第 1 条）。
/// </summary>
public class NullNativeServiceTests
{
    /// <summary>
    /// 遍历每个能力接口的每个方法并反射调用，确认全部以
    /// <see cref="PlatformNotSupportedException"/> 失败。
    /// <para>
    /// 用反射而非逐个手写：接口共有百余个成员，手写必然漏测，而漏掉的成员一旦返回 <c>default</c>
    /// （例如 <c>null</c> 的 <c>DeviceInfo</c>）就是「静默返回假数据」——正是本测试要防的。
    /// </para>
    /// </summary>
    [Theory]
    [MemberData(nameof(NativeServiceRegistrationTests.AllServices), MemberType = typeof(NativeServiceRegistrationTests))]
    public async Task Every_method_should_throw_PlatformNotSupported(Type serviceType, Type implementationType)
    {
        // IMotionService 只有事件、没有方法，没有可抛的调用面。
        if (serviceType == typeof(IMotionService)) return;

        var provider = new ServiceCollection().AddMikoNative().BuildServiceProvider();
        var service = provider.GetRequiredService(serviceType);
        service.ShouldBeOfType(implementationType);

        var methods = serviceType.GetMethods()
            .Where(m => !m.IsSpecialName) // 排除 event 的 add_/remove_ 访问器
            .ToList();

        methods.ShouldNotBeEmpty();

        foreach (var method in methods)
        {
            var args = method.GetParameters().Select(CreateArgument).ToArray();

            // 返回失败 Task 而非同步 throw，因此异常在 await 时才浮现。
            var task = (Task)method.Invoke(service, args)!;

            var ex = await Should.ThrowAsync<PlatformNotSupportedException>(async () => await task);

            // 消息必须点名具体成员，否则调用方无从判断是哪项能力缺失。
            ex.Message.ShouldContain(method.Name);
            ex.Message.ShouldContain(implementationType.Name);
        }
    }

    /// <summary>
    /// 事件订阅/退订本身不得抛异常：订阅一个永不触发的事件是无害的，
    /// 页面不该因为「本平台没有该能力」而在初始化阶段崩溃。
    /// </summary>
    [Fact]
    public void Subscribing_to_events_on_null_services_should_not_throw()
    {
        var provider = new ServiceCollection().AddMikoNative().BuildServiceProvider();

        foreach (var (serviceType, _) in NativeServiceRegistrationTests.AllServices
                     .Select(row => ((Type)row[0], (Type)row[1])))
        {
            var service = provider.GetRequiredService(serviceType);

            foreach (var evt in serviceType.GetEvents())
            {
                var handler = CreateNoOpHandler(evt.EventHandlerType!);

                Should.NotThrow(() => evt.AddEventHandler(service, handler));
                Should.NotThrow(() => evt.RemoveEventHandler(service, handler));
            }
        }
    }

    [Fact]
    public void Null_motion_service_exposes_events_only()
    {
        // IMotionService 在上面的方法遍历里被跳过，这里单独固化「它确实没有方法」这一前提，
        // 以免将来给它加了方法却没人测到。
        typeof(IMotionService).GetMethods().Where(m => !m.IsSpecialName).ShouldBeEmpty();
        typeof(IMotionService).GetEvents().Length.ShouldBe(2);
    }

    [Fact]
    public async Task Null_service_should_keep_failing_on_repeated_calls()
    {
        var service = new NullToastService();
        var options = new ToastOptions { Text = "hi" };

        await Should.ThrowAsync<PlatformNotSupportedException>(() => service.ShowAsync(options));
        await Should.ThrowAsync<PlatformNotSupportedException>(() => service.ShowAsync(options));
    }

    /// <summary>为反射调用构造一个合法参数：必填成员需要真实实例，其余用默认值。</summary>
    private static object? CreateArgument(ParameterInfo parameter)
    {
        var type = parameter.ParameterType;

        // 可空引用/值类型与带默认值的可选参数直接给 null/默认值。
        if (!type.IsValueType) return CreateReferenceArgument(type);
        return Activator.CreateInstance(type);
    }

    private static object? CreateReferenceArgument(Type type)
    {
        if (type == typeof(string)) return string.Empty;
        if (type.IsArray) return Array.CreateInstance(type.GetElementType()!, 0);
        if (typeof(Delegate).IsAssignableFrom(type)) return CreateNoOpHandler(type);

        // record 选项类型：走无参或全默认构造；构造不出来就传 null——Null* 实现不读参数，
        // 它在读参数之前就已经抛了。
        try
        {
            var ctor = type.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault();
            if (ctor is null) return null;

            var args = ctor.GetParameters()
                .Select(p => p.ParameterType.IsValueType
                    ? Activator.CreateInstance(p.ParameterType)
                    : p.ParameterType == typeof(string) ? string.Empty : null)
                .ToArray();

            return ctor.Invoke(args);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>为任意委托类型生成一个什么都不做的处理器。</summary>
    private static Delegate CreateNoOpHandler(Type delegateType)
    {
        var invoke = delegateType.GetMethod("Invoke")!;
        var parameters = invoke.GetParameters()
            .Select(p => System.Linq.Expressions.Expression.Parameter(p.ParameterType))
            .ToArray();

        var body = System.Linq.Expressions.Expression.Empty();
        return System.Linq.Expressions.Expression.Lambda(delegateType, body, parameters).Compile();
    }
}
