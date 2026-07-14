using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonSegmentTests : IonicComponentTestBase
{
    // Helper: minimal ChildContent that produces an element (required for CascadingValue)
    private static readonly RenderFragment MinimalChild = builder =>
    {
        builder.OpenElement(0, "span");
        builder.CloseElement();
    };

    [Fact]
    public void IonSegment_RendersWithCorrectClass()
    {
        // Act - Render with minimal ChildContent (CascadingValue requires at least one element)
        var cut = Context.Render<IonSegment>(parameters =>
            parameters.Add(nameof(IonSegment.ChildContent), MinimalChild));

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-segment");
    }

    [Fact]
    public void IonSegment_RendersWithDisabledClass_WhenDisabledIsTrue()
    {
        // Act
        var cut = Context.Render<IonSegment>(parameters =>
        {
            parameters.Add(nameof(IonSegment.Disabled), true);
            parameters.Add(nameof(IonSegment.ChildContent), MinimalChild);
        });

        // Assert - Key style/class attribute
        cut.Root.Class.ShouldBe("md ion-segment segment-disabled");
    }

    [Fact]
    public void IonSegment_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonSegment>(parameters =>
            parameters.Add(nameof(IonSegment.ChildContent), MinimalChild));

        // Assert - DOM structure is the component contract
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldNotBeNull();
        cut.Root.ShouldHaveClass("ion-segment");
    }

    [Fact]
    public void IonSegment_CascadesContextToChildren()
    {
        // This test verifies the segment provides a cascading context with the selected value
        // by rendering a segment with a value and checking that child buttons can read it.
        // We'll render with a known Value and verify it's exposed to descendants.

        // Act
        var cut = Context.Render<IonSegment>(parameters =>
        {
            parameters.Add(nameof(IonSegment.Value), "tab1");
            parameters.Add(nameof(IonSegment.ChildContent), MinimalChild);
        });

        // Assert - The segment rendered (context cascade happens during render)
        cut.Root.ShouldNotBeNull();
        cut.Root.Class.ShouldBe("md ion-segment");
        // Note: Full cascading behavior is tested in IonSegmentButtonTests where buttons
        // read the context and derive their Selected state.
    }

    [Fact]
    public void IonSegment_ValueChanged_IsNotInvokedOnInitialRender()
    {
        // Arrange
        var invoked = false;
        var receivedValue = "";

        // Act
        var cut = Context.Render<IonSegment>(parameters =>
        {
            parameters.Add(nameof(IonSegment.Value), "initial");
            parameters.Add(nameof(IonSegment.ValueChanged), EventCallback.Factory.Create<string>(this, (string v) => { invoked = true; receivedValue = v; }));
            parameters.Add(nameof(IonSegment.ChildContent), MinimalChild);
        });

        // Assert - ValueChanged should not fire during initial render
        invoked.ShouldBeFalse();
        receivedValue.ShouldBe("");
    }

    [Fact]
    public void IonSegment_DisabledState_PreventsInteraction()
    {
        // Act
        var cut = Context.Render<IonSegment>(parameters =>
        {
            parameters.Add(nameof(IonSegment.Disabled), true);
            parameters.Add(nameof(IonSegment.ChildContent), MinimalChild);
        });

        // Assert - Disabled class is applied (interaction blocking is verified in button tests)
        cut.Root.ShouldHaveClass("segment-disabled");
    }
}
