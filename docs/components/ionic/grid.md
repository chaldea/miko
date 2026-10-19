# Grid

The grid is a 12-column flexbox layout system for arranging content in rows and columns.

`IonGrid` is the padded container, `IonRow` is a wrapping flex row, and `IonCol` is a column inside a row. Put `IonCol`s directly in an `IonRow` and rows in an `IonGrid`: the column sizing relies on the row being their flex container.

## Basic usage

Columns without a `Size` grow equally (`flex: 1 1 0`), so each row splits its width evenly between its columns.

<!-- miko:example examples/grid/Basic.razor -->
```razor
@* Columns without a Size share the row equally. *@
<IonGrid>
    <IonRow>
        <IonCol>1 of 2</IonCol>
        <IonCol>2 of 2</IonCol>
    </IonRow>
    <IonRow>
        <IonCol>1 of 3</IonCol>
        <IonCol>2 of 3</IonCol>
        <IonCol>3 of 3</IonCol>
    </IonRow>
</IonGrid>
```
<!-- /miko:example -->

## Column size

`Size` takes a column count from `1` to `12`; the column then takes `Size / 12` of the row width and does not grow or shrink. Unsized columns share whatever is left. A row whose sizes add up to more than 12 wraps onto a new line.

<!-- miko:example examples/grid/Sizes.razor -->
```razor
@* Size is a count out of 12 columns; unsized columns split what is left. *@
<IonGrid>
    <IonRow>
        <IonCol Size="6">Size 6 (half)</IonCol>
        <IonCol>Rest</IonCol>
        <IonCol>Rest</IonCol>
    </IonRow>
    <IonRow>
        <IonCol Size="3">Size 3</IonCol>
        <IonCol Size="9">Size 9</IonCol>
    </IonRow>
    <IonRow>
        @* More than 12 in a row wraps onto a new line. *@
        <IonCol Size="8">Size 8</IonCol>
        <IonCol Size="8">Size 8 (wraps)</IonCol>
    </IonRow>
</IonGrid>
```
<!-- /miko:example -->

## Auto-width columns

`Size="auto"` sizes the column to its content (`flex: 0 0 auto`), capped at the row width.

<!-- miko:example examples/grid/Auto.razor -->
```razor
<IonGrid>
    <IonRow>
        <IonCol>Fills</IonCol>
        <IonCol Size="auto">Sized to its content</IonCol>
        <IonCol>Fills</IonCol>
    </IonRow>
</IonGrid>
```
<!-- /miko:example -->

## Offset

`Offset` (1–12) adds a left margin of `Offset / 12` of the row width, pushing the column and everything after it to the right.

<!-- miko:example examples/grid/Offset.razor -->
```razor
<IonGrid>
    <IonRow>
        <IonCol Size="4" Offset="4">Offset 4</IonCol>
    </IonRow>
    <IonRow>
        <IonCol Size="3">Size 3</IonCol>
        <IonCol Size="3" Offset="3">Size 3, offset 3</IonCol>
    </IonRow>
</IonGrid>
```
<!-- /miko:example -->

## Push and pull

`Push` and `Pull` (1–12) shift a column right or left by `n / 12` of the row width with relative positioning. Unlike `Offset`, the other columns keep their places, so a push/pull pair swaps the visual order of two columns without changing the markup order.

<!-- miko:example examples/grid/PushPull.razor -->
```razor
@* Push/Pull move a column visually (position: relative); neighbours keep their slots. *@
<IonGrid>
    <IonRow>
        <IonCol Size="9" Push="3">First in markup, shown second</IonCol>
        <IonCol Size="3" Pull="9">Second in markup, shown first</IonCol>
    </IonRow>
</IonGrid>
```
<!-- /miko:example -->

Values outside 1–12 (or not a number) are ignored, as is a `Size` other than a number or `"auto"`.

## Fixed grid

`Fixed` caps the grid at the `FixedWidth` theme token (1140px by default) and centers it. Ionic varies the fixed width per breakpoint; this port uses the single maximum width.

<!-- miko:example examples/grid/Fixed.razor -->
```razor
@* Fixed caps the grid at the theme's FixedWidth (1140px by default) and centers it. *@
<IonGrid Fixed="true">
    <IonRow>
        <IonCol>1 of 2</IonCol>
        <IonCol>2 of 2</IonCol>
    </IonRow>
</IonGrid>
```
<!-- /miko:example -->

## Responsive sizes

Ionic's breakpoint-specific attributes (`size-md`, `offset-lg`, `push-sm`, …) are not ported: `Size`, `Offset`, `Push`, and `Pull` apply at every viewport width.

## API

<!-- miko:api IonGrid IonRow IonCol -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonGrid

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Fixed` | `bool` | `false` | When true, caps the grid at the `Grid.FixedWidth` theme token (1140px by default) and centers it (Ionic `fixed`; Ionic's per-breakpoint widths are not ported). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonRow

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonCol

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Offset` | `string?` | `null` | Left margin of this many twelfths of the row width, `"1"`–`"12"` (Ionic `offset`). Moves the following columns too. |
| `Pull` | `string?` | `null` | Shifts the column left by this many twelfths of the row width, `"1"`–`"12"` (Ionic `pull`), using relative positioning; other columns keep their places. |
| `Push` | `string?` | `null` | Shifts the column right by this many twelfths of the row width, `"1"`–`"12"` (Ionic `push`), using relative positioning; other columns keep their places. |
| `Size` | `string?` | `null` | Column size (Ionic `size`): a count from `"1"` to `"12"` (the column takes that many twelfths of the row and does not grow or shrink), or `"auto"` (sized to its content). When null, the column shares the remaining row width equally. Other values are ignored. Not breakpoint-aware. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

`IonCol` writes its sizing as inline style (flex basis, width, max-width, margin-left, left/right), merged over the `Style` you pass. Inline style beats any stylesheet rule, so set column widths through these parameters rather than with app CSS.

## Styling

### Theme tokens

<!-- miko:tokens Grid -->
Theme token `IonicTheme.Grid` (`GridToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Grid = new GridToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `ColumnPadding` | `float` | `5` | `5` |
| `FixedWidth` | `float` | `1140` | `1140` |
| `Padding` | `float` | `5` | `5` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-grid` | The grid container (padding from the `Padding` token). Also carries the mode (`md` / `ios`). |
| `.grid-fixed` | `Fixed`. |
| `.ion-row` | A row: `display: flex`, `flex-wrap: wrap`. |
| `.ion-col` | A column (padding from the `ColumnPadding` token). |

```csharp
// GlobalStyles.cs — a gutter-less grid: <IonGrid Class="flush">…</IonGrid>
[".ion-grid.flush"] = new() { Padding = new Padding(0) },
[".ion-grid.flush .ion-col"] = new() { Padding = new Padding(0) },
```
