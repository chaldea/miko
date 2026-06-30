using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonSlideTests : IonicComponentTestBase
{
    private static readonly RenderFragment TextChild = builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddContent(1, "Slide 1");
        builder.CloseElement();
    };

    [Fact]
    public void IonSlide_RendersWithSwiperClasses_MdMode()
    {
        // DOM/contract: host carries mode + swiper-slide + swiper-zoom-container (slide.tsx).
        var cut = Context.Render<IonSlide>(p =>
            p.Add(nameof(IonSlide.ChildContent), TextChild));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md swiper-slide swiper-zoom-container");
    }

    [Fact]
    public void IonSlide_RendersWithIosModeClass()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonSlide>(p =>
            p.Add(nameof(IonSlide.ChildContent), TextChild));

        cut.Root.Class.ShouldBe("ios swiper-slide swiper-zoom-container");
    }

    [Fact]
    public void IonSlide_RendersChildContent()
    {
        var cut = Context.Render<IonSlide>(p =>
            p.Add(nameof(IonSlide.ChildContent), TextChild));

        cut.Root.Children.Count.ShouldBe(1);
        cut.Root.Children[0].TagName.ShouldBe("span");
    }

    [Fact]
    public void IonSlide_CentersContent_FromStylesheet()
    {
        // Key style assertion: a slide fills the viewport and centers its content.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSlide>(p =>
            p.Add(nameof(IonSlide.ChildContent), TextChild));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.Display.ShouldBe(Miko.Common.Display.Flex);
        style.AlignItems.ShouldBe(Miko.Common.AlignItems.Center);
        style.JustifyContent.ShouldBe(Miko.Common.JustifyContent.Center);
    }
}
