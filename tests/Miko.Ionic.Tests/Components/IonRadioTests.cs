using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-radio</c>. Covers the DOM contract (label wrapper, hidden native input, the
/// icon/inner mark), the ios vs md mode class, the checked state derived from the enclosing group,
/// selecting via the group on click, and the disabled no-op.
/// </summary>
public class IonRadioTests : IonicComponentTestBase
{
    private static RenderFragment Label(string value) => builder => builder.AddContent(0, value);

    // A radio-group hosting radios; the group cascades IonRadioGroupContext to them.
    private static ComponentUnderTest RenderGroup(TestContext ctx, RenderFragment radios,
        Action<ComponentParameterBuilder<IonRadioGroup>>? configure = null)
        => ctx.Render<IonRadioGroup>(p =>
        {
            p.Add(nameof(IonRadioGroup.ChildContent), radios);
            configure?.Invoke(p);
        });

    // A single radio with the given value/label and optional simple attributes. Children are built
    // through the raw RenderTreeBuilder (ComponentParameterBuilder is only for the ctx.Render root).
    private static RenderFragment Radio(string value, string? label = "Option",
        bool disabled = false, string? color = null, string? justify = null,
        string? alignment = null, string? labelPlacement = null) => builder =>
    {
        int seq = 0;
        builder.OpenComponent<IonRadio>(seq++);
        builder.AddComponentParameter(seq++, nameof(IonRadio.Value), value);
        if (label is not null)
            builder.AddComponentParameter(seq++, nameof(IonRadio.ChildContent), Label(label));
        if (disabled)
            builder.AddComponentParameter(seq++, nameof(IonRadio.Disabled), true);
        if (color is not null)
            builder.AddComponentParameter(seq++, nameof(IonRadio.Color), color);
        if (justify is not null)
            builder.AddComponentParameter(seq++, nameof(IonRadio.Justify), justify);
        if (alignment is not null)
            builder.AddComponentParameter(seq++, nameof(IonRadio.Alignment), alignment);
        if (labelPlacement is not null)
            builder.AddComponentParameter(seq++, nameof(IonRadio.LabelPlacement), labelPlacement);
        builder.CloseComponent();
    };

    // Two radios: "a" and "b".
    private static RenderFragment TwoRadios() => builder =>
    {
        int seq = 0;
        foreach (var v in new[] { "a", "b" })
        {
            var captured = v;
            builder.OpenComponent<IonRadio>(seq++);
            builder.AddComponentParameter(seq++, nameof(IonRadio.Value), captured);
            builder.AddComponentParameter(seq++, nameof(IonRadio.ChildContent), Label(captured.ToUpperInvariant()));
            builder.CloseComponent();
        }
    };

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonRadio_RendersDomContract()
    {
        var cut = RenderGroup(Context, Radio("a", "First option"));

        var radio = cut.FindByClass("ion-radio").ShouldHaveSingleItem();
        radio.TagName.ShouldBe("div");
        radio.ShouldHaveClass("md ion-radio");
        radio.ShouldHaveClass("radio-label-placement-start");

        var wrapper = radio.FindByClass("radio-wrapper").Single();
        wrapper.TagName.ShouldBe("label");

        var input = radio.FindByClass("radio-native").Single();
        input.TagName.ShouldBe("input");
        input.ShouldBeOfType<InputElement>().Type.ShouldBe(InputType.Radio);

        radio.FindByClass("label-text-wrapper").ShouldHaveSingleItem();
        radio.FindByClass("native-wrapper").ShouldHaveSingleItem();
        radio.FindByClass("radio-icon").ShouldHaveSingleItem();
        radio.FindByClass("radio-inner").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("First option");
    }

    [Fact]
    public void IonRadio_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderGroup(Context, Radio("a"));

        cut.FindByClass("ion-radio").Single().ShouldHaveClass("ios ion-radio");
    }

    [Theory]
    [InlineData("start", "radio-label-placement-start")]
    [InlineData("end", "radio-label-placement-end")]
    [InlineData("fixed", "radio-label-placement-fixed")]
    [InlineData("stacked", "radio-label-placement-stacked")]
    public void IonRadio_StampsLabelPlacementClass(string placement, string expected)
    {
        var cut = RenderGroup(Context, Radio("a", "Option", labelPlacement: placement));

        cut.FindByClass("ion-radio").Single().ShouldHaveClass(expected);
    }

    [Fact]
    public void IonRadio_StampsJustifyAlignmentAndColorClasses()
    {
        var cut = RenderGroup(Context,
            Radio("a", "Option", justify: "space-between", alignment: "center", color: "danger"));

        var radio = cut.FindByClass("ion-radio").Single();
        radio.ShouldHaveClass("radio-justify-space-between");
        radio.ShouldHaveClass("radio-alignment-center");
        radio.ShouldHaveClass("ion-color");
        radio.ShouldHaveClass("ion-color-danger");
    }

    [Fact]
    public void IonRadio_EmptyLabel_HidesLabelWrapper()
    {
        var cut = RenderGroup(Context, Radio("a", label: null));

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldHaveClass("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonRadio_WithLabel_DoesNotHideLabelWrapper()
    {
        var cut = RenderGroup(Context, Radio("a", "Accept"));

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldNotHaveClass("label-text-wrapper-hidden");
    }

    // ---- Checked derives from the group -----------------------------------

    [Fact]
    public void IonRadio_Unchecked_WhenGroupValueDiffers()
    {
        var cut = RenderGroup(Context, TwoRadios(),
            p => p.Add(nameof(IonRadioGroup.Value), "a"));

        var radios = cut.FindByClass("ion-radio");
        radios[0].ShouldHaveClass("radio-checked");
        radios[1].ShouldNotHaveClass("radio-checked");
    }

    [Fact]
    public void IonRadio_Checked_SyncsNativeInput()
    {
        var cut = RenderGroup(Context, Radio("a"),
            p => p.Add(nameof(IonRadioGroup.Value), "a"));

        cut.FindByClass("ion-radio").Single().ShouldHaveClass("radio-checked");
        cut.FindByClass("radio-native").Single()
            .ShouldBeOfType<InputElement>().Checked.ShouldBeTrue();
    }

    [Fact]
    public void IonRadio_Standalone_IsNeverChecked()
    {
        // A radio with no enclosing group has no selection source, so it is never checked.
        var cut = Context.Render<IonRadio>(p =>
        {
            p.Add(nameof(IonRadio.Value), "a");
            p.Add(nameof(IonRadio.ChildContent), Label("Solo"));
        });

        cut.Root.ShouldNotHaveClass("radio-checked");
    }

    // ---- Interaction -------------------------------------------------------

    [Fact]
    public void IonRadio_Click_SelectsViaGroup()
    {
        string? changed = null;
        var cut = RenderGroup(Context, TwoRadios(), p =>
            p.Add(nameof(IonRadioGroup.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => changed = v)));

        // Click the second radio's wrapper — it asks the group to select "b".
        var secondWrapper = cut.FindByClass("ion-radio")[1].FindByClass("radio-wrapper").Single();
        secondWrapper.OnClick!.Invoke(new MouseEventArgs { Target = secondWrapper });

        changed.ShouldBe("b");
    }

    [Fact]
    public void IonRadio_Disabled_StampsClassAndIsNoOpOnClick()
    {
        string? changed = null;
        var cut = RenderGroup(Context,
            Radio("a", "Option", disabled: true),
            g => g.Add(nameof(IonRadioGroup.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => changed = v)));

        var radio = cut.FindByClass("ion-radio").Single();
        radio.ShouldHaveClass("radio-disabled");
        radio.FindByClass("radio-native").Single().IsDisabled.ShouldBeTrue();

        var wrapper = radio.FindByClass("radio-wrapper").Single();
        wrapper.OnClick!.Invoke(new MouseEventArgs { Target = wrapper });

        changed.ShouldBeNull();
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonRadio_Style_HostIsInlineBlock()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, Radio("a"));
        var radio = cut.FindByClass("ion-radio").Single();

        cut.GetComputedStyle(radio)!.Display.ShouldBe(Display.InlineBlock);
    }

    [Fact]
    public void IonRadio_Style_MdIconUsesRingSizeAndBorder()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, Radio("a"));
        var icon = cut.FindByClass("radio-icon").Single();
        var style = cut.GetComputedStyle(icon)!;

        // md: 20px bordered ring.
        style.Width.ShouldBe(Length.Px(20));
        style.Height.ShouldBe(Length.Px(20));
        style.BorderTopWidth.ShouldBe(Length.Px(2));
    }

    [Fact]
    public void IonRadio_Style_IosIconUsesIosSize()
    {
        UsePlatform(HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, Radio("a"));
        var icon = cut.FindByClass("radio-icon").Single();

        // ios: 15x24, no ring.
        cut.GetComputedStyle(icon)!.Width.ShouldBe(Length.Px(15));
    }

    [Fact]
    public void IonRadio_Style_CheckedRevealsInner()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, Radio("a"),
            p => p.Add(nameof(IonRadioGroup.Value), "a"));

        var inner = cut.FindByClass("radio-inner").Single();
        cut.GetComputedStyle(inner)!.Opacity.ShouldBe(1f);
    }

    [Fact]
    public void IonRadio_Style_UncheckedHidesInner()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, Radio("a"));

        var inner = cut.FindByClass("radio-inner").Single();
        cut.GetComputedStyle(inner)!.Opacity.ShouldBe(0f);
    }
}
