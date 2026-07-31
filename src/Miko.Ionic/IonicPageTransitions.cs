using Miko.Animation;
using Miko.Routing;

namespace Miko.Ionic;

/// <summary>
/// Ionic 风格的页面转场效果集（issues/ion-animation）：把 Ionic 的
/// <c>ios.transition.ts</c> / <c>md.transition.ts</c> 移植为 ISSUE-108 的
/// <see cref="NavigationTransition"/> 图层动画（逐帧写入 leaving/entering 两层的
/// 偏移与不透明度，不重排布局）。
/// <para>
/// 每个效果都有前进（Push）与返回（Pop）两个方向：返回方向把
/// <see cref="NavigationTransitionContext.EnteringBelow"/> 置为 true
/// （旧页面在上滑出，露出下方的新页面）。实例无状态，以静态单例复用。
/// 按 mode 取用：<see cref="Push(string)"/> / <see cref="Pop(string)"/>。
/// </para>
/// <para>
/// 图层模型只支持整体偏移与不透明度：Ionic 原实现中的新页面边缘阴影（iOS）等
/// 装饰性细节在此省略。
/// </para>
/// </summary>
public static class IonicPageTransitions
{
    /// <summary>按 Ionic mode 取前进（push）转场：<c>"ios"</c> → iOS 推入，其余（md）→ MD 淡入上移。</summary>
    public static NavigationTransition Push(string mode)
        => mode == "ios" ? IosPageTransition.Push : MdPageTransition.Push;

    /// <summary>按 Ionic mode 取返回（pop）转场。</summary>
    public static NavigationTransition Pop(string mode)
        => mode == "ios" ? IosPageTransition.Pop : MdPageTransition.Pop;

    /// <summary>
    /// iOS 推入/返回转场（对应 Ionic <c>ios.transition.ts</c>：540ms，
    /// cubic-bezier(0.32, 0.72, 0, 1)）。
    /// <para>
    /// 前进：新页面从右侧滑入盖在旧页面上，旧页面视差左移 30%；
    /// 返回：旧页面在上方向右滑出，新页面从 -30% 处复位。
    /// </para>
    /// </summary>
    public sealed class IosPageTransition : NavigationTransition
    {
        /// <summary>前进方向单例。</summary>
        public static IosPageTransition Push { get; } = new(reverse: false);

        /// <summary>返回方向单例。</summary>
        public static IosPageTransition Pop { get; } = new(reverse: true);

        private readonly bool _reverse;

        private IosPageTransition(bool reverse) => _reverse = reverse;

        public override float Duration => 0.54f;

        public override TimingFunction TimingFunction => TimingFunction.CubicBezier;

        public override CubicBezierParams? CubicBezier => new(0.32f, 0.72f, 0f, 1f);

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
    /// MD 推入/返回转场（对应 Ionic <c>md.transition.ts</c>）。
    /// <para>
    /// 前进：新页面自下方 40px 处上移并淡入，盖在静止的旧页面上
    /// （280ms，cubic-bezier(0.36, 0.66, 0.04, 1)）。
    /// 返回：旧页面盖在新页面上方淡出（200ms，cubic-bezier(0.47, 0, 0.745, 0.715)）。
    /// </para>
    /// </summary>
    public sealed class MdPageTransition : NavigationTransition
    {
        /// <summary>新页面上移距离（像素），对应 Ionic 的 <c>OFF_BOTTOM = '40px'</c>。</summary>
        private const float OffBottom = 40f;

        /// <summary>前进方向单例。</summary>
        public static MdPageTransition Push { get; } = new(reverse: false);

        /// <summary>返回方向单例。</summary>
        public static MdPageTransition Pop { get; } = new(reverse: true);

        private readonly bool _reverse;

        private MdPageTransition(bool reverse) => _reverse = reverse;

        // Ionic 前进/返回使用不同的时长与缓动（见 md.transition.ts 的 backDirection 分支）。
        public override float Duration => _reverse ? 0.2f : 0.28f;

        public override TimingFunction TimingFunction => TimingFunction.CubicBezier;

        public override CubicBezierParams? CubicBezier => _reverse
            ? new(0.47f, 0f, 0.745f, 0.715f)
            : new(0.36f, 0.66f, 0.04f, 1f);

        public override void OnStart(NavigationTransitionContext context)
        {
            // 返回：新页面（底下的页面）静止不动，旧页面在其上方淡出。
            context.EnteringBelow = _reverse;
        }

        public override void Apply(NavigationTransitionContext context, float progress)
        {
            if (_reverse)
            {
                context.LeavingOpacity = 1f - progress;
            }
            else
            {
                context.EnteringOffsetY = OffBottom * (1f - progress);
                context.EnteringOpacity = progress;
            }
        }
    }
}
