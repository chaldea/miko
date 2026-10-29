# Styling

Miko styles elements with a **CSS-like model**: stylesheets made of selector → style
rules, a specificity-based cascade, and inline styles. You build styles with strongly typed C#
objects; CSS written in components is translated into the same objects when they compile (see
[CSS in Razor Components](./css.md)).

## The `Style` object

A `Style` holds layout, box-model, visual, and positioning properties. Properties are
**nullable** so that "not set" (`null`) can be distinguished from a real value during the
cascade.

```csharp
using Miko.Common;
using Miko.Styling;

var style = new Style
{
    Display = Display.Flex,
    FlexDirection = FlexDirection.Row,
    Width = Length.Percent(100),
    Padding = new Padding(Length.Px(16)),
    BackgroundColor = new Color(255, 255, 255),
    Color = Color.Black,
    FontSize = Length.Px(14),
    FontWeight = FontWeight.SemiBold,
    BorderRadius = new BorderRadius(4)
};
```

## Lengths

`Length` represents a sized value and supports several units:

```csharp
Length.Px(16)        // pixels
Length.Percent(50)   // percentage of the parent
Length.Auto          // automatic
Length.Rem(1.5f)     // relative to the root font size
Length.Em(1.2f)      // relative to the element's font size
```

A bare `float` implicitly converts to pixels, so `Width = 200` is the same as
`Width = Length.Px(200)`.

| Unit | Meaning |
| --- | --- |
| `Px` | Absolute pixels. |
| `Percent` | Percentage of the containing block. |
| `Auto` | Computed automatically. |
| `Rem` | Multiple of the root font size. |
| `Em` | Multiple of the element's own font size. |
| `Number` | Unitless number (used for unitless `line-height`: pixels = factor × font size). |

## Colors

`Color` is an RGBA value. Construct it directly, from helpers, or from a hex string:

```csharp
new Color(245, 245, 245)              // RGB (alpha defaults to 255)
new Color(0, 0, 0, 128)               // RGBA
Color.FromHex("#512BD4")              // hex
Color.FromRgba(0, 0, 0, 0.5f)         // float alpha (0..1)
Color.White, Color.Black, Color.Transparent
```

A `string` implicitly converts to a color via hex, so `BackgroundColor = "#512BD4"`
works too.

## Box model & spacing

`Padding`, margins, borders, and `BorderRadius` follow CSS conventions. The struct
constructors mirror CSS shorthand:

```csharp
new Padding(Length.Px(16))                         // all sides
new Padding(Length.Px(8), Length.Px(16))           // vertical, horizontal
new Padding(Length.Px(8), Length.Px(16), Length.Px(8))   // top, horizontal, bottom
new Padding(t, r, b, l)                            // top, right, bottom, left

new BorderRadius(4)                                // all corners (float → px)
new BorderRadius(t, r, b, l)                       // per-corner
```

See [Layout](./layout.md) for how the content / padding / border / margin boxes are
computed.

## Logical properties

Miko supports CSS logical properties for internationalization-friendly styles
(`margin-inline-start`, `inline-size`, etc.). Logical values are the canonical storage in
`ComputedStyle`: physical properties are resolved onto them during style computation
using the element's `Direction` and `WritingMode`, and the physical computed properties
(`MarginLeft`, `Width`, …) read back through that mapping — so layout and painting code
is unchanged and existing physical styles behave exactly as before.

| Physical                | Logical                                  |
| ----------------------- | ---------------------------------------- |
| `MarginLeft/Right`      | `MarginInlineStart/End`                  |
| `MarginTop/Bottom`      | `MarginBlockStart/End`                   |
| `PaddingLeft/Right`     | `PaddingInlineStart/End`                 |
| `PaddingTop/Bottom`     | `PaddingBlockStart/End`                  |
| `BorderLeft/Right`      | `BorderInlineStart/End`                  |
| `BorderTop/Bottom`      | `BorderBlockStart/End`                   |
| `Left/Right`            | `InsetInlineStart/End`                   |
| `Top/Bottom`            | `InsetBlockStart/End`                    |
| `Width` / `Height`      | `InlineSize` / `BlockSize`               |
| `MinWidth` / `MinHeight`| `MinInlineSize` / `MinBlockSize`         |
| `MaxWidth` / `MaxHeight`| `MaxInlineSize` / `MaxBlockSize`         |

