using Foundation;
using Miko.Native;
using Miko.Native.LocalNotifications;
using Miko.Native.PushNotifications;
using UIKit;
using UserNotifications;
using NativeNotificationChannel = Miko.Native.LocalNotifications.NotificationChannel;

namespace Miko.iOS.Native;

/// <summary>
/// iOS 本地通知，基于 <see cref="UNUserNotificationCenter"/>。
/// </summary>
internal sealed class IosLocalNotificationService : NativeServiceBase, ILocalNotificationService
{
    private static UNUserNotificationCenter Center => UNUserNotificationCenter.Current;

    public async Task<ScheduleResult> ScheduleAsync(ScheduleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var scheduled = new List<LocalNotificationDescriptor>();

        foreach (var notification in options.Notifications)
        {
            using var content = new UNMutableNotificationContent
            {
                Title = notification.Title ?? string.Empty,
                Body = notification.Body ?? string.Empty,
            };

            if (notification.SummaryText is { } summary) content.Subtitle = summary;
            if (notification.Badge is { } badge) content.Badge = badge;
            if (notification.ThreadIdentifier is { } thread) content.ThreadIdentifier = thread;
            if (notification.ActionTypeId is { } category) content.CategoryIdentifier = category;

            // Silent 通知不带声音；否则用指定音效或系统默认音。
            if (!notification.Silent)
                content.Sound = notification.Sound is { } sound
                    ? UNNotificationSound.GetSound(sound)
                    : UNNotificationSound.Default;

            // 用 *2 取值：旧的 Critical/Passive/TimeSensitive/Active 已被绑定标记过时。
            if (notification.InterruptionLevel is { } level)
                content.InterruptionLevel = level switch
                {
                    Miko.Native.LocalNotifications.InterruptionLevel.Critical => UNNotificationInterruptionLevel.Critical2,
                    Miko.Native.LocalNotifications.InterruptionLevel.Passive => UNNotificationInterruptionLevel.Passive2,
                    Miko.Native.LocalNotifications.InterruptionLevel.TimeSensitive => UNNotificationInterruptionLevel.TimeSensitive2,
                    _ => UNNotificationInterruptionLevel.Active2,
                };

            if (notification.RelevanceScore is { } score) content.RelevanceScore = score;

            var trigger = BuildTrigger(notification.Schedule);

            using var request = UNNotificationRequest.FromIdentifier(
                notification.Id.ToString(), content, trigger);

            await Center.AddNotificationRequestAsync(request);
            scheduled.Add(new LocalNotificationDescriptor(notification.Id));

            trigger?.Dispose();
        }

        return new ScheduleResult(scheduled);
    }

    /// <summary>同 Id 重新提交即为更新，与 UNUserNotificationCenter 的语义一致。</summary>
    public Task<ScheduleResult> UpdateAsync(ScheduleOptions options) => ScheduleAsync(options);

    public async Task<PendingResult> GetPendingAsync()
    {
        var pending = await Center.GetPendingNotificationRequestsAsync();

        return new PendingResult(pending
            .Select(r => int.TryParse(r.Identifier, out var id) ? id : (int?)null)
            .Where(id => id is not null)
            .Select(id => new LocalNotificationDescriptor(id!.Value))
            .ToList());
    }

    public Task RegisterActionTypesAsync(RegisterActionTypesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var categories = options.Types.Select(type =>
        {
            var actions = type.Actions.Select(action =>
            {
                var actionOptions = UNNotificationActionOptions.None;
                if (action.Foreground) actionOptions |= UNNotificationActionOptions.Foreground;
                if (action.Destructive) actionOptions |= UNNotificationActionOptions.Destructive;
                if (action.RequiresAuthentication) actionOptions |= UNNotificationActionOptions.AuthenticationRequired;

                // 带输入框的操作是另一个类型，不能靠 options 表达。
                return action.Input
                    ? (UNNotificationAction)UNTextInputNotificationAction.FromIdentifier(
                        action.Id, action.Title, actionOptions,
                        action.InputButtonTitle ?? "Send", action.InputPlaceholder ?? string.Empty)
                    : UNNotificationAction.FromIdentifier(action.Id, action.Title, actionOptions);
            }).ToArray();

            return UNNotificationCategory.FromIdentifier(
                type.Id, actions, [], UNNotificationCategoryOptions.None);
        }).ToArray();

        Center.SetNotificationCategories(new NSSet<UNNotificationCategory>(categories));
        return Task.CompletedTask;
    }

    public Task CancelAsync(CancelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var ids = options.Notifications.Select(n => n.Id.ToString()).ToArray();
        Center.RemovePendingNotificationRequests(ids);

        return Task.CompletedTask;
    }

    public Task CancelAllAsync()
    {
        Center.RemoveAllPendingNotificationRequests();
        return Task.CompletedTask;
    }

