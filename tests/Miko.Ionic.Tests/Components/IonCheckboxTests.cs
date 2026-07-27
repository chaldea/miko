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
        cut.Root.ShouldHaveClass("md ion-checkbox");
        cut.Root.ShouldHaveClass("checkbox-label-placement-start");

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

        cut.Root.ShouldHaveClass(expected);
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

        cut.Root.ShouldHaveClass("checkbox-justify-space-between");
        cut.Root.ShouldHaveClass("checkbox-alignment-center");
        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-danger");
    }

    [Fact]
    public void IonCheckbox_EmptyLabel_HidesLabelWrapper()
    {
        var cut = RenderCheckbox(Context, label: null);

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldHaveClass("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonCheckbox_WithLabel_DoesNotHideLabelWrapper()
    {
        var cut = RenderCheckbox(Context, label: "Accept");

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldNotHaveClass("label-text-wrapper-hidden");
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

        cut.Root.ShouldHaveClass("checkbox-checked");
        cut.FindByClass("checkbox-native").Single().ShouldBeOfType<InputElement>().Checked.ShouldBeTrue();
    }

    [Fact]
    public void IonCheckbox_Unchecked_DoesNotStampCheckedClass()
    {
        var cut = RenderCheckbox(Context);

        cut.Root.ShouldNotHaveClass("checkbox-checked");
        cut.FindByClass("checkbox-native").Single().ShouldBeOfType<InputElement>().Checked.ShouldBeFalse();
    }

    [Fact]
    public void IonCheckbox_Indeterminate_StampsClass()
    {
        var cut = RenderCheckbox(Context, p => p.Add(nameof(IonCheckbox.Indeterminate), true));

        cut.Root.ShouldHaveClass("checkbox-indeterminate");
    }

    [Fact]
    public void IonCheckbox_Disabled_StampsClassAndNativeState()
    {
        var cut = RenderCheckbox(Context, p => p.Add(nameof(IonCheckbox.Disabled), true));

        cut.Root.ShouldHaveClass("checkbox-disabled");
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

    // ---- Issue regressions (issues/ion-checkbox.md) -------------------------

    [Fact]
    public void IonCheckbox_Style_LabelWrapperEllipsizesOverflowingText()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckbox(Context);
        var label = cut.FindByClass("label-text-wrapper").Single();
        var style = cut.GetComputedStyle(label)!;

        // checkbox.scss .label-text-wrapper: text-overflow: ellipsis; white-space: nowrap; overflow: hidden.
        style.TextOverflow.ShouldBe(TextOverflow.Ellipsis);
        style.WhiteSpace.ShouldBe(WhiteSpace.Nowrap);
        style.OverflowX.ShouldBe(Overflow.Hidden);
    }

    [Fact]
    public void IonCheckbox_Style_PlacementEnd_WrapperIsRowReverse()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckbox(Context, p => p.Add(nameof(IonCheckbox.LabelPlacement), "end"));
        var wrapper = cut.FindByClass("checkbox-wrapper").Single();

        cut.GetComputedStyle(wrapper)!.FlexDirection.ShouldBe(FlexDirection.RowReverse);
    }

    [Fact]
    public void IonCheckbox_Layout_PlacementStart_LabelBeforeBox()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckbox(Context, label: "Label at the Start");

        var labelBox = cut.GetBoxModel(cut.FindByClass("label-text-wrapper").Single())!;
        var nativeBox = cut.GetBoxModel(cut.FindByClass("native-wrapper").Single())!;
        labelBox.Content.X.ShouldBeLessThan(nativeBox.Content.X);
    }

    [Fact]
    public void IonCheckbox_Layout_PlacementEnd_BoxBeforeLabel()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckbox(Context,
            p => p.Add(nameof(IonCheckbox.LabelPlacement), "end"), label: "Label at the End");

        // row-reverse mirrors the main axis: the box renders before (left of) the label.
        var labelBox = cut.GetBoxModel(cut.FindByClass("label-text-wrapper").Single())!;
        var nativeBox = cut.GetBoxModel(cut.FindByClass("native-wrapper").Single())!;
        nativeBox.Content.X.ShouldBeLessThan(labelBox.Content.X);
    }

    [Fact]
    public void IonCheckbox_Style_InItem_HostGrowsToFillItem()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckboxInItem(Context);

        // checkbox.scss :host(.in-item): flex: 1 1 0; width: 100%; height: 100%.
        var host = cut.FindByClass("ion-checkbox").Single();
        var style = cut.GetComputedStyle(host)!;
        style.FlexGrow.ShouldBe(1f);
        style.FlexShrink.ShouldBe(1f);
        style.FlexBasis.ShouldBe(Length.Px(0));
    }

    [Fact]
    public void IonCheckbox_Style_InItem_LabelAndBoxGetVerticalMargins()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckboxInItem(Context);

        // $checkbox-item-label-margin-top/bottom = 10px on label + native wrappers.
        var label = cut.GetComputedStyle(cut.FindByClass("label-text-wrapper").Single())!;
        label.MarginTop.ShouldBe(Length.Px(10));
        label.MarginBottom.ShouldBe(Length.Px(10));

        var native = cut.GetComputedStyle(cut.FindByClass("native-wrapper").Single())!;
        native.MarginTop.ShouldBe(Length.Px(10));
        native.MarginBottom.ShouldBe(Length.Px(10));
    }

    [Fact]
    public void IonCheckbox_Style_InItemStacked_SwapsLabelBottomMargin()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckboxInItem(Context, labelPlacement: "stacked");

        // Stacked in-item: label bottom margin becomes $form-control-label-margin (16px),
        // the box loses its top margin.
        var label = cut.GetComputedStyle(cut.FindByClass("label-text-wrapper").Single())!;
        label.MarginTop.ShouldBe(Length.Px(10));
        label.MarginBottom.ShouldBe(Length.Px(16));

        var native = cut.GetComputedStyle(cut.FindByClass("native-wrapper").Single())!;
        native.MarginTop.ShouldBe(Length.Px(0));
        native.MarginBottom.ShouldBe(Length.Px(10));
    }

    [Fact]
    public void IonCheckbox_Layout_InItem_JustifyEnd_PacksContentToTrailingEdge()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckboxInItem(Context, justify: "end");

        // With the host stretched to the item's content area, justify="end" packs the label
        // and box to the trailing (right) edge of the host.
        var hostBox = cut.GetBoxModel(cut.FindByClass("ion-checkbox").Single())!;
        var nativeBox = cut.GetBoxModel(cut.FindByClass("native-wrapper").Single())!;
        hostBox.Content.Width.ShouldBeGreaterThan(nativeBox.MarginBox.Width + 50);
        nativeBox.MarginBox.Right.ShouldBe(hostBox.Content.Right, 0.5f);
    }

    [Fact]
    public void IonCheckbox_Layout_InItem_JustifySpaceBetween_PushesBoxToTrailingEdge()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderCheckboxInItem(Context, justify: "space-between");

        var hostBox = cut.GetBoxModel(cut.FindByClass("ion-checkbox").Single())!;
        var labelBox = cut.GetBoxModel(cut.FindByClass("label-text-wrapper").Single())!;
        var nativeBox = cut.GetBoxModel(cut.FindByClass("native-wrapper").Single())!;
        labelBox.MarginBox.Left.ShouldBe(hostBox.Content.Left, 0.5f);
        nativeBox.MarginBox.Right.ShouldBe(hostBox.Content.Right, 0.5f);
    }

    private static ComponentUnderTest RenderCheckboxInItem(TestContext ctx,
        string? justify = null, string? labelPlacement = null, string label = "Packed in the Item")
        => ctx.Render<IonItem>(p => p.Add(nameof(IonItem.ChildContent), (RenderFragment)(b =>
        {
            b.OpenComponent<IonCheckbox>(0);
            b.AddAttribute(1, nameof(IonCheckbox.ChildContent), Label(label));
            if (justify is not null) b.AddAttribute(2, nameof(IonCheckbox.Justify), justify);
            if (labelPlacement is not null) b.AddAttribute(3, nameof(IonCheckbox.LabelPlacement), labelPlacement);
            b.CloseComponent();
        })));
}
