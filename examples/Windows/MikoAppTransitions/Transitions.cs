using Miko.Animation;
using Miko.Routing;

namespace MikoAppTransitions;

/// <summary>一种演示用转场效果的元数据（key = /detail/{key} 路由段）。</summary>
public sealed record EffectEntry(string Key, string Title, string Subtitle);

/// <summary>
/// 本示例的页面转场效果集（ISSUE-108）。
/// <para>
/// 核心引擎只提供转场底层支持（双树图层绘制 + 转场时钟）；具体效果由应用/组件库
/// 实现 <see cref="NavigationTransition"/>：在 <see cref="NavigationTransition.Apply"/>
/// 中逐帧写入 leaving/entering 两层的偏移与不透明度，用
/// <see cref="NavigationTransitionContext.EnteringBelow"/> 控制叠放次序。
/// </para>
/// <para>
/// 每种效果都有"前进"（Push）与"返回"（Pop）两个方向：返回方向通常把
/// EnteringBelow 置为 true（旧页面在上滑出，露出下方的新页面），并把位移取反。
/// 转场实例无状态，可静态复用。
/// </para>
/// </summary>
public static class PageTransitions
{
    /// <summary>本示例演示的转场效果目录（key 即 /detail/{key} 路由段）。</summary>
    public static readonly EffectEntry[] Effects =
    {
        new("ios",   "iOS 推入",  "新页面从右侧滑入，旧页面视差左移 30%"),
        new("slide", "整体滑动",  "新旧页面像相邻两屏一样一起水平滑动"),
        new("fade",  "淡入淡出",  "新页面以透明度淡入，盖在静止的旧页面上"),
        new("modal", "底部滑入",  "新页面从底部向上滑入（Modal 风格）"),
        new("none",  "无转场",    "瞬时切换（Tab 切换 / Root 导航场景）"),
    };

    public static EffectEntry Get(string key)
        => Effects.FirstOrDefault(e => e.Key == key) ?? Effects[0];

    /// <summary>按效果键创建对应方向的转场实例（"none" 返回 null = 瞬时切换）。</summary>
    public static NavigationTransition? Create(string key, bool reverse) => key switch
    {
        "ios" => reverse ? IosPushTransition.Pop : IosPushTransition.Push,
        "slide" => reverse ? SlideTransition.Pop : SlideTransition.Push,
        "fade" => FadeTransition.Shared,
        "modal" => reverse ? ModalTransition.Pop : ModalTransition.Push,
        _ => null,
    };

    /// <summary>
    /// iOS 风格推入：新页面从右侧滑入盖在旧页面上，旧页面视差左移 30%；
    /// 返回时方向反转（旧页面在上方向右滑出，新页面从 -30% 处复位）。
    /// </summary>
    public sealed class IosPushTransition : NavigationTransition
    {
        public static IosPushTransition Push { get; } = new(reverse: false);
        public static IosPushTransition Pop { get; } = new(reverse: true);

        private readonly bool _reverse;
        private IosPushTransition(bool reverse) => _reverse = reverse;

        public override float Duration => 0.35f;
        public override TimingFunction TimingFunction => TimingFunction.CubicBezier;
        public override CubicBezierParams? CubicBezier => new(0.4f, 0f, 0.2f, 1f);

        public override void OnStart(NavigationTransitionContext context)
        {
            // 返回：新页面（上一页）在下方，旧页面在其上方向右滑出。
            context.EnteringBelow = _reverse;
        }

        public override void Apply(NavigationTransitionContext context, float progress)
        {
            float w = context.ViewportWidth;
            if (_reverse)
            {
                context.LeavingOffsetX = w * progress;
                context.EnteringOffsetX = -w * 0.3f * (1f - progress);
            }
            else
            {
                context.EnteringOffsetX = w * (1f - progress);
                context.LeavingOffsetX = -w * 0.3f * progress;
            }
        }
    }

    /// <summary>
    /// 整体滑动：新旧页面像同一长条上的相邻两屏一样一起水平滑动（前进左移、返回右移）。
    /// </summary>
    public sealed class SlideTransition : NavigationTransition
    {
        public static SlideTransition Push { get; } = new(reverse: false);
        public static SlideTransition Pop { get; } = new(reverse: true);

        private readonly bool _reverse;
        private SlideTransition(bool reverse) => _reverse = reverse;

        public override float Duration => 0.3f;
        public override TimingFunction TimingFunction => TimingFunction.EaseInOut;

        public override void Apply(NavigationTransitionContext context, float progress)
        {
            float w = context.ViewportWidth;
            if (_reverse)
            {
                context.LeavingOffsetX = w * progress;
                context.EnteringOffsetX = -w * (1f - progress);
            }
            else
            {
                context.EnteringOffsetX = w * (1f - progress);
                context.LeavingOffsetX = -w * progress;
            }
        }
    }

    /// <summary>
    /// 淡入淡出：新页面以透明度 0 → 1 淡入，盖在保持静止的旧页面上（两个方向同效果）。
    /// </summary>
    public sealed class FadeTransition : NavigationTransition
    {
        public static FadeTransition Shared { get; } = new();

        private FadeTransition() { }

        public override float Duration => 0.4f;
        public override TimingFunction TimingFunction => TimingFunction.Ease;

        public override void Apply(NavigationTransitionContext context, float progress)
        {
            context.EnteringOpacity = progress;
        }
    }

    /// <summary>
    /// Modal 风格：新页面从底部向上滑入盖在旧页面上；关闭（返回）时向下滑出。
    /// </summary>
    public sealed class ModalTransition : NavigationTransition
    {
        public static ModalTransition Push { get; } = new(reverse: false);
        public static ModalTransition Pop { get; } = new(reverse: true);

        private readonly bool _reverse;
        private ModalTransition(bool reverse) => _reverse = reverse;

        public override float Duration => 0.3f;
        public override TimingFunction TimingFunction => TimingFunction.CubicBezier;
        public override CubicBezierParams? CubicBezier => new(0.32f, 0.72f, 0f, 1f);

        public override void OnStart(NavigationTransitionContext context)
        {
            // 关闭：新页面（底下的页面）在下方，旧页面（Modal）在其上方向下滑出。
            context.EnteringBelow = _reverse;
        }

        public override void Apply(NavigationTransitionContext context, float progress)
        {
            float h = context.ViewportHeight;
            if (_reverse)
                context.LeavingOffsetY = h * progress;
            else
                context.EnteringOffsetY = h * (1f - progress);
        }
    }
}
