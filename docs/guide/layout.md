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

`Display.Grid` places children on explicit tracks. Tracks are `GridTrackSize` values:
`GridTrackSize.Px(120)`, `GridTrackSize.Percent(25)`, `GridTrackSize.Fr(1)`, `GridTrackSize.Auto`
(a bare number means pixels).

```csharp
new Style
{
    Display = Display.Grid,
    GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(200), GridTrackSize.Fr(1) },
    Gap = Length.Px(12),
}
```

Children are placed automatically, or explicitly with `GridColumnStart` / `GridColumnEnd` /
`GridRowStart` / `GridRowEnd` (1-based line numbers; `-1` is the last line). `JustifyItems` /
`JustifySelf` and `AlignItems` / `AlignSelf` align items inside their cells. `repeat()`,
`minmax()` and named areas are not supported.

## Positioning

The `Position` property controls how an element is placed relative to the normal flow:

| Position | Behavior |
| --- | --- |
| `Static` | Normal flow (default). |
| `Relative` | Offset from its normal position by `Top` / `Right` / `Bottom` / `Left`. |
| `Absolute` | Positioned relative to the nearest positioned ancestor; removed from flow. |
| `Fixed` | Positioned relative to the viewport. |

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
