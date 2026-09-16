namespace Miko.Native.SplashScreen;

/// <summary>显示启动画面的选项。</summary>
public sealed record ShowSplashOptions
{
    /// <summary>是否在 <see cref="ShowDuration"/> 之后自动隐藏。</summary>
    public bool AutoHide { get; init; }

    /// <summary>淡入时长（毫秒）。</summary>
    public int FadeInDuration { get; init; }

    /// <summary>淡出时长（毫秒）。</summary>
    public int FadeOutDuration { get; init; }

    /// <summary>显示时长（毫秒），<see cref="AutoHide"/> 为 <c>true</c> 时生效。</summary>
    public int ShowDuration { get; init; }
}

/// <summary>隐藏启动画面的选项。</summary>
public sealed record HideSplashOptions
{
    /// <summary>淡出时长（毫秒）。</summary>
    public int FadeOutDuration { get; init; }
}
