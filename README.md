# Miko

[![NuGet](https://img.shields.io/nuget/v/Miko.svg)](https://www.nuget.org/packages/Miko)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)

**Miko** is a **native, cross-platform UI rendering engine for .NET** that uses
**[Razor](https://learn.microsoft.com/aspnet/core/blazor/components/) as its layout
DSL**. It draws every pixel itself with [SkiaSharp](https://github.com/mono/SkiaSharp) —
**no browser, no WebView, no HTML/CSS runtime** — running a browser-like pipeline of
style cascade, layout, and incremental painting directly onto a GPU-accelerated canvas.

You write your UI in Razor components — the same `.razor` syntax you already know from
Blazor — and Miko's source generator compiles them into native draw calls. If you know
HTML and Razor, you already know how to build a Miko app; there is **no XAML to learn**.

```text
Razor components  ─►  Miko source generator  ─►  native SkiaSharp rendering
```

- 🪶 **Tiny & dependency-free** — ships as a few .NET assemblies plus SkiaSharp. No
  bundled Chromium, no Node, no WebView. Binaries are a fraction of an Electron app's.
- ⚡ **Native rendering** — pixels are drawn directly on a GPU-backed Skia surface, not
  inside a web engine.
- 🖥️ **Cross-platform** — one codebase runs on Windows, macOS, Linux, and Android,
  wherever SkiaSharp does.
- 🚀 **Trimming & AOT-aware** — the routing and rendering paths are designed for
  trimmed / Native AOT publishing for fast startup and small, self-contained binaries.
- 🔥 **Hot reload** — edit a `.razor` component and see the change without restarting.
- ⚛️ **Reuse your Blazor skills (and components)** — Razor components, layouts, routing,
  and data binding work the way you expect; many existing Blazor components can be
  recompiled to render natively with Miko instead of in a browser.

> ⚠️ Miko is an experimental project under active development. APIs may change between
> versions.

---

## Why Miko?

**vs. Electron / WebView-based UIs**

- No embedded browser. There is no Chromium or system WebView to ship, update, or
  sandbox — Miko renders with SkiaSharp directly.
- Dramatically smaller binaries and lower memory use; no separate renderer process.
- Pure .NET. Your UI logic, layout, and rendering all run in one managed process, with
  optional Native AOT for near-instant startup.

**vs. .NET MAUI**

- **Razor instead of XAML.** Miko's layout DSL is Razor + a CSS-like styling model, so
  you build UIs with HTML/Razor knowledge instead of learning XAML and its binding
  syntax.
- **One renderer everywhere.** Miko draws its own widgets with Skia on every platform
  rather than wrapping each OS's native controls, so the UI looks and behaves
  identically across targets.
- **Bring your Blazor components.** Because the component model is Razor, much of an
  existing Blazor app can be recompiled to render natively with Miko — no browser
  required.

---

## Features

- ⚛️ **Razor as the UI DSL** — author your UI as `.razor` components with routing,
  layouts, data binding, and **hot reload**; a source generator compiles them ahead of
  time into native draw calls.
- 🎨 **Browser-like rendering pipeline** — DOM construction → style cascade → layout →
  paint, drawn natively with SkiaSharp.
- 🧩 **Rich element set** — `div`, `span`, `p`, `h1`–`h6`, `button`, `input`, `select`,
  `img`, lists, and tables.
- 💅 **CSS-like styling** — stylesheets with tag / class / id / pseudo-class / compound
  selectors, specificity-based cascade, and inline styles.
- 📐 **Box model & layout** — full content/padding/border/margin box model with **block**,
  **inline**, and **flex** layout algorithms.
- ⚡ **Incremental rendering** — dirty-region tracking and merging repaint only what changed.
- ✍️ **Typography** — font registration (TTF/OTF/WOFF/WOFF2), a configurable fallback
  chain, and text measurement.
- 🖱️ **Events** — DOM-style event dispatch with capture and bubbling.
- 🅱️ **Bootstrap-style component library** — ready-made buttons, cards, alerts,
  accordions, and more.
- 🛠️ **Dev tools** — inspect the DOM and layout tree at runtime.
- 🖥️ **Cross-platform & AOT-aware** — one codebase on Windows, macOS, Linux, and Android,
  built for trimmed / Native AOT publishing.

---

## Packages

| Package                                                              | Description                                                      |
| -------------------------------------------------------------------- | ---------------------------------------------------------------- |
| [`Miko`](https://www.nuget.org/packages/Miko)                        | Core rendering engine: DOM, styling, layout, and painting.       |
| [`Miko.Bootstrap`](https://www.nuget.org/packages/Miko.Bootstrap)    | Bootstrap-style Razor component library and styles.              |
| [`Miko.DevTools`](https://www.nuget.org/packages/Miko.DevTools)      | Runtime debugging tools for the DOM and layout tree.             |
| [`Miko.Razor.Compiler`](https://www.nuget.org/packages/Miko.Razor.Compiler) | Source generator that compiles `.razor` components and routes. |
| [`Miko.Templates`](https://www.nuget.org/packages/Miko.Templates)    | `dotnet new` templates for scaffolding Miko apps.                |

---

## Getting started

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/) or later
- A platform supported by SkiaSharp (Windows, macOS, Linux, or Android)

### Create an app from the template

The fastest way to start is the Razor app template:

```bash
# Install the templates
dotnet new install Miko.Templates

# Scaffold and run a new app
dotnet new miko-razor -o MyApp
cd MyApp
dotnet run
```

A window opens showing the home page (`Pages/Home.razor`). Add a page by dropping a new
`.razor` file with a `@page` directive into `Pages/` — routes are discovered automatically.

```razor
@page "/about"
@namespace MyApp.Pages

<h1>About</h1>
<p>Hello from Miko!</p>
```

The app is configured with a fluent builder:

```csharp
using Miko.Hosting;

var builder = MikoAppBuilder.CreateDefault();
builder.UseTitle("My App");
builder.UseSize(1024, 768);
builder.UseGeneratedRoutes();        // routes from .razor @page directives
builder.UseDefaultLayout<MainLayout>();
builder.EnableHotReload();

var app = builder.Build();
app.Run();
```

### Use the engine directly

You don't need Razor — you can build the DOM and render a frame by hand. The example
below renders a small tree to a PNG:

```csharp
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Common;
using SkiaSharp;

// 1. Define a stylesheet
var styleSheet = new StyleSheet();
styleSheet.AddRule(new ClassSelector("container"), new Style
{
    Display = Display.Flex,
    FlexDirection = FlexDirection.Column,
    Padding = new Padding(Length.Px(20)),
    BackgroundColor = new Color(245, 245, 245)
});

// 2. Build the DOM tree
var root = new DivElement { Class = "container" };
root.AddChild(new H1Element { TextContent = "Hello Miko" });
root.AddChild(new ParagraphElement { TextContent = "A lightweight rendering engine." });

// 3. Render to a Skia canvas
using var surface = SKSurface.Create(new SKImageInfo(800, 600));
var canvas = surface.Canvas;
canvas.Clear(SKColors.White);

var engine = new MikoEngine();
engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);

// 4. Export the frame
using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite("output.png");
data.SaveTo(stream);
```

For interactive scenes, mutate the DOM and repaint only what changed:

```csharp
element.TextContent = "Updated text";
engine.InvalidateElement(element);
engine.Update(canvas);            // incremental render of dirty regions only
```

---

## How it works

Miko mirrors a browser's rendering pipeline:

```
1. DOM construction   →  build an Element tree in code
2. Style computation  →  StyleResolver matches selectors and cascades styles
3. Layout tree build  →  filter elements by their display property
4. Layout             →  constraints flow down, sizes flow up
5. Paint              →  RenderEngine draws to an SKCanvas (dirty-region optimized)
```

| Layer       | Responsibility                                                            |
| ----------- | ------------------------------------------------------------------------- |
| **Core**    | `Element` tree and `MikoEngine`, which coordinates layout and rendering.  |
| **Styling** | Stylesheets, selectors, the cascade, and computed styles.                 |
| **Layout**  | The box model plus block, inline, and flex layout algorithms.             |
| **Rendering** | `RenderEngine`, the SkiaSharp `Painter`, and the dirty-region manager.  |
| **Fonts**   | Font registration, fallback resolution, and text measurement.             |
| **Events**  | DOM-style event dispatch with capture and bubbling.                       |

Style cascade order (lowest to highest precedence): **Tag → Class → ID → inline**.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the full design and
[`DEVELOPMENT.md`](DEVELOPMENT.md) for a developer-oriented walkthrough.

---

## Building from source

```bash
# Clone
git clone https://github.com/chaldea/miko.git
cd miko

# Build the solution
dotnet build miko.slnx

# Run the tests
dotnet test
```

The repository also contains runnable samples under [`examples/`](examples/), including
Console, WinUI, and Android launchers.

---

## Contributing

Contributions are welcome! Please open an issue to discuss substantial changes before
sending a pull request. When contributing:

- Follow the existing code style (`.editorconfig` is enforced).
- Add or update tests for the behavior you change.
- Make sure `dotnet build` and `dotnet test` pass.

---

## License

Miko is released under the [MIT License](LICENSE).
