using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonSegmentContentTests : IonicComponentTestBase
{
    [Fact]
    public void IonSegmentContent_RendersWithCorrectClass()
    {
        // Act - Render standalone (no view context)
        var cut = Context.Render<IonSegmentContent>(parameters =>
            parameters.Add(nameof(IonSegmentContent.Id), "content1"));

        // Assert - DOM structure (without view context, it's inactive so gets hidden class)
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-segment-content segment-content-hidden");
    }

    [Fact]
    public void IonSegmentContent_RendersWithIdAttribute()
    {
        // Act
        var cut = Context.Render<IonSegmentContent>(parameters =>
            parameters.Add(nameof(IonSegmentContent.Id), "my-content"));

        // Assert - DOM structure includes the id attribute
        cut.Root.Id.ShouldBe("my-content");
    }

    [Fact]
    public void IonSegmentContent_IsVisible_WhenIdMatchesViewValue()
    {
        // This test verifies a content element reads the view's cascaded value and shows itself
        // when its Id matches. We render a view with a value, nest a content with matching id.

        // Act - Render view with value "all", content with id "all"
        var cut = Context.Render<IonSegmentView>(parameters =>
        {
            parameters.Add(nameof(IonSegmentView.Value), "all");
            parameters.Add(nameof(IonSegmentView.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonSegmentContent>(0);
                builder.AddComponentParameter(1, nameof(IonSegmentContent.Id), "all");
                builder.CloseComponent();
            }));
        });

        // Assert - The content inside should NOT have the hidden class
        var contentElement = FindContentInTree(cut.Root);
        contentElement.ShouldNotBeNull();
        contentElement.Class.ShouldBe("md ion-segment-content");
        contentElement.ShouldNotHaveClass("segment-content-hidden");
    }

    [Fact]
    public void IonSegmentContent_IsHidden_WhenIdDoesNotMatchViewValue()
    {
        // Act - Render view with value "all", content with id "favorites"
        var cut = Context.Render<IonSegmentView>(parameters =>
        {
            parameters.Add(nameof(IonSegmentView.Value), "all");
            parameters.Add(nameof(IonSegmentView.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonSegmentContent>(0);
                builder.AddComponentParameter(1, nameof(IonSegmentContent.Id), "favorites");
                builder.CloseComponent();
            }));
        });

        // Assert - The content should have the hidden class
        var contentElement = FindContentInTree(cut.Root);
        contentElement.ShouldNotBeNull();
        contentElement.ShouldHaveClass("segment-content-hidden");
    }

    [Fact]
    public void IonSegmentContent_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonSegmentContent>(parameters =>
            parameters.Add(nameof(IonSegmentContent.Id), "test"));

        // Assert - DOM structure is the component contract
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Id.ShouldBe("test");
        cut.Root.Class.ShouldNotBeNull();
        cut.Root.ShouldHaveClass("ion-segment-content");
    }

    [Fact]
    public void IonSegmentContent_CanBeFoundById()
    {
        // Act
        var cut = Context.Render<IonSegmentContent>(parameters =>
            parameters.Add(nameof(IonSegmentContent.Id), "findme"));

        // Assert - FindById works (the id is set on the element)
        var found = cut.FindById("findme");
        found.ShouldNotBeNull();
        found.ShouldBe(cut.Root);
    }

    // Helper: recursively finds the first element with class containing "ion-segment-content"
    private Core.Element? FindContentInTree(Core.Element root)
    {
        if (root.Class?.Contains("ion-segment-content") == true) return root;
        foreach (var child in root.Children)
        {
            var found = FindContentInTree(child);
            if (found != null) return found;
        }
        return null;
    }
}
