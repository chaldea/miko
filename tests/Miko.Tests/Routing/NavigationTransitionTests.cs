using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Routing;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Routing;

/// <summary>
/// 页面转场（ISSUE-108）的引擎级测试：导航时旧页面树保留为 leaving 图层、
/// 与新页面（entering 图层）按 <see cref="NavigationTransitionContext"/> 的
/// 偏移/透明度/叠放次序共同绘制，转场完成后回收旧页面树。
/// </summary>
public class NavigationTransitionTests : IDisposable
{
    private const float W = 200;
    private const float H = 100;

    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;

    public NavigationTransitionTests()
    {
        _bitmap = new SKBitmap((int)W, (int)H);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private static DivElement CreatePage(byte r, byte g, byte b) => new()
    {
        Style = new Style
        {
            Width = Length.Px(W),
            Height = Length.Px(H),
            BackgroundColor = Color.FromRgb(r, g, b)
        }
    };

    private static NavigationTransitionInfo Info(NavigationTransition transition, NavigationDirection direction = NavigationDirection.Forward)
        => new(transition, direction, "/from", "/to");

    private SKColor Pixel(int x, int y) => _bitmap.GetPixel(x, y);

    [Fact]
    public void Initialize_WithTransition_ShouldActivateTransition_AndRenderFirstFrameAtProgressZero()
    {
        var engine = new MikoEngine();
        engine.Initialize(CreatePage(255, 0, 0), [], _canvas, W, H);

        var slide = new SlideTransition();
        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H, Info(slide));

        engine.IsNavigationTransitionActive.ShouldBeTrue();
        slide.Started.ShouldBeTrue();

        // progress=0：新页面整体偏移到视口右侧之外，整个视口仍是旧页面（首帧不闪烁）
        Pixel(10, 50).ShouldBe(SKColors.Red);
        Pixel(190, 50).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void Initialize_WithTransition_WithoutPreviousPage_ShouldFallBackToInstantSwitch()
    {
        var engine = new MikoEngine();
        var slide = new SlideTransition();

        // 首帧导航：没有旧页面，转场不生效
        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H, Info(slide));

        engine.IsNavigationTransitionActive.ShouldBeFalse();
        Pixel(10, 50).ShouldBe(SKColors.Lime); // (0,255,0)
    }

    [Fact]
    public void Initialize_WithZeroDurationTransition_ShouldBeTreatedAsInstantSwitch()
    {
        var engine = new MikoEngine();
        engine.Initialize(CreatePage(255, 0, 0), [], _canvas, W, H);

        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H, Info(new SlideTransition(duration: 0f)));

        engine.IsNavigationTransitionActive.ShouldBeFalse();
        Pixel(10, 50).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Advance_ForwardSlide_ShouldMoveEnteringLayerOverLeavingLayer()
    {
        var engine = new MikoEngine();
        engine.Initialize(CreatePage(255, 0, 0), [], _canvas, W, H);
        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H, Info(new SlideTransition()));

        // 线性时长 1s，推进到 50%：新页面（绿色）滑入覆盖右半屏，左半屏仍是旧页面（红色）
        engine.AdvanceNavigationTransition(0.5f);
        engine.Render(_canvas);

