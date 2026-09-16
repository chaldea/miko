namespace Miko.Native.LocalNotifications;

/// <summary>未注册平台实现时的 <see cref="ILocalNotificationService"/> 占位实现，调用即抛。</summary>
public sealed class NullLocalNotificationService : NativeServiceBase, ILocalNotificationService
{
    public Task<ScheduleResult> ScheduleAsync(ScheduleOptions options) => UnsupportedAsync<ScheduleResult>();
    public Task<ScheduleResult> UpdateAsync(ScheduleOptions options) => UnsupportedAsync<ScheduleResult>();
    public Task<PendingResult> GetPendingAsync() => UnsupportedAsync<PendingResult>();
    public Task RegisterActionTypesAsync(RegisterActionTypesOptions options) => UnsupportedAsync();
    public Task CancelAsync(CancelOptions options) => UnsupportedAsync();
    public Task CancelAllAsync() => UnsupportedAsync();
    public Task<EnabledResult> AreEnabledAsync() => UnsupportedAsync<EnabledResult>();
    public Task<DeliveredNotifications> GetDeliveredNotificationsAsync() => UnsupportedAsync<DeliveredNotifications>();
    public Task RemoveDeliveredNotificationsAsync(DeliveredNotifications notifications) => UnsupportedAsync();
    public Task RemoveDeliveredNotificationsByIdAsync(int[] ids) => UnsupportedAsync();
    public Task RemoveAllDeliveredNotificationsAsync() => UnsupportedAsync();
    public Task<GetNotificationsResult> GetByIdsAsync(int[] ids) => UnsupportedAsync<GetNotificationsResult>();
    public Task<GetNotificationsResult> GetAllAsync(NotificationState? state = null) => UnsupportedAsync<GetNotificationsResult>();
    public Task CreateChannelAsync(NotificationChannel channel) => UnsupportedAsync();
    public Task DeleteChannelAsync(string id) => UnsupportedAsync();
    public Task<ListChannelsResult> ListChannelsAsync() => UnsupportedAsync<ListChannelsResult>();
    public Task<NotificationPermissionStatus> CheckPermissionsAsync() => UnsupportedAsync<NotificationPermissionStatus>();
    public Task<NotificationPermissionStatus> RequestPermissionsAsync() => UnsupportedAsync<NotificationPermissionStatus>();
    public Task<ExactAlarmPermissionStatus> ChangeExactNotificationSettingAsync() => UnsupportedAsync<ExactAlarmPermissionStatus>();
    public Task<ExactAlarmPermissionStatus> CheckExactNotificationSettingAsync() => UnsupportedAsync<ExactAlarmPermissionStatus>();

    public event Action<LocalNotification>? OnLocalNotificationReceived { add { } remove { } }
    public event Action<LocalNotificationActionPerformed>? OnLocalNotificationActionPerformed { add { } remove { } }
}
