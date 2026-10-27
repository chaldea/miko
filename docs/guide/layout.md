# Layout

Miko computes the size and position of every element with a CSS-style **box model** and
one of three **layout algorithms**, selected by the element's `Display` value.

## The box model

Every laid-out element has four nested boxes, from inside out:

```text
┌─────────────────────────── margin box ───────────────────────────┐
│  ┌──────────────────────── border box ─────────────────────────┐ │
│  │  ┌───────────────────── padding box ──────────────────────┐ │ │
│  │  │                                                         │ │ │
│  │  │                     content box                         │ │ │
│  │  │                                                         │ │ │
│  │  └─────────────────────────────────────────────────────────┘ │ │
│  └──────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────┘
```

- **Content box** — where text and child boxes live; sized by `Width` / `Height` (or by
  content).
- **Padding box** = content + `Padding`.
- **Border box** = padding box + border width.
- **Margin box** = border box + `Margin`.

`BoxSizing` controls whether `Width`/`Height` refer to the content box (`ContentBox`,
default) or the border box (`BorderBox`).

### Aspect ratio

`AspectRatio` (CSS `aspect-ratio`) gives a box a preferred width-to-height ratio. When one of
`Width` / `Height` is `auto`, layout derives it from the other — in the same pass, so a 16:9
media frame follows its container's width without any code measuring it first:

```csharp
new Style
{
    Width = Length.Percent(100),
    AspectRatio = new AspectRatio(16, 9)   // height = width × 9 / 16
};
```

| Value | Meaning |
| --- | --- |
| `AspectRatio.Auto` | Default. Only images and videos keep their natural ratio. |
| `new AspectRatio(16, 9)` / `new AspectRatio(0.5f)` | Width / height. Applies to the box chosen by `BoxSizing`. |
| `AspectRatio.AutoOr(16, 9)` | Use an image's or video's natural ratio, else 16 / 9. Always applies to the content box. |

- It has no effect when both `Width` and `Height` are set, or on plain inline boxes (`span`).
- A ratio with a zero or infinite side behaves as `Auto`.
- `Min*` / `Max*` sizes clamp both axes. A box that does not scroll grows taller to fit its
  content instead of overflowing; set `Overflow` to keep the ratio exactly.
- Flex and grid items derive their size from the ratio, and a ratio-derived height is a
  definite size, so children's `Height = Length.Percent(100)` resolves against it.
- On an `<img>` that has not loaded yet, it reserves the final space, so the page does not
  shift when the image arrives.

## Two-phase layout

Layout flows in two directions, like a browser:

```text
Constraints flow down   (parent → child): available space
Sizes flow up           (child → parent): content size requirements
```

A parent passes available-space constraints to each child; the child reports back the
size it needs; the parent then places the child and computes its own final size.

## Display types

The `Display` property selects the layout algorithm:

| Display | Behavior |
| --- | --- |
| `Block` | Children stack vertically, each on its own line. |
| `Inline` | Children flow horizontally and wrap onto new lines. |
| `InlineBlock` | Flows inline but lays its own contents out as a block. |
| `Flex` | Flexbox layout along a main and cross axis. |
| `InlineFlex` | Flows inline (shrink-to-fit) and lays its children out as flexbox. |
| `Grid` | Grid layout on explicit tracks (a subset of CSS grid, see below). |
| `Table` / `TableRow` / `TableCell` | Table layout. |
| `None` | Not laid out and not rendered. |

### Block layout

Children are stacked top-to-bottom. Width comes from constraints (or an explicit `Width`);
height is the sum of the children's heights (or an explicit `Height`).

### Inline layout

Children are arranged left-to-right into line boxes, wrapping to a new line when they run
out of horizontal space — this is how text flows and breaks.

### Flex layout

Flexbox arranges children along a **main axis** (set by `FlexDirection`) and a **cross
axis**, distributing free space and aligning items:

```csharp
new Style
{
    Display = Display.Flex,
    FlexDirection = FlexDirection.Row,        // Row, RowReverse, Column, ColumnReverse
    JustifyContent = JustifyContent.SpaceBetween, // main-axis distribution
    AlignItems = AlignItems.Center,           // cross-axis alignment
    FlexWrap = FlexWrap.Wrap                   // allow wrapping
};
```

