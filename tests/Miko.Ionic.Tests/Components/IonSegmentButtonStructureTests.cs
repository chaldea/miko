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
/// ISSUE-064 §5: the segment-button DOM must mirror Ionic's host structure — the indicator is a
/// SIBLING of the native button, not nested inside it. With the indicator out of the button's
/// flex flow, the label centers correctly inside .button-native and (md label-only) sits 12px
/// from the button's top/bottom edges. Also covers the corrected md min-height (48px).
/// </summary>
public class IonSegmentButtonStructureTests
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

    private static ComponentUnderTest RenderLabelButton(TestContext ctx) =>
        ctx.Render<IonSegmentButton>(p =>
        {
            p.Add(nameof(IonSegmentButton.Value), "a");
            p.AddChildContent(b =>
            {
                b.OpenComponent<IonLabel>(0);
                b.AddComponentParameter(1, nameof(IonLabel.ChildContent),
                    (RenderFragment)(l => l.AddContent(0, "All")));
                b.CloseComponent();
            });
        });

    [Fact]
    public void Indicator_IsSiblingOfNativeButton_NotNestedInsideIt()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderLabelButton(ctx);

        var host = cut.Root;
        host.TagName.ShouldBe("div");
        host.ShouldHaveClass("ion-segment-button");

        // Children: [0] = .button-native, [1] = .segment-button-indicator (siblings).
        host.Children[0].Class.ShouldBe("button-native");
        host.Children[1].ShouldHaveClass("segment-button-indicator");

        // The indicator must NOT be inside the native button.
        host.Children[0].FindByClass("segment-button-indicator").Count.ShouldBe(0);
    }

    [Fact]
    public void Label_IsNestedInButtonNativeViaButtonInner()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderLabelButton(ctx);

        var native = cut.Root.Children[0];
        // button-native > button-inner > ion-label
        var inner = native.FindByClass("button-inner").First();
        inner.FindByClass("ion-label").Count.ShouldBe(1);
    }

    [Fact]
    public void MdLabelOnlyButton_LabelHas12pxVerticalMargin()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderLabelButton(ctx);

        var native = cut.Root.Children[0];
        var label = native.FindByClass("ion-label").First();
        var style = cut.GetComputedStyle(label)!;

        // The label-only rule (segment-button.md.scss) gives the label 12px top/bottom margin.
        style.MarginTop.Value.ShouldBe(12f);
        style.MarginBottom.Value.ShouldBe(12f);
    }

    [Fact]
    public void MdSegmentButton_MinHeightIs48px()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderLabelButton(ctx);

        // The host's min-height comes from the md theme token (was wrongly 32, now 48).
        var style = cut.GetComputedStyle(cut.Root)!;
        style.MinHeight.Value.ShouldBe(48f);
    }

    [Fact]
    public void MdLabel_IsVerticallyCenteredInButtonNative()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderLabelButton(ctx);

        var native = cut.Root.Children[0];
        var nativeBox = cut.GetBoxModel(native)!;
        var label = native.FindByClass("ion-label").First();
        var labelBox = cut.GetBoxModel(label)!;

        // With the indicator out of the flow and min-height 48px, the label's center aligns with
        // the button-native's center (label-only margin is symmetric, so centering holds).
        float nativeMidY = nativeBox.Content.Y + nativeBox.Content.Height / 2f;
        float labelMidY = labelBox.Content.Y + labelBox.Content.Height / 2f;
        Math.Abs(labelMidY - nativeMidY).ShouldBeLessThan(1f);
    }
}
