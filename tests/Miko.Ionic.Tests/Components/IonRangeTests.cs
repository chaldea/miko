using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-range</c> (the single-knob visual port). Covers the DOM contract (wrapper /
/// slider / bar container / knob handle), the label-placement classes, the computed knob position
/// and active-bar offset for a mid value, the pin class + element, and the disabled class. Knob and
/// active-bar positions are set as inline styles (percent left/right), read back off the element.
/// </summary>
public class IonRangeTests : IonicComponentTestBase
{
    private static ComponentUnderTest RenderRange(TestContext ctx,
        Action<ComponentParameterBuilder<IonRange>>? configure = null)
        => ctx.Render<IonRange>(p => configure?.Invoke(p));

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonRange_RendersDomContract()
    {
        var cut = RenderRange(Context, p => p.Add(nameof(IonRange.Label), "Volume"));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-range");
        cut.Root.ShouldHaveClass("range-label-placement-start");

        cut.FindByClass("range-wrapper").ShouldHaveSingleItem()
            .TagName.ShouldBe("label");
        cut.FindByClass("label-text-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("native-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("range-slider").ShouldHaveSingleItem();
        cut.FindByClass("range-bar-container").ShouldHaveSingleItem();
        // The container holds the inactive track and the active fill.
        cut.FindByClass("range-bar").Count.ShouldBe(2);
        cut.FindByClass("range-bar-active").ShouldHaveSingleItem();
        cut.FindByClass("range-knob-handle").ShouldHaveSingleItem();
        cut.FindByClass("range-knob").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Volume");
    }

    [Fact]
    public void IonRange_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderRange(Context);

        cut.Root.Class.ShouldStartWith("ios ion-range");
    }

    [Fact]
    public void IonRange_EmptyLabel_HidesLabelWrapper()
    {
        var cut = RenderRange(Context);

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldHaveClass("label-text-wrapper-hidden");
    }

    [Theory]
    [InlineData("start", "range-label-placement-start")]
    [InlineData("end", "range-label-placement-end")]
    [InlineData("fixed", "range-label-placement-fixed")]
    [InlineData("stacked", "range-label-placement-stacked")]
    public void IonRange_StampsLabelPlacementClass(string placement, string expected)
    {
        var cut = RenderRange(Context, p =>
        {
            p.Add(nameof(IonRange.Label), "L");
            p.Add(nameof(IonRange.LabelPlacement), placement);
        });

        cut.Root.ShouldHaveClass(expected);
    }

    [Fact]
    public void IonRange_StampsColorClasses()
    {
        var cut = RenderRange(Context, p => p.Add(nameof(IonRange.Color), "secondary"));

        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-secondary");
    }

    // ---- Computed positions ------------------------------------------------

    [Fact]
    public void IonRange_MidValue_PositionsKnobAtHalf()
    {
        // Value 50 of [0,100] → ratio 0.5 → knob left 50%, active bar right 50%.
        var cut = RenderRange(Context, p =>
        {
            p.Add(nameof(IonRange.Min), 0d);
            p.Add(nameof(IonRange.Max), 100d);
            p.Add(nameof(IonRange.Value), 50d);
        });

        var knob = cut.FindByClass("range-knob-handle").Single();
        knob.Style.ShouldNotBeNull();
        knob.Style!.Left.ShouldBe(Length.Percent(50));

        var active = cut.FindByClass("range-bar-active").Single();
        active.Style.ShouldNotBeNull();
        active.Style!.Left.ShouldBe(Length.Percent(0));
        active.Style.Right.ShouldBe(Length.Percent(50));
    }

    [Fact]
    public void IonRange_MinValue_PositionsKnobAtStart()
    {
        var cut = RenderRange(Context, p =>
        {
            p.Add(nameof(IonRange.Min), 0d);
            p.Add(nameof(IonRange.Max), 100d);
            p.Add(nameof(IonRange.Value), 0d);
        });

        var knob = cut.FindByClass("range-knob-handle").Single();
        knob.Style!.Left.ShouldBe(Length.Percent(0));
        cut.Root.ShouldHaveClass("range-value-min");
    }

    [Fact]
    public void IonRange_CustomRange_ComputesRatioAgainstBounds()
    {
        // Value 30 of [20,40] → ratio (30-20)/(40-20) = 0.5 → 50%.
        var cut = RenderRange(Context, p =>
        {
            p.Add(nameof(IonRange.Min), 20d);
            p.Add(nameof(IonRange.Max), 40d);
            p.Add(nameof(IonRange.Value), 30d);
        });

        cut.FindByClass("range-knob-handle").Single().Style!.Left.ShouldBe(Length.Percent(50));
    }

    // ---- Pin ---------------------------------------------------------------

    [Fact]
    public void IonRange_Pin_StampsClassAndRendersPinWithValue()
    {
        var cut = RenderRange(Context, p =>
        {
            p.Add(nameof(IonRange.Pin), true);
            p.Add(nameof(IonRange.Value), 42d);
        });

        cut.Root.ShouldHaveClass("range-has-pin");
        cut.FindByClass("range-pin").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("42");
    }

    [Fact]
    public void IonRange_NoPin_OmitsPin()
    {
        var cut = RenderRange(Context);

        cut.FindByClass("range-pin").ShouldBeEmpty();
        cut.Root.ShouldNotHaveClass("range-has-pin");
    }

    // ---- Disabled ----------------------------------------------------------

    [Fact]
    public void IonRange_Disabled_StampsClass()
    {
        var cut = RenderRange(Context, p => p.Add(nameof(IonRange.Disabled), true));

        cut.Root.ShouldHaveClass("range-disabled");
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonRange_Style_HostIsFlex()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderRange(Context);

        cut.GetComputedStyle(cut.Root)!.Display.ShouldBe(Display.Flex);
    }

    [Fact]
    public void IonRange_Style_SliderHasFixedHeight()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderRange(Context);
        var slider = cut.FindByClass("range-slider").Single();

        cut.GetComputedStyle(slider)!.Height.ShouldBe(Length.Px(42));
    }
}
