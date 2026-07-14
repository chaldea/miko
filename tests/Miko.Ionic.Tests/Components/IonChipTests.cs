using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonChipTests : IonicComponentTestBase
{
    private static readonly RenderFragment Label = builder => builder.AddContent(0, "Active");

    [Fact]
    public void IonChip_RendersDefaultDom()
    {
        var cut = Context.Render<IonChip>(p => p.Add(nameof(IonChip.ChildContent), Label));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-chip ion-activatable");
        cut.GetTextContent().ShouldContain("Active");
    }

    [Fact]
    public void IonChip_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonChip>();

        cut.Root.Class.ShouldStartWith("ios ion-chip");
    }

    [Fact]
    public void IonChip_StampsStateAndColorClasses()
    {
        var cut = Context.Render<IonChip>(p =>
        {
            p.Add(nameof(IonChip.Outline), true);
            p.Add(nameof(IonChip.Disabled), true);
            p.Add(nameof(IonChip.Color), "success");
        });

        cut.Root.ShouldHaveClass("chip-outline");
        cut.Root.ShouldHaveClass("chip-disabled");
        cut.Root.ShouldHaveClass("ion-color-success");
    }

    [Fact]
    public void IonChip_DefaultStyle_IsInlinePill()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonChip>();
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.InlineBlock);
        style.MinHeight.ShouldBe(Length.Px(32));
        style.BorderTopLeftRadius.Value.ShouldBe(16f);
    }

    [Fact]
    public void IonChip_OutlineStyle_UsesBorderAndTransparentFill()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonChip>(p => p.Add(nameof(IonChip.Outline), true));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.BackgroundColor.ShouldBe(Color.Transparent);
        style.BorderTopWidth.ShouldBe(Length.Px(1));
        style.BorderTopStyle.ShouldBe(BorderStyle.Solid);
    }

    [Fact]
    public void IonChip_DisabledStyle_IsDimmed()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonChip>(p => p.Add(nameof(IonChip.Disabled), true));

        cut.GetComputedStyle(cut.Root)!.Opacity.ShouldBe(0.4f);
    }
}
