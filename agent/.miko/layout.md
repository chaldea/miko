# Layout

Miko sizes and positions every element with a CSS-style box model and one of several layout algorithms, chosen by the element's `Display`.

## Box model

Four nested boxes, inside out: **content → padding → border → margin**.

- Content box: text and child boxes; sized by `Width`/`Height` or by content.
- Padding box = content + `Padding`.
- Border box = padding box + border width.
- Margin box = border box + `Margin`.

`BoxSizing` = `ContentBox` (default; `Width`/`Height` = content box) or `BorderBox` (`Width`/`Height` = border box).

## Two-phase layout

Constraints flow **down** (parent → child: available space); sizes flow **up** (child → parent: content-size requirement). The parent places children, then computes its own size.

## `Display`

| Value | Behavior |
| --- | --- |
| `Block` | Children stack vertically, each on its own line. Width from constraints/`Width`; height = sum of children/`Height`. |
| `Inline` | Children flow horizontally and wrap onto new lines (how text flows). |
| `InlineBlock` | Flows inline; lays its own contents out as a block. |
| `Flex` | Flexbox along main + cross axis. |
| `Table` / `TableRow` / `TableCell` | Table layout. |
| `None` | Not laid out, not rendered. |

## Flexbox

```csharp
new Style
{
    Display = Display.Flex,
    FlexDirection = FlexDirection.Row,             // Row, RowReverse, Column, ColumnReverse
    JustifyContent = JustifyContent.SpaceBetween,  // main-axis distribution
    AlignItems = AlignItems.Center,                // cross-axis alignment
    FlexWrap = FlexWrap.Wrap,                       // allow wrapping
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

Per-item `FlexGrow` (distribute leftover main-axis space) and `FlexShrink` (absorb overflow). Common full-height fill:

```csharp
[".layout"]       = new() { Display = Display.Flex, Width = Length.Percent(100), Height = Length.Percent(100) },
[".main-content"] = new() { FlexGrow = 1, Display = Display.Flex },
```

## Positioning

| `Position` | Behavior |
| --- | --- |
| `Static` | Normal flow (default). |
| `Relative` | Offset from normal position via `Top`/`Right`/`Bottom`/`Left`. |
| `Absolute` | Relative to nearest positioned ancestor; removed from flow. |
| `Fixed` | Relative to the viewport. |

Set offsets with `Top`/`Right`/`Bottom`/`Left` (`Length`).

## Overflow

`Overflow` = `Visible` (default), `Hidden`, `Scroll`, or `Auto` — controls clipping and scrolling of content that exceeds the box.

## Sizing properties (quick list)

`Width`, `Height`, `MinWidth`, `MaxWidth`, `MinHeight`, `MaxHeight` — all `Length?`. Omit for content-driven sizing.
