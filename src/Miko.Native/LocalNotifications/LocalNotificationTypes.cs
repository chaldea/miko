namespace Miko.Native.LocalNotifications;

/// <summary>重复调度的周期。</summary>
public enum ScheduleEvery
{
    /// <summary>每年。</summary>
    Year,

    /// <summary>每月。</summary>
    Month,

    /// <summary>每两周。</summary>
    TwoWeeks,

    /// <summary>每周。</summary>
    Week,

    /// <summary>每天。</summary>
    Day,

    /// <summary>每小时。</summary>
    Hour,

    /// <summary>每分钟。</summary>
    Minute,

    /// <summary>每秒。</summary>
    Second,
}

/// <summary>通知查询状态。</summary>
public enum NotificationState
{
    /// <summary>已调度但尚未触发。</summary>
    Scheduled,

    /// <summary>已触发。</summary>
    Triggered,
}

/// <summary>通知打断级别（iOS 15+）。</summary>
public enum InterruptionLevel
{
    /// <summary>默认，会点亮屏幕。</summary>
    Active,

    /// <summary>关键，可穿透专注模式与静音。</summary>
    Critical,

    /// <summary>被动，不点亮屏幕。</summary>
    Passive,

    /// <summary>时效性，可穿透专注模式。</summary>
    TimeSensitive,
}

/// <summary>按日历字段匹配的调度条件。未设置的字段表示「任意」。</summary>
public sealed record ScheduleOn
{
    /// <summary>年。</summary>
    public int? Year { get; init; }

    /// <summary>月（1～12）。</summary>
    public int? Month { get; init; }

    /// <summary>日（1～31）。</summary>
    public int? Day { get; init; }

    /// <summary>星期几（1＝周日 … 7＝周六）。</summary>
    public int? Weekday { get; init; }

    /// <summary>小时（0～23）。</summary>
    public int? Hour { get; init; }

    /// <summary>分钟（0～59）。</summary>
    public int? Minute { get; init; }

    /// <summary>秒（0～59）。</summary>
    public int? Second { get; init; }
}

/// <summary>通知调度设置。</summary>
public sealed record NotificationSchedule
{
    /// <summary>触发时间点。</summary>
    public DateTimeOffset? At { get; init; }

    /// <summary>是否按 <see cref="At"/> 的间隔重复。</summary>
    public bool Repeats { get; init; }

    /// <summary>是否允许在 Android 低电耗（Doze）模式下触发。</summary>
    public bool AllowWhileIdle { get; init; }

    /// <summary>按日历字段匹配触发。</summary>
    public ScheduleOn? On { get; init; }

    /// <summary>按固定周期重复触发。</summary>
    public ScheduleEvery? Every { get; init; }

    /// <summary>配合 <see cref="Every"/> 使用的重复次数。</summary>
    public int? Count { get; init; }
}

/// <summary>通知附件的额外选项。</summary>
public sealed record AttachmentOptions
{
    /// <summary>iOS 缩略图裁剪区域（归一化矩形字符串）。</summary>
    public string? IosUNNotificationAttachmentOptionsThumbnailClippingRectKey { get; init; }

    /// <summary>iOS 缩略图是否隐藏。</summary>
    public string? IosUNNotificationAttachmentOptionsThumbnailHiddenKey { get; init; }

    /// <summary>iOS 缩略图时间点（视频）。</summary>
    public string? IosUNNotificationAttachmentOptionsThumbnailTimeKey { get; init; }

    /// <summary>iOS 附件类型提示。</summary>
    public string? IosUNNotificationAttachmentOptionsTypeHintKey { get; init; }
}

/// <summary>通知附件。</summary>
/// <param name="Id">附件标识。</param>
/// <param name="Url">附件资源 URL。</param>
/// <param name="Options">附件额外选项。</param>
public sealed record Attachment(string Id, string Url, AttachmentOptions? Options = null);

/// <summary>一条本地通知。</summary>
public sealed record LocalNotification
{
    /// <summary>通知标识（必填，用于更新和取消）。</summary>
    public required int Id { get; init; }

    /// <summary>标题。</summary>
    public string? Title { get; init; }

    /// <summary>正文。</summary>
    public string? Body { get; init; }

    /// <summary>展开后的长正文（Android）。</summary>
    public string? LargeBody { get; init; }

    /// <summary>摘要文本（Android）。</summary>
    public string? SummaryText { get; init; }

