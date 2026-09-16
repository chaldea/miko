namespace Miko.Native.PushNotifications;

/// <summary>未注册平台实现时的 <see cref="IPushNotificationService"/> 占位实现，调用即抛。</summary>
public sealed class NullPushNotificationService : NativeServiceBase, IPushNotificationService
{
    public Task RegisterAsync() => UnsupportedAsync();
    public Task UnregisterAsync() => UnsupportedAsync();
    public Task<DeliveredPushNotifications> GetDeliveredNotificationsAsync() => UnsupportedAsync<DeliveredPushNotifications>();
    public Task RemoveDeliveredNotificationsAsync(DeliveredPushNotifications notifications) => UnsupportedAsync();
    public Task RemoveAllDeliveredNotificationsAsync() => UnsupportedAsync();
    public Task CreateChannelAsync(PushNotificationChannel channel) => UnsupportedAsync();
    public Task DeleteChannelAsync(string id) => UnsupportedAsync();
    public Task<PushChannelList> ListChannelsAsync() => UnsupportedAsync<PushChannelList>();
    public Task<PushPermissionStatus> CheckPermissionsAsync() => UnsupportedAsync<PushPermissionStatus>();
    public Task<PushPermissionStatus> RequestPermissionsAsync() => UnsupportedAsync<PushPermissionStatus>();

    public event Action<PushToken>? OnRegistration { add { } remove { } }
    public event Action<RegistrationError>? OnRegistrationError { add { } remove { } }
    public event Action<PushNotification>? OnPushNotificationReceived { add { } remove { } }
    public event Action<PushAction>? OnPushNotificationActionPerformed { add { } remove { } }
}
