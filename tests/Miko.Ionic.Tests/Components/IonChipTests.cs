using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Miko.Testing;
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
    public void IonChip_DefaultStyle_IsInlineFlexPill()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonChip>();
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.InlineFlex);
        style.AlignItems.ShouldBe(AlignItems.Center);
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

    [Fact]
    public void IonChip_HoverStyle_DarkensBackground()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonChip>();

        var before = cut.GetComputedStyle(cut.Root)!.BackgroundColor;
        before.A.ShouldBe((byte)31); // text 0.12

        var hovered = Hover(cut.Root);
        var after = hovered.GetComputedStyle(hovered.Root)!.BackgroundColor;
        after.A.ShouldBe((byte)41); // text 0.16
        (after.R, after.G, after.B).ShouldBe((before.R, before.G, before.B));
    }

    [Fact]
    public void IonChip_HoverStyle_ColorChip_RaisesColorWash()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonChip>(p => p.Add(nameof(IonChip.Color), "success"));

        cut.GetComputedStyle(cut.Root)!.BackgroundColor.A.ShouldBe((byte)20); // color 0.08

        var hovered = Hover(cut.Root);
        hovered.GetComputedStyle(hovered.Root)!.BackgroundColor.A.ShouldBe((byte)31); // color 0.12
    }

    [Fact]
    public void IonChip_HoverStyle_OutlineChip_GetsFaintWash()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonChip>(p => p.Add(nameof(IonChip.Outline), true));

        cut.GetComputedStyle(cut.Root)!.BackgroundColor.ShouldBe(Color.Transparent);

        var hovered = Hover(cut.Root);
        hovered.GetComputedStyle(hovered.Root)!.BackgroundColor.A.ShouldBe((byte)10); // text 0.04
    }

    [Fact]
    public void IonChip_HoverStyle_OutlineColorChip_UsesColorWash_NotTransparent()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonChip>(p =>
        {
            p.Add(nameof(IonChip.Outline), true);
            p.Add(nameof(IonChip.Color), "success");
        });

        var hovered = Hover(cut.Root);

        // :host(.ion-color:hover) beats the transparent outline fill.
        hovered.GetComputedStyle(hovered.Root)!.BackgroundColor.A.ShouldBe((byte)31); // color 0.12
    }

    /// <summary>Re-runs style resolution with <see cref="Miko.Core.ElementState.Hover"/> set.</summary>
    private ComponentUnderTest Hover(Miko.Core.Element root)
    {
        root.SetState(Miko.Core.ElementState.Hover);
        return Context.RenderElement(root);
    }

    [Fact]
    public void IonChip_SlottedIcon_Is20pxBox_TuckedIntoLeadingEdge()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonChip>(p => p.Add(nameof(IonChip.ChildContent), (RenderFragment)(builder =>
        {
            builder.OpenComponent<IonIcon>(0);
            builder.CloseComponent();
            builder.OpenComponent<IonLabel>(1);
            builder.AddAttribute(2, nameof(IonLabel.ChildContent), (RenderFragment)(b => b.AddContent(0, "Active")));
            builder.CloseComponent();
        })));

        var icon = cut.Root.FindByClass("ion-icon").ShouldHaveSingleItem();
        var style = cut.GetComputedStyle(icon)!;

        style.Width.ShouldBe(Length.Px(20));
        style.Height.ShouldBe(Length.Px(20));
        // First-child icon pulls toward the pill edge and leaves an 8px gap before the label.
        style.MarginTop.ShouldBe(Length.Px(-4));
        style.MarginRight.ShouldBe(Length.Px(8));
        style.MarginBottom.ShouldBe(Length.Px(-4));
        style.MarginLeft.ShouldBe(Length.Px(-4));
    }

    [Fact]
    public void IonChip_SlottedAvatar_OverridesIntrinsicSize_AndKeepsShape()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonChip>(p => p.Add(nameof(IonChip.ChildContent), (RenderFragment)(builder =>
        {
            builder.OpenComponent<IonAvatar>(0);
            builder.CloseComponent();
            builder.OpenComponent<IonLabel>(1);
            builder.AddAttribute(2, nameof(IonLabel.ChildContent), (RenderFragment)(b => b.AddContent(0, "Active")));
            builder.CloseComponent();
        })));

        var avatar = cut.Root.FindByClass("ion-avatar").ShouldHaveSingleItem();
        var style = cut.GetComputedStyle(avatar)!;

        style.Width.ShouldBe(Length.Px(24));
        style.Height.ShouldBe(Length.Px(24));
        style.FlexShrink.ShouldBe(0f);
        style.MarginLeft.ShouldBe(Length.Px(-8));
        style.MarginRight.ShouldBe(Length.Px(8));
    }
}