    /// <summary>调度设置；为 <c>null</c> 时立即展示。</summary>
    public NotificationSchedule? Schedule { get; init; }

    /// <summary>提示音资源名。</summary>
    public string? Sound { get; init; }

    /// <summary>通知渠道标识（Android O+）。</summary>
    public string? ChannelId { get; init; }

    /// <summary>关联的通知操作类型标识。</summary>
    public string? ActionTypeId { get; init; }

    /// <summary>随通知携带的自定义数据。</summary>
    public object? Extra { get; init; }

    /// <summary>是否为常驻通知（用户不可滑动清除，Android）。</summary>
    public bool Ongoing { get; init; }

    /// <summary>点击后是否自动清除。</summary>
    public bool AutoCancel { get; init; }

    /// <summary>应用角标数字。</summary>
    public int? Badge { get; init; }

    /// <summary>应用在前台时是否仍然展示（iOS）。</summary>
    public bool? Foreground { get; init; }

    /// <summary>是否使用精确闹钟调度（Android 12+ 需额外权限）。</summary>
    public bool IsExactNotification { get; init; }

    /// <summary>精确闹钟是否为强制要求；为 <c>true</c> 时缺少权限将失败而非降级。</summary>
    public bool IsExactMandatory { get; init; }

    /// <summary>附件列表。</summary>
    public IReadOnlyList<Attachment>? Attachments { get; init; }

    /// <summary>会话线程标识（iOS，用于分组）。</summary>
    public string? ThreadIdentifier { get; init; }

    /// <summary>分组摘要参数（iOS）。</summary>
    public string? SummaryArgument { get; init; }

    /// <summary>相关性评分，0～1（iOS，影响通知摘要排序）。</summary>
    public double? RelevanceScore { get; init; }

    /// <summary>打断级别（iOS 15+）。</summary>
    public InterruptionLevel? InterruptionLevel { get; init; }

    /// <summary>分组标识（Android）。</summary>
    public string? Group { get; init; }

    /// <summary>是否为分组摘要通知（Android）。</summary>
    public bool GroupSummary { get; init; }

    /// <summary>收件箱样式的多行内容（Android）。</summary>
    public IReadOnlyList<string>? InboxList { get; init; }

    /// <summary>是否静默展示（无声音无振动）。</summary>
    public bool Silent { get; init; }
}

/// <summary>调度通知的选项。</summary>
/// <param name="Notifications">要调度的通知列表。</param>
public sealed record ScheduleOptions(IReadOnlyList<LocalNotification> Notifications);

/// <summary>已调度通知的标识描述。</summary>
/// <param name="Id">通知标识。</param>
public sealed record LocalNotificationDescriptor(int Id);

/// <summary>调度时产生的警告（如精确闹钟权限缺失而降级）。</summary>
/// <param name="Message">警告信息。</param>
public sealed record ScheduleWarning(string Message);

/// <summary>调度结果。</summary>
/// <param name="Notifications">已调度的通知标识列表。</param>
/// <param name="Warning">调度警告；无警告时为 <c>null</c>。</param>
public sealed record ScheduleResult(
    IReadOnlyList<LocalNotificationDescriptor> Notifications,
    ScheduleWarning? Warning = null);

/// <summary>待发送通知的查询结果。</summary>
/// <param name="Notifications">待发送通知列表。</param>
public sealed record PendingResult(IReadOnlyList<LocalNotificationDescriptor> Notifications);

/// <summary>按标识/状态查询通知的结果。</summary>
/// <param name="Notifications">查询到的通知列表。</param>
public sealed record GetNotificationsResult(IReadOnlyList<LocalNotification> Notifications);

/// <summary>一条已送达（展示在通知中心）的通知。</summary>
public sealed record DeliveredNotification
{
    /// <summary>通知标识。</summary>
    public required int Id { get; init; }

    /// <summary>通知标签（Android）。</summary>
    public string? Tag { get; init; }

    /// <summary>标题。</summary>
    public string? Title { get; init; }

    /// <summary>正文。</summary>
    public string? Body { get; init; }

    /// <summary>分组标识。</summary>
    public string? Group { get; init; }

    /// <summary>是否为分组摘要。</summary>
    public bool GroupSummary { get; init; }

    /// <summary>通知携带的数据。</summary>
    public object? Data { get; init; }

