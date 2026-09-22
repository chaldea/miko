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
/// <param name="FromPageKey">
/// 来源页的<b>身份</b>键（通常是路由解析出的组件类型名），由交互控制器填入；为 null 时
/// 回落到 <paramref name="FromPath"/>。
/// <para>之所以不能只用路径：同一个页面可以挂在多个路由模板上
/// （<c>@page "/"</c> + <c>@page "/home"</c> 是 Razor 里极常见的写法）。按<b>路径</b>存取滚动
/// 快照时，启动落在 <c>/</c> 而 Tab 按钮导航到 <c>/home</c>，同一个首页于是有了两个键，
/// 切回来时取不到那张快照——ISSUE-144 问题3 在引擎侧修好之后，真机上仍然会重置，
/// 就是这个原因。</para>
/// </param>
/// <param name="ToPageKey">目标页的身份键；为 null 时回落到 <paramref name="ToPath"/>。</param>
public sealed record NavigationTransitionInfo(
    NavigationTransition? Transition,
    NavigationDirection Direction,
    string FromPath,
    string ToPath,
    string? FromPageKey = null,
    string? ToPageKey = null)
{
    /// <summary>来源页的滚动快照键（<see cref="FromPageKey"/>，缺省为 <see cref="FromPath"/>）。</summary>
    public string FromKey => FromPageKey ?? FromPath;

    /// <summary>目标页的滚动快照键（<see cref="ToPageKey"/>，缺省为 <see cref="ToPath"/>）。</summary>
    public string ToKey => ToPageKey ?? ToPath;
}
