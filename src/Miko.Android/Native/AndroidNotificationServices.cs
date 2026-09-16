using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Miko.Native;
using Miko.Native.LocalNotifications;
using Miko.Native.PushNotifications;
using AndroidNotification = Android.App.Notification;
using AndroidNotificationChannel = Android.App.NotificationChannel;
using NativeNotificationChannel = Miko.Native.LocalNotifications.NotificationChannel;

namespace Miko.Android.Native;

/// <summary>
/// Android 本地通知，基于 <see cref="NotificationManager"/>。
/// <para>
/// 立即展示的通知直接 <c>Notify</c>；带 <c>Schedule.At</c> 的定时通知需要
/// <c>AlarmManager</c> + <c>BroadcastReceiver</c>，而接收器必须在应用的
/// AndroidManifest 中声明——那属于应用侧的平台配置职责（<c>issues/feat-platform.md</c>
/// 要求第 3 条）。因此定时通知在此明确抛出，而不是悄悄变成立即通知。
/// </para>
/// </summary>
internal sealed class AndroidLocalNotificationService : NativeServiceBase, ILocalNotificationService
{
    private const string DefaultChannelId = "miko-default";

    private readonly INativeHostContext _hostContext;
    private readonly Lock _gate = new();
    private readonly Dictionary<int, LocalNotification> _posted = new();

    public AndroidLocalNotificationService(INativeHostContext hostContext) => _hostContext = hostContext;

    private Context Context => _hostContext.RequireHost<AndroidNativeHost>().Context;

    private NotificationManager Manager =>
        Context.GetSystemService(Context.NotificationService) as NotificationManager
        ?? throw new InvalidOperationException("NotificationManager is unavailable.");

    public Task<ScheduleResult> ScheduleAsync(ScheduleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var manager = Manager;
        var context = Context;
        var scheduled = new List<LocalNotificationDescriptor>();

        foreach (var notification in options.Notifications)
        {
            if (notification.Schedule is { At: not null } or { Every: not null } or { On: not null })
                throw new PlatformNotSupportedException(
                    "Scheduled local notifications need an AlarmManager BroadcastReceiver declared in the " +
                    "app's AndroidManifest; Miko.Android only posts immediate notifications. " +
                    $"Notification {notification.Id} requested a schedule.");

            var channelId = notification.ChannelId ?? DefaultChannelId;
            EnsureChannel(manager, channelId);

            var builder = new AndroidNotification.Builder(context, channelId)
                .SetContentTitle(notification.Title)
                .SetContentText(notification.Body)
                .SetAutoCancel(notification.AutoCancel)
                .SetOngoing(notification.Ongoing)
                // 必须设置小图标，否则 Android 会直接拒绝这条通知。
                .SetSmallIcon(context.ApplicationInfo?.Icon ?? global::Android.Resource.Drawable.IcDialogInfo);

            if (notification.LargeBody is { } largeBody)
                builder.SetStyle(new AndroidNotification.BigTextStyle().BigText(largeBody)!);

            if (notification.Badge is { } badge) builder.SetNumber(badge);
            if (notification.Group is { } group) builder.SetGroup(group);
            if (notification.GroupSummary) builder.SetGroupSummary(true);
            // 静默：清掉声音/振动/灯光的默认行为，并降到 Low 优先级（不弹浮动通知）。
            // Notification.Builder 的 SetSilent 未出现在当前绑定中，故用等价的组合。
            if (notification.Silent)
            {
                builder.SetDefaults(0);
                builder.SetSound(null);
                builder.SetVibrate(null);
                builder.SetPriority((int)NotificationPriority.Low);
            }

            manager.Notify(notification.Id, builder.Build());

            lock (_gate) _posted[notification.Id] = notification;
            scheduled.Add(new LocalNotificationDescriptor(notification.Id));
        }

        return Task.FromResult(new ScheduleResult(scheduled));
    }

