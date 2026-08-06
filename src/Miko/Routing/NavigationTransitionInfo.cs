namespace Miko.Routing;

/// <summary>
/// 引擎执行一次导航所需的全部信息：导航方向 + 来源/目标路径 + 可选的页面转场效果。
/// 由交互控制器在路由重建时从 <see cref="NavigationEventArgs"/> 构造，随
/// <c>MikoEngine.Initialize</c> 传入。
/// <para>
/// <see cref="Transition"/> 为 null 表示本次导航不做页面转场（如 Tab 根级切换，
/// 或未配置转场的应用）——但方向与路径仍然有效：引擎据此维护按路径的滚动快照，
/// 使返回上一页时能恢复其滚动位置（ISSUE-118）。因此本类型描述的是「一次导航」，
/// 而不仅是「一次转场」；转场（ISSUE-108）只是其中可选的一部分。
/// </para>
/// </summary>
public sealed record NavigationTransitionInfo(
    NavigationTransition? Transition,
    NavigationDirection Direction,
    string FromPath,
    string ToPath);
