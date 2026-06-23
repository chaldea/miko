using Miko.Testing;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonContentTests : IonicComponentTestBase
{
    [Fact]
    public void IonContent_RendersWithCorrectClass_WhenFullscreenIsFalse()
    {
        // Act
        var cut = Context.Render<IonContent>();

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("ion-content");
    }

    [Fact]
    public void IonContent_RendersWithFullscreenClass_WhenFullscreenIsTrue()
    {
        // Act
        var cut = Context.Render<IonContent>(parameters =>
            parameters.Add(nameof(IonContent.Fullscreen), true));

        // Assert - Key style/class attribute
        cut.Root.Class.ShouldBe("ion-content content-fullscreen");
    }

    [Fact]
    public void IonContent_HasInnerScrollContainer()
    {
        // Act
        var cut = Context.Render<IonContent>();

        // Assert - DOM structure is the component contract
        cut.Root.Children.Count.ShouldBe(1);
        var innerScroll = cut.Root.Children[0];
        innerScroll.TagName.ShouldBe("div");
        innerScroll.Class.ShouldBe("inner-scroll");
    }

    [Fact]
    public void IonContent_ParameterChange_UpdatesCssClass()
    {
        // This test demonstrates interaction/state assertion
        // In a real scenario, you might re-render with different parameters

        // Act - First render without fullscreen
        var cut1 = Context.Render<IonContent>(parameters =>
            parameters.Add(nameof(IonContent.Fullscreen), false));

        // Assert
        cut1.Root.Class.ShouldBe("ion-content");

        // Act - Second render with fullscreen
        var cut2 = Context.Render<IonContent>(parameters =>
            parameters.Add(nameof(IonContent.Fullscreen), true));

        // Assert
        cut2.Root.Class.ShouldBe("ion-content content-fullscreen");
    }
}
