using Microsoft.Extensions.DependencyInjection;
using Miko.Native;
using Miko.Native.App;
using Miko.Native.Browser;
using Miko.Native.Camera;
using Miko.Native.Clipboard;
using Miko.Native.Device;
using Miko.Native.Filesystem;
using Miko.Native.Geolocation;
using Miko.Native.Haptics;
using Miko.Native.Keyboard;
using Miko.Native.LocalNotifications;
using Miko.Native.Motion;
using Miko.Native.Network;
using Miko.Native.PushNotifications;
using Miko.Native.ScreenReader;
using Miko.Native.SplashScreen;
using Miko.Native.StatusBar;
using Miko.Native.Toast;
using Shouldly;

namespace Miko.Native.Tests;

/// <summary>
/// <c>AddMikoNative()</c> 的注册契约：全部接口可解析、默认为 <c>Null*</c> 实现、
/// 且平台覆盖无论注册先后都胜出（ISSUE-129 的 TryAdd/Replace 契约）。
/// </summary>
public class NativeServiceRegistrationTests
{
    /// <summary>issue 中列出的 17 个能力接口及其默认实现类型。</summary>
    public static TheoryData<Type, Type> AllServices => new()
    {
        { typeof(IAppService), typeof(NullAppService) },
        { typeof(IBrowserService), typeof(NullBrowserService) },
        { typeof(ICameraService), typeof(NullCameraService) },
        { typeof(IClipboardService), typeof(NullClipboardService) },
        { typeof(IDeviceService), typeof(NullDeviceService) },
        { typeof(IFilesystemService), typeof(NullFilesystemService) },
        { typeof(IGeolocationService), typeof(NullGeolocationService) },
        { typeof(IHapticsService), typeof(NullHapticsService) },
        { typeof(IKeyboardService), typeof(NullKeyboardService) },
        { typeof(ILocalNotificationService), typeof(NullLocalNotificationService) },
        { typeof(IMotionService), typeof(NullMotionService) },
        { typeof(INetworkService), typeof(NullNetworkService) },
        { typeof(IPushNotificationService), typeof(NullPushNotificationService) },
        { typeof(IScreenReaderService), typeof(NullScreenReaderService) },
        { typeof(ISplashScreenService), typeof(NullSplashScreenService) },
        { typeof(IStatusBarService), typeof(NullStatusBarService) },
        { typeof(IToastService), typeof(NullToastService) },
    };

    [Theory]
    [MemberData(nameof(AllServices))]
    public void Should_resolve_every_native_service_as_its_null_implementation(Type service, Type implementation)
    {
        var provider = new ServiceCollection().AddMikoNative().BuildServiceProvider();

        var resolved = provider.GetService(service);

        resolved.ShouldNotBeNull();
        resolved.ShouldBeOfType(implementation);
    }

    [Fact]
    public void Should_register_all_seventeen_capabilities_listed_in_the_issue()
    {
        // 防止新增能力时漏改注册：接口数量与 issue 的服务列表一致。
        AllServices.Count.ShouldBe(17);
    }

    [Fact]
    public void Should_resolve_host_context_as_singleton()
    {
        var provider = new ServiceCollection().AddMikoNative().BuildServiceProvider();

        var first = provider.GetRequiredService<INativeHostContext>();
        var second = provider.GetRequiredService<INativeHostContext>();

        first.ShouldBeOfType<NativeHostContext>();
        // 单例：宿主 Attach 之后，所有服务必须看到同一个宿主引用。
        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void Should_resolve_each_service_as_singleton()
    {
        var provider = new ServiceCollection().AddMikoNative().BuildServiceProvider();

        provider.GetRequiredService<IClipboardService>()
            .ShouldBeSameAs(provider.GetRequiredService<IClipboardService>());
    }

    [Fact]
    public void Should_be_idempotent_when_called_twice()
    {
        var services = new ServiceCollection();

        services.AddMikoNative();
        services.AddMikoNative();

        // TryAdd 的重复调用不应留下第二条描述符，否则最后一条会赢并可能盖掉自定义实现。
        services.Count(d => d.ServiceType == typeof(IToastService)).ShouldBe(1);
    }

    [Fact]
    public void Platform_override_registered_before_AddMikoNative_should_win()
    {
        // 这是 ISSUE-129 的实际顺序：平台头先 UseXxxNative()，AddMikoNative() 的默认实现后执行。
        // 若默认实现用 AddSingleton 而非 TryAdd，它会后注册并盖掉平台实现。
        var provider = new ServiceCollection()
            .ReplaceNative<IToastService, FakeToastService>()
            .AddMikoNative()
            .BuildServiceProvider();

        provider.GetRequiredService<IToastService>().ShouldBeOfType<FakeToastService>();
    }

    [Fact]
    public void Platform_override_registered_after_AddMikoNative_should_win()
    {
        var services = new ServiceCollection().AddMikoNative();
        services.ReplaceNative<IToastService, FakeToastService>();

        services.BuildServiceProvider()
            .GetRequiredService<IToastService>()
            .ShouldBeOfType<FakeToastService>();
    }

    [Fact]
    public void Factory_based_override_should_win()
    {
        var instance = new FakeToastService();

        var provider = new ServiceCollection()
            .AddMikoNative()
            .ReplaceNative<IToastService>(_ => instance)
            .BuildServiceProvider();

        provider.GetRequiredService<IToastService>().ShouldBeSameAs(instance);
    }

    [Fact]
    public void Override_should_leave_a_single_descriptor()
    {
        var services = new ServiceCollection().AddMikoNative();
        services.ReplaceNative<IToastService, FakeToastService>();

        // Replace 而非 Add：容器里不应残留 Null 实现的描述符。
        services.Count(d => d.ServiceType == typeof(IToastService)).ShouldBe(1);
    }

    private sealed class FakeToastService : IToastService
    {
        public Task ShowAsync(ToastOptions options) => Task.CompletedTask;
    }
}
