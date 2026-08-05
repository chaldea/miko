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

    // ---- Justify + LabelPlacement together (ISSUE-116 problem 4) -----------

    // Renders a standalone checkbox against a known viewport width, so the block-level host has
    // real free main-axis space for justify-content to distribute.
    private ComponentUnderTest RenderStyled(string? justify, string? placement,
        string? color = null, bool isChecked = false, string label = "Jon Snow")
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.ViewportWidth = 400;
        return Context.Render<IonCheckbox>(p =>
        {
            p.Add(nameof(IonCheckbox.ChildContent), Label(label));
            if (justify is not null) p.Add(nameof(IonCheckbox.Justify), justify);
            if (placement is not null) p.Add(nameof(IonCheckbox.LabelPlacement), placement);
            if (color is not null) p.Add(nameof(IonCheckbox.Color), color);
            if (isChecked) p.Add(nameof(IonCheckbox.Checked), true);
        });
    }

    /// <summary>
    /// Justify="start" + LabelPlacement="end": the pair packs to the LEFT while the label still
    /// follows the box. Ionic uses the absolute `justify-content: start` keyword, which does not
    /// flip under the row-reverse that label-placement-end applies — flex-start would push the
    /// pair to the right instead.
    /// </summary>
    [Fact]
    public void IonCheckbox_Layout_JustifyStartWithPlacementEnd_PacksToLeadingEdge()
    {
        var cut = RenderStyled(justify: "start", placement: "end");

        var wrapperBox = cut.GetBoxModel(cut.FindByClass("checkbox-wrapper").Single())!;
        var labelBox = cut.GetBoxModel(cut.FindByClass("label-text-wrapper").Single())!;
        var nativeBox = cut.GetBoxModel(cut.FindByClass("native-wrapper").Single())!;

        // The host is block-level, so the wrapper spans the viewport and there IS free space.
        wrapperBox.Content.Width.ShouldBeGreaterThan(nativeBox.MarginBox.Width + 100);
        // Whole group at the leading edge (this is what regressed: it sat at the trailing edge).
        nativeBox.MarginBox.Left.ShouldBe(wrapperBox.Content.Left, 0.5f);
        // Label still follows the box (row-reverse ordering preserved).
        nativeBox.Content.X.ShouldBeLessThan(labelBox.Content.X);
        // And the group does not reach the trailing edge.
        labelBox.MarginBox.Right.ShouldBeLessThan(wrapperBox.Content.Right - 100);
    }

    /// <summary>
    /// Justify="start" + LabelPlacement="end" resolves to the absolute Start keyword, so the
    /// explicit Justify wins over the justify-content that label-placement-end sets for itself
    /// (equal specificity — Ionic relies on source order, with the justify rules declared last).
    /// </summary>
    [Fact]
    public void IonCheckbox_Style_JustifyOverridesPlacementEndJustification()
    {
        var cut = RenderStyled(justify: "start", placement: "end");

        var wrapper = cut.FindByClass("checkbox-wrapper").Single();
        cut.GetComputedStyle(wrapper)!.JustifyContent.ShouldBe(JustifyContent.Start);
        cut.GetComputedStyle(wrapper)!.FlexDirection.ShouldBe(FlexDirection.RowReverse);
    }

    /// <summary>
    /// LabelPlacement="end" with no Justify still packs to the leading edge — checkbox.scss's own
    /// `justify-content: start` on label-placement-end (also the absolute keyword).
    /// </summary>
    [Fact]
    public void IonCheckbox_Layout_PlacementEndAlone_PacksToLeadingEdge()
    {
        var cut = RenderStyled(justify: null, placement: "end");

        var wrapper = cut.FindByClass("checkbox-wrapper").Single();
        cut.GetComputedStyle(wrapper)!.JustifyContent.ShouldBe(JustifyContent.Start);

        var nativeBox = cut.GetBoxModel(cut.FindByClass("native-wrapper").Single())!;
        var wrapperBox = cut.GetBoxModel(wrapper)!;
        nativeBox.MarginBox.Left.ShouldBe(wrapperBox.Content.Left, 0.5f);
    }

    /// <summary>Justify="end" + LabelPlacement="end": the pair packs to the trailing edge.</summary>
    [Fact]
    public void IonCheckbox_Layout_JustifyEndWithPlacementEnd_PacksToTrailingEdge()
    {
        var cut = RenderStyled(justify: "end", placement: "end");

        var wrapperBox = cut.GetBoxModel(cut.FindByClass("checkbox-wrapper").Single())!;
        var labelBox = cut.GetBoxModel(cut.FindByClass("label-text-wrapper").Single())!;
        var nativeBox = cut.GetBoxModel(cut.FindByClass("native-wrapper").Single())!;

        labelBox.MarginBox.Right.ShouldBe(wrapperBox.Content.Right, 0.5f);
        // Ordering unchanged: box still precedes the label.
        nativeBox.Content.X.ShouldBeLessThan(labelBox.Content.X);
    }

    /// <summary>Justify="start" with the default (start) placement keeps the label before the box.</summary>
    [Fact]
    public void IonCheckbox_Layout_JustifyStartWithPlacementStart_PacksToLeadingEdge()
    {
        var cut = RenderStyled(justify: "start", placement: "start");

        var wrapperBox = cut.GetBoxModel(cut.FindByClass("checkbox-wrapper").Single())!;
        var labelBox = cut.GetBoxModel(cut.FindByClass("label-text-wrapper").Single())!;
        var nativeBox = cut.GetBoxModel(cut.FindByClass("native-wrapper").Single())!;

        labelBox.MarginBox.Left.ShouldBe(wrapperBox.Content.Left, 0.5f);
        labelBox.Content.X.ShouldBeLessThan(nativeBox.Content.X);
    }

    // ---- Color (ISSUE-116 problem 5) ---------------------------------------

    /// <summary>
    /// A named Color fills the checked box with that palette color. checkbox.scss's
    /// `:host(.ion-color)` redefines --checkbox-background-checked / --border-color-checked; the
    /// port resolves those into ion-color-* scoped rules. Previously the class was stamped but no
    /// rule matched it, so Color did nothing.
    /// </summary>
    [Theory]
    [InlineData("danger")]
    [InlineData("success")]
    [InlineData("secondary")]
    public void IonCheckbox_Style_Color_TintsCheckedBox(string color)
    {
        var themed = RenderStyled(justify: null, placement: null, color: color, isChecked: true);
        var plain = RenderStyled(justify: null, placement: null, isChecked: true);

        var themedIcon = themed.GetComputedStyle(themed.FindByClass("checkbox-icon").Single())!;
        var plainIcon = plain.GetComputedStyle(plain.FindByClass("checkbox-icon").Single())!;

        // Fill and border both take the palette base, and differ from the default primary.
        themedIcon.BackgroundColor.ShouldBe(themedIcon.BorderTopColor);
        themedIcon.BackgroundColor.ShouldNotBe(plainIcon.BackgroundColor);
    }

    [Fact]
    public void IonCheckbox_Style_Color_UsesPaletteBase()
    {
        var cut = RenderStyled(justify: null, placement: null, color: "danger", isChecked: true);

        var expected = IonicTheme.CreateMd().Danger;
        var icon = cut.GetComputedStyle(cut.FindByClass("checkbox-icon").Single())!;
        icon.BackgroundColor.ShouldBe(expected);
        icon.BorderTopColor.ShouldBe(expected);
    }

    /// <summary>
    /// The checkmark takes the palette CONTRAST color (--checkmark-color), so it stays legible on
    /// the fill — e.g. black on warning/light, white on danger.
    /// </summary>
    [Fact]
    public void IonCheckbox_Style_Color_TintsCheckmarkWithContrast()
    {
        var onDanger = RenderStyled(justify: null, placement: null, color: "danger", isChecked: true);
        var onWarning = RenderStyled(justify: null, placement: null, color: "warning", isChecked: true);

        onDanger.GetComputedStyle(onDanger.FindByClass("checkbox-icon-mark").Single())!
            .Color.ShouldBe(Color.FromHex("ffffff"));
        onWarning.GetComputedStyle(onWarning.FindByClass("checkbox-icon-mark").Single())!
            .Color.ShouldBe(Color.FromHex("000000"));
    }

    /// <summary>An unchecked colored checkbox keeps the neutral unchecked border / background.</summary>
    [Fact]
    public void IonCheckbox_Style_Color_DoesNotTintUncheckedBox()
    {
        var cut = RenderStyled(justify: null, placement: null, color: "danger", isChecked: false);

        var t = IonicTheme.CreateMd();
        var icon = cut.GetComputedStyle(cut.FindByClass("checkbox-icon").Single())!;
        icon.BackgroundColor.ShouldBe(t.CheckboxBackgroundOff);
        icon.BorderTopColor.ShouldBe(t.CheckboxBorderColorOff);
    }

    /// <summary>Color also applies to the indeterminate state (same fill rule as checked).</summary>
    [Fact]
    public void IonCheckbox_Style_Color_TintsIndeterminateBox()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = Context.Render<IonCheckbox>(p =>
        {
            p.Add(nameof(IonCheckbox.ChildContent), Label("Jon Snow"));
            p.Add(nameof(IonCheckbox.Color), "danger");
            p.Add(nameof(IonCheckbox.Indeterminate), true);
        });

        var icon = cut.GetComputedStyle(cut.FindByClass("checkbox-icon").Single())!;
        icon.BackgroundColor.ShouldBe(IonicTheme.CreateMd().Danger);
    }
}
