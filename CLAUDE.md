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

### Packing (local NuGet feed)
```bash
# Writes every package to <repo>/packages (Directory.Build.props sets PackageOutputPath; git-ignored)
dotnet pack miko.slnx
dotnet pack templates/Miko.Templates.csproj
```
The in-repo templates' `nuget.config` restores `Miko*` from `packages/`. `Directory.Build.props` sets
`VersionPrefix=1.0.0` + `VersionSuffix=dev`, so every local pack is `1.0.0-dev` (below any
`1.0.0-preview.N`) and the templates reference `1.0.0-dev`; the publish workflow passes `-p:Version=`
and rewrites the template references to the released version. NuGet caches by version, so after
re-packing delete the stale copies (`rm -rf ~/.nuget/packages/miko*`) before restoring again.
CI passes `-o`, which overrides the default output path.

### Template end-to-end check
```bash
# Packs everything into a private feed, scaffolds every template/layout and builds every head in Release
dotnet run --project tools/Miko.TemplateE2E
dotnet run --project tools/Miko.TemplateE2E -- --layouts blank,full --heads Desktop,Android --parallel 4
```
Not part of PR validation: run it after changing the templates or the packaging, and before a release.
Builds run in parallel, each in a scaffold of its own (the heads of one app share its library project),
with one log per build under `<work dir>/logs/`; heads whose workload is missing are skipped. The
default parallelism is min(cores / 2, GB of RAM / 3), at most 6: a mobile Release build takes 2.5–3 GB.
Passing apps are deleted as they finish (a full run is ~7 GB) unless `--keep`.

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
   - Provides window management, GPU presentation, and input handling
   - `SilkWindowGraphics` picks the presentation backend: OpenGL on Windows/Linux, Metal
     (`CAMetalLayer` via Objective-C interop) on macOS — the same Skia Metal path as iOS
     (ISSUE-147). `MIKO_GRAPHICS_BACKEND=opengl` forces OpenGL on macOS for comparison
   - `SilkWindowThreadRunner`: native events on the main thread, rendering on a dedicated thread. The
     first `Run()` loop is also the process-wide `SilkEventLoop`. Windows opened later (DevTools)
     implement `ISilkEventLoopWindow` and attach via `SilkEventLoop.TryAttach()` — AppKit only allows
     the main thread to create windows and pump their events, so a window running its own loop on a
     background thread hangs on macOS (ISSUE-148)

3. **Miko.Android** (`src/Miko.Android/`)
   - Android implementation with GLSurfaceView and touch input

4. **Miko.iOS** (`src/Miko.iOS/`)
   - iOS implementation with a Metal `MTKView` (`MikoMetalView`) and touch input

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
   - Custom Razor source generator (targets net10.0)
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
- `Virtualization/`: `Virtualize<TItem>` (ISSUE-151), its slots, size index and items-provider types

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

**Text** (`src/Miko/Text/`, ISSUE-157)
- `Bidi/`: UAX #9 (`BidiParagraph`, `UnicodeBidi` facade) over tables generated by `tools/Miko.UnicodeGen`
- `TextComplexity.cs`: simple (nominal glyphs, legacy path) vs needs-shaping
- `HarfBuzzShaper.cs`, `TextShaping.cs`, `ShapedText.cs`: per-line fallback → bidi → itemize → shape → visual order

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

Pseudo-classes: `:hover`, `:focus`, `:focus-within`, `:active`, `:disabled`, `:enabled`, `:checked`, `:first-child`, `:last-child`, `:first-of-type`, `:last-of-type`, `:empty`, `:nth-child()` / `:nth-last-child()` (with `of S`), `:nth-of-type()` / `:nth-last-of-type()`, and the selector-list functions `:is()`, `:where()`, `:not()`, `:has()` (ISSUE-158)

Pseudo-elements: `::before`, `::after`, `::placeholder`, `::range-thumb` / `::range-track` / `::range-progress`

Selector invariants (ISSUE-158):
- `:has()` arguments are `RelativeSelector`s matched **left to right** from the anchor (`MatchesFrom`), so
  selectors hold no mutable "current scope" state and one sheet is safe across engines on many threads.
