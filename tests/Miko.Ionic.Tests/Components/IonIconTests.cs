using Miko.Common;
using Miko.Styling;
using Miko.Testing;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonIconTests : IonicComponentTestBase
{
    [Fact]
    public void IonIcon_RendersWithCorrectClass()
    {
        // Act
        var cut = Context.Render<IonIcon>();

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-icon");
    }

    [Fact]
    public void IonIcon_HasNoStyle_WhenIconIsNull()
    {
        // Act
        var cut = Context.Render<IonIcon>();

        // Assert
        cut.Root.Style.ShouldBeNull();
    }

    [Fact]
    public void IonIcon_AppliesBackgroundStyle_WhenIconIsProvided()
    {
        // Act
        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), "triangle"));

        // Assert - Key style attribute
        cut.Root.Style.ShouldNotBeNull();
        cut.Root.Style!.BackgroundImage.ShouldNotBeNull();
        cut.Root.Style.BackgroundSize.ShouldBe(BackgroundSize.Contain);
        cut.Root.Style.BackgroundPosition.ShouldBe(BackgroundPosition.Center);
        cut.Root.Style.BackgroundRepeat.ShouldBe(BackgroundRepeat.NoRepeat);
    }

    [Fact]
    public void IonIcon_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), "triangle"));

        // Assert - DOM structure is the component contract
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-icon");
        cut.Root.Children.Count.ShouldBe(0); // Icon is rendered via background-image, not children
    }

    [Fact]
    public void IonIcon_PreservesColor_WhenMergingIconStyle()
    {
        // Color drives the icon tint (CSS fill: currentColor) and must survive the
        // background-image merge — the icon style only sets background properties.
        var cut = Context.Render<IonIcon>(parameters => parameters
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Style), new Style { Color = Color.FromRgb(255, 255, 255) }));

        cut.Root.Style.ShouldNotBeNull();
        cut.Root.Style!.BackgroundImage.ShouldNotBeNull();
        cut.Root.Style.Color.ShouldBe(Color.FromRgb(255, 255, 255));
    }

    [Fact]
    public void IonIcon_MarksBackgroundImageAsTemplate()
    {
        // Ionicons glyphs are monochrome masks — the resolved image must be tintable.
        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), "triangle"));

        cut.Root.Style!.BackgroundImage!.Value.Value.IsTemplate.ShouldBeTrue();
    }

    [Fact]
    public void IonIcon_UpdatesStyle_WhenIconParameterChanges()
    {
        // Act - First render with no icon
        var cut1 = Context.Render<IonIcon>();

        // Assert
        cut1.Root.Style.ShouldBeNull();

        // Act - Second render with icon
        var cut2 = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), "triangle"));

        // Assert - State change assertion
        cut2.Root.Style.ShouldNotBeNull();
        cut2.Root.Style!.BackgroundImage.ShouldNotBeNull();
    }
}
