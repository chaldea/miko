using Microsoft.Extensions.Logging;
using Miko.Native;
using Miko.Native.LocalNotifications;
using Miko.Native.PushNotifications;

namespace Miko.Simulator.Native;

/// <summary>
/// 模拟器本地通知：在内存里维护一份通知表，调度、查询、取消、渠道全部如实记账。
/// <para>
/// 不真正弹系统通知，但**状态是真的**——调度后查得到、取消后查不到，
/// 因此应用管理通知的逻辑（去重、更新、清理）可以在桌面上完整验证。
/// </para>
/// </summary>
internal sealed class SimulatorLocalNotificationService : SimulatorNativeServiceBase, ILocalNotificationService
{
    private readonly Lock _gate = new();
    private readonly Dictionary<int, LocalNotification> _pending = new();
    private readonly Dictionary<int, DeliveredNotification> _delivered = new();
    private readonly Dictionary<string, NotificationChannel> _channels = new();
    private readonly List<ActionType> _actionTypes = new();

    public SimulatorLocalNotificationService(
        INativeHostContext hostContext,
        ILogger<SimulatorLocalNotificationService>? logger)
        : base(hostContext, logger) { }

    public Task<ScheduleResult> ScheduleAsync(ScheduleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        lock (_gate)
        {
            foreach (var notification in options.Notifications)
                _pending[notification.Id] = notification;
        }

        Log(nameof(ScheduleAsync), string.Join(",", options.Notifications.Select(n => n.Id)));

        return Task.FromResult(new ScheduleResult(
            options.Notifications.Select(n => new LocalNotificationDescriptor(n.Id)).ToList()));
    }

    /// <summary>更新与调度同语义（按 Id 覆盖），与 Capacitor 一致。</summary>
    public Task<ScheduleResult> UpdateAsync(ScheduleOptions options) => ScheduleAsync(options);

    public Task<PendingResult> GetPendingAsync()
    {
        lock (_gate)
            return Task.FromResult(new PendingResult(
                _pending.Keys.Select(id => new LocalNotificationDescriptor(id)).ToList()));
    }

    public Task RegisterActionTypesAsync(RegisterActionTypesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        lock (_gate) _actionTypes.AddRange(options.Types);
        Log(nameof(RegisterActionTypesAsync), string.Join(",", options.Types.Select(t => t.Id)));
        return Task.CompletedTask;
    }

    public Task CancelAsync(CancelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        lock (_gate)
        {
            foreach (var descriptor in options.Notifications) _pending.Remove(descriptor.Id);
        }

        Log(nameof(CancelAsync), string.Join(",", options.Notifications.Select(n => n.Id)));
        return Task.CompletedTask;
    }

    public Task CancelAllAsync()
    {
        lock (_gate) _pending.Clear();
        Log(nameof(CancelAllAsync));
        return Task.CompletedTask;
    }

    public Task<EnabledResult> AreEnabledAsync() => Task.FromResult(new EnabledResult(true));

    public Task<DeliveredNotifications> GetDeliveredNotificationsAsync()
    {
        lock (_gate) return Task.FromResult(new DeliveredNotifications(_delivered.Values.ToList()));
    }

    public Task RemoveDeliveredNotificationsAsync(DeliveredNotifications notifications)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        lock (_gate)
        {
            foreach (var n in notifications.Notifications) _delivered.Remove(n.Id);
        }

