using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonSlidesTests : IonicComponentTestBase
{
    // Three <ion-slide> children, the canonical usage from the issue.
    private static RenderFragment ThreeSlides => builder =>
    {
        for (var i = 0; i < 3; i++)
        {
            builder.OpenComponent<IonSlide>(i);
            builder.CloseComponent();
        }
    };

    [Fact]
    public void IonSlides_RendersHostWithSwiperContainerClasses_MdMode()
    {
        // DOM/contract: host carries mode + slides-{mode} + the swiper container classes
        // (slides.tsx render() + swiper's own container modifier classes).
        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md");
        cut.Root.ShouldHaveClass("slides-md");
        cut.Root.ShouldHaveClass("swiper-container");
        cut.Root.ShouldHaveClass("swiper-horizontal");
    }

    [Fact]
    public void IonSlides_RendersIosModeClasses()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        cut.Root.ShouldHaveClass("ios");
        cut.Root.ShouldHaveClass("slides-ios");
        cut.Root.ShouldHaveClass("swiper-container");
    }

    [Fact]
    public void IonSlides_WrapsSlidesInSwiperWrapper()
    {
        // DOM structure is the contract: host > swiper-wrapper > the <ion-slide> children.
        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        cut.Root.Children.Count.ShouldBe(1);
        var wrapper = cut.Root.Children[0];
        wrapper.TagName.ShouldBe("div");
        wrapper.ShouldHaveClass("swiper-wrapper");
        wrapper.Children.Count.ShouldBe(3);
        wrapper.Children[0].ShouldHaveClass("swiper-slide");
        wrapper.Children[0].ShouldHaveClass("swiper-zoom-container");
    }

    [Fact]
    public void IonSlides_OmitsFurniture_ByDefault()
    {
        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        // Only the wrapper — no navigation/pagination/scrollbar furniture.
        cut.Root.Children.Count.ShouldBe(1);
        cut.FindByClass("swiper-pagination").ShouldBeEmpty();
        cut.FindByClass("swiper-scrollbar").ShouldBeEmpty();
        cut.FindByClass("swiper-button-next").ShouldBeEmpty();
        // The host advertises that the arrows are off (navigation.css hides them by this class).
        cut.Root.ShouldHaveClass("swiper-navigation-disabled");
    }

    [Fact]
    public void IonSlides_RendersPagination_WhenEnabled()
    {
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Pagination), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        var pager = cut.FindByClass("swiper-pagination").ShouldHaveSingleItem();
        pager.TagName.ShouldBe("div");
        // One bullet per slide, the active one marked (pagination.mjs render/update).
        var bullets = cut.FindByClass("swiper-pagination-bullet");
        bullets.Count.ShouldBe(3);
        bullets[0].ShouldHaveClass("swiper-pagination-bullet-active");
        bullets[1].ShouldNotHaveClass("swiper-pagination-bullet-active");
    }

    [Fact]
    public void IonSlides_RendersNavigationArrows_WhenEnabled()
    {
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Navigation), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        cut.FindByClass("swiper-button-prev").ShouldHaveSingleItem();
        cut.FindByClass("swiper-button-next").ShouldHaveSingleItem();
        cut.Root.ShouldNotHaveClass("swiper-navigation-disabled");
    }

    [Fact]
    public void IonSlides_DisablesPrevArrow_AtFirstSlide()
    {
        // navigation.mjs update(): the edge arrow is disabled unless looping.
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Navigation), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        cut.FindByClass("swiper-button-prev").ShouldHaveSingleItem()
            .ShouldHaveClass("swiper-button-disabled");
        cut.FindByClass("swiper-button-next").ShouldHaveSingleItem()
            .ShouldNotHaveClass("swiper-button-disabled");
    }

    [Fact]
    public void IonSlides_DisablesNextArrow_AtLastSlide()
    {
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Navigation), true);
            p.Add(nameof(IonSlides.ActiveIndex), 2);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        cut.FindByClass("swiper-button-next").ShouldHaveSingleItem()
            .ShouldHaveClass("swiper-button-disabled");
        cut.FindByClass("swiper-button-prev").ShouldHaveSingleItem()
            .ShouldNotHaveClass("swiper-button-disabled");
    }

    [Fact]
    public void IonSlides_NeverDisablesArrows_WhenLooping()
    {
        // navigation.mjs update() returns early when loop is on — both arrows stay live.
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Navigation), true);
            p.Add(nameof(IonSlides.Loop), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        cut.FindByClass("swiper-button-prev").ShouldHaveSingleItem()
            .ShouldNotHaveClass("swiper-button-disabled");
        cut.FindByClass("swiper-button-next").ShouldHaveSingleItem()
            .ShouldNotHaveClass("swiper-button-disabled");
    }

    [Fact]
    public void IonSlides_RendersScrollbar_WhenEnabled()
    {
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Scrollbar), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        var scrollbar = cut.FindByClass("swiper-scrollbar").ShouldHaveSingleItem();
        scrollbar.TagName.ShouldBe("div");
        cut.FindByClass("swiper-scrollbar-drag").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonSlides_RendersAllFurniture_WhenAllEnabled()
    {
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Navigation), true);
            p.Add(nameof(IonSlides.Pagination), true);
            p.Add(nameof(IonSlides.Scrollbar), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        // wrapper + prev + next + pagination + scrollbar
        cut.Root.Children.Count.ShouldBe(5);
        cut.FindByClass("swiper-pagination").ShouldHaveSingleItem();
        cut.FindByClass("swiper-scrollbar").ShouldHaveSingleItem();
        cut.FindByClass("swiper-button-prev").ShouldHaveSingleItem();
        cut.FindByClass("swiper-button-next").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonSlides_DiscoversSlideCount_FromRenderedChildren()
    {
        // State: ChildContent is a render fragment, so the slide count is only knowable after the
        // subtree is built. The bullets are the observable proof it was discovered.
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Pagination), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        cut.FindByClass("swiper-slide").Count.ShouldBe(3);
        cut.FindByClass("swiper-pagination-bullet").Count.ShouldBe(3);
    }

    [Fact]
    public void IonSlides_ClampsActiveIndex_BeyondLastSlide()
    {
        // An out-of-range index settles on the last slide rather than translating into blank space.
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.ActiveIndex), 99);
            p.Add(nameof(IonSlides.Pagination), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        var bullets = cut.FindByClass("swiper-pagination-bullet");
        bullets[2].ShouldHaveClass("swiper-pagination-bullet-active");
        TranslateX(cut.Root.Children[0]).ShouldNotBeNull().X.Value.ShouldBe(-200f);
    }

    [Fact]
    public void IonSlides_DoesNotOffsetTrack_OnFirstSlide()
    {
        // State: ActiveIndex 0 (default) leaves the wrapper at its resting position.
        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        var wrapper = cut.Root.Children[0];
        TranslateX(wrapper).ShouldNotBeNull().X.Value.ShouldBe(0f);
    }

    [Fact]
    public void IonSlides_StampsPercentTransform_ForActiveIndex()
    {
        // State: ActiveIndex 2 offsets the track by two viewport widths so slide 3 is in view.
        // The resting offset is in percent because Build() runs before layout — a measured pixel
        // width would still be 0 on the first pass.
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.ActiveIndex), 2);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        var wrapper = cut.Root.Children[0];
        var fn = TranslateX(wrapper).ShouldNotBeNull();
        fn.X.Value.ShouldBe(-200f);
        fn.X.Unit.ShouldBe(Miko.Common.LengthUnit.Percent);
    }

    [Fact]
    public void IonSlides_ClipsOverflow_FromStylesheet()
    {
        // Key style assertion: the host clips so off-screen slides stay hidden.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.OverflowX.ShouldBe(Miko.Common.Overflow.Hidden);
        style.Display.ShouldBe(Miko.Common.Display.Block);
    }

    [Fact]
    public void IonSlides_LaysSlidesOutSideBySide_AcrossTheViewport()
    {
        // BoxModel: each slide fills the viewport and the track runs horizontally, which is what
        // makes a -1 * width track offset land exactly on the next slide.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        var slides = cut.FindByClass("swiper-slide");
        slides.Count.ShouldBe(3);

        var first = cut.GetBoxModel(slides[0]).ShouldNotBeNull();
        var second = cut.GetBoxModel(slides[1]).ShouldNotBeNull();
        first.BorderBox.Width.ShouldBe(cut.ViewportWidth);
        second.BorderBox.X.ShouldBe(first.BorderBox.X + cut.ViewportWidth);
    }

    /// <summary>Reads the horizontal offset the component stamped onto the track.</summary>
    private static Miko.Animation.TransformFunction.TranslateX? TranslateX(Core.Element element) =>
        element.Style?.Transform?.Value.Functions
            .OfType<Miko.Animation.TransformFunction.TranslateX>()
            .LastOrDefault();
}
