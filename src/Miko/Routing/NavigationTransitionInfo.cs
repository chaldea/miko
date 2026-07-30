namespace Miko.Routing;

/// <summary>
/// 引擎执行一次页面转场所需的全部信息（ISSUE-108）：
/// 转场效果 + 导航方向 + 来源/目标路径。由交互控制器在路由重建时从
/// <see cref="NavigationEventArgs"/> 构造，随 <c>MikoEngine.Initialize</c> 传入。
/// </summary>
public sealed record NavigationTransitionInfo(
    NavigationTransition Transition,
    NavigationDirection Direction,
    string FromPath,
    string ToPath);