    /// <summary>同 Id 重新 Notify 即为更新，与 Android 的通知语义一致。</summary>
    public Task<ScheduleResult> UpdateAsync(ScheduleOptions options) => ScheduleAsync(options);

    /// <summary>本实现不做定时，因此没有「待发送」的通知。</summary>
    public Task<PendingResult> GetPendingAsync() => Task.FromResult(new PendingResult([]));

    /// <summary>通知操作需要 BroadcastReceiver 接收点击回调，属于应用侧清单配置。</summary>
    public Task RegisterActionTypesAsync(RegisterActionTypesOptions options) => UnsupportedAsync();

    public Task CancelAsync(CancelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var manager = Manager;
        foreach (var descriptor in options.Notifications)
        {
            manager.Cancel(descriptor.Id);
            lock (_gate) _posted.Remove(descriptor.Id);
        }

        return Task.CompletedTask;
    }

    public Task CancelAllAsync()
    {
        Manager.CancelAll();
        lock (_gate) _posted.Clear();
        return Task.CompletedTask;
    }

    public Task<EnabledResult> AreEnabledAsync()
        => Task.FromResult(new EnabledResult(Manager.AreNotificationsEnabled()));

    public Task<DeliveredNotifications> GetDeliveredNotificationsAsync()
    {
        var active = Manager.GetActiveNotifications() ?? [];

        var delivered = active.Select(n =>
        {
            lock (_gate) _posted.TryGetValue(n.Id, out var source);

            return new DeliveredNotification
            {
                Id = n.Id,
                Tag = n.Tag,
                Title = n.Notification?.Extras?.GetString(AndroidNotification.ExtraTitle),
                Body = n.Notification?.Extras?.GetString(AndroidNotification.ExtraText),
                Group = n.Notification?.Group,
            };
        }).ToList();

        return Task.FromResult(new DeliveredNotifications(delivered));
    }

    public Task RemoveDeliveredNotificationsAsync(DeliveredNotifications notifications)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        var manager = Manager;
        foreach (var n in notifications.Notifications)
        {
            if (n.Tag is not null) manager.Cancel(n.Tag, n.Id);
            else manager.Cancel(n.Id);
        }

