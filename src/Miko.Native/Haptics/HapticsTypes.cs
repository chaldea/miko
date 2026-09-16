namespace Miko.Native.Haptics;

/// <summary>碰撞触觉强度。</summary>
public enum ImpactStyle
{
    /// <summary>重。</summary>
    Heavy,

    /// <summary>中（默认）。</summary>
    Medium,

    /// <summary>轻。</summary>
    Light,
}

/// <summary>通知触觉类型。</summary>
public enum NotificationType
{
    /// <summary>成功。</summary>
    Success,

    /// <summary>警告。</summary>
    Warning,

    /// <summary>错误。</summary>
    Error,
}

/// <summary>碰撞触觉选项。</summary>
public sealed record ImpactOptions
{
    /// <summary>触觉强度，默认 <see cref="ImpactStyle.Medium"/>。</summary>
    public ImpactStyle Style { get; init; } = ImpactStyle.Medium;
}

/// <summary>通知触觉选项。</summary>
public sealed record NotificationOptions
{
    /// <summary>通知类型，默认 <see cref="NotificationType.Success"/>。</summary>
    public NotificationType Type { get; init; } = NotificationType.Success;
}

/// <summary>振动选项。</summary>
public sealed record VibrateOptions
{
    /// <summary>振动时长（毫秒），默认 300。</summary>
    public int Duration { get; init; } = 300;
}
