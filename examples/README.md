# Miko Examples

This folder contains example applications demonstrating how to use the Miko rendering engine.
The examples are grouped by theme into subfolders, each illustrating a different aspect of
the framework — from a static render-to-PNG console app to full cross-platform Razor apps.

## Overview

| Folder                              | What it shows                                                                 |
| ----------------------------------- | ----------------------------------------------------------------------------- |
| [`Bootstrap/`](#bootstrap)          | A Bootstrap-styled component gallery hosted from many platform heads.          |
| [`Multiplatform/`](#multiplatform)  | Starter templates with a shared Razor core and Android / iOS / Desktop heads.  |
| [`Windows/`](#windows)              | The same starter templates as single Windows desktop projects.                 |
| [`Async/`](#async)                  | `async`/`await` components, Blazor-style `EventCallback`, and real HTTP calls.  |
| [`Media/`](#media)                  | Network `<img>` (with `placeholder`) and `<video>` via the unified resource manager. |

---

## Bootstrap

`Bootstrap/` — A gallery of Bootstrap-inspired components (buttons, forms, tables, lists,
icons, animations) implemented once and hosted from several different platform "heads".

The shared UI lives in **`MikoApp1/`** (components, styles, layout, routing). Each sibling
project is a thin host that boots that UI on a particular platform or authoring style:

| Project              | Host                                                                  |
| -------------------- | --------------------------------------------------------------------- |
| `MikoApp1`           | Shared component library (Bootstrap styles + example components).     |
| `MikoApp1.Console`   | Headless console app that renders the UI to an `output.png` file.     |
| `MikoApp1.Razor`     | Desktop window with the gallery authored as `.razor` components.      |
| `MikoApp1.WinUI`     | WinUI desktop host.                                                   |
| `MikoApp1.Droid`     | Android host.                                                         |
| `MikoApp1.iOS`       | iOS host.                                                             |

**Run the console renderer (writes a PNG):**

```bash
dotnet run --project examples/Bootstrap/MikoApp1.Console/MikoApp1.Console.csproj
```

It prints the path of the generated `output.png` showing all component variants.

**Run the desktop window:**

```bash
dotnet run --project examples/Bootstrap/MikoApp1.Razor/MikoApp1.Razor.csproj
```

**What it demonstrates:**

- Authoring a reusable component/UI library and hosting it from multiple entry points
- Bootstrap-inspired color palette, buttons, forms, tables, lists, and icon fonts
- Routing (`Router` / `RouteView`) and a shared `MainLayout`
- Rendering both to an image file (Console) and to a live window (Razor / WinUI / mobile)

---

## Multiplatform

`Multiplatform/` — Starter templates structured as a **shared Razor core** plus one host
project per platform (Android, iOS, Desktop). This is the recommended layout for an app
that targets several platforms from a single codebase.

Three templates are provided, each in its own folder:

- **`MikoAppBlank/`** — an empty single-page app.
- **`MikoAppSidemenu/`** — a layout with a side navigation menu.
- **`MikoAppTabs/`** — a layout with a tab bar.

Each template contains four projects, e.g. for `MikoAppBlank`:

| Project                   | Role                                                              |
| ------------------------- | ---------------------------------------------------------------- |
| `MikoAppBlank`            | Shared Razor UI — pages, layout, global styles, `App.cs`.        |
| `MikoAppBlank.Desktop`    | Desktop host (`Program.cs`).                                     |
| `MikoAppBlank.Android`    | Android host (`MainActivity`, manifest).                         |
| `MikoAppBlank.iOS`        | iOS host (`AppDelegate`, `Info.plist`).                          |

**Run the desktop head:**

```bash
dotnet run --project examples/Multiplatform/MikoAppBlank/MikoAppBlank.Desktop/MikoAppBlank.Desktop.csproj
```

The Android and iOS heads require the corresponding .NET workloads and are typically
built / deployed from an IDE.

**What it demonstrates:**

- Sharing one Razor UI project across Desktop, Android, and iOS heads
- Common app templates (blank, side menu, tabs) to start a new project from
- File-based routing discovered by `Miko.Razor.Compiler` (`UseGeneratedRoutes()`)

---

## Windows

`Windows/` — The same three starter templates as **single Windows desktop projects**
(no separate platform heads). Use these when you only target Windows and want the
simplest possible project layout.

- **`MikoAppBlank/`** — empty single-page app.
- **`MikoAppSidemenu/`** — side navigation menu.
- **`MikoAppTabs/`** — tab bar.

Each project's UI lives directly in the project (`Pages/`, `MainLayout.razor`,
`GlobalStyles.cs`, `App.cs`, `Program.cs`).

**Run:**

```bash
dotnet run --project examples/Windows/MikoAppBlank/MikoAppBlank.csproj
```

A window opens showing the home page (`Pages/Home.razor`). Each template folder has its
own `README.md` with the project layout and instructions for adding a page.

**What it demonstrates:**

- The minimal single-project setup for a Windows-only Miko app
- Razor components rendered with SkiaSharp, plus `Miko.DevTools` and hot reload

---

## Async

`Async/` (`MikoApp.AsyncDemo`) — Demonstrates `async`/`await` support in Miko components,
including async lifecycle methods, Blazor-style `EventCallback`, and **real HTTP
communication** between a Miko client and an ASP.NET Core API.

It contains three projects:

| Project                       | Role                                                               |
| ----------------------------- | ----------------------------------------------------------------- |
| `MikoApp.AsyncDemo`           | Shared Razor UI — product search built with Bootstrap components. |
| `MikoApp.AsyncDemo.Desktop`   | Desktop startup project (the Miko client window).                 |
| `MikoApp.AsyncDemo.Api`       | ASP.NET Core minimal API serving product data (with a delay).     |

**Run (two terminals):**

```bash
# Terminal 1 — start the API on http://localhost:5000
dotnet run --project examples/Async/MikoApp.AsyncDemo.Api/MikoApp.AsyncDemo.Api.csproj

# Terminal 2 — start the Miko client window
dotnet run --project examples/Async/MikoApp.AsyncDemo.Desktop/MikoApp.AsyncDemo.Desktop.csproj
```

The window has two nav buttons: **Bootstrap Demo** (`/`) and **Native Demo** (`/native`),
which render the same product-search UI using Bootstrap components vs. raw HTML elements.

**What it demonstrates:**

- Async lifecycle (`OnInitializedAsync`) with non-blocking loading states
- Blazor-style `EventCallback` / `EventCallback<T>` parameters and `@onclick` handlers
- Automatic re-render after an `await` completes (twice, matching Blazor)
- Continuations marshalled back to the render thread via `MikoSynchronizationContext`

See [`Async/MikoApp.AsyncDemo/README.md`](Async/MikoApp.AsyncDemo/README.md) for the full
walkthrough.

---

## Media

`Media/` — Loads **network** images and a video through Miko's unified resource manager
(ISSUE-062). Three projects modeled on the `Async/` layout:

- `MikoApp.Media.Api` — ASP.NET Core minimal API that generates 24 products, thumbnails
  (SkiaSharp) and a sample video (FFmpeg) **offline** and serves them over http.
- `MikoApp.Media` — the shared Miko client UI.
- `MikoApp.Media.Desktop` — the desktop head (registers the FFmpeg video backend).

**Run** (two terminals — API first):

```bash
dotnet run --project examples/Media/MikoApp.Media.Api        # http://localhost:5050
dotnet run --project examples/Media/MikoApp.Media.Desktop    # opens the window
```

**What it demonstrates:**

- A unified `MediaSource` + resource manager resolving `file://` / `res://` / `http(s)://` / `data:`
- `<img>` loading from the network asynchronously, with a `res://` `placeholder` shown until ready
- `VideoElement` streaming a sample clip over http via the FFmpeg backend (`UseFFmpegVideo()`)
- A replaced-element image/video sized by its intrinsic dimensions inside the box model

> Note: this demo targets Windows (`win-x64`) and requires the FFmpeg backend.

---

## Creating Your Own Example

The simplest starting point is one of the templates under [`Windows/`](#windows) or
[`Multiplatform/`](#multiplatform) — copy a template folder and rename the projects.

For a headless render-to-image example, model it on
[`Bootstrap/MikoApp1.Console`](Bootstrap/MikoApp1.Console/Program.cs):

```csharp
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using SkiaSharp;

// Create DOM tree
var root = new DivElement { /* ... */ };

// Create stylesheet
var styleSheet = new StyleSheet();
// Add style rules...

// Initialize engine and render
using var surface = SKSurface.Create(new SKImageInfo(width, height));
var canvas = surface.Canvas;
canvas.Clear(SKColors.White);

var engine = new MikoEngine();
engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, width, height);
engine.Render(canvas);

// Save output
using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite(outputPath);
data.SaveTo(stream);
```

Add a reference to Miko (and `Miko.Windowing` for a windowed app):

```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\src\Miko\Miko.csproj" />
  <ProjectReference Include="..\..\..\src\Miko.Windowing\Miko.Windowing.csproj" />
</ItemGroup>
```

