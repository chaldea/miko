using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// End-to-end coverage for <see cref="IonSlides"/> driven through the real
/// <see cref="MikoInteractionController"/>: a genuine mouse press/move/release against laid-out
/// pixels, exercising hit testing and the engine's implicit pointer capture. The unit-level
/// interaction tests dispatch straight at an element and so cannot catch a slide that is
/// unreachable by hit test, or a drag that dies when the pointer leaves the component.
/// </summary>
public class IonSlidesControllerTests : IDisposable
{
    private const float Width = 400;
    private const float Height = 300;
    private readonly SKBitmap _bitmap = new((int)Width, (int)Height);
    private readonly SKCanvas _canvas;

    public IonSlidesControllerTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void MouseDrag_AcrossTheSlides_AdvancesToTheNextSlide()
    {
        // The issue this component was rewritten for: dragging with the mouse must change slides.
        var (app, page) = BuildApp();

        DragAcross(app, fromX: Width - 40, toX: 40);

        page.ActiveIndex.ShouldBe(1);
        page.Changes.ShouldHaveSingleItem().ShouldBe(1);
    }

    [Fact]
    public void MouseDrag_Backwards_ReturnsToThePreviousSlide()
    {
        var (app, page) = BuildApp(p => p.ActiveIndex = 1);

        DragAcross(app, fromX: 40, toX: Width - 40);

        page.ActiveIndex.ShouldBe(0);
    }

    [Fact]
    public void Drag_ContinuesAfterThePointerLeavesTheComponent()
    {
        // The engine captures the pointer on press, so a drag that runs off the edge of the
        // component (or the window) still resolves rather than being abandoned mid-gesture.
        var (app, page) = BuildApp();

        app.Controller.OnPointerDown(Width - 40, Height / 2, MouseButton.Left);
        app.Controller.OnPointerMove(Width - 60, Height / 2);
        app.Controller.OnPointerMove(-200, Height / 2);
        app.Controller.OnPointerUp(-200, Height / 2, MouseButton.Left);

        page.ActiveIndex.ShouldBe(1);
    }

    [Fact]
    public void ClickingTheNextArrow_AdvancesTheSlide()
    {
        // Proves the arrow is actually hit-testable at its laid-out position, not merely present
        // in the DOM.
        var (app, page) = BuildApp(p => p.Navigation = true);

        var arrow = FindByClass(app, "swiper-button-next");
        var (x, y) = CenterOf(app, arrow);
        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);

