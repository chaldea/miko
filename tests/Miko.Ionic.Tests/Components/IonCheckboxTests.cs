using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonCheckboxTests : IonicComponentTestBase
{
    private static RenderFragment Label(string value) => builder => builder.AddContent(0, value);

    private static ComponentUnderTest RenderCheckbox(TestContext ctx,
        Action<ComponentParameterBuilder<IonCheckbox>>? configure = null,
        string? label = "I agree")
        => ctx.Render<IonCheckbox>(p =>
        {
            if (label is not null) p.Add(nameof(IonCheckbox.ChildContent), Label(label));
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonCheckbox_RendersDomContract()
    {
        var cut = RenderCheckbox(Context, label: "I agree to the terms");

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldContain("md ion-checkbox");
        cut.Root.Class.ShouldContain("checkbox-label-placement-start");

        var wrapper = cut.FindByClass("checkbox-wrapper").Single();
        wrapper.TagName.ShouldBe("label");

        var input = cut.FindByClass("checkbox-native").Single();
        input.TagName.ShouldBe("input");
        input.ShouldBeOfType<InputElement>().Type.ShouldBe(InputType.Checkbox);

        cut.FindByClass("label-text-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("native-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("checkbox-icon").ShouldHaveSingleItem();
        cut.FindByClass("checkbox-icon-mark").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("I agree to the terms");
    }

    [Fact]
    public void IonCheckbox_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderCheckbox(Context);

        cut.Root.Class.ShouldStartWith("ios ion-checkbox");
    }

    [Theory]
    [InlineData("start", "checkbox-label-placement-start")]
    [InlineData("end", "checkbox-label-placement-end")]
    [InlineData("fixed", "checkbox-label-placement-fixed")]
    [InlineData("stacked", "checkbox-label-placement-stacked")]
    public void IonCheckbox_StampsLabelPlacementClass(string placement, string expected)
    {
        var cut = RenderCheckbox(Context, p => p.Add(nameof(IonCheckbox.LabelPlacement), placement));

        cut.Root.Class.ShouldContain(expected);
    }

    [Fact]
    public void IonCheckbox_StampsJustifyAlignmentAndColorClasses()
    {
        var cut = RenderCheckbox(Context, p =>
        {
            p.Add(nameof(IonCheckbox.Justify), "space-between");
            p.Add(nameof(IonCheckbox.Alignment), "center");
            p.Add(nameof(IonCheckbox.Color), "danger");
        });

        cut.Root.Class.ShouldContain("checkbox-justify-space-between");
        cut.Root.Class.ShouldContain("checkbox-alignment-center");
        cut.Root.Class.ShouldContain("ion-color");
        cut.Root.Class.ShouldContain("ion-color-danger");
    }

    [Fact]
    public void IonCheckbox_EmptyLabel_HidesLabelWrapper()
    {
        var cut = RenderCheckbox(Context, label: null);

        cut.FindByClass("label-text-wrapper").Single()
            .Class.ShouldContain("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonCheckbox_WithLabel_DoesNotHideLabelWrapper()
    {
        var cut = RenderCheckbox(Context, label: "Accept");

        cut.FindByClass("label-text-wrapper").Single()
            .Class.ShouldNotContain("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonCheckbox_RendersHelperAndErrorText()
    {
        var cut = RenderCheckbox(Context, p =>
        {
            p.Add(nameof(IonCheckbox.HelperText), "Optional");
            p.Add(nameof(IonCheckbox.ErrorText), "Required");
        });

        cut.FindByClass("checkbox-bottom").ShouldHaveSingleItem();
        cut.FindByClass("helper-text").Single().TextContent.ShouldBe("Optional");
        cut.FindByClass("error-text").Single().TextContent.ShouldBe("Required");
    }

    [Fact]
    public void IonCheckbox_NoHintText_OmitsCheckboxBottom()
    {
        var cut = RenderCheckbox(Context);

        cut.FindByClass("checkbox-bottom").ShouldBeEmpty();
    }

    // ---- State / interaction ----------------------------------------------

    [Fact]
    public void IonCheckbox_Checked_StampsClassAndSyncsNativeInput()
    {
        var cut = RenderCheckbox(Context, p => p.Add(nameof(IonCheckbox.Checked), true));

        cut.Root.Class.ShouldContain("checkbox-checked");
        cut.FindByClass("checkbox-native").Single().ShouldBeOfType<InputElement>().Checked.ShouldBeTrue();
    }

    [Fact]
    public void IonCheckbox_Unchecked_DoesNotStampCheckedClass()
    {
        var cut = RenderCheckbox(Context);

        cut.Root.Class.ShouldNotContain("checkbox-checked");
        cut.FindByClass("checkbox-native").Single().ShouldBeOfType<InputElement>().Checked.ShouldBeFalse();
    }

    [Fact]
    public void IonCheckbox_Indeterminate_StampsClass()
    {
        var cut = RenderCheckbox(Context, p => p.Add(nameof(IonCheckbox.Indeterminate), true));

        cut.Root.Class.ShouldContain("checkbox-indeterminate");
    }

    [Fact]
    public void IonCheckbox_Disabled_StampsClassAndNativeState()
    {
        var cut = RenderCheckbox(Context, p => p.Add(nameof(IonCheckbox.Disabled), true));

        cut.Root.Class.ShouldContain("checkbox-disabled");
        cut.FindByClass("checkbox-native").Single().IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void IonCheckbox_Click_TogglesAndRaisesCallbacks()
    {
        bool? changed = null;
        bool? ionChanged = null;
        var cut = RenderCheckbox(Context, p =>
        {
            p.Add(nameof(IonCheckbox.CheckedChanged),
                EventCallback.Factory.Create<bool>(this, v => changed = v));
            p.Add(nameof(IonCheckbox.IonChange),
                EventCallback.Factory.Create<bool>(this, v => ionChanged = v));
        });

        var wrapper = cut.FindByClass("checkbox-wrapper").Single();
        wrapper.OnClick!.Invoke(new MouseEventArgs { Target = wrapper });

        changed.ShouldBe(true);
        ionChanged.ShouldBe(true);
    }

    [Fact]
    public void IonCheckbox_Click_WhenDisabled_DoesNotToggle()
    {
        bool? changed = null;
        var cut = RenderCheckbox(Context, p =>
        {
            p.Add(nameof(IonCheckbox.Disabled), true);
            p.Add(nameof(IonCheckbox.CheckedChanged),
                EventCallback.Factory.Create<bool>(this, v => changed = v));
        });

        var wrapper = cut.FindByClass("checkbox-wrapper").Single();
        wrapper.OnClick!.Invoke(new MouseEventArgs { Target = wrapper });

        changed.ShouldBeNull();
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonCheckbox_Style_HostIsInlineBlock()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckbox(Context);
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.InlineBlock);
    }

    [Fact]
    public void IonCheckbox_Style_JustifySwitchesHostToBlock()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckbox(Context, p => p.Add(nameof(IonCheckbox.Justify), "space-between"));

        cut.GetComputedStyle(cut.Root)!.Display.ShouldBe(Display.Block);
    }

    [Fact]
    public void IonCheckbox_Style_MdBoxUsesMdSize()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckbox(Context);
        var icon = cut.FindByClass("checkbox-icon").Single();
        var style = cut.GetComputedStyle(icon)!;

        style.Width.ShouldBe(Length.Px(18));
        style.Height.ShouldBe(Length.Px(18));
        style.BorderTopWidth.ShouldBe(Length.Px(2));
    }

    [Fact]
    public void IonCheckbox_Style_IosBoxUsesIosSize()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckbox(Context);
        var icon = cut.FindByClass("checkbox-icon").Single();

        cut.GetComputedStyle(icon)!.Width.ShouldBe(Length.Px(22));
    }

    [Fact]
    public void IonCheckbox_Style_CheckedFillsBoxAndRevealsMark()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckbox(Context, p => p.Add(nameof(IonCheckbox.Checked), true));

        var mark = cut.FindByClass("checkbox-icon-mark").Single();
        cut.GetComputedStyle(mark)!.Opacity.ShouldBe(1f);
    }

    [Fact]
    public void IonCheckbox_Style_UncheckedHidesMark()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckbox(Context);

        var mark = cut.FindByClass("checkbox-icon-mark").Single();
        cut.GetComputedStyle(mark)!.Opacity.ShouldBe(0f);
    }
}
