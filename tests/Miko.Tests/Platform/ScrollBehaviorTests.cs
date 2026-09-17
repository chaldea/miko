using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// ISSUE-134：滚动能力平台化。引擎默认实现 + 平台可替换，移动端补惯性滚动。
/// </summary>
public sealed class ScrollBehaviorTests
{
    [Fact]
    public void DefaultBehavior_DelegatesToEngine()
    {
        var engine = CreateEngine();
        var behavior = new DefaultScrollBehavior();

        behavior.ScrollBy(engine, 100, 100, 0, 40).ShouldBeTrue();
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(40f);
    }

    [Fact]
    public void Controller_UsesInjectedBehaviorForTouchDrag()
    {
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(100) },
            Children = { new DivElement { Style = new Style { Height = Length.Px(500) } } },
        };
        var engine = CreateEngine(root, 200, 100);
        var behavior = new RecordingBehavior();
        var controller = CreateController(root, engine, behavior);

        controller.OnPointerDown(50, 50, MouseButton.Left, PointerType.Touch);
        controller.OnPointerMove(50, 20);

        behavior.Calls.ShouldBe(1);
        behavior.LastDeltaY.ShouldBe(30f);
    }

    /// <summary>滚轮（非手势）路径同样经平台行为，桌面宿主才能替换滚轮语义。</summary>
    [Fact]
    public void Controller_UsesInjectedBehaviorForWheel()
    {
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(100), OverflowY = Overflow.Auto },
            Children = { new DivElement { Style = new Style { Height = Length.Px(500) } } },
        };
        var engine = CreateEngine(root, 200, 100);
        var behavior = new RecordingBehavior();
        var controller = CreateController(root, engine, behavior);

        controller.OnScroll(50, 50, 0, 40);

        behavior.Calls.ShouldBe(1);
        behavior.LastDeltaY.ShouldBe(40f);
    }

    [Fact]
    public void InertialBehavior_ContinuesAfterPointerRelease()
    {
        var engine = CreateEngine();
        var behavior = new InertialScrollBehavior();
        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, 60).ShouldBeTrue();
        behavior.PointerUp(100, 100);
        behavior.HasPendingWork.ShouldBeTrue();

        var before = engine.GetCurrentLayout()!.ScrollTop;
        behavior.Update(engine, 1f / 60f);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBeGreaterThan(before);
    }

    /// <summary>
    /// 惯性需要宿主继续出帧。稳态空闲检测（ISSUE-096）只看 HasPendingWork：
    /// 惯性不计入就会在松手后立刻停帧，滑动当场僵住。
    /// </summary>
    [Fact]
    public void Controller_ReportsPendingWorkWhileFlinging()
    {
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(100), OverflowY = Overflow.Auto },
            Children = { new DivElement { Style = new Style { Height = Length.Px(2000) } } },
        };
        var engine = CreateEngine(root, 200, 100);
        var controller = CreateController(root, engine, new InertialScrollBehavior());

        controller.OnPointerDown(50, 80, MouseButton.Left, PointerType.Touch);
        controller.OnPointerMove(50, 20);
        controller.OnPointerUp(50, 20, MouseButton.Left, PointerType.Touch);

        controller.HasPendingWork.ShouldBeTrue();

        var before = engine.GetCurrentLayout()!.ScrollTop;
        controller.Update(1f / 60f);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBeGreaterThan(before);
    }

    /// <summary>
    /// 回归：松手速度必须按时间窗估计，不能取「最后一次 move / 距上次采样的间隔」。
    /// 宿主会成批投递移动事件（Android 合并排队的 MotionEvent，高刷屏一毫秒内多点），
    /// 按间隔算出的速度反映的是宿主怎么分批，而不是手指多快。
    /// <para>
    /// 同一个物理手势——8ms 起手后 40ms 内移动 60px——按两种粒度投递：宿主合并成一次 60px，
    /// 或拆成 6 次 10px。位移与耗时都相同，甩出距离就应当相同。按「末次增量 / 上次间隔」算
    /// 会把粗粒度那次算成 6 倍速（60px 除以被地板的间隔，而不是除以真实跨度）。
    /// </para>
    /// <para>
    /// 时间由假时钟推进，不能睡真觉：满负载的 CI 机器上 <c>Sleep</c> 会超睡，越过 100ms 速度
    /// 时间窗后所有样本都已过期、松手被判为静止，甩出距离成了 0——正是 macOS runner 上的表现。
    /// </para>
    /// </summary>
    [Fact]
    public void InertialBehavior_ReleaseVelocityIsIndependentOfHostBatching()
    {
        // (推进毫秒, 位移像素)：两者都在 t=8 起手、t=48 松手，共走 60px。
        var coarse = MeasureFling([(8, 60f)], releaseAfterMs: 40);
        var fine = MeasureFling([(8, 10f), (8, 10f), (8, 10f), (8, 10f), (8, 10f), (8, 10f)], releaseAfterMs: 0);

        coarse.ShouldBeGreaterThan(0f);
        fine.ShouldBeGreaterThan(0f);
        // 同一手势、同一速度，故甩出距离一致（浮点与逐帧积分留 2% 余量）。
        coarse.ShouldBe(fine, tolerance: fine * 0.02f);
    }

    /// <summary>
    /// 回归：拖完停住再松手不应甩出。速度样本必须随时间过期——留住最后一次观测速度，
    /// 会让用户体感静止的松手动作突然滑走。
    /// </summary>
    [Fact]
    public void InertialBehavior_DoesNotFlingAfterHoldingStill()
    {
        var engine = CreateEngine();
        var clock = new FakeClock();
        var behavior = new InertialScrollBehavior(timeProvider: clock);

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, 30);
        clock.Advance(250);         // 手指按住不动
        behavior.PointerUp(100, 100);

        behavior.HasPendingWork.ShouldBeFalse();
    }

    /// <summary>边界处被拒绝的拖拽没有速度可言，松手不应继续。</summary>
    [Fact]
    public void InertialBehavior_DoesNotFlingWhenDragWasRejected()
    {
        var engine = CreateEngine();
        var behavior = new InertialScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, -60).ShouldBeFalse();  // 已在顶端，向上拖无效
        behavior.PointerUp(100, 100);

        behavior.HasPendingWork.ShouldBeFalse();
    }

    [Fact]
    public void InertialBehavior_PointerCancelStopsFling()
    {
        var engine = CreateEngine();
        var behavior = new InertialScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, 60);
        behavior.PointerCancel();

        behavior.HasPendingWork.ShouldBeFalse();
        var before = engine.GetCurrentLayout()!.ScrollTop;
        behavior.Update(engine, 1f / 60f);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(before);
    }

    /// <summary>惯性在到达内容尽头时结束，不会永远占着待办工作（宿主也就能重回空闲）。</summary>
    [Fact]
    public void InertialBehavior_StopsAtContentEnd()
    {
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(300), Height = Length.Px(200), OverflowY = Overflow.Auto },
            Children = { new DivElement { Style = new Style { Height = Length.Px(260) } } },
        };
        var engine = CreateEngine(root, 300, 200);
        var behavior = new InertialScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, 50);
        behavior.PointerUp(100, 100);

        for (int i = 0; i < 600 && behavior.HasPendingWork; i++)
            behavior.Update(engine, 1f / 60f);

        behavior.HasPendingWork.ShouldBeFalse();
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(60f);   // 260 内容 - 200 视口
    }

    /// <summary>更大的摩擦系数衰减更快，因此滑得更近——供宿主调成平台手感。</summary>
    [Fact]
    public void InertialBehavior_HigherFrictionGlidesShorter()
    {
        static float Glide(float friction)
        {
            var engine = CreateEngine();
            var behavior = new InertialScrollBehavior(friction);
            behavior.PointerDown(100, 100);
            behavior.ScrollBy(engine, 100, 100, 0, 40);
            behavior.PointerUp(100, 100);
            var start = engine.GetCurrentLayout()!.ScrollTop;
            for (int i = 0; i < 1200 && behavior.HasPendingWork; i++)
                behavior.Update(engine, 1f / 60f);
            return engine.GetCurrentLayout()!.ScrollTop - start;
        }

        Glide(8f).ShouldBeLessThan(Glide(2f));
    }

    [Fact]
    public void SetScrollBehavior_NullRestoresDefault()
    {
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(100), OverflowY = Overflow.Auto },
            Children = { new DivElement { Style = new Style { Height = Length.Px(500) } } },
        };
        var engine = CreateEngine(root, 200, 100);
        var controller = CreateController(root, engine, new RecordingBehavior());

        controller.SetScrollBehavior(null);

        controller.ScrollBehavior.ShouldBeOfType<DefaultScrollBehavior>();
        controller.OnScroll(50, 50, 0, 40);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(40f);
    }

    /// <summary>换掉行为时，被替换者进行中的惯性必须终止，不能继续驱动出帧。</summary>
    [Fact]
    public void SetScrollBehavior_CancelsPreviousGestureInFlight()
    {
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(100), OverflowY = Overflow.Auto },
            Children = { new DivElement { Style = new Style { Height = Length.Px(2000) } } },
        };
        var engine = CreateEngine(root, 200, 100);
        var inertial = new InertialScrollBehavior();
        var controller = CreateController(root, engine, inertial);

        controller.OnPointerDown(50, 80, MouseButton.Left, PointerType.Touch);
        controller.OnPointerMove(50, 20);
        controller.OnPointerUp(50, 20, MouseButton.Left, PointerType.Touch);
        inertial.HasPendingWork.ShouldBeTrue();

        controller.SetScrollBehavior(new DefaultScrollBehavior());

        inertial.HasPendingWork.ShouldBeFalse();
        // 惯性停了，但这一段拖拽本身产生的脏区域仍待重绘，故不断言控制器整体已空闲。
        // 关键是继续推进帧不再有额外滑动。
        var settled = engine.GetCurrentLayout()!.ScrollTop;
        controller.Update(1f / 60f);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(settled);
    }

    /// <summary>默认容器（无宿主注入）用引擎自带实现，行为与直接调用引擎一致。</summary>
    [Fact]
    public void Controller_WithoutInjection_UsesDefaultBehavior()
    {
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(100), OverflowY = Overflow.Auto },
            Children = { new DivElement { Style = new Style { Height = Length.Px(500) } } },
        };
        var engine = CreateEngine(root, 200, 100);
        var controller = CreateController(root, engine, behavior: null);

        controller.ScrollBehavior.ShouldBeOfType<DefaultScrollBehavior>();
        controller.OnScroll(50, 50, 0, 40);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(40f);
    }

    /// <summary>
    /// 回归：PointerDown 是无条件通知行为的，Cancel 也必须无条件通知。
    /// 抓住滚动条的那一路在设置 _pointerDownTarget 之前就 return 了，漏掉取消会把行为
    /// 留在「仍在拖拽」状态，之后的增量会被当成拖拽速度采样。
    /// </summary>
    [Fact]
    public void Controller_PointerCancel_ReachesBehaviorEvenWhenScrollbarTookThePress()
    {
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(300), Height = Length.Px(200), OverflowY = Overflow.Scroll },
            Children = { new DivElement { Style = new Style { Height = Length.Px(1000) } } },
        };
        var engine = CreateEngine(root, 300, 200);
        var spy = new CancelSpyBehavior();
        var controller = CreateController(root, engine, spy);

        controller.OnPointerDown(299, 100, MouseButton.Left, PointerType.Mouse);   // 命中滚动条
        spy.Downs.ShouldBe(1);
        controller.OnPointerCancel(299, 100);

        spy.Cancels.ShouldBe(1);
    }

    /// <summary>
    /// 按给定的 (推进毫秒, 位移像素) 序列走一段拖拽，把惯性跑完，返回松手后额外滑动的距离。
    /// 时间由假时钟推进，每步先推进时间再投递位移。
    /// <paramref name="releaseAfterMs"/> 是最后一次投递到松手之间的间隔，用于让粗细两种投递
    /// 粒度落在同一个松手时刻。
    /// </summary>
    private static float MeasureFling((int advanceMs, float deltaY)[] moves, int releaseAfterMs)
    {
        var engine = CreateEngine(contentHeight: 200000);
        var clock = new FakeClock();
        var behavior = new InertialScrollBehavior(timeProvider: clock);

        behavior.PointerDown(100, 100);
        foreach (var (advanceMs, deltaY) in moves)
        {
            clock.Advance(advanceMs);
            behavior.ScrollBy(engine, 100, 100, 0, deltaY);
        }
        clock.Advance(releaseAfterMs);
        behavior.PointerUp(100, 100);

        var start = engine.GetCurrentLayout()!.ScrollTop;
        for (int i = 0; i < 3000 && behavior.HasPendingWork; i++)
            behavior.Update(engine, 1f / 60f);
        return engine.GetCurrentLayout()!.ScrollTop - start;
    }

    private static MikoInteractionController CreateController(
        Element root, MikoEngine engine, IScrollBehavior? behavior)
        => new(
            Options.Create(new MikoAppOptions { RootComponentFactory = () => root }),
            new EmptyServiceProvider(), engine, new EventDispatcher(), new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance, scrollBehavior: behavior);

    private static MikoEngine CreateEngine(
        Element? root = null, float width = 300, float height = 200, float contentHeight = 1000)
    {
        root ??= new DivElement
        {
            Style = new Style { Width = Length.Px(width), Height = Length.Px(height), OverflowY = Overflow.Auto },
            Children = { new DivElement { Style = new Style { Height = Length.Px(contentHeight) } } },
        };
        var engine = new MikoEngineBuilder().Build();
        // The engine keeps the canvas for later Render calls, so the surface must outlive this
        // helper. Scroll tests only mutate layout offsets and never repaint, so leaving it to the
        // finalizer is fine — disposing here would hand the engine a dead canvas.
        var surface = SKSurface.Create(new SKImageInfo((int)width, (int)height));
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, width, height);
        return engine;
    }

    private sealed class RecordingBehavior : IScrollBehavior
    {
        public int Calls { get; private set; }
        public float LastDeltaY { get; private set; }
        public bool ScrollBy(MikoEngine engine, float x, float y, float deltaX, float deltaY)
        {
            Calls++;
            LastDeltaY = deltaY;
            return true;
        }
    }

    private sealed class CancelSpyBehavior : IScrollGestureBehavior
    {
        public int Downs { get; private set; }
        public int Cancels { get; private set; }
        public bool HasPendingWork => false;
        public void PointerDown(float x, float y) => Downs++;
        public void PointerUp(float x, float y) { }
        public void PointerCancel() => Cancels++;
        public void Update(MikoEngine engine, float deltaTime) { }
        public bool ScrollBy(MikoEngine engine, float x, float y, float deltaX, float deltaY)
            => engine.ScrollBy(x, y, deltaX, deltaY);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    /// <summary>
    /// 只推进 <see cref="TimeProvider.GetTimestamp"/> 的手动时钟。速度估计断言必须用它而不是
    /// <see cref="Thread.Sleep"/>：睡眠只保证「至少」这么久，CI 上超睡会把样本推出速度时间窗，
    /// 让「甩出距离」这类断言随宿主负载红绿摇摆（1 嘀嗒 = 1μs，与真实频率无关）。
    /// </summary>
    private sealed class FakeClock : TimeProvider
    {
        private long _timestamp;
        public override long TimestampFrequency => 1_000_000;
        public override long GetTimestamp() => _timestamp;
        public void Advance(int milliseconds) => _timestamp += milliseconds * 1_000L;
    }
}
