namespace Miko.Native.Toast;

/// <summary>Toast 显示时长。</summary>
public enum ToastDuration
{
    /// <summary>短（约 2000ms），默认值。</summary>
    Short,

    /// <summary>长（约 3500ms）。</summary>
    Long,
}

/// <summary>Toast 显示位置。Android 12+ 上系统 Toast 始终显示在底部，该设置被忽略。</summary>
public enum ToastPosition
{
    /// <summary>顶部。</summary>
    Top,

    /// <summary>居中。</summary>
    Center,

    /// <summary>底部，默认值。</summary>
    Bottom,
}

/// <summary>Toast 选项。</summary>
public sealed record ToastOptions
{
    /// <summary>要显示的文本（必填）。</summary>
    public required string Text { get; init; }

    /// <summary>显示时长，默认 <see cref="ToastDuration.Short"/>。</summary>
    public ToastDuration Duration { get; init; } = ToastDuration.Short;

    /// <summary>显示位置，默认 <see cref="ToastPosition.Bottom"/>。</summary>
    public ToastPosition Position { get; init; } = ToastPosition.Bottom;
}