- `HoverRelevance` must look inside functional arguments: `.card:has(.btn:hover)` hovers `.btn`, not
  `.card`; `:is(.a:hover .b)` hovers ancestor `.a`. A new functional pseudo-class needs a case in
  `ContainsHover` / `CollectNested`, or hover changes on the real target are skipped (ISSUE-104 gating).
  `:not(:hover)` must stay scoped to its compound (no universal pattern).
- `::placeholder` has no box: `StyleResolver.ResolvePseudoElement` cascades it (layer → specificity → order,
  UA gray below author rules, inherits from the field) into `LayoutBox.PlaceholderStyle`. Paths that carry a
  `ComputedStyle` to a new box (`TryReuseStyles`, `ComponentBase.TransferLayoutBox`) must carry it too.
- Pseudo-elements inside functional arguments, empty arguments, and stray characters throw `ArgumentException`
  (an unknown character used to spin `ParseCompound` forever).

Combinators: descendant (` `), child (`>`), adjacent sibling (`+`), general sibling (`~`)

### Layout Algorithms

- **BlockLayout**: Vertical stacking, width fills parent, height fits content
- **InlineLayout**: Horizontal flow with automatic line wrapping
- **FlexLayout**: Flexbox with `flex-direction`, `justify-content`, `align-items`, `flex-grow`, `flex-shrink`
- **GridLayout**: CSS Grid — track lists (`GridTrackSize`: fixed / fr / auto / min- & max-content /
  `minmax` / `fit-content` / `repeat` incl. auto-fill & auto-fit / line names), `GridTemplateAreas`,
  `GridLine` placement (numbers, names, areas, spans)
- **TableLayout**: Table rows and cells with column width distribution

### Sticky positioning and grid templates (ISSUE-159)

- **Sticky is applied at paint time, not layout time.** Scrolling never relayouts (the layout cache
  stays hot), so `LayoutEngine.ApplyPositioning` only registers sticky boxes — with their nearest
  scroll container (any `Overflow` other than `Visible`) and containing block — on the root's
  `LayoutBox.StickyBoxes`. `StickyPositioning.Update(root)` re-applies the offsets from the current
  scroll position before every `RenderEngine.Render` / `RenderLayer` / `RenderDirty` and every
  `MikoEngine.HitTest`. Offsets are incremental (`LayoutBox.StickyOffsetX/Y`), so one cached tree
  can be updated every frame; fixed descendants are not moved.
- Sticky children paint after all their siblings (`RenderEngine.RenderChildren`) and are hit-tested
  first (`HitTestBox`) — twins. Sticky always establishes a stacking context (both
  `EstablishesStackingContext` twins). A containing block that *is* the scroll container constrains
  by its whole scrollable area, not its one-screen content box.
- `GridLine` must stay 8 bytes with no references (four slots on both `Style` and `ComputedStyle`,
  see `StyleStorageSizeTests`): line names are interned in `GridLineNames` (process-wide,
  content-addressed).
- Track sizing follows CSS Grid §11 (`GridLayout.TrackSizer`); `1fr` is `minmax(auto, 1fr)`, so it
  needs min-content contributions, measured by laying the item out at `MinContentProbeWidth` (an
  available width of 0 means "indefinite"). `MinWidth` / `MinHeight` compute to 0 (no `auto`), so
  only a positive min-size counts as explicit. Placement follows §8.3: an end line alone is a
  definite placement, and lines outside the explicit grid add implicit tracks on either side.

### Viewport observers and `Virtualize` (ISSUE-151)

- **Settle pass before paint.** `LayoutEngine.BuildLayoutTree` collects `IViewportObserver` elements
  (also when they are layout-transparent `FragmentElement`s; never under `display: none`) onto
  `LayoutBox.ViewportObserverSet`. `MikoEngine.Render` = `PrepareFrameLayout()` (the old fast/slow
  layout path) + `SettleViewportObservers(relayout)` + paint: observers get
  `ViewportObservers.Observe()` (viewport ∩ every clipping ancestor, in unscrolled layout coordinates,
  stopping at a `fixed` ancestor); one that mutates the DOM makes the engine relayout and notify again,
  up to 8 passes (then `_repaintRequested`). `Update` and `Initialize` settle too. A new paint entry
  point must settle before painting, or a scrolled frame shows blank spacer.
