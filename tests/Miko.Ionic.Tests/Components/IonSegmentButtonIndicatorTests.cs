using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-064 §2: the checked segment button is shown by an indicator overlay (Ionic's
/// <c>.segment-button-indicator</c>), not by filling the button. md renders a 2px primary
/// underline bar; ios renders a full-height light rounded pill with a soft shadow. This suite
/// covers the indicator DOM contract, the checked-opacity behavior, and the md/ios differences.
/// </summary>
public class IonSegmentButtonIndicatorTests
{
    private static TestContext ContextFor(HostPlatform platform)
    {
        var ctx = new TestContext();
        ctx.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(platform));
        // Mode-scoped Ionic rules drive the indicator styles.
        ctx.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        ctx.ViewportWidth = 300f;
        ctx.ViewportHeight = 200f;
        return ctx;
    }

    // Renders a segment containing one button (so it gets the cascaded context to derive checked).
    private static ComponentUnderTest RenderButtonInSegment(TestContext ctx, string segmentValue, string buttonValue)
        => ctx.Render<IonSegment>(p =>
        {
            p.Add(nameof(IonSegment.Value), segmentValue);
            p.Add(nameof(IonSegment.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<IonSegmentButton>(0);
                b.AddComponentParameter(1, nameof(IonSegmentButton.Value), buttonValue);
                b.AddComponentParameter(2, nameof(IonSegmentButton.ChildContent), (RenderFragment)(inner =>
                {
                    inner.OpenComponent<IonLabel>(0);
                    inner.AddComponentParameter(1, nameof(IonLabel.ChildContent),
                        (RenderFragment)(l => l.AddContent(0, "All")));
                    inner.CloseComponent();
                }));
                b.CloseComponent();
            }));
        });

    private static Element Indicator(ComponentUnderTest cut) =>
        cut.Root.FindByClass("segment-button-indicator").First();

    private static Element IndicatorBackground(ComponentUnderTest cut) =>
        cut.Root.FindByClass("segment-button-indicator-background").First();

    [Fact]
    public void SegmentButton_RendersIndicatorElement()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = ctx.Render<IonSegmentButton>(p => p.Add(nameof(IonSegmentButton.Value), "a"));

        // The indicator + its background are always present (so it can fade in/out).
        var indicator = cut.Root.FindByClass("segment-button-indicator");
        indicator.Count.ShouldBe(1);
        cut.Root.FindByClass("segment-button-indicator-background").Count.ShouldBe(1);

        // The indicator is a SIBLING of the native button (the host's second child), not nested
        // inside it — matching Ionic's host structure.
        cut.Root.Children[1].ShouldHaveClass("segment-button-indicator");
        cut.Root.Children[0].FindByClass("segment-button-indicator").Count.ShouldBe(0);
    }

    [Fact]
    public void Indicator_IsHidden_WhenNotChecked()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderButtonInSegment(ctx, segmentValue: "other", buttonValue: "a");

        // Not checked → indicator fully transparent.
        cut.GetComputedStyle(Indicator(cut))!.Opacity.ShouldBe(0f);
    }

    [Fact]
    public void Indicator_IsVisible_WhenChecked()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderButtonInSegment(ctx, segmentValue: "a", buttonValue: "a");

        // Checked → indicator faded in.
        cut.GetComputedStyle(Indicator(cut))!.Opacity.ShouldBe(1f);
    }

    [Fact]
    public void MdIndicator_IsThinPrimaryUnderlineBar()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var theme = IonicTheme.CreateMd();
        var cut = RenderButtonInSegment(ctx, "a", "a");

        var bg = cut.GetComputedStyle(IndicatorBackground(cut))!;

        // md: 2px tall bar, square corners, primary color, no shadow.
        bg.Height.Unit.ShouldBe(LengthUnit.Px);
        bg.Height.Value.ShouldBe(2f);
        bg.BackgroundColor.ShouldBe(theme.Primary);
        bg.BorderTopLeftRadius.Value.ShouldBe(0f);
        (bg.BoxShadow == null || bg.BoxShadow.Value.Value.Count == 0).ShouldBeTrue();
    }

    [Fact]
    public void MdCheckedButton_TurnsLabelPrimary_AndKeepsTransparentBackground()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var theme = IonicTheme.CreateMd();
        var cut = RenderButtonInSegment(ctx, "a", "a");

        var button = cut.Root.FindByClass("ion-segment-button").First();
        var style = cut.GetComputedStyle(button)!;

        // md checked: label turns primary; the button itself stays transparent (no fill).
        style.Color.ShouldBe(theme.Primary);
        style.BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void IosIndicator_IsFullHeightRoundedPillWithShadow()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = RenderButtonInSegment(ctx, "a", "a");

        var bg = cut.GetComputedStyle(IndicatorBackground(cut))!;

        // ios: full-height pill, rounded (7px), light surface, with a soft shadow.
        bg.Height.Unit.ShouldBe(LengthUnit.Percent);
        bg.Height.Value.ShouldBe(100f);
        bg.BorderTopLeftRadius.Value.ShouldBe(7f);
        bg.BackgroundColor.ShouldBe(Color.FromHex("ffffff"));
        bg.BoxShadow.ShouldNotBeNull();
        bg.BoxShadow!.Value.Value.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void IosCheckedButton_KeepsDarkLabel()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = RenderButtonInSegment(ctx, "a", "a");

        var button = cut.Root.FindByClass("ion-segment-button").First();
        // ios checked: the label stays dark; the pill behind it provides the contrast.
        cut.GetComputedStyle(button)!.Color.ShouldBe(Color.FromHex("000000"));
    }

    [Fact]
    public void MdAndIos_IndicatorHeightsDiffer()
    {
        using var md = ContextFor(HostPlatform.Android);
        using var ios = ContextFor(HostPlatform.Ios);

        var mdCut = RenderButtonInSegment(md, "a", "a");
        var iosCut = RenderButtonInSegment(ios, "a", "a");
        var mdBg = mdCut.GetComputedStyle(IndicatorBackground(mdCut))!;
        var iosBg = iosCut.GetComputedStyle(IndicatorBackground(iosCut))!;

        // Concrete behavioral difference: md is a fixed 2px bar, ios fills the height.
        mdBg.Height.Unit.ShouldBe(LengthUnit.Px);
        iosBg.Height.Unit.ShouldBe(LengthUnit.Percent);
    }
}
