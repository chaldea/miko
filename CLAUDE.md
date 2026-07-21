# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Miko is a native, cross-platform UI rendering engine for .NET that uses Razor as its layout DSL. It draws every pixel with SkiaSharp — no browser, no WebView, no HTML/CSS runtime — running a browser-like pipeline of style cascade, layout, and incremental painting directly onto a GPU-accelerated canvas.

**Key characteristics:**
- Target framework: .NET 10.0
- Rendering backend: SkiaSharp
- UI DSL: Razor components (compiled by custom source generator)
- Testing framework: xUnit with Shouldly assertions
- Language features: C# with nullable reference types enabled

## Build and Test Commands

### Building the project
```bash
# Build all projects
dotnet build miko.slnx

# Build a specific project
dotnet build src/Miko/Miko.csproj

# Build mobile projects (requires Android/iOS workloads)
dotnet build src/Miko.Android/Miko.Android.csproj
dotnet build src/Miko.iOS/Miko.iOS.csproj
```

### Running tests
```bash
# Run all tests
dotnet test

# Run tests in a specific project
dotnet test tests/Miko.Tests/Miko.Tests.csproj
dotnet test tests/Miko.Ionic.Tests/Miko.Ionic.Tests.csproj

# Run a specific test class
dotnet test --filter "FullyQualifiedName~Miko.Tests.Common.ColorTests"

# Run tests with detailed output
dotnet test --verbosity detailed
```

### Running examples
```bash
# Desktop examples (using Miko.Windowing)
dotnet run --project examples/Windows/MikoAppBlank/MikoAppBlank.csproj
dotnet run --project examples/Multiplatform/MikoAppBlank/MikoAppBlank.Desktop/MikoAppBlank.Desktop.csproj

# Simulator (device preview with settings panel)
dotnet run --project examples/Multiplatform/MikoAppBlank/MikoAppBlank.Simulator/MikoAppBlank.Simulator.csproj
```

### Cleaning build artifacts
```bash
dotnet clean
```

## Architecture Overview

Miko follows a layered architecture similar to browser rendering engines:

```
Razor Components (.razor files)
    ↓
Miko.Razor.Compiler (source generator)
    ↓
Application Layer (DOM construction + styling)
    ↓
DOM Layer (Element tree structure)
    ↓
Style System (StyleSheet + Selectors + Cascade)
    ↓
Layout Engine (Box Model + Layout Algorithms)
    ↓
Render Engine (Dirty regions + SkiaSharp painting)
    ↓
Platform Hosts (Desktop/Android/iOS)
```

### Solution Structure

The repository uses a single solution file:
- **miko.slnx**: Contains all projects (core libraries + desktop + mobile)

### Core Packages

1. **Miko** (`src/Miko/`)
   - Core rendering engine with platform abstractions
   - No platform-specific dependencies (no Silk.NET, no native UI frameworks)
   - Contains: DOM, styling, layout, rendering, fonts, events, routing, components

2. **Miko.Windowing** (`src/Miko.Windowing/`)
   - Desktop implementation using Silk.NET (Windows/Linux/macOS)
   - Provides window management, OpenGL context, and input handling

3. **Miko.Android** (`src/Miko.Android/`)
   - Android implementation with GLSurfaceView and touch input

4. **Miko.iOS** (`src/Miko.iOS/`)
   - iOS implementation with GLKView and touch input

5. **Miko.Simulator** (`src/Miko.Simulator/`)
   - Device simulator host for previewing mobile apps on desktop
   - Renders the app in a device-sized canvas with a settings panel

6. **Miko.Bootstrap** (`src/Miko.Bootstrap/`)
   - Bootstrap-style Razor component library
   - Uses custom Razor compiler via `_OverrideRazorSourceGenerator` target

7. **Miko.Ionic** (`src/Miko.Ionic/`)
   - Ionic-style Razor component library with iOS/Android mode support
   - Platform-aware theming using `IonicPlatform` and mode system

8. **Miko.Razor.Compiler** (`src/Miko.Razor.Compiler/`)
   - Custom Razor source generator (targets net9.0)
   - Compiles `.razor` components into native Miko DOM elements
   - Consumed as analyzer DLLs by Razor projects

### Core Module Structure

**Core** (`src/Miko/Core/`)
- `Element.cs`: Abstract base class for all DOM elements
- `DomElements/`: Concrete element implementations (Div, Heading, Button, Input, Image, Video, Table, List, etc.)
- `MikoEngine.cs`: Main engine coordinating layout, rendering, animations, and events
- `ElementState.cs`: Element states (Hover, Focus, Disabled, Active, etc.)