- **Structural scroll restore treats two observers as the same content** (`IsSamePresentedContent`):
  their children change with every window move; without it a row changing shape reset the scroller.
- **`Virtualize` renders** a `VirtualizeHostElement` (fragment) = spacer + one `VirtualizeSlot` per item
  + spacer, keyed (`VirtualizeSpacerKey`, `VirtualizeSlotKey`). A slot returns its `CurrentRoot` while
  the list's generation is unchanged, so items that stay in the window keep elements and instances;
  the generation bumps on every parent render and `RefreshDataAsync`. Self renders call `RenderNow`
  with `_selfRender` set (`OnParametersSet` then skips data sync).
- **Measurement**: slot starts are the leading spacer's margin-box bottom, then each later slot's first
  laid-out element's margin-box top, then the trailing spacer's margin-box top; height = next start −
  start (`ItemSizeIndex`: Fenwick trees, unmeasured = mean). After measuring, spacer heights are
  rewritten in place (`SyncSpacers`) — otherwise the scroll range is off by the estimate change × the
  unrendered count.
- **Scroll anchoring**: the first *rendered* item on screen (else the model position of the first
  visible index) is recorded every notification; after a new layout the scroller moves by how far it
  moved, starting from the *previous* layout box's offset (the engine clamped the new one).
  `ItemKey` re-keys on parent render (tail-only when items were appended/removed at the end) and
  shifts the window with the anchor on inserts above.

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
    <_CustomRazorGeneratorDir>$(MSBuildThisFileDirectory)..\Miko.Razor.Compiler\bin\$(Configuration)\net10.0\</_CustomRazorGeneratorDir>
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

`Miko.Razor.Compiler.props` (the NuGet package's build file) locates the compiler under
`tools/<the compiler's own TFM>/` (`_MikoRazorCompilerToolFramework`), never the consumer's
`$(TargetFramework)`, so platform projects (`net10.0-android`, `net10.0-ios`) with `.razor` files
build from the package (ISSUE-154). The compiler csproj fails `pack` if the two drift apart.

### Component identity, reuse and render batching (ISSUE-154)

A parent re-render **reuses** each nested component whose identity is unchanged instead of
recreating it, as in Blazor. Identity (`ComponentIdentity`) = scope (enclosing render-fragment call
sites and keyed elements) + type + call-site sequence + `@key`, or for unkeyed components the
ordinal among same-call-site components. The compiler emits `OpenComponent<T>(seq, key)`; hand-written
`OpenComponent<T>()` uses seq -1 (position only). `RenderTreeBuilder` takes the owner's previous
`ComponentRegistry` and claims instances from it; duplicate keys throw.

- `OnInitialized` runs once per instance; parameters are reassigned and `OnParametersSet` runs on every
  parent render; unassigned parameters keep their value. Components must not register (events,
  registries, lists) in `OnParametersSet` without making it idempotent, and must not derive state once
  from a parameter in `OnInitialized` if a parent may change it later.
- Disposal is element-driven: `ComponentBase.StampRoot` makes an element's `DisposeCallback` dispose
  only components whose root is *still* that element, so a reused instance (which has moved to a new
  root) survives the walk over the discarded subtree. Root-sharing chains (`_rootChild`/`_rootOwner`,
  a component whose markup is one component) are kept in step on self re-renders.
- `RenderBatch`: `StateHasChanged` while an `EventCallback` or a controller-dispatched event runs is
  queued and flushed after the handler's synchronous part, skipping components rendered since. Without
  it, the first callback of a handler would re-render the parent synchronously and overwrite values the
  child has not reported yet (`IonCheckbox`: `IndeterminateChanged` then `CheckedChanged`).
- A component re-rendering itself keeps the root classes an ancestor's `Build()` post-pass added
  (`ExternalRootClasses`), e.g. `IonItem`'s `button-small` on a child `IonButton`.
- The root of a parentless component is preserved on re-render: children are moved in as built (text
  order intact) and the root adopts the new root's full state (`Element.AdoptStateFrom`, with
  subclass properties copied by the generated `ElementAttributeAccessor.CopyDeclaredProperties`).
