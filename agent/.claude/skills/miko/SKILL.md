---
name: miko
description: Build UI for this Miko app. Use whenever adding or editing pages, layouts, components, or styles — Miko renders Razor with SkiaSharp (no browser/HTML/CSS runtime), so its element set, typed Style objects, and Ionic components differ from the web. Triggers on ".razor", "add a page", "style this", "Ionic", "IonButton/IonList/IonTabs", "layout/flexbox", "MikoAppBuilder".
---

# Building UI with Miko

This project uses **Miko** — a native .NET UI engine that compiles Razor components and draws them with SkiaSharp. It is *not* the web: there is no HTML/CSS runtime, styles are C# objects, and only a fixed set of elements exists.

## Before writing any UI, read the reference

The authoritative, agent-optimized docs live in [`.miko/`](../../../.miko/) at the project root. Start with [`.miko/llms.txt`](../../../.miko/llms.txt), then open the topic you need:

- **`.miko/overview.md`** — mental model + the hard constraints that differ from a browser. Read first.
- **`.miko/elements/index.md`** — the exact set of DOM elements. Check this before using any tag; there is **no** `<textarea>`, `<form>`, `<section>`, `<em>`, `<br>`, `<hr>`.
- **`.miko/components/index.md`** — the `Miko.Ionic` components (`IonButton`, `IonList`, `IonTabs`…) and their parent/child rules.
- **`.miko/styling.md`** — the typed `Style` object, `Length`, `Color`, `StyleSheet` + `CssObject`. Styles are **C# objects, not CSS strings**.
- **`.miko/layout.md`**, **`.miko/events.md`**, **`.miko/razor.md`**, **`.miko/project.md`** — layout, events, Razor authoring, and app wiring.
- **`.miko/examples/`** — copy-pasteable counter / tabs / sidemenu patterns.

## Workflow

1. **Add a page** → create `Pages/Foo.razor` with `@page "/foo"`. Routes are discovered automatically.
2. **Style it** → add rules to `GlobalStyles.cs` (a `StyleSheet` built with `CssObject`), or set an inline `Style` object. Never write `style="color:red"` strings.
3. **Use a tag you're unsure about** → confirm it exists in `.miko/elements/index.md`. If not, use the listed substitute (usually `<div>`/`<span>`).
4. **Reach for a widget** (button variants, cards, tabs, menus) → use Ionic (`.miko/components/index.md`); ensure `builder.AddIonic()` is in `App.cs`.
5. **Wire the app** → `App.cs` uses `MikoAppBuilder`; see `.miko/project.md`.

## Guardrails

- Styles are typed C# (`new Style { BackgroundColor = "#512BD4" }`), never CSS text.
- Miko has no `inherit` keyword for non-inherited properties — set values explicitly.
- Ionic components take a `Style` **object** (not a string) and require `AddIonic()` + `@using Miko.Ionic.Components`.
- Respect Ionic parent/child rules (e.g. `IonItem` inside `IonList`, `IonCol` inside `IonRow`).
