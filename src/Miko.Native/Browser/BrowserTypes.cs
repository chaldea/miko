namespace Miko.Native.Browser;

/// <summary>内嵌浏览器的呈现方式（iOS）。</summary>
public enum BrowserPresentationStyle
{
    /// <summary>全屏呈现（默认）。</summary>
    Fullscreen,

    /// <summary>以 Popover 呈现（iPad）。</summary>
    Popover,
}

/// <summary>打开浏览器的选项。</summary>
public sealed record OpenOptions
{
    /// <summary>要打开的 URL（必填）。</summary>
    public required string Url { get; init; }

    /// <summary>窗口名称（Web 目标使用，对应 <c>window.open</c> 的 target）。</summary>
    public string? WindowName { get; init; }

    /// <summary>工具栏颜色，十六进制字符串（如 <c>#ffffff</c>）。</summary>
    public string? ToolbarColor { get; init; }

    /// <summary>呈现方式，默认全屏。</summary>
    public BrowserPresentationStyle PresentationStyle { get; init; } = BrowserPresentationStyle.Fullscreen;

    /// <summary>Popover 宽度（iOS Popover 时有效）。</summary>
    public double? Width { get; init; }

    /// <summary>Popover 高度（iOS Popover 时有效）。</summary>
    public double? Height { get; init; }
}
