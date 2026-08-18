using Miko.Common;
using Miko.Ionic.Components;
using Miko.Styling;
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

    // ---- Height (issue #2: nbsp gives the bar its height) -------------------

    [Fact]
    public void IonSkeletonText_HasNonZeroHeight_FromNbsp()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSkeletonText>();

        var box = cut.GetBoxModel(cut.Root);
        box.ShouldNotBeNull();
        box.Content.Height.ShouldBeGreaterThan(0f,
            "the inner span's non-breaking space must contribute line box height");
    }

    // ---- in-media (issue #1) -----------------------------------------------

    [Fact]
    public void IonSkeletonText_InsideThumbnail_StampsInMediaClass()
    {
        var cut = Context.Render<IonThumbnail>(p =>
            p.Add(nameof(IonThumbnail.ChildContent), (Miko.Components.RenderFragment)(b =>
            {
                b.OpenComponent<IonSkeletonText>(0);
                b.CloseComponent();
            })));

        var skeleton = cut.FindByClass("ion-skeleton-text").ShouldHaveSingleItem();
        skeleton.ShouldHaveClass("in-media");
    }

    [Fact]
    public void IonSkeletonText_InsideAvatar_StampsInMediaClass()
    {
        var cut = Context.Render<IonAvatar>(p =>
            p.Add(nameof(IonAvatar.ChildContent), (Miko.Components.RenderFragment)(b =>
            {
                b.OpenComponent<IonSkeletonText>(0);
                b.CloseComponent();
            })));

        var skeleton = cut.FindByClass("ion-skeleton-text").ShouldHaveSingleItem();
        skeleton.ShouldHaveClass("in-media");
    }

    [Fact]
    public void IonSkeletonText_Standalone_OmitsInMediaClass()
    {
        var cut = Context.Render<IonSkeletonText>();

        cut.Root.ShouldNotHaveClass("in-media");
    }

    [Fact]
    public void IonSkeletonText_InMedia_FillsContainerHeightWithNoMargin()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonThumbnail>(p =>
            p.Add(nameof(IonThumbnail.ChildContent), (Miko.Components.RenderFragment)(b =>
            {
                b.OpenComponent<IonSkeletonText>(0);
                b.CloseComponent();
            })));

        var skeleton = cut.FindByClass("ion-skeleton-text").ShouldHaveSingleItem();
        var style = cut.GetComputedStyle(skeleton)!;

        style.Height.ShouldBe(Length.Percent(100));
        style.MarginTop.ShouldBe(Length.Px(0));
        style.MarginBottom.ShouldBe(Length.Px(0));
    }

    // ---- Animation (issue #3) ----------------------------------------------

    [Fact]
    public void IonSkeletonText_Animated_EmitsShimmerAnimation()
    {
        var cut = Context.Render<IonSkeletonText>(p => p.Add(nameof(IonSkeletonText.Animated), true));

        var animation = cut.Root.Style!.Animations!.Value.Value.ShouldHaveSingleItem();
        animation.Name.ShouldBe("ion-skeleton-shimmer");
        animation.Infinite.ShouldBeTrue();
        animation.TimingFunction.ShouldBe(Miko.Animation.TimingFunction.Linear);
        animation.Keyframes.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void IonSkeletonText_NotAnimated_EmitsNoAnimation()
    {
        var cut = Context.Render<IonSkeletonText>();

        cut.Root.Style?.Animations.ShouldBeNull();
    }

    [Fact]
    public void IonSkeletonText_Animated_PreservesUserStyle()
    {
        // The generated animation style must not clobber a caller-supplied width
        // (the demo page sets Width on each skeleton bar).
        var cut = Context.Render<IonSkeletonText>(p =>
        {
            p.Add(nameof(IonSkeletonText.Animated), true);
            p.Add(nameof(IonSkeletonText.Style), new Style { Width = Length.Percent(80) });
        });

        cut.Root.Style!.Width!.Value.Value.ShouldBe(Length.Percent(80));
        cut.Root.Style!.Animations!.Value.Value.ShouldHaveSingleItem();
    }
}
