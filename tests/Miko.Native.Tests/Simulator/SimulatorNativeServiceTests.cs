using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Native.Camera;
using Miko.Native.Device;
using Miko.Native.Geolocation;
using Miko.Native.Haptics;
using Miko.Native.LocalNotifications;
using Miko.Native.Motion;
using Miko.Native.PushNotifications;
using Miko.Native.ScreenReader;
using Miko.Native.SplashScreen;
using Miko.Native.StatusBar;
using Miko.Native.Toast;
using Miko.Platform;
using Miko.Simulator;
using Miko.Simulator.Native;
using Miko.Windowing.Native;
using Shouldly;

namespace Miko.Native.Tests.Simulator;

/// <summary>
/// 模拟器 Native 能力：注册接线、随设备切换的设备信息，以及各仿真服务的行为。
/// <para>
/// 模拟器的仿真实现**不抛** <see cref="PlatformNotSupportedException"/>——它的定位是让开发者
/// 在桌面上跑通完整调用链。本组测试固化这一点。
/// </para>
/// </summary>
public class SimulatorNativeServiceTests
{
    /// <summary>可控的设备来源，代替真实的 <see cref="SimulatorHost"/>。</summary>
    private sealed class FakeDeviceSource : ISimulatedDeviceSource
    {
        public DeviceProfile CurrentDevice { get; set; } = DeviceProfile.IPhone15Pro;
    }

    private static (INativeHostContext Host, FakeDeviceSource Source) AttachedHost()
    {
        var host = new NativeHostContext();
        var source = new FakeDeviceSource();
        host.Attach(source);
        return (host, source);
    }

    private static IServiceProvider Build()
        => MikoAppBuilder.CreateDefault().UseSimulatorNative().Services.BuildServiceProvider();

    [Fact]
    public void Simulator_should_simulate_mobile_only_capabilities()
    {
        var provider = Build();

        provider.GetRequiredService<IDeviceService>().ShouldBeOfType<SimulatorDeviceService>();
        provider.GetRequiredService<ICameraService>().ShouldBeOfType<SimulatorCameraService>();
        provider.GetRequiredService<IGeolocationService>().ShouldBeOfType<SimulatorGeolocationService>();
        provider.GetRequiredService<IHapticsService>().ShouldBeOfType<SimulatorHapticsService>();
        provider.GetRequiredService<ILocalNotificationService>().ShouldBeOfType<SimulatorLocalNotificationService>();
        provider.GetRequiredService<IMotionService>().ShouldBeOfType<SimulatorMotionService>();
        provider.GetRequiredService<IPushNotificationService>().ShouldBeOfType<SimulatorPushNotificationService>();
        provider.GetRequiredService<IScreenReaderService>().ShouldBeOfType<SimulatorScreenReaderService>();
        provider.GetRequiredService<ISplashScreenService>().ShouldBeOfType<SimulatorSplashScreenService>();
        provider.GetRequiredService<IStatusBarService>().ShouldBeOfType<SimulatorStatusBarService>();
        provider.GetRequiredService<IToastService>().ShouldBeOfType<SimulatorToastService>();
    }

    [Fact]
    public void Simulator_should_reuse_desktop_implementations_where_they_are_real()
    {
        var provider = Build();

        // 模拟器本来就跑在桌面上，这些能力在宿主机上是真的，仿真反而不如真实。
        provider.GetRequiredService<Miko.Native.Filesystem.IFilesystemService>()
            .ShouldBeOfType<DesktopFilesystemService>();
        provider.GetRequiredService<Miko.Native.Network.INetworkService>()
            .ShouldBeOfType<DesktopNetworkService>();
        provider.GetRequiredService<Miko.Native.Browser.IBrowserService>()
            .ShouldBeOfType<DesktopBrowserService>();
        provider.GetRequiredService<Miko.Native.App.IAppService>()
            .ShouldBeOfType<DesktopAppService>();
    }

