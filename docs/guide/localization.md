# Localization & Right-to-Left

Miko apps are localized the same way as ASP.NET Core and Blazor apps: strings live in `.resx`
files and are read through `IStringLocalizer<T>`. The engine itself handles the text side:
right-to-left layout, mixed-direction text, and the shaping that Arabic, Indic, Thai and other
complex scripts need.

The [`examples/Localization`](../examples.md#localization) app puts all of this together: a
language picker switching between English, Chinese, Arabic, Hebrew and Hindi at run time.

## Translating strings

Add the `Microsoft.Extensions.Localization` package to the project that holds your UI, register it
in `App.cs`, and put one `.resx` file per language next to each other:

```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
```

```text
Resources/
  SharedResource.resx          ← default (English)
  SharedResource.zh-Hans.resx
  SharedResource.ar.resx
```

`SharedResource` is an empty marker class in the project's root namespace; the localizer finds
`Resources/SharedResource.<culture>.resx` from its name. Inject the localizer and index it by key:

```razor
@inject IStringLocalizer<SharedResource> L

<h1>@L["HomeTitle"]</h1>
<p>@L["ItemsInCart", count]</p>   @* "You have {0} items in your cart." *@
<span>@price.ToString("C")</span>  @* formatted for the current culture *@
```

A key missing from a language falls back to its parent culture (`zh-CN` → `zh-Hans` → default)
and finally to the key itself.

## Switching language at run time

The localizer and the formatting methods read `CultureInfo.CurrentUICulture` and
`CultureInfo.CurrentCulture`. To change language while the app runs, set the **process-wide
defaults** and re-render:

```csharp
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
```

Do not assign `CultureInfo.CurrentUICulture` in an event handler: it flows with the async
context and is restored when the handler returns. The defaults are what the render thread falls
back to.

Keep the current language in a singleton service with a `Changed` event, and let the layout
subscribe to it. When the layout re-renders, its page re-renders with it, so every string on
screen is looked up again:

```razor
@inherits LayoutComponentBase
@inject AppLanguage Language

<div class="app" dir="@(Language.IsRightToLeft ? "rtl" : "ltr")">
    <LanguagePicker />
    @Body
</div>

@code {
    protected override void OnInitialized() => Language.Changed += StateHasChanged;
    protected override void OnDispose() => Language.Changed -= StateHasChanged;
}
```

`CultureInfo.TextInfo.IsRightToLeft` tells you whether a culture is written right to left.

## Text direction

Set the direction with the `dir` attribute or the `Direction` style property. Both inherit, so
setting it once on the root of the app is enough:

```razor
<div dir="rtl">…</div>
```

```csharp
new Style { Direction = Direction.Rtl }
```

`dir` accepts `rtl` and `ltr` and works like the browser's: a style rule that sets `Direction`
overrides it. `dir="auto"` is not supported.

In a right-to-left element:

| What | Behavior |
| --- | --- |
| Text | Lines start at the right. `TextAlign.Start` (the default) is right, `TextAlign.End` is left. `Left` and `Right` stay physical. |
| Inline content | Words, inline elements and inline-blocks flow from right to left. |
| Flex rows | `FlexDirection.Row` starts at the right (`RowReverse` at the left). `JustifyContent.Start` / `End` follow the direction; `FlexStart` / `FlexEnd` follow the flex direction. |
| Flex columns | Items are aligned from the right (`AlignItems.FlexStart` is the right edge). |
| Blocks | A block narrower than its container (a fixed `Width` or `MaxWidth`, no `auto` margins) sits against the right edge. |
| Logical properties | `MarginInlineStart`, `PaddingInlineEnd`, `BorderInlineStart*`, `InsetInlineStart` … map to the right/left side (see [Styling](./styling.md#logical-properties)). |

Physical properties (`MarginLeft`, `PaddingRight`, `Left`, …) are **not** mirrored. Write styles
with logical properties and `TextAlign.Start` / `End`, and the same stylesheet serves both
directions.

## Mixed-direction text

Text is ordered with the Unicode Bidirectional Algorithm (UAX #9). In a right-to-left paragraph,
English words and numbers keep their left-to-right order; in a left-to-right paragraph, an
Arabic or Hebrew word reads right to left. Punctuation and brackets take the direction of the
text around them, and brackets are mirrored in right-to-left text.

```razor
<p dir="rtl">يعرض Miko 1.0 الأرقام (2026) في سطر واحد.</p>
```

Each paragraph is resolved as a whole; a `<br>` starts a new one. The text keeps its logical
order in the DOM and in `TextContent`; only its on-screen order changes.

## Complex scripts

Text in Arabic, Persian, Urdu, Hebrew, the Indic scripts (Devanagari, Bengali, Tamil, …), Thai,
Lao, Khmer, Myanmar and Tibetan is shaped with HarfBuzz: joined and contextual letter forms,
ligatures, conjuncts, reordered vowel signs and stacked marks. So are combining diacritics and
emoji sequences (ZWJ families, flags, skin tones). Latin, Greek, Cyrillic and CJK text without
combining marks keeps Miko's faster path.

Glyphs come from the fonts in `FontFamily`, then from the operating system's fallback for each
character (see [Fonts & Text](./fonts.md#font-fallback)). Windows, macOS, iOS and Android include
fonts for all of these scripts; on Linux install the Noto fonts. To get the same look on every
platform, register a font that covers the script and name it first:

```csharp
builder.UseFonts(fonts => fonts.AddResource("Noto Sans Arabic", typeof(App).Assembly,
    "MyApp.Resources.Fonts.NotoSansArabic-Regular.ttf"));
```

## Limitations

- **Line breaking** happens at spaces, between CJK characters and around inline-blocks. Thai, Lao,
  Khmer and Myanmar, which do not separate words with spaces, break only at spaces.
- **Text inputs in RTL**: the text and placeholder are right-aligned and shaped, and the caret is
  placed on the right character edge. Text longer than the field scrolls as in LTR, so the start
  of a long right-to-left value can be clipped while editing. Clicking places the caret at the end.
- **Not mirrored**: grid and table columns, absolutely positioned elements without offsets,
  scrollbars (they stay on the right) and multi-line `FlexWrap` in columns.
- **`text-align: justify`** is laid out like `start`.
- There is no `unicode-bidi` property and no `dir="auto"`.