        return Task.CompletedTask;
    }

    public Task RemoveDeliveredNotificationsByIdAsync(int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var manager = Manager;
        foreach (var id in ids) manager.Cancel(id);

        return Task.CompletedTask;
    }

    public Task RemoveAllDeliveredNotificationsAsync()
    {
        Manager.CancelAll();
        return Task.CompletedTask;
    }

    public Task<GetNotificationsResult> GetByIdsAsync(int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        lock (_gate)
            return Task.FromResult(new GetNotificationsResult(
                ids.Where(_posted.ContainsKey).Select(id => _posted[id]).ToList()));
    }

    public Task<GetNotificationsResult> GetAllAsync(NotificationState? state = null)
    {
        // 本实现不做定时，所有通知都已触发；查询 Scheduled 得到空集。
        if (state == NotificationState.Scheduled)
            return Task.FromResult(new GetNotificationsResult([]));

        lock (_gate) return Task.FromResult(new GetNotificationsResult(_posted.Values.ToList()));
    }

    public Task CreateChannelAsync(NativeNotificationChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        var importance = channel.Importance switch
        {
            0 => NotificationImportance.None,
            1 => NotificationImportance.Min,
            2 => NotificationImportance.Low,
            4 => NotificationImportance.High,
            5 => NotificationImportance.Max,
            _ => NotificationImportance.Default,
        };

        using var androidChannel = new AndroidNotificationChannel(channel.Id, channel.Name, importance)
        {
            Description = channel.Description,
            LockscreenVisibility = (NotificationVisibility)channel.Visibility,
        };

        androidChannel.EnableLights(channel.Lights);
        androidChannel.EnableVibration(channel.Vibration);

        if (channel.LightColor is { } lightColor)
            androidChannel.LightColor = global::Android.Graphics.Color.ParseColor(lightColor).ToArgb();

        Manager.CreateNotificationChannel(androidChannel);
        return Task.CompletedTask;
    }

    public Task DeleteChannelAsync(string id)
    {
        Manager.DeleteNotificationChannel(id);
        return Task.CompletedTask;
    }

    public Task<ListChannelsResult> ListChannelsAsync()
    {
        var channels = (Manager.NotificationChannels ?? [])
            .Select(c => new NativeNotificationChannel
            {
                Id = c.Id ?? string.Empty,
                Name = c.Name ?? string.Empty,
                Description = c.Description,
                Importance = (int)c.Importance,
                Visibility = (int)c.LockscreenVisibility,
                Lights = c.ShouldShowLights(),
                Vibration = c.ShouldVibrate(),
            })
            .ToList();

        return Task.FromResult(new ListChannelsResult(channels));
    }

    public Task<NotificationPermissionStatus> CheckPermissionsAsync()
    {
        // Android 13 (API 33) 起 POST_NOTIFICATIONS 是运行时权限；更低版本默认授予。
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return Task.FromResult(new NotificationPermissionStatus(
                Manager.AreNotificationsEnabled() ? PermissionState.Granted : PermissionState.Denied));

        var granted = Context.CheckSelfPermission(Manifest.Permission.PostNotifications) == Permission.Granted;

        return Task.FromResult(new NotificationPermissionStatus(
            granted ? PermissionState.Granted : PermissionState.Denied));
    }

    /// <summary>权限请求需要 Activity 的 <c>OnRequestPermissionsResult</c> 回调，宿主未接线。</summary>
    public Task<NotificationPermissionStatus> RequestPermissionsAsync()
        => UnsupportedAsync<NotificationPermissionStatus>();

    public Task<ExactAlarmPermissionStatus> ChangeExactNotificationSettingAsync()
    {
        // 打开系统设置页由用户手动开启；无法在此等待结果（用户可能直接返回）。
        var context = Context;

        if (!OperatingSystem.IsAndroidVersionAtLeast(31)) throw Unsupported();

        using var intent = new Intent(global::Android.Provider.Settings.ActionRequestScheduleExactAlarm);
        intent.AddFlags(ActivityFlags.NewTask);
        context.StartActivity(intent);

        return CheckExactNotificationSettingAsync();
    }

    public Task<ExactAlarmPermissionStatus> CheckExactNotificationSettingAsync()
    {
        // API 31 之前没有这项权限，恒为已授予。
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            return Task.FromResult(new ExactAlarmPermissionStatus(PermissionState.Granted));

        var alarmManager = Context.GetSystemService(Context.AlarmService) as AlarmManager;
        var granted = alarmManager?.CanScheduleExactAlarms() ?? false;

        return Task.FromResult(new ExactAlarmPermissionStatus(
            granted ? PermissionState.Granted : PermissionState.Denied));
    }

    /// <summary>接收通知展示/点击需要在应用清单中声明 BroadcastReceiver，属于应用侧配置。</summary>
    public event Action<LocalNotification>? OnLocalNotificationReceived { add { } remove { } }
    public event Action<LocalNotificationActionPerformed>? OnLocalNotificationActionPerformed { add { } remove { } }

    private static void EnsureChannel(NotificationManager manager, string channelId)
    {
        if (manager.GetNotificationChannel(channelId) is not null) return;

        // Android O+ 上没有渠道的通知会被直接丢弃，因此默认渠道必须兜底创建。
        using var channel = new AndroidNotificationChannel(
            channelId, "Miko", NotificationImportance.Default);

        manager.CreateNotificationChannel(channel);
    }
}

