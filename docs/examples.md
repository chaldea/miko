# Examples

The repository's [`examples/`](https://github.com/chaldea/miko/tree/main/examples)
folder contains runnable sample apps, grouped by theme. Each shows a different aspect of
the framework — from a static render-to-PNG console app to full cross-platform Razor apps.

| Folder | What it shows |
| --- | --- |
| [Bootstrap](#bootstrap) | A Bootstrap-styled component gallery hosted from many platform heads. |
| [Multiplatform](#multiplatform) | Starter templates with a shared Razor core and Android / iOS / Desktop heads. |
| [Windows](#windows) | The same starter templates as single Windows desktop projects. |
| [Async](#async) | `async`/`await` components, Blazor-style `EventCallback`, and real HTTP calls. |
| [Media](#media) | Network `<img>` (with `placeholder`) and `<video>` via the unified resource manager. |

## Bootstrap

A gallery of Bootstrap-inspired components (buttons, forms, tables, lists, icons,
animations) implemented once and hosted from several platform "heads". The shared UI lives
in `MikoApp1/`; each sibling project is a thin host (Console, Razor desktop, WinUI,
Android, iOS).

```bash
# Run the console renderer (writes output.png showing all component variants)
dotnet run --project examples/Bootstrap/MikoApp1.Console/MikoApp1.Console.csproj

# Run the desktop window
dotnet run --project examples/Bootstrap/MikoApp1.Razor/MikoApp1.Razor.csproj
```

Demonstrates: authoring a reusable component library and hosting it from multiple entry
points; a Bootstrap-inspired palette; routing and a shared `MainLayout`; rendering both to
an image file and to a live window.

## Multiplatform

Starter templates structured as a **shared Razor core** plus one host project per platform
(Android, iOS, Desktop) — the recommended layout for a multi-target app. Three templates
are provided: `MikoAppBlank`, `MikoAppSidemenu`, and `MikoAppTabs`.

```bash
# Run the desktop head
dotnet run --project examples/Multiplatform/MikoAppBlank/MikoAppBlank.Desktop/MikoAppBlank.Desktop.csproj
```

The Android and iOS heads require the corresponding .NET workloads and are typically built
from an IDE.

## Windows

The same three starter templates as **single Windows desktop projects** (no separate
platform heads) — the simplest layout when you only target Windows.

```bash
dotnet run --project examples/Windows/MikoAppBlank/MikoAppBlank.csproj
```

A window opens showing the home page (`Pages/Home.razor`). Each template folder has its
own README with instructions for adding a page.

## Async

`MikoApp.AsyncDemo` demonstrates `async`/`await` support — async lifecycle methods,
Blazor-style `EventCallback`, and **real HTTP communication** between a Miko client and an
ASP.NET Core API. It has three projects: the shared UI, a desktop client, and a minimal
API.

```bash
# Terminal 1 — start the API on http://localhost:5000
dotnet run --project examples/Async/MikoApp.AsyncDemo.Api/MikoApp.AsyncDemo.Api.csproj

# Terminal 2 — start the Miko client window
dotnet run --project examples/Async/MikoApp.AsyncDemo.Desktop/MikoApp.AsyncDemo.Desktop.csproj
```

Demonstrates: async lifecycle (`OnInitializedAsync`) with non-blocking loading states;
`EventCallback` parameters and `@onclick` handlers; automatic re-render after an `await`;
continuations marshalled back to the render thread. See
[Async & Lifecycle](/guide/async) for the concepts.

## Media

`Media/` loads **network** images and a video through Miko's unified resource manager.
An ASP.NET Core API serves offline-generated product thumbnails (SkiaSharp) and a sample
video (FFmpeg); the Miko client renders them with `<img>` (with a `res://` `placeholder`
shown until each thumbnail loads) and `<video>` (FFmpeg backend).

```bash
# Two terminals — start the API first
dotnet run --project examples/Media/MikoApp.Media.Api        # http://localhost:5050
dotnet run --project examples/Media/MikoApp.Media.Desktop    # opens the window
```

> This demo targets Windows (`win-x64`) and requires the FFmpeg backend.

---

For the full walkthroughs, see the
[`examples/README.md`](https://github.com/chaldea/miko/blob/main/examples/README.md)
in the repository.
