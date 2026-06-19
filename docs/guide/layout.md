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

Per-item `flex-grow` / `flex-shrink` distribute or absorb leftover space along the main
axis.

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

## Next steps

- [Styling](/guide/styling) — set these properties via stylesheets and inline styles.
- [Pipeline Overview](/engine/overview) — where layout sits in the render pipeline.