    /// <summary>自定义附加数据。</summary>
    public object? Extra { get; init; }

    /// <summary>附件列表。</summary>
    public IReadOnlyList<Attachment>? Attachments { get; init; }

    /// <summary>关联的通知操作类型标识。</summary>
    public string? ActionTypeId { get; init; }

    /// <summary>调度设置。</summary>
    public NotificationSchedule? Schedule { get; init; }

    /// <summary>提示音。</summary>
    public string? Sound { get; init; }
}

/// <summary>已送达通知的集合。</summary>
/// <param name="Notifications">已送达通知列表。</param>
public sealed record DeliveredNotifications(IReadOnlyList<DeliveredNotification> Notifications);

/// <summary>通知上的一个可执行操作。</summary>
public sealed record NotificationAction
{
    /// <summary>操作标识。</summary>
    public required string Id { get; init; }

    /// <summary>操作按钮标题。</summary>
    public required string Title { get; init; }

    /// <summary>执行前是否需要设备解锁认证。</summary>
    public bool RequiresAuthentication { get; init; }

    /// <summary>执行时是否把应用带到前台。</summary>
    public bool Foreground { get; init; }

    /// <summary>是否为破坏性操作（红色显示）。</summary>
    public bool Destructive { get; init; }

    /// <summary>是否附带文本输入框。</summary>
    public bool Input { get; init; }

    /// <summary>输入框提交按钮标题。</summary>
    public string? InputButtonTitle { get; init; }

    /// <summary>输入框占位文本。</summary>
    public string? InputPlaceholder { get; init; }
}

/// <summary>一组通知操作。</summary>
/// <param name="Id">操作类型标识，供 <c>LocalNotification.ActionTypeId</c> 引用。</param>
/// <param name="Actions">该类型包含的操作。</param>
public sealed record ActionType(string Id, IReadOnlyList<NotificationAction> Actions);

/// <summary>注册通知操作类型的选项。</summary>
/// <param name="Types">要注册的操作类型列表。</param>
public sealed record RegisterActionTypesOptions(IReadOnlyList<ActionType> Types);

/// <summary>取消通知的选项。</summary>
/// <param name="Notifications">要取消的通知标识列表。</param>
public sealed record CancelOptions(IReadOnlyList<LocalNotificationDescriptor> Notifications);

/// <summary>通知是否启用的查询结果。</summary>
/// <param name="Value">通知是否已启用。</param>
public sealed record EnabledResult(bool Value);

/// <summary>Android 通知渠道。</summary>
public sealed record NotificationChannel
{
    /// <summary>渠道标识。</summary>
    public required string Id { get; init; }

    /// <summary>渠道名称（用户可见）。</summary>
    public required string Name { get; init; }

    /// <summary>渠道描述。</summary>
    public string? Description { get; init; }

    /// <summary>提示音资源名。</summary>
    public string? Sound { get; init; }

    /// <summary>重要性，0～5。</summary>
    public int Importance { get; init; } = 3;

    /// <summary>锁屏可见性，-1～1。</summary>
    public int Visibility { get; init; }

    /// <summary>是否启用呼吸灯。</summary>
    public bool Lights { get; init; }

    /// <summary>呼吸灯颜色（十六进制字符串）。</summary>
    public string? LightColor { get; init; }

    /// <summary>是否启用振动。</summary>
    public bool Vibration { get; init; }
}

/// <summary>通知渠道列表。</summary>
/// <param name="Channels">渠道列表。</param>
public sealed record ListChannelsResult(IReadOnlyList<NotificationChannel> Channels);

/// <summary>通知权限状态。</summary>
/// <param name="Display">展示通知的权限。</param>
public sealed record NotificationPermissionStatus(PermissionState Display);

/// <summary>Android 精确闹钟权限状态。</summary>
/// <param name="ExactAlarm">精确闹钟权限。</param>
public sealed record ExactAlarmPermissionStatus(PermissionState ExactAlarm);

/// <summary>用户对通知执行操作的事件。</summary>
/// <param name="ActionId">被执行的操作标识；点击通知本身时为 <c>tap</c>。</param>
/// <param name="InputValue">操作附带输入框时用户输入的文本。</param>
/// <param name="Notification">关联的通知。</param>
public sealed record LocalNotificationActionPerformed(
    string ActionId,
    string? InputValue,
    LocalNotification Notification);