**Styling** (`src/Miko/Styling/`)
- `Style.cs`: Style properties (layout, box model, visual, positioning)
- `StyleSheet.cs`: Style rules and CSS-like selectors
- `StyleResolver.cs`: Style computation and cascade
- `ComputedStyle.cs`: Final computed styles after cascade
- `Selectors/`: Tag, Class, ID, Pseudo-class, Attribute, Combinator selectors
- `CssSelectorParser.cs`: Parses CSS selector strings
- `MediaRule.cs` & `MediaCondition.cs`: Media queries
- `CssObject.cs` & `TypedStyleBuilder.cs`: Type-safe style building

**Layout** (`src/Miko/Layout/`)
- `LayoutEngine.cs`: Orchestrates style computation and layout calculation
- `LayoutBox.cs`: Layout tree node with computed dimensions
- `BoxModel.cs`: CSS box model (content, padding, border, margin)
- `LayoutConstraints.cs`: Layout constraints passed from parent to child
- `LayoutAlgorithms/`: Block, Inline, Flex, Grid, and Table layout implementations
- `LayoutDispatcher.cs`: Routes layout calculations to appropriate algorithm

**Rendering** (`src/Miko/Rendering/`)
- `RenderEngine.cs`: Manages rendering with dirty region optimization
- `Painter.cs`: SkiaSharp drawing primitives
- `DirtyRegionManager.cs`: Tracks and merges dirty regions for incremental rendering

**Fonts** (`src/Miko/Fonts/`)
- `FontManager.cs`: Singleton font registry with TTF/OTF/WOFF/WOFF2 support
- `Woff2Decoder.cs`: WOFF2 font decompression
- `FontFallbackResolver.cs`: Multi-script font fallback chain
- `TextRun.cs`: Text segmentation by script and font

**Events** (`src/Miko/Events/`)
- `EventDispatcher.cs`: DOM-style event dispatch with capture and bubbling
- `EventTypes.cs`: Event type constants
- `EventArgs.cs`: Mouse, Keyboard, Focus, Change, Scroll event args

**Platform** (`src/Miko/Platform/`)
- `MikoInput.cs`: Platform-agnostic input enums (MikoKey, MikoKeyModifiers)
- `MikoInteractionController.cs`: Platform-agnostic interaction logic (hit testing, focus, click, scroll, text editing)
- `MikoDispatcher.cs` & `MikoSynchronizationContext.cs`: Thread marshaling
- `Resources/`: Image loading abstractions (`IImageLoader`, `ResourceManager`)
- `Video/`: Video playback abstractions (`IVideoBackend`, `IVideoSession`, `IVideoFrameSource`)

**Components** (`src/Miko/Components/`)
- `MikoComponent.cs`: Base class for Razor components
- `RenderFragment.cs`: Delegate type for rendering child content
- `IComponent.cs` & `LayoutComponentBase.cs`: Component lifecycle
- `EventCallback.cs`: Async event handlers
- Attributes: `[Parameter]`, `[CascadingParameter]`, `[Route]`, `[Layout]`, `[Inject]`
- `CompilerServices/RuntimeHelpers.cs`: Runtime support for generated code

**Routing** (`src/Miko/Routing/`)
- `Router.cs`: Client-side routing with pattern matching
- `NavigationManager.cs`: Programmatic navigation
- `RouteData.cs`: Route parameters and values

**Animation** (`src/Miko/Animation/`)
- `AnimationManager.cs`: Manages keyframe animations and transitions
- `KeyframeAnimation.cs`: CSS-like keyframe animations
- `Transition.cs` & `TransitionBuilder.cs`: Property transitions
- `Transform.cs`: 2D transforms (translate, rotate, scale)
- `EasingFunctions.cs`: Easing curves

**Hosting** (`src/Miko/Hosting/`)
- `MikoAppBuilder.cs`: Fluent builder for configuring apps with DI
- `MikoAppContext.cs`: Platform-agnostic app context consumed by platform hosts
- `MikoAppOptions.cs`: Configuration options (title, size, root component, stylesheets, routes)
- `HotReloadService.cs`: Hot reload support for Razor components

**Common** (`src/Miko/Common/`)
- `Length.cs`: CSS-like length units (px, %, auto)
- `Color.cs`: RGBA color with hex/named color parsing
- `RectF.cs`: Rectangle geometry
- `Padding.cs`, `Margin.cs`, `Border.cs`, `BorderRadius.cs`: Box model types
- `BoxShadow.cs`: Shadow effects
- `SafeAreaInsets.cs`: Safe area for notched devices
- Enums: `Display`, `Position`, `FlexDirection`, `JustifyContent`, `AlignItems`, etc.

**Utils** (`src/Miko/Utils/`)
- `TreeTraversal.cs`: DOM tree traversal utilities
- `TextMeasurer.cs`: Text measurement with SkiaSharp
- `GeometryUtils.cs`: Geometric calculations

### Platform Abstraction Model

