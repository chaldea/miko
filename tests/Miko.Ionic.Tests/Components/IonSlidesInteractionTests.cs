using Miko.Animation;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Interaction coverage for <see cref="IonSlides"/>: the drag gesture ported from Swiper's
/// onTouchStart/Move/End, the navigation arrows and pagination bullets, looping, the fade effect
/// and autoplay. The gestures are driven through <see cref="EventDispatcher"/> against the built
/// DOM, which is what the engine does for a real pointer.
/// </summary>
public class IonSlidesInteractionTests : IonicComponentTestBase
{
    // The viewport the TestContext lays out against, and therefore the width of one slide.
    private const float SlideWidth = 800f;

    private static RenderFragment ThreeSlides => builder =>
    {
        for (var i = 0; i < 3; i++)
        {
            builder.OpenComponent<IonSlide>(i);
            builder.CloseComponent();
        }
    };

    // ------------------------------------------------------------------------------- drag/swipe

    [Fact]
    public void Drag_PastHalfASlide_AdvancesToNextSlide()
    {
        // Swiper's long-swipe rule (onTouchEnd.mjs): a slow drag covering >= longSwipesRatio (0.5)
        // of a slide advances.
        var changes = new List<int>();
        var cut = RenderSlides(changes);

        Drag(cut.Root, fromX: 700, toX: 200, elapsedMs: 400);

        changes.ShouldHaveSingleItem().ShouldBe(1);
    }

    [Fact]
    public void Drag_ShortOfHalfASlide_SnapsBackWithoutChangingIndex()
    {
        // A slow drag that does not cover half a slide snaps back to where it started.
        var changes = new List<int>();
        var cut = RenderSlides(changes);

        Drag(cut.Root, fromX: 700, toX: 600, elapsedMs: 400);

        changes.ShouldBeEmpty();
        // Back at the resting position for slide 0.
        TranslateXValue(Wrapper(cut)).ShouldBe(0f);
    }

    [Fact]
    public void Flick_AdvancesEvenWhenShorterThanHalfASlide()
    {
        // Swiper's short-swipe rule: under longSwipesMs (300ms) any recognized drag advances,
        // regardless of distance. This is what makes a quick flick feel responsive.
        var changes = new List<int>();
        var cut = RenderSlides(changes);

        Drag(cut.Root, fromX: 700, toX: 620, elapsedMs: 0);

        changes.ShouldHaveSingleItem().ShouldBe(1);
    }

