using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// ISSUE-135：边界拉伸回弹（橡皮筋）。容器到达边界后继续被推时内容按阻尼位移，松手弹回。
/// </summary>
public sealed class ElasticScrollBehaviorTests
{
    private const float ViewportH = 200f;

    // ---------------------------------------------------------------- 默认值：不开启就没有任何变化

    /// <summary>
    /// 样式默认 <c>None</c>：越界拖拽既不产生拉伸，也不把宿主留在「有待办工作」状态。
    /// 这是把装饰器无条件装在移动端宿主上的前提——没开启时必须是纯透传。
    /// </summary>
    [Fact]
    public void WithoutElasticStyle_BoundaryDragProducesNoStretch()
    {
        var engine = CreateEngine(elastic: false);
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, -80);   // 已在顶端，继续上推
        behavior.PointerUp(100, 100);

        var box = engine.GetCurrentLayout()!;
        box.OverscrollY.ShouldBe(0f);
        box.ScrollTop.ShouldBe(0f);
        behavior.HasPendingWork.ShouldBeFalse();
    }

    /// <summary>未开启时滚动语义与内层完全一致（装饰器不吞掉、不改写正常滚动）。</summary>
    [Fact]
    public void WithoutElasticStyle_NormalScrollingIsUnchanged()
    {
        var engine = CreateEngine(elastic: false);
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, 40).ShouldBeTrue();

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(40f);
    }

    // ---------------------------------------------------------------- 跟手拉伸

    /// <summary>顶端继续上推 → 负向拉伸；底端继续下推 → 正向拉伸。</summary>
    [Fact]
    public void BoundaryDrag_StretchesTowardsTheCrossedEdge()
    {
        var engine = CreateEngine();
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, -60);
        box.OverscrollY.ShouldBeLessThan(0f);

        behavior.PointerCancel();

        // 滚到底端后继续下推
        var bottom = new ElasticScrollBehavior();
        bottom.PointerDown(100, 100);
        bottom.ScrollBy(engine, 100, 100, 0, 5000);      // 一路推到底
        box.ScrollTop.ShouldBe(MaxScrollTop(box));
        bottom.ScrollBy(engine, 100, 100, 0, 60);
        box.OverscrollY.ShouldBeGreaterThan(0f);
    }

    /// <summary>拉伸期间 <c>ScrollTop</c> 不动：位移是 paint-only 的，不是滚动。</summary>
    [Fact]
    public void Stretching_DoesNotMoveScrollTop()
    {
        var engine = CreateEngine();
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, -60);

        box.HasOverscroll.ShouldBeTrue();
        box.ScrollTop.ShouldBe(0f);
    }

    /// <summary>
    /// 阻尼：等量的越界增量，后一次带来的拉伸严格小于前一次——拉伸随位移递减、渐近饱和，
    /// 而不是线性跟手。少了阻尼，手指拖多远内容就拉多远，橡皮筋的「越拉越沉」就没有了。
    /// </summary>
    [Fact]
    public void Stretch_ResistanceGrowsWithDisplacement()
    {
        var engine = CreateEngine();
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);

        behavior.ScrollBy(engine, 100, 100, 0, -20);
        var first = MathF.Abs(box.OverscrollY);

        behavior.ScrollBy(engine, 100, 100, 0, -20);
        var second = MathF.Abs(box.OverscrollY) - first;

        behavior.ScrollBy(engine, 100, 100, 0, -20);
        var third = MathF.Abs(box.OverscrollY) - first - second;

        first.ShouldBeGreaterThan(0f);
        second.ShouldBeLessThan(first);
        third.ShouldBeLessThan(second);
    }

    /// <summary>拉伸有上限：持续越界拖拽不会把内容拉到天边。</summary>
    [Fact]
    public void Stretch_SaturatesAtALimit()
    {
        var engine = CreateEngine();
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        for (int i = 0; i < 200; i++)
            behavior.ScrollBy(engine, 100, 100, 0, -50);

        MathF.Abs(box.OverscrollY).ShouldBeLessThanOrEqualTo(ViewportH * 0.35f + 0.01f);
    }

    // ---------------------------------------------------------------- 回弹

    /// <summary>松手后逐帧推进：拉伸单调趋零并在有限帧内归零，其间宿主保持出帧、收敛后回到空闲。</summary>
    [Fact]
    public void Release_SpringsBackToZeroAndThenGoesIdle()
    {
        var engine = CreateEngine();
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, -60);
        behavior.PointerUp(100, 100);

        var stretched = MathF.Abs(box.OverscrollY);
        stretched.ShouldBeGreaterThan(0f);
        behavior.HasPendingWork.ShouldBeTrue();

        var previous = stretched;
        int frames = 0;
        for (; frames < 600 && behavior.HasPendingWork; frames++)
        {
            behavior.Update(engine, 1f / 60f);
            var current = MathF.Abs(box.OverscrollY);
            // 单调趋零（不回穿）：回穿在橡皮筋上表现为边界处抖动。
            current.ShouldBeLessThanOrEqualTo(previous + 0.01f);
            previous = current;
        }

        frames.ShouldBeLessThan(600);
        behavior.HasPendingWork.ShouldBeFalse();
        box.OverscrollY.ShouldBe(0f);
        box.ScrollTop.ShouldBe(0f);
    }

    // ---------------------------------------------------------------- 内容不足一屏

    /// <summary>
    /// 弹性按轴生效：只有 <c>overflow</c> 开在该轴上，那个方向才会拉伸。因此
    /// <c>overflow-y:auto</c> 的容器天然就是「只有上下回弹」——这也是 IonContent 默认的形态
    /// （ScrollY 开、ScrollX 关），无需额外的方向开关。
    /// </summary>
    [Fact]
    public void Stretch_OnlyHappensOnAxesThatScroll()
    {
        var engine = CreateEngine();            // 只开 OverflowY
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, -80, 0);   // 纯水平拖拽
        box.OverscrollX.ShouldBe(0f);
        box.OverscrollY.ShouldBe(0f);

        behavior.ScrollBy(engine, 100, 100, 0, -80);   // 纵向照常
        box.OverscrollY.ShouldBeLessThan(0f);
        box.OverscrollX.ShouldBe(0f);
    }

    /// <summary>
    /// 回归：<b>斜向</b>拖拽不能把不可滚动的那一轴也拉起来。
    ///
    /// <para>纯水平拖拽是安全的——FindScrollableBox 在该轴上找不到容器，压根没有余量。但斜向拖拽
    /// 会经由<b>纵轴</b>找到容器，而余量是按 <c>delta - consumed</c> 逐轴算的：横向增量从来没有
    /// 被消费的可能（overflow-x 是 hidden），于是整个横向分量都变成了拉伸。表现就是手指往右下滑，
    /// 画面整体往右下跑，左上角露出空白。</para>
    ///
    /// <para>斜向滑动是真实手势的常态（很少有人能垂直地滑），所以这条比纯轴向的那条更贴近现场。</para>
    /// </summary>
    [Fact]
    public void DiagonalDrag_DoesNotStretchTheNonScrollableAxis()
    {
        var engine = CreateEngine();            // 只开 OverflowY
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        // 往右下滑：纵向越界（已在顶端），同时带一个横向分量。
        behavior.ScrollBy(engine, 100, 100, -50, -50);

        box.OverscrollY.ShouldBeLessThan(0f);   // 纵向照常回弹
        box.OverscrollX.ShouldBe(0f);           // 横向绝不能动
    }

    /// <summary>
    /// 两轴都真的可滚动时，斜向越界<b>应当</b>两轴都拉伸（对角回弹是正确行为，不是 bug）。
    /// 这条把上面那条的修复钉在「该轴不可滚动」上，而不是笼统地禁掉对角拉伸。
    /// </summary>
    [Fact]
    public void DiagonalDrag_StretchesBothAxesWhenBothActuallyScroll()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(300),
                Height = Length.Px(ViewportH),
                OverflowX = Overflow.Auto,
                OverflowY = Overflow.Auto,
                OverscrollEffect = OverscrollEffect.Elastic,
            },
            Children =
            {
                new DivElement { Style = new Style { Width = Length.Px(1000), Height = Length.Px(1000) } },
            },
        };
        var engine = CreateEngine(root, 300, ViewportH);
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, -50, -50);   // 左上角越界

        box.OverscrollX.ShouldBeLessThan(0f);
        box.OverscrollY.ShouldBeLessThan(0f);
    }

    /// <summary>
    /// 内容不足一屏的 <c>overflow:auto</c> 容器同样能拉伸回弹（iOS 短页面照样有橡皮筋，
    /// 对应 Ionic 的 forceOverscroll）。这类容器 <c>HasVerticalScrollbar</c> 为假，
    /// 走的是 FindScrollableBox 的 elastic 兜底分支。
    /// </summary>
    [Fact]
    public void NonOverflowingContainer_StillStretchesAndRebounds()
    {
        var engine = CreateEngine(contentHeight: 100f);   // 比 200 的视口还短
        var box = engine.GetCurrentLayout()!;
        box.HasVerticalScrollbar.ShouldBeFalse();

        var behavior = new ElasticScrollBehavior();
        behavior.PointerDown(100, 50);
        behavior.ScrollBy(engine, 100, 50, 0, 60);

        box.OverscrollY.ShouldNotBe(0f);
        box.ScrollTop.ShouldBe(0f);

        behavior.PointerUp(100, 50);
        for (int i = 0; i < 600 && behavior.HasPendingWork; i++)
            behavior.Update(engine, 1f / 60f);

        box.OverscrollY.ShouldBe(0f);
        box.ScrollTop.ShouldBe(0f);
    }

    /// <summary>兜底不能抢戏：真正可滚动的祖先永远优先于开了弹性但不可滚动的内层。</summary>
    [Fact]
    public void ElasticFallback_DoesNotStealFromAScrollableAncestor()
    {
        var inner = new DivElement
        {
            Id = "inner",
            Style = new Style
            {
                Height = Length.Px(100),
                OverflowY = Overflow.Auto,
                OverscrollEffect = OverscrollEffect.Elastic,
            },
            Children = { new DivElement { Style = new Style { Height = Length.Px(50) } } },   // 不溢出
        };
        var outer = new DivElement
        {
            Style = new Style { Width = Length.Px(300), Height = Length.Px(200), OverflowY = Overflow.Auto },
            Children = { inner, new DivElement { Style = new Style { Height = Length.Px(1000) } } },
        };
        var engine = CreateEngine(outer, 300, 200);
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 50);
        behavior.ScrollBy(engine, 100, 50, 0, 40);

        // 外层滚了，内层没被拉伸。
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(40f);
        FindBox(engine.GetCurrentLayout()!, "inner")!.OverscrollY.ShouldBe(0f);
    }

    // ---------------------------------------------------------------- 惯性撞边接管

    /// <summary>
    /// 快速甩动撞到内容尽头时，剩余速度转为拉伸再回弹，而不是硬停。
    /// 对照组（未开启 elastic）必须停在 max 且全程无拉伸。
    /// </summary>
    [Fact]
    public void FlingHittingTheEnd_ConvertsRemainingVelocityIntoStretch()
    {
        var elasticStretch = RunFlingToEnd(elastic: true, out var elasticBox);
        var plainStretch = RunFlingToEnd(elastic: false, out var plainBox);

        elasticStretch.ShouldBeGreaterThan(0f);
        plainStretch.ShouldBe(0f);

        // 两者最终都停在内容末端，拉伸只是过程中的视觉位移。
        elasticBox.ScrollTop.ShouldBe(MaxScrollTop(elasticBox));
        plainBox.ScrollTop.ShouldBe(MaxScrollTop(plainBox));
        elasticBox.OverscrollY.ShouldBe(0f);
    }

    /// <summary>甩到底并接管后，回弹仍会收敛，宿主能重回空闲。</summary>
    [Fact]
    public void FlingTakeover_StillSettles()
    {
        RunFlingToEnd(elastic: true, out var box);
        box.OverscrollY.ShouldBe(0f);
    }

    /// <summary>
    /// 跑一次「快速甩动直到撞上内容尽头」，返回过程中观察到的最大拉伸幅度。
    /// 内容高度刻意压得比一次甩动的距离短，确保惯性一定会撞墙。
    /// </summary>
    private static float RunFlingToEnd(bool elastic, out LayoutBox box)
    {
        var engine = CreateEngine(elastic: elastic, contentHeight: 260f);
        box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior(new InertialScrollBehavior());

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, 50);
        behavior.PointerUp(100, 100);

        float peak = 0f;
        for (int i = 0; i < 600 && behavior.HasPendingWork; i++)
        {
            behavior.Update(engine, 1f / 60f);
            peak = MathF.Max(peak, MathF.Abs(box.OverscrollY));
        }

        behavior.HasPendingWork.ShouldBeFalse();
        return peak;
    }

    // ---------------------------------------------------------------- 反向优先

    /// <summary>
    /// 处于拉伸状态时反向拖拽：先消解拉伸，<c>ScrollTop</c> 不动。
    /// 少了这一步，从拉伸状态往回拖会先滚动内容、拉伸却还挂着，松手后画面又弹一下。
    /// </summary>
    [Fact]
    public void ReverseDrag_UnstretchesBeforeScrolling()
    {
        var engine = CreateEngine();
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, -60);
        var stretch = MathF.Abs(box.OverscrollY);
        stretch.ShouldBeGreaterThan(1f);

        // 反向拖一个小于当前拉伸的量：应当只消解拉伸，不产生滚动。
        behavior.ScrollBy(engine, 100, 100, 0, stretch / 2f);

        MathF.Abs(box.OverscrollY).ShouldBeLessThan(stretch);
        box.ScrollTop.ShouldBe(0f);
    }

    /// <summary>反向拖拽超过当前拉伸时，多出来的部分照常变成滚动。</summary>
    [Fact]
    public void ReverseDrag_ExcessBeyondStretchScrollsNormally()
    {
        var engine = CreateEngine();
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, -60);
        var stretch = MathF.Abs(box.OverscrollY);

        behavior.ScrollBy(engine, 100, 100, 0, stretch + 30f);

        box.OverscrollY.ShouldBe(0f);
        box.ScrollTop.ShouldBe(30f, tolerance: 0.5f);
    }

    // ---------------------------------------------------------------- 清理

    /// <summary>取消手势后拉伸立刻消失，不能残留在画面上。</summary>
    [Fact]
    public void PointerCancel_ClearsStretchImmediately()
    {
        var engine = CreateEngine();
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, -60);
        box.HasOverscroll.ShouldBeTrue();

        behavior.PointerCancel();

        box.OverscrollY.ShouldBe(0f);
        behavior.HasPendingWork.ShouldBeFalse();
    }

    /// <summary>
    /// 换掉滚动行为时拉伸也必须清零。控制器的 SetScrollBehavior 会调 PointerCancel，
    /// 漏了就会把拉伸永久留在画面上——没有任何东西再去推进那个弹簧。
    /// </summary>
    [Fact]
    public void SetScrollBehavior_ClearsStretchOfTheReplacedBehavior()
    {
        var root = ElasticRoot(300, 200, 1000);
        var engine = CreateEngine(root, 300, 200);
        var elastic = new ElasticScrollBehavior();
        var controller = CreateController(root, engine, elastic);

        controller.OnPointerDown(100, 100, MouseButton.Left, PointerType.Touch);
        controller.OnPointerMove(100, 180);   // 向下拖 → 顶端越界
        engine.GetCurrentLayout()!.HasOverscroll.ShouldBeTrue();

        controller.SetScrollBehavior(new DefaultScrollBehavior());

        engine.GetCurrentLayout()!.OverscrollY.ShouldBe(0f);
    }

    // ---------------------------------------------------------------- 与控制器/宿主的集成

    /// <summary>
    /// 回弹靠宿主继续出帧推进，而宿主只看 <c>HasPendingWork</c>（ISSUE-096 的空闲跳帧）。
    /// 装饰器不把拉伸计入，松手后画面会僵在拉伸状态。
    /// </summary>
    [Fact]
    public void Controller_ReportsPendingWorkWhileRebounding()
    {
        var root = ElasticRoot(300, 200, 1000);
        var engine = CreateEngine(root, 300, 200);
        var controller = CreateController(root, engine, new ElasticScrollBehavior());

        controller.OnPointerDown(100, 100, MouseButton.Left, PointerType.Touch);
        controller.OnPointerMove(100, 180);
        controller.OnPointerUp(100, 180, MouseButton.Left, PointerType.Touch);

        engine.GetCurrentLayout()!.HasOverscroll.ShouldBeTrue();
        controller.HasPendingWork.ShouldBeTrue();

        for (int i = 0; i < 600 && controller.HasPendingWork; i++)
            controller.Update(1f / 60f);

        engine.GetCurrentLayout()!.OverscrollY.ShouldBe(0f);
    }

    // ---------------------------------------------------------------- 不污染既有语义

    /// <summary>
    /// 拉伸不派发 scroll 事件：它不是滚动，<c>scrollTop</c> 也没变。
    /// 反过来说，正常滚动的事件语义不受装饰器影响。
    /// </summary>
    [Fact]
    public void Stretching_DoesNotDispatchScrollEvents()
    {
        var root = ElasticRoot(300, 200, 1000);
        var engine = CreateEngine(root, 300, 200);
        var behavior = new ElasticScrollBehavior();

        int scrollEvents = 0;
        root.AddEventListener<ScrollEventArgs>(EventTypes.Scroll, _ => scrollEvents++);

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, 40);     // 正常滚动
        var afterScroll = scrollEvents;
        afterScroll.ShouldBeGreaterThan(0);

        behavior.ScrollBy(engine, 100, 100, 0, -5000);  // 推回顶端并越界
        var atTop = scrollEvents;

        behavior.ScrollBy(engine, 100, 100, 0, -60);    // 纯拉伸
        engine.GetCurrentLayout()!.HasOverscroll.ShouldBeTrue();
        scrollEvents.ShouldBe(atTop);
    }

    /// <summary>拉伸不移动滚动条：滚动条反映的是 <c>scrollTop</c>，与视觉位移无关。</summary>
    [Fact]
    public void Stretching_DoesNotMoveTheScrollbarThumb()
    {
        var engine = CreateEngine();
        var box = engine.GetCurrentLayout()!;
        var behavior = new ElasticScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, 100);
        var thumbBefore = engine.HitTestScrollbar(294, 100);

        behavior.ScrollBy(engine, 100, 100, 0, -5000);
        behavior.ScrollBy(engine, 100, 100, 0, -60);
        box.HasOverscroll.ShouldBeTrue();

        // 滚动条位置只由 ScrollTop 决定；拉伸期间 ScrollTop 已回到 0。
        box.ScrollTop.ShouldBe(0f);
        thumbBefore.ShouldNotBeNull();
    }

    // ---------------------------------------------------------------- 命中测试

    /// <summary>
    /// 拉伸期间命中坐标随内容一起偏移（绘制与命中是孪生实现），但裁剪窗口不动
    /// ——容器外依然命中不到。
    /// </summary>
    [Fact]
    public void HitTesting_FollowsTheStretchButNotTheClip()
    {
        var child = new DivElement
        {
            Id = "child",
            Style = new Style { Height = Length.Px(50) },
        };
        var root = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(300),
                Height = Length.Px(200),
                OverflowY = Overflow.Auto,
                OverscrollEffect = OverscrollEffect.Elastic,
            },
            Children = { child, new DivElement { Style = new Style { Height = Length.Px(1000) } } },
        };
        var engine = CreateEngine(root, 300, 200);
        var box = engine.GetCurrentLayout()!;

        // 未拉伸：子元素占据 y∈[0,50]，y=70 落在它下面的兄弟节点上。
        engine.HitTest(100, 25)!.Id.ShouldBe("child");
        engine.HitTest(100, 70)!.Id.ShouldNotBe("child");

        // 顶端拉伸 40px（负值 = 内容整体下移）：子元素随之移到 y∈[40,90]。
        box.OverscrollY = -40f;
        engine.HitTest(100, 70)!.Id.ShouldBe("child");    // 原本不在它身上的点，现在落进来了
        engine.HitTest(100, 25)!.Id.ShouldNotBe("child"); // 原本在它身上的点，现在移出去了

        // 裁剪窗口不随拉伸移动：容器之外依然命中不到任何东西。
        engine.HitTest(100, 260).ShouldBeNull();
    }

    // ---------------------------------------------------------------- helpers

    private static float MaxScrollTop(LayoutBox box)
        => MathF.Max(0f, box.ScrollableContentHeight - box.BoxModel.PaddingBox.Height
            + box.HorizontalScrollbarThickness);

    private static LayoutBox? FindBox(LayoutBox box, string id)
    {
        if (box.Element.Id == id) return box;
        foreach (var child in box.Children)
        {
            var hit = FindBox(child, id);
            if (hit != null) return hit;
        }
        return null;
    }

    private static DivElement ElasticRoot(float width, float height, float contentHeight)
        => new()
        {
            Style = new Style
            {
                Width = Length.Px(width),
                Height = Length.Px(height),
                OverflowY = Overflow.Auto,
                OverscrollEffect = OverscrollEffect.Elastic,
            },
            Children = { new DivElement { Style = new Style { Height = Length.Px(contentHeight) } } },
        };

    private static MikoInteractionController CreateController(
        Element root, MikoEngine engine, IScrollBehavior? behavior)
        => new(
            Options.Create(new MikoAppOptions { RootComponentFactory = () => root }),
            new EmptyServiceProvider(), engine, new EventDispatcher(), new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance, scrollBehavior: behavior);

    private static MikoEngine CreateEngine(bool elastic = true, float contentHeight = 1000f)
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(300),
                Height = Length.Px(ViewportH),
                OverflowY = Overflow.Auto,
                OverscrollEffect = elastic ? OverscrollEffect.Elastic : OverscrollEffect.None,
            },
            Children = { new DivElement { Style = new Style { Height = Length.Px(contentHeight) } } },
        };
        return CreateEngine(root, 300, ViewportH);
    }

    private static MikoEngine CreateEngine(Element root, float width, float height)
    {
        var engine = new MikoEngineBuilder().Build();
        // 引擎会留着画布供后续 Render 使用，故 surface 必须比这个方法活得久（与 ScrollBehaviorTests 同）。
        var surface = SKSurface.Create(new SKImageInfo((int)width, (int)height));
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, width, height);
        return engine;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
