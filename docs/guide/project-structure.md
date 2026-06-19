# Project Structure

This page covers two things: the layout of a **generated app** and the layout of the
**Miko repository** itself (useful if you build from source).

## Anatomy of a generated app

A single-platform (Windows desktop) app scaffolded from the template looks like this:

```text
MyApp/
├── App.cs              # MikoAppBuilder configuration (title, size, routes, layout)
├── Program.cs          # Entry point — builds the context and runs the desktop host
├── GlobalStyles.cs     # App-wide stylesheet (StyleSheet built in code)
├── MainLayout.razor    # Default layout wrapping every routed page
├── _Imports.razor      # Shared @using directives for components
└── Pages/
    └── Home.razor      # A routed page (@page "/")
```

A **multiplatform** app splits the shared UI from per-platform hosts:

```text
MyApp/
├── MyApp/                 # Shared Razor UI — Pages, MainLayout, GlobalStyles, App.cs
├── MyApp.Desktop/         # Desktop host (Program.cs, references Miko.Windowing)
├── MyApp.Android/         # Android host (MainActivity, manifest, references Miko.Android)
└── MyApp.iOS/             # iOS host (AppDelegate, Info.plist, references Miko.iOS)
```

The shared project references only the core `Miko` (plus component packages such as
`Miko.Bootstrap` / `Miko.Ionic`). Each host project owns the window / GL surface / native
input for its platform and drives the render loop:

```csharp
// Desktop startup project
MyApp.App.CreateContext().RunDesktop();   // needs Miko.Windowing

// Android Activity
SetContentView(MikoAndroidApp.CreateView(this, MyApp.App.CreateContext)); // needs Miko.Android

// iOS AppDelegate
Window.RootViewController = new MikoViewController(MyApp.App.CreateContext()); // needs Miko.iOS
```

## The Miko repository

If you build from source, the repository is organized by module:

```text
miko/
├── miko.slnx                   # Solution (core library + desktop)
├── src/
│   ├── Miko/                   # Core engine (platform-agnostic)
│   │   ├── Common/             # Length, Color, RectF, Enums
│   │   ├── Core/               # Element base + DomElements/ + MikoEngine
│   │   ├── Styling/            # Style, StyleSheet, StyleResolver, Selectors/
│   │   ├── Layout/             # LayoutEngine, BoxModel, LayoutAlgorithms/
│   │   ├── Rendering/          # RenderEngine, Painter, DirtyRegionManager
│   │   ├── Fonts/              # FontManager, fallback resolver, WOFF2 decoder
│   │   ├── Events/             # EventDispatcher, event types & args
│   │   ├── Platform/           # MikoInput, MikoInteractionController (platform-agnostic)
│   │   ├── Hosting/            # MikoAppBuilder, MikoAppContext, HotReloadService
│   │   └── Utils/              # TreeTraversal, TextMeasurer, GeometryUtils
│   ├── Miko.Windowing/         # Desktop host (Windows/Linux/macOS, Silk.NET)
│   ├── Miko.Android/           # Android host
│   ├── Miko.iOS/               # iOS host
│   ├── Miko.Bootstrap/         # Bootstrap-style Razor component library
│   ├── Miko.Ionic/             # Ionic-style Razor component library
│   ├── Miko.DevTools/          # Runtime DOM / layout inspector
│   └── Miko.Razor.Compiler/    # Source generator for .razor components & routes
├── tests/Miko.Tests/           # xUnit + Shouldly unit tests
├── examples/                   # Runnable sample apps (see Examples)
└── templates/                  # dotnet new templates
```

### Platform abstraction

The core `Miko` library contains only **platform abstractions**: normalized input enums
(`MikoKey` / `MikoKeyModifiers`) and the platform-agnostic `MikoInteractionController`
(hit-testing, focus, click/dropdown handling, scrollbar & slider dragging, text editing,
cursor resolution, and event dispatch). Each platform implementation layer only owns the
window / GL surface / native input and forwards normalized events to the controller:

- **Miko.Windowing** — desktop (Windows / Linux / macOS), based on Silk.NET.
- **Miko.Android** — `MikoSurfaceView` hosts rendering and maps touch to pointer events.
- **Miko.iOS** — `MikoGLView` hosts rendering and maps touch to pointer events.

## Build and test from source

```bash
# Build the solution
dotnet build miko.slnx

# Run all tests
dotnet test

# Run a specific test class
dotnet test --filter "FullyQualifiedName~Miko.Tests.Layout.BoxModelTests"

# Clean build artifacts
dotnet clean
```
