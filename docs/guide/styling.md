# Styling

Miko styles elements with a **CSS-like model**: stylesheets made of selector → style
rules, a specificity-based cascade, and inline styles. There is no CSS text to parse —
you build styles with strongly typed C# objects.

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
| Pseudo-classes | `:hover`, `:active`, `:focus`, `:focus-within`, `:disabled`, `:enabled`, `:checked`, `:first-child`, `:last-child`, `:first-of-type`, `:last-of-type`, `:empty`, `:not(…)` |
| Pseudo-elements | `::before`, `::after`; `::range-thumb`, `::range-track`, `::range-progress` on `<input type="range">` |

An unsupported pseudo-class (for example `:nth-child()`) throws when the stylesheet is built.

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

In `.razor` markup, `style` binds a `Style` **object**, not a CSS string — there is no CSS text
parser. Component libraries take the same object through their `Style` parameter.

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
