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
        // DOM/contract: host carries mode + slides-{mode} + swiper-container (slides.tsx).
        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md slides-md swiper-container");
    }

    [Fact]
    public void IonSlides_RendersIosModeClasses()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        cut.Root.Class.ShouldBe("ios slides-ios swiper-container");
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
        wrapper.Class.ShouldBe("swiper-wrapper");
        wrapper.Children.Count.ShouldBe(3);
        wrapper.Children[0].Class.ShouldBe("md swiper-slide swiper-zoom-container");
    }

    [Fact]
    public void IonSlides_OmitsPagerAndScrollbar_ByDefault()
    {
        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        // Only the wrapper — no pagination/scrollbar furniture.
        cut.Root.Children.Count.ShouldBe(1);
        FindByClassInTree(cut.Root, "swiper-pagination").ShouldBeNull();
        FindByClassInTree(cut.Root, "swiper-scrollbar").ShouldBeNull();
    }

    [Fact]
    public void IonSlides_RendersPager_WhenEnabled()
    {
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Pager), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        var pager = FindByClassInTree(cut.Root, "swiper-pagination");
        pager.ShouldNotBeNull();
        pager.TagName.ShouldBe("div");
    }

    [Fact]
    public void IonSlides_RendersScrollbar_WhenEnabled()
    {
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Scrollbar), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        var scrollbar = FindByClassInTree(cut.Root, "swiper-scrollbar");
        scrollbar.ShouldNotBeNull();
        scrollbar.TagName.ShouldBe("div");
    }

    [Fact]
    public void IonSlides_RendersPagerAndScrollbar_WhenBothEnabled()
    {
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.Pager), true);
            p.Add(nameof(IonSlides.Scrollbar), true);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        // wrapper + pagination + scrollbar
        cut.Root.Children.Count.ShouldBe(3);
        FindByClassInTree(cut.Root, "swiper-pagination").ShouldNotBeNull();
        FindByClassInTree(cut.Root, "swiper-scrollbar").ShouldNotBeNull();
    }

    [Fact]
    public void IonSlides_DoesNotStampTransform_OnFirstSlide()
    {
        // State: ActiveIndex 0 (default) leaves the wrapper at its resting position (no transform).
        var cut = Context.Render<IonSlides>(p =>
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides));

        var wrapper = cut.Root.Children[0];
        wrapper.Style.ShouldBeNull();
    }

    [Fact]
    public void IonSlides_StampsTransform_ForActiveIndex()
    {
        // State: ActiveIndex 2 translates the wrapper by -200% so the third slide is in view.
        var cut = Context.Render<IonSlides>(p =>
        {
            p.Add(nameof(IonSlides.ActiveIndex), 2);
            p.Add(nameof(IonSlides.ChildContent), ThreeSlides);
        });

        var wrapper = cut.Root.Children[0];
        wrapper.Style.ShouldNotBeNull();
        wrapper.Style!.Transform.ShouldNotBeNull();
        var fn = wrapper.Style.Transform!.Functions[0].ShouldBeOfType<Miko.Animation.TransformFunction.TranslateX>();
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

    private static Core.Element? FindByClassInTree(Core.Element root, string cls)
    {
        if (root.Class?.Split(' ').Contains(cls) == true) return root;
        foreach (var child in root.Children)
        {
            var found = FindByClassInTree(child, cls);
            if (found != null) return found;
        }
        return null;
    }
}
