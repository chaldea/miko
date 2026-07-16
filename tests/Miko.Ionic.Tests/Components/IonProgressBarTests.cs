using Miko.Common;
using Miko.Core;
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
        // No buffer track when buffer defaults to 1 (a solid bar).
        cut.Root.ShouldHaveClass("progress-bar-solid");
        cut.FindByClass("progress-buffer-bar").ShouldBeEmpty();
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
}
