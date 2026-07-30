using Miko.Animation;

namespace Miko.Routing;

/// <summary>
/// 页面转场效果基类（ISSUE-108）。
/// <para>
/// 核心引擎只提供转场的底层支持：导航时保留旧页面树（leaving 层）、把新旧页面
/// 作为两个叠放图层共同绘制、逐帧推进转场时钟、完成后回收旧页面树。
/// 具体的转场效果（滑动、淡入淡出等）由组件库实现本类——如 Miko.Ionic 按平台
/// mode 提供 iOS/Android 风格的页面推入/返回转场。
/// </para>
/// <para>
/// 实现约定：在 <see cref="OnStart"/> 中设置初始图层状态与叠放次序
/// （<see cref="NavigationTransitionContext.EnteringBelow"/>）；引擎随后逐帧调用
/// <see cref="Apply"/>（progress 为已按 <see cref="TimingFunction"/> 缓动的 0..1，
/// 首帧保证恰好为 0、自然完成的末帧保证恰好为 1）。
/// </para>
/// </summary>
public abstract class NavigationTransition
{
    /// <summary>
    /// 转场时长（秒）。必须为正数；非正时长会被引擎视为无转场（瞬时切换）。
    /// </summary>
    public abstract float Duration { get; }

    /// <summary>
    /// 缓动函数。引擎先把线性进度（已消耗时间 / <see cref="Duration"/>）缓动化，
    /// 再把缓动后的进度交给 <see cref="Apply"/>。
    /// </summary>
    public virtual TimingFunction TimingFunction => TimingFunction.Ease;

    /// <summary><see cref="Animation.TimingFunction.CubicBezier"/> 时的贝塞尔参数。</summary>
    public virtual CubicBezierParams? CubicBezier => null;

    /// <summary>
    /// 转场开始（首帧绘制前调用一次）。在此设置各层初始偏移/透明度与叠放次序。
    /// </summary>
    public virtual void OnStart(NavigationTransitionContext context) { }

    /// <summary>
    /// 逐帧应用转场状态：把 leaving/entering 两层的偏移与透明度写入
    /// <paramref name="context"/>，引擎据此绘制两个图层。
    /// </summary>
    /// <param name="progress">已缓动的进度（0..1）。</param>
    public abstract void Apply(NavigationTransitionContext context, float progress);

    /// <summary>
    /// 转场自然完成（进度到达 1）时调用。被新导航打断（取消）时不调用。
    /// </summary>
    public virtual void OnEnd(NavigationTransitionContext context) { }
}