        page.ActiveIndex.ShouldBe(1);
    }

    [Fact]
    public void ClickingAPaginationBullet_JumpsToThatSlide()
    {
        var (app, page) = BuildApp(p => p.Pagination = true);

        var bullets = app.Engine.GetRoot()!.FindByClass("swiper-pagination-bullet");
        bullets.Count.ShouldBe(3);
        var (x, y) = CenterOf(app, bullets[2]);
        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);

        page.ActiveIndex.ShouldBe(2);
    }

    [Fact]
    public void SlidesFillTheViewport_AndSitSideBySide()
    {
        // The geometry the drag arithmetic assumes: one slide per viewport width, laid out in a row.
        var (app, _) = BuildApp();

        var slides = app.Engine.GetRoot()!.FindByClass("swiper-slide");
        slides.Count.ShouldBe(3);
        slides[0].OffsetWidth.ShouldBe(Width, 0.5f);

        var first = BorderBox(app, slides[0]);
        var second = BorderBox(app, slides[1]);
        second.X.ShouldBe(first.X + Width, 0.5f);
    }

    [Fact]
    public void Drag_MovesTheTrackWithTheFinger()
    {
        // The track must follow the pointer while dragging. This is what broke when the gesture was
        // bound through Razor's @onpointer* directives: each move re-rendered the component and
        // replaced the wrapper, so the offset written by a move was discarded by the rebuild that
        // same move triggered, and the track sat still.
        var (app, _) = BuildApp();

        app.Controller.OnPointerDown(Width - 40, Height / 2, MouseButton.Left);
        app.Controller.OnPointerMove(Width - 50, Height / 2);
        app.Controller.OnPointerMove(Width - 250, Height / 2);

        // Written in percent, the same unit the resting offset uses, so the settle animation can
        // interpolate between them (see IonSlides.RestingOffset).
        var offset = TrackOffset(app);
        offset.Unit.ShouldBe(LengthUnit.Percent);
        offset.Value.ShouldBe(-200f / Width * 100f, 0.5f);
    }

    [Fact]
    public void DragRelease_EasesToTheNextSlide_WhenFramesRenderDuringTheDrag()
    {
        // The gesture as the host actually delivers it: a frame is rendered after every pointer
        // move. That is what made this case different from an arrow click — the drag writes the
        // wrapper's inline transform directly, and transforms are paint-only (applied through the
        // animation overlay, deliberately skipping layout), so the drag never reaches the computed
        // style. With nothing to diff, the engine started no transition on release and the track
        // jumped to the next slide in one frame. The settle is therefore driven explicitly.
        var (app, page) = BuildApp();

        app.Controller.OnPointerDown(Width - 40, Height / 2, MouseButton.Left);
        app.Controller.OnPointerMove(Width - 50, Height / 2);
        app.Engine.Tick(1f / 60f, _canvas);
        for (var x = Width - 80; x >= 120; x -= 40)
        {
            app.Controller.OnPointerMove(x, Height / 2);
            app.Engine.Tick(1f / 60f, _canvas);
        }
        Thread.Sleep(350);              // past longSwipesMs, so distance decides
        app.Controller.OnPointerUp(120, Height / 2, MouseButton.Left);
        app.Engine.Render(_canvas);

        page.ActiveIndex.ShouldBe(1);

        // Eases from where the finger let go towards the next slide, over several frames.
        var released = ComputedTrackOffset(app);
        released.ShouldBeInRange(-100f, -1f, "must resume from the drag position, not from 0");

        var samples = AnimationSamples(app);
        samples.ShouldNotBeEmpty("the release must animate rather than jump");
        for (var i = 1; i < samples.Count; i++)
            samples[i].ShouldBeLessThan(samples[i - 1], $"frame {i} did not advance");
    }

    [Fact]
    public void SlideChange_AnimatesTheTrack_RatherThanJumping()
    {
        // Releasing a drag must ease the track to the next slide over several frames. Asserting
        // that a transition merely exists is not enough — a unit mismatch between the drag offset
        // (px) and the resting offset (was %) let the engine "interpolate" -360 → -100 and the
        // track appeared to jump and then stall.
        var (app, _) = BuildApp();

        DragAcross(app, fromX: Width - 40, toX: 40);
        app.Engine.Render(_canvas);

        var samples = new List<float>();
        for (var frame = 0; frame < 10; frame++)
        {
            app.Engine.Tick(1f / 60f, _canvas);
            samples.Add(ComputedTrackOffset(app));
        }

        // Advancing towards the destination every frame, and still short of it — i.e. easing, not
        // a single jump to the end.
        for (var i = 1; i < samples.Count; i++)
            samples[i].ShouldBeLessThan(samples[i - 1], $"frame {i} did not advance");
        samples[0].ShouldBeGreaterThan(-Width);
        samples[^1].ShouldBeGreaterThan(-Width);
    }

    [Fact]
    public void Loop_StepAfterAWrapForward_StillAnimates()
    {
        // Only the wrap itself is instant. The step that follows it (4 -> 1, then 1 -> 2) is an
        // ordinary neighbouring move and must animate like any other. Getting this wrong is easy:
        // the "this move is a wrap" signal cannot live in a field, because a bound host rebuilds
        // the component as a new instance between the two clicks.
        var (app, page) = BuildApp(p => { p.Navigation = true; p.Loop = true; p.SlideCount = 4; p.ActiveIndex = 3; });

        ClickNext(app);                 // 4 -> 1, the wrap: instant by design
        Settle(app);
        ClickNext(app);                 // 1 -> 2, must animate
        app.Engine.Render(_canvas);

        page.ActiveIndex.ShouldBe(1);
        AnimationSamples(app).ShouldNotBeEmpty("the step after a wrap must animate");
    }

    [Fact]
    public void Loop_StepAfterAWrapBackward_StillAnimates()
    {
        // The mirror case: 1 -> 4 wraps, then 4 -> 3 must animate.
        var (app, page) = BuildApp(p => { p.Navigation = true; p.Loop = true; p.SlideCount = 4; });

        ClickPrev(app);                 // 1 -> 4, the wrap
        Settle(app);
        ClickPrev(app);                 // 4 -> 3, must animate
        app.Engine.Render(_canvas);

        page.ActiveIndex.ShouldBe(2);
        AnimationSamples(app).ShouldNotBeEmpty("the step after a wrap must animate");
    }

    [Fact]
    public void Loop_TheWrapLandsOnTheFirstSlide()
    {
        // A forward wrap moves the track to the first slide. It animates its way there (sliding
        // back across the whole track) rather than cutting: the engine decides whether to animate
        // from the transitions captured on the previous frame, so a component cannot cancel the
        // transition for the very step that triggers it. Documented as accepted behaviour — the
        // wrap is not seamless the way Swiper's cloned-slide track is.
        var (app, page) = BuildApp(p => { p.Navigation = true; p.Loop = true; p.SlideCount = 4; p.ActiveIndex = 3; });

        ClickNext(app);                 // 4 -> 1
        Settle(app);

        page.ActiveIndex.ShouldBe(0);
        ComputedTrackOffset(app).ShouldBe(0f, 0.5f);
    }

    [Fact]
    public void Loop_OrdinaryStep_Animates()
    {
        // The baseline the two tests above are measured against: with looping on but no wrap
        // involved, a plain 1 -> 2 step animates.
        var (app, _) = BuildApp(p => { p.Navigation = true; p.Loop = true; p.SlideCount = 4; });

        ClickNext(app);                 // 1 -> 2
        app.Engine.Render(_canvas);

        AnimationSamples(app).ShouldNotBeEmpty();
    }

    [Fact]
    public void SlideChange_StartsATransition_SoTheTrackAnimates()
    {
        // The track must animate to the next slide, not snap to it. The engine detects transitions
        // by diffing the previous frame's computed style against the new one, keyed by element
        // reference — so this also pins down that the wrapper element survives the re-render a
        // slide change triggers. A replaced wrapper has no previous value to interpolate from and
        // would jump.
        var (app, _) = BuildApp(p => p.Navigation = true);

        var arrow = FindByClass(app, "swiper-button-next");
        var (x, y) = CenterOf(app, arrow);
        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
        app.Engine.Render(_canvas);

        app.Engine.AnimationManager.ActiveTransitionCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void DragRelease_StartsATransition_SoTheTrackSettlesSmoothly()
    {
        // Same contract for the gesture path: releasing a drag animates to the resting position
        // rather than snapping there.
        var (app, _) = BuildApp();

        DragAcross(app, fromX: Width - 40, toX: 40);
        app.Engine.Render(_canvas);

        app.Engine.AnimationManager.ActiveTransitionCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ConsecutiveDrags_KeepAdvancing()
    {
        // Two gestures in a row, with a frame rendered between them as the host loop does every
        // tick. The frame is required, not incidental: hit testing walks the layout tree, which is
        // only rebuilt by Render, so without one the second press would resolve against the slides
        // detached by the first gesture's re-render and never reach the component.
        //
        // What this pins down is that the per-gesture drag state is reset on press — a leaked
        // _isTouched / threshold flag would let only the first drag through.
        var (app, page) = BuildApp();

        DragAcross(app, fromX: Width - 40, toX: 40);
        app.Engine.Render(_canvas);
        DragAcross(app, fromX: Width - 40, toX: 40);

        page.ActiveIndex.ShouldBe(2);
        page.Changes.ShouldBe(new[] { 1, 2 });
    }

    [Fact]
    public void DragBelowThreshold_LeavesTheSlideAlone()
    {
        var (app, page) = BuildApp();

        app.Controller.OnPointerDown(200, Height / 2, MouseButton.Left);
        app.Controller.OnPointerMove(197, Height / 2);
        app.Controller.OnPointerUp(197, Height / 2, MouseButton.Left);

        page.ActiveIndex.ShouldBe(0);
        page.Changes.ShouldBeEmpty();
    }

    // ================================ helpers ================================

    /// <summary>
    /// A full press/move/release. The intermediate move exists because Swiper re-bases the drag
    /// origin on the move that crosses its threshold, so a real gesture always has one before the
    /// travel that counts. Sleeps past longSwipesMs so the release is judged on distance rather
    /// than being treated as a flick.
    /// </summary>
    private static void DragAcross(MikoAppContext app, float fromX, float toX)
    {
        var y = Height / 2;
        var nudge = fromX + (toX < fromX ? -10f : 10f);

        app.Controller.OnPointerDown(fromX, y, MouseButton.Left);
        app.Controller.OnPointerMove(nudge, y);
        app.Controller.OnPointerMove(toX, y);
        Thread.Sleep(350);
        app.Controller.OnPointerUp(toX, y, MouseButton.Left);
    }

    private static Element FindByClass(MikoAppContext app, string className) =>
        app.Engine.GetRoot()!.FindByClass(className).ShouldHaveSingleItem();

    private static RectF BorderBox(MikoAppContext app, Element element)
    {
        var box = FindLayoutBox(app.Engine.GetCurrentLayout(), element);
        box.ShouldNotBeNull();
        return box.BoxModel.BorderBox;
    }

    private static (float x, float y) CenterOf(MikoAppContext app, Element element)
    {
        var rect = BorderBox(app, element);
        rect.Width.ShouldBeGreaterThan(0f);
        rect.Height.ShouldBeGreaterThan(0f);
        return (rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    }

    private static Layout.LayoutBox? FindLayoutBox(Layout.LayoutBox? box, Element target)
    {
        if (box is null) return null;
        if (ReferenceEquals(box.Element, target)) return box;
        foreach (var child in box.Children)
        {
            var found = FindLayoutBox(child, target);
            if (found is not null) return found;
        }
        return null;
    }

    private static void ClickNext(MikoAppContext app) =>
        ClickCenterOf(app, FindByClass(app, "swiper-button-next"));

    private static void ClickPrev(MikoAppContext app) =>
        ClickCenterOf(app, FindByClass(app, "swiper-button-prev"));

    private static void ClickCenterOf(MikoAppContext app, Element element)
    {
        var (x, y) = CenterOf(app, element);
        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
    }

    /// <summary>
    /// Runs frames until any transition has finished, so the next move starts from a settled track
    /// rather than interrupting an animation still in flight.
    /// </summary>
    private void Settle(MikoAppContext app)
    {
        app.Engine.Render(_canvas);
        for (var frame = 0; frame < 40; frame++)
            app.Engine.Tick(1f / 60f, _canvas);
    }

    /// <summary>
    /// The track positions observed over the next few frames that differ from where it started.
    /// Empty means it did not animate — it was already at its destination on the first frame.
    /// This measures the observable outcome rather than the presence of a transition object, which
    /// can be present and still produce no motion.
    /// </summary>
    private List<float> AnimationSamples(MikoAppContext app)
    {
        var start = ComputedTrackOffset(app);
        var moved = new List<float>();
        for (var frame = 0; frame < 8; frame++)
        {
            app.Engine.Tick(1f / 60f, _canvas);
            var offset = ComputedTrackOffset(app);
            if (Math.Abs(offset - start) > 0.5f) moved.Add(offset);
        }
        return moved;
    }

    /// <summary>The transitions the stylesheet leaves on the track, i.e. whether it will animate.</summary>
    private static IReadOnlyList<Miko.Animation.Transition> TrackTransitions(MikoAppContext app)
    {
        var wrapper = FindByClass(app, "swiper-wrapper");
        return wrapper.LayoutBox?.ComputedStyle?.Transitions ?? new List<Miko.Animation.Transition>();
    }

    /// <summary>The offset the component wrote onto the track's inline style.</summary>
    private static Length TrackOffset(MikoAppContext app)
    {
        var wrapper = FindByClass(app, "swiper-wrapper");
        var fn = wrapper.Style?.Transform?.Value.Functions
            .OfType<Miko.Animation.TransformFunction.TranslateX>()
            .LastOrDefault();
        fn.ShouldNotBeNull();
        return fn.X;
    }

    /// <summary>The offset actually in effect after the cascade and any running transition.</summary>
    private static float ComputedTrackOffset(MikoAppContext app)
    {
        var wrapper = FindByClass(app, "swiper-wrapper");
        return wrapper.LayoutBox?.ComputedStyle?.Transform?.Functions
            .OfType<Miko.Animation.TransformFunction.TranslateX>()
            .LastOrDefault()?.X.Value ?? 0f;
    }

    private (MikoAppContext app, HostPage page) BuildApp(Action<HostPage>? configure = null)
    {
        var page = new HostPage();
        configure?.Invoke(page);
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRootComponent(page.Build);
        var app = builder.Build();
        app.Controller.Initialize(_canvas, Width, Height);
        app.Engine.Render(_canvas);
        return (app, page);
    }

    /// <summary>Stands in for a page binding <c>@bind-ActiveIndex</c> onto the slides.</summary>
    private sealed class HostPage : ComponentBase
    {
        public int ActiveIndex { get; set; }
        public bool Navigation { get; set; }
        public bool Pagination { get; set; }
        public bool Loop { get; set; }
        public int SlideCount { get; set; } = 3;
        public List<int> Changes { get; } = new();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<IonSlides>(0);
            builder.AddComponentParameter(1, nameof(IonSlides.ActiveIndex), ActiveIndex);
            builder.AddComponentParameter(2, nameof(IonSlides.Navigation), Navigation);
            builder.AddComponentParameter(3, nameof(IonSlides.Pagination), Pagination);
            builder.AddComponentParameter(6, nameof(IonSlides.Loop), Loop);
            builder.AddComponentParameter(4, nameof(IonSlides.ActiveIndexChanged),
                EventCallback.Factory.Create<int>(this, index =>
                {
                    ActiveIndex = index;
                    Changes.Add(index);
                }));
            builder.AddComponentParameter(5, nameof(IonSlides.ChildContent), (RenderFragment)(b =>
            {
                for (var i = 0; i < SlideCount; i++)
                {
                    b.OpenComponent<IonSlide>(i);
                    b.AddComponentParameter(i * 10 + 1, nameof(IonSlide.ChildContent),
                        (RenderFragment)(sb => sb.AddContent(0, $"Slide {i}")));
                    b.CloseComponent();
                }
            }));
            builder.CloseComponent();
        }
    }
}
