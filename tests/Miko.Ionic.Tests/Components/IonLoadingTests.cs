using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Styling;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonLoadingTests : IonicComponentTestBase
{
    [Fact]
    public void IonLoading_DefaultMd_RendersOverlayWithCrescentSpinner()
    {
        var cut = Context.Render<IonLoading>(p =>
        {
            p.Add(nameof(IonLoading.IsOpen), true);
            p.Add(nameof(IonLoading.Message), "Loading...");
        });

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class?.ShouldContain("md");
        cut.Root.Class?.ShouldContain("ion-loading");
        cut.Root.Class?.ShouldNotContain("overlay-hidden");

        var spinner = cut.FindByClass("ion-spinner").FirstOrDefault();
        spinner.ShouldNotBeNull();
        spinner.Class?.ShouldContain("spinner-crescent");

        var message = cut.FindByClass("loading-content").FirstOrDefault();
        message.ShouldNotBeNull();
        message.TextContent.ShouldBe("Loading...");
    }

    [Fact]
    public void IonLoading_DefaultIos_UsesLinesSpinner()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = Context.Render<IonLoading>(p =>
        {
            p.Add(nameof(IonLoading.IsOpen), true);
        });

        cut.Root.Class?.ShouldContain("ios");
        var spinner = cut.FindByClass("ion-spinner").FirstOrDefault();
        spinner.ShouldNotBeNull();
        spinner.Class?.ShouldContain("spinner-lines");
    }

    [Fact]
    public void IonLoading_Closed_AppliesOverlayHidden()
    {
        var cut = Context.Render<IonLoading>(p =>
        {
            p.Add(nameof(IonLoading.IsOpen), false);
        });

        cut.Root.Class?.ShouldContain("overlay-hidden");
    }

    [Fact]
    public void IonLoading_CustomSpinner_UsesSpecifiedSpinner()
    {
        var cut = Context.Render<IonLoading>(p =>
        {
            p.Add(nameof(IonLoading.IsOpen), true);
            p.Add(nameof(IonLoading.Spinner), "dots");
        });

        var spinner = cut.FindByClass("ion-spinner").FirstOrDefault();
        spinner.ShouldNotBeNull();
        spinner.Class?.ShouldContain("spinner-dots");
    }

    [Fact]
    public void IonLoading_EmptySpinner_HidesSpinner()
    {
        var cut = Context.Render<IonLoading>(p =>
        {
            p.Add(nameof(IonLoading.IsOpen), true);
            p.Add(nameof(IonLoading.Spinner), "");
            p.Add(nameof(IonLoading.Message), "Loading...");
        });

        var spinner = cut.FindByClass("ion-spinner").FirstOrDefault();
        spinner.ShouldBeNull();

        var message = cut.FindByClass("loading-content").FirstOrDefault();
        message.ShouldNotBeNull();
    }

    [Fact]
    public void IonLoading_NoMessage_HidesContent()
    {
        var cut = Context.Render<IonLoading>(p =>
        {
            p.Add(nameof(IonLoading.IsOpen), true);
        });

        var message = cut.FindByClass("loading-content").FirstOrDefault();
        message.ShouldBeNull();
    }

    [Fact]
    public void IonLoading_ShowBackdropFalse_HidesBackdrop()
    {
        var cut = Context.Render<IonLoading>(p =>
        {
            p.Add(nameof(IonLoading.IsOpen), true);
            p.Add(nameof(IonLoading.ShowBackdrop), false);
        });

        var backdrop = cut.FindByClass("ion-backdrop").FirstOrDefault();
        backdrop.ShouldBeNull();
    }

    [Fact]
    public void IonLoading_Translucent_AppliesTranslucentClass()
    {
        var cut = Context.Render<IonLoading>(p =>
        {
            p.Add(nameof(IonLoading.IsOpen), true);
            p.Add(nameof(IonLoading.Translucent), true);
        });

        cut.Root.Class?.ShouldContain("loading-translucent");
    }

    [Fact]
    public void IonLoading_SpinnerHasAnimations()
    {
        var cut = Context.Render<IonLoading>(p =>
        {
            p.Add(nameof(IonLoading.IsOpen), true);
        });

        var spinner = cut.FindByClass("ion-spinner").FirstOrDefault();
        spinner.ShouldNotBeNull();

        // The spinner should have inline animations in its Style
        var animations = spinner.Style?.Animations.RefValueOrNull();
        animations.ShouldNotBeNull();
        animations.Count.ShouldBeGreaterThan(0);

        // Each animation should have a non-zero duration
        foreach (var anim in animations)
        {
            anim.Duration.ShouldBeGreaterThan(0f);
            anim.Infinite.ShouldBeTrue();
        }
    }
}
