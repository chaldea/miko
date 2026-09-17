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

Alongside that pipeline, `Miko.Native` provides a parallel capability axis: the app injects
capability interfaces (camera, clipboard, filesystem, …) and the same platform hosts supply the
implementations.

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

8. **Miko.Native** (`src/Miko.Native/`)
   - Cross-platform native capability interfaces (17 areas modelled on Ionic Capacitor v8)
   - Pure abstractions with zero platform dependencies; implementations live in the platform packages

9. **Miko.Razor.Compiler** (`src/Miko.Razor.Compiler/`)
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

Cascade order: **cascade layer → selector specificity → source order → inline style last (highest)**.

`StyleSheet.Layer` (default 0) mirrors CSS `@layer`: rules from a higher-layer sheet always beat lower-layer rules regardless of specificity. Component libraries sit below the app layer (`Miko.Ionic` uses layer -1, `IonicStyleSheetFactory.CascadeLayer`) so app rules override component host styles — mirroring how outer-document rules beat shadow-tree `:host` rules in the browser (ISSUE-107).

Selector specificity within a layer (lowest to highest): **Tag → Class → ID → inline style**

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
- Platform hosts register their backend in DI; it reaches the engine by constructor injection.
  The core default is `NullVideoBackend`, which creates no sessions — `<video>` then renders
  background/poster only.
- Frame sources provide `IVideoFrameSource` for zero-copy GPU texture wrapping

**Backends use the OS decoder, never a bundled one.** Each platform registers a system-native
backend; none of them ship third-party native binaries:

| Platform | Registration | Decoder |
|---|---|---|
| Windows | `UseSystemVideo()` | Media Foundation (D3D11VA) |
| Linux | `UseSystemVideo()` | GStreamer (VAAPI) |
| macOS | `UseSystemVideo()` | AVFoundation (VideoToolbox) |
| Android | `UseAndroidVideo()` | MediaExtractor + MediaCodec |
| iOS | `UseIosVideo()` | AVPlayer + AVPlayerItemVideoOutput |

`Miko.Video.FFmpeg` is an **opt-in extension** (`UseFFmpegVideo()`) for formats the system
decoder does not cover; it pulls in >80MB of native FFmpeg and must never be a platform default.

Shared plumbing in `src/Miko/Platform/Video/`:
- `VideoFrameBuffer` — one frame as either GPU texture handles (zero-copy) or CPU planes (fallback)
- `VideoFrameSourceBase` — the wrap logic both paths share; backends only fill `VideoFrameBuffer`
- `Nv12FrameComposer` — NV12→RGB via an `SKRuntimeEffect` (SkSL) shader. **Required** because
  SkiaSharp 3.119.1 exports no `SKYUVAInfo` / multi-plane `SKImage.FromTextures`, so Skia cannot
  convert YUV itself; Y is wrapped as `R8Unorm`, UV as `Rg88`, and the shader does the matrix.
- `VideoSourceDescriptor.ResolveForBackend()` — normalizes relative paths against
  `AppContext.BaseDirectory`. System decoders resolve them against the process CWD, which differs
  when launched from an IDE; skipping this makes `<video>` fail where a sibling `<img>` loads.

NV12 gotcha: hardware decoders align the **Y plane row count** up to a macroblock boundary
(e.g. 180→192), so the UV plane does not start at `stride * height`. Deriving the offset from the
buffer length is what keeps a green bar off the top of the picture (U=V=0 reads as pure green).

### Native Capabilities

`Miko.Native` defines 17 capability interfaces (app, browser, camera, clipboard, device,
filesystem, geolocation, haptics, keyboard, local notifications, motion, network, push
notifications, screen reader, splash screen, status bar, toast), modelled on Ionic Capacitor v8.
Apps depend only on the interfaces; platform packages supply the implementations.

Four rules govern the layer:

1. **`Null*` defaults, registered with `TryAdd`.** `AddMikoNative()` registers a `Null*`
   implementation for every interface, so `[Inject] ICameraService` always resolves. Platform
   packages override with `Replace` (`ReplaceNative<TService, TImpl>()`), so the real
   implementation wins regardless of registration order — same ISSUE-129 contract as
   `IImageLoader` / `IVideoBackend`.

2. **Unsupported means throw.** A capability the platform cannot provide throws
   `PlatformNotSupportedException` (via `NativeServiceBase.Unsupported()`), never a silently
   empty or fabricated result. Event subscription stays harmless — only method calls fail.

