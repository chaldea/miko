# Examples

The repository's [`examples/`](https://github.com/chaldea/miko/tree/main/examples)
folder contains runnable sample apps, grouped by theme. Each shows a different aspect of
the framework — from cross-platform Razor apps to async HTTP communication and media playback.

| Folder | What it shows |
| --- | --- |
| [Multiplatform](#multiplatform) | Starter templates with a shared Razor core and Android / iOS / Desktop heads. |
| [Windows](#windows) | The same starter templates as single Windows desktop projects. |
| [Async](#async) | `async`/`await` components, Blazor-style `EventCallback`, and real HTTP calls. |
| [Media](#media) | Network `<img>` (with `placeholder`) and `<video>` via the unified resource manager. |
| [Localization](#localization) | `IStringLocalizer` + `.resx`, live language switching, right-to-left layout, complex scripts. |

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
[Async & Lifecycle](./guide/async.md) for the concepts.

## Media

`Media/` loads **network and local** images and video through Miko's unified resource manager.
The client shows two `<video>` elements side by side — one streamed over http, one from a local
file — both decoded by the **system hardware decoder**. An ASP.NET Core API serves
offline-generated product thumbnails (SkiaSharp), rendered with `<img>` and a `res://`
`placeholder` shown until each thumbnail loads.

```bash
# The local video plays with no server at all
dotnet run --project examples/Media/MikoApp.Media.Desktop

# For the network video and thumbnails, start the API first (separate terminal)
dotnet run --project examples/Media/MikoApp.Media.Api        # http://localhost:5050
```

> The client carries **no FFmpeg binaries** — `UseSystemVideo()` dispatches to Media Foundation
> (Windows), GStreamer (Linux) or AVFoundation (macOS). Only the API project references FFmpeg,
> and only to pre-generate the sample clip offline.

## Localization

`MikoApp.Localization` reads its strings through `IStringLocalizer<T>` from `.resx` files and
switches the running app between English, Simplified Chinese, Arabic, Hebrew and Hindi. In Arabic
and Hebrew the layout is mirrored with `dir="rtl"`; a second page shows complex scripts and
mixed-direction text.

```bash
dotnet run --project examples/Localization/MikoApp.Localization.Desktop
# start in a given language:
dotnet run --project examples/Localization/MikoApp.Localization.Desktop -- ar-EG
```

See [Localization & Right-to-Left](./guide/localization.md) for the concepts.

---

For the full walkthroughs, see the
[`examples/README.md`](https://github.com/chaldea/miko/blob/main/examples/README.md)
in the repository.