The core `Miko` package contains only platform abstractions. Shared UI projects reference only `Miko` and produce a `MikoAppContext` via `MikoAppBuilder.Build()`. Platform-specific startup projects then drive the rendering:

```csharp
// Shared UI project (references only Miko)
public static class App
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("My App");
        builder.UseSize(1024, 768);
        builder.UseGeneratedRoutes();
        return builder.Build();
    }
}

// Desktop startup (references Miko.Windowing)
App.CreateContext().RunDesktop();

// Android Activity (references Miko.Android)
SetContentView(MikoAndroidApp.CreateView(this, App.CreateContext));

// iOS AppDelegate (references Miko.iOS)
Window.RootViewController = new MikoViewController(App.CreateContext());

// Simulator (references Miko.Simulator)
App.CreateContext().RunSimulator();
```

Platform hosts provide:
- Window/view management
- OpenGL/Metal context creation
- Native input event translation to `MikoKey`/pointer events
- Forwarding events to `MikoInteractionController`

## Rendering Pipeline

```
1. DOM Construction   →  Razor components build Element tree programmatically
2. Style Computation  →  StyleResolver matches selectors, computes cascade
3. Layout Tree Build  →  Filter elements by display property
4. Layout Calculation →  Constraints flow down (parent→child), sizes flow up (child→parent)
5. Painting           →  RenderEngine draws to SKCanvas (dirty region optimized)
```

### Style Cascade

Selector specificity (lowest to highest): **Tag → Class → ID → inline style**

Pseudo-classes: `:hover`, `:focus`, `:active`, `:disabled`, `:checked`, `:first-child`, `:last-child`, `:nth-child()`

Pseudo-elements: `::before`, `::after`

Combinators: descendant (` `), child (`>`), adjacent sibling (`+`), general sibling (`~`)

### Layout Algorithms

- **BlockLayout**: Vertical stacking, width fills parent, height fits content
- **InlineLayout**: Horizontal flow with automatic line wrapping
- **FlexLayout**: Flexbox with `flex-direction`, `justify-content`, `align-items`, `flex-grow`, `flex-shrink`
- **TableLayout**: Table rows and cells with column width distribution

### Dirty Marking and Incremental Rendering

Elements track `IsDirty` flag. When an element changes (style, text, children), it's marked dirty. `RenderEngine` repaints only dirty regions, merging overlapping/adjacent rectangles.

- Full render: `MikoEngine.Render(canvas)`
- Incremental: `MikoEngine.InvalidateElement(element); MikoEngine.Update(canvas)`

## Development Guidelines

### Working with Elements

- All elements inherit from `Element` base class
- Elements must implement `TagName` property
- Use `AddChild()`/`RemoveChild()` to maintain parent-child relationships (automatically sets `IsDirty`)
- Element lookup: `FindById()`, `FindByClass()`, `FindByTagName()`
- State flags: `ElementState.Hover`, `ElementState.Focus`, `ElementState.Active`, `ElementState.Disabled`

### Working with Styles

- Style properties are nullable to support cascade (null = not set)
- Use `Style.Merge()` for cascading styles
- `ComputedStyle` contains final resolved values after cascade
- Media queries: `MediaRule` with `MediaCondition` for responsive styles
- Type-safe style building: `TypedStyleBuilder` and `CssObject`

### Working with Razor Components

Razor projects using Miko's custom compiler must include the `_OverrideRazorSourceGenerator` MSBuild target to replace the default Razor compiler:

```xml
<Target Name="_OverrideRazorSourceGenerator" AfterTargets="_PrepareRazorSourceGenerators">
  <PropertyGroup>
    <_CustomRazorGeneratorDir>$(MSBuildThisFileDirectory)..\Miko.Razor.Compiler\bin\$(Configuration)\net9.0\</_CustomRazorGeneratorDir>
  </PropertyGroup>
  <ItemGroup>
    <Analyzer Remove="@(_RazorAnalyzer)" />
    <_RazorAnalyzer Remove="@(_RazorAnalyzer)" />
  </ItemGroup>
  <ItemGroup>
    <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.CodeAnalysis.Razor.Compiler.dll" />
    <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.AspNetCore.Razor.Utilities.Shared.dll" />
    <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.Extensions.ObjectPool.dll" />
    <Analyzer Include="@(_RazorAnalyzer)" />
  </ItemGroup>
</Target>
```

Components inherit from `MikoComponent` or `LayoutComponentBase` and use standard Razor syntax with `@page` directives for routing.

### Working with Ionic Components

Ionic components use a platform-aware mode system:
- Platform is detected from `IPlatformInfo` (injected singleton)
- Components inherit from `IonicComponentBase` which provides `Mode` property
- Mode flows from platform to components: iOS devices → `"ios"` mode, others → `"md"` (Material Design)
- Components use `ClassMapper` utility to build CSS classes: `.AddIf("ion-color-primary", () => Color == "primary")`
- Segment buttons use an indicator overlay (MD: underline bar, iOS: pill) for checked state

