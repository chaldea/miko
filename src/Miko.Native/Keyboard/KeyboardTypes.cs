namespace Miko.Native.Keyboard;

/// <summary>iOS 键盘外观样式。</summary>
public enum KeyboardStyle
{
    /// <summary>深色键盘。</summary>
    Dark,

    /// <summary>浅色键盘。</summary>
    Light,

    /// <summary>跟随系统。</summary>
    Default,
}

/// <summary>键盘弹出时视图的调整方式（iOS）。</summary>
public enum KeyboardResize
{
    /// <summary>调整 body 高度。</summary>
    Body,

    /// <summary>调整 Ionic 应用容器高度。</summary>
    Ionic,

    /// <summary>调整原生视图高度。</summary>
    Native,

    /// <summary>不调整。</summary>
    None,
}

/// <summary>键盘信息。</summary>
/// <param name="KeyboardHeight">键盘高度（逻辑像素）。</param>
public sealed record KeyboardInfo(double KeyboardHeight);

/// <summary>设置键盘样式的选项。</summary>
public sealed record KeyboardStyleOptions
{
    /// <summary>目标样式。</summary>
    public required KeyboardStyle Style { get; init; }
}

/// <summary>键盘调整模式选项。</summary>
public sealed record KeyboardResizeOptions
{
    /// <summary>调整模式。</summary>
    public required KeyboardResize Mode { get; init; }
}
