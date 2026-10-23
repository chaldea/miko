# Icon

Icons display a glyph from the bundled Ionicons set, or a custom SVG, as a square box that scales with the font size and takes the surrounding text color.

`IonIcon` renders one icon. The `Ionicons` class (namespace `Miko.Ionic`) holds a constant for every bundled glyph.

## Basic usage

`Icon` takes an Ionicons name: use the `Ionicons` constants (`Ionicons.Heart` is `"heart"`) or the kebab-case string directly. Most glyphs come in three styles — filled (`heart`), `-outline` (`heart-outline`), and `-sharp` (`heart-sharp`); logos (`logo-*`) have a single style. An unknown name renders an empty box.

<!-- miko:example examples/icon/Basic.razor -->
```razor
@* Ionicons constants hold the kebab-case names; most icons come in filled, -outline and -sharp. *@
<IonIcon Icon="@Ionicons.Heart" />
<IonIcon Icon="@Ionicons.HeartOutline" />
<IonIcon Icon="@Ionicons.HeartSharp" />
<IonIcon Icon="logo-ionic" />
```
<!-- /miko:example -->

## Per-mode icons

`Ios` and `Md` choose a different glyph for each mode — for example an outline glyph on iOS and a sharp one on Material Design. When the current mode's property is unset, `Icon` is used.

<!-- miko:example examples/icon/Mode.razor -->
```razor
@* Ios / Md pick the glyph per mode; when the current mode's one is unset, Icon is used. *@
<IonIcon Ios="@Ionicons.ShareOutline" Md="@Ionicons.ShareSocialSharp" />
<IonIcon Icon="@Ionicons.SettingsSharp" Ios="@Ionicons.SettingsOutline" />
```
<!-- /miko:example -->

## Size

The icon box is `1em` × `1em`, so by default it is as large as the surrounding text. `Size="small"` and `Size="large"` set its font size to 18px and 32px; for any other size set `FontSize` (or `Width` and `Height`) through `Style` or an app stylesheet. Components that host icons — buttons, items, tabs, fab buttons — size their slotted icons themselves, often with a fixed `Width` / `Height`; override those properties to resize such an icon.

<!-- miko:example examples/icon/Size.razor -->
```razor
@* The icon box is 1em × 1em: Size picks 18px / 32px, or set FontSize for any other size. *@
<IonIcon Icon="@Ionicons.Star" Size="small" />
<IonIcon Icon="@Ionicons.Star" />
<IonIcon Icon="@Ionicons.Star" Size="large" />
<IonIcon Icon="@Ionicons.Star" Style="@(new Style { FontSize = Length.Px(48) })" />
```
<!-- /miko:example -->

## Color

Built-in glyphs are monochrome and painted in the element's text color, so an icon inherits the color of its parent. `Color` sets a palette color instead (see [Theming](./theming.md#palette)); a `Color` in `Style` sets any color.

<!-- miko:example examples/icon/Colors.razor -->
```razor
@* Built-in glyphs are tinted with the text color: a palette Color, or whatever color they inherit. *@
<IonIcon Icon="@Ionicons.Heart" Color="danger" />
<IonIcon Icon="@Ionicons.Star" Color="warning" />
<IonIcon Icon="@Ionicons.Leaf" Style="@(new Style { Color = Color.FromHex("#0e7490") })" />
```
<!-- /miko:example -->

## Custom SVG

`Src` loads an SVG file instead of a bundled glyph and takes precedence over `Icon`, `Ios`, and `Md`. A `res://` path names an embedded resource relative to the project root, with no assembly name (`res://Assets/brand-mark.svg` for `Assets/brand-mark.svg`); it is found in any assembly registered with `builder.AddResourceAssembly(...)` — the same lookup `<img>` uses. `file://` URIs and plain file paths are read from disk, and `data:` URIs are decoded; `http(s)` URLs are not loaded. A value in `Icon` that contains `/` or `.` is also treated as a source when no name resolves.

Custom SVGs are drawn with their own colors: `Color` and the text color do not tint them. A source that cannot be found renders an empty box.

<!-- miko:example examples/icon/Custom.razor -->
```razor
@* Src loads an SVG from an embedded resource (res://, path relative to the project root) or a
   file. External SVGs are drawn with their own colors, not tinted. *@
<IonIcon Src="res://Assets/brand-mark.svg" />
```
<!-- /miko:example -->

## API

<!-- miko:api IonIcon -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonIcon

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class the stylesheet maps to the icon's text color, which tints the monochrome glyph (CSS `fill: currentColor`). |
| `Icon` | `string?` | `null` | The icon to render — Ionic's `icon` prop, a combination of `name` and `src`. A src-looking value (containing `/` or `.`, e.g. `"res://Assets/avatar.svg"`) is loaded as an external source; anything else is treated as a kebab-case Ionicons name (see `Ionicons` for constants), rendered from the bundled SVG set. |
| `Ios` | `string?` | `null` | Ionicons name to use in `ios` mode (Ionic `ios`). Overrides [`Icon`](#ionicon) when the active mode is iOS. |
| `Md` | `string?` | `null` | Ionicons name to use in `md` mode (Ionic `md`). Overrides [`Icon`](#ionicon) when the active mode is Material Design. |
| `Size` | `string?` | `null` | Size (Ionic `size`): `"small"` or `"large"`. The icon box is 1em × 1em, so the size classes drive it through the font size. |
| `Slot` | `string?` | `null` | Named slot the icon occupies in its parent (Ionic's `slot` attribute), e.g. `"icon-only"`, `"start"`, `"end"`. Stamped as an `ion-slot-*` marker class so a parent (e.g. [`IonButton`](./button.md#ionbutton)) can detect it via class lookup — mirroring Ionic's `querySelector('[slot="…"]')`. |
| `Src` | `string?` | `null` | Exact source of an SVG file to use (Ionic `src`), e.g. `"res://…"`, `"file://…"` or a bare path. Takes precedence over [`Icon`](#ionicon), [`Ios`](#ionicon) and [`Md`](#ionicon). Embedded resources are resolved through the resource assemblies registered at startup (`builder.AddResourceAssembly(...)`); `http(s)` URLs are not loaded. The SVG is drawn with its own colors (not tinted by [`Color`](#ionicon)). |
<!-- /miko:api -->

`Slot` exists for parity with Ionic's `slot` attribute; to place an icon in a component, use that component's named slot instead (for example `<Start>` in [`IonItem`](./item.md) or `<IconOnly>` in [`IonButton`](./button.md#icons)).

## Styling

`IonIcon` has no theme token of its own.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-icon` | The icon box (`inline-block`, `1em` × `1em`). Also carries the mode (`md` / `ios`). |
| `.icon-small`, `.icon-large` | `Size`. |
| `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |
| `.ion-slot-{slot}` | `Slot` (e.g. `.ion-slot-start`). |

```csharp
// GlobalStyles.cs — a muted icon style: <IonIcon Icon="@Ionicons.Time" Class="hint" />
[".ion-icon.hint"] = new()
{
    FontSize = Length.Px(14),
    Color = Color.FromHex("#636469"),
},
```