/// <summary>
/// Android 推送。
/// <para>
/// FCM 需要 <c>google-services.json</c>、Firebase SDK 与一个 <c>FirebaseMessagingService</c>
/// 子类——三者都是**应用侧**的平台配置与代码（<c>issues/feat-platform.md</c> 要求第 3 条），
/// 不能由 Miko.Android 代为提供。因此除渠道管理（纯 <see cref="NotificationManager"/> 操作）
/// 外，其余成员明确抛出。
/// </para>
/// </summary>
internal sealed class AndroidPushNotificationService : NativeServiceBase, IPushNotificationService
{
    private readonly AndroidLocalNotificationService _channels;
    private readonly INativeHostContext _hostContext;

    public AndroidPushNotificationService(INativeHostContext hostContext)
    {
        _hostContext = hostContext;
        // 推送渠道与本地通知渠道是同一套系统对象，直接复用实现而不是抄一遍。
        _channels = new AndroidLocalNotificationService(hostContext);
    }

    private Context Context => _hostContext.RequireHost<AndroidNativeHost>().Context;

    private NotificationManager Manager =>
        Context.GetSystemService(Context.NotificationService) as NotificationManager
        ?? throw new InvalidOperationException("NotificationManager is unavailable.");

    public Task RegisterAsync() => UnsupportedAsync();
    public Task UnregisterAsync() => UnsupportedAsync();

    public Task<DeliveredPushNotifications> GetDeliveredNotificationsAsync()
    {
        var active = Manager.GetActiveNotifications() ?? [];

        var delivered = active.Select(n => new PushNotification
        {
            Id = n.Id.ToString(),
            Tag = n.Tag,
            Title = n.Notification?.Extras?.GetString(AndroidNotification.ExtraTitle),
            Body = n.Notification?.Extras?.GetString(AndroidNotification.ExtraText),
            Group = n.Notification?.Group,
        }).ToList();

        return Task.FromResult(new DeliveredPushNotifications(delivered));
    }

    public Task RemoveDeliveredNotificationsAsync(DeliveredPushNotifications notifications)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        var manager = Manager;
        foreach (var n in notifications.Notifications)
        {
            if (!int.TryParse(n.Id, out var id)) continue;

            if (n.Tag is not null) manager.Cancel(n.Tag, id);
            else manager.Cancel(id);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAllDeliveredNotificationsAsync()
    {
        Manager.CancelAll();
        return Task.CompletedTask;
    }

    public Task CreateChannelAsync(PushNotificationChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        return _channels.CreateChannelAsync(new NativeNotificationChannel
        {
            Id = channel.Id,
            Name = channel.Name,
            Description = channel.Description,
            Sound = channel.Sound,
            Importance = channel.Importance,
            Visibility = channel.Visibility,
            Lights = channel.Lights,
            LightColor = channel.LightColor,
            Vibration = channel.Vibration,
        });
    }

    public Task DeleteChannelAsync(string id) => _channels.DeleteChannelAsync(id);

    public async Task<PushChannelList> ListChannelsAsync()
    {
        var result = await _channels.ListChannelsAsync();

        return new PushChannelList(result.Channels.Select(c => new PushNotificationChannel
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Sound = c.Sound,
            Importance = c.Importance,
            Visibility = c.Visibility,
            Lights = c.Lights,
            LightColor = c.LightColor,
            Vibration = c.Vibration,
        }).ToList());
    }

    public async Task<PushPermissionStatus> CheckPermissionsAsync()
    {
        // 推送与本地通知共用 POST_NOTIFICATIONS 权限。
        var status = await _channels.CheckPermissionsAsync();
        return new PushPermissionStatus(status.Display);
    }

    public Task<PushPermissionStatus> RequestPermissionsAsync() => UnsupportedAsync<PushPermissionStatus>();

    /// <summary>需要应用侧的 FirebaseMessagingService 才能产生这些事件。</summary>
    public event Action<PushToken>? OnRegistration { add { } remove { } }
    public event Action<RegistrationError>? OnRegistrationError { add { } remove { } }
    public event Action<PushNotification>? OnPushNotificationReceived { add { } remove { } }
    public event Action<PushAction>? OnPushNotificationActionPerformed { add { } remove { } }
}