    [Fact]
    public async Task Device_info_should_follow_the_selected_device()
    {
        var (host, source) = AttachedHost();
        var service = new SimulatorDeviceService(host, logger: null);

        source.CurrentDevice = DeviceProfile.IPhone15Pro;
        var ios = await service.GetInfoAsync();

        source.CurrentDevice = DeviceProfile.Generic1x; // Android 预设
        var android = await service.GetInfoAsync();

        // 换设备后 IDeviceService 的返回值必须跟着变——服务读的是「当前」设备，
        // 不能在构造时把设备拷走。
        ios.Platform.ShouldBe(DevicePlatform.Ios);
        ios.OperatingSystem.ShouldBe(Miko.Native.Device.OperatingSystem.Ios);
        ios.Name.ShouldBe(DeviceProfile.IPhone15Pro.Name);

        android.Platform.ShouldBe(DevicePlatform.Android);
        android.OperatingSystem.ShouldBe(Miko.Native.Device.OperatingSystem.Android);
        android.Name.ShouldBe(DeviceProfile.Generic1x.Name);
    }

    [Fact]
    public async Task Simulated_device_should_report_itself_as_virtual()
    {
        var (host, _) = AttachedHost();

        (await new SimulatorDeviceService(host, null).GetInfoAsync()).IsVirtual.ShouldBeTrue();
    }

    [Fact]
    public async Task Device_id_should_change_with_the_device()
    {
        var (host, source) = AttachedHost();
        var service = new SimulatorDeviceService(host, null);

        source.CurrentDevice = DeviceProfile.IPhone15Pro;
        var first = await service.GetIdAsync();

        source.CurrentDevice = DeviceProfile.IPhoneSE;
        var second = await service.GetIdAsync();

        first.Identifier.ShouldNotBe(second.Identifier);
        first.Identifier.ShouldStartWith("miko-simulator-");
    }

    [Fact]
    public async Task Device_service_should_work_before_a_host_is_attached()
    {
        // 服务可能在首帧之前就被调用；那时报错没有意义（模拟器本就不该失败）。
        var info = await new SimulatorDeviceService(new NativeHostContext(), null).GetInfoAsync();

        info.ShouldNotBeNull();
        info.IsVirtual.ShouldBeTrue();
    }

    [Fact]
    public async Task Camera_should_return_identifiable_sample_media()
    {
        var (host, _) = AttachedHost();
        var camera = new SimulatorCameraService(host, null);

        var photo = await camera.TakePhotoAsync(new TakePhotoOptions { IncludeMetadata = true });

        photo.Type.ShouldBe(MediaType.Photo);
        // 仿真数据必须可辨识，不会被误当成真实文件。
        photo.Uri.ShouldStartWith("miko-simulator://");
        photo.Thumbnail.ShouldNotBeNullOrWhiteSpace();
        photo.Metadata.ShouldNotBeNull();
    }

    [Fact]
    public async Task Camera_metadata_should_be_omitted_unless_requested()
    {
        var (host, _) = AttachedHost();

        var photo = await new SimulatorCameraService(host, null)
            .TakePhotoAsync(new TakePhotoOptions { IncludeMetadata = false });

        photo.Metadata.ShouldBeNull();
    }

