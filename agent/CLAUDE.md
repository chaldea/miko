# CLAUDE.md

Guidance for Claude Code when working in this app.

## What this app is

This is a **Miko** app. Miko is a native .NET UI engine: you author UI as **Razor components** (`.razor`) and Miko draws every pixel with **SkiaSharp**. There is **no browser, no WebView, and no HTML/CSS runtime**. Its element set, styling model, and components differ from the web — do not assume web behavior.

## Read the Miko reference before building UI

Agent-optimized Miko docs live in [`.miko/`](.miko/). **Start with [`.miko/llms.txt`](.miko/llms.txt)**, then open the topic you need:

| Doc | When |
| --- | --- |
| [`.miko/overview.md`](.miko/overview.md) | First — mental model + the hard constraints (no CSS text, no `inherit`, typed `Style`). |
| [`.miko/elements/index.md`](.miko/elements/index.md) | Before using any tag. There is **no** `<textarea>`, `<form>`, `<section>`, `<em>`, `<br>`, `<hr>`. |
| [`.miko/components/index.md`](.miko/components/index.md) | Ionic components (`IonButton`, `IonList`, `IonTabs`…) + parent/child rules. |
| [`.miko/styling.md`](.miko/styling.md) | Styles are **C# objects**, not CSS strings. |
| [`.miko/layout.md`](.miko/layout.md), [`.miko/events.md`](.miko/events.md), [`.miko/razor.md`](.miko/razor.md), [`.miko/project.md`](.miko/project.md) | Layout, events, Razor authoring, app wiring. |
| [`.miko/examples/`](.miko/examples/) | Copy-pasteable counter / tabs / sidemenu patterns. |

## Project layout

- `App.cs` — `MikoAppBuilder` configuration (title, size, routes, layout, `AddIonic()`).
- `Program.cs` — entry point; builds the context and runs the host.
- `Pages/` — routed pages. A `.razor` with `@page "/route"` is a route (auto-discovered).
- `MainLayout.razor` — default layout wrapping every page (`@Body`).
- `GlobalStyles.cs` — the app `StyleSheet`, built in C# with `CssObject`.
- `_Imports.razor` — shared `@using` directives.

## Build & run

```bash
dotnet build
dotnet run           # single-platform template
# multiplatform template:
dotnet run --project <AppName>.Desktop
```

## Guardrails

- **Styles are typed C#** (`new Style { BackgroundColor = "#512BD4" }`), never CSS text.
- **Only listed elements exist** — check `.miko/elements/index.md`; use `<div>`/`<span>` for missing ones.
- **No `inherit`** for non-inherited properties — set them explicitly.
- **Ionic** needs `builder.AddIonic()` + `@using Miko.Ionic.Components`, takes a `Style` object, and follows parent/child rules (e.g. `IonItem` inside `IonList`).
