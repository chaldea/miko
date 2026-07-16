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
}
