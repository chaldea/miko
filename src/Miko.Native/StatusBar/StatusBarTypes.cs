namespace Miko.Native.StatusBar;

/// <summary>状态栏文字/图标样式。</summary>
public enum StatusBarStyle
{
    /// <summary>深色内容（适用于浅色背景）。</summary>
    Dark,

    /// <summary>浅色内容（适用于深色背景）。</summary>
    Light,

    /// <summary>跟随系统。</summary>
    Default,
}

/// <summary>状态栏显隐动画。</summary>
public enum StatusBarAnimation
{
    /// <summary>无动画。</summary>
    None,

    /// <summary>滑动。</summary>
    Slide,

    /// <summary>淡入淡出。</summary>
    Fade,
}

/// <summary>设置状态栏样式的选项。</summary>
public sealed record StatusBarStyleOptions
{
    /// <summary>目标样式。</summary>
    public required StatusBarStyle Style { get; init; }
}

/// <summary>设置状态栏背景色的选项。Android 15+ 强制 edge-to-edge，该设置无效。</summary>
public sealed record BackgroundColorOptions
{
    /// <summary>十六进制颜色字符串，如 <c>#ffffff</c>。</summary>
    public required string Color { get; init; }
}

/// <summary>状态栏显隐的动画选项。</summary>
public sealed record AnimationOptions
{
    /// <summary>动画类型，默认无动画。</summary>
    public StatusBarAnimation Animation { get; init; } = StatusBarAnimation.None;
}

/// <summary>设置状态栏是否覆盖内容的选项。Android 15+ 无效。</summary>
public sealed record SetOverlaysWebViewOptions
{
    /// <summary>为 <c>true</c> 时状态栏覆盖在内容之上。</summary>
    public required bool Overlay { get; init; }
}

/// <summary>状态栏当前状态。</summary>
/// <param name="Visible">是否可见。</param>
/// <param name="Style">当前样式。</param>
/// <param name="Color">当前背景色（十六进制字符串）。</param>
/// <param name="Overlays">是否覆盖在内容之上。</param>
/// <param name="Height">状态栏高度（逻辑像素）。</param>
public sealed record StatusBarInfo(
    bool Visible,
    StatusBarStyle Style,
    string Color,
    bool Overlays,
    double Height);