- `@key` on an element scopes the components inside it and pairs runtime state (focus, caret,
  layout box) by key when carried across a re-render (`ComponentBase.PairChildren`).

### Working with Ionic Components

Ionic components use a platform-aware mode system:
- Platform is detected from `IPlatformInfo` (injected singleton)
- Components inherit from `IonicComponentBase` which provides `Mode` property
- Mode flows from platform to components: iOS devices → `"ios"` mode, others → `"md"` (Material Design)
- Components use `ClassMapper` utility to build CSS classes: `.AddIf("ion-color-primary", () => Color == "primary")`
- Segment buttons use an indicator overlay (MD: underline bar, iOS: pill) for checked state
- `IonDatetime` ports Ionic's datetime utils one to one (`DatetimeUtils` / `DatetimeData`, values
  as partial `DatetimeParts`, never `DateTime`). Its texts come from `DatetimeLocale`, which derives
  `Intl`-style field sets from each culture's .NET patterns (no skeleton API exists in .NET).
- Content that must close its overlay (Ionic `el.closest('ion-modal, ion-popover')`) looks it up
  with `IonOverlayHosts.FindEnclosing(root)`; `IonModal` / `IonPopover` register their host element
  through `@ref`. Do not cascade a context from an overlay instead: a `CascadingValue` inserts a
  `FragmentElement` between the wrapper and its content, which breaks the overlay DOM contract.

See memory files for mode scoping and segment indicator patterns.

### Documentation

`docs/` is the single source of user documentation — the VitePress site **and** the reference
shipped to AI agents in scaffolded apps (`templates/Miko.Templates.csproj` packs `docs/**/*.md`
into `.miko/docs/`). `agent/` holds only agent-specific files: entry files, `.miko/rules.md`, and
the `.miko/llms.txt` index. Links in shipped pages must be relative `.md` links.

Pages mix hand-written prose with generated regions filled by `tools/Miko.DocGen`
(`dotnet run --project tools/Miko.DocGen`; it also runs when the solution builds locally, never
on CI). Never edit between the markers:

- `<!-- miko:api IonFoo … -->` — parameter / event / slot tables from reflection over
  `Miko.Ionic` plus its XML doc comments: a parameter's `///` summary **is** its documentation.
- `<!-- miko:tokens Foo -->` — `IonicTheme.Foo` with md and ios defaults.
- `<!-- miko:type … -->` — options / event-args / controller types.
- `<!-- miko:example examples/x/Y.razor -->` — inlines an example; the `.razor` files under
  `docs/components/**/examples/` are compiled and rendered by `tests/Miko.Docs.Tests`.
- `<!-- miko:elements -->` — the Razor compiler's tag / attribute table (`MikoElements`).
- `<!-- miko:component-index … -->`, `<!-- miko:include … -->` — indexes and shared fragments.

`tests/Miko.Docs.Tests` fails when generated regions are stale, a public Ionic component is on
no page, a parameter lacks a `///` summary, an example is unused or does not render, a link does
not resolve (on the site or inside a scaffolded app), or the packed template is missing docs.

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

### Text shaping and bidi (ISSUE-157)

- **Two paths.** Text that `TextComplexity.IsSimple` accepts (Latin/Greek/Cyrillic/CJK without
  combining marks) and is laid out LTR keeps the original glyph-for-character measuring and drawing,
  so existing metrics never move. Everything else — RTL paragraphs, complex scripts, marks, joiners,
  anything outside the BMP — goes through `TextShaping.Shape` (cached like the other text caches,
  cleared with them by `FontManager`). Inside shaped text, simple LTR items still use nominal
  advances, so a Latin word measures the same alone and inside an Arabic sentence.
- **HarfBuzz faces** are built from `SKTypeface.TryGetTableData` per table (never the whole font
  file, Apple Color Emoji is ~180 MB). A `Blob` returned from the table delegate is owned by
  HarfBuzz: do not dispose it. Shapers are disposed under their own lock on cache clear.
