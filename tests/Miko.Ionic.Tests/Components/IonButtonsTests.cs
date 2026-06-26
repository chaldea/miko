using Miko.Testing;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonButtonsTests : IonicComponentTestBase
{
    [Fact]
    public void IonButtons_RendersWithStartSlotByDefault()
    {
        // Act
        var cut = Context.Render<IonButtons>();

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-buttons buttons-start");
    }

    [Fact]
    public void IonButtons_RendersWithStartSlot_WhenSlotIsStart()
    {
        // Act
        var cut = Context.Render<IonButtons>(parameters =>
            parameters.Add(nameof(IonButtons.Slot), "start"));

        // Assert
        cut.Root.Class.ShouldBe("md ion-buttons buttons-start");
    }

    [Fact]
    public void IonButtons_RendersWithEndSlot_WhenSlotIsEnd()
    {
        // Act
        var cut = Context.Render<IonButtons>(parameters =>
            parameters.Add(nameof(IonButtons.Slot), "end"));

        // Assert - Key style/class attribute
        cut.Root.Class.ShouldBe("md ion-buttons buttons-end");
    }

    [Fact]
    public void IonButtons_SlotIsCaseInsensitive()
    {
        // Act
        var cut1 = Context.Render<IonButtons>(parameters =>
            parameters.Add(nameof(IonButtons.Slot), "END"));

        var cut2 = Context.Render<IonButtons>(parameters =>
            parameters.Add(nameof(IonButtons.Slot), "End"));

        // Assert
        cut1.Root.Class.ShouldBe("md ion-buttons buttons-end");
        cut2.Root.Class.ShouldBe("md ion-buttons buttons-end");
    }

    [Fact]
    public void IonButtons_DefaultsToStart_ForInvalidSlotValue()
    {
        // Act
        var cut = Context.Render<IonButtons>(parameters =>
            parameters.Add(nameof(IonButtons.Slot), "invalid"));

        // Assert
        cut.Root.Class.ShouldBe("md ion-buttons buttons-start");
    }

    [Fact]
    public void IonButtons_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonButtons>();

        // Assert - DOM structure is the component contract
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldNotBeNull();
        cut.Root.Class.ShouldContain("ion-buttons");
    }
}
