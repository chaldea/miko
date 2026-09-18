using Miko.Core;
using Miko.Layout;

namespace Miko.Platform;

/// <summary>
/// 边界拉伸回弹（橡皮筋）。装饰另一个 <see cref="IScrollBehavior"/>：怎么滚由内层决定，
/// 本类只接管内层<b>没能消费掉的越界余量</b>（ISSUE-135）。
///
/// <para>
/// 只对开启了 <c>overscroll-effect: elastic</c> 的容器生效
/// （<see cref="Styling.Style.OverscrollEffect"/>）。样式默认 <c>None</c>，因此把本类装在宿主上
/// 不会改变任何既有页面的表现——没开启时它是纯透传。
/// </para>
///
/// <para><b>位移是 paint-only 的</b>：拉伸量写在 <see cref="LayoutBox.OverscrollY"/> 上，不碰
/// <c>ScrollTop</c>，因此不触发重排、不派发 scroll 事件、不移动滚动条、也不会被滚动状态恢复
/// 携带到下一次重建。每帧推进用 <see cref="MikoEngine.RequestRepaint"/> 而非
/// <c>InvalidateElement</c>——回弹期间每帧重排整棵树是没必要的开销。</para>
/// </summary>
public sealed class ElasticScrollBehavior : IScrollGestureBehavior
{
    /// <summary>
    /// 拉伸上限占容器该轴尺寸的比例。越界增量按 <c>1 - |当前拉伸| / 上限</c> 衰减后累加，
    /// 因此拉伸渐近饱和于该上限而非线性跟手——与 iOS 手感一致，也和
    /// <c>IonSlides.ApplyResistance</c> 的边缘阻尼同一思路。
    /// </summary>
    private const float MaxStretchRatio = 0.35f;

    /// <summary>位移低于此阈值（px）且速度低于 <see cref="SettleVelocity"/> 时判定回弹结束。</summary>
    private const float SettleDisplacement = 0.5f;

    /// <summary>回弹结束的速度阈值（px/s）。低于它继续弹也不会有可见位移。</summary>
    private const float SettleVelocity = 10f;

    /// <summary>
    /// 单帧积分步长上限（秒）。长帧拆成若干小步再积分：显式积分在 <c>dt</c> 偏大时会发散，
    /// 恢复帧（宿主空闲跳帧后的第一帧）正是这种情形。
    /// </summary>
    private const float MaxIntegrationStep = 1f / 120f;

    private readonly IScrollBehavior _inner;
    private readonly IScrollGestureBehavior? _innerGesture;
    private readonly float _stiffness;
    private readonly float _damping;

    /// <summary>当前被拉伸的容器。为 null 表示没有拉伸，也没有待回弹的弹簧。</summary>
    private LayoutBox? _box;

    /// <summary>回弹速度（px/s）。手指按住期间恒为 0——跟手位移不需要速度状态。</summary>
    private float _velocityX, _velocityY;

    private bool _dragging;
    private float _x, _y;

    /// <param name="inner">
    /// 实际执行滚动的行为，默认 <see cref="InertialScrollBehavior"/>。其惯性在撞到内容尽头时
    /// 会被本类接管（剩余速度转成拉伸），因此两者组合即可得到「甩到底 → 拉伸 → 回弹」的完整手感。
    /// </param>
    /// <param name="stiffness">
    /// 回弹弹簧的劲度系数（s⁻²）。越大回弹越快。
    /// </param>
    /// <param name="damping">
    /// 回弹弹簧的阻尼系数（s⁻¹）。临界阻尼为 <c>2·√stiffness</c>；默认 27 略高于
    /// <c>2·√180 ≈ 26.8</c>，取轻微过阻尼以确保不回穿——回穿在橡皮筋上表现为边界处抖动。
    /// </param>
    public ElasticScrollBehavior(IScrollBehavior? inner = null, float stiffness = 180f, float damping = 27f)
    {
        if (stiffness <= 0f)
            throw new ArgumentOutOfRangeException(nameof(stiffness), stiffness, "Stiffness must be positive.");
        if (damping <= 0f)
            throw new ArgumentOutOfRangeException(nameof(damping), damping, "Damping must be positive.");

        _inner = inner ?? new InertialScrollBehavior();
        _innerGesture = _inner as IScrollGestureBehavior;
        _stiffness = stiffness;
        _damping = damping;
    }

    /// <summary>被装饰的滚动行为。</summary>
    public IScrollBehavior Inner => _inner;

    /// <summary>当前拉伸中的容器（没有则为 null）。供宿主与测试观察，不用于驱动。</summary>
    public LayoutBox? StretchedBox => _box;