    public async Task<EnabledResult> AreEnabledAsync()
    {
        var settings = await Center.GetNotificationSettingsAsync();

        return new EnabledResult(settings.AuthorizationStatus
            is UNAuthorizationStatus.Authorized
            or UNAuthorizationStatus.Provisional
            or UNAuthorizationStatus.Ephemeral);
    }

    public async Task<DeliveredNotifications> GetDeliveredNotificationsAsync()
    {
        var delivered = await Center.GetDeliveredNotificationsAsync();

        return new DeliveredNotifications(delivered
            .Select(n => new DeliveredNotification
            {
                Id = int.TryParse(n.Request?.Identifier, out var id) ? id : 0,
                Title = n.Request?.Content?.Title,
                Body = n.Request?.Content?.Body,
                ActionTypeId = n.Request?.Content?.CategoryIdentifier,
            })
            .ToList());
    }

    public Task RemoveDeliveredNotificationsAsync(DeliveredNotifications notifications)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        Center.RemoveDeliveredNotifications(
            notifications.Notifications.Select(n => n.Id.ToString()).ToArray());

        return Task.CompletedTask;
    }

    public Task RemoveDeliveredNotificationsByIdAsync(int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        Center.RemoveDeliveredNotifications(ids.Select(id => id.ToString()).ToArray());
        return Task.CompletedTask;
    }

    public Task RemoveAllDeliveredNotificationsAsync()
    {
        Center.RemoveAllDeliveredNotifications();
        return Task.CompletedTask;
    }

    /// <summary>
    /// iOS 不回读通知的原始业务字段（<c>Schedule</c>、<c>Extra</c> 等），
    /// 只能给出 Id/标题/正文；因此按 Id 查询只返回可从系统读回的那部分。
    /// </summary>
    public async Task<GetNotificationsResult> GetByIdsAsync(int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var wanted = ids.ToHashSet();
        var all = await GetAllAsync();

        return new GetNotificationsResult(all.Notifications.Where(n => wanted.Contains(n.Id)).ToList());
    }

    public async Task<GetNotificationsResult> GetAllAsync(NotificationState? state = null)
    {
        var notifications = new List<LocalNotification>();

        if (state != NotificationState.Triggered)
        {
            var pending = await Center.GetPendingNotificationRequestsAsync();
            notifications.AddRange(pending.Select(ToNotification));
        }

        if (state != NotificationState.Scheduled)
        {
            var delivered = await Center.GetDeliveredNotificationsAsync();
            notifications.AddRange(delivered
                .Where(n => n.Request is not null)
                .Select(n => ToNotification(n.Request!)));
        }

        return new GetNotificationsResult(notifications);
    }

    /// <summary>通知渠道是 Android 专有概念。</summary>
    public Task CreateChannelAsync(NativeNotificationChannel channel) => UnsupportedAsync();
    public Task DeleteChannelAsync(string id) => UnsupportedAsync();
    public Task<ListChannelsResult> ListChannelsAsync() => UnsupportedAsync<ListChannelsResult>();

    public async Task<NotificationPermissionStatus> CheckPermissionsAsync()
    {
        var settings = await Center.GetNotificationSettingsAsync();

        return new NotificationPermissionStatus(settings.AuthorizationStatus switch
        {
            UNAuthorizationStatus.Authorized
                or UNAuthorizationStatus.Provisional
                or UNAuthorizationStatus.Ephemeral => PermissionState.Granted,
            UNAuthorizationStatus.Denied => PermissionState.Denied,
            _ => PermissionState.Prompt,
        });
    }

    public async Task<NotificationPermissionStatus> RequestPermissionsAsync()
    {
        await Center.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound);

        return await CheckPermissionsAsync();
    }

    /// <summary>精确闹钟权限是 Android 12+ 专有概念。</summary>
    public Task<ExactAlarmPermissionStatus> ChangeExactNotificationSettingAsync()
        => UnsupportedAsync<ExactAlarmPermissionStatus>();

    public Task<ExactAlarmPermissionStatus> CheckExactNotificationSettingAsync()
        => UnsupportedAsync<ExactAlarmPermissionStatus>();

    /// <summary>
    /// 展示/点击回调需要在 <c>AppDelegate</c> 中设置 <c>UNUserNotificationCenter.Delegate</c>；
    /// 宿主未接线该委托，故不触发。
    /// </summary>
    public event Action<LocalNotification>? OnLocalNotificationReceived { add { } remove { } }
    public event Action<LocalNotificationActionPerformed>? OnLocalNotificationActionPerformed { add { } remove { } }

    private static UNNotificationTrigger? BuildTrigger(NotificationSchedule? schedule)
    {
        if (schedule is null) return null;

        if (schedule.At is { } at)
        {
            var seconds = (at - DateTimeOffset.Now).TotalSeconds;

            // UNTimeIntervalNotificationTrigger 要求间隔 > 0；已过的时间点立即触发。
            if (seconds <= 0) return null;

            return UNTimeIntervalNotificationTrigger.CreateTrigger(seconds, schedule.Repeats);
        }

        if (schedule.On is { } on)
        {
            var components = new NSDateComponents();
            if (on.Year is { } year) components.Year = year;
            if (on.Month is { } month) components.Month = month;
            if (on.Day is { } day) components.Day = day;
            if (on.Weekday is { } weekday) components.Weekday = weekday;
            if (on.Hour is { } hour) components.Hour = hour;
            if (on.Minute is { } minute) components.Minute = minute;
            if (on.Second is { } second) components.Second = second;

            return UNCalendarNotificationTrigger.CreateTrigger(components, schedule.Repeats);
        }

        if (schedule.Every is { } every)
        {
            var seconds = every switch
            {
                ScheduleEvery.Year => 365 * 24 * 3600.0,
                ScheduleEvery.Month => 30 * 24 * 3600.0,
                ScheduleEvery.TwoWeeks => 14 * 24 * 3600.0,
                ScheduleEvery.Week => 7 * 24 * 3600.0,
                ScheduleEvery.Day => 24 * 3600.0,
                ScheduleEvery.Hour => 3600.0,
                ScheduleEvery.Minute => 60.0,
                _ => 1.0,
            };

            // iOS 要求重复触发的间隔至少 60 秒。
            if (seconds < 60) seconds = 60;

            return UNTimeIntervalNotificationTrigger.CreateTrigger(seconds, repeats: true);
        }

        return null;
    }

    private static LocalNotification ToNotification(UNNotificationRequest request) => new()
    {
        Id = int.TryParse(request.Identifier, out var id) ? id : 0,
        Title = request.Content?.Title,
        Body = request.Content?.Body,
        SummaryText = request.Content?.Subtitle,
        ActionTypeId = request.Content?.CategoryIdentifier,
    };
}

