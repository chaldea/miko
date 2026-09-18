using Miko.Animation;
using Miko.Common;
using Miko.Components;
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

        var animation = cut.Root.Style!.Animations!.Value.Value.ShouldHaveSingleItem();
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

        cut.Root.Style!.Animations!.Value.Value.ShouldHaveSingleItem().PlayState.ShouldBe(AnimationPlayState.Paused);
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

    [Theory]
    [InlineData("lines-small")]
    [InlineData("lines-sharp-small")]
    public void IonSpinner_SmallLinePreset_KeepsStandardHostSize(string name)
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Name), name));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Width.ShouldBe(Length.Px(28));
        style.Height.ShouldBe(Length.Px(28));
    }

    [Theory]
    [InlineData("lines-small")]
    [InlineData("lines-sharp-small")]
    public void IonSpinner_SmallLinePreset_RotatesAroundHostCenter(string name)
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Name), name));
        var lineStyle = cut.GetComputedStyle(cut.FindByClass("spinner-line-1").Single())!;

        lineStyle.Top.ShouldBe(Length.Px(2));
        lineStyle.Left.ShouldBe(Length.Percent(50));
        lineStyle.TransformOrigin.ShouldBe(
            new TransformOrigin(Length.Percent(50), Length.Px(12)));
    }

    [Fact]
    public void IonSpinner_InItemStartSlot_HasEndMargin()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        RenderFragment spinner = builder =>
        {
            builder.OpenComponent<IonSpinner>();
            builder.CloseComponent();
        };
        var cut = Context.Render<IonItem>(p => p.Add(nameof(IonItem.Start), spinner));
        var spinnerElement = cut.FindByClass("ion-spinner").Single();

        cut.GetComputedStyle(spinnerElement)!.MarginInlineEnd.ShouldBe(Length.Px(16));
    }

    [Fact]
    public void IonSpinner_Dots_AnimatesSegmentsFromLeftToRight()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Name), "dots"));
        var circles = cut.FindByClass("spinner-circle");

        cut.Root.Style?.Animations.ShouldBeNull();
        circles.Count.ShouldBe(3);
        circles.Select(circle => cut.GetComputedStyle(circle)!.Left)
            .ShouldBe(new[] { Length.Px(2), Length.Px(11), Length.Px(20) });
        circles.Select(circle => circle.Style!.Animations!.Value.Value.ShouldHaveSingleItem().Name)
            .ShouldAllBe(name => name == "ion-spinner-dots");
        circles.Select(circle => circle.Style!.Animations!.Value.Value.Single().Delay)
            .ShouldBe(new[] { -0.22f, -0.11f, 0f });
    }

    [Theory]
    [InlineData("bubbles")]
    [InlineData("circles")]
    public void IonSpinner_RingPreset_RotatesSegmentsAroundHostCenter(string name)
    {
        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Name), name));
        var firstCircle = cut.FindByClass("spinner-circle-1").Single();

        cut.Root.Style!.TransformOrigin!.Value.Value.ShouldBe(TransformOrigin.Center);
        cut.Root.Style.Animations!.Value.Value.ShouldHaveSingleItem().Name.ShouldBe("ion-spinner-rotate");
        firstCircle.Style!.Top!.Value.Value.ShouldBe(Length.Percent(50));
        firstCircle.Style.Left!.Value.Value.ShouldBe(Length.Percent(82));
        firstCircle.Style.Transform!.Value.Value.Functions.ShouldBeEmpty();
    }

    [Fact]
    public void IonSpinner_Bubbles_ScalesEachBubbleWithStaggeredTiming()
    {
        var cut = Context.Render<IonSpinner>(p => p.Add(nameof(IonSpinner.Name), "bubbles"));
        var bubbles = cut.FindByClass("spinner-circle");

        bubbles.Count.ShouldBe(9);
        bubbles.Select(bubble => bubble.Style!.Animations!.Value.Value.ShouldHaveSingleItem().Name)
            .ShouldAllBe(name => name == "ion-spinner-scale-out");

        var firstAnimation = bubbles[0].Style!.Animations!.Value.Value.Single();
        firstAnimation.Delay.ShouldBe(-1f);
        firstAnimation.Keyframes[0].Style.Transform!.Value.Value.Functions
            .ShouldHaveSingleItem().ShouldBe(new TransformFunction.Scale(1f, 1f));
        firstAnimation.Keyframes[1].Style.Transform!.Value.Value.Functions
            .ShouldHaveSingleItem().ShouldBe(new TransformFunction.Scale(0f, 0f));

        bubbles.Select(bubble => bubble.Style!.Animations!.Value.Value.Single().Delay)
            .ShouldBeInOrder(SortDirection.Ascending);
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
