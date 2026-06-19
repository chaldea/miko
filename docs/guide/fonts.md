# Fonts & Text

Miko has a font subsystem that supports **multiple font families with fallback**,
**mixed Latin/CJK text**, and **custom font loading** — including **WOFF2**, which it
decodes itself because SkiaSharp cannot load WOFF2 directly.

## Specifying fonts in styles

`FontFamily` accepts a comma-separated list, just like CSS. The families are tried in
order, and any glyph missing from the first family falls back to the next:

```csharp
new Style
{
    FontFamily = "Inter, Microsoft YaHei, Arial",
    FontSize = Length.Px(14),
    FontWeight = FontWeight.SemiBold
};
```

This is what makes mixed Latin/CJK text render correctly: a Latin font for the Latin runs
and a CJK font for the CJK runs, resolved per glyph.

## The default fallback chain

When a requested family (or a glyph within it) is unavailable, Miko falls back through a
built-in chain that covers Latin and CJK scripts:

```text
Arial → Segoe UI → Microsoft YaHei → SimSun → MS Gothic → Malgun Gothic
```

(Chinese: Microsoft YaHei / SimSun; Japanese: MS Gothic; Korean: Malgun Gothic.)

## Registering custom fonts

`FontManager` is a singleton. Register a custom family from a file, a byte array, or a
stream, then use its name in any `FontFamily`:

```csharp
using Miko.Fonts;

// From a file path
FontManager.Instance.RegisterFont("MyFont", "/path/to/font.woff2");

// From bytes
byte[] data = File.ReadAllBytes("font.ttf");
FontManager.Instance.RegisterFont("MyFont", data);

// From a stream
using var stream = File.OpenRead("font.otf");
FontManager.Instance.RegisterFont("MyFont", stream);
```

Supported formats: **TTF, OTF, WOFF, and WOFF2**. WOFF2 files are decompressed by Miko's
built-in WOFF2 decoder before being handed to SkiaSharp.

## Registering fonts via the app builder

When building an app, prefer the `UseFonts` builder, which registers fonts from embedded
assembly resources:

```csharp
using System.Reflection;
using Miko.Hosting;

builder.UseFonts(fonts =>
{
    fonts.AddResource(
        familyName: "Inter",
        assembly: Assembly.GetExecutingAssembly(),
        resourceName: "MyApp.Resources.Fonts.Inter.woff2");
});
```

Mark the font file as an embedded resource in your project so the resource name resolves
at runtime, then reference `"Inter"` from your styles.

## Text measurement

Miko measures text with SkiaSharp's font metrics during layout (see
[Layout](/guide/layout)), so wrapping, line height, and box sizing account for the actual
rendered glyphs of the resolved font.