| Property | Values |
| --- | --- |
| `FlexDirection` | `Row`, `RowReverse`, `Column`, `ColumnReverse` |
| `JustifyContent` | `FlexStart`, `FlexEnd`, `Center`, `SpaceBetween`, `SpaceAround`, `SpaceEvenly` |
| `AlignItems` | `FlexStart`, `FlexEnd`, `Center`, `Stretch`, `Baseline` |
| `AlignContent` | `FlexStart`, `FlexEnd`, `Center`, `SpaceBetween`, `SpaceAround`, `Stretch` |
| `AlignSelf` | `Auto`, `FlexStart`, `FlexEnd`, `Center`, `Stretch`, `Baseline` |
| `FlexWrap` | `Nowrap`, `Wrap`, `WrapReverse` |

Per-item `FlexGrow` / `FlexShrink` distribute or absorb leftover space along the main axis, and
`FlexBasis` sets the starting size. `Gap` (or `RowGap` / `ColumnGap`) spaces the items.

A common app shell — a layout that fills the window, with the routed page taking the remaining
space:

```csharp
[".layout"]       = new() { Display = Display.Flex, Width = Length.Percent(100), Height = Length.Percent(100) },
[".main-content"] = new() { FlexGrow = 1, Display = Display.Flex },
```

### Grid layout

`Display.Grid` places children on rows and columns. `GridTemplateColumns` / `GridTemplateRows` are
lists of `GridTrackSize` entries, written like CSS track lists (a bare number means pixels):

| Entry | CSS |
| --- | --- |
| `GridTrackSize.Px(120)`, `GridTrackSize.Percent(25)`, `(GridTrackSize)Length.Em(10)` | `120px`, `25%`, `10em` |
| `GridTrackSize.Fr(1)` | `1fr` — a share of the remaining space |
| `GridTrackSize.Auto`, `GridTrackSize.MinContent`, `GridTrackSize.MaxContent` | `auto`, `min-content`, `max-content` |
| `GridTrackSize.MinMax(120, GridTrackSize.Fr(1))` | `minmax(120px, 1fr)` |
| `GridTrackSize.FitContent(Length.Px(300))` | `fit-content(300px)` |
| `GridTrackSize.Repeat(3, GridTrackSize.Fr(1))` | `repeat(3, 1fr)` |
| `GridTrackSize.Repeat(GridAutoRepeat.AutoFill, …)` / `GridAutoRepeat.AutoFit` | `repeat(auto-fill, …)` / `repeat(auto-fit, …)` |
| `GridTrackSize.LineNames("content-start")` | `[content-start]` — names the line at that position |

```csharp
// A responsive card grid: as many 160px-or-wider columns as fit.
new Style
{
    Display = Display.Grid,
    GridTemplateColumns = new List<GridTrackSize>
    {
        GridTrackSize.Repeat(GridAutoRepeat.AutoFill, GridTrackSize.MinMax(160, GridTrackSize.Fr(1))),
    },
    Gap = Length.Px(12),
}
```

- Tracks are sized like a browser does: `auto` and `fr` tracks never shrink below the narrowest
  their content can be (`1fr` is `minmax(auto, 1fr)`, so a long word or a wide image widens its
  column). Use `GridTrackSize.MinMax(0, GridTrackSize.Fr(1))` for columns that always split the
  space evenly. Items spanning several tracks grow the `auto` tracks they cross.
- `auto-fill` repeats the tracks as many times as fit the container (by its `Width`/`Height`, else
  `MaxWidth`/`MaxHeight`, else just enough to reach `MinWidth`/`MinHeight`, else once). Each repeated
  track needs a fixed length on at least one end. `auto-fit` does the same, then collapses the
  repeated tracks that stay empty, so the occupied ones take the space.
- `GridAutoRows` / `GridAutoColumns` size the tracks created outside the template, e.g.
  `GridAutoRows = GridTrackSize.MinMax(48, GridTrackSize.Auto)`.

`GridTemplateAreas` names regions of the grid, one string per row; `.` marks an empty cell, and the
cells of one name must form a rectangle. Children go into an area with `GridArea`:

```csharp
[".page"] = new()
{
    Display = Display.Grid,
    GridTemplateAreas = new GridTemplateAreas(
        "header header",
        "nav    main",
        "footer footer"),
    GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(200), GridTrackSize.Fr(1) },
},
[".page-header"] = new() { GridArea = "header" },
[".page-main"]   = new() { GridArea = "main" },
```

Children without a placement are placed automatically, row by row. Otherwise
`GridColumnStart` / `GridColumnEnd` / `GridRowStart` / `GridRowEnd` take a `GridLine`, and the
`GridColumn` / `GridRow` shorthands set both ends:

