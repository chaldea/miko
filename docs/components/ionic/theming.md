# Theming

Ionic components are themed with typed **tokens** — one token object per component (`ButtonToken`, `ToolbarToken`, …) plus a shared `PaletteToken` — instead of CSS variables. A theme can be set for the whole app, overridden for one subtree with `ConfigProvider`, and refined further with ordinary app stylesheet rules.

Every component page lists its token with the Material Design (`md`) and iOS (`ios`) defaults,
under **Styling › Theme tokens**.

## How a theme is resolved

`IonicTheme` holds every token. A token remembers which properties you **assigned**; everything
you leave alone keeps the default of the active mode. The effective value of a property is,
from lowest to highest priority:

1. the mode default (`md` or `ios`),
2. the app theme (`IonicOptions.Theme`, set through `AddIonic`),
3. each enclosing `ConfigProvider`, outermost first.

So always start from `new IonicTheme()` / `new ButtonToken { … }` and assign only what you want
to change. `IonicTheme.CreateMd()` and `IonicTheme.CreateIos()` return complete themes with every
value assigned, which would pin all values, including those that differ per mode.

## App-wide theme

```csharp
builder.AddIonic(options =>
{
    options.Theme.Palette = new PaletteToken { Primary = Color.FromHex("#0e7490") };
    options.Theme.Button = new ButtonToken
    {
        SolidBackground = Color.FromHex("#0e7490"),
        BorderRadius = 12,
    };
});
```

Assigning `0`, `Color.Transparent` or `null` is an override like any other value.

### From configuration

`IonicOptions` binds from configuration like any options type. The app loads the JSON itself:

```csharp
builder.Services.Configure<IonicOptions>(configuration.GetSection("Ionic"));
builder.AddIonic();
```

```json
{
  "Ionic": {
    "Platform": "Ios",
    "Theme": {
      "Button": {
        "SolidBackground": "#0e7490",
        "SolidColor": "#ffffff",
        "FontSize": 18,
        "MinHeight": "48px",
        "LetterSpacing": "0.05em"
      }
    }
  }
}
```

Colors are hex strings; lengths accept `px`, `rem`, `em`, `%`, `vw`, `vh`, `auto`,
`fit-content`, or a unitless number (pixels).

## Theme for a subtree — `ConfigProvider`

`ConfigProvider` applies a partial theme to everything inside it. Providers nest; the inner one
wins for the properties it sets.

```razor
<IonButton>App theme</IonButton>

<ConfigProvider IonicTheme="@_danger">
    <IonButton>Red button</IonButton>
</ConfigProvider>

@code {
    private readonly IonicTheme _danger = new()
    {
        Button = new ButtonToken { SolidBackground = Color.FromHex("#c5000f") },
    };
}
```