See memory files for mode scoping and segment indicator patterns.

### Testing Conventions

- Test files mirror source structure: `tests/Miko.Tests/[Module]/[Class]Tests.cs`
- Use Shouldly for assertions: `result.ShouldBe(expected)`, `element.HasClass("test").ShouldBeTrue()`
- Test naming: descriptive method names with `Should_` prefix explaining the scenario
- xUnit attributes: `[Fact]` for single test, `[Theory]` with `[InlineData]` for parameterized tests
- Unit tests focus on individual components (selectors, box model, layout algorithms, etc.)

### Font Management

`FontManager` is a singleton supporting custom font registration:

```csharp
FontManager.Instance.RegisterFont("MyFont", "/path/to/font.woff2");
```

Built-in fallback chain: Arial → Segoe UI → Microsoft YaHei → SimSun → MS Gothic → Malgun Gothic

### Video Playback

Video is implemented via platform-injected `IVideoBackend`:
- `VideoElement` in DOM triggers video session creation
- Sessions are cached in `MikoEngine._videoSessions` and reused across rebuilds
- Platform hosts inject backend via `MikoEngine.VideoBackend`
- Frame sources provide `IVideoFrameSource` for zero-copy GPU texture wrapping

### Image Loading

Images are loaded asynchronously via `IImageLoader`:
- Default implementation: `ResourceManager` (embedded resources and file paths)
- Platform hosts inject via `MikoEngine.ImageLoader`
- `ImageElement` tracks loading state and supports placeholder images
- Loaded bitmaps are cached in element's `Bitmap` property

## Common Patterns

### Creating a Razor app

```csharp
var builder = MikoAppBuilder.CreateDefault();
builder.UseTitle("My App");
builder.UseSize(1024, 768);
builder.UseGeneratedRoutes();                    // Auto-discover @page routes
builder.UseDefaultLayout<MainLayout>();
builder.EnableHotReload();
builder.AddStyleSheet(myStyleSheet);

var app = builder.Build();
app.RunDesktop();  // or RunSimulator(), or pass to platform host
```

### Building DOM programmatically (without Razor)

```csharp
var styleSheet = new StyleSheet();
styleSheet.AddRule(new ClassSelector("container"), new Style
{
    Display = Display.Flex,
    FlexDirection = FlexDirection.Column,
    Padding = new Padding(Length.Px(20)),
    BackgroundColor = Color.FromRgb(245, 245, 245)
});

var root = new DivElement { Class = "container" };
root.AddChild(new H1Element { TextContent = "Hello Miko" });
root.AddChild(new ParagraphElement { TextContent = "Lightweight rendering" });

using var surface = SKSurface.Create(new SKImageInfo(800, 600));
var canvas = surface.Canvas;

var engine = new MikoEngine();
engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);
```

### Incremental updates

```csharp
// Modify element and mark dirty
element.TextContent = "Updated text";
engine.InvalidateElement(element);

// Incremental render (only dirty regions)
engine.Update(canvas);
```

### Event handling

```csharp
var button = new ButtonElement { TextContent = "Click me" };
button.OnClick = (sender, args) =>
{
    Console.WriteLine($"Clicked at ({args.X}, {args.Y})");
};

// Or use AddEventListener
button.AddEventListener<MouseEventArgs>("click", (sender, args) =>
{
    // Custom handler
});
```

### Transitions and animations

```csharp
// Property transition
element.Style = new Style
{
    Transition = TransitionBuilder.Create()
        .Property("background-color")
        .Duration(300)
        .Easing(EasingFunction.EaseInOut)
        .Build()
};

// Keyframe animation (requires AnimationManager from DI)
var animation = new KeyframeAnimation
{
    Duration = 1000,
    Keyframes = new Dictionary<float, Style>
    {
        { 0f, new Style { Opacity = 0 } },
        { 1f, new Style { Opacity = 1 } }
    }
};
animationManager.PlayAnimation(element, animation);
```

## Important Notes

- This is a .NET 10.0 project (requires appropriate SDK)
- SkiaSharp resources should be properly disposed (use `using` statements)
- The project uses nullable reference types — respect null annotations
- Layout calculations use floats — be aware of floating-point precision
- Style cascade: null means "not set", not a default value
- `IsDirty` is automatically set on `AddChild`/`RemoveChild`/state changes
- The Razor compiler targets net9.0 and is consumed only as analyzer DLLs
- Safe area insets are opt-in via `env()` function, never applied as root viewport insets (see memory)
- Documentation site under `docs/` (VitePress) contains the usage guide
- `DEVELOPMENT.md` (in Chinese) has developer-oriented walkthrough
