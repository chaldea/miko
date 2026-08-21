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
/// ISSUE-064 §2: in <b>md</b> mode an <c>ion-segment</c> inside an <c>ion-toolbar</c> must inherit
/// the toolbar's height (Ionic's <c>segment.md.scss :host(.in-toolbar) { min-height: var(--min-height) }</c>,
/// where <c>--min-height</c> is the toolbar's 56px). A standalone segment, and the iOS toolbar
/// case (whose <c>.in-toolbar</c> rule sets margin/width, not min-height), get no such floor.
/// </summary>
public class IonSegmentInToolbarTests
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

    // Renders <IonToolbar><IonSegment/></IonToolbar> with the common two-button example.
    private static ComponentUnderTest RenderSegmentInToolbar(TestContext ctx) =>
        ctx.Render<IonToolbar>(p => p.Add(nameof(IonToolbar.ChildContent), (RenderFragment)(b =>
        {
            b.OpenComponent<IonSegment>(0);
            b.AddComponentParameter(1, nameof(IonSegment.Value), "all");
            b.AddComponentParameter(2, nameof(IonSegment.ChildContent), (RenderFragment)(seg =>
            {
                seg.OpenComponent<IonSegmentButton>(0);
                seg.AddComponentParameter(1, nameof(IonSegmentButton.Value), "all");
                seg.AddComponentParameter(2, nameof(IonSegmentButton.ChildContent),
                    (RenderFragment)(label => label.AddContent(0, "All")));
                seg.CloseComponent();
                seg.OpenComponent<IonSegmentButton>(3);
                seg.AddComponentParameter(4, nameof(IonSegmentButton.Value), "favorites");
                seg.AddComponentParameter(5, nameof(IonSegmentButton.ChildContent),
                    (RenderFragment)(label => label.AddContent(0, "Favorites")));
                seg.CloseComponent();
            }));
            b.CloseComponent();
        })));

    private static Element Segment(ComponentUnderTest cut) =>
        cut.Root.FindByClass("ion-segment").First();

    [Fact]
    public void MdSegment_InToolbar_InheritsToolbarMinHeight()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var toolbarMinHeight = IonicTheme.CreateMd().ToolbarMinHeight; // 56

        var cut = RenderSegmentInToolbar(ctx);
        var style = cut.GetComputedStyle(Segment(cut))!;

        Segment(cut).ShouldHaveClass("in-toolbar");
        style.MinHeight.Unit.ShouldBe(LengthUnit.Px);
        style.MinHeight.Value.ShouldBe(toolbarMinHeight);
    }

    [Fact]
    public void MdSegment_Standalone_HasNoToolbarMinHeight()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var toolbarMinHeight = IonicTheme.CreateMd().ToolbarMinHeight;

        // No enclosing toolbar → the .in-toolbar descendant rule never applies.
        var cut = ctx.Render<IonSegment>(p =>
        {
            p.Add(nameof(IonSegment.Value), "a");
            p.Add(nameof(IonSegment.ChildContent), (RenderFragment)(seg =>
            {
                seg.OpenComponent<IonSegmentButton>(0);
                seg.AddComponentParameter(1, nameof(IonSegmentButton.Value), "a");
                seg.CloseComponent();
            }));
        });

        cut.GetComputedStyle(Segment(cut))!.MinHeight.Value.ShouldNotBe(toolbarMinHeight);
    }

    [Fact]
    public void IosSegment_InToolbar_DoesNotInheritToolbarMinHeight()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var toolbarMinHeight = IonicTheme.CreateIos().ToolbarMinHeight; // 44

        // iOS's .in-toolbar rule sets margin/width/background, NOT min-height.
        var cut = RenderSegmentInToolbar(ctx);
        var style = cut.GetComputedStyle(Segment(cut))!;
        Segment(cut).ShouldHaveClass("in-toolbar");
        style.MinHeight.Value.ShouldNotBe(toolbarMinHeight);
        style.Width.IsAuto.ShouldBeTrue();
        style.MarginLeft.IsAuto.ShouldBeTrue();
        style.MarginRight.IsAuto.ShouldBeTrue();
        style.MarginTop.Value.ShouldBe(0f);
        style.MarginBottom.Value.ShouldBe(0f);
    }

    [Fact]
    public void IosSegment_InToolbar_AutoWidthShrinksToContentAndCenters()
    {
        using var ctx = ContextFor(HostPlatform.Ios);

        var cut = RenderSegmentInToolbar(ctx);
        var toolbar = cut.Root;
        var content = cut.FindByClass("toolbar-content").ShouldHaveSingleItem();
        var segment = Segment(cut);

        toolbar.ShouldHaveClass("toolbar-segment");
        cut.GetComputedStyle(content)!.Display.ShouldBe(Display.InlineFlex);

        var contentBox = cut.GetBoxModel(content).ShouldNotBeNull();
        var segmentBox = cut.GetBoxModel(segment).ShouldNotBeNull();
        segmentBox.BorderBox.Width.ShouldBeLessThan(contentBox.Content.Width);
        segmentBox.BorderBox.Width.ShouldBeGreaterThanOrEqualTo(140f);
        segmentBox.BorderBox.Left.ShouldBe(
            contentBox.Content.Left + (contentBox.Content.Width - segmentBox.BorderBox.Width) / 2f,
            0.01f);
    }

    [Fact]
    public void IosSegment_Standalone_RemainsFullWidth()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = ctx.Render<IonSegment>(p =>
        {
            p.Add(nameof(IonSegment.Value), "all");
            p.Add(nameof(IonSegment.ChildContent), (RenderFragment)(seg =>
            {
                seg.OpenComponent<IonSegmentButton>(0);
                seg.AddComponentParameter(1, nameof(IonSegmentButton.Value), "all");
                seg.AddComponentParameter(2, nameof(IonSegmentButton.ChildContent),
                    (RenderFragment)(label => label.AddContent(0, "All")));
                seg.CloseComponent();
            }));
        });

        var segment = Segment(cut);
        cut.GetComputedStyle(segment)!.Width.ShouldBe(Length.Percent(100));
        cut.GetBoxModel(segment)!.BorderBox.Width.ShouldBe(ctx.ViewportWidth, 0.01f);
    }
}
