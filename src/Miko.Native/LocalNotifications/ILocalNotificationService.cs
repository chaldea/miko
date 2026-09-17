namespace Miko.Native.LocalNotifications;

/// <summary>
/// 本地通知调度。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/native/local-notifications#api">Local Notifications API</see>。
/// 支持平台：Android、iOS、Windowing。
/// </summary>
public interface ILocalNotificationService
{
    /// <summary>创建通知；必要时会先请求通知权限。</summary>
    Task<ScheduleResult> ScheduleAsync(ScheduleOptions options);

    /// <summary>更新已调度的通知（按 <c>Id</c> 匹配）。</summary>
    Task<ScheduleResult> UpdateAsync(ScheduleOptions options);

    /// <summary>获取待发送的通知。</summary>
    Task<PendingResult> GetPendingAsync();

    /// <summary>注册通知操作类型，供通知的 <c>ActionTypeId</c> 引用。</summary>
    Task RegisterActionTypesAsync(RegisterActionTypesOptions options);

    /// <summary>取消指定的待发送通知。</summary>
    Task CancelAsync(CancelOptions options);

    /// <summary>取消全部待发送通知。</summary>
    Task CancelAllAsync();

    /// <summary>查询通知是否已启用。</summary>
    Task<EnabledResult> AreEnabledAsync();

    /// <summary>获取通知中心里已送达的通知。</summary>
    Task<DeliveredNotifications> GetDeliveredNotificationsAsync();

    /// <summary>删除指定的已送达通知。</summary>
    Task RemoveDeliveredNotificationsAsync(DeliveredNotifications notifications);

    /// <summary>按标识删除已送达通知。</summary>
    Task RemoveDeliveredNotificationsByIdAsync(int[] ids);

    /// <summary>删除全部已送达通知。</summary>
    Task RemoveAllDeliveredNotificationsAsync();

    /// <summary>按标识查询通知。</summary>
    Task<GetNotificationsResult> GetByIdsAsync(int[] ids);

    /// <summary>按状态查询通知；<paramref name="state"/> 为 <c>null</c> 时返回全部。</summary>
    Task<GetNotificationsResult> GetAllAsync(NotificationState? state = null);

    /// <summary>创建 Android 通知渠道。</summary>
    Task CreateChannelAsync(NotificationChannel channel);

    /// <summary>删除 Android 通知渠道。</summary>
    Task DeleteChannelAsync(string id);

    /// <summary>列出 Android 通知渠道。</summary>
    Task<ListChannelsResult> ListChannelsAsync();

    /// <summary>查询通知权限。</summary>
    Task<NotificationPermissionStatus> CheckPermissionsAsync();

    /// <summary>请求通知权限。</summary>
    Task<NotificationPermissionStatus> RequestPermissionsAsync();

    /// <summary>打开系统设置以更改 Android 精确闹钟权限，返回更改后的状态。</summary>
    Task<ExactAlarmPermissionStatus> ChangeExactNotificationSettingAsync();

    /// <summary>查询 Android 精确闹钟权限。</summary>
    Task<ExactAlarmPermissionStatus> CheckExactNotificationSettingAsync();

    /// <summary>通知展示时触发。</summary>
    event Action<LocalNotification>? OnLocalNotificationReceived;

    /// <summary>
    /// 用户执行通知操作时触发。
    /// <para>
    /// 载荷用 <see cref="LocalNotificationActionPerformed"/> 而非原始文档中的 <c>NotificationAction</c>：
    /// 后者是操作的**声明**（标题、是否需要认证等），不含被操作的通知，也无法表达用户在输入框里
    /// 填了什么。事件必须回答「哪条通知上执行了哪个操作、输入了什么」，故单独建模。
    /// </para>
    /// </summary>
    event Action<LocalNotificationActionPerformed>? OnLocalNotificationActionPerformed;
}