3. **`INativeHostContext` carries the host, lazily.** `App.CreateContext()` builds the DI
   container *before* any platform host exists — `MainActivity.OnCreate` creates the view only
   after it has the `MikoAppContext`. So Android/iOS services cannot receive `Activity` /
   `UIViewController` at construction. Hosts (`MikoSurfaceView`, `MikoViewController`,
   `SilkDesktopHost`, `SimulatorHost`) call `Attach(this)` as they are built; services read the
   host at call time via `RequireHost<T>()`. A missing host is an `InvalidOperationException`
   (wiring bug), distinct from `PlatformNotSupportedException` (capability absent).

4. **Subscriptions must be releasable.** Services register OS callbacks only while they have
   subscribers and unregister on the last removal (see `DesktopNetworkService`,
   `AndroidMotionService`, `IosKeyboardService`). Watch-style APIs return an id for explicit
   teardown; `INativeListener.RemoveAsync()` is idempotent.

Filesystem read/write is identical on all three platforms (`System.IO`); only the directory
mapping differs. That logic lives once in `NativeFileOperations` — platforms supply a path
resolver and nothing else. The simulator answers mobile-only capabilities with recognisable
sample data (`miko-simulator://` URIs, `IsVirtual = true`) so a call chain can be exercised on
the desktop, and reuses the real desktop implementations where they genuinely apply.

Android camera/gallery calls need the host Activity to forward `OnActivityResult` to
`MikoAndroidApp.HandleActivityResult`; without it those calls wait forever. Permissions,
manifests, `Info.plist` entries, and FCM/APNS configuration are app-side responsibilities — the
interfaces expose only permission state and business parameters.

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

var engine = new MikoEngineBuilder().Build();
engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);
```

### Multiple engine instances and isolation

Engines are always constructed through a DI container — never `new MikoEngine(...)` directly:

- `new MikoEngineBuilder().Build()` — a standalone engine (DevTools window, simulator settings
  panel, unit tests).
- `MikoAppBuilder` — a full app (routing, hot reload, interaction controller). It reuses the same
  `Services.AddMikoEngine()` registration, so both paths yield identically-wired engines.

One container = one engine. Each instance owns its own **mutation version** (`MikoEngine.Mutations`),
layout cache, dirty regions, animation state, and video/image sessions, so several engines can run in
one process without interfering (ISSUE-129).

Elements are attributed to an engine via `Element.Owner`, which the engine assigns by walking the
tree in `Initialize` and again each frame in `Render`/`Update`. Elements not yet attached to any
engine have no owner and their mutations are silently uncounted — nothing is observing them.
`RemoveChild` clears the owner of the detached subtree, so elements outside the DOM stop generating
invalidation work for the engine they used to belong to.

`Element.Children` is an `ElementCollection`, not a bare `List<Element>`. Every write to it (`Add`,
`Insert`, `Clear`, `Remove`, `RemoveAt`, `RemoveAll`, and the indexer) sets the child's parent
reference, propagates engine ownership, and bumps the mutation version. That accounting has to live
in the collection because `Children` is public: callers — including collection initializers
(`Children = { ... }`), the `TextContent` setter, and component re-renders — write to it directly,
bypassing `AddChild`. If those writes went unrecorded, a structural change after an idle frame would
not invalidate the layout cache and the added/removed/replaced nodes simply would not appear.
Collection initializers still work, since the type exposes a public `Add`.

**Registration is `TryAdd`-based.** `AddMikoEngine` registers every default with `TryAdd`, so it is
idempotent *and* never overrides a registration the caller already made — a custom `IImageLoader`,
`ISyntaxHighlighter`, or `IVideoBackend` wins regardless of whether it was registered before or
after. This matters because `MikoEngineBuilder.ConfigureServices` runs *before* `Build()` calls
`AddMikoEngine()`; with plain `AddSingleton`, the defaults would be registered last and Microsoft DI
resolves a single service to the last registration, silently discarding the customization.

Those three optional services are plain constructor parameters on `MikoEngine` — each has an
in-core default (`ResourceManager`, `SyntaxHighlighter`, and `NullVideoBackend`), so the engine
needs no `IServiceProvider` for optional resolution. `NullVideoBackend` creates no sessions, which
is exactly the old "no backend registered" behavior: `<video>` renders background/poster only.

**Shared across the whole process (deliberately):** `FontManager.Instance` (font registry + glyph
cache) and `TextMeasurer`'s measurement caches. These are content-addressed (keys include font
family, size, and text), so sharing is both correct and beneficial. Note that `FontManager.RegisterFont`
is a global side effect that changes text metrics — register fonts before any engine renders.

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
