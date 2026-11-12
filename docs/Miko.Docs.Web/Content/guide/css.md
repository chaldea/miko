# CSS in Razor Components

Miko's styles are typed C# objects (see [Styling](./styling.md)), but components can also be
written with CSS — so markup ported from Blazor keeps its look. The Razor compiler **translates
the CSS into C# when the component compiles**: nothing parses CSS at run time, and anything Miko
cannot represent is reported as a build warning instead of being silently ignored.

There are three places to write CSS:

| Where | Applies to | Compiled into |
|---|---|---|
| `style="margin: 15px"` on an element | that element (inline style) | a `Style` object |
| a `<style>` element in a component | the whole app, like a page stylesheet | the component, registered on its first render |
| `Button.razor.css` next to `Button.razor` | `Button`'s own elements only (CSS isolation) | the component, registered on its first render |

## Inline styles

```razor
<div style="display: flex; gap: 8px; padding: 4px 8px; background-color: #0054e9"></div>
```

compiles to the same code as writing the `Style` by hand:

```csharp
new Style
{
    Display = Display.Flex, RowGap = Length.Px(8), ColumnGap = Length.Px(8),
    PaddingTop = Length.Px(4), PaddingRight = Length.Px(8), /* … */
    BackgroundColor = new Color(0, 84, 233),
}
```

`style="@_style"` still binds a `Style` object, as before.

### Values from C#

A C# expression can supply a **length** (followed by its unit), a **number** or a **color**:

```razor
<div style="width: @(Progress)%; opacity: @Alpha; color: @Accent; height: calc(100% - @(Header)px)"></div>
```

- A length needs a unit after the expression: `@(w)px`, `%`, `em`, `rem`, `vw`, `vh`. The value may
  be any number type or a numeric string.
- A color is a `Color`, or a hex string (`"#0054e9"`).
- Keywords (`display: @mode`) cannot come from C# — the compiler reports it. Bind a `Style`
  object, or switch classes, for those.

A `style` value built entirely at run time (`style="@CssString"`) is not CSS Miko can see, and is
ignored.

## `<style>` elements

```razor
<div class="card">…</div>

<style>
    .card { padding: 16px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0, 0, 0, .2); }
    .card:hover { background-color: #f4f5f8; }

    @@media (max-width: 600px) {
        .card { padding: 8px; }
    }
</style>
```

- The element is removed from the markup; its rules join the app's styles the first time the
  component renders, and stay for the life of the app — as a stylesheet the browser has loaded.
- In a `.razor` file write `@@media` and `@@keyframes`: Razor reads a single `@` as C#.
- The rules are compiled once per component type, so they cannot contain C# expressions. A
  `<style>` inside `@if` is registered regardless of the condition.
- `<style media="(min-width: 600px)">` wraps its rules in that media query.

## `.razor.css` files (CSS isolation)

A file named after the component — `Pages/Counter.razor.css` for `Pages/Counter.razor` — holds
styles for that component only, as Blazor's CSS isolation does:

```css
/* Counter.razor.css */
.count { font-size: 32px; }              /* only Counter's .count, not a child component's */
.panel ::deep .ion-item { margin: 0; }  /* ::deep reaches into child components */
```

Each element in the component gets the component's scope (`b-xxxxxxxxxx`), and every selector is
rewritten to require it on its last part (`.count[b-xxxxxxxxxx]`), or on the part before `::deep`.
Elements rendered by child components do not carry the parent's scope.

The `Miko.Razor.Compiler` package passes these files to the compiler. A project that wires the
compiler in by hand (an `_OverrideRazorSourceGenerator` target) must also import
`Miko.Razor.Compiler.targets`; otherwise the build warns with RZ10109.

## What is translated

