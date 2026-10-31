namespace Miko.Platform;

/// <summary>
/// 系统栏的显隐（沉浸式全屏）。供组件在全屏交互期间请求平台隐藏状态栏与导航栏——例如视频
/// 播放器全屏时让画面占满整块屏幕，而不是在状态栏、刘海区留下几条边。
///
/// <para>
/// 各平台的含义：Android 隐藏状态栏与导航栏（从屏幕边缘滑动可临时呼出），并让内容延伸进
/// 刘海 / 挖孔区；iOS 隐藏状态栏并自动隐藏 Home 指示条；桌面窗口内没有系统栏，是空操作。
/// 隐藏后宿主报告的安全区随之变化（不再含状态栏，仍含刘海），组件用 <c>env(safe-area-inset-*)</c>
/// 让按钮避开刘海即可。
/// </para>
///
/// <para>
/// 与 <see cref="IPlatformService"/> 的定位一致：<b>同步调用、不支持时静默降级</b>，组件无需判空。
/// </para>
/// </summary>
public interface ISystemBarsService
{
    /// <summary>
    /// 隐藏系统栏，进入沉浸式全屏。与 <see cref="Show"/> 成对使用，引用计数——嵌套请求不会互相干扰，
    /// 最后一个请求解除后才恢复。
    /// </summary>
    void Hide();

    /// <summary>解除一次 <see cref="Hide"/>。已无隐藏请求时为空操作。</summary>
    void Show();
}

/// <summary>
/// <see cref="ISystemBarsService"/> 的空实现：所有调用都不做任何事。
/// 桌面以及未接入平台的宿主使用它，因此组件无需判空。
/// </summary>
public sealed class NullSystemBarsService : ISystemBarsService
{
    public void Hide() { }
    public void Show() { }
}