Border logical sides expose the same width/color/style triplet as physical sides
(`BorderInlineStartWidth`, `BorderInlineStartStyle`, `BorderInlineStartColor`, …), and
the unified `BorderWidth`/`BorderColor`/`BorderStyle` fallbacks fill any logical side
that was not set explicitly. Shorthands `MarginInline`, `MarginBlock`, `PaddingInline`,
`PaddingBlock`, `InsetInline`, `InsetBlock`, `BorderInline`, and `BorderBlock` set both
sides of an axis at once.

```csharp
new Style
{
    MarginInlineStart = Length.Px(16),  // left under ltr, right under rtl
    PaddingBlock = Length.Px(8),        // top + bottom under horizontal-tb
    InlineSize = Length.Px(200),        // width under horizontal-tb
}
```

The mapping is driven by two inheritable properties:

- `Direction` — `Ltr` (default) or `Rtl`. Flips the inline axis: under `Rtl`,
  `MarginInlineStart` maps to the physical right side.
- `WritingMode` — `HorizontalTb` (default), `VerticalRl`, or `VerticalLr`. Swaps the
  axes: under vertical modes `InlineSize` maps to `Height`, and the block axis runs
  horizontally (`BlockStart` = right for `VerticalRl`, left for `VerticalLr`).

If the same style sets both a physical property and its logical counterpart, the logical
property wins.

## Selectors and stylesheets

A `StyleSheet` is a list of rules, each pairing a selector with a style. Add rules with
`AddRule`:

```csharp
using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Common;

var styleSheet = new StyleSheet();

styleSheet.AddRule(new TagSelector("p"), new Style
{
    Color = new Color(60, 60, 60),
    FontSize = Length.Px(14)
});

styleSheet.AddRule(new ClassSelector("container"), new Style
{
    Display = Display.Flex,
    FlexDirection = FlexDirection.Column,
    Padding = new Padding(Length.Px(20)),
    BackgroundColor = new Color(245, 245, 245)
});

styleSheet.AddRule(new IdSelector("header"), new Style
{
    BackgroundColor = Color.FromHex("#512BD4")
});
```

Available selector building blocks include:

- `TagSelector("div")` — matches by element tag.
- `ClassSelector("container")` — matches by class.
- `IdSelector("header")` — matches by id.
- **Compound / combinator / pseudo-class** selectors (e.g. descendant, `:hover`) for more
  specific matching.

### `CssObject` — selector strings (recommended)

For an app stylesheet, `CssObject` is shorter: keys are CSS selector strings that Miko parses,
values are `Style` objects. This is the shape the app templates use in `GlobalStyles.cs`:

```csharp
using Miko.Common;
using Miko.Styling;

namespace MyApp;

internal static class GlobalStyles
{
    public static StyleSheet Create()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".layout"] = new() { Display = Display.Flex, Width = Length.Percent(100), Height = Length.Percent(100) },
            [".home"] = new()
            {
                FlexGrow = 1,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Padding = new Padding(24),
            },
            [".home-title"] = new() { FontSize = Length.Px(32), FontWeight = FontWeight.Bold },
            [".counter-btn"] = new()
            {
                Padding = new Padding(10, 20),
                BackgroundColor = Color.FromRgb(13, 110, 253),
                Color = Color.White,
                BorderRadius = new BorderRadius(Length.Px(6)),
                Cursor = Cursor.Pointer,
            },
            [".counter-btn:hover"] = new() { BackgroundColor = Color.FromRgb(11, 94, 215) },
        });
        return sheet;
    }
}
```

