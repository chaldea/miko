using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonSegmentButtonTests : IonicComponentTestBase
{
    [Fact]
    public void IonSegmentButton_RendersWithCorrectTag()
    {
        // Act - Render a button standalone (no segment context)
        var cut = Context.Render<IonSegmentButton>(parameters =>
            parameters.Add(nameof(IonSegmentButton.Value), "test"));

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("button");
        cut.Root.Class.ShouldBe("ion-segment-button");
    }

    [Fact]
    public void IonSegmentButton_RendersCheckedClass_WhenSelectedBySegment()
    {
        // This test verifies a button reads the segment's cascaded context and derives its
        // Selected state. We build a segment with a value, nest a button with a matching value,
        // and verify the button gets the checked class.

        // Arrange - Build a segment wrapping a button
        var segment = new IonSegment { Value = "tab1" };
        var button = new IonSegmentButton { Value = "tab1" };

        // Manually build the segment's element tree (simulating what a Razor file would do)
        // with the button as a child. The segment's Build() will cascade the context.
        var segmentElement = segment.Build();

        // The segment wraps children in a CascadingValue, so we need to simulate the child
        // button being rendered inside that scope. For this test, we'll render the segment
        // and check the generated tree structure.

        // Act - Render the segment with inline ChildContent that includes the button
        var cut = Context.Render<IonSegment>(parameters =>
        {
            parameters.Add(nameof(IonSegment.Value), "tab1");
            parameters.Add(nameof(IonSegment.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonSegmentButton>(0);
                builder.AddComponentParameter(1, nameof(IonSegmentButton.Value), "tab1");
                builder.CloseComponent();
            }));
        });

        // Assert - The button inside should have the checked class
        // Navigate to the button element (segment > cascading wrapper > button)
        var buttonElement = FindButtonInTree(cut.Root);
        buttonElement.ShouldNotBeNull();
        buttonElement.Class.ShouldContain("segment-button-checked");
    }

    [Fact]
    public void IonSegmentButton_DoesNotRenderCheckedClass_WhenNotSelected()
    {
        // Act - Render segment with value "tab1", button with value "tab2"
        var cut = Context.Render<IonSegment>(parameters =>
        {
            parameters.Add(nameof(IonSegment.Value), "tab1");
            parameters.Add(nameof(IonSegment.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonSegmentButton>(0);
                builder.AddComponentParameter(1, nameof(IonSegmentButton.Value), "tab2");
                builder.CloseComponent();
            }));
        });

        // Assert - The button should NOT have the checked class
        var buttonElement = FindButtonInTree(cut.Root);
        buttonElement.ShouldNotBeNull();
        buttonElement.Class.ShouldNotContain("segment-button-checked");
        buttonElement.Class.ShouldBe("ion-segment-button");
    }

    [Fact]
    public void IonSegmentButton_RendersDisabledClass_WhenDisabled()
    {
        // Act
        var cut = Context.Render<IonSegmentButton>(parameters =>
        {
            parameters.Add(nameof(IonSegmentButton.Value), "test");
            parameters.Add(nameof(IonSegmentButton.Disabled), true);
        });

        // Assert - Key style/class attribute
        cut.Root.Class.ShouldContain("segment-button-disabled");
    }

    [Fact]
    public void IonSegmentButton_RendersLayoutClass_WhenLayoutProvided()
    {
        // Act
        var cut = Context.Render<IonSegmentButton>(parameters =>
        {
            parameters.Add(nameof(IonSegmentButton.Value), "test");
            parameters.Add(nameof(IonSegmentButton.Layout), "icon-top");
        });

        // Assert
        cut.Root.Class.ShouldContain("segment-button-layout-icon-top");
    }

    [Fact]
    public void IonSegmentButton_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonSegmentButton>(parameters =>
            parameters.Add(nameof(IonSegmentButton.Value), "test"));

        // Assert - DOM structure is the component contract
        cut.Root.TagName.ShouldBe("button");
        cut.Root.Class.ShouldNotBeNull();
        cut.Root.Class.ShouldContain("ion-segment-button");
    }

    // Helper: recursively finds the first <button> element in the tree
    private Core.Element? FindButtonInTree(Core.Element root)
    {
        if (root.TagName == "button") return root;
        foreach (var child in root.Children)
        {
            var found = FindButtonInTree(child);
            if (found != null) return found;
        }
        return null;
    }
}