    /// <summary>
    /// 手指按住期间的拉伸也算待办工作：它虽不自行推进，但松手那一刻必须已经有帧在跑，
    /// 否则回弹的第一帧无从触发。
    /// </summary>
    public bool HasPendingWork => (_innerGesture?.HasPendingWork ?? false) || _box != null;

    public void PointerDown(float x, float y)
    {
        _x = x; _y = y;
        _dragging = true;
        // 上一次回弹还没走完就再次按下：接住当前拉伸继续跟手（不清零，否则画面会跳回），
        // 但速度归零——新手势的位移由新的拖拽决定。
        _velocityX = 0; _velocityY = 0;
        _innerGesture?.PointerDown(x, y);
    }

    public void PointerUp(float x, float y)
    {
        _x = x; _y = y;
        _dragging = false;
        _innerGesture?.PointerUp(x, y);
        // 拉伸着松手：弹簧从静止开始回弹。此处不设初速度——跟手拉伸的「速度」已经体现在
        // 位移里，再叠加会让回弹冲过头。
    }

    public void PointerCancel()
    {
        _dragging = false;
        _innerGesture?.PointerCancel();
        // 取消意味着这次交互不作数：拉伸必须立刻消失，不能留在画面上。
        ReleaseBox();
    }

    public bool ScrollBy(MikoEngine engine, float x, float y, float deltaX, float deltaY)
    {
        _x = x; _y = y;

        bool unstretched = false;
        // 已有拉伸时，反方向的增量先用来消解拉伸，剩下的才交给内层滚动。
        // 少了这一步，从拉伸状态往回拖会先滚动内容、拉伸却还挂着，松手后画面又弹一下。
        if (_box != null)
        {
            var (restX, restY) = ConsumeByUnstretching(deltaX, deltaY);
            unstretched = restX != deltaX || restY != deltaY;
            (deltaX, deltaY) = (restX, restY);

            if (unstretched && MathF.Abs(deltaX) <= 0.01f && MathF.Abs(deltaY) <= 0.01f)
            {
                engine.RequestRepaint();
                return true;
            }
        }

        var scrolled = _inner.ScrollBy(engine, x, y, deltaX, deltaY);
        var result = engine.LastScrollResult;

        // 只有手指按住时才跟手拉伸。滚轮（没有 PointerDown/Up 生命周期）拉了就没有松手事件来
        // 触发回弹，会永久停在拉伸状态；惯性阶段的越界另走 Update 里的接管路径。
        if (!_dragging || result.Box is not { IsElastic: true } box)
            return scrolled || unstretched;

        float stretchX = result.HasRemainingX ? Damp(result.RemainingX, box.OverscrollX, StretchLimitX(box)) : 0f;
        float stretchY = result.HasRemainingY ? Damp(result.RemainingY, box.OverscrollY, StretchLimitY(box)) : 0f;

        if (stretchX == 0f && stretchY == 0f)
            return scrolled || unstretched;

        Adopt(box);
        box.OverscrollX += stretchX;
        box.OverscrollY += stretchY;
        engine.RequestRepaint();
        return true;   // 画面确实动了——宿主据此继续出帧
    }

    public void Update(MikoEngine engine, float deltaTime)
    {
        var dt = Math.Clamp(deltaTime, 0f, 0.1f);
        if (dt <= 0f) return;

        AbsorbInnerFling(engine, dt);

        if (_box == null || _dragging) return;

        // 布局重建会换掉 LayoutBox 实例，旧盒子上的拉伸再也不会被绘制——继续弹它只是空转。
        if (!IsBoxLive(_box))
        {
            ReleaseBox();
            return;
        }

        StepSpring(_box, dt);
        engine.RequestRepaint();

        if (IsSettled(_box))
            ReleaseBox();
    }

    /// <summary>
    /// 内层惯性撞到内容尽头时，把剩余速度转成拉伸并接管（ISSUE-135 的「甩到底也要回弹」）。
    ///
    /// <para>做法是先让内层自己推进一帧，再通过 <see cref="MikoEngine.LastScrollResult"/> 看这一帧
    /// 有没有越界余量。必须在内层推进之后按本轮余量判断，而不能事后去问它还剩多少速度：
    /// <see cref="InertialScrollBehavior"/> 见到滚动被拒会当场把惯性清零。</para>
    /// </summary>
    private void AbsorbInnerFling(MikoEngine engine, float dt)
    {
        if (_innerGesture == null || _dragging || !_innerGesture.HasPendingWork) return;

        _innerGesture.Update(engine, dt);

        var result = engine.LastScrollResult;
        if (result.Box is not { IsElastic: true } box) return;
        if (!result.HasRemainingX && !result.HasRemainingY) return;

        // 余量是「这一帧本该走却没走成的距离」，除以帧长即撞墙瞬间的速度。
        Adopt(box);
        if (result.HasRemainingX) _velocityX = result.RemainingX / dt;
        if (result.HasRemainingY) _velocityY = result.RemainingY / dt;

        // 接管：终止内层惯性，之后由本类的弹簧独占推进，避免两者同时驱动同一个盒子。
        _innerGesture.PointerCancel();
        engine.RequestRepaint();
    }

