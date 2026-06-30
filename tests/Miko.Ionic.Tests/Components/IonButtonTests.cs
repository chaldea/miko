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