Register it in `App.cs` with `builder.AddStyleSheet(GlobalStyles.Create());`.

Selector strings support:

| Kind | Syntax |
| --- | --- |
| Simple | `div`, `.class`, `#id`, compound `.a.b` / `button.primary` |
| Combinators | descendant (`.a .b`), child (`>`), adjacent sibling (`+`), general sibling (`~`) |
| Attributes | `[href]`, `[type="checkbox"]`, `~=`, `^=`, `$=`, `*=`, … |
| Pseudo-classes | `:hover`, `:active`, `:focus`, `:focus-within`, `:disabled`, `:enabled`, `:checked`, `:first-child`, `:last-child`, `:only-child`, `:first-of-type`, `:last-of-type`, `:only-of-type`, `:empty`, `:root` (an element without a parent: the root of the tree) |
| Structural | `:nth-child(An+B)`, `:nth-last-child(An+B)` (both also `An+B of S`), `:nth-of-type(An+B)`, `:nth-last-of-type(An+B)` |
| Logical | `:is(…)`, `:where(…)`, `:not(…)`, `:has(…)`, each taking a comma-separated list |
| Pseudo-elements | `::before`, `::after`; `::placeholder` on text `<input>` and `<textarea>`; `::range-thumb`, `::range-track`, `::range-progress` on `<input type="range">` |

An unsupported pseudo-class (for example `:visited`) throws when the stylesheet is built. So does an
invalid selector inside `:is()` or `:where()`: browsers skip it, but a Miko stylesheet is code, so a
typo should fail rather than silently drop a branch. (CSS written in components is checked when it
compiles instead, see [CSS in Razor Components](./css.md).)

### Structural pseudo-classes

`:nth-child(An+B)` matches an element whose position among its siblings (counting from 1) is
`A×n + B` for some `n ≥ 0`. `odd` means `2n+1`, `even` means `2n`, and a plain number picks one
position. Text between elements is not counted.

```csharp
sheet.Add(new CssObject
{
    [".list > .row:nth-child(even)"] = new() { BackgroundColor = Color.FromRgb(245, 245, 245) },
    [".list > .row:nth-child(-n+3)"] = new() { FontWeight = FontWeight.Bold },   // first three
    [".list > .row:nth-last-child(1)"] = new() { BorderBottomWidth = Length.Px(0) }, // last one
    ["td:nth-of-type(2)"] = new() { TextAlign = TextAlign.End },                  // second cell
    [".tab:nth-child(1 of .visible)"] = new() { MarginLeft = Length.Px(0) },       // first visible tab
});
```

`:nth-last-child()` and `:nth-last-of-type()` count from the end. The `-of-type` variants count only
siblings with the same tag. `An+B of S` counts only the siblings that match `S`, and the element itself
must match `S`: `:nth-child(2 of .item)` is the second `.item`, not an `.item` in second place.

### `:is()`, `:where()`, `:not()` and `:has()`

- `:is(…)` matches when any selector in the list matches, so `:is(h1, h2, .title)` replaces three
  rules. The arguments may contain combinators (`:is(.toolbar .title, h1)`).
- `:where(…)` matches exactly like `:is(…)` but adds nothing to specificity, which makes it the way to
  write defaults that any other rule overrides: `:where(.card) .title` weighs as much as `.title`.
- `:not(…)` matches when no selector in the list matches: `.btn:not(.primary, .danger)`.
- `:has(…)` matches an element when a *relative* selector, starting from it, finds an element:
  `.card:has(img)` (any descendant), `.card:has(> img)` (a child), `label:has(+ input)` (the next
  sibling), `h2:has(~ p)` (any later sibling), `.list:has(> .item .badge)` (several steps).

`:has()` walks the element's subtree, or its later siblings, each time it is evaluated. Put it in a
compound with a class, as in `.card:has(…)`, so only `.card` elements pay for the walk. A bare
`:has(…)` is evaluated against every element.

