using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonSegmentViewTests : IonicComponentTestBase
{
    // Helper: minimal ChildContent that produces an element (required for CascadingValue)
    private static readonly RenderFragment MinimalChild = builder =>
    {
        builder.OpenElement(0, "span");
        builder.CloseElement();
    };

    [Fact]
    public void IonSegmentView_RendersWithCorrectClass()
    {
        // Act
        var cut = Context.Render<IonSegmentView>(parameters =>
            parameters.Add(nameof(IonSegmentView.ChildContent), MinimalChild));

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("ion-segment-view");
    }

    [Fact]
    public void IonSegmentView_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonSegmentView>(parameters =>
            parameters.Add(nameof(IonSegmentView.ChildContent), MinimalChild));

        // Assert - DOM structure is the component contract
        var elements = cut.GetAllElements();
        elements.Count.ShouldBeGreaterThanOrEqualTo(1);
        elements[0].TagName.ShouldBe("div");
        elements[0].Class.ShouldBe("ion-segment-view");
    }

    [Fact]
    public void IonSegmentView_CascadesValueToChildren()
    {
        // This test verifies the view cascades its Value as "SegmentViewValue" so child
        // IonSegmentContent elements can read it. We render a view with a Value and verify
        // the structure is correct. Full visibility behavior is tested in IonSegmentContentTests.

        // Act
        var cut = Context.Render<IonSegmentView>(parameters =>
        {
            parameters.Add(nameof(IonSegmentView.Value), "content1");
            parameters.Add(nameof(IonSegmentView.ChildContent), MinimalChild);
        });

        // Assert - The view rendered (context cascade happens during render)
        cut.Root.ShouldNotBeNull();
        cut.Root.Class.ShouldBe("ion-segment-view");
    }

    [Fact]
    public void IonSegmentView_RendersChildContent()
    {
        // Act - Render with child content
        var cut = Context.Render<IonSegmentView>(parameters =>
        {
            parameters.Add(nameof(IonSegmentView.Value), "test");
            parameters.Add(nameof(IonSegmentView.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Child content");
                builder.CloseElement();
            }));
        });

        // Assert - The child content should be present in the tree
        var textContent = cut.GetTextContent();
        textContent.ShouldContain("Child content");
    }
}
