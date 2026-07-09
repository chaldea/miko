# Styling

Miko styles elements with a CSS-like model, but **there is no CSS text**. You build styles as strongly typed C# objects: `Style` rules in a `StyleSheet`, matched by selectors, resolved by a specificity cascade, plus inline styles.

## The `Style` object

`Style` holds layout, box-model, visual, and positioning properties. Every property is **nullable** — `null` means "not set" (distinct from a real value) so the cascade works.

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
    BorderRadius = new BorderRadius(4),
};
```

## `Length` — sized values

```csharp
Length.Px(16)        // pixels
Length.Percent(50)   // % of containing block
Length.Auto          // automatic
Length.Rem(1.5f)     // × root font size
Length.Em(1.2f)      // × element font size
```

A bare `float` implicitly converts to px: `Width = 200` ≡ `Width = Length.Px(200)`.

| Unit | Meaning |
| --- | --- |
| `Px` | Absolute pixels. |
| `Percent` | % of containing block. |
| `Auto` | Computed automatically. |
| `Rem` | Multiple of root font size. |
| `Em` | Multiple of element's own font size. |
| `Number` | Unitless (e.g. unitless `line-height`: px = factor × font size). |

## `Color` — RGBA

```csharp
new Color(245, 245, 245)          // RGB (alpha = 255)
new Color(0, 0, 0, 128)           // RGBA (0..255)
Color.FromHex("#512BD4")          // hex
Color.FromRgb(13, 110, 253)       // RGB helper
Color.FromRgba(0, 0, 0, 0.5f)     // float alpha (0..1)
Color.White, Color.Black, Color.Transparent
```

A bare hex `string` implicitly converts: `BackgroundColor = "#512BD4"`.

## Box model & spacing

Struct constructors mirror CSS shorthand:

```csharp
new Padding(Length.Px(16))                              // all sides
new Padding(Length.Px(8), Length.Px(16))                // vertical, horizontal
new Padding(Length.Px(8), Length.Px(16), Length.Px(8))  // top, horizontal, bottom
new Padding(t, r, b, l)                                 // top, right, bottom, left

new Margin(Length.Px(8))                                // same shorthand shape
new BorderRadius(4)                                     // all corners (float → px)
new BorderRadius(t, r, b, l)                            // per-corner
```

`BoxSizing` picks whether `Width`/`Height` mean the content box (`ContentBox`, default) or the border box (`BorderBox`). See [layout.md](layout.md).

## Stylesheets & selectors

A `StyleSheet` is a list of `selector → Style` rules. Two ways to add rules.

### Selector objects — `AddRule`

```csharp
using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Common;

var sheet = new StyleSheet();

sheet.AddRule(new TagSelector("p"), new Style
{
    Color = new Color(60, 60, 60),
    FontSize = Length.Px(14),
});
sheet.AddRule(new ClassSelector("container"), new Style
{
    Display = Display.Flex,
    FlexDirection = FlexDirection.Column,
    Padding = new Padding(Length.Px(20)),
});
sheet.AddRule(new IdSelector("header"), new Style
{
    BackgroundColor = Color.FromHex("#512BD4"),
});
```

Selector building blocks: `TagSelector("div")`, `ClassSelector("container")`, `IdSelector("header")`, plus compound / combinator (descendant, child `>`, sibling `+`/`~`) and pseudo-class (`:hover`, `:focus`, `:active`, `:disabled`, `:checked`, `:first-child`, `:last-child`, `:nth-child()`) selectors.

### `CssObject` — string keys (recommended for app stylesheets)

Cleaner for many rules; keys are CSS-selector strings parsed by Miko:

```csharp
using Miko.Common;
using Miko.Styling;

var sheet = new StyleSheet();
sheet.Add(new CssObject
{
    [".home"] = new()
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        JustifyContent = JustifyContent.Center,
        AlignItems = AlignItems.Center,
        Padding = new Padding(24),
    },
    [".home-title"] = new()
    {
        FontSize = Length.Px(32),
        FontWeight = FontWeight.Bold,
        Color = Color.FromRgb(33, 37, 41),
    },
    [".counter-btn:hover"] = new()
    {
        BackgroundColor = Color.FromRgb(11, 94, 215),
    },
});
```

This is the shape generated apps use in `GlobalStyles.cs`.

## The cascade

When several rules match, the final value is resolved by specificity, lowest → highest:

```
Tag  →  Class  →  ID  →  inline
```

Inline styles (the element's own `Style`) always win. The result is a `ComputedStyle` with every property resolved.

**Inherited** properties (`Color`, `FontFamily`, `FontSize`, `FontWeight`, `LineHeight`, `TextAlign`) flow to descendants. All other properties do **not** inherit — Miko has no `inherit` keyword, so set them explicitly where needed.

## Inline styles

Highest precedence; set directly on an element / component:

```razor
<div class="container" style="@_boxStyle">…</div>
@code {
    private readonly Style _boxStyle = new() { BackgroundColor = Color.White };
}
```

In `.razor`, `style="@expr"` binds a `Style` **object** (not a CSS string). Ionic components also take a `Style` object via their `Style` parameter.