Pseudo-elements are not allowed inside any of these arguments.

### Styling placeholders

`::placeholder` styles the placeholder text of a text `<input>` or a `<textarea>`. It inherits from
the field, so it uses the field's font unless the rule sets one. Placeholders are gray by default,
and a rule that does not set `Color` keeps the gray.

```csharp
sheet.Add(new CssObject
{
    [".search::placeholder"] = new() { Color = Color.FromRgb(120, 120, 140), FontWeight = FontWeight.Light },
    [".search:disabled::placeholder"] = new() { Opacity = 0.5f },
});
```

The placeholder honours `Color`, `Opacity`, `FontFamily`, `FontSize` and `FontWeight`. Its rules
cascade like any other rule (layer, then specificity, then order).

## The cascade

When multiple rules match an element, Miko resolves the final value by specificity, from
lowest to highest precedence:

```text
Tag  →  Class  →  ID  →  inline
```

Inline styles (the element's own `Style`) always win, then id selectors, then class
selectors, then tag selectors. Between rules of equal specificity, the one added later wins. The
result is a `ComputedStyle` with every property resolved (including inherited values such as
`Color` and `FontFamily`).

Pseudo-classes and attribute selectors weigh as much as a class. `:is()`, `:not()` and `:has()` weigh
as much as their most specific argument, `:where()` weighs nothing, and `:nth-child(An+B of S)` weighs
a class plus the most specific selector in `S`.

### Cascade layers

`StyleSheet.Layer` (default `0`) works like CSS `@layer`: a rule from a sheet with a higher layer
beats any rule from a lower layer, whatever their specificity. Component libraries register their
sheets below the app — `Miko.Ionic` uses layer `-1` — so a rule in your app stylesheet always
overrides a component's own styling.

## Inheritance and CSS-wide keywords

These properties inherit from the parent element by default: `Color`, `CaretColor`,
`FontFamily`, `FontSize`, `FontWeight`, `LineHeight`, `TextAlign`, `TextTransform`,
`LetterSpacing`, `WhiteSpace`, `OverflowWrap`, `WordBreak`, `Visibility`, `UserSelect`,
`PointerEvents`, `Direction`, and `WritingMode`. Every other property starts from its initial
value on each element.

Any property also accepts the CSS-wide keywords, from `Miko.Styling.Css` (or the
`StyleKeyword` enum):

```csharp
using static Miko.Styling.Css;

sheet.Add(new CssObject
{
    [".card .title"] = new() { Width = Inherit },   // take the parent's computed width
    [".reset"] = new() { Color = Initial },         // back to the initial value
    [".plain"] = new() { MarginTop = Unset },       // inherit if inheritable, else initial
});
```

`Inherit` works for non-inherited properties too. `Revert` and `RevertLayer` behave like `Unset`.

## Applying styles

Pass stylesheets to the engine (or register them on the app builder with
`AddStyleSheet` / `UseStyleSheets`), and set inline styles directly on an element:

```csharp
var box = new DivElement
{
    Class = "container",
    Style = new Style { BackgroundColor = Color.White } // inline — highest precedence
};
```

## Inline styles

In `.razor` markup, `style` binds a `Style` object, or takes CSS that the compiler translates into
one (`style="margin: 15px"`, see [CSS in Razor Components](./css.md#inline-styles)). Component
libraries take a `Style` object through their `Style` parameter.

```razor
<div class="container" style="@_boxStyle">…</div>
<IonButton Style="@_buttonStyle">Save</IonButton>

@code {
    private readonly Style _boxStyle = new() { BackgroundColor = Color.White, Padding = new Padding(16) };
    private readonly Style _buttonStyle = new() { MarginTop = Length.Px(24) };
}
```

## Next steps

- [Layout](./layout.md) — block, inline, and flex layout.
- [Fonts & Text](./fonts.md) — typography and custom fonts.
