using Miko.Testing;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonHeaderTests : IonicComponentTestBase
{
    [Fact]
    public void IonHeader_RendersWithCorrectClass()
    {
        // Act
        var cut = Context.Render<IonHeader>();

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("ion-header");
    }

    [Fact]
    public void IonHeader_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonHeader>();

        // Assert - DOM structure is the component contract
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("ion-header");
        cut.Root.Children.Count.ShouldBe(0); // No children when ChildContent is empty
    }
}
