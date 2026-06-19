# Pipeline Overview

Under the Razor layer, Miko runs a **browser-like rendering pipeline**. Whether you author
in Razor or build the DOM by hand, every frame goes through the same stages.

## The five stages

```text
1. DOM construction   →  build an Element tree (in code or via the Razor compiler)
2. Style computation  →  StyleResolver matches selectors and cascades styles
3. Layout tree build  →  filter elements by their display property
4. Layout             →  constraints flow down, sizes flow up
5. Paint              →  RenderEngine draws to an SKCanvas (dirty-region optimized)
```

1. **DOM construction.** An `Element` tree is created — either by your code or by the
   `.razor` source generator, which compiles components into element-building calls.
2. **Style computation.** `StyleResolver` matches each element against the stylesheets,
   sorts matches by specificity, and cascades them into a `ComputedStyle`. Cascade order
   is **Tag → Class → ID → inline** (see [Styling](/guide/styling)).
3. **Layout tree build.** Elements are filtered and grouped by their `Display` value into
   a layout tree (`Display.None` elements are excluded).
4. **Layout.** Each box is sized and positioned with the box model and the block / inline
   / flex algorithms — constraints flow down from parents, content sizes flow back up (see
   [Layout](/guide/layout)).
5. **Paint.** `RenderEngine` walks the layout tree and draws backgrounds, borders, text,
   and images to an `SKCanvas` via the `Painter`, repainting only dirty regions.

## Layer responsibilities

| Layer | Responsibility |
| --- | --- |
| **Core** | The `Element` tree and `MikoEngine`, which coordinates layout and rendering. |
| **Styling** | Stylesheets, selectors, the cascade, and computed styles. |
| **Layout** | The box model plus block, inline, and flex layout algorithms. |
| **Rendering** | `RenderEngine`, the SkiaSharp `Painter`, and the dirty-region manager. |
| **Fonts** | Font registration, fallback resolution, and text measurement. |
| **Events** | DOM-style event dispatch with capture and bubbling. |

## Incremental rendering

Repainting the whole canvas every frame is wasteful, so Miko tracks **dirty regions**:

- `MikoEngine.InvalidateElement(element)` marks an element (and its area) dirty.
- The `DirtyRegionManager` collects dirty rectangles and **merges** overlapping or
  adjacent ones into fewer, larger regions.
- On an incremental update, the `RenderEngine` clips to those regions and repaints only
  what intersects them, skipping unchanged areas.

This is why a small change — updating one label, toggling one button — costs roughly the
area of that element, not the whole window.

```text
Full render        →  MikoEngine.Render(canvas)
Incremental render →  MikoEngine.Update(canvas)   // dirty regions only
```

## Next steps

- [Using the Engine Directly](/engine/direct-api) — drive this pipeline without Razor.
- [Styling](/guide/styling) and [Layout](/guide/layout) — the stages in detail.
