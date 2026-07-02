using Miko.Animation;
using Miko.Common;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonSpinnerTests : IonicComponentTestBase
{
    [Fact]
    public void IonSpinner_DefaultMd_UsesCircular()
    {
        var cut = Context.Render<IonSpinner>();

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-spinner spinner-circular");
        cut.FindByClass("spinner-circle").Count.ShouldBe(1);
    }

    [Fact]
    public void IonSpinner_DefaultIos_UsesLines()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonSpinner>();

        cut.Root.Class.ShouldBe("ios ion-spinner spinner-lines");
        cut.FindByClass("spinner-line").Count.ShouldBe(8);
    }

    [Fact]
    public void IonSpinner_Dots_RendersThreeCircles()
    {
        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Name), "dots"));

        cut.Root.Class.ShouldBe("md ion-spinner spinner-dots");
        cut.FindByClass("spinner-circle").Count.ShouldBe(3);
    }

    [Fact]
    public void IonSpinner_LinesSharp_RendersTwelveLines()
    {
        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Name), "lines-sharp"));

        cut.Root.Class.ShouldBe("md ion-spinner spinner-lines-sharp");
        cut.FindByClass("spinner-line").Count.ShouldBe(12);
    }

    [Fact]
    public void IonSpinner_UnknownName_FallsBackToLines()
    {
        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Name), "unknown"));

        cut.Root.Class.ShouldBe("md ion-spinner spinner-lines");
        cut.FindByClass("spinner-line").Count.ShouldBe(8);
    }

    [Fact]
    public void IonSpinner_StampsColorPausedAndCustomClasses()
    {
        var cut = Context.Render<IonSpinner>(p =>
        {
            p.Add(nameof(IonSpinner.Color), "danger");
            p.Add(nameof(IonSpinner.Paused), true);
            p.Add(nameof(IonSpinner.Class), "my-spinner");
        });

        cut.Root.Class.ShouldBe("md ion-spinner spinner-circular spinner-paused ion-color ion-color-danger my-spinner");
    }

    [Fact]
    public void IonSpinner_Duration_MapsToHostAnimation()
    {
        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Duration), 2500));

        var animation = cut.Root.Style!.Animations.ShouldHaveSingleItem();
        animation.Name.ShouldBe("ion-spinner-rotate");
        animation.Duration.ShouldBe(2.5f);
        animation.Infinite.ShouldBeTrue();
        animation.TimingFunction.ShouldBe(TimingFunction.Linear);
        animation.PlayState.ShouldBe(AnimationPlayState.Running);
    }

    [Fact]
    public void IonSpinner_Paused_PausesHostAnimation()
    {
        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Paused), true));

        cut.Root.Style!.Animations.ShouldHaveSingleItem().PlayState.ShouldBe(AnimationPlayState.Paused);
    }

    [Fact]
    public void IonSpinner_DefaultStyle_UsesSpinnerBox()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSpinner>();
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.InlineBlock);
        style.Position.ShouldBe(Position.Relative);
        style.Width.ShouldBe(Length.Px(28));
        style.Height.ShouldBe(Length.Px(28));
        style.Color.ShouldBe(Color.FromHex("0054e9"));
    }

    [Fact]
    public void IonSpinner_DangerColor_UsesDangerColor()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Color), "danger"));

        cut.GetComputedStyle(cut.Root)!.Color.ShouldBe(Color.FromHex("c5000f"));
        cut.GetComputedStyle(cut.FindByClass("spinner-circle").Single())!.BorderTopColor.ShouldBe(Color.FromHex("c5000f"));
    }
}
