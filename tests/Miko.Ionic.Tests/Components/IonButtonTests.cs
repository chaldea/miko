using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-button</c>. Covers the DOM contract (host → .button-native → .button-inner),
/// the fill / size / shape / expand / color / state class stamping, the icon-only marker, key
/// theme-driven styles, and click interaction.
/// </summary>
public class IonButtonTests : IonicComponentTestBase
{
    private static readonly RenderFragment Label = builder => builder.AddContent(0, "Default");

    private static ComponentUnderTest RenderButton(TestContext ctx,
        Action<ComponentParameterBuilder<IonButton>>? configure = null)
        => ctx.Render<IonButton>(p =>
        {
            p.Add(nameof(IonButton.ChildContent), Label);
            configure?.Invoke(p);
        });

    [Fact]
    public void IonButton_HasCorrectDOMStructure()
    {
        // DOM contract: host <div> → <button class="button-native"> → <span class="button-inner">.
        var cut = RenderButton(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldContain("ion-button");

        var native = cut.Root.Children[0];
        native.TagName.ShouldBe("button");
        native.Class.ShouldBe("button-native");

        var inner = native.Children[0];
        inner.Class.ShouldBe("button-inner");
        cut.GetTextContent().ShouldContain("Default");
    }

    [Fact]
    public void IonButton_DefaultsToSolidFill()
    {
        var cut = RenderButton(Context);
        cut.Root.Class.ShouldBe("md ion-button button-solid");
    }

    [Fact]
    public void IonButton_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        var cut = RenderButton(Context);
        cut.Root.Class.ShouldStartWith("ios ion-button");
    }