- **Bidi data** is generated (`dotnet run --project tools/Miko.UnicodeGen`) and pinned to Unicode
  15.1, the version HarfBuzzSharp 8.3 uses for scripts and mirroring — bump both together. The repo
  carries subsets of the conformance files; `MIKO_UCD_DIR=<dir with BidiTest.txt and
  BidiCharacterTest.txt>` runs all ~860k cases.
- **Inline layout**: levels are resolved per paragraph (forced breaks and block-in-inline end one),
  a word whose characters get different levels is split into pieces with `NoBreakBefore`, lines are
  broken in logical order and reordered with L2 when positioned. `TextLineFragment.IsRtl` marks a
  fragment shaped RTL and anchored right; its `Text` stays in logical order. Inline-box edges take
  the level of their adjacent content, and at an odd level the start edge carries the box's right
  side. LTR runs without RTL characters skip all of this.
- `ComputedStyle.TextAlign` defaults to `TextAlign.Start`; resolve it with `ResolveFor(direction)`
  wherever a physical side is needed. `dir` is a presentational hint (UA level, below author rules).
- Flex rows under `rtl` reverse like `row-reverse` (and `row-reverse` un-reverses); the absolute
  `start`/`end` keywords flip with the direction alone. Physical margins and paddings never mirror.
- A layout's `@Body` survives the layout's own re-render: `LayoutComponentBase` keeps the page
  *component* (whose `CurrentRoot` is its latest generation) and re-renders it with the layout.
  It must not keep a page *element* — that is the ISSUE-142 generation leak.

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
| macOS | `UseSystemVideo()` | AVFoundation (VideoToolbox), zero-copy via `CVMetalTextureCache` |
| Android | `UseAndroidVideo()` | MediaExtractor + MediaCodec |
| iOS | `UseIosVideo()` | AVPlayer + AVPlayerItemVideoOutput |

`Miko.Video.FFmpeg` is an **opt-in extension** (`UseFFmpegVideo()`) for formats the system
decoder does not cover; it pulls in >80MB of native FFmpeg and must never be a platform default.
It is deliberately **excluded from `miko.slnx`** — experiment/test use only, so it is neither
built by CI nor published as a package. Build it explicitly
(`dotnet build src/Miko.Video.FFmpeg/Miko.Video.FFmpeg.csproj`) when working on it.

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

### Component-facing platform services