        return Task.CompletedTask;
    }

    public Task RemoveDeliveredNotificationsByIdAsync(int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        lock (_gate)
        {
            foreach (var id in ids) _delivered.Remove(id);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAllDeliveredNotificationsAsync()
    {
        lock (_gate) _delivered.Clear();
        return Task.CompletedTask;
    }

    public Task<GetNotificationsResult> GetByIdsAsync(int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        lock (_gate)
            return Task.FromResult(new GetNotificationsResult(
                ids.Where(_pending.ContainsKey).Select(id => _pending[id]).ToList()));
    }

    public Task<GetNotificationsResult> GetAllAsync(NotificationState? state = null)
    {
        lock (_gate)
        {
            // 模拟器不推进时间，所有通知都停留在 Scheduled；查询 Triggered 得到空集。
            var items = state == NotificationState.Triggered
                ? new List<LocalNotification>()
                : _pending.Values.ToList();

            return Task.FromResult(new GetNotificationsResult(items));
        }
    }

    public Task CreateChannelAsync(NotificationChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        lock (_gate) _channels[channel.Id] = channel;
        Log(nameof(CreateChannelAsync), channel.Id);
        return Task.CompletedTask;
    }

    public Task DeleteChannelAsync(string id)
    {
        lock (_gate) _channels.Remove(id);
        return Task.CompletedTask;
    }

    public Task<ListChannelsResult> ListChannelsAsync()
    {
        lock (_gate) return Task.FromResult(new ListChannelsResult(_channels.Values.ToList()));
    }

    public Task<NotificationPermissionStatus> CheckPermissionsAsync()
        => Task.FromResult(new NotificationPermissionStatus(PermissionState.Granted));

    public Task<NotificationPermissionStatus> RequestPermissionsAsync()
        => Task.FromResult(new NotificationPermissionStatus(PermissionState.Granted));

    public Task<ExactAlarmPermissionStatus> ChangeExactNotificationSettingAsync()
        => Task.FromResult(new ExactAlarmPermissionStatus(PermissionState.Granted));

    public Task<ExactAlarmPermissionStatus> CheckExactNotificationSettingAsync()
        => Task.FromResult(new ExactAlarmPermissionStatus(PermissionState.Granted));

    /// <summary>模拟器不推进调度时间，通知不会自行触发。</summary>
    public event Action<LocalNotification>? OnLocalNotificationReceived { add { } remove { } }
    public event Action<LocalNotificationActionPerformed>? OnLocalNotificationActionPerformed { add { } remove { } }
}

/// <summary>
/// 模拟器推送：没有 APNS/FCM 连接，但注册会回一个可辨识的仿真 token，
/// 让「注册 → 拿 token → 上报后端」这条链路在桌面上可跑通。
/// </summary>
internal sealed class SimulatorPushNotificationService : SimulatorNativeServiceBase, IPushNotificationService
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, PushNotificationChannel> _channels = new();
    private Action<PushToken>? _registration;

    public SimulatorPushNotificationService(
        INativeHostContext hostContext,
        ILogger<SimulatorPushNotificationService>? logger)
        : base(hostContext, logger) { }

    public Task RegisterAsync()
    {
        Log(nameof(RegisterAsync));

        Action<PushToken>? handlers;
        lock (_gate) handlers = _registration;

        // 与真机一致：注册结果通过事件异步返回，而不是从 RegisterAsync 直接返回。
        handlers?.Invoke(new PushToken($"miko-simulator-token-{Guid.NewGuid():n}"));
        return Task.CompletedTask;
    }

    public Task UnregisterAsync()
    {
        Log(nameof(UnregisterAsync));
        return Task.CompletedTask;
    }

    public Task<DeliveredPushNotifications> GetDeliveredNotificationsAsync()
        => Task.FromResult(new DeliveredPushNotifications([]));

    public Task RemoveDeliveredNotificationsAsync(DeliveredPushNotifications notifications) => Task.CompletedTask;

    public Task RemoveAllDeliveredNotificationsAsync() => Task.CompletedTask;

    public Task CreateChannelAsync(PushNotificationChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        lock (_gate) _channels[channel.Id] = channel;
        return Task.CompletedTask;
    }

    public Task DeleteChannelAsync(string id)
    {
        lock (_gate) _channels.Remove(id);
        return Task.CompletedTask;
    }

    public Task<PushChannelList> ListChannelsAsync()
    {
        lock (_gate) return Task.FromResult(new PushChannelList(_channels.Values.ToList()));
    }

    public Task<PushPermissionStatus> CheckPermissionsAsync()
        => Task.FromResult(new PushPermissionStatus(PermissionState.Granted));

    public Task<PushPermissionStatus> RequestPermissionsAsync()
        => Task.FromResult(new PushPermissionStatus(PermissionState.Granted));

    public event Action<PushToken>? OnRegistration
    {
        add { if (value is not null) lock (_gate) _registration += value; }
        remove { if (value is not null) lock (_gate) _registration -= value; }
    }

    /// <summary>模拟器的注册永远成功，也收不到真实推送。</summary>
    public event Action<RegistrationError>? OnRegistrationError { add { } remove { } }
    public event Action<PushNotification>? OnPushNotificationReceived { add { } remove { } }
    public event Action<PushAction>? OnPushNotificationActionPerformed { add { } remove { } }
}