::: warning Routed pages are built before the layout
A `ConfigProvider` around `@Body` in a layout does **not** reach the routed page: the page is
built first and placed into the layout afterwards. Put the provider inside the page, or change
the app theme instead (see [Switching themes](#switching-themes)).
:::

<!-- miko:api ConfigProvider -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### ConfigProvider

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `IonicTheme` | `IonicTheme?` | `null` | Partial theme values applied to this subtree. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Palette

The palette defines the named colors used by every `Color="…"` parameter, plus the app
background and text colors.

<!-- miko:tokens Palette -->
Theme token `IonicTheme.Palette` (`PaletteToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Palette = new PaletteToken { … })`; for one subtree: a `ConfigProvider`.

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `BackdropColor` | `Color` | `rgba(0, 0, 0, 0.32)` | `rgba(0, 0, 0, 0.4)` |
| `BackgroundColor` | `Color` | `#ffffff` | `#ffffff` |
| `Danger` | `Color` | `#c5000f` | `#c5000f` |
| `Dark` | `Color` | `#222428` | `#222428` |
| `Light` | `Color` | `#f4f5f8` | `#f4f5f8` |
| `Medium` | `Color` | `#636469` | `#636469` |
| `Primary` | `Color` | `#0054e9` | `#0054e9` |
| `Secondary` | `Color` | `#0163aa` | `#0163aa` |
| `Success` | `Color` | `#2dd55b` | `#2dd55b` |
| `Tertiary` | `Color` | `#6030ff` | `#6030ff` |
| `TextColor` | `Color` | `#000000` | `#000000` |
| `Warning` | `Color` | `#ffc409` | `#ffc409` |
<!-- /miko:tokens -->

::: tip Rebranding
Component defaults that look like the primary color — a button's `SolidBackground`, the
selected tab color, the toggle and checkbox "on" colors — are their **own** tokens, filled from
the palette when the mode theme is created. Overriding `Palette.Primary` re-colors
`Color="primary"` everywhere, but not those defaults: set them in the component tokens too.
:::

## Switching themes

Changing the theme at run time (for example a dark-mode switch) means replacing the app theme
and re-rendering. A singleton service can hold both themes:

```csharp
public sealed class ThemeService(IOptions<IonicOptions> options)
{
    private readonly IonicTheme _light = options.Value.Theme;
    private readonly IonicTheme _dark = new()
    {
        Palette = new PaletteToken { BackgroundColor = "121212", TextColor = "f1f3f4", Primary = "7ca8ff" },
        Content = new ContentToken { Background = "121212", Color = "f1f3f4" },
        Toolbar = new ToolbarToken { Background = "202124", Color = "f1f3f4" },
        // …the other tokens your app uses
    };

    public bool IsDark
    {
        get => options.Value.Theme == _dark;
        set => options.Value.Theme = value ? _dark : _light;
    }
}
```

Pages navigated to afterwards, and overlays created by the controllers, pick up the new theme.
To re-theme the page that is already on screen, wrap it in
`<ConfigProvider IonicTheme="@options.Value.Theme">` and re-render it. Styles are cached per
theme instance, so switching back and forth does not generate new rules. The `IonicDemo`
example (`examples/Ionic/IonicDemo`) implements a complete dark mode this way.

## Mode

Components render in one of two design languages: `md` (Material Design) or `ios`. The mode is
chosen in this order:

1. `IonicOptions.Platform`, if set — `HostPlatform.Ios` selects `ios`, any other platform `md`;
2. `IonicOptions.Mode = IonicMode.Ios` (kept for compatibility);
3. the host platform — `ios` on iOS, `md` on Android and the desktop. In the simulator the mode
   follows the selected device.

```csharp
builder.AddIonic(options => options.Platform = HostPlatform.Ios);  // iOS look everywhere
```

Each component stamps the mode as a class on its root (`md` or `ios`), so app styles can target
one mode: `[".ios .my-banner"] = new() { … }`.

<!-- miko:type IonicOptions IonicMode -->
### IonicOptions

Configuration for the Ionic component library.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Mode` | `IonicMode` | `IonicMode.Md` | iOS opt-in, retained for compatibility. Otherwise components follow the host platform. Use [`Platform`](#ionicoptions) to force either design mode independently of the host. |
| `Theme` | `IonicTheme` | — | The partial theme applied at application startup. Its explicitly set token values are resolved against the active mode's defaults for every component. |
| `Platform` | `HostPlatform?` | `null` | Optional Ionic platform override. When null, components follow the host platform unless an iOS mode/theme was explicitly supplied. This does not replace the host's platform service. |

### IonicMode

Ionic visual mode. Ionic ships two design languages — Material Design (used on Android and the web) and iOS. The active mode is supplied by the platform and defaults to [`Md`](#ionicmode).

| Value | Description |
| --- | --- |
| `Md` | Material Design mode (default). |
| `Ios` | iOS / Cupertino mode. |
<!-- /miko:type -->

## Overriding with app styles

Ionic's stylesheet sits in a lower [cascade layer](../../guide/styling.md#cascade-layers)
(`-1`) than the app's stylesheets, so **any** rule in your app stylesheet beats Ionic's own
rules, whatever their specificity. Target the classes listed under **Styling › CSS classes**
on each component page:

```csharp
// GlobalStyles.cs
[".ion-button.cta .button-native"] = new() { BorderRadius = new BorderRadius(12) },
[".ion-title .toolbar-title"] = new() { FontWeight = FontWeight.Bold },
```

Use tokens for values that should follow the mode or a `ConfigProvider`, and stylesheet rules
for one-off adjustments.
