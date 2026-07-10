# Miko Overview

Miko is a native, cross-platform UI rendering engine for .NET 10. You write UI as **Razor components**; Miko compiles them (via the `Miko.Razor.Compiler` source generator) into a DOM-like element tree and draws every pixel with **SkiaSharp** onto a GPU-backed surface. There is no browser, no WebView, no HTML/CSS runtime.

One codebase runs on Windows, macOS, Linux, Android, and iOS.

## Mental model

Miko mirrors a browser engine, but everything is C# ahead-of-time:

```
.razor components
  → Miko.Razor.Compiler (source generator, build time)
  → Element tree (DOM)              see elements/index.md
  → Style cascade (StyleSheet)      see styling.md
  → Layout (box model + algorithms) see layout.md
  → Paint to SKCanvas (dirty-region incremental)
  → Platform host (Desktop / Android / iOS)
```

`.razor` markup that looks like `<div class="x">` builds a `DivElement { Class = "x" }`. Style rules match those elements by tag/class/id and cascade into a `ComputedStyle`. Layout computes box sizes/positions. The render engine paints only dirty regions.

## The pipeline (5 stages)

1. **DOM construction** — Razor components build the `Element` tree.
2. **Style computation** — `StyleResolver` matches selectors and computes the cascade.
3. **Layout tree build** — elements filtered by `Display` (`None` is excluded).
4. **Layout calculation** — constraints flow **down** (parent → child, available space); sizes flow **up** (child → parent, content size).
5. **Painting** — `RenderEngine` draws to the canvas, repainting only dirty regions.

## Hard constraints (differ from HTML/CSS — read before coding)

| Constraint | What it means for you |
| --- | --- |
| **No CSS text** | Styles are typed C# objects (`Style`, `Length`, `Color`). There is no `style="..."` string parser. See [styling.md](styling.md). |
| **No `inherit` keyword** | Non-inherited properties do not cascade from parent. Mirror the value onto each element/descendant explicitly. (Inherited props like `Color`, `FontFamily`, `FontSize` do inherit.) |
| **Fixed element set** | Only the elements in [elements/index.md](elements/index.md) exist. No `<textarea>`, `<form>`, `<header>`, `<footer>`, `<section>`, `<article>`, `<main>`, `<aside>`, `<em>`, `<br>`, `<hr>`, `<iframe>`, `<canvas>`, `<svg>`. Use `<div>`/`<span>`. |
| **Lengths are typed** | Use `Length.Px(16)`, `Length.Percent(50)`, `Length.Auto`; a bare `float` implicitly means px. |
| **Colors are typed** | `Color.FromHex("#512BD4")`, `new Color(r,g,b[,a])`; a bare hex `string` implicitly converts. |
| **Dispose Skia** | Wrap `SKSurface`/`SKBitmap` etc. in `using` when you touch them directly. |

## When to use what

- **Author a page** → drop a `.razor` file with `@page` in `Pages/`. See [razor.md](razor.md).
- **Style it** → add rules to the app's `StyleSheet` (built in C#), or set an inline `Style`. See [styling.md](styling.md).
- **Arrange it** → set `Display`/`FlexDirection`/etc. See [layout.md](layout.md).
- **Prebuilt widgets** → use `Miko.Ionic` (buttons, lists, tabs, cards). Call `builder.AddIonic()`. See [components/index.md](components/index.md).
- **No Razor** → build the `Element` tree directly and drive `MikoEngine`. See [project.md](project.md) "Direct engine API".

## Packages

| Package | Role |
| --- | --- |
| `Miko` | Core engine (platform-agnostic): DOM, styling, layout, rendering, events, routing, hosting. |
| `Miko.Razor.Compiler` | Source generator compiling `.razor` → elements + routes. Analyzer-only. |
| `Miko.Ionic` | Ionic-style Razor component library (iOS/Material modes). |
| `Miko.Bootstrap` | Bootstrap-style Razor component library. |
| `Miko.DevTools` | Runtime DOM/layout inspector (`builder.AddDevTools()`). |
| `Miko.Windowing` | Desktop host (Windows/Linux/macOS, Silk.NET). |
| `Miko.Android` / `Miko.iOS` | Mobile hosts. |
