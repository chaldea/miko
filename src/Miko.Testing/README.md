# Miko Testing

Testing framework for Miko - includes component testing, layout testing, and more utilities.

## Overview

This testing framework provides utilities for unit testing Miko applications. Currently focused on Razor component testing (inspired by [bUnit](https://bunit.dev)), with plans to expand into additional testing utilities for layout, styling, and other Miko features.

### Current Features

- **Component testing** - Unit test Razor components with full lifecycle support
- **DOM structure assertions** - Verify the component's rendered HTML structure
- **Computed style assertions** - Check applied styles and computed CSS properties
- **Layout assertions** - Validate box model, dimensions, and positioning
- **Parameter assertions** - Test component behavior with different parameters
- **Interaction testing** - Simulate user interactions and state changes

## Installation

Add a reference to `Miko.Testing` in your test project:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Miko.Testing\Miko.Testing.csproj" />
</ItemGroup>
```

## Quick Start

### Basic Test Setup

```csharp
using Miko.Testing;
using Shouldly;
using Xunit;

public class MyComponentTests : IDisposable
{
    protected TestContext Context { get; }

    public MyComponentTests()
    {
        Context = new TestContext();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
```

### Rendering a Component

```csharp
[Fact]
public void MyComponent_RendersCorrectly()
{
    // Act
    var cut = Context.Render<MyComponent>();

    // Assert
    cut.Root.TagName.ShouldBe("div");
    cut.Root.Class.ShouldBe("my-component");
}
```

### Testing with Parameters

```csharp
[Fact]
public void MyComponent_AppliesParameterCorrectly()
{
    // Act
    var cut = Context.Render<MyComponent>(parameters =>
        parameters.Add(nameof(MyComponent.Title), "Hello World"));

    // Assert
    cut.GetTextContent().ShouldContain("Hello World");
}
```

## Testing Standards

The framework supports the standard component testing dimensions:

### 1. DOM Structure Testing

The DOM structure is part of the component contract. Test that the component renders the expected elements:

```csharp
[Fact]
public void Component_HasCorrectDOMStructure()
{
    var cut = Context.Render<MyComponent>();

    cut.Root.TagName.ShouldBe("div");
    cut.Root.Children.Count.ShouldBe(2);
    cut.Root.Children[0].TagName.ShouldBe("header");
    cut.Root.Children[1].TagName.ShouldBe("section");
}
```

### 2. Parameter and State Testing

Test how components respond to parameters and state changes:

```csharp
[Fact]
public void Component_UpdatesOnParameterChange()
{
    var cut1 = Context.Render<MyComponent>(p => p.Add("Active", false));
    cut1.Root.Class.ShouldBe("component");

    var cut2 = Context.Render<MyComponent>(p => p.Add("Active", true));
    cut2.Root.Class.ShouldBe("component active");
}
```

### 3. Key Style Testing

Test critical styles that affect component appearance:

```csharp
[Fact]
public void Component_AppliesKeyStyles()
{
    var cut = Context.Render<MyComponent>();

    cut.Root.Style.Display.ShouldBe(Display.Flex);
    cut.Root.Style.FlexDirection.ShouldBe(FlexDirection.Column);
}
```

### 4. Box Model Testing (Layout Components)

For layout-critical components, test box model properties:

```csharp
[Fact]
public void LayoutComponent_HasCorrectBoxModel()
{
    var cut = Context.Render<LayoutComponent>();

    var boxModel = cut.GetBoxModel(cut.Root);
    boxModel.ShouldNotBeNull();
    boxModel!.Content.Width.ShouldBeGreaterThan(0);
}
```

## API Reference

### TestContext

Main entry point for rendering components.

```csharp
var context = new TestContext();

// Set viewport dimensions
context.ViewportWidth = 1024f;
context.ViewportHeight = 768f;

// Add stylesheets
context.AddStyleSheet(myStyleSheet);

// Render a component
var cut = context.Render<MyComponent>();
```

### ComponentUnderTest

Result of rendering a component, provides access to DOM, styles, and layout.

```csharp
// Access root element
Element root = cut.Root;

// Find elements
Element? element = cut.FindById("my-id");
List<Element> elements = cut.FindByClass("my-class");
List<Element> divs = cut.FindByTagName("div");

// Get computed styles
ComputedStyle? style = cut.GetComputedStyle(element);

// Get layout information
LayoutBox? layoutBox = cut.FindLayoutBox(element);
BoxModel? boxModel = cut.GetBoxModel(element);

// Get text content
string text = cut.GetTextContent();

// Get all elements
List<Element> allElements = cut.GetAllElements();
```

### ComponentParameterBuilder

Builder for setting component parameters.

```csharp
Context.Render<MyComponent>(parameters =>
{
    parameters.Add("Title", "My Title");
    parameters.Add("IsActive", true);
    parameters.AddChildContent(builder =>
    {
        builder.AddContent(0, "Child content");
    });
});
```

### Extension Methods

```csharp
// Fluent assertion extensions
cut.ShouldContainId("my-id");
cut.ShouldContainClass("my-class");
cut.ShouldContainTag("div");
cut.ShouldContainText("expected text");
```

## Example: Ionic Component Tests

See [tests/Miko.Ionic.Tests/](../../tests/Miko.Ionic.Tests/) for complete examples of component tests.

```csharp
[Fact]
public void IonButton_RendersWithCorrectSlot()
{
    var cut = Context.Render<IonButtons>(parameters =>
        parameters.Add(nameof(IonButtons.Slot), "end"));

    cut.Root.Class.ShouldBe("ion-buttons buttons-end");
}
```

## Best Practices

1. **Test the contract, not the implementation**: Focus on the component's public API (parameters, rendered output) rather than internal implementation details.

2. **Keep tests focused**: Each test should verify one aspect of the component's behavior.

3. **Use meaningful test names**: Test names should describe what is being tested and the expected outcome.

4. **Test edge cases**: Include tests for boundary conditions, null values, and error cases.

5. **Don't over-test styles**: Only test styles that are critical to the component's function. Don't test every CSS property.

6. **Use computed styles for assertions**: When checking styles, use `GetComputedStyle()` to get the final resolved values.

## Comparison with bUnit

This framework is inspired by bUnit but adapted for Miko's architecture:

| Feature | bUnit | Miko.Testing |
|---------|-------|----------------------|
| Component rendering | ✓ | ✓ |
| Parameter passing | ✓ | ✓ |
| DOM assertions | ✓ | ✓ |
| Style assertions | Limited | ✓ Full access |
| Layout assertions | ✗ | ✓ Box model access |
| Event simulation | ✓ | Planned |
| JSInterop mocking | ✓ | N/A |

## Contributing

When adding new assertion helpers or testing utilities, follow these guidelines:

1. Keep the API simple and fluent
2. Provide clear error messages when assertions fail
3. Add XML documentation for all public APIs
4. Include examples in tests

## License

MIT License - see LICENSE file for details.
