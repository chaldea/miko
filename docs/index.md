---
layout: home

hero:
  name: Miko
  text: Native UI for .NET, authored in Razor
  tagline: A cross-platform rendering engine that draws every pixel with SkiaSharp — no browser, no WebView, no HTML/CSS runtime.
  actions:
    - theme: brand
      text: Get Started
      link: /guide/getting-started
    - theme: alt
      text: What is Miko?
      link: /guide/introduction
    - theme: alt
      text: View on GitHub
      link: https://github.com/chaldea/miko

features:
  - icon: ⚛️
    title: Razor as the UI DSL
    details: Author your UI as .razor components — the same syntax you know from Blazor. A source generator compiles them ahead of time into native draw calls. No XAML to learn.
  - icon: ⚡
    title: Native SkiaSharp rendering
    details: Pixels are drawn directly onto a GPU-backed Skia surface through a browser-like pipeline of style cascade, layout, and incremental painting.
  - icon: 🪶
    title: Tiny & dependency-free
    details: Ships as a few .NET assemblies plus SkiaSharp. No bundled Chromium, no Node, no WebView. Binaries are a fraction of an Electron app's.
  - icon: 🖥️
    title: Cross-platform
    details: One codebase runs on Windows, macOS, Linux, Android, and iOS — everywhere SkiaSharp does.
  - icon: 🚀
    title: Trimming & AOT-aware
    details: The routing and rendering paths are designed for trimmed / Native AOT publishing, for fast startup and small, self-contained binaries.
  - icon: 🔥
    title: Hot reload
    details: Edit a .razor component and see the change without restarting the app.
---

## Why Miko?

**vs. Electron / WebView UIs** — no embedded browser to ship, update, or sandbox;
dramatically smaller binaries and lower memory; everything runs in one managed .NET
process with optional Native AOT.

**vs. .NET MAUI** — Razor instead of XAML, one Skia renderer on every platform (the UI
looks identical across targets), and you can bring much of an existing Blazor component
codebase.

::: tip Experimental
Miko is an experimental project under active development. APIs may change between versions.
:::

## A taste of the code

Author a page in Razor:

```razor
@page "/about"
@namespace MyApp.Pages

<h1>About</h1>
<p>Hello from Miko!</p>
```

…or drive the engine directly without Razor:

```csharp
var root = new DivElement { Class = "container" };
root.AddChild(new H1Element { TextContent = "Hello Miko" });

var engine = new MikoEngine();
engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);
engine.Render(canvas);
```

Head to the [Getting Started](/guide/getting-started) guide to scaffold your first app.