    [Fact]
    public void Drag_Rightwards_GoesToPreviousSlide()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p => p.Add(nameof(IonSlides.ActiveIndex), 1));

        Drag(cut.Root, fromX: 200, toX: 700, elapsedMs: 400);

        changes.ShouldHaveSingleItem().ShouldBe(0);
    }

    [Fact]
    public void Drag_FollowsThePointer_WhileMoving()
    {
        // The track follows the finger, and the stylesheet's transform transition is suppressed so
        // it does not lag behind. The offset is written in percent — the same unit the resting
        // position uses — because the engine interpolates a transition without converting units,
        // so a px/percent mix would break the settle animation (see IonSlides.RestingOffset).
        var cut = RenderSlides(new List<int>());
        var wrapper = Wrapper(cut);

        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(cut.Root, EventTypes.PointerDown, Pointer(cut.Root, 700, 100, true));
        // First move past the threshold only re-bases the origin (Swiper resets startX there).
        dispatcher.Dispatch(cut.Root, EventTypes.PointerMove, Pointer(cut.Root, 690, 100, true));
        dispatcher.Dispatch(cut.Root, EventTypes.PointerMove, Pointer(cut.Root, 590, 100, true));

        var fn = TranslateX(wrapper).ShouldNotBeNull();
        fn.X.Unit.ShouldBe(Miko.Common.LengthUnit.Percent);
        fn.X.Value.ShouldBe(-100f / SlideWidth * 100f, 0.01f);
        wrapper.Style!.Transitions!.Value.Value!.ShouldBeEmpty();
    }

    [Fact]
    public void Drag_ResistsPastTheFirstSlide()
    {
        // Swiper's edge resistance: over-dragging past an edge moves the track by overshoot^0.85
        // rather than the full distance, so the edge feels elastic instead of dead.
        var cut = RenderSlides(new List<int>());
        var wrapper = Wrapper(cut);

        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(cut.Root, EventTypes.PointerDown, Pointer(cut.Root, 100, 100, true));
        dispatcher.Dispatch(cut.Root, EventTypes.PointerMove, Pointer(cut.Root, 110, 100, true));
        dispatcher.Dispatch(cut.Root, EventTypes.PointerMove, Pointer(cut.Root, 310, 100, true));

        // Dragged 200px past the left edge; resistance keeps the track well short of that.
        // Compared in pixels for readability — the stamped value is the same offset in percent.
        var offsetPx = TranslateXValue(wrapper) / 100f * SlideWidth;
        offsetPx.ShouldBeGreaterThan(0f);
        offsetPx.ShouldBeLessThan(200f);
        offsetPx.ShouldBe(MathF.Pow(200f, 0.85f), tolerance: 0.5f);
    }

    [Fact]
    public void Drag_AtLastSlide_CannotAdvancePastTheEnd()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p => p.Add(nameof(IonSlides.ActiveIndex), 2));

        Drag(cut.Root, fromX: 700, toX: 100, elapsedMs: 400);

        changes.ShouldBeEmpty();
    }

    [Fact]
    public void VerticalDrag_IsReleasedToTheScroller()
    {
        // Swiper's direction lock (touchAngle 45deg): a steep drag belongs to the page scroller,
        // so the slides must neither move nor swallow the gesture.
        var changes = new List<int>();
        var cut = RenderSlides(changes);

        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(cut.Root, EventTypes.PointerDown, Pointer(cut.Root, 400, 100, true));
        var move = Pointer(cut.Root, 405, 300, true);
        dispatcher.Dispatch(cut.Root, EventTypes.PointerMove, move);
        dispatcher.Dispatch(cut.Root, EventTypes.PointerUp, Pointer(cut.Root, 405, 300, false));

        changes.ShouldBeEmpty();
        // Not claimed, so the engine is free to treat it as a scroll.
        move.DefaultPrevented.ShouldBeFalse();
    }

    [Fact]
    public void HorizontalDrag_ClaimsTheGesture()
    {
        // The mirror of the above: a horizontal drag must be claimed, or the engine would also
        // scroll the page with it.
        var cut = RenderSlides(new List<int>());

        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(cut.Root, EventTypes.PointerDown, Pointer(cut.Root, 700, 100, true));
        dispatcher.Dispatch(cut.Root, EventTypes.PointerMove, Pointer(cut.Root, 690, 100, true));
        var move = Pointer(cut.Root, 500, 100, true);
        dispatcher.Dispatch(cut.Root, EventTypes.PointerMove, move);

        move.DefaultPrevented.ShouldBeTrue();
    }

    [Fact]
    public void Drag_IsIgnored_WhenAllowTouchMoveIsFalse()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p => p.Add(nameof(IonSlides.AllowTouchMove), false));

        Drag(cut.Root, fromX: 700, toX: 100, elapsedMs: 400);

        changes.ShouldBeEmpty();
    }

    [Fact]
    public void Tap_WithoutMovement_DoesNotChangeSlide()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes);

        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(cut.Root, EventTypes.PointerDown, Pointer(cut.Root, 400, 100, true));
        dispatcher.Dispatch(cut.Root, EventTypes.PointerUp, Pointer(cut.Root, 400, 100, false));

        changes.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------------ navigation arrows

    [Fact]
    public void NextArrow_AdvancesTheSlide()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p => p.Add(nameof(IonSlides.Navigation), true));

        Click(cut.FindByClass("swiper-button-next").ShouldHaveSingleItem());

        changes.ShouldHaveSingleItem().ShouldBe(1);
    }

    [Fact]
    public void PrevArrow_GoesBack()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p =>
        {
            p.Add(nameof(IonSlides.Navigation), true);
            p.Add(nameof(IonSlides.ActiveIndex), 2);
        });

        Click(cut.FindByClass("swiper-button-prev").ShouldHaveSingleItem());

        changes.ShouldHaveSingleItem().ShouldBe(1);
    }

    [Fact]
    public void NextArrow_AtLastSlide_DoesNothing_WithoutLoop()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p =>
        {
            p.Add(nameof(IonSlides.Navigation), true);
            p.Add(nameof(IonSlides.ActiveIndex), 2);
        });

        Click(cut.FindByClass("swiper-button-next").ShouldHaveSingleItem());

        changes.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------------ pagination bullets

    [Fact]
    public void BulletClick_JumpsToThatSlide()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p => p.Add(nameof(IonSlides.Pagination), true));

        Click(cut.FindByClass("swiper-pagination-bullet")[2]);

        changes.ShouldHaveSingleItem().ShouldBe(2);
    }

    [Fact]
    public void BulletActiveState_FollowsTheDrag()
    {
        // The bullets are rebuilt from the new active index after a gesture, so the pager stays in
        // sync with an uncontrolled drag (no ActiveIndex binding involved).
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Pagination), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        Drag(cut.Root, fromX: 700, toX: 200, elapsedMs: 400);

        var bullets = cut.FindByClass("swiper-pagination-bullet");
        bullets[0].ShouldNotHaveClass("swiper-pagination-bullet-active");
        bullets[1].ShouldHaveClass("swiper-pagination-bullet-active");
    }

    // ------------------------------------------------------------------------------------- loop

    [Fact]
    public void Loop_WrapsForwardFromLastToFirst()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p =>
        {
            p.Add(nameof(IonSlides.Loop), true);
            p.Add(nameof(IonSlides.Navigation), true);
            p.Add(nameof(IonSlides.ActiveIndex), 2);
        });

        Click(cut.FindByClass("swiper-button-next").ShouldHaveSingleItem());

        changes.ShouldHaveSingleItem().ShouldBe(0);
    }

    [Fact]
    public void Loop_WrapsBackwardFromFirstToLast()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p =>
        {
            p.Add(nameof(IonSlides.Loop), true);
            p.Add(nameof(IonSlides.Navigation), true);
        });

        Click(cut.FindByClass("swiper-button-prev").ShouldHaveSingleItem());

        changes.ShouldHaveSingleItem().ShouldBe(2);
    }

    [Fact]
    public void Loop_WrapReturnsToTheFirstSlide()
    {
        // Wrapping last -> first puts the track back at its origin. The wrap is not instant: the
        // engine decides whether to animate from the transitions captured on the *previous* frame,
        // so a component cannot cancel the transition for the very step that triggers it. Accepted
        // behaviour — the wrap slides back rather than cutting the way Swiper's cloned track does.
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Loop), true);
            p.Add(nameof(IonSlides.Navigation), true);
            p.Add(nameof(IonSlides.ActiveIndex), 2);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        Click(cut.FindByClass("swiper-button-next").ShouldHaveSingleItem());

        // Re-queried after the click: StateHasChanged rebuilds the subtree, so the pre-click
        // wrapper instance is detached.
        TranslateXValue(Wrapper(cut)).ShouldBe(0f);
    }

    [Fact]
    public void Loop_SingleStepStillAnimates()
    {
        // Only the wrap is instant — an ordinary neighbouring step keeps the stylesheet transition.
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Loop), true);
            p.Add(nameof(IonSlides.Navigation), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        Click(cut.FindByClass("swiper-button-next").ShouldHaveSingleItem());

        var wrapper = Wrapper(cut);
        TranslateXValue(wrapper).ShouldBe(-100f);
        wrapper.Style!.Transitions.ShouldBeNull();
    }

    [Fact]
    public void Loop_DragPastTheLastSlideWraps()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p =>
        {
            p.Add(nameof(IonSlides.Loop), true);
            p.Add(nameof(IonSlides.ActiveIndex), 2);
        });

        Drag(cut.Root, fromX: 700, toX: 200, elapsedMs: 400);

        changes.ShouldHaveSingleItem().ShouldBe(0);
    }

    // ------------------------------------------------------------------------------- fade effect

    [Fact]
    public void Fade_StacksSlidesAndShowsOnlyTheActiveOne()
    {
        // effect-fade.mjs: the track never moves; the slides overlap and only the active one is
        // opaque. The active slide also carries swiper-slide-active, which re-enables input on it.
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Effect), "fade");
            p.Add(nameof(IonSlides.ActiveIndex), 1);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        cut.Root.ShouldHaveClass("swiper-fade");
        Wrapper(cut).Style.ShouldBeNull();

        var slides = cut.FindByClass("swiper-slide");
        slides[0].Style!.Opacity!.Value.Value.ShouldBe(0f);
        slides[1].Style!.Opacity!.Value.Value.ShouldBe(1f);
        slides[1].ShouldHaveClass("swiper-slide-active");
        slides[0].ShouldNotHaveClass("swiper-slide-active");
    }

    [Fact]
    public void Fade_OverlapsSlidesAndTransitionsOpacity_FromStylesheet()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Effect), "fade");
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        var slides = cut.FindByClass("swiper-slide");
        var computed = cut.GetComputedStyle(slides[1]).ShouldNotBeNull();
        computed.Position.ShouldBe(Miko.Common.Position.Absolute);
        computed.PointerEvents.ShouldBe(Miko.Common.PointerEvents.None);

        // The active slide takes input again (effect-fade.css .swiper-slide-active).
        cut.GetComputedStyle(slides[0]).ShouldNotBeNull()
            .PointerEvents.ShouldBe(Miko.Common.PointerEvents.Auto);
    }

    [Fact]
    public void Fade_StillAdvancesOnDrag()
    {
        var changes = new List<int>();
        var cut = RenderSlides(changes, p => p.Add(nameof(IonSlides.Effect), "fade"));

        Drag(cut.Root, fromX: 700, toX: 200, elapsedMs: 400);

        changes.ShouldHaveSingleItem().ShouldBe(1);
    }

    [Fact]
    public void UnknownEffect_FallsBackToSliding()
    {
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Effect), "cube");
            p.Add(nameof(IonSlides.ActiveIndex), 1);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        cut.Root.ShouldNotHaveClass("swiper-fade");
        TranslateXValue(Wrapper(cut)).ShouldBe(-100f);
    }

    // ----------------------------------------------------------------------------------- autoplay

    [Fact]
    public async Task Autoplay_AdvancesAfterTheDelay()
    {
        var dispatcher = new MikoDispatcher();
        Context.Services.AddSingleton(dispatcher);

        var changes = new List<int>();
        var cut = RenderSlides(changes, p =>
        {
            p.Add(nameof(IonSlides.Autoplay), true);
            p.Add(nameof(IonSlides.AutoplayDelay), 40);
        });

        await WaitFor(() => { dispatcher.Drain(); return changes.Count >= 1; });

        changes[0].ShouldBe(1);
    }

    [Fact]
    public async Task Autoplay_StopsAtTheLastSlide_WithoutLoop()
    {
        var dispatcher = new MikoDispatcher();
        Context.Services.AddSingleton(dispatcher);

        var changes = new List<int>();
        RenderSlides(changes, p =>
        {
            p.Add(nameof(IonSlides.Autoplay), true);
            p.Add(nameof(IonSlides.AutoplayDelay), 30);
            p.Add(nameof(IonSlides.ActiveIndex), 2);
        });

        // Give the timer several delays' worth of opportunity to fire.
        var fired = await WaitFor(() => { dispatcher.Drain(); return changes.Count >= 1; },
            timeoutMs: 250);

        fired.ShouldBeFalse();
        changes.ShouldBeEmpty();
    }

    [Fact]
    public void Autoplay_IsInert_WithoutADispatcher()
    {
        // A bare unit test registers no MikoDispatcher; autoplay must degrade quietly rather than
        // touching component state off the render thread.
        var cut = RenderSlides(new List<int>(), p =>
        {
            p.Add(nameof(IonSlides.Autoplay), true);
            p.Add(nameof(IonSlides.AutoplayDelay), 10);
        });

        cut.Root.ShouldNotBeNull();
    }

    // ================================ helpers ================================

    private ComponentUnderTest RenderSlides(
        List<int> changes, Action<ComponentParameterBuilder<IonSlides>>? extra = null)
        => Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
            p.Add(nameof(IonSlides.ActiveIndexChanged),
                EventCallback.Factory.Create<int>(this, i => changes.Add(i)));
            extra?.Invoke(p);
        });

    private static Element Wrapper(ComponentUnderTest cut) =>
        cut.FindByClass("swiper-wrapper").ShouldHaveSingleItem();

    /// <summary>
    /// Drives a full drag. The extra first move exists because Swiper re-bases the drag origin on
    /// the move that crosses the threshold, so a realistic gesture always has one before the
    /// travel that counts.
    /// </summary>
    private static void Drag(Element target, float fromX, float toX, long elapsedMs, float y = 100)
    {
        var dispatcher = new EventDispatcher();
        var nudge = fromX + (toX < fromX ? -10f : 10f);

        dispatcher.Dispatch(target, EventTypes.PointerDown, Pointer(target, fromX, y, true));
        dispatcher.Dispatch(target, EventTypes.PointerMove, Pointer(target, nudge, y, true));
        dispatcher.Dispatch(target, EventTypes.PointerMove, Pointer(target, toX, y, true));
        if (elapsedMs > 0) Thread.Sleep((int)elapsedMs);
        dispatcher.Dispatch(target, EventTypes.PointerUp, Pointer(target, toX, y, false));
    }

    private static void Click(Element target) =>
        new EventDispatcher().Dispatch(target, EventTypes.Click, new MouseEventArgs
        {
            Target = target,
            Button = MouseButton.Left,
        });

    private static PointerEventArgs Pointer(Element target, float x, float y, bool pressed) => new()
    {
        Target = target,
        X = x,
        Y = y,
        TargetWidth = SlideWidth,
        TargetHeight = 600f,
        ViewportWidth = SlideWidth,
        ViewportHeight = 600f,
        Button = MouseButton.Left,
        IsButtonPressed = pressed,
        PointerType = PointerType.Touch,
    };

    private static TransformFunction.TranslateX? TranslateX(Element element) =>
        element.Style?.Transform?.Value.Functions
            .OfType<TransformFunction.TranslateX>()
            .LastOrDefault();

    private static float TranslateXValue(Element element) => TranslateX(element)?.X.Value ?? 0f;

    private static async Task<bool> WaitFor(Func<bool> condition, int timeoutMs = 2000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition()) return true;
            await Task.Delay(10);
        }
        return condition();
    }
}
