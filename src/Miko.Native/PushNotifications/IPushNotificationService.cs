namespace Miko.Native.PushNotifications;

/// <summary>
/// APNS / FCM 推送。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/native/push-notifications#api">Push Notifications API</see>。
/// 支持平台：Android、iOS、Windowing。
/// <para>
/// APNS 证书、FCM 配置文件、清单与后台模式声明属于平台实现职责，不在本接口内暴露。
/// </para>
/// </summary>
public interface IPushNotificationService
{
    /// <summary>向 APNS 或 FCM 注册；成功后通过 <see cref="OnRegistration"/> 返回令牌。</summary>
    Task RegisterAsync();

    /// <summary>注销推送注册。</summary>
    Task UnregisterAsync();

    /// <summary>获取通知中心里已送达的推送。</summary>
    Task<DeliveredPushNotifications> GetDeliveredNotificationsAsync();

    /// <summary>删除指定的已送达推送。</summary>
    Task RemoveDeliveredNotificationsAsync(DeliveredPushNotifications notifications);

    /// <summary>删除全部已送达推送。</summary>
    Task RemoveAllDeliveredNotificationsAsync();

    /// <summary>创建 Android O+ 推送渠道。</summary>
    Task CreateChannelAsync(PushNotificationChannel channel);

    /// <summary>删除 Android O+ 推送渠道。</summary>
    Task DeleteChannelAsync(string id);

    /// <summary>列出 Android O+ 推送渠道。</summary>
    Task<PushChannelList> ListChannelsAsync();

    /// <summary>查询推送权限。</summary>
    Task<PushPermissionStatus> CheckPermissionsAsync();

    /// <summary>请求推送权限。</summary>
    Task<PushPermissionStatus> RequestPermissionsAsync();

    /// <summary>注册成功，返回 APNS / FCM 令牌。</summary>
    event Action<PushToken>? OnRegistration;

    /// <summary>注册失败。</summary>
    event Action<RegistrationError>? OnRegistrationError;

    /// <summary>收到推送。</summary>
    event Action<PushNotification>? OnPushNotificationReceived;

    /// <summary>用户点击推送或执行推送上的操作。</summary>
    event Action<PushAction>? OnPushNotificationActionPerformed;
}
