# Miko branding

`miko.svg` is the vector master, redrawn from the approved **M.** design in
ISSUE-167. It uses paths instead of fonts: indigo `#3851b8` on lavender `#e9e8ff`,
with a 48-unit corner radius on a 240-unit canvas. The reference screenshot is
not an input to the generator and is not distributed.

From the repository root:

```sh
dotnet run --project tools/Miko.Branding
dotnet run --project tools/Miko.Branding -- --check
```

Commit the generated assets with changes to the master. The generator also updates
the templates, checked-in starter examples and documentation/demo sites.

| Asset | Format / sizes |
| --- | --- |
| `miko.svg` | Scalable rounded logo |
| `miko.png` | 1024 px rounded logo, transparent corners |
| `icon.png` | 128 px NuGet package icon |
| `miko.ico`, desktop `app.ico` | 16, 24, 32, 48, 64, 128, 256 px |
| Site `favicon.ico` | 16, 32, 48 px; `logo.svg` is the scalable favicon and navigation logo |
| `apple-touch-icon.png` | 180 px opaque square, masked by the OS |
| Android fallback | 48, 72, 96, 144, 192 px |
| Android adaptive layers | 108, 162, 216, 324, 432 px; transparent foreground within the 66dp safe circle and solid lavender background |
| iOS app icons | Sizes from each `AppIcon.appiconset/Contents.json`; opaque squares without baked-in corners |
| iOS launch logo | 120, 240, 360 px transparent mark, centered on a lavender launch screen |

Every bitmap is rasterized from vector geometry at its own resolution with 4x
supersampling. There are no screenshot crops or upscaled raster inputs.