        Pixel(50, 50).ShouldBe(SKColors.Red);
        Pixel(150, 50).ShouldBe(SKColors.Lime);
        engine.IsNavigationTransitionActive.ShouldBeTrue();
    }

    [Fact]
    public void Advance_BackSlide_ShouldSlideLeavingLayerOut_RevealingEnteringLayerBelow()
    {
        var engine = new MikoEngine();
        engine.Initialize(CreatePage(255, 0, 0), [], _canvas, W, H);

        // 返回转场：旧页面（红色）在上方向右滑出，露出下方的新页面（绿色）
        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H,
            Info(new SlideTransition(reverse: true), NavigationDirection.Back));

        engine.AdvanceNavigationTransition(0.5f);
        engine.Render(_canvas);

        Pixel(50, 50).ShouldBe(SKColors.Lime);   // 旧页面已滑走，露出下层新页面
        Pixel(150, 50).ShouldBe(SKColors.Red);   // 旧页面仍覆盖右半屏
    }

    [Fact]
    public void Advance_PastDuration_ShouldCompleteTransition_AndDropLeavingLayer()
    {
        var engine = new MikoEngine();
        engine.Initialize(CreatePage(255, 0, 0), [], _canvas, W, H);

        var slide = new SlideTransition();
        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H, Info(slide));

        engine.AdvanceNavigationTransition(0.5f);
        engine.Render(_canvas);
        slide.Ended.ShouldBeFalse();

        engine.AdvanceNavigationTransition(0.6f);
        engine.Render(_canvas);

        engine.IsNavigationTransitionActive.ShouldBeFalse();
        slide.Ended.ShouldBeTrue();

        // 旧页面图层已回收：整屏只剩新页面
        Pixel(10, 50).ShouldBe(SKColors.Lime);
        Pixel(190, 50).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void NewNavigation_WithoutTransition_ShouldCancelActiveTransition()
    {
        var engine = new MikoEngine();
        engine.Initialize(CreatePage(255, 0, 0), [], _canvas, W, H);

        var slide = new SlideTransition();
        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H, Info(slide));
        engine.AdvanceNavigationTransition(0.3f);
        engine.Render(_canvas);
        engine.IsNavigationTransitionActive.ShouldBeTrue();

        // 转场进行中的新导航（无转场）：直接取消旧转场，瞬时切换到新页面
        engine.Initialize(CreatePage(0, 0, 255), [], _canvas, W, H);

        engine.IsNavigationTransitionActive.ShouldBeFalse();
        slide.Ended.ShouldBeFalse(); // 被取消的转场不触发 OnEnd
        Pixel(10, 50).ShouldBe(SKColors.Blue);
        Pixel(190, 50).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void FadeTransition_ShouldBlendLayersByOpacity()
    {
        var engine = new MikoEngine();
        engine.Initialize(CreatePage(255, 0, 0), [], _canvas, W, H);
        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H, Info(new FadeTransition()));

        // 50%：绿色新页面以 0.5 不透明度盖在红色旧页面上 → 黄调色
        engine.AdvanceNavigationTransition(0.5f);
        engine.Render(_canvas);

        var pixel = Pixel(100, 50);
        pixel.Red.ShouldBeInRange((byte)120, (byte)136);
        pixel.Green.ShouldBeInRange((byte)119, (byte)135);
        pixel.Blue.ShouldBe((byte)0);
    }

    [Fact]
    public void HasPendingVisualWork_ShouldBeTrueDuringTransition_AndFalseAfterCompletion()
    {
        var engine = new MikoEngine();
        engine.Initialize(CreatePage(255, 0, 0), [], _canvas, W, H);
        engine.Render(_canvas);
        engine.HasPendingVisualWork.ShouldBeFalse(); // 稳态（ISSUE-096）

        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H, Info(new SlideTransition()));
        engine.HasPendingVisualWork.ShouldBeTrue();  // 转场期间宿主必须持续出帧

        engine.AdvanceNavigationTransition(1.1f);
        engine.Render(_canvas);

        engine.HasPendingVisualWork.ShouldBeFalse(); // 完成后回到稳态
    }

    [Fact]
    public void Tick_ShouldAdvanceTransition()
    {
        var engine = new MikoEngine();
        engine.Initialize(CreatePage(255, 0, 0), [], _canvas, W, H);
        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H, Info(new SlideTransition()));

        // Tick 同时推进转场时钟并渲染（与 MikoInteractionController.Update + Render 的帧循环等价）
        engine.Tick(0.5f, _canvas);

        Pixel(50, 50).ShouldBe(SKColors.Red);
        Pixel(150, 50).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Update_ShouldRepaintTransitionFrame_EvenWithoutDirtyRegions()
    {
        var engine = new MikoEngine();
        engine.Initialize(CreatePage(255, 0, 0), [], _canvas, W, H);
        engine.Initialize(CreatePage(0, 255, 0), [], _canvas, W, H, Info(new SlideTransition()));

        // 转场推进只改变图层偏移，不产生脏区域；Update 路径也必须整体重绘两层
        engine.AdvanceNavigationTransition(0.5f);
        engine.Update(_canvas);

        Pixel(50, 50).ShouldBe(SKColors.Red);
        Pixel(150, 50).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Context_ShouldHaveExpectedDefaults()
    {
        var leaving = CreatePage(255, 0, 0);
        var entering = CreatePage(0, 255, 0);
        var ctx = new NavigationTransitionContext(leaving, entering, NavigationDirection.Forward, "/a", "/b", W, H);

        ctx.LeavingElement.ShouldBeSameAs(leaving);
        ctx.EnteringElement.ShouldBeSameAs(entering);
        ctx.Direction.ShouldBe(NavigationDirection.Forward);
        ctx.FromPath.ShouldBe("/a");
        ctx.ToPath.ShouldBe("/b");
        ctx.ViewportWidth.ShouldBe(W);
        ctx.ViewportHeight.ShouldBe(H);

        ctx.LeavingOffsetX.ShouldBe(0f);
        ctx.LeavingOffsetY.ShouldBe(0f);
        ctx.LeavingOpacity.ShouldBe(1f);
        ctx.EnteringOffsetX.ShouldBe(0f);
        ctx.EnteringOffsetY.ShouldBe(0f);
        ctx.EnteringOpacity.ShouldBe(1f);
        ctx.EnteringBelow.ShouldBeFalse();
    }

    /// <summary>
    /// 测试用滑动转场（核心库不内置具体效果——效果由组件库实现，见 ISSUE-108）：
    /// 前进时新页面从右侧滑入盖在旧页面上；反向（返回）时旧页面向右滑出、新页面在下方。
    /// </summary>
    private sealed class SlideTransition : NavigationTransition
    {
        private readonly bool _reverse;

        public SlideTransition(bool reverse = false, float duration = 1f)
        {
            _reverse = reverse;
            Duration = duration;
        }

        public override float Duration { get; }
        public override TimingFunction TimingFunction => TimingFunction.Linear;

        public bool Started { get; private set; }
        public bool Ended { get; private set; }

        public override void OnStart(NavigationTransitionContext context)
        {
            Started = true;
            context.EnteringBelow = _reverse;
        }

        public override void Apply(NavigationTransitionContext context, float progress)
        {
            float w = context.ViewportWidth;
            if (_reverse)
                context.LeavingOffsetX = w * progress;
            else
                context.EnteringOffsetX = w * (1f - progress);
        }

        public override void OnEnd(NavigationTransitionContext context) => Ended = true;
    }

    /// <summary>测试用淡入转场：新页面不透明度 0 → 1。</summary>
    private sealed class FadeTransition : NavigationTransition
    {
        public override float Duration => 1f;
        public override TimingFunction TimingFunction => TimingFunction.Linear;

        public override void Apply(NavigationTransitionContext context, float progress)
            => context.EnteringOpacity = progress;
    }
}
