using Miko.Common;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonSkeletonTextTests : IonicComponentTestBase
{
    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonSkeletonText_RendersDefaultDom()
    {
        var cut = Context.Render<IonSkeletonText>();

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-skeleton-text");

        // Inner spacer span (Ionic's <span>&nbsp;</span>).
        cut.Root.Children.Count.ShouldBe(1);
        cut.Root.Children[0].TagName.ShouldBe("span");
    }

    [Fact]
    public void IonSkeletonText_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonSkeletonText>();

        cut.Root.Class.ShouldBe("ios ion-skeleton-text");
    }

    // ---- State -------------------------------------------------------------

    [Fact]
    public void IonSkeletonText_Animated_StampsAnimatedClass()
    {
        var cut = Context.Render<IonSkeletonText>(p => p.Add(nameof(IonSkeletonText.Animated), true));

        cut.Root.Class.ShouldBe("md ion-skeleton-text skeleton-text-animated");
    }

    [Fact]
    public void IonSkeletonText_NotAnimated_OmitsAnimatedClass()
    {
        var cut = Context.Render<IonSkeletonText>();

        cut.Root.ShouldNotHaveClass("skeleton-text-animated");
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonSkeletonText_DefaultStyle_IsGrayBlock()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSkeletonText>();
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.Block);
        style.Width.ShouldBe(Length.Percent(100));
        style.BackgroundColor.ShouldBe(new Color(0, 0, 0, 17));   // rgba(0,0,0,.065)
        style.UserSelect.ShouldBe(UserSelect.None);
        style.PointerEvents.ShouldBe(PointerEvents.None);
    }

    [Fact]
    public void IonSkeletonText_Animated_UsesLighterFill()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSkeletonText>(p => p.Add(nameof(IonSkeletonText.Animated), true));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.BackgroundColor.ShouldBe(new Color(0, 0, 0, 34));   // rgba(0,0,0,.135)
    }

    [Fact]
    public void IonSkeletonText_InnerSpan_IsInlineBlock()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSkeletonText>();
        var span = cut.Root.Children[0];

        cut.GetComputedStyle(span)!.Display.ShouldBe(Display.InlineBlock);
    }
}
