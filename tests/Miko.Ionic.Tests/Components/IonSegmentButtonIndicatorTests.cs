using Microsoft.Extensions.DependencyInjection;
using Miko.Animation;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Styling;
using Miko.Testing;
using Shouldly;
using SkiaSharp;

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
    public void IosIndicator_IsBehindButtonContent()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = RenderButtonInSegment(ctx, "a", "a");

        var button = cut.Root.FindByClass("button-native").First();
        var indicator = Indicator(cut);
        cut.GetComputedStyle(button)!.ZIndex.ShouldBe(1);
        cut.GetComputedStyle(button)!.Color.ShouldBe(Color.FromHex("000000"));
        cut.GetComputedStyle(indicator)!.ZIndex.ShouldBe(0);
    }

    [Fact]
    public void IosSegmentButton_ProvidesPositionedStackingContextForIndicatorLayers()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = RenderButtonInSegment(ctx, "a", "a");

        var button = cut.Root.FindByClass("ion-segment-button").Single();
        cut.GetComputedStyle(button)!.Position.ShouldBe(Position.Relative);
        cut.GetComputedStyle(button)!.ZIndex.ShouldBe(0);
    }

    [Fact]
    public void IosIndicatorBackground_UsesAnimatedTransformTransition()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = RenderButtonInSegment(ctx, "a", "a");
        var indicator = Indicator(cut);

        cut.Root.FindByClass("segment-button-indicator-animated").Count.ShouldBe(1);
        var style = cut.GetComputedStyle(indicator)!;
        style.Transitions.ShouldContain(t =>
            t.Property == nameof(Style.Transform) && Math.Abs(t.Duration - 0.26f) < 0.001f);
    }

    [Fact]
    public void ChangingSelection_MovesPreviousIndicatorTowardNewButton()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = ctx.Render<IonSegment>(p =>
        {
            p.Add(nameof(IonSegment.Value), "first");
            p.AddChildContent(builder =>
            {
                builder.OpenComponent<IonSegmentButton>(0);
                builder.AddComponentParameter(1, nameof(IonSegmentButton.Value), "first");
                builder.CloseComponent();
                builder.OpenComponent<IonSegmentButton>(2);
                builder.AddComponentParameter(3, nameof(IonSegmentButton.Value), "second");
                builder.CloseComponent();
            });
        });

        var buttons = cut.Root.FindByClass("ion-segment-button");
        cut.GetComputedStyle(buttons[0].FindByClass("segment-button-indicator").Single())!
            .Opacity.ShouldBe(1f);
        buttons[1].FindByClass("button-native").Single().OnClick!
            .Invoke(new MouseEventArgs { Target = buttons[1] });

        buttons = cut.Root.FindByClass("ion-segment-button");
        buttons[0].ShouldHaveClass("segment-button-previous");
        buttons[1].ShouldHaveClass("segment-button-checked");
        buttons[1].ShouldNotHaveClass("segment-button-moving-target");
        var movingIndicator = buttons[0].FindByClass("segment-button-indicator").Single();
        var translate = movingIndicator.Style!.Transform!.Value.Value.Functions
            .OfType<TransformFunction.TranslateX>()
            .Single();
        translate.X.Unit.ShouldBe(LengthUnit.Percent);
        translate.X.Value.ShouldBe(100f);

        buttons[0].FindByClass("button-native").Single().OnClick!
            .Invoke(new MouseEventArgs { Target = buttons[0] });
        buttons = cut.Root.FindByClass("ion-segment-button");
        var reverseIndicator = buttons[1].FindByClass("segment-button-indicator").Single();
        var reverseTranslate = reverseIndicator.Style!.Transform!.Value.Value.Functions
            .OfType<TransformFunction.TranslateX>()
            .Single();
        reverseTranslate.X.Value.ShouldBe(-100f);
    }

    [Fact]
    public void BoundValue_StillStartsIndicatorTransformTransition()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = ctx.Render<BoundSegmentHost>();
        using var bitmap = new SKBitmap((int)ctx.ViewportWidth, (int)ctx.ViewportHeight);
        using var canvas = new SKCanvas(bitmap);
        var engine = new MikoEngine();
        engine.Initialize(cut.Root, ctx.StyleSheets, canvas, ctx.ViewportWidth, ctx.ViewportHeight);
        engine.Render(canvas);

        var buttons = cut.Root.FindByClass("ion-segment-button");
        buttons[1].FindByClass("button-native").Single().OnClick!
            .Invoke(new MouseEventArgs { Target = buttons[1] });

        buttons = cut.Root.FindByClass("ion-segment-button");
        buttons[1].ShouldHaveClass("segment-button-checked");
        IndicatorTranslateX(buttons[0]).ShouldBe(100f);
        IndicatorTranslateX(buttons[1]).ShouldBe(0f);

        engine.Render(canvas);
        buttons = cut.Root.FindByClass("ion-segment-button");
        buttons.Select(button => button.FindByClass("segment-button-indicator").Single())
            .ShouldContain(indicator =>
                engine.AnimationManager.HasActiveTransition(indicator, nameof(Style.Transform)));
    }

    [Fact]
    public void IosDirectText_RemainsVisibleAboveIndicatorAfterSelectionChanges()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = ctx.Render<IonSegment>(p =>
        {
            p.Add(nameof(IonSegment.Value), "all");
            p.AddChildContent(builder =>
            {
                builder.OpenComponent<IonSegmentButton>(0);
                builder.AddComponentParameter(1, nameof(IonSegmentButton.Value), "all");
                builder.AddComponentParameter(2, nameof(IonSegmentButton.ChildContent),
                    (RenderFragment)(inner => inner.AddContent(0, "All")));
                builder.CloseComponent();
                builder.OpenComponent<IonSegmentButton>(3);
                builder.AddComponentParameter(4, nameof(IonSegmentButton.Value), "favorites");
                builder.AddComponentParameter(5, nameof(IonSegmentButton.ChildContent),
                    (RenderFragment)(inner => inner.AddContent(0, "Favorites")));
                builder.CloseComponent();
            });
        });

        using var bitmap = new SKBitmap((int)ctx.ViewportWidth, (int)ctx.ViewportHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        var engine = new MikoEngine();
        engine.Initialize(cut.Root, ctx.StyleSheets, canvas, ctx.ViewportWidth, ctx.ViewportHeight);
        engine.Render(canvas);
        CountDarkPixels(bitmap, 0, 150).ShouldBeGreaterThan(0);
        CountDarkPixels(bitmap, 150, 300).ShouldBeGreaterThan(0);

        var buttons = cut.Root.FindByClass("ion-segment-button");
        buttons[1].FindByClass("button-native").Single().OnClick!
            .Invoke(new MouseEventArgs { Target = buttons[1] });
        canvas.Clear(SKColors.White);
        engine.Initialize(cut.Root, ctx.StyleSheets, canvas, ctx.ViewportWidth, ctx.ViewportHeight);
        engine.Render(canvas);
        CountDarkPixels(bitmap, 0, 150).ShouldBeGreaterThan(0);
        CountDarkPixels(bitmap, 150, 300).ShouldBeGreaterThan(0);

        buttons = cut.Root.FindByClass("ion-segment-button");
        buttons[0].FindByClass("button-native").Single().OnClick!
            .Invoke(new MouseEventArgs { Target = buttons[0] });
        canvas.Clear(SKColors.White);
        engine.Initialize(cut.Root, ctx.StyleSheets, canvas, ctx.ViewportWidth, ctx.ViewportHeight);
        engine.Render(canvas);
        CountDarkPixels(bitmap, 0, 150).ShouldBeGreaterThan(0);
        CountDarkPixels(bitmap, 150, 300).ShouldBeGreaterThan(0);
    }

    private static int CountDarkPixels(SKBitmap bitmap, int xStart, int xEnd)
    {
        var count = 0;
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = xStart; x < xEnd; x++)
        {
            var pixel = bitmap.GetPixel(x, y);
            if (pixel.Red < 100 && pixel.Green < 100 && pixel.Blue < 100) count++;
        }
        return count;
    }

    private static float IndicatorTranslateX(Element button)
    {
        var indicator = button.FindByClass("segment-button-indicator").Single();
        if (indicator.Style?.Transform is not { } property) return 0f;
        var transform = property.Value;
        if (transform.Functions.Count == 0) return 0f;
        return transform.Functions.OfType<TransformFunction.TranslateX>().Single().X.Value;
    }

    private sealed class BoundSegmentHost : ComponentBase
    {
        private string? _value = "all";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.OpenComponent<IonSegment>(1);
            builder.AddComponentParameter(2, nameof(IonSegment.Value), _value);
            builder.AddComponentParameter(3, nameof(IonSegment.ValueChanged),
                EventCallback.Factory.Create<string>(this, value => _value = value));
            builder.AddComponentParameter(4, nameof(IonSegment.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<IonSegmentButton>(0);
                content.AddComponentParameter(1, nameof(IonSegmentButton.Value), "all");
                content.AddComponentParameter(2, nameof(IonSegmentButton.ChildContent),
                    (RenderFragment)(text => text.AddContent(0, "All")));
                content.CloseComponent();
                content.OpenComponent<IonSegmentButton>(3);
                content.AddComponentParameter(4, nameof(IonSegmentButton.Value), "favorites");
                content.AddComponentParameter(5, nameof(IonSegmentButton.ChildContent),
                    (RenderFragment)(text => text.AddContent(0, "Favorites")));
                content.CloseComponent();
            }));
            builder.CloseComponent();
            builder.CloseElement();
        }
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

    [Fact]
    public void IosIndicator_StretchesToIconBottomButtonHeight()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = ctx.Render<IonSegment>(p =>
        {
            p.Add(nameof(IonSegment.Value), "call");
            p.AddChildContent(builder =>
            {
                builder.OpenComponent<IonSegmentButton>(0);
                builder.AddComponentParameter(1, nameof(IonSegmentButton.Value), "call");
                builder.AddComponentParameter(2, nameof(IonSegmentButton.Layout), "icon-bottom");
                builder.AddComponentParameter(3, nameof(IonSegmentButton.ChildContent), (RenderFragment)(content =>
                {
                    content.OpenComponent<IonIcon>(0);
                    content.AddComponentParameter(1, nameof(IonIcon.Icon), "call");
                    content.CloseComponent();
                    content.OpenComponent<IonLabel>(2);
                    content.AddComponentParameter(3, nameof(IonLabel.ChildContent),
                        (RenderFragment)(label => label.AddContent(0, "Call")));
                    content.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        var button = cut.FindByClass("ion-segment-button").ShouldHaveSingleItem();
        var indicator = Indicator(cut);
        var buttonBox = cut.GetBoxModel(button).ShouldNotBeNull();
        var indicatorBox = cut.GetBoxModel(indicator).ShouldNotBeNull();
        indicatorBox.BorderBox.Height.ShouldBe(buttonBox.BorderBox.Height, 0.01f);
        buttonBox.BorderBox.Height.ShouldBeGreaterThan(28f);
    }
}