`IPlatformService : IPlatformInfo` (`src/Miko/Platform/`) is what *component libraries* use, as opposed
to the app-facing `Miko.Native`: `Back` (system back-button chain, priority-ordered),
`Orientation` (lock / keep-awake, reference counted) and `Haptics` (`IHapticFeedback`:
`SelectionStart/Changed/End`, `Impact`, mirroring Ionic's haptic utilities). Every member is
synchronous, safe to call while rendering, and silently inert where unsupported — components never
null-check or catch. One `PlatformInfo` instance backs `PlatformInfo`, `IPlatformInfo` and
`IPlatformService` (ISSUE-138, ISSUE-149).

- The real orientation / haptics implementations are bound by the platform packages' `Use*Native()`
  (through `MikoAppBuilder.UseOrientation` / `UseHaptics`). An app that does not call it keeps the
  `Null*` defaults — picker ticks and fullscreen landscape then do nothing. The desktop keeps
  `NullHapticFeedback` on purpose; the simulator logs and counts haptics (`SimulatorHapticFeedback`).
- Android haptics use `View.PerformHapticFeedback` (no `VIBRATE` permission, honours the system
  touch-feedback setting). On the emulator, `adb shell dumpsys vibrator_manager` lists each call
  under "Recent vibrations" with the app package and constant.
- Android's minimum is API 26 (Android 8.0) in `Miko.Android`, templates and examples alike; newer APIs
  sit behind `OperatingSystem.IsAndroidVersionAtLeast` (CA1416 is an error in `Miko.Android`).
- An Android Activity whose app locks orientation must declare `ConfigurationChanges = Orientation |
  ScreenSize | …`; otherwise rotation recreates the Activity and rebuilds the whole app.
- Hosts report their size only through `MikoInteractionController.RenderFrame(canvas, width, height, …)`;
  it updates the engine viewport, so rotation needs no separate resize call.
- `IonPickerColumn` follows a touch drag freely and commits `ValueChanged` on release (as Ionic sets the
  value once scrolling settles); wheel and fling input still snap per scroll event.
- Scrollbar presentation follows the platform (ISSUE-156): `PlatformScrollbars.GetDefault` gives Classic
  (Windows/Linux), Overlay (macOS), None (iOS/Android); apps adjust it with `UseScrollbarStyle`. The
  controller pushes the style for the current `PlatformInfo.Platform` into `MikoEngine.ScrollbarStyle`
  every frame (a no-op when unchanged); standalone engines stay Classic, so tests are OS-independent
  unless they go through `MikoAppBuilder` — pin the style there if a test depends on it. `LayoutEngine`
  stamps it on `ComputedStyle.ScrollbarStyle`. Only Classic takes space: clip, max scroll and
  scroll-into-view use `*ScrollbarGutter`; track geometry (paint, hit test, drag) uses `*ScrollbarThickness`.
  Overlay visibility lives in `OverlayScrollbarActivity`: the 1s hold is timed with the DI `TimeProvider`
  and renders no frames, a timer posts to `MikoDispatcher` to wake the host, and only the 0.3s fade
  counts as pending render work.

### Image Loading

Images are loaded asynchronously via `IImageLoader`:
- Default implementation: `ResourceManager` (embedded resources and file paths)
- Platform hosts inject via `MikoEngine.ImageLoader`
- `ImageElement` tracks loading state and supports placeholder images
- Loaded bitmaps are cached in element's `Bitmap` property

`ResourceManager` caches successful decodes only: a failed entry stays listed (Failed) for DevTools
but the next request for that path loads again, and a consumer's `CancellationToken` cancels only its
own wait — the shared load is cancelled (and dropped) only when every waiting consumer has cancelled
(ISSUE-154).

`res://` paths are written like `file://` ones — **no assembly name**, `/` separators, relative
to the project root: `res://Assets/logo.svg` for `Assets/logo.svg` embedded in any assembly
registered with `AddResourceAssembly`. `EmbeddedResources` maps a path onto the actual .NET
manifest resource name (`MyApp.Assets.logo.svg`) by normalizing separators to dots and matching
on a dot boundary suffix, so renaming an assembly or moving a project never rewrites resource
paths (ISSUE-139). The dot boundary matters: a bare suffix compare would let `res://avatar.svg`
hit `MyApp.Assets.my-avatar.svg`. Old dotted paths that include the assembly name still resolve
(they are an exact manifest match), so both spellings coexist.

`ResourceManager` also implements `IResourceDiagnostics` — the DevTools **Resources** panel reads
it to show what Miko downloaded (url, size, duration) and what sits in the decode cache. Its
`Version` is a monotonic counter: the panel is rebuilt off that, not by polling snapshots, because
the DevTools window skips frames when idle. A custom `IImageLoader` that does not implement the
interface simply leaves those two sections explaining why they are empty; the embedded-resource
list comes from assembly metadata and always works.

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

The collection also enforces **one parent per element** (ISSUE-154): `Add`, `Insert`, the indexer,
`AddRange` and `AddChild` *move* an element that already has a parent (DOM `appendChild` /
`insertBefore` / `replaceChild` semantics, also within the same collection), and reject `null` and
cycles before changing anything. Moving across engines bumps both engines and transfers ownership.
Code that moves children from one element to another must iterate a snapshot.

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
- The Razor compiler targets net10.0 and is consumed only as analyzer DLLs
- Safe area insets are opt-in via `env()` function, never applied as root viewport insets (see memory)
- Text editing (`ITextEditable`): `CursorPosition`, input-method selection and `MaxLength` count UTF-16
  code units; insert/backspace/delete/arrow keys work on grapheme clusters (`TextEditing` in
  `ITextEditable.cs`), so emoji and combining sequences are never split (ISSUE-154)
- Documentation site under `docs/` (VitePress) contains the usage guide; see "Documentation" below
- `DEVELOPMENT.md` (in Chinese) has developer-oriented walkthrough
