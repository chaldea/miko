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

See [Layout](/guide/layout) for how the content / padding / border / margin boxes are
computed.

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

## The cascade

When multiple rules match an element, Miko resolves the final value by specificity, from
lowest to highest precedence:

```text
Tag  →  Class  →  ID  →  inline
```

Inline styles (the element's own `Style`) always win, then id selectors, then class
selectors, then tag selectors. The result is a `ComputedStyle` with every property
resolved (including inherited values such as `Color` and `FontFamily`).

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

## Next steps

- [Layout](/guide/layout) — block, inline, and flex layout.
- [Fonts & Text](/guide/fonts) — typography and custom fonts.
