using Miko.Core.DomElements;
using Miko.Routing;
using Shouldly;

namespace Miko.Ionic.Tests;

/// <summary>
/// Tests for <see cref="IonicPageTransitions"/> — the ported Ionic page transitions
/// (issues/ion-animation). Asserts mode resolution and the layer state written by
/// <see cref="NavigationTransition.Apply"/> at key progress points.
/// </summary>
public class IonicPageTransitionsTests
{
    private static NavigationTransitionContext CreateContext(
        NavigationDirection direction = NavigationDirection.Forward, float width = 400f, float height = 800f)
        => new(new DivElement(), new DivElement(), direction, "/from", "/to", width, height);

    // ---- Mode resolution ----------------------------------------------------

    [Fact]
    public void Push_IosMode_ReturnsIosTransition()
    {
        IonicPageTransitions.Push("ios").ShouldBeSameAs(IonicPageTransitions.IosPageTransition.Push);
    }

    [Fact]
    public void Push_MdMode_ReturnsMdTransition()
    {
        IonicPageTransitions.Push("md").ShouldBeSameAs(IonicPageTransitions.MdPageTransition.Push);
    }

    [Fact]
    public void Pop_IosMode_ReturnsIosPopTransition()
    {
        IonicPageTransitions.Pop("ios").ShouldBeSameAs(IonicPageTransitions.IosPageTransition.Pop);
    }

    [Fact]
    public void Pop_MdMode_ReturnsMdPopTransition()
    {
        IonicPageTransitions.Pop("md").ShouldBeSameAs(IonicPageTransitions.MdPageTransition.Pop);
    }

    // ---- iOS push -----------------------------------------------------------

    [Fact]
    public void IosPush_EnteringSlidesInFromRight_LeavingParallaxLeft()
    {
        var ctx = CreateContext(width: 400f);
        var t = IonicPageTransitions.IosPageTransition.Push;

        t.OnStart(ctx);
        ctx.EnteringBelow.ShouldBeFalse();

        t.Apply(ctx, 0f);
        ctx.EnteringOffsetX.ShouldBe(400f);
        ctx.LeavingOffsetX.ShouldBe(0f);

        t.Apply(ctx, 0.5f);
        ctx.EnteringOffsetX.ShouldBe(200f);
        ctx.LeavingOffsetX.ShouldBe(-60f, 0.001f); // -30% parallax

        t.Apply(ctx, 1f);
        ctx.EnteringOffsetX.ShouldBe(0f);
        ctx.LeavingOffsetX.ShouldBe(-120f, 0.001f);
    }

    [Fact]
    public void IosPush_UsesIonicIosTiming()
    {
        var t = IonicPageTransitions.IosPageTransition.Push;
        t.Duration.ShouldBe(0.54f);
        t.CubicBezier.ShouldBe(new Miko.Animation.CubicBezierParams(0.32f, 0.72f, 0f, 1f));
    }

    // ---- iOS pop ------------------------------------------------------------

    [Fact]
    public void IosPop_LeavingSlidesOutRight_EnteringReturnsFromParallax()
    {
        var ctx = CreateContext(NavigationDirection.Back, width: 400f);
        var t = IonicPageTransitions.IosPageTransition.Pop;

        t.OnStart(ctx);
        ctx.EnteringBelow.ShouldBeTrue(); // old page slides out on top, new page revealed below

        t.Apply(ctx, 0f);
        ctx.LeavingOffsetX.ShouldBe(0f);
        ctx.EnteringOffsetX.ShouldBe(-120f, 0.001f);

        t.Apply(ctx, 1f);
        ctx.LeavingOffsetX.ShouldBe(400f);
        ctx.EnteringOffsetX.ShouldBe(0f);
    }

    // ---- MD push ------------------------------------------------------------

    [Fact]
    public void MdPush_EnteringFadesInFromBelow()
    {
        var ctx = CreateContext(width: 400f);
        var t = IonicPageTransitions.MdPageTransition.Push;

        t.OnStart(ctx);
        ctx.EnteringBelow.ShouldBeFalse();

        t.Apply(ctx, 0f);
        ctx.EnteringOffsetY.ShouldBe(40f);
        ctx.EnteringOpacity.ShouldBe(0f);

        t.Apply(ctx, 0.5f);
        ctx.EnteringOffsetY.ShouldBe(20f);
        ctx.EnteringOpacity.ShouldBe(0.5f);

        t.Apply(ctx, 1f);
        ctx.EnteringOffsetY.ShouldBe(0f);
        ctx.EnteringOpacity.ShouldBe(1f);
    }

    [Fact]
    public void MdPush_UsesIonicMdTiming()
    {
        var t = IonicPageTransitions.MdPageTransition.Push;
        t.Duration.ShouldBe(0.28f);
        t.CubicBezier.ShouldBe(new Miko.Animation.CubicBezierParams(0.36f, 0.66f, 0.04f, 1f));
    }

    // ---- MD pop -------------------------------------------------------------

    [Fact]
    public void MdPop_LeavingFadesOutAboveEntering()
    {
        var ctx = CreateContext(NavigationDirection.Back, width: 400f);
        var t = IonicPageTransitions.MdPageTransition.Pop;

        t.OnStart(ctx);
        ctx.EnteringBelow.ShouldBeTrue();

        t.Apply(ctx, 0f);
        ctx.LeavingOpacity.ShouldBe(1f);

        t.Apply(ctx, 0.5f);
        ctx.LeavingOpacity.ShouldBe(0.5f);

        t.Apply(ctx, 1f);
        ctx.LeavingOpacity.ShouldBe(0f);

        // Entering page stays put underneath (no offsets).
        ctx.EnteringOffsetX.ShouldBe(0f);
        ctx.EnteringOffsetY.ShouldBe(0f);
    }

    [Fact]
    public void MdPop_UsesShorterBackTiming()
    {
        var t = IonicPageTransitions.MdPageTransition.Pop;
        t.Duration.ShouldBe(0.2f);
        t.CubicBezier.ShouldBe(new Miko.Animation.CubicBezierParams(0.47f, 0f, 0.745f, 0.715f));
    }
}