/// <summary>
/// iOS 推送（APNS）。
/// <para>
/// 注册通过 <c>UIApplication.RegisterForRemoteNotifications</c> 发起，但 device token
/// 只会送到 <c>AppDelegate.RegisteredForRemoteNotifications</c>——宿主未接线该回调，
/// 因此 <see cref="OnRegistration"/> 不会触发。应用应在自己的 AppDelegate 中处理 token。
/// </para>
/// </summary>
internal sealed class IosPushNotificationService : NativeServiceBase, IPushNotificationService
{
    private readonly IosLocalNotificationService _local = new();

    public Task RegisterAsync()
    {
        UIApplication.SharedApplication.InvokeOnMainThread(
            UIApplication.SharedApplication.RegisterForRemoteNotifications);

        return Task.CompletedTask;
    }

    public Task UnregisterAsync()
    {
        UIApplication.SharedApplication.InvokeOnMainThread(
            UIApplication.SharedApplication.UnregisterForRemoteNotifications);

        return Task.CompletedTask;
    }

    public async Task<DeliveredPushNotifications> GetDeliveredNotificationsAsync()
    {
        // 远程与本地通知都落在同一个通知中心里。
        var delivered = await _local.GetDeliveredNotificationsAsync();

        return new DeliveredPushNotifications(delivered.Notifications
            .Select(n => new PushNotification
            {
                Id = n.Id.ToString(),
                Title = n.Title,
                Body = n.Body,
            })
            .ToList());
    }

    public Task RemoveDeliveredNotificationsAsync(DeliveredPushNotifications notifications)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        UNUserNotificationCenter.Current.RemoveDeliveredNotifications(
            notifications.Notifications.Select(n => n.Id).ToArray());

        return Task.CompletedTask;
    }

    public Task RemoveAllDeliveredNotificationsAsync() => _local.RemoveAllDeliveredNotificationsAsync();

    /// <summary>通知渠道是 Android 专有概念。</summary>
    public Task CreateChannelAsync(PushNotificationChannel channel) => UnsupportedAsync();
    public Task DeleteChannelAsync(string id) => UnsupportedAsync();
    public Task<PushChannelList> ListChannelsAsync() => UnsupportedAsync<PushChannelList>();

    public async Task<PushPermissionStatus> CheckPermissionsAsync()
    {
        // 推送与本地通知共用同一套通知授权。
        var status = await _local.CheckPermissionsAsync();
        return new PushPermissionStatus(status.Display);
    }

    public async Task<PushPermissionStatus> RequestPermissionsAsync()
    {
        var status = await _local.RequestPermissionsAsync();
        return new PushPermissionStatus(status.Display);
    }

    /// <summary>需要应用侧在 AppDelegate 中接线 APNS 回调才能产生这些事件。</summary>
    public event Action<PushToken>? OnRegistration { add { } remove { } }
    public event Action<RegistrationError>? OnRegistrationError { add { } remove { } }
    public event Action<PushNotification>? OnPushNotificationReceived { add { } remove { } }
    public event Action<PushAction>? OnPushNotificationActionPerformed { add { } remove { } }
}