| Value | CSS | Meaning |
| --- | --- | --- |
| `GridLine.Line(2)`, `GridLine.Line(-1)` | `2`, `-1` | A line number, 1-based; negative numbers count from the end of the explicit grid. |
| `GridLine.Named("content-start")` | `content-start` | The first line with that name. An area name (`"main"`) means the area's start line on the start side and its end line on the end side. |
| `GridLine.Named("col", 2)` | `2 col` | The second line named `col`. |
| `GridLine.Span(2)`, `GridLine.Span("col")` | `span 2`, `span col` | Span two tracks, or up to the next line named `col`. |

```csharp
GridColumn = new GridPlacement(1, GridLine.Span(2)),          // grid-column: 1 / span 2
GridColumn = new GridPlacement("content-start", "content-end"),
GridRow = 3,                                                  // grid-row: 3
```

Lines outside the explicit grid add implicit tracks, sized by `GridAutoRows` / `GridAutoColumns`.
`JustifyItems` / `JustifySelf` and `AlignItems` / `AlignSelf` align items inside their cells;
`JustifyContent` / `AlignContent` distribute the tracks when the grid is smaller than its container.
Not supported: `grid-auto-flow: column` / `dense`, `subgrid`, and track lists in
`GridAutoRows` / `GridAutoColumns`. Because `MinWidth` / `MinHeight` default to `0` rather than
`auto`, setting them to `0` does not opt an item out of the content-based minimum — use
`MinMax(0, …)` tracks instead.

## Positioning

The `Position` property controls how an element is placed relative to the normal flow:

| Position | Behavior |
| --- | --- |
| `Static` | Normal flow (default). |
| `Relative` | Offset from its normal position by `Top` / `Right` / `Bottom` / `Left`. |
| `Absolute` | Positioned relative to the nearest positioned ancestor; removed from flow. |
| `Fixed` | Positioned relative to the viewport. |
| `Sticky` | Laid out in normal flow, then kept inside the visible area of its nearest scroll container while it scrolls. |

A `Sticky` element sticks once it would scroll past the edge given by `Top` / `Bottom` / `Left` /
`Right` (percentages refer to the scroll container's visible area; an element with none of them set
behaves like `Static`). It never leaves its parent's content box, so a section header is pushed out
when its section scrolls away and the next section's header takes over:

```csharp
[".section-title"] = new() { Position = Position.Sticky, Top = Length.Px(0) },
```

- The scroll container is the nearest ancestor with `Overflow` other than `Visible` — `Hidden`
  counts, as in CSS. Without one, the viewport is used.
- A stuck element paints above its sibling content and receives taps on top of it, and it is the
  containing block of its absolutely positioned descendants.

`Overflow` (`Visible`, `Hidden`, `Scroll`, `Auto`) controls clipping and scrolling of
content that exceeds the box.

## Scrollbars

How a scroll container's scrollbars look follows the platform, as `scrollbar-width: auto` does in
a browser:

| Platform | Default `ScrollbarStyle` | Behavior |
| --- | --- | --- |
| Windows, Linux | `Classic` | Track and thumb are always visible and take layout space. |
| macOS | `Overlay` | A thin thumb is drawn over the content without taking space. It appears while the container scrolls and fades out about a second after scrolling stops. |
| iOS, Android | `None` | No scrollbar is drawn. |

Whatever the style, an `Overflow.Auto` / `Overflow.Scroll` container still scrolls by wheel and
touch. `ScrollbarWidth` (`Auto`, `Thin`, `None`) still sets the thickness per container, and
`ScrollbarWidth.None` hides its scrollbars on every platform.

To change the defaults, configure the app builder:

```csharp
builder.UseScrollbarStyle(HostPlatform.MacOS, ScrollbarStyle.Classic); // one platform
builder.UseScrollbarStyle(ScrollbarStyle.None);                        // every platform
```

The style follows the platform the app currently runs as, so the device simulator switches
scrollbars together with the selected device. An engine built directly with `MikoEngineBuilder`
(no app builder) always uses `Classic`; set `MikoEngine.ScrollbarStyle` to change it.

## Sizing

`Width`, `Height`, `MinWidth`, `MaxWidth`, `MinHeight`, and `MaxHeight` take a `Length`
(`Length.Px`, `Length.Percent`, `Length.Em`, `Length.Rem`, `Length.Auto`). Leave them unset for
content-driven sizing. A percentage height needs a parent with a definite height — otherwise it
behaves like `auto`.

## Next steps

- [Styling](./styling.md) — set these properties via stylesheets and inline styles.
- [Pipeline Overview](../engine/overview.md) — where layout sits in the render pipeline.
