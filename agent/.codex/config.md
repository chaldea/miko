# Codex configuration — Miko project

This project renders UI with **Miko**: Razor components compiled and drawn with SkiaSharp. There is no browser and no HTML/CSS runtime. Before writing or editing UI, read the Miko reference so you use the correct element set, typed styles, and components.

## Reference (read before coding)

The agent-optimized Miko docs live under [`.miko/`](../.miko/) at the project root. Entry point: [`.miko/llms.txt`](../.miko/llms.txt).

Priority order:

1. [`.miko/overview.md`](../.miko/overview.md) — mental model + hard constraints (no CSS text, no `inherit`, typed `Style`).
2. [`.miko/elements/index.md`](../.miko/elements/index.md) — the exact DOM element set. No `<textarea>`/`<form>`/`<section>`/`<em>`/`<br>`/`<hr>`; use the listed substitutes.
3. [`.miko/components/index.md`](../.miko/components/index.md) — Ionic components + parent/child rules (needs `builder.AddIonic()`).
4. [`.miko/styling.md`](../.miko/styling.md) — styles are **C# objects**, not CSS strings.
5. [`.miko/layout.md`](../.miko/layout.md), [`.miko/events.md`](../.miko/events.md), [`.miko/razor.md`](../.miko/razor.md), [`.miko/project.md`](../.miko/project.md).
6. [`.miko/examples/`](../.miko/examples/) — counter / tabs / sidemenu patterns.

## Rules

- Add a page: `Pages/Foo.razor` with `@page "/foo"` (routes auto-discovered).
- Style via `GlobalStyles.cs` (`StyleSheet` + `CssObject`) or an inline `Style` object. No `style="..."` strings.
- Confirm any tag exists in `.miko/elements/index.md` before using it.
- Ionic components need `AddIonic()` + `@using Miko.Ionic.Components`, take a `Style` object, and follow parent/child rules.
