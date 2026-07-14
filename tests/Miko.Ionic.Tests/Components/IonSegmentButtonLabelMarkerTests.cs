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
/// ISSUE-064 §3: an <c>ion-label</c> inside an <c>ion-segment-button</c> (with no icon) must stamp
/// the <c>segment-button-has-label-only</c> marker on the button host, which drives the md label
/// margin rule (<c>segment-button.md.scss :host(.segment-button-has-label-only) ::slotted(ion-label)
/// { margin: 12px }</c>). iOS has no such rule.
/// </summary>
public class IonSegmentButtonLabelMarkerTests
{
    private static TestContext ContextFor(HostPlatform platform)
    {
        var ctx = new TestContext();
        ctx.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(platform));
        ctx.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        ctx.ViewportWidth = 400f;
        ctx.ViewportHeight = 300f;
        return ctx;
    }

    private static RenderFragment LabelChild(string text = "All") => builder =>
    {
        builder.OpenComponent<IonLabel>(0);
        builder.AddComponentParameter(1, nameof(IonLabel.ChildContent),
            (RenderFragment)(l => l.AddContent(0, text)));
        builder.CloseComponent();
    };

    private static RenderFragment IconChild(string icon = "star") => builder =>
    {
        builder.OpenComponent<IonIcon>(0);
        builder.AddComponentParameter(1, nameof(IonIcon.Icon), icon);
        builder.CloseComponent();
    };

    private static ComponentUnderTest RenderButton(TestContext ctx, RenderFragment? content = null) =>
        ctx.Render<IonSegmentButton>(p =>
        {
            p.Add(nameof(IonSegmentButton.Value), "a");
            if (content != null) p.AddChildContent(content);
        });

    [Fact]
    public void LabelOnlyButton_GetsHasLabelOnlyMarker()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderButton(ctx, LabelChild());

        cut.Root.ShouldHaveClass("segment-button-has-label");
        cut.Root.ShouldHaveClass("segment-button-has-label-only");
        cut.Root.ShouldNotHaveClass("segment-button-has-icon");
        cut.Root.ShouldNotHaveClass("segment-button-has-icon-only");
    }

    [Fact]
    public void EmptyButton_GetsNoContentMarkers()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderButton(ctx, content: null);

        cut.Root.ShouldNotHaveClass("segment-button-has-label");
        cut.Root.ShouldNotHaveClass("segment-button-has-label-only");
        cut.Root.ShouldNotHaveClass("segment-button-has-icon");
        cut.Root.ShouldNotHaveClass("segment-button-has-icon-only");
    }

    [Fact]
    public void IconOnlyButton_GetsHasIconOnlyMarker()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderButton(ctx, IconChild());

        cut.Root.ShouldHaveClass("segment-button-has-icon");
        cut.Root.ShouldHaveClass("segment-button-has-icon-only");
        cut.Root.ShouldNotHaveClass("segment-button-has-label");
        cut.Root.ShouldNotHaveClass("segment-button-has-label-only");
    }

    [Fact]
    public void LabelAndIconButton_GetsHasLabelAndHasIcon_ButNeitherOnly()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = ctx.Render<IonSegmentButton>(p =>
        {
            p.Add(nameof(IonSegmentButton.Value), "a");
            p.AddChildContent(builder =>
            {
                LabelChild()(builder);
                IconChild()(builder);
            });
        });

        cut.Root.ShouldHaveClass("segment-button-has-label");
        cut.Root.ShouldHaveClass("segment-button-has-icon");
        cut.Root.ShouldNotHaveClass("segment-button-has-label-only");
        cut.Root.ShouldNotHaveClass("segment-button-has-icon-only");
    }

    [Fact]
    public void MdLabelOnlyButton_LabelGetsVerticalMargin()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = RenderButton(ctx, LabelChild());

        var label = cut.Root.FindByClass("ion-label").First();
        var style = cut.GetComputedStyle(label)!;

        // md label-only rule: margin top/bottom 12px.
        style.MarginTop.Unit.ShouldBe(LengthUnit.Px);
        style.MarginTop.Value.ShouldBe(12f);
        style.MarginBottom.Value.ShouldBe(12f);
    }

    [Fact]
    public void IosLabelOnlyButton_LabelGetsNoVerticalMargin()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = RenderButton(ctx, LabelChild());

        var label = cut.Root.FindByClass("ion-label").First();
        var style = cut.GetComputedStyle(label)!;

        // iOS has no has-label-only margin rule.
        style.MarginTop.Value.ShouldNotBe(12f);
        style.MarginBottom.Value.ShouldNotBe(12f);
    }

    [Fact]
    public void MdButtonWithLabelAndIcon_LabelGetsNoLabelOnlyMargin()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = ctx.Render<IonSegmentButton>(p =>
        {
            p.Add(nameof(IonSegmentButton.Value), "a");
            p.AddChildContent(builder =>
            {
                LabelChild()(builder);
                IconChild()(builder);
            });
        });

        var label = cut.Root.FindByClass("ion-label").First();
        var style = cut.GetComputedStyle(label)!;

        // Not label-only → the margin rule doesn't apply.
        style.MarginTop.Value.ShouldNotBe(12f);
    }
}
