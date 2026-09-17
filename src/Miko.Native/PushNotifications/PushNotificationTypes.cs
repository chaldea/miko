namespace Miko.Native.PushNotifications;

/// <summary>APNS / FCM 推送令牌。</summary>
/// <param name="Value">令牌字符串，需上报给业务后端。</param>
public sealed record PushToken(string Value);

/// <summary>推送注册失败信息。</summary>
/// <param name="Error">失败原因。</param>
public sealed record RegistrationError(string Error);

/// <summary>一条收到的推送通知。</summary>
public sealed record PushNotification
{
    /// <summary>通知标识。</summary>
    public required string Id { get; init; }

    /// <summary>标题。</summary>
    public string? Title { get; init; }

    /// <summary>副标题。</summary>
    public string? Subtitle { get; init; }

    /// <summary>正文。</summary>
    public string? Body { get; init; }

    /// <summary>通知标签（Android）。</summary>
    public string? Tag { get; init; }

    /// <summary>应用角标数字。</summary>
    public int? Badge { get; init; }

    /// <summary>推送携带的自定义数据。</summary>
    public object? Data { get; init; }

    /// <summary>点击动作（Android）。</summary>
    public string? ClickAction { get; init; }

    /// <summary>深链地址。</summary>
    public string? Link { get; init; }

    /// <summary>分组标识（Android）。</summary>
    public string? Group { get; init; }

    /// <summary>是否为分组摘要（Android）。</summary>
    public bool? GroupSummary { get; init; }
}

/// <summary>用户对推送执行的操作。</summary>
/// <param name="ActionId">操作标识；点击通知本身时为 <c>tap</c>。</param>
/// <param name="InputValue">操作附带输入框时用户输入的文本。</param>
/// <param name="Notification">关联的推送通知。</param>
public sealed record PushAction(string ActionId, string? InputValue, PushNotification Notification);

/// <summary>已送达推送通知的集合。</summary>
/// <param name="Notifications">已送达的推送通知列表。</param>
public sealed record DeliveredPushNotifications(IReadOnlyList<PushNotification> Notifications);

/// <summary>Android O+ 推送通知渠道。</summary>
public sealed record PushNotificationChannel
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

/// <summary>推送渠道列表。</summary>
/// <param name="Channels">渠道列表。</param>
public sealed record PushChannelList(IReadOnlyList<PushNotificationChannel> Channels);

/// <summary>推送权限状态。</summary>
/// <param name="Receive">接收推送的权限。</param>
public sealed record PushPermissionStatus(PermissionState Receive);