    [Theory]
    [InlineData("outline", "button-outline")]
    [InlineData("clear", "button-clear")]
    [InlineData("solid", "button-solid")]
    public void IonButton_StampsFillClass(string fill, string expected)
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.Fill), fill));
        cut.Root.Class.ShouldContain(expected);
    }

    [Theory]
    [InlineData("block", "button-block")]
    [InlineData("full", "button-full")]
    public void IonButton_StampsExpandClass(string expand, string expected)
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.Expand), expand));
        cut.Root.Class.ShouldContain(expected);
    }

    [Theory]
    [InlineData("small", "button-small")]
    [InlineData("large", "button-large")]
    [InlineData("default", "button-default")]
    public void IonButton_StampsSizeClass(string size, string expected)
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.Size), size));
        cut.Root.Class.ShouldContain(expected);
    }

    [Fact]
    public void IonButton_StampsRoundShapeClass()
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.Shape), "round"));
        cut.Root.Class.ShouldContain("button-round");
    }

    [Fact]
    public void IonButton_StampsStrongClass()
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.Strong), true));
        cut.Root.Class.ShouldContain("button-strong");
    }

    [Fact]
    public void IonButton_StampsDisabledClass()
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.Disabled), true));
        cut.Root.Class.ShouldContain("button-disabled");
    }

    [Fact]
    public void IonButton_StampsColorClass()
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.Color), "danger"));
        cut.Root.Class.ShouldContain("ion-color-danger");
    }

    [Fact]
    public void IonButton_StampsIconOnlyMarker_WhenChildIsIconOnlySlot()
    {
        // Ionic adds 'button-has-icon-only' when the only slotted child is an icon-only icon.
        var cut = Context.Render<IonButton>(p =>
            p.AddChildContent(b =>
            {
                b.OpenComponent<IonIcon>(0);
                b.AddComponentParameter(1, nameof(IonIcon.Icon), "heart");
                b.AddComponentParameter(2, nameof(IonIcon.Slot), "icon-only");
                b.CloseComponent();
            }));

        cut.Root.Class.ShouldContain("button-has-icon-only");
    }

    [Fact]
    public void IonButton_OmitsIconOnlyMarker_ForTextButton()
    {
        var cut = RenderButton(Context);
        cut.Root.Class.ShouldNotContain("button-has-icon-only");
    }

    // --- Slots (icon-only / start / end) -------------------------------------------------

    [Fact]
    public void IonButton_RendersStartSlot_BeforeLabel_WrappedInMarker()
    {
        var cut = Context.Render<IonButton>(p =>
        {
            p.Add(nameof(IonButton.Start), (RenderFragment)(b => b.AddContent(0, "S")));
            p.Add(nameof(IonButton.ChildContent), (RenderFragment)(b => b.AddContent(0, "Label")));
        });

        var inner = cut.Root.Children[0].Children[0];
        // start marker span comes first, then the default label content.
        inner.Children[0].Class.ShouldBe("ion-slot-start");
        inner.Children[0].TextContent.ShouldBe("S");
        cut.GetTextContent().ShouldContain("Label");
    }

    [Fact]
    public void IonButton_RendersEndSlot_AfterLabel_WrappedInMarker()
    {
        var cut = Context.Render<IonButton>(p =>
        {
            p.Add(nameof(IonButton.ChildContent), (RenderFragment)(b => b.AddContent(0, "Label")));
            p.Add(nameof(IonButton.End), (RenderFragment)(b => b.AddContent(0, "E")));
        });

        var inner = cut.Root.Children[0].Children[0];
        var last = inner.Children[^1];
        last.Class.ShouldBe("ion-slot-end");
        last.TextContent.ShouldBe("E");
    }

    [Fact]
    public void IonButton_RendersIconOnlySlot_WrappedInMarker()
    {
        var cut = Context.Render<IonButton>(p =>
            p.Add(nameof(IonButton.IconOnly), (RenderFragment)(b =>
            {
                b.OpenComponent<IonIcon>(0);
                b.AddComponentParameter(1, nameof(IonIcon.Icon), "heart");
                b.CloseComponent();
            })));

        var inner = cut.Root.Children[0].Children[0];
        inner.Children[0].Class.ShouldBe("ion-slot-icon-only");
    }

    [Fact]
    public void IonButton_StampsIconOnlyMarker_WhenIconOnlySlotUsed()
    {
        // The icon-only slot alone (no text) makes the host a square icon button.
        var cut = Context.Render<IonButton>(p =>
            p.Add(nameof(IonButton.IconOnly), (RenderFragment)(b =>
            {
                b.OpenComponent<IonIcon>(0);
                b.AddComponentParameter(1, nameof(IonIcon.Icon), "heart");
                b.CloseComponent();
            })));

        cut.Root.Class.ShouldContain("button-has-icon-only");
    }

    private static ComponentUnderTest RenderIconOnlyButton(TestContext ctx,
        Action<ComponentParameterBuilder<IonButton>>? configure = null)
        => ctx.Render<IonButton>(p =>
        {
            p.Add(nameof(IonButton.IconOnly), (RenderFragment)(b =>
            {
                b.OpenComponent<IonIcon>(0);
                b.AddComponentParameter(1, nameof(IonIcon.Icon), "heart");
                b.CloseComponent();
            }));
            configure?.Invoke(p);
        });

    [Fact]
    public void IonButton_IconOnly_HostIsSquare()
    {
        // :host(.button-has-icon-only) — square min-width == min-height.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderIconOnlyButton(Context);
        var style = cut.GetComputedStyle(cut.Root)!;

        style.MinWidth.Value.ShouldBe(40f);
        style.MinHeight.Value.ShouldBe(40f);
        style.MinWidth.Value.ShouldBe(style.MinHeight.Value);
    }

    [Fact]
    public void IonButton_IconOnly_NativeHasZeroPadding()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderIconOnlyButton(Context);
        var native = cut.Root.Children[0];
        var style = cut.GetComputedStyle(native)!;

        style.PaddingLeft.Value.ShouldBe(0f);
        style.PaddingRight.Value.ShouldBe(0f);
        style.PaddingTop.Value.ShouldBe(0f);
        style.PaddingBottom.Value.ShouldBe(0f);
    }

    [Fact]
    public void IonButton_IconOnly_SizesTheIcon()
    {
        // ::slotted(ion-icon[slot="icon-only"]) — the icon-only icon is larger than the default box.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderIconOnlyButton(Context);
        var icon = cut.Root.FindByClass("ion-icon")[0];
        var style = cut.GetComputedStyle(icon)!;

        // md default icon-only icon size.
        style.Width.Value.ShouldBe(22.4f);
        style.Height.Value.ShouldBe(22.4f);
    }

    [Fact]
    public void IonButton_SmallIconOnly_UsesSmallerSquareAndIcon()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderIconOnlyButton(Context, p => p.Add(nameof(IonButton.Size), "small"));
        var hostStyle = cut.GetComputedStyle(cut.Root)!;
        var iconStyle = cut.GetComputedStyle(cut.Root.FindByClass("ion-icon")[0])!;

        hostStyle.MinWidth.Value.ShouldBe(28f);
        hostStyle.MinHeight.Value.ShouldBe(28f);
        iconStyle.Width.Value.ShouldBe(16f);
    }

    [Fact]
    public void IonButton_LargeIconOnly_UsesLargerSquareAndIcon()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderIconOnlyButton(Context, p => p.Add(nameof(IonButton.Size), "large"));
        var hostStyle = cut.GetComputedStyle(cut.Root)!;
        var iconStyle = cut.GetComputedStyle(cut.Root.FindByClass("ion-icon")[0])!;

        hostStyle.MinWidth.Value.ShouldBe(50f);
        hostStyle.MinHeight.Value.ShouldBe(50f);
        iconStyle.Width.Value.ShouldBe(28f);
    }

    // --- Native surface min-height mirrors the host (Ionic: min-height: inherit) --------------
    // In Ionic .button-native uses `min-height: inherit`, so it follows whatever min-height the
    // host resolved. Miko has no `inherit` keyword, so ButtonStyles mirrors the host value onto
    // the native surface in every variant that changes it. These guard that the native surface
    // never lags behind the host (issues/ion-button.md #4).

    [Fact]
    public void IonButton_Default_NativeMinHeightMatchesHost()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context);
        var native = cut.Root.Children[0];

        // md default host min-height is 36px; the native surface mirrors it.
        cut.GetComputedStyle(native)!.MinHeight.Value.ShouldBe(36f);
    }

    [Fact]
    public void IonButton_IconOnly_NativeMinHeightMatchesHost()
    {
        // The bug in #4: the icon-only host grows to 40px but the native surface stayed at 36px.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderIconOnlyButton(Context);
        var hostStyle = cut.GetComputedStyle(cut.Root)!;
        var nativeStyle = cut.GetComputedStyle(cut.Root.Children[0])!;

        hostStyle.MinHeight.Value.ShouldBe(40f);
        nativeStyle.MinHeight.Value.ShouldBe(hostStyle.MinHeight.Value);
    }

    [Fact]
    public void IonButton_SmallIconOnly_NativeMinHeightMatchesHost()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderIconOnlyButton(Context, p => p.Add(nameof(IonButton.Size), "small"));
        var hostStyle = cut.GetComputedStyle(cut.Root)!;
        var nativeStyle = cut.GetComputedStyle(cut.Root.Children[0])!;

        hostStyle.MinHeight.Value.ShouldBe(28f);
        nativeStyle.MinHeight.Value.ShouldBe(hostStyle.MinHeight.Value);
    }

    [Fact]
    public void IonButton_LargeIconOnly_NativeMinHeightMatchesHost()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderIconOnlyButton(Context, p => p.Add(nameof(IonButton.Size), "large"));
        var hostStyle = cut.GetComputedStyle(cut.Root)!;
        var nativeStyle = cut.GetComputedStyle(cut.Root.Children[0])!;

        hostStyle.MinHeight.Value.ShouldBe(50f);
        nativeStyle.MinHeight.Value.ShouldBe(hostStyle.MinHeight.Value);
    }

    [Fact]
    public void IonButton_OmitsSlotMarkers_WhenNoSlotContent()
    {
        var cut = RenderButton(Context);
        var inner = cut.Root.Children[0].Children[0];
        inner.FindByClass("ion-slot-start").ShouldBeEmpty();
        inner.FindByClass("ion-slot-end").ShouldBeEmpty();
        inner.FindByClass("ion-slot-icon-only").ShouldBeEmpty();
    }

    [Fact]
    public void IonButton_StartSlotMarker_DoesNotShrink()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonButton>(p =>
        {
            p.Add(nameof(IonButton.Start), (RenderFragment)(b => b.AddContent(0, "S")));
            p.Add(nameof(IonButton.ChildContent), (RenderFragment)(b => b.AddContent(0, "Label")));
        });

        var inner = cut.Root.Children[0].Children[0];
        var startMarker = inner.Children[0];
        var style = cut.GetComputedStyle(startMarker)!;
        style.FlexShrink.ShouldBe(0f);
    }

    // --- Key style assertions (theme-driven) ---------------------------------------------

    [Fact]
    public void IonButton_SolidFill_UsesPrimaryBackground_WhiteLabel()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context);
        var native = cut.Root.Children[0];
        var style = cut.GetComputedStyle(native);

        style.ShouldNotBeNull();
        // Default solid fill is the primary brand color with a white (contrast) label.
        style.BackgroundColor.ShouldBe(Color.FromHex("0054e9"));
        style.Color.ShouldBe(Color.FromHex("ffffff"));
    }

    [Fact]
    public void IonButton_OutlineFill_IsTransparent_WithPrimaryBorder()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.Fill), "outline"));
        var native = cut.Root.Children[0];
        var style = cut.GetComputedStyle(native)!;

        style.BackgroundColor.ShouldBe(Color.Transparent);
        style.Color.ShouldBe(Color.FromHex("0054e9"));
        // md outline border is 2px.
        style.BorderTopWidth.Value.ShouldBe(2f);
    }

    [Fact]
    public void IonButton_ColoredSolid_UsesColorBackground()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.Color), "danger"));
        var native = cut.Root.Children[0];
        var style = cut.GetComputedStyle(native)!;

        style.BackgroundColor.ShouldBe(Color.FromHex("c5000f"));
    }

    [Fact]
    public void IonButton_DisabledHost_IsDimmed()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.Disabled), true));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Opacity.ShouldBe(0.5f);
    }

    [Fact]
    public void IonButton_MdHost_UppercasesLabel()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context);
        var style = cut.GetComputedStyle(cut.Root)!;

        // md transforms the label to uppercase; iOS does not.
        style.TextTransform.ShouldBe(TextTransform.Uppercase);
    }

    // --- Interaction ---------------------------------------------------------------------

    [Fact]
    public void IonButton_InvokesOnClick_WhenTapped()
    {
        var clicked = false;
        var cut = RenderButton(Context, p => p.Add(nameof(IonButton.OnClick),
            EventCallback.Factory.Create(this, () => clicked = true)));

        var native = cut.Root.Children[0];
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        clicked.ShouldBeTrue();
    }

    [Fact]
    public void IonButton_DoesNotInvokeOnClick_WhenDisabled()
    {
        var clicked = false;
        var cut = RenderButton(Context, p =>
        {
            p.Add(nameof(IonButton.Disabled), true);
            p.Add(nameof(IonButton.OnClick),
                EventCallback.Factory.Create(this, () => clicked = true));
        });

        var native = cut.Root.Children[0];
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        clicked.ShouldBeFalse();
    }
}
