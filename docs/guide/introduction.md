# What is Miko?

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

::: warning Experimental
Miko is an experimental project under active development. APIs may change between versions.
:::

## Highlights

- 🪶 **Tiny & dependency-free** — ships as a few .NET assemblies plus SkiaSharp. No
  bundled Chromium, no Node, no WebView. Binaries are a fraction of an Electron app's.
- ⚡ **Native rendering** — pixels are drawn directly on a GPU-backed Skia surface, not
  inside a web engine.
- 🖥️ **Cross-platform** — one codebase runs on Windows, macOS, Linux, Android, and iOS,
  wherever SkiaSharp does.
- 🚀 **Trimming & AOT-aware** — the routing and rendering paths are designed for
  trimmed / Native AOT publishing for fast startup and small, self-contained binaries.
- 🔥 **Hot reload** — edit a `.razor` component and see the change without restarting.
- ⚛️ **Reuse your Blazor skills (and components)** — Razor components, layouts, routing,
  and data binding work the way you expect; many existing Blazor components can be
  recompiled to render natively with Miko instead of in a browser.

## Why Miko?

### vs. Electron / WebView-based UIs

- **No embedded browser.** There is no Chromium or system WebView to ship, update, or
  sandbox — Miko renders with SkiaSharp directly.
- **Smaller, lighter.** Dramatically smaller binaries and lower memory use; no separate
  renderer process.
- **Pure .NET.** Your UI logic, layout, and rendering all run in one managed process,
  with optional Native AOT for near-instant startup.

### vs. .NET MAUI

- **Razor instead of XAML.** Miko's layout DSL is Razor + a CSS-like styling model, so
  you build UIs with HTML/Razor knowledge instead of learning XAML and its binding syntax.
- **One renderer everywhere.** Miko draws its own widgets with Skia on every platform
  rather than wrapping each OS's native controls, so the UI looks and behaves identically
  across targets.
- **Bring your Blazor components.** Because the component model is Razor, much of an
  existing Blazor app can be recompiled to render natively with Miko — no browser required.

## Feature overview

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

## Where to next?

- New to Miko? Start with [Getting Started](/guide/getting-started).
- Want to understand how a frame is produced? See the
  [Pipeline Overview](/engine/overview).
- Prefer to skip Razor and build the DOM by hand? See
  [Using the Engine Directly](/engine/direct-api).
