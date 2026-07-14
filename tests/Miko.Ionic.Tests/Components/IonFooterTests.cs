using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Platform;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonFooterTests : IonicComponentTestBase
{
    [Fact]
    public void IonFooter_RendersWithCorrectDefaultClass()
    {
        // Act
        var cut = Context.Render<IonFooter>();

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md");
        cut.Root.ShouldHaveClass("ion-footer");
        cut.Root.ShouldHaveClass("footer-md");
        cut.Root.ShouldHaveClass("footer-toolbar-padding");
    }

    [Fact]
    public void IonFooter_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonFooter>();

        // Assert - DOM structure is the component contract
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Children.Count.ShouldBe(0); // No children when ChildContent is empty
    }

    [Fact]
    public void IonFooter_WithTranslucent_AddsTranslucentClass()
    {
        // Act
        var cut = Context.Render<IonFooter>(p => p.Add(nameof(IonFooter.Translucent), true));

        // Assert
        cut.Root.ShouldHaveClass("footer-translucent");
        cut.Root.ShouldHaveClass("footer-translucent-md");
    }

    [Fact]
    public void IonFooter_WithTranslucent_IosMode_RendersBackgroundDiv()
    {
        // Arrange
        UsePlatform(HostPlatform.Ios);

        // Act
        var cut = Context.Render<IonFooter>(p => p.Add(nameof(IonFooter.Translucent), true));

        // Assert - iOS mode with translucent renders footer-background div
        cut.Root.ShouldHaveClass("ios");
        cut.Root.ShouldHaveClass("footer-translucent-ios");
        cut.Root.Children.Count.ShouldBe(1);
        cut.Root.Children[0].TagName.ShouldBe("div");
        cut.Root.Children[0].Class.ShouldBe("footer-background");
    }

    [Fact]
    public void IonFooter_WithTranslucent_MdMode_DoesNotRenderBackgroundDiv()
    {
        // Act
        var cut = Context.Render<IonFooter>(p => p.Add(nameof(IonFooter.Translucent), true));

        // Assert - MD mode doesn't render footer-background
        cut.Root.ShouldHaveClass("md");
        cut.Root.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void IonFooter_WithCollapseFade_AddsCollapseClass()
    {
        // Act
        var cut = Context.Render<IonFooter>(p => p.Add(nameof(IonFooter.Collapse), "fade"));

        // Assert
        cut.Root.ShouldHaveClass("footer-collapse-fade");
    }

    [Fact]
    public void IonFooter_WithoutCollapse_DoesNotAddCollapseClass()
    {
        // Act
        var cut = Context.Render<IonFooter>();

        // Assert
        cut.Root.ShouldNotHaveClass("footer-collapse");
    }

    [Fact]
    public void IonFooter_IosMode_HasCorrectModeClass()
    {
        // Arrange
        UsePlatform(HostPlatform.Ios);

        // Act
        var cut = Context.Render<IonFooter>();

        // Assert
        cut.Root.ShouldHaveClass("ios");
        cut.Root.ShouldHaveClass("ion-footer");
        cut.Root.ShouldHaveClass("footer-ios");
    }

    [Fact]
    public void IonFooter_MdMode_HasCorrectModeClass()
    {
        // Act - default is MD mode
        var cut = Context.Render<IonFooter>();

        // Assert
        cut.Root.ShouldHaveClass("md");
        cut.Root.ShouldHaveClass("ion-footer");
        cut.Root.ShouldHaveClass("footer-md");
    }

    [Fact]
    public void IonFooter_WithAllOptions_CombinesClassesCorrectly()
    {
        // Arrange
        UsePlatform(HostPlatform.Ios);

        // Act
        var cut = Context.Render<IonFooter>(p =>
        {
            p.Add(nameof(IonFooter.Translucent), true);
            p.Add(nameof(IonFooter.Collapse), "fade");
        });

        // Assert
        cut.Root.ShouldHaveClass("ios");
        cut.Root.ShouldHaveClass("ion-footer");
        cut.Root.ShouldHaveClass("footer-ios");
        cut.Root.ShouldHaveClass("footer-translucent");
        cut.Root.ShouldHaveClass("footer-translucent-ios");
        cut.Root.ShouldHaveClass("footer-toolbar-padding");
        cut.Root.ShouldHaveClass("footer-collapse-fade");
    }
}
