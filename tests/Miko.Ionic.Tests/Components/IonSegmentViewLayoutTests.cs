using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Layout/BoxModel coverage for the Segment view + content (ISSUE-064 follow-up).
/// <para>
/// Regression guard: Ionic's <c>segment-view</c> has <c>height: 100%</c> (filling its parent),
/// while <c>segment-content</c> has NO explicit height (it sizes to its children via
/// <c>min-height: 1px</c> + <c>flex-shrink: 0</c>). An early porting error had the view with NO
/// height (auto) AND the content pinning <c>height: 100%</c> (which doesn't exist in Ionic's
/// source) — that created a circular dependency where the view's auto height waited on the content,
/// while the content's percentage height resolved against the view's still-zero height — collapsing
/// the whole subtree to 0 and hiding all content. The faithful port has <c>view { height: 100% }</c>
/// and <c>content { min-height: 1px }</c> (no height: 100%), so the view fills available height
/// and the content sizes to its children.
/// </para>
/// </summary>
public class IonSegmentViewLayoutTests : IonicComponentTestBase
{
    public IonSegmentViewLayoutTests()
    {
        // Load the real Ionic stylesheet so the ported segment styles drive layout.
        Context.AddStyleSheet(IonicStyleSheetFactory.Create(IonicTheme.CreateMd()));

        // Give the inner content child a definite 120px height so the active content has a
        // concrete natural height to size to.
        Context.AddStyleSheet(new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new ClassSelector("sized-child"),
                    Style = new Style { Display = Display.Block, Height = Length.Px(120), Width = Length.Percent(100) },
                },
            },
        });
    }

    // Builds: <IonSegmentView Value="all"> <IonSegmentContent Id="all"> <div class="sized-child"/> </IonSegmentContent> </IonSegmentView>
    private ComponentUnderTest RenderViewWithSizedContent()
    {
        return Context.Render<IonSegmentView>(parameters =>
        {
            parameters.Add(nameof(IonSegmentView.Value), "all");
            parameters.Add(nameof(IonSegmentView.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonSegmentContent>(0);
                builder.AddComponentParameter(1, nameof(IonSegmentContent.Id), "all");
                builder.AddComponentParameter(2, nameof(IonSegmentContent.ChildContent), (RenderFragment)(inner =>
                {
                    inner.OpenElement(0, "div");
                    inner.AddAttribute(1, "class", "sized-child");
                    inner.CloseElement();
                }));
                builder.CloseComponent();
            }));
        });
    }

    [Fact]
    public void ActiveSegmentContent_HasNonZeroHeight_SizedToItsChildren()
    {
        // Act
        var cut = RenderViewWithSizedContent();

        // Find the active content element (visible — no hidden class)
        var content = cut.FindById("all");
        content.ShouldNotBeNull();
        content!.Class.ShouldBe("ion-segment-content"); // active, not hidden

        // Assert - the content sizes to its 120px child, NOT collapsed to 0
        var box = cut.GetBoxModel(content);
        box.ShouldNotBeNull();
        box!.Content.Height.ShouldBe(120f);
    }

    [Fact]
    public void SegmentView_HasNonZeroHeight_SizedToActiveContent()
    {
        // Act
        var cut = RenderViewWithSizedContent();

        // The view is the rendered root (its CascadingValue wrapper sits inside it).
        var view = cut.Root.FindByClass("ion-segment-view").FirstOrDefault() ?? cut.Root;
        view.ShouldNotBeNull();

        // Assert - the view has a real, non-zero height (it fills its available height via
        // `height: 100%`), NOT collapsed to 0 as it was when content also pinned `height: 100%`.
        var box = cut.GetBoxModel(view);
        box.ShouldNotBeNull();
        box!.Content.Height.ShouldBeGreaterThanOrEqualTo(120f);
    }

    [Fact]
    public void HiddenSegmentContent_DoesNotContributeHeight()
    {
        // Act - render a view whose active value is "all" but with a hidden "favorites" content
        var cut = Context.Render<IonSegmentView>(parameters =>
        {
            parameters.Add(nameof(IonSegmentView.Value), "all");
            parameters.Add(nameof(IonSegmentView.ChildContent), (RenderFragment)(builder =>
            {
                // active content sized 120px
                builder.OpenComponent<IonSegmentContent>(0);
                builder.AddComponentParameter(1, nameof(IonSegmentContent.Id), "all");
                builder.AddComponentParameter(2, nameof(IonSegmentContent.ChildContent), (RenderFragment)(inner =>
                {
                    inner.OpenElement(0, "div");
                    inner.AddAttribute(1, "class", "sized-child");
                    inner.CloseElement();
                }));
                builder.CloseComponent();

                // hidden content sized 120px — should contribute 0 (display:none)
                builder.OpenComponent<IonSegmentContent>(3);
                builder.AddComponentParameter(4, nameof(IonSegmentContent.Id), "favorites");
                builder.AddComponentParameter(5, nameof(IonSegmentContent.ChildContent), (RenderFragment)(inner =>
                {
                    inner.OpenElement(0, "div");
                    inner.AddAttribute(1, "class", "sized-child");
                    inner.CloseElement();
                }));
                builder.CloseComponent();
            }));
        });

        // Assert - the ACTIVE content sizes to its 120px child; the HIDDEN content is
        // display:none and so contributes no height (its box collapses to 0). This proves
        // the inactive page is laid out as hidden rather than stacking 240px of content.
        var active = cut.FindById("all");
        var hidden = cut.FindById("favorites");
        active.ShouldNotBeNull();
        hidden.ShouldNotBeNull();
        hidden!.Class.ShouldContain("segment-content-hidden");

        var activeBox = cut.GetBoxModel(active!);
        var hiddenBox = cut.GetBoxModel(hidden);
        activeBox.ShouldNotBeNull();
        activeBox!.Content.Height.ShouldBe(120f);
        // display:none → no layout box height (display:none elements collapse to 0).
        (hiddenBox?.Content.Height ?? 0f).ShouldBe(0f);
    }
}
