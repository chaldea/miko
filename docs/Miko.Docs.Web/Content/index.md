---
layout: home
title: Miko
---

## Why Miko?

**vs. Electron / WebView UIs** — no embedded browser to ship, update, or sandbox;
dramatically smaller binaries and lower memory; everything runs in one managed .NET
process with optional Native AOT.

**vs. .NET MAUI** — Razor instead of XAML, one Skia renderer on every platform (the UI
looks identical across targets), and you can bring much of an existing Blazor component
codebase.

::: tip 1.0.0-preview.1
This is a preview release. APIs may change before the stable 1.0 release. See [Known Limitations](./guide/known-limitations.md) for the current feature boundaries.
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

var engine = new MikoEngineBuilder().Build();
engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);
engine.Render(canvas);
```

Head to the [Getting Started](./guide/getting-started.md) guide to scaffold your first app.
