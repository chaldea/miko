using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-064 §4: a segment button's top/bottom padding must be 0 in BOTH modes. Ionic sets
/// <c>$segment-button-md-padding-top/-bottom: 0</c> and the iOS <c>--padding-top/-bottom: 0</c> —
/// the button's height comes from its <c>min-height</c>, not from vertical padding. The label is
/// centered by flex justify-content, so non-zero vertical padding would wrongly inflate the box.
/// </summary>
public class IonSegmentButtonPaddingTests
{
    private static TestContext ContextFor(HostPlatform platform)
    {
        var ctx = new TestContext();
        ctx.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(platform));
        ctx.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        ctx.ViewportWidth = 300f;
        ctx.ViewportHeight = 200f;
        return ctx;
    }

    private static ComponentUnderTest RenderButton(TestContext ctx) =>
        ctx.Render<IonSegmentButton>(p =>
        {
            p.Add(nameof(IonSegmentButton.Value), "a");
            p.AddChildContent(builder =>
            {
                builder.OpenComponent<IonLabel>(0);
                builder.AddComponentParameter(1, nameof(IonLabel.ChildContent),
                    (RenderFragment)(l => l.AddContent(0, "All")));
                builder.CloseComponent();
            });
        });

    // The padding lives on the native <button> (button-native), the host's first child — matching
    // Ionic where --padding-* maps to .button-native.
    private static Element NativeButton(ComponentUnderTest cut) =>
        cut.Root.Children[0];

    [Theory]
    [InlineData(HostPlatform.Android)] // md
    [InlineData(HostPlatform.Ios)]
    public void SegmentButton_HasZeroVerticalPadding(HostPlatform platform)
    {
        using var ctx = ContextFor(platform);
        var cut = RenderButton(ctx);

        var style = cut.GetComputedStyle(NativeButton(cut))!;
        style.PaddingTop.Value.ShouldBe(0f);
        style.PaddingBottom.Value.ShouldBe(0f);
    }

    [Fact]
    public void MdAndIosThemes_SegmentButtonVerticalPaddingIsZero()
    {
        // Per-mode theme tokens: both md and ios set the vertical padding to 0.
        IonicTheme.CreateMd().SegmentButtonPaddingY.ShouldBe(0f);
        IonicTheme.CreateIos().SegmentButtonPaddingY.ShouldBe(0f);
    }

    [Fact]
    public void SegmentButton_KeepsHorizontalPadding()
    {
        // Sanity: only the vertical padding is zeroed; the horizontal padding is unchanged
        // (md $segment-button-md-padding-end maps to a non-zero start/end here).
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderButton(ctx);

        var style = cut.GetComputedStyle(NativeButton(cut))!;
        style.PaddingLeft.Value.ShouldBeGreaterThan(0f);
        style.PaddingRight.Value.ShouldBeGreaterThan(0f);
    }
}
