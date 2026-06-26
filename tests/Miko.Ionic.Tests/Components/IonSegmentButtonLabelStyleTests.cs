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
/// ISSUE-064 §4: the base <c>segment-button.scss</c> <c>::slotted(ion-label)</c> rule must be
/// ported — a label inside a segment button is a centered, single-line (22px), clipped,
/// non-interactive block. Applies in both modes (it's the base rule, not mode-specific).
/// </summary>
public class IonSegmentButtonLabelStyleTests
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

    private static ComponentUnderTest RenderButtonWithLabel(TestContext ctx) =>
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

    [Theory]
    [InlineData(HostPlatform.Android)] // md
    [InlineData(HostPlatform.Ios)]
    public void LabelInSegmentButton_GetsSlottedLabelStyles(HostPlatform platform)
    {
        using var ctx = ContextFor(platform);
        var cut = RenderButtonWithLabel(ctx);
        var label = cut.Root.FindByClass("ion-label").First();
        var computed = cut.GetComputedStyle(label)!;

        computed.Display.ShouldBe(Display.Block);
        computed.AlignSelf.ShouldBe(AlignSelf.Center);
        computed.MaxWidth.Unit.ShouldBe(LengthUnit.Percent);
        computed.MaxWidth.Value.ShouldBe(100f);
        computed.LineHeight.Value.ShouldBe(22f);
        computed.WhiteSpace.ShouldBe(WhiteSpace.Nowrap);
        computed.OverflowX.ShouldBe(Overflow.Hidden);
        computed.OverflowY.ShouldBe(Overflow.Hidden);
        computed.BoxSizing.ShouldBe(BoxSizing.BorderBox);
        computed.PointerEvents.ShouldBe(PointerEvents.None);
    }

    [Fact]
    public void StandaloneLabel_DoesNotGetSegmentButtonLabelStyles()
    {
        // A bare ion-label (not inside a segment button) keeps the default ion-label styles —
        // the descendant rule must not leak out.
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = ctx.Render<IonLabel>(p =>
            p.AddChildContent(l => l.AddContent(0, "Plain")));

        var style = cut.GetComputedStyle(cut.Root)!;
        // The segment-button-scoped 22px line-height must not apply here.
        style.LineHeight.Value.ShouldNotBe(22f);
    }
}
