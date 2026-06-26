using Miko.Testing;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonLabelTests : IonicComponentTestBase
{
    [Fact]
    public void IonLabel_RendersWithCorrectClass()
    {
        // Act
        var cut = Context.Render<IonLabel>();

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-label");
    }

    [Fact]
    public void IonLabel_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonLabel>();

        // Assert - DOM structure is the component contract
        var elements = cut.GetAllElements();
        elements.Count.ShouldBe(1); // Only the root div
        elements[0].TagName.ShouldBe("div");
        elements[0].Class.ShouldBe("md ion-label");
    }
}
