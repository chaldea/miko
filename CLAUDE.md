# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Miko is a lightweight DOM rendering engine written in C# that uses SkiaSharp for graphics rendering. It implements a browser-like rendering pipeline but constructs the DOM tree and styles programmatically (no HTML/CSS parsing). The project is similar to a simplified browser rendering engine.

**Key characteristics:**
- Target framework: .NET 10.0
- Rendering backend: SkiaSharp
- Testing framework: xUnit with Shouldly assertions
- Language features: C# with nullable reference types enabled

## Build and Test Commands

### Building the project
```bash
dotnet build miko.slnx
```

### Running tests
```bash
# Run all tests
dotnet test

# Run tests in a specific project
dotnet test tests/Miko.Tests/Miko.Tests.csproj

# Run a specific test class
dotnet test --filter "FullyQualifiedName~Miko.Tests.Common.ColorTests"

# Run tests with detailed output
dotnet test --verbosity detailed
```

### Cleaning build artifacts
```bash
dotnet clean
```

## Architecture Overview

Miko follows a layered architecture similar to browser rendering engines:

```
Application Layer (DOM construction + styling)
    ↓
DOM Layer (Element tree structure)
    ↓
Style System (StyleSheet + Selectors + Cascade)
    ↓
Layout Engine (Box Model + Layout Algorithms)
    ↓
Render Engine (Dirty regions + SkiaSharp painting)
```

### Core Modules

1. **Core** (`src/Miko/Core/`)
   - `Element.cs`: Abstract base class for all DOM elements
   - `DomElements/`: Concrete element implementations (Div, Heading, Button, Image, etc.)
   - `MikoEngine.cs`: Main engine coordinating layout and rendering

2. **Styling** (`src/Miko/Styling/`)
   - `Style.cs`: Style properties (layout, box model, visual, positioning)
   - `StyleSheet.cs` & `StyleResolver.cs`: CSS-like style system with selectors
   - `Selectors/`: Selector implementations (class, id, tag)
   - `ComputedStyle.cs`: Final computed styles after cascade

3. **Layout** (`src/Miko/Layout/`)
   - `LayoutEngine.cs`: Orchestrates style computation and layout calculation
   - `LayoutBox.cs`: Layout tree node with computed dimensions
   - `BoxModel.cs`: CSS box model (content, padding, border, margin)
   - `LayoutAlgorithms/`: Block, Inline, and Flex layout implementations

4. **Rendering** (`src/Miko/Rendering/`)
   - `RenderEngine.cs`: Manages rendering with dirty region optimization
   - `Painter.cs`: SkiaSharp drawing primitives
   - `DirtyRegionManager.cs`: Tracks and merges dirty regions for incremental rendering

5. **Common** (`src/Miko/Common/`)
   - `Length.cs`: CSS-like length units (px, %, auto)
   - `Color.cs`: Color representation
   - `RectF.cs`: Rectangle geometry
   - `Enums.cs`: Display, Position, FlexDirection, etc.

6. **Utils** (`src/Miko/Utils/`)
   - `TreeTraversal.cs`: DOM tree traversal utilities
   - `GeometryUtils.cs`: Geometric calculations

### Rendering Pipeline

1. **DOM Construction**: Application creates Element tree programmatically
2. **Style Computation**: StyleResolver matches selectors and cascades styles
3. **Layout Tree Building**: Filter elements by display property
4. **Layout Calculation**: Compute box dimensions and positions using layout algorithms
5. **Painting**: RenderEngine draws to SkiaSharp canvas (with dirty region optimization)

### Key Design Patterns

- **Tree Structure**: DOM elements form a parent-child tree
- **Dirty Marking**: Elements track when they need re-layout/re-render
- **Two-Phase Layout**: Constraints flow down (parent→child), sizes flow up (child→parent)
- **Style Cascade**: Inline styles > ID selectors > Class selectors > Tag selectors
- **Incremental Rendering**: Only repaint dirty regions for performance

## Development Guidelines

### Working with Elements

- All elements inherit from `Element` base class
- Elements must implement `TagName` property
- Use `AddChild()`/`RemoveChild()` to maintain parent-child relationships (automatically sets `IsDirty`)
- Element lookup methods: `FindById()`, `FindByClass()`, `FindByTagName()`

### Working with Styles

- Styles use nullable properties to support cascade (null = not set)
- Use `Style.Merge()` for cascading styles
- `ComputedStyle` contains final resolved values after cascade
- Selector specificity: ID (highest) > Class > Tag (lowest)

### Working with Layout

- Layout algorithms are in `Layout/LayoutAlgorithms/`
- Each display type (Block, Inline, Flex) has its own layout algorithm
- `LayoutConstraints` pass available space from parent to child
- `BoxModel` calculates content/padding/border/margin boxes

### Working with Rendering

- `RenderEngine` requires an `SKCanvas` to be set before rendering
- Use `MikoEngine.InvalidateElement()` to mark elements dirty
- `DirtyRegionManager` automatically merges overlapping/adjacent regions
- Full render: `MikoEngine.Render()`, Incremental: `MikoEngine.Update()`

### Testing Conventions

- Test files mirror source structure: `tests/Miko.Tests/[Module]/[Class]Tests.cs`
- Use Shouldly for assertions: `result.ShouldBe(expected)`
- Test naming: descriptive method names explaining the scenario
- Unit tests focus on individual components (selectors, box model, etc.)

## Common Patterns

### Creating a DOM tree with styles

```csharp
var styleSheet = new StyleSheet
{
    Rules = new List<StyleRule>
    {
        new StyleRule
        {
            Selector = new ClassSelector("container"),
            Style = new Style { Display = Display.Flex, Padding = Length.Px(20) }
        }
    }
};

var root = new DivElement
{
    Class = "container",
    Children = { new H1Element { TextContent = "Hello" } }
};
```

### Initializing and rendering

```csharp
var engine = new MikoEngine();
engine.Initialize(root, styleSheets, canvas, viewportWidth, viewportHeight);

// Later, for updates:
element.TextContent = "Updated";
engine.InvalidateElement(element);
engine.Update(canvas);
```

### Implementing a new layout algorithm

Layout algorithms should:
1. Calculate element's own dimensions based on constraints
2. Calculate margin, border, padding
3. Layout children (algorithm-specific)
4. Set final dimensions on the LayoutBox

## File Organization

The project follows a clear module-based structure:
- Source code: `src/Miko/`
- Tests: `tests/Miko.Tests/`
- Documentation: `docs/` (ARCHITECTURE.md, REQUIREMENT.md)
- Solution file: `miko.slnx` (Visual Studio solution)

## Important Notes

- This is a .NET 10.0 project (requires appropriate SDK)
- SkiaSharp resources should be properly disposed (use `using` statements)
- The project uses nullable reference types - respect null annotations
- Layout calculations use floats - be aware of floating-point precision
- The architecture document (`docs/ARCHITECTURE.md`) contains detailed design specifications
