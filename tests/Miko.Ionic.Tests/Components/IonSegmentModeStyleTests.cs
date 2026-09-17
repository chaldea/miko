using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Guards the mode-specific rules ported from Ionic's segment and segment-button SCSS files.
/// </summary>
public class IonSegmentModeStyleTests
{
    private static TestContext ContextFor(HostPlatform platform)
    {
        var context = new TestContext();
        context.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(platform));
        context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        context.ViewportWidth = 500f;
        context.ViewportHeight = 300f;
        return context;
    }

    private static ComponentUnderTest RenderSegment(TestContext context) =>
        context.Render<IonSegment>(parameters =>
        {
            parameters.Add(nameof(IonSegment.Value), "all");
            parameters.AddChildContent(builder =>
            {
                var __c1 = builder.OpenComponent<IonSegmentButton>();
                __c1.Value = "all";
                builder.CloseComponent();
            });
        });

    private static ComponentUnderTest RenderButton(TestContext context, bool disabled = false) =>
        context.Render<IonSegmentButton>(parameters =>
        {
            parameters.Add(nameof(IonSegmentButton.Value), "all");
            parameters.Add(nameof(IonSegmentButton.Disabled), disabled);
        });

    private static ComponentUnderTest RenderTextButton(TestContext context) =>
        context.Render<IonSegmentButton>(parameters =>
        {
            parameters.Add(nameof(IonSegmentButton.Value), "all");
            parameters.AddChildContent(builder => builder.AddContent("All"));
        });

    private static ComponentUnderTest RenderButtonWithIconAndLabel(TestContext context, string layout) =>
        context.Render<IonSegmentButton>(parameters =>
        {
            parameters.Add(nameof(IonSegmentButton.Value), "all");
            parameters.Add(nameof(IonSegmentButton.Layout), layout);
            parameters.AddChildContent(builder =>
            {
                var __c2 = builder.OpenComponent<IonIcon>();
                __c2.Icon = "star";
                builder.CloseComponent();
                var __c3 = builder.OpenComponent<IonLabel>();
                __c3.ChildContent = 
                    (RenderFragment)(label => label.AddContent("All"));
                builder.CloseComponent();
            });
        });

    [Fact]
    public void MdSegment_IsTransparentAndHasNoIosCornerRadius()
    {
        using var context = ContextFor(HostPlatform.Android);
        var cut = RenderSegment(context);
        var style = cut.GetComputedStyle(cut.Root)!;

        style.BackgroundColor.ShouldBe(Color.Transparent);
        style.BorderTopLeftRadius.Value.ShouldBe(0f);
        style.OverflowX.ShouldNotBe(Overflow.Hidden);
        style.OverflowY.ShouldNotBe(Overflow.Hidden);
    }

    [Fact]
    public void IosSegment_HasTranslucentBackgroundAndRoundedClipping()
    {
        using var context = ContextFor(HostPlatform.Ios);
        var cut = RenderSegment(context);
        var style = cut.GetComputedStyle(cut.Root)!;

        style.BackgroundColor.ShouldBe(IonicTheme.CreateIos().SegmentBackground);
        style.BackgroundColor.ShouldNotBe(Color.Transparent);
        style.BorderTopLeftRadius.Value.ShouldBe(8f);
        style.OverflowX.ShouldBe(Overflow.Hidden);
        style.OverflowY.ShouldBe(Overflow.Hidden);
    }

    [Fact]
    public void MdSegmentButton_UsesMaterialTypographyAndSpacing()
    {
        using var context = ContextFor(HostPlatform.Android);
        var cut = RenderButton(context);
        var host = cut.GetComputedStyle(cut.Root)!;
        var native = cut.GetComputedStyle(cut.Root.Children[0])!;

        host.MaxWidth.IsAuto.ShouldBeTrue();
        host.MinWidth.Value.ShouldBe(90f);
        host.MinHeight.Value.ShouldBe(48f);
        host.Color.ShouldBe(new Color(0, 0, 0, 153));
        host.FontSize.Value.ShouldBe(14f);
        host.FontWeight.ShouldBe(FontWeight.Medium);
        host.LineHeight.Value.ShouldBe(40f);
        host.LetterSpacing.Unit.ShouldBe(LengthUnit.Em);
        host.LetterSpacing.Value.ShouldBe(0.06f);
        host.TextTransform.ShouldBe(TextTransform.Uppercase);
        host.FlexDirection.ShouldBe(FlexDirection.Column);
        host.MarginTop.Value.ShouldBe(0f);
        host.MarginBottom.Value.ShouldBe(0f);
        native.PaddingLeft.Value.ShouldBe(16f);
        native.PaddingRight.Value.ShouldBe(16f);
    }

    [Fact]
    public void IosSegmentButton_UsesIosTypographyAndSpacing()
    {
        using var context = ContextFor(HostPlatform.Ios);
        var cut = RenderButton(context);
        var host = cut.GetComputedStyle(cut.Root)!;
        var native = cut.GetComputedStyle(cut.Root.Children[0])!;

        host.MaxWidth.IsAuto.ShouldBeTrue();
        host.MinWidth.Value.ShouldBe(70f);
        host.MinHeight.Value.ShouldBe(28f);
        host.FontSize.Value.ShouldBe(13f);
        host.LineHeight.Value.ShouldBe(37f);
        host.LetterSpacing.Value.ShouldBe(0f);
        host.TextTransform.ShouldBe(TextTransform.None);
        host.FlexDirection.ShouldBe(FlexDirection.Row);
        host.MarginTop.Value.ShouldBe(2f);
        host.MarginBottom.Value.ShouldBe(2f);
        host.BorderTopLeftRadius.Value.ShouldBe(7f);
        native.PaddingLeft.Value.ShouldBe(13f);
        native.PaddingRight.Value.ShouldBe(13f);
    }

    [Fact]
    public void SegmentButton_DefaultLayout_IsIconTop()
    {
        using var context = ContextFor(HostPlatform.Android);
        var cut = RenderButton(context);
        cut.Root.ShouldHaveClass("segment-button-layout-icon-top");
    }

    [Fact]
    public void Segment_Color_PropagatesToButtonsAndPaletteStyles()
    {
        using var context = ContextFor(HostPlatform.Ios);
        var cut = context.Render<IonSegment>(parameters =>
        {
            parameters.Add(nameof(IonSegment.Value), "all");
            parameters.Add(nameof(IonSegment.Color), "primary");
            parameters.AddChildContent(builder =>
            {
                var __c4 = builder.OpenComponent<IonSegmentButton>();
                __c4.Value = "all";
                builder.CloseComponent();
            });
        });

        cut.Root.ShouldHaveClass("ion-color-primary");
        var button = cut.FindByClass("ion-segment-button").ShouldHaveSingleItem();
        button.ShouldHaveClass("in-segment-color");
        button.ShouldHaveClass("ion-color-primary");
        cut.GetComputedStyle(cut.Root)!.BackgroundColor.ShouldBe(
            new Color(IonicTheme.CreateIos().Primary.R, IonicTheme.CreateIos().Primary.G,
                IonicTheme.CreateIos().Primary.B, 17));
        cut.GetComputedStyle(button)!.Color.ShouldBe(IonicTheme.CreateIos().SegmentButtonColor);

        var indicatorBackground = cut.FindByClass("segment-button-indicator-background").ShouldHaveSingleItem();
        cut.GetComputedStyle(indicatorBackground)!.BackgroundColor.ShouldBe(
            IonicTheme.CreateIos().SegmentIndicatorColor);
    }

    [Fact]
    public void MdSegment_Color_ChangesIndicatorToPalette()
    {
        using var context = ContextFor(HostPlatform.Android);
        var cut = context.Render<IonSegment>(parameters =>
        {
            parameters.Add(nameof(IonSegment.Value), "all");
            parameters.Add(nameof(IonSegment.Color), "primary");
            parameters.AddChildContent(builder =>
            {
                var __c5 = builder.OpenComponent<IonSegmentButton>();
                __c5.Value = "all";
                builder.CloseComponent();
            });
        });

        var indicatorBackground = cut.FindByClass("segment-button-indicator-background").ShouldHaveSingleItem();
        cut.GetComputedStyle(indicatorBackground)!.BackgroundColor.ShouldBe(
            IonicTheme.CreateMd().Primary);
    }

    [Theory]
    [InlineData(HostPlatform.Android, TextTransform.Uppercase, "ALL")]
    [InlineData(HostPlatform.Ios, TextTransform.None, "All")]
    public void SegmentButton_TextTransform_ReachesRenderedText(
        HostPlatform platform, TextTransform expectedTransform, string expectedText)
    {
        using var context = ContextFor(platform);
        var cut = RenderTextButton(context);
        var native = cut.FindByClass("button-native").ShouldHaveSingleItem();
        var inner = cut.FindByClass("button-inner").ShouldHaveSingleItem();
        var textNode = cut.GetAllElements().OfType<TextNode>().ShouldHaveSingleItem();

        cut.GetComputedStyle(cut.Root)!.TextTransform.ShouldBe(expectedTransform);
        cut.GetComputedStyle(native)!.TextTransform.ShouldBe(expectedTransform);
        cut.GetComputedStyle(inner)!.TextTransform.ShouldBe(expectedTransform);

        var textStyle = cut.GetComputedStyle(textNode)!;
        textStyle.TextTransform.ShouldBe(expectedTransform);
        Miko.Utils.TextTransformer.Apply(textNode.Text, textStyle.TextTransform).ShouldBe(expectedText);
    }

    [Theory]
    [InlineData(HostPlatform.Android)]
    [InlineData(HostPlatform.Ios)]
    public void DisabledSegmentButton_UsesIonicOpacity(HostPlatform platform)
    {
        using var context = ContextFor(platform);
        var cut = RenderButton(context, disabled: true);

        cut.GetComputedStyle(cut.Root)!.Opacity.ShouldBe(0.3f);
    }

    [Theory]
    [InlineData("icon-top", FlexDirection.Column)]
    [InlineData("icon-start", FlexDirection.Row)]
    [InlineData("icon-end", FlexDirection.RowReverse)]
    [InlineData("icon-bottom", FlexDirection.ColumnReverse)]
    public void Layout_ControlsInnerContentDirection(string layout, FlexDirection expected)
    {
        using var context = ContextFor(HostPlatform.Android);
        var cut = RenderButtonWithIconAndLabel(context, layout);
        var inner = cut.Root.FindByClass("button-inner").First();

        cut.GetComputedStyle(inner)!.FlexDirection.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HostPlatform.Android, 8f)]
    [InlineData(HostPlatform.Ios, 2f)]
    public void IconStart_UsesModeSpecificLabelGap(HostPlatform platform, float expected)
    {
        using var context = ContextFor(platform);
        var cut = RenderButtonWithIconAndLabel(context, "icon-start");
        var label = cut.Root.FindByClass("ion-label").First();

        cut.GetComputedStyle(label)!.MarginLeft.Value.ShouldBe(expected);
    }
}
