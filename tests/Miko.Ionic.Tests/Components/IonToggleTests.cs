using Miko.Animation;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonToggleTests : IonicComponentTestBase
{
    private static RenderFragment Label(string value) => builder => builder.AddContent(0, value);

    private static ComponentUnderTest RenderToggle(TestContext ctx,
        Action<ComponentParameterBuilder<IonToggle>>? configure = null,
        string? label = "Wi-Fi")
        => ctx.Render<IonToggle>(p =>
        {
            if (label is not null) p.Add(nameof(IonToggle.ChildContent), Label(label));
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonToggle_RendersDomContract()
    {
        var cut = RenderToggle(Context, label: "Enable notifications");

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-toggle");
        cut.Root.ShouldHaveClass("toggle-label-placement-start");

        var wrapper = cut.FindByClass("toggle-wrapper").Single();
        wrapper.TagName.ShouldBe("label");

        var input = cut.FindByClass("toggle-native").Single();
        input.TagName.ShouldBe("input");
        input.ShouldBeOfType<InputElement>().Type.ShouldBe(InputType.Checkbox);

        cut.FindByClass("label-text-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("native-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("toggle-icon").ShouldHaveSingleItem();
        cut.FindByClass("toggle-icon-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("toggle-inner").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Enable notifications");
    }

    [Fact]
    public void IonToggle_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderToggle(Context);

        cut.Root.Class.ShouldStartWith("ios ion-toggle");
    }

    [Theory]
    [InlineData("start", "toggle-label-placement-start")]
    [InlineData("end", "toggle-label-placement-end")]
    [InlineData("fixed", "toggle-label-placement-fixed")]
    [InlineData("stacked", "toggle-label-placement-stacked")]
    public void IonToggle_StampsLabelPlacementClass(string placement, string expected)
    {
        var cut = RenderToggle(Context, p => p.Add(nameof(IonToggle.LabelPlacement), placement));

        cut.Root.ShouldHaveClass(expected);
    }

    [Fact]
    public void IonToggle_StampsJustifyAlignmentAndColorClasses()
    {
        var cut = RenderToggle(Context, p =>
        {
            p.Add(nameof(IonToggle.Justify), "space-between");
            p.Add(nameof(IonToggle.Alignment), "center");
            p.Add(nameof(IonToggle.Color), "danger");
        });

        cut.Root.ShouldHaveClass("toggle-justify-space-between");
        cut.Root.ShouldHaveClass("toggle-alignment-center");
        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-danger");
    }

    [Fact]
    public void IonToggle_EmptyLabel_HidesLabelWrapper()
    {
        var cut = RenderToggle(Context, label: null);

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldHaveClass("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonToggle_WithLabel_DoesNotHideLabelWrapper()
    {
        var cut = RenderToggle(Context, label: "Sound");

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldNotHaveClass("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonToggle_RendersHelperAndErrorText()
    {
        var cut = RenderToggle(Context, p =>
        {
            p.Add(nameof(IonToggle.HelperText), "Optional");
            p.Add(nameof(IonToggle.ErrorText), "Required");
        });

        cut.FindByClass("toggle-bottom").ShouldHaveSingleItem();
        cut.FindByClass("helper-text").Single().TextContent.ShouldBe("Optional");
        cut.FindByClass("error-text").Single().TextContent.ShouldBe("Required");
    }

    [Fact]
    public void IonToggle_NoHintText_OmitsToggleBottom()
    {
        var cut = RenderToggle(Context);

        cut.FindByClass("toggle-bottom").ShouldBeEmpty();
    }

    // ---- State / interaction ----------------------------------------------

    [Fact]
    public void IonToggle_Checked_StampsClassAndSyncsNativeInput()
    {
        var cut = RenderToggle(Context, p => p.Add(nameof(IonToggle.Checked), true));

        cut.Root.ShouldHaveClass("toggle-checked");
        cut.FindByClass("toggle-native").Single().ShouldBeOfType<InputElement>().Checked.ShouldBeTrue();
    }

    [Fact]
    public void IonToggle_Unchecked_DoesNotStampCheckedClass()
    {
        var cut = RenderToggle(Context);

        cut.Root.ShouldNotHaveClass("toggle-checked");
        cut.FindByClass("toggle-native").Single().ShouldBeOfType<InputElement>().Checked.ShouldBeFalse();
    }

    [Fact]
    public void IonToggle_Disabled_StampsClassAndNativeState()
    {
        var cut = RenderToggle(Context, p => p.Add(nameof(IonToggle.Disabled), true));

        cut.Root.ShouldHaveClass("toggle-disabled");
        cut.FindByClass("toggle-native").Single().IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void IonToggle_Click_TogglesAndRaisesCallbacks()
    {
        bool? changed = null;
        bool? ionChanged = null;
        var cut = RenderToggle(Context, p =>
        {
            p.Add(nameof(IonToggle.CheckedChanged),
                EventCallback.Factory.Create<bool>(this, v => changed = v));
            p.Add(nameof(IonToggle.IonChange),
                EventCallback.Factory.Create<bool>(this, v => ionChanged = v));
        });

        var wrapper = cut.FindByClass("toggle-wrapper").Single();
        wrapper.OnClick!.Invoke(new MouseEventArgs { Target = wrapper });

        changed.ShouldBe(true);
        ionChanged.ShouldBe(true);
    }

    [Fact]
    public void IonToggle_Click_WhenDisabled_DoesNotToggle()
    {
        bool? changed = null;
        var cut = RenderToggle(Context, p =>
        {
            p.Add(nameof(IonToggle.Disabled), true);
            p.Add(nameof(IonToggle.CheckedChanged),
                EventCallback.Factory.Create<bool>(this, v => changed = v));
        });

        var wrapper = cut.FindByClass("toggle-wrapper").Single();
        wrapper.OnClick!.Invoke(new MouseEventArgs { Target = wrapper });

        changed.ShouldBeNull();
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonToggle_Style_HostIsInlineBlock()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggle(Context);
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.InlineBlock);
    }

    [Fact]
    public void IonToggle_Style_JustifySwitchesHostToBlock()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggle(Context, p => p.Add(nameof(IonToggle.Justify), "space-between"));

        cut.GetComputedStyle(cut.Root)!.Display.ShouldBe(Display.Block);
    }

    [Fact]
    public void IonToggle_Style_MdTrackUsesMdSize()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggle(Context);
        var track = cut.FindByClass("toggle-icon").Single();
        var style = cut.GetComputedStyle(track)!;

        style.Width.ShouldBe(Length.Px(36));
        style.Height.ShouldBe(Length.Px(14));
    }

    [Fact]
    public void IonToggle_Style_IosTrackUsesIosSize()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggle(Context);
        var track = cut.FindByClass("toggle-icon").Single();
        var style = cut.GetComputedStyle(track)!;

        style.Width.ShouldBe(Length.Px(51));
        style.Height.ShouldBe(Length.Px(31));
    }

    [Fact]
    public void IonToggle_Style_CheckedFillsTrackAndSlidesKnob()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggle(Context, p => p.Add(nameof(IonToggle.Checked), true));

        // Track fills with the checked (on) background (md = primary @ .5 alpha).
        var track = cut.FindByClass("toggle-icon").Single();
        cut.GetComputedStyle(track)!.BackgroundColor
            .ShouldBe(IonicTheme.CreateMd().ToggleTrackBackgroundOn);

        // The knob carrier translates right by (track-width - handle-width) = 16px on md.
        var wrapper = cut.FindByClass("toggle-icon-wrapper").Single();
        var transform = cut.GetComputedStyle(wrapper)!.Transform;
        transform.Functions
            .OfType<TransformFunction.TranslateX>()
            .Any(tx => Length.Px(16).Equals(tx.X))
            .ShouldBeTrue();
    }

    [Fact]
    public void IonToggle_Style_UncheckedDoesNotSlideKnob()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggle(Context);

        // No translate applied when unchecked (an empty transform).
        var wrapper = cut.FindByClass("toggle-icon-wrapper").Single();
        cut.GetComputedStyle(wrapper)!.Transform.Functions.ShouldBeEmpty();
    }

    // ---- In-item (CascadingParameter IonItemContext) -----------------------

    // hostHeight gives the toggle host a definite height, so its descendants' percentage heights
    // have a base to resolve against (the item's own wrappers are auto-height).
    private static ComponentUnderTest RenderToggleInItem(TestContext ctx,
        string? justify = null, string? labelPlacement = null, string label = "Toggle in item",
        float? hostHeight = null)
        => ctx.Render<IonItem>(p => p.Add(nameof(IonItem.ChildContent), (RenderFragment)(b =>
        {
            b.OpenComponent<IonToggle>(0);
            b.AddAttribute(1, nameof(IonToggle.ChildContent), Label(label));
            if (justify is not null) b.AddAttribute(2, nameof(IonToggle.Justify), justify);
            if (labelPlacement is not null) b.AddAttribute(3, nameof(IonToggle.LabelPlacement), labelPlacement);
            if (hostHeight is not null)
            {
                b.AddAttribute(4, nameof(IonToggle.Style),
                    new Miko.Styling.Style { Height = Length.Px(hostHeight.Value) });
            }
            b.CloseComponent();
        })));

    [Fact]
    public void IonToggle_InItem_StampsInItemClass()
    {
        // IonItem cascades IonItemContext → IonToggle stamps "in-item" on the host.
        var cut = RenderToggleInItem(Context);

        cut.FindByClass("ion-toggle").Single().ShouldHaveClass("in-item");
    }

    [Fact]
    public void IonToggle_Standalone_DoesNotStampInItemClass()
    {
        var cut = RenderToggle(Context);

        cut.Root.ShouldNotHaveClass("in-item");
    }

    [Fact]
    public void IonToggle_Style_InItem_HostGrowsToFillItem()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggleInItem(Context);

        // toggle.scss :host(.in-item): flex: 1 1 0; width: 100%; height: 100%.
        var host = cut.FindByClass("ion-toggle").Single();
        var style = cut.GetComputedStyle(host)!;
        style.FlexGrow.ShouldBe(1f);
        style.FlexShrink.ShouldBe(1f);
        style.FlexBasis.ShouldBe(Length.Px(0));
    }

    [Fact]
    public void IonToggle_Style_InItem_LabelAndNativeWrapperGetVerticalMargins()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggleInItem(Context);

        // $toggle-item-label-margin-top/bottom = 10px on label + native wrappers.
        var label = cut.GetComputedStyle(cut.FindByClass("label-text-wrapper").Single())!;
        label.MarginTop.ShouldBe(Length.Px(10));
        label.MarginBottom.ShouldBe(Length.Px(10));

        var native = cut.GetComputedStyle(cut.FindByClass("native-wrapper").Single())!;
        native.MarginTop.ShouldBe(Length.Px(10));
        native.MarginBottom.ShouldBe(Length.Px(10));
    }

    [Fact]
    public void IonToggle_Style_InItemStacked_SwapsLabelBottomMargin()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggleInItem(Context, labelPlacement: "stacked");

        // Stacked in-item: label bottom margin becomes $form-control-label-margin (16px),
        // the native-wrapper loses its top margin.
        var label = cut.GetComputedStyle(cut.FindByClass("label-text-wrapper").Single())!;
        label.MarginTop.ShouldBe(Length.Px(10));
        label.MarginBottom.ShouldBe(Length.Px(16));

        var native = cut.GetComputedStyle(cut.FindByClass("native-wrapper").Single())!;
        native.MarginTop.ShouldBe(Length.Px(0));
        native.MarginBottom.ShouldBe(Length.Px(10));
    }

    // ---- .toggle-wrapper height: inherit (problem 2) -----------------------

    /// <summary>
    /// toggle.scss gives <c>.toggle-wrapper</c> <c>height: inherit</c> so the label/switch row spans
    /// the host's height. Miko has no CSS <c>inherit</c> keyword for Length props, so the host value
    /// is mirrored: <c>:host(.in-item)</c> sets <c>height: 100%</c>, and the wrapper matches it.
    /// Without the mirror the wrapper shrink-wraps its content and the toggle renders too short.
    /// </summary>
    [Fact]
    public void IonToggle_Style_InItem_WrapperInheritsHostHeight()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggleInItem(Context);

        var wrapper = cut.FindByClass("toggle-wrapper").Single();
        cut.GetComputedStyle(wrapper)!.Height.ShouldBe(Length.Percent(100));
    }

    /// <summary>
    /// The mirrored height is real at layout time. The wrapper's <c>height: 100%</c> only resolves
    /// against a DEFINITE host height, so the host is given one here; the wrapper then fills it
    /// instead of shrink-wrapping the label + switch row. (In the default item DOM the host's own
    /// <c>height: 100%</c> degrades to auto because <c>.input-wrapper</c> is auto-height, so the two
    /// heights coincide and the difference is invisible — hence the explicit host height.)
    /// </summary>
    [Fact]
    public void IonToggle_Layout_InItem_WrapperFillsDefiniteHostHeight()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggleInItem(Context, hostHeight: 120);

        var hostBox = cut.GetBoxModel(cut.FindByClass("ion-toggle").Single())!;
        var wrapperBox = cut.GetBoxModel(cut.FindByClass("toggle-wrapper").Single())!;

        hostBox.Content.Height.ShouldBe(120f, 0.5f);
        wrapperBox.Content.Height.ShouldBe(120f, 0.5f);
    }

    /// <summary>A standalone toggle has no host height to inherit, so the wrapper stays auto.</summary>
    [Fact]
    public void IonToggle_Style_Standalone_WrapperHeightStaysAuto()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToggle(Context);

        var wrapper = cut.FindByClass("toggle-wrapper").Single();
        cut.GetComputedStyle(wrapper)!.Height.ShouldBe(Length.Auto);
    }

    // ---- Color (problem 3) -------------------------------------------------

    private ComponentUnderTest RenderColored(string? color, bool isChecked = true)
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        return Context.Render<IonToggle>(p =>
        {
            p.Add(nameof(IonToggle.ChildContent), Label("Wi-Fi"));
            if (color is not null) p.Add(nameof(IonToggle.Color), color);
            p.Add(nameof(IonToggle.Checked), isChecked);
        });
    }

    /// <summary>
    /// A named Color tints the checked track. toggle.md.scss's
    /// <c>:host(.ion-color.toggle-checked) .toggle-icon</c> resolves --track-background-checked to
    /// <c>current-color(base)</c>; the port emits ion-color-* scoped rules for it. Previously the
    /// class was stamped but no rule matched it, so Color did nothing.
    /// </summary>
    [Theory]
    [InlineData("secondary")]
    [InlineData("danger")]
    [InlineData("success")]
    public void IonToggle_Style_Color_TintsCheckedTrack(string color)
    {
        var themed = RenderColored(color);
        var plain = RenderColored(null);

        var themedTrack = themed.GetComputedStyle(themed.FindByClass("toggle-icon").Single())!;
        var plainTrack = plain.GetComputedStyle(plain.FindByClass("toggle-icon").Single())!;

        themedTrack.BackgroundColor.ShouldNotBe(plainTrack.BackgroundColor);
    }

    /// <summary>md tints the checked track with the palette base at .5 alpha
    /// (<c>$toggle-md-track-background-color-alpha-on</c>).</summary>
    [Fact]
    public void IonToggle_Style_Color_MdTrackUsesPaletteBaseAtHalfAlpha()
    {
        var cut = RenderColored("secondary");

        var t = IonicTheme.CreateMd();
        var expected = Color.FromRgba(t.Secondary.R, t.Secondary.G, t.Secondary.B, 0.5f);

        cut.GetComputedStyle(cut.FindByClass("toggle-icon").Single())!
            .BackgroundColor.ShouldBe(expected);
    }

    /// <summary>md also repaints the knob with the solid palette base
    /// (<c>:host(.ion-color.toggle-checked) .toggle-inner</c>).</summary>
    [Fact]
    public void IonToggle_Style_Color_MdKnobUsesSolidPaletteBase()
    {
        var cut = RenderColored("secondary");

        cut.GetComputedStyle(cut.FindByClass("toggle-inner").Single())!
            .BackgroundColor.ShouldBe(IonicTheme.CreateMd().Secondary);
    }

    /// <summary>ios paints the checked track with the SOLID palette base (no alpha) and keeps the
    /// knob white — toggle.ios.scss only overrides <c>--track-background-checked</c>.</summary>
    [Fact]
    public void IonToggle_Style_Color_IosTrackIsSolidAndKnobStaysWhite()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderColored("secondary");
        var t = IonicTheme.CreateIos();

        cut.GetComputedStyle(cut.FindByClass("toggle-icon").Single())!
            .BackgroundColor.ShouldBe(t.Secondary);
        cut.GetComputedStyle(cut.FindByClass("toggle-inner").Single())!
            .BackgroundColor.ShouldBe(t.ToggleHandleBackground);
    }

    /// <summary>An unchecked colored toggle keeps the neutral off-state track.</summary>
    [Fact]
    public void IonToggle_Style_Color_DoesNotTintUncheckedTrack()
    {
        var cut = RenderColored("secondary", isChecked: false);

        cut.GetComputedStyle(cut.FindByClass("toggle-icon").Single())!
            .BackgroundColor.ShouldBe(IonicTheme.CreateMd().ToggleTrackBackgroundOff);
    }

    /// <summary>Without a Color, md still repaints the checked knob solid primary
    /// (<c>--handle-background-checked: ion-color(primary, base)</c>).</summary>
    [Fact]
    public void IonToggle_Style_MdCheckedKnobUsesPrimary()
    {
        var cut = RenderColored(null);

        cut.GetComputedStyle(cut.FindByClass("toggle-inner").Single())!
            .BackgroundColor.ShouldBe(IonicTheme.CreateMd().Primary);
    }
}
