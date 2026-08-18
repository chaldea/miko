using Miko.Common;
using Miko.Core;
using Miko.Animation;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonProgressBarTests : IonicComponentTestBase
{
    private static ComponentUnderTest RenderBar(TestContext ctx,
        Action<ComponentParameterBuilder<IonProgressBar>>? configure = null)
        => ctx.Render<IonProgressBar>(p => configure?.Invoke(p));

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonProgressBar_RendersDeterminateDomContract()
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonProgressBar.Value), 0.5));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-progress-bar");
        cut.Root.ShouldHaveClass("progress-bar-determinate");

        cut.FindByClass("progress").ShouldHaveSingleItem();
        // Ionic keeps the full-width track behind a solid progress bar so its unfilled area is visible.
        cut.Root.ShouldHaveClass("progress-bar-solid");
        cut.FindByClass("progress-buffer-bar").ShouldHaveSingleItem();
        cut.FindByClass("buffer-circles-container").Count.ShouldBe(2);
        cut.FindByClass("buffer-circles").ShouldHaveSingleItem();
        cut.FindByClass("ion-hide").ShouldHaveSingleItem();
        // No indeterminate stripes in determinate mode.
        cut.FindByClass("indeterminate-bar-primary").ShouldBeEmpty();
    }

    [Fact]
    public void IonProgressBar_RendersIndeterminateDomContract()
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonProgressBar.Type), "indeterminate"));

        cut.Root.ShouldHaveClass("progress-bar-indeterminate");

        cut.FindByClass("progress-buffer-bar").ShouldHaveSingleItem();
        cut.FindByClass("indeterminate-bar-primary").ShouldHaveSingleItem();
        cut.FindByClass("indeterminate-bar-secondary").ShouldHaveSingleItem();
        cut.FindByClass("progress-indeterminate").Count.ShouldBe(2);
        // No determinate value fill (FindByClass matches the exact "progress" token only).
        cut.FindByClass("progress").ShouldBeEmpty();
    }

    [Fact]
    public void IonProgressBar_WithBuffer_RendersBufferTrack()
    {
        var cut = RenderBar(Context, p =>
        {
            p.Add(nameof(IonProgressBar.Value), 0.3);
            p.Add(nameof(IonProgressBar.Buffer), 0.6);
        });

        cut.Root.ShouldNotHaveClass("progress-bar-solid");
        cut.FindByClass("progress-buffer-bar").ShouldHaveSingleItem();
        cut.FindByClass("progress").ShouldHaveSingleItem();
        cut.FindByClass("buffer-circles-container").Count.ShouldBe(2);
        cut.FindByClass("buffer-circles").ShouldHaveSingleItem();
        cut.FindByClass("ion-hide").ShouldBeEmpty();
    }

    [Fact]
    public void IonProgressBar_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderBar(Context);

        cut.Root.Class.ShouldStartWith("ios ion-progress-bar");
    }

    [Fact]
    public void IonProgressBar_StampsReversedClass()
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonProgressBar.Reversed), true));

        cut.Root.ShouldHaveClass("progress-bar-reversed");
    }

    [Fact]
    public void IonProgressBar_Reversed_MirrorsHost()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBar(Context, p =>
        {
            p.Add(nameof(IonProgressBar.Type), "indeterminate");
            p.Add(nameof(IonProgressBar.Reversed), true);
        });

        var transform = cut.GetComputedStyle(cut.Root)!.Transform;
        var scale = transform.Functions.ShouldHaveSingleItem().ShouldBeOfType<TransformFunction.Scale>();
        scale.X.ShouldBe(-1f);
        scale.Y.ShouldBe(1f);
    }

    [Fact]
    public void IonProgressBar_StampsColorClass()
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonProgressBar.Color), "success"));

        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-success");
    }

    // ---- Value / buffer widths --------------------------------------------

    [Fact]
    public void IonProgressBar_Value_SetsProgressWidth()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBar(Context, p => p.Add(nameof(IonProgressBar.Value), 0.4));

        var progress = cut.FindByClass("progress").Single();
        cut.GetComputedStyle(progress)!.Width.ShouldBe(Length.Percent(40));
    }

    [Fact]
    public void IonProgressBar_Value_IsClampedToUnitRange()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBar(Context, p => p.Add(nameof(IonProgressBar.Value), 1.7));

        var progress = cut.FindByClass("progress").Single();
        cut.GetComputedStyle(progress)!.Width.ShouldBe(Length.Percent(100));
    }

    [Fact]
    public void IonProgressBar_Buffer_SetsBufferTrackWidth()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBar(Context, p =>
        {
            p.Add(nameof(IonProgressBar.Value), 0.2);
            p.Add(nameof(IonProgressBar.Buffer), 0.7);
        });

        var buffer = cut.FindByClass("progress-buffer-bar").Single();
        cut.GetComputedStyle(buffer)!.Width.ShouldBe(Length.Percent(70));
    }

    [Fact]
    public void IonProgressBar_Buffer_PositionsAndAnimatesCircleStream()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBar(Context, p => p.Add(nameof(IonProgressBar.Buffer), 0.5));

        var containers = cut.FindByClass("buffer-circles-container");
        AssertTranslateX(cut.GetComputedStyle(containers[0])!.Transform, 50f, LengthUnit.Percent);
        AssertTranslateX(cut.GetComputedStyle(containers[1])!.Transform, -50f, LengthUnit.Percent);

        var circles = cut.FindByClass("buffer-circles").Single();
        var circlesStyle = cut.GetComputedStyle(circles)!;
        circlesStyle.BorderTopWidth.ShouldBe(Length.Px(4));
        circlesStyle.BorderTopStyle.ShouldBe(BorderStyle.Dotted);
        circlesStyle.BorderTopColor.ShouldBe(IonicTheme.CreateMd().ProgressBarBackground);

        var animation = circles.Style!.Animations!.Value.Value.ShouldHaveSingleItem();
        animation.Name.ShouldBe("buffering");
        animation.Duration.ShouldBe(0.45f);
        animation.Infinite.ShouldBeTrue();
        animation.TimingFunction.ShouldBe(TimingFunction.Linear);
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonProgressBar_Style_HostIsBlockWithMdHeight()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBar(Context);
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.Block);
        style.Height.ShouldBe(Length.Px(4));
    }

    [Fact]
    public void IonProgressBar_Style_ProgressUsesPrimaryFill()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBar(Context, p => p.Add(nameof(IonProgressBar.Value), 0.5));

        var progress = cut.FindByClass("progress").Single();
        cut.GetComputedStyle(progress)!.BackgroundColor
            .ShouldBe(IonicTheme.CreateMd().ProgressBarProgressBackground);
    }

    [Fact]
    public void IonProgressBar_Style_DefaultTrackUsesPrimaryTint()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBar(Context, p => p.Add(nameof(IonProgressBar.Value), 0.5));

        var track = cut.FindByClass("progress-buffer-bar").Single();
        cut.GetComputedStyle(track)!.BackgroundColor
            .ShouldBe(IonicTheme.CreateMd().ProgressBarBackground);
        cut.GetComputedStyle(track)!.Width.ShouldBe(Length.Percent(100));
    }

    [Fact]
    public void IonProgressBar_Style_ColorOverridesFill()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBar(Context, p =>
        {
            p.Add(nameof(IonProgressBar.Value), 0.5);
            p.Add(nameof(IonProgressBar.Color), "danger");
        });

        var progress = cut.FindByClass("progress").Single();
        cut.GetComputedStyle(progress)!.BackgroundColor
            .ShouldBe(IonicTheme.CreateMd().Danger);
    }

    [Fact]
    public void IonProgressBar_Style_ColorOverridesTrack()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBar(Context, p =>
        {
            p.Add(nameof(IonProgressBar.Buffer), 0.5);
            p.Add(nameof(IonProgressBar.Color), "danger");
        });

        var danger = IonicTheme.CreateMd().Danger;
        var expectedTrack = new Color(danger.R, danger.G, danger.B, 77);
        cut.GetComputedStyle(cut.FindByClass("progress-buffer-bar").Single())!.BackgroundColor
            .ShouldBe(expectedTrack);
        cut.GetComputedStyle(cut.FindByClass("buffer-circles").Single())!.BorderTopColor
            .ShouldBe(expectedTrack);
    }

    [Fact]
    public void IonProgressBar_Indeterminate_EmitsSlidingAnimations()
    {
        var cut = RenderBar(Context, p => p.Add(nameof(IonProgressBar.Type), "indeterminate"));

        AssertAnimation(cut.FindByClass("indeterminate-bar-primary").Single(),
            "primary-indeterminate-translate");
        AssertAnimation(cut.FindByClass("indeterminate-bar-secondary").Single(),
            "secondary-indeterminate-translate");

        var fills = cut.FindByClass("progress-indeterminate");
        AssertAnimation(fills[0], "primary-indeterminate-scale");
        AssertAnimation(fills[1], "secondary-indeterminate-scale");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IonProgressBar_IndeterminateTrack_FillsHostWidth_InLayout(bool reversed)
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.ViewportWidth = 400f;

        var cut = RenderBar(Context, p =>
        {
            p.Add(nameof(IonProgressBar.Type), "indeterminate");
            p.Add(nameof(IonProgressBar.Reversed), reversed);
        });

        var hostBox = cut.GetBoxModel(cut.Root)!;
        var track = cut.FindByClass("progress-buffer-bar").Single();
        var trackBox = cut.GetBoxModel(track)!;

        cut.GetComputedStyle(track)!.Width.ShouldBe(Length.Percent(100));
        trackBox.Content.Width.ShouldBe(hostBox.Content.Width, 0.01f);
        trackBox.Content.Width.ShouldBeGreaterThan(0f);
    }

    private static void AssertAnimation(Element element, string expectedName)
    {
        var animation = element.Style!.Animations!.Value.Value.ShouldHaveSingleItem();
        animation.Name.ShouldBe(expectedName);
        animation.Duration.ShouldBe(2f);
        animation.Infinite.ShouldBeTrue();
        animation.TimingFunction.ShouldBe(TimingFunction.Linear);
        animation.Keyframes.Count.ShouldBe(4);
    }

    private static void AssertTranslateX(Transform transform, float value, LengthUnit unit)
    {
        var translate = transform.Functions.ShouldHaveSingleItem()
            .ShouldBeOfType<TransformFunction.TranslateX>();
        translate.X.Value.ShouldBe(value);
        translate.X.Unit.ShouldBe(unit);
    }
}
