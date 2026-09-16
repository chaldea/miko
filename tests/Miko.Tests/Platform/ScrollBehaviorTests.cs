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
    /// 按间隔算出的速度反映的是宿主怎么分批，而不是手指多快——同一段拖拽分批越密，
    /// 甩出越远。此处两种分批走过相同的位移与耗时，甩出距离应当接近。
    /// </summary>
    [Fact]
    public void InertialBehavior_ReleaseVelocityIsIndependentOfHostBatching()
    {
        // 同样 60px / 约 48ms：一次成批投递 vs 逐帧投递。
        var batched = MeasureFling(samples: 6, sleepMsPerSample: 0, pxPerSample: 10f, totalSleepMs: 48);
        var paced = MeasureFling(samples: 6, sleepMsPerSample: 8, pxPerSample: 10f, totalSleepMs: 0);

        batched.ShouldBeGreaterThan(0f);
        paced.ShouldBeGreaterThan(0f);
        // 修复前成批那组会因间隔被地板为 1 个计时嘀嗒而快出十几倍。
        batched.ShouldBeLessThan(paced * 3f);
    }

    /// <summary>
    /// 回归：拖完停住再松手不应甩出。速度样本必须随时间过期——留住最后一次观测速度，
    /// 会让用户体感静止的松手动作突然滑走。
    /// </summary>
    [Fact]
    public void InertialBehavior_DoesNotFlingAfterHoldingStill()
    {
        var engine = CreateEngine();
        var behavior = new InertialScrollBehavior();

        behavior.PointerDown(100, 100);
        behavior.ScrollBy(engine, 100, 100, 0, 30);
        Thread.Sleep(250);          // 手指按住不动
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
    /// 走一段拖拽并把惯性跑完，返回松手后额外滑动的距离。
    /// <paramref name="totalSleepMs"/> 用于在成批投递后补足同样的耗时。
    /// </summary>
    private static float MeasureFling(int samples, int sleepMsPerSample, float pxPerSample, int totalSleepMs)
    {
        var engine = CreateEngine(contentHeight: 200000);
        var behavior = new InertialScrollBehavior();

        behavior.PointerDown(100, 100);
        for (int i = 0; i < samples; i++)
        {
            if (sleepMsPerSample > 0) Thread.Sleep(sleepMsPerSample);
            behavior.ScrollBy(engine, 100, 100, 0, pxPerSample);
        }
        if (totalSleepMs > 0) Thread.Sleep(totalSleepMs);
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
}
