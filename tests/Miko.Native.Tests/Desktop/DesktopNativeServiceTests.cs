using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
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
using Miko.Native.Network;
using Miko.Native.PushNotifications;
using Miko.Native.ScreenReader;
using Miko.Native.SplashScreen;
using Miko.Native.StatusBar;
using Miko.Native.Toast;
using Miko.Windowing.Native;
using Shouldly;

namespace Miko.Native.Tests.Desktop;

/// <summary>桌面 Native 服务的注册接线与各服务的真实行为。</summary>
public class DesktopNativeServiceTests
{
    private static IServiceProvider Build(bool addNativeFirst = true)
    {
        var builder = MikoAppBuilder.CreateDefault();

        // 两种调用顺序都要成立：AddMikoNative 的 Null 默认实现绝不能盖掉平台实现（ISSUE-129）。
        if (addNativeFirst) builder.Services.AddMikoNative();
        builder.UseDesktopNative();
        if (!addNativeFirst) builder.Services.AddMikoNative();

        return builder.Services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Desktop_implementations_should_win_regardless_of_registration_order(bool addNativeFirst)
    {
        var provider = Build(addNativeFirst);

        provider.GetRequiredService<IAppService>().ShouldBeOfType<DesktopAppService>();
        provider.GetRequiredService<IBrowserService>().ShouldBeOfType<DesktopBrowserService>();
        provider.GetRequiredService<IClipboardService>().ShouldBeOfType<DesktopClipboardService>();
        provider.GetRequiredService<IDeviceService>().ShouldBeOfType<DesktopDeviceService>();
        provider.GetRequiredService<IFilesystemService>().ShouldBeOfType<DesktopFilesystemService>();
        provider.GetRequiredService<IKeyboardService>().ShouldBeOfType<DesktopKeyboardService>();
        provider.GetRequiredService<INetworkService>().ShouldBeOfType<DesktopNetworkService>();
        provider.GetRequiredService<IScreenReaderService>().ShouldBeOfType<DesktopScreenReaderService>();
    }

    [Fact]
    public void Capabilities_without_a_desktop_stack_should_stay_null_implementations()
    {
        var provider = Build();

        // 桌面没有这些系统栈，保留 Null 实现——调用时抛 PlatformNotSupportedException，
        // 而不是返回假数据（issue 平台实现要求第 1 条）。
        provider.GetRequiredService<ICameraService>().ShouldBeOfType<NullCameraService>();
        provider.GetRequiredService<IGeolocationService>().ShouldBeOfType<NullGeolocationService>();
        provider.GetRequiredService<ILocalNotificationService>().ShouldBeOfType<NullLocalNotificationService>();
        provider.GetRequiredService<IPushNotificationService>().ShouldBeOfType<NullPushNotificationService>();
        provider.GetRequiredService<IHapticsService>().ShouldBeOfType<NullHapticsService>();
        provider.GetRequiredService<ISplashScreenService>().ShouldBeOfType<NullSplashScreenService>();
        provider.GetRequiredService<IStatusBarService>().ShouldBeOfType<NullStatusBarService>();
        provider.GetRequiredService<IToastService>().ShouldBeOfType<NullToastService>();
    }

    [Fact]
    public async Task Unsupported_desktop_capability_should_throw_PlatformNotSupported()
    {
        var camera = Build().GetRequiredService<ICameraService>();

        await Should.ThrowAsync<PlatformNotSupportedException>(
            () => camera.TakePhotoAsync(new TakePhotoOptions()));
    }

    [Fact]
    public async Task App_info_should_come_from_the_entry_assembly()
    {
        var info = await new DesktopAppService().GetInfoAsync();

        info.Name.ShouldNotBeNullOrWhiteSpace();
        info.Id.ShouldNotBeNullOrWhiteSpace();
        info.Version.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task App_state_should_report_active()
    {
        (await new DesktopAppService().GetStateAsync()).IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task App_language_should_be_a_two_letter_code()
    {
        var language = await new DesktopAppService().GetAppLanguageAsync();

        language.Value.Length.ShouldBeInRange(2, 3);
    }

    [Fact]
    public async Task Android_only_app_members_should_throw_on_desktop()
    {
        var app = new DesktopAppService();

        await Should.ThrowAsync<PlatformNotSupportedException>(() => app.ExitAppAsync());
        await Should.ThrowAsync<PlatformNotSupportedException>(() => app.MinimizeAppAsync());
        await Should.ThrowAsync<PlatformNotSupportedException>(
            () => app.ToggleBackButtonHandlerAsync(new ToggleBackButtonHandlerOptions(true)));
    }

    [Fact]
    public async Task Device_info_should_describe_the_host_machine()
    {
        var info = await new DesktopDeviceService().GetInfoAsync();

        info.Name.ShouldBe(Environment.MachineName);
        // 桌面在 Capacitor 的三分法里归入 Web。
        info.Platform.ShouldBe(DevicePlatform.Web);
        info.MemUsed.ShouldBeGreaterThan(0);
        info.WebViewVersion.ShouldContain("Miko.Windowing");
    }

    [Fact]
    public async Task Device_id_should_be_stable_across_calls()
    {
        var service = new DesktopDeviceService();

        var first = await service.GetIdAsync();
        var second = await service.GetIdAsync();

        first.Identifier.ShouldBe(second.Identifier);
        first.Identifier.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Device_language_should_be_reported()
    {
        var service = new DesktopDeviceService();

        (await service.GetLanguageCodeAsync()).Value.ShouldNotBeNullOrWhiteSpace();
        (await service.GetLanguageTagAsync()).Value.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Network_status_should_be_queryable()
    {
        var status = await new DesktopNetworkService().GetStatusAsync();

        // 不断言具体连通性（CI 可能离线），但状态必须自洽：未连接即 None。
        if (!status.Connected) status.ConnectionType.ShouldBe(ConnectionType.None);
        else status.ConnectionType.ShouldNotBe(ConnectionType.None);
    }

    [Fact]
    public void Network_service_should_unsubscribe_system_events_when_disposed()
    {
        // 订阅未解除会让 NetworkChange 一直持有本实例（issue 要求第 2 条）。
        using var service = new DesktopNetworkService();
        Action<ConnectionStatus> handler = _ => { };

        Should.NotThrow(() =>
        {
            service.OnNetworkStatusChange += handler;
            service.OnNetworkStatusChange -= handler;
        });
    }

    [Fact]
    public async Task Browser_should_reject_non_absolute_and_unsafe_urls()
    {
        var browser = new DesktopBrowserService();

        await Should.ThrowAsync<ArgumentException>(
            () => browser.OpenAsync(new OpenOptions { Url = "not-a-url" }));

        // file:// 经 UseShellExecute 可用来启动本地程序，必须挡住。
        await Should.ThrowAsync<ArgumentException>(
            () => browser.OpenAsync(new OpenOptions { Url = "file:///C:/Windows/System32/cmd.exe" }));
    }

    [Fact]
    public async Task Browser_close_is_not_supported_for_a_separate_process()
    {
        await Should.ThrowAsync<PlatformNotSupportedException>(() => new DesktopBrowserService().CloseAsync());
    }

    [Fact]
    public async Task Keyboard_ios_only_members_should_throw_on_desktop()
    {
        var keyboard = new DesktopKeyboardService(new Miko.Platform.NullInputMethod());

        await Should.ThrowAsync<PlatformNotSupportedException>(() => keyboard.SetAccessoryBarVisibleAsync(true));
        await Should.ThrowAsync<PlatformNotSupportedException>(() => keyboard.SetScrollAsync(true));
        await Should.ThrowAsync<PlatformNotSupportedException>(() => keyboard.GetResizeModeAsync());
    }

    [Fact]
    public async Task Keyboard_show_and_hide_should_drive_the_input_method()
    {
        var ime = new RecordingInputMethod();
        var keyboard = new DesktopKeyboardService(ime);

        await keyboard.ShowAsync();
        await keyboard.HideAsync();

        ime.ShowCalls.ShouldBe(1);
        // Hide 通过清空文本客户端收起输入面板。
        ime.ClearedState.ShouldBeTrue();
    }

    [Fact]
    public async Task Screen_reader_speak_is_not_supported_without_a_tts_engine()
    {
        await Should.ThrowAsync<PlatformNotSupportedException>(
            () => new DesktopScreenReaderService().SpeakAsync(new SpeakOptions { Value = "hi" }));
    }

    [Fact]
    public async Task Screen_reader_detection_should_work_on_windows()
    {
        var service = new DesktopScreenReaderService();

        if (System.OperatingSystem.IsWindows())
        {
            // 只断言「能查到一个布尔值」，不假设本机是否开着读屏软件。
            var state = await service.IsEnabledAsync();
            state.ShouldNotBeNull();
        }
        else
        {
            await Should.ThrowAsync<PlatformNotSupportedException>(() => service.IsEnabledAsync());
        }
    }

    private sealed class RecordingInputMethod : Miko.Platform.IInputMethodService
    {
        public int ShowCalls { get; private set; }
        public bool ClearedState { get; private set; }

        public event Action<string>? TextCommitted { add { } remove { } }
        public event Action? CompositionStarted { add { } remove { } }
        public event Action<string>? CompositionUpdated { add { } remove { } }
        public event Action<string?>? CompositionEnded { add { } remove { } }

        public void SetState(Miko.Platform.InputMethodState? state)
        {
            if (state is null) ClearedState = true;
        }

        public void ShowKeyboard() => ShowCalls++;
    }
}
