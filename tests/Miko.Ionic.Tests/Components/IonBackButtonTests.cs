using Miko.Common;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <see cref="IonBackButton"/>. Verifies DOM structure, state, and key styles.
/// </summary>
public class IonBackButtonTests : IonicComponentTestBase
{
    private static ComponentUnderTest RenderBackButton(TestContext ctx,
        Action<ComponentParameterBuilder<IonBackButton>>? configure = null)
        => ctx.Render<IonBackButton>(configure ?? (_ => { }));

    [Fact]
    public void Build_ShouldGenerateCorrectDomStructure()
    {
        // Arrange & Act
        var cut = RenderBackButton(Context, p => p.Add(nameof(IonBackButton.DefaultHref), "/home"));

        // Assert
        cut.Root.ShouldHaveClass("ion-back-button");
        cut.Root.Children.Count.ShouldBe(1);

        var button = cut.Root.Children[0];
        button.ShouldHaveClass("button-native");
        button.Children.Count.ShouldBe(1);

        var inner = button.Children[0];
        inner.ShouldHaveClass("button-inner");
        // inner should contain icon + optional text span
        inner.Children.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Build_WithoutDefaultHref_ShouldBeHidden()
    {
        // Arrange & Act
        var cut = RenderBackButton(Context);

        // Assert
        cut.Root.ShouldNotHaveClass("show-back-button");
    }

    [Fact]
    public void Build_WithDefaultHref_ShouldShowButton()
    {
        // Arrange & Act
        var cut = RenderBackButton(Context, p => p.Add(nameof(IonBackButton.DefaultHref), "/home"));

        // Assert
        cut.Root.ShouldHaveClass("show-back-button");
    }

    [Fact]
    public void Build_WhenDisabled_ShouldHaveDisabledClass()
    {
        // Arrange & Act
        var cut = RenderBackButton(Context, p =>
        {
            p.Add(nameof(IonBackButton.DefaultHref), "/home");
            p.Add(nameof(IonBackButton.Disabled), true);
        });

        // Assert
        cut.Root.ShouldHaveClass("back-button-disabled");
    }

    [Fact]
    public void Build_WithColor_ShouldHaveColorClass()
    {
        // Arrange & Act
        var cut = RenderBackButton(Context, p =>
        {
            p.Add(nameof(IonBackButton.DefaultHref), "/home");
            p.Add(nameof(IonBackButton.Color), "primary");
        });

        // Assert
        cut.Root.ShouldHaveClass("ion-color-primary");
    }

    [Fact]
    public void Build_IconOnly_ShouldHaveIconOnlyClass()
    {
        // Arrange - MD mode defaults to no text (icon-only)
        var cut = RenderBackButton(Context, p => p.Add(nameof(IonBackButton.DefaultHref), "/home"));

        // Assert - MD mode should have icon-only by default
        cut.Root.ShouldHaveClass("back-button-has-icon-only");
    }

    [Fact]
    public void Build_WithCustomIcon_ShouldUseCustomIcon()
    {
        // Arrange & Act
        var cut = RenderBackButton(Context, p =>
        {
            p.Add(nameof(IonBackButton.DefaultHref), "/home");
            p.Add(nameof(IonBackButton.Icon), "arrow-forward");
        });

        // Assert - the icon element should be present
        var inner = cut.Root.Children[0].Children[0];
        var icon = inner.Children[0];
        icon.ShouldHaveClass("back-button-icon");
    }

    [Fact]
    public void Build_WithCustomText_ShouldIncludeText()
    {
        // Arrange & Act
        var cut = RenderBackButton(Context, p =>
        {
            p.Add(nameof(IonBackButton.DefaultHref), "/home");
            p.Add(nameof(IonBackButton.Text), "返回");
        });

        // Assert - should NOT have icon-only class
        cut.Root.ShouldNotHaveClass("back-button-has-icon-only");

        // Should have both icon and text
        var inner = cut.Root.Children[0].Children[0];
        inner.Children.Count.ShouldBe(2);  // icon + text span
    }

    [Fact]
    public void Styles_WithDefaultHref_ShouldBeVisible()
    {
        // Arrange
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = RenderBackButton(Context, p => p.Add(nameof(IonBackButton.DefaultHref), "/home"));

        // Act & Assert - with show-back-button, display should be Block
        var computed = cut.GetComputedStyle(cut.Root);
        computed.ShouldNotBeNull();
        computed.Display.ShouldBe(Display.Block);
    }
}