    /// <summary>
    /// 把拉伸往零的方向消解掉增量中反向的部分，返回剩余的、应当交给内层滚动的增量。
    /// </summary>
    private (float x, float y) ConsumeByUnstretching(float deltaX, float deltaY)
    {
        var box = _box!;

        var (stretchX, restX) = Unstretch(box.OverscrollX, deltaX);
        var (stretchY, restY) = Unstretch(box.OverscrollY, deltaY);
        box.OverscrollX = stretchX;
        box.OverscrollY = stretchY;

        // 拉伸被拖回零：本次弹簧作废（速度也清掉），之后就是普通滚动。
        if (stretchX == 0f && stretchY == 0f)
            ReleaseBox();

        return (restX, restY);

        // 返回 (剩余拉伸, 未被消解的增量)。
        static (float stretch, float rest) Unstretch(float stretch, float delta)
        {
            // 同号说明是在继续加大拉伸，不该在这里消解。
            if (stretch == 0f || delta == 0f || MathF.Sign(stretch) == MathF.Sign(delta))
                return (stretch, delta);

            // 反号，故相加即抵消。
            if (MathF.Abs(delta) >= MathF.Abs(stretch))
                return (0f, delta + stretch);

            return (stretch + delta, 0f);
        }
    }

    /// <summary>
    /// 越界增量经阻尼后的实际拉伸量：越接近上限，同样的手指位移带来的拉伸越小，
    /// 使拉伸渐近饱和而非无限跟手。
    /// </summary>
    private static float Damp(float remaining, float current, float limit)
    {
        if (limit <= 0f) return 0f;
        var resistance = 1f - Math.Clamp(MathF.Abs(current) / limit, 0f, 1f);
        return remaining * resistance;
    }

    private static float StretchLimitX(LayoutBox box) => box.BoxModel.PaddingBox.Width * MaxStretchRatio;
    private static float StretchLimitY(LayoutBox box) => box.BoxModel.PaddingBox.Height * MaxStretchRatio;

    /// <summary>
    /// 推进一帧弹簧。用「阻尼谐振子」而非指数衰减：后者没有速度这一状态，
    /// 接不住惯性撞墙转化来的初速度。长帧拆成不超过 <see cref="MaxIntegrationStep"/> 的小步，
    /// 避免显式积分在大步长下发散。
    /// </summary>
    private void StepSpring(LayoutBox box, float dt)
    {
        int steps = Math.Max(1, (int)MathF.Ceiling(dt / MaxIntegrationStep));
        float h = dt / steps;

        for (int i = 0; i < steps; i++)
        {
            // 半隐式欧拉：先更新速度再用新速度更新位移，在弹簧上比显式欧拉稳定得多。
            _velocityX += (-_stiffness * box.OverscrollX - _damping * _velocityX) * h;
            _velocityY += (-_stiffness * box.OverscrollY - _damping * _velocityY) * h;
            box.OverscrollX += _velocityX * h;
            box.OverscrollY += _velocityY * h;
        }
    }

    private bool IsSettled(LayoutBox box) =>
        MathF.Abs(box.OverscrollX) < SettleDisplacement && MathF.Abs(_velocityX) < SettleVelocity &&
        MathF.Abs(box.OverscrollY) < SettleDisplacement && MathF.Abs(_velocityY) < SettleVelocity;

    private void Adopt(LayoutBox box)
    {
        // 换容器（手势移到了另一个滚动区）时，旧容器的拉伸要抹掉，否则会僵在画面上。
        if (!ReferenceEquals(_box, box))
        {
            ClearStretch(_box);
            _velocityX = 0; _velocityY = 0;
        }
        _box = box;
    }

    private void ReleaseBox()
    {
        ClearStretch(_box);
        _box = null;
        _velocityX = 0; _velocityY = 0;
    }

    private static void ClearStretch(LayoutBox? box)
    {
        if (box == null) return;
        box.OverscrollX = 0f;
        box.OverscrollY = 0f;
    }

    /// <summary>该盒子是否仍在当前布局树中（重建后旧实例不再被绘制）。</summary>
    private static bool IsBoxLive(LayoutBox box) => ReferenceEquals(box.Element.LayoutBox, box);
}