Selectors: everything listed in [Styling](./styling.md#selectors-and-stylesheets) — type, class,
id, attribute, the four combinators, `:hover` and the other state pseudo-classes, `:root`,
`:only-child`, the structural and functional pseudo-classes, `::before`, `::after`,
`::placeholder`. `html` and `body` mean `:root`. A selector list is split into one rule per
selector, so each keeps its own specificity, and an unsupported selector only drops itself.

At-rules: `@media` (width, height, aspect-ratio and orientation, including the range syntax) and
`@keyframes` (used by `animation`).

Values: lengths in `px`, `em`, `rem`, `%`, `vw`, `vh` (and the absolute units, converted to
pixels), `calc()`, `var()` with a fallback, `env(safe-area-inset-*)`, hex / named / `rgb()` /
`hsl()` colors, and the CSS-wide keywords `inherit`, `initial`, `unset`, `revert`. Custom
properties (`--name: value`) hold a color, a length, a number or a string.

Properties:

- **Box**: `width`, `height`, `min-*`, `max-*`, the logical sizes, `margin`, `padding`, `inset`,
  `top` / `right` / `bottom` / `left` and their logical forms, `box-sizing`, `aspect-ratio`
- **Border**: `border` and every side / logical side, `border-width`, `-style`, `-color`,
  `border-radius` and its corners, `outline` and its longhands
- **Layout**: `display`, `position`, `z-index`, `overflow` (`-x`, `-y`), `visibility`, `flex` and its
  longhands, `flex-flow`, `gap`, `order`, the `justify-*` / `align-*` / `place-*` properties,
  `grid-template-columns` / `-rows` / `-areas`, `grid-template`, `grid-auto-rows` / `-columns`,
  `grid-row` / `grid-column` / `grid-area` and their longhands, `table-layout`
- **Text**: `color`, `font`, `font-family`, `font-size`, `font-weight`, `font-style`, `line-height`,
  `letter-spacing`, `text-align`, `text-transform`, `text-decoration` (the line),
  `text-overflow`, `white-space`, `word-break`, `overflow-wrap`, `vertical-align`, `direction`,
  `writing-mode`, `caret-color`, `content`
- **Visual**: `background` (color, repeat, position, size), `background-color`, `opacity`,
  `box-shadow`, `transform`, `transform-origin`, `cursor`, `pointer-events`, `user-select`,
  `scrollbar-width`
- **Motion**: `transition` and its longhands, `animation` and its longhands

Vendor-prefixed properties (`-webkit-…`) and properties with no visible effect (`will-change`,
`contain`, …) are dropped without a warning.

## Warnings

| Warning | Meaning |
|---|---|
| RZ10102 | The property is not supported (`float`, `filter`, `text-shadow`, `list-style`, …). |
| RZ10103 | The value cannot be translated (an image in `background`, `vmin`, `min()`, …); the declaration is ignored. |
| RZ10104 | The selector is not supported (`:visited`, `::selection`, `&` nesting, …); its rule is ignored. |
| RZ10105 | The at-rule is not supported (`@import`, `@font-face`, `@supports`, …). |
| RZ10106 | The CSS is invalid (a missing value, an unclosed block); parsing recovers as a browser does. |
| RZ10107 | A C# expression is used where Miko cannot take one. |
| RZ10108 | The declaration is applied approximately — `!important` (no precedence), `cursor: grab` (as `move`), a rounded font weight, … |
| RZ10109 | A `.razor.css` file did not reach the compiler (see above). |

Each warning points at the CSS in the `.razor` or `.razor.css` file.

## Differences from a browser

- **Cascade order.** Component rules come after the app's style sheets, in the order the components
  first rendered. Within a sheet, `@media` rules are ordered after the other rules.
- **Shorthands** set their longhands as in CSS, but an omitted color is Miko's initial color
  (black), not `currentcolor`, which Miko does not have.
- **`var()`** in a keyword property (`display: var(--d)`) is not resolved when the variable holds a
  keyword; custom properties are typed values.
- A component's `Style` **parameter** (`<IonButton Style="…">`) takes a `Style` object, not CSS:
  give the component a `Class` and style it from a `<style>` rule.

See [Known limitations](./known-limitations.md#css-subset) for the CSS features Miko's engine does
not have at all.
