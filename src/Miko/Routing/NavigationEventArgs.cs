namespace Miko.Routing;

/// <summary>
/// 一次导航的描述：来源/目标路径、导航方向，以及可选的页面转场效果（ISSUE-108）。
/// 转场效果由调用方（组件库或应用，如 Miko.Ionic）按方向提供；引擎只负责执行。
/// </summary>
public sealed class NavigationEventArgs
{
    /// 导航前的路径。
    public required string FromPath { get; init; }

    /// 导航到的路径。
    public required string ToPath { get; init; }

    /// 导航方向（压栈 / 返回 / 根级切换）。
    public NavigationDirection Direction { get; init; }

    /// 页面转场效果；为 null 时本次导航瞬时切换（如 Tab 切换）。
    public NavigationTransition? Transition { get; init; }
}