    [Fact]
    public async Task Gallery_multi_selection_should_return_multiple_results()
    {
        var (host, _) = AttachedHost();
        var camera = new SimulatorCameraService(host, null);

        var single = await camera.ChooseFromGalleryAsync(new ChooseFromGalleryOptions());
        var multi = await camera.ChooseFromGalleryAsync(new ChooseFromGalleryOptions
        {
            AllowMultipleSelection = true,
            Limit = 3,
        });

        single.Results.Count.ShouldBe(1);
        // 多选路径也要能被走到，否则应用的多选渲染在桌面上无从验证。
        multi.Results.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Camera_permissions_should_be_granted_in_the_simulator()
    {
        var (host, _) = AttachedHost();

        var status = await new SimulatorCameraService(host, null).CheckPermissionsAsync();

        // 否则开发者在桌面上永远走不到成功分支。
        status.Camera.ShouldBe(PermissionState.Granted);
        status.Photos.ShouldBe(PermissionState.Granted);
    }

    [Fact]
    public async Task Geolocation_should_return_a_sample_position()
    {
        var (host, _) = AttachedHost();

        var position = await new SimulatorGeolocationService(host, null).GetCurrentPositionAsync();

        position.Coords.Latitude.ShouldBe(31.2304, 0.0001);
        position.Coords.Longitude.ShouldBe(121.4737, 0.0001);
        position.Timestamp.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Watching_position_should_push_updates_and_stop_after_clear()
    {
        var (host, _) = AttachedHost();
        var service = new SimulatorGeolocationService(host, null);
        var received = new List<Position>();
        var gate = new Lock();

        var watchId = await service.WatchPositionAsync(
            new PositionOptions { Interval = 200 },
            (position, _) => { if (position is not null) lock (gate) received.Add(position); });

        // 首次立即回调 + 至少一次周期推送。
        await WaitUntil(() => { lock (gate) return received.Count >= 2; }, TimeSpan.FromSeconds(5));

        await service.ClearWatchAsync(watchId);

        int countAfterClear;
        lock (gate) countAfterClear = received.Count;

        await Task.Delay(600);

        // 退订后不得再有回调（issue 要求第 2 条）。
        lock (gate) received.Count.ShouldBe(countAfterClear);
    }

    [Fact]
    public async Task Clearing_a_watch_should_never_report_an_error()
    {
        // 退订是正常收尾，不是定位故障：取消推送循环产生的任何异常都**不能**经用户回调
        // 报成「定位失败」——页面进出时那会变成莫名其妙的错误提示。
        // 反复快速建/撤以覆盖取消恰好落在推送与等待之间的各种时序。
        var (host, _) = AttachedHost();
        var errors = new List<Exception>();
        var gate = new Lock();

        for (var i = 0; i < 20; i++)
        {
            var service = new SimulatorGeolocationService(host, null);

            var watchId = await service.WatchPositionAsync(
                new PositionOptions { Interval = 60_000 },
                (_, error) => { if (error is not null) lock (gate) errors.Add(error); });

            // 让首次立即回调跑完，循环随即进入等待。
            await Task.Delay(20);
            await service.ClearWatchAsync(watchId);
        }

        // 给所有已取消的循环留出跑到收尾分支的时间。
        await Task.Delay(200);

        lock (gate) errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Clearing_an_unknown_watch_should_be_a_no_op()
    {
        var (host, _) = AttachedHost();

        await Should.NotThrowAsync(() => new SimulatorGeolocationService(host, null).ClearWatchAsync("nope"));
    }

    [Fact]
    public async Task Local_notifications_should_be_tracked_in_memory()
    {
        var (host, _) = AttachedHost();
        var service = new SimulatorLocalNotificationService(host, null);

        await service.ScheduleAsync(new ScheduleOptions(
        [
            new LocalNotification { Id = 1, Title = "a" },
            new LocalNotification { Id = 2, Title = "b" },
        ]));

        (await service.GetPendingAsync()).Notifications.Select(n => n.Id).OrderBy(i => i).ShouldBe([1, 2]);

        await service.CancelAsync(new CancelOptions([new LocalNotificationDescriptor(1)]));

        // 状态是真的：调度后查得到、取消后查不到。
        (await service.GetPendingAsync()).Notifications.Select(n => n.Id).ShouldBe([2]);

        await service.CancelAllAsync();
        (await service.GetPendingAsync()).Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task Updating_a_notification_should_replace_it_by_id()
    {
        var (host, _) = AttachedHost();
        var service = new SimulatorLocalNotificationService(host, null);

        await service.ScheduleAsync(new ScheduleOptions([new LocalNotification { Id = 7, Title = "old" }]));
        await service.UpdateAsync(new ScheduleOptions([new LocalNotification { Id = 7, Title = "new" }]));

        var found = await service.GetByIdsAsync([7]);

        found.Notifications.Count.ShouldBe(1);
        found.Notifications[0].Title.ShouldBe("new");
    }

    [Fact]
    public async Task Notification_channels_should_round_trip()
    {
        var (host, _) = AttachedHost();
        var service = new SimulatorLocalNotificationService(host, null);

        await service.CreateChannelAsync(new NotificationChannel { Id = "chat", Name = "Chat" });
        (await service.ListChannelsAsync()).Channels.Select(c => c.Id).ShouldBe(["chat"]);

        await service.DeleteChannelAsync("chat");
        (await service.ListChannelsAsync()).Channels.ShouldBeEmpty();
    }

    [Fact]
    public async Task Push_registration_should_deliver_a_simulated_token()
    {
        var (host, _) = AttachedHost();
        var service = new SimulatorPushNotificationService(host, null);
        PushToken? token = null;

        service.OnRegistration += t => token = t;
        await service.RegisterAsync();

        // 与真机一致：token 经事件返回，而不是 RegisterAsync 的返回值。
        token.ShouldNotBeNull();
        token.Value.ShouldStartWith("miko-simulator-token-");
    }

    [Fact]
    public async Task Status_bar_should_report_the_state_it_was_given()
    {
        var (host, source) = AttachedHost();
        source.CurrentDevice = DeviceProfile.IPhone15Pro;
        var service = new SimulatorStatusBarService(host, null);

        await service.SetStyleAsync(new StatusBarStyleOptions { Style = StatusBarStyle.Light });
        await service.HideAsync();

        var info = await service.GetInfoAsync();

        info.Style.ShouldBe(StatusBarStyle.Light);
        info.Visible.ShouldBeFalse();
        // 高度取当前设备的顶部安全区——模拟器就是用它画刘海/状态栏的。
        info.Height.ShouldBe(DeviceProfile.IPhone15Pro.SafeArea.Top);
    }

    [Fact]
    public async Task Status_bar_visibility_change_should_raise_the_event_once()
    {
        var (host, _) = AttachedHost();
        var service = new SimulatorStatusBarService(host, null);
        var events = new List<StatusBarInfo>();
        service.OnVisibilityChanged += events.Add;

        await service.HideAsync();
        await service.HideAsync(); // 状态未变，不应重复触发

        events.Count.ShouldBe(1);
        events[0].Visible.ShouldBeFalse();
    }

    [Fact]
    public async Task Simulated_feedback_services_should_not_throw()
    {
        var (host, _) = AttachedHost();

        // 模拟器的定位是「跑通调用链」，这些调用绝不能抛 PlatformNotSupportedException。
        await Should.NotThrowAsync(() => new SimulatorHapticsService(host, null).ImpactAsync());
        await Should.NotThrowAsync(() => new SimulatorHapticsService(host, null).VibrateAsync());
        await Should.NotThrowAsync(
            () => new SimulatorToastService(host, null).ShowAsync(new ToastOptions { Text = "hi" }));
        await Should.NotThrowAsync(() => new SimulatorSplashScreenService(host, null).HideAsync());
        await Should.NotThrowAsync(
            () => new SimulatorScreenReaderService(host, null).SpeakAsync(new SpeakOptions { Value = "hi" }));
    }

    [Fact]
    public async Task Screen_reader_should_report_disabled_in_the_simulator()
    {
        var (host, _) = AttachedHost();

        (await new SimulatorScreenReaderService(host, null).IsEnabledAsync()).Value.ShouldBeFalse();
    }

    [Fact]
    public void Motion_events_should_stay_silent_on_the_desktop()
    {
        // 编造抖动数据会让「摇一摇」之类的逻辑在桌面上被误触发。
        var service = new SimulatorMotionService();
        var fired = false;

        service.OnAccel += _ => fired = true;
        Thread.Sleep(50);

        fired.ShouldBeFalse();
    }

    private static async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return;
            await Task.Delay(25);
        }

        throw new TimeoutException("Condition was not met within the timeout.");
    }
}
