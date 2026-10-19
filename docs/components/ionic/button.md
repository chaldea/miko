# Button

Buttons provide a clickable element for actions anywhere in an app. They can show text, icons, or both, and are styled through `Fill`, `Expand`, `Shape`, `Size`, and `Color`.

`IonButtons` groups buttons inside an [`IonToolbar`](./toolbar.md) slot.

## Basic usage

<!-- miko:example examples/button/Basic.razor -->
```razor
<IonButton>Default</IonButton>
<IonButton Disabled="true">Disabled</IonButton>
```
<!-- /miko:example -->

## Expand

By default a button is inline (`display: inline-block`). `Expand="block"` stretches it to the full width with rounded corners; `Expand="full"` also removes the corners and side borders.

<!-- miko:example examples/button/Expand.razor -->
```razor
<IonButton Expand="block">Block</IonButton>
<IonButton Expand="full">Full</IonButton>
```
<!-- /miko:example -->

## Shape

`Shape="round"` gives the button pill-shaped corners.

<!-- miko:example examples/button/Shape.razor -->
```razor
<IonButton>Default</IonButton>
<IonButton Shape="round">Round</IonButton>
```
<!-- /miko:example -->

## Fill

`Fill` sets the background and border: `solid` (default), `outline`, or `clear`. Inside `IonButtons` or an [`IonListHeader`](./list.md) a button without an explicit `Fill` defaults to `clear`.

<!-- miko:example examples/button/Fill.razor -->
```razor
<IonButton>Default</IonButton>
<IonButton Fill="clear">Clear</IonButton>
<IonButton Fill="outline">Outline</IonButton>
<IonButton Fill="solid">Solid</IonButton>
```
<!-- /miko:example -->

## Size

`Size` changes the height, padding, and font size: `small`, `default`, or `large`.

<!-- miko:example examples/button/Size.razor -->
```razor
<IonButton Size="small">Small</IonButton>
<IonButton Size="default">Default</IonButton>
<IonButton Size="large">Large</IonButton>
```
<!-- /miko:example -->

## Icons

Buttons have three named slots for icons: `IconOnly` (a square, text-less button), `Start` (before the label), and `End` (after the label). When you use a named slot, put the label in `<ChildContent>`.

<!-- miko:example examples/button/Icons.razor -->
```razor
@* Icon-only: put the icon in the IconOnly slot (the button becomes square). *@
<IonButton>
    <IconOnly>
        <IonIcon Icon="@Ionicons.Heart" />
    </IconOnly>
</IonButton>

@* Leading / trailing icons: Start and End slots around the label. *@
<IonButton>
    <Start>
        <IonIcon Icon="@Ionicons.Star" />
    </Start>
    <ChildContent>Left Icon</ChildContent>
</IonButton>

<IonButton>
    <End>
        <IonIcon Icon="@Ionicons.Heart" />
    </End>
    <ChildContent>Right Icon</ChildContent>
</IonButton>
```
<!-- /miko:example -->

## Colors

`Color` takes a palette name. Colors come from the theme's palette — see [Theming](./theming.md#palette).

<!-- miko:example examples/button/Colors.razor -->
```razor
<IonButton Color="primary">Primary</IonButton>
<IonButton Color="secondary">Secondary</IonButton>
<IonButton Color="tertiary">Tertiary</IonButton>
<IonButton Color="success">Success</IonButton>
<IonButton Color="warning">Warning</IonButton>
<IonButton Color="danger">Danger</IonButton>
<IonButton Color="light">Light</IonButton>
<IonButton Color="medium">Medium</IonButton>
<IonButton Color="dark">Dark</IonButton>
```
<!-- /miko:example -->

## Handling taps

`OnClick` is an `EventCallback`; it is not raised while `Disabled` is true.

<!-- miko:example examples/button/Click.razor -->
```razor
<IonButton Strong="true" OnClick="Increment">Tapped @_count times</IonButton>

@code {
    private int _count;

    // The component re-renders after the handler returns; no StateHasChanged() needed.
    private void Increment() => _count++;
}
```
<!-- /miko:example -->

## Buttons in a toolbar

Wrap toolbar buttons in `IonButtons` and place the group in the toolbar's `Start` or `End` slot. The slot decides the position — `IonButtons` itself does not move the group.

<!-- miko:example examples/button/Toolbar.razor -->
```razor
@* Position comes from the toolbar slot (Start / End). Inside IonButtons a button defaults to
   the clear fill and the denser toolbar metrics. *@
<IonToolbar>
    <Start>
        <IonButtons>
            <IonButton>Cancel</IonButton>
        </IonButtons>
    </Start>
    <ChildContent>
        <IonTitle>Edit</IonTitle>
    </ChildContent>
    <End>
        <IonButtons>
            <IonButton Strong="true">Save</IonButton>
        </IonButtons>
    </End>
</IonToolbar>
```
<!-- /miko:example -->

## API

<!-- miko:api IonButton IonButtons -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonButton

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class the stylesheet maps to fill / text color. |
| `Disabled` | `bool` | `false` | When true, the button is dimmed and non-interactive (Ionic `disabled`). |
| `Expand` | `string?` | `null` | Width expansion (Ionic `expand`): `"block"` (full-width) or `"full"` (full-width, square corners, no side borders). |
| `Fill` | `string?` | `null` | Fill style (Ionic `fill`): `"solid"` (default), `"outline"`, or `"clear"`. Null defaults to clear inside toolbar button groups and list headers, otherwise solid. |
| `Href` | `string?` | `null` | Link target (Ionic `href`). Kept as a marker for API parity; the renderer does not perform navigation. |
| `Shape` | `string?` | `null` | Corner shape (Ionic `shape`): `"round"` for pill corners. |
| `Size` | `string?` | `null` | Size (Ionic `size`): `"small"`, `"default"`, or `"large"`. |
| `Strong` | `bool` | `false` | When true, uses a heavier font weight (Ionic `strong`). |

| Event | Type | Description |
| --- | --- | --- |
| `OnClick` | `EventCallback` | Raised when the button is tapped (skipped when disabled). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
| `End` | `RenderFragment?` | Content for Ionic's `end` slot — placed after the label (right in LTR, left in RTL). Typically a trailing icon. |
| `IconOnly` | `RenderFragment?` | Content for Ionic's `icon-only` slot — an icon in a button that has no text. When set (and no text/start/end content), the host gets the `button-has-icon-only` class so it renders as a square icon button. |
| `Start` | `RenderFragment?` | Content for Ionic's `start` slot — placed before the label (left in LTR, right in RTL). Typically a leading icon. |

### IonButtons

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Slot` | `string` | `"start"` | Ionic's `slot` attribute on `ion-buttons`: `"start"` (default) or `"end"`, stamped as a `buttons-start` / `buttons-end` class. It does not move the group — the [`IonToolbar`](./toolbar.md#iontoolbar) slot it is placed in (`Start`, `End`, `Primary`, `Secondary`) decides the position. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Button -->
Theme token `IonicTheme.Button` (`ButtonToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Button = new ButtonToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `BorderRadius` | `float` | `4` | `14` |
| `Color` | `Color` | `#0054e9` | `#0054e9` |
| `FontSize` | `float` | `14` | `16` |
| `FontWeight` | `FontWeight` | `FontWeight.Medium` | `FontWeight.Medium` |
| `IconOnlyIconSize` | `float` | `22.4` | `18` |
| `IconOnlyMinSize` | `float` | `40` | `40` |
| `LargeBorderRadius` | `float` | `4` | `16` |
| `LargeFontSize` | `float` | `20` | `20` |
| `LargeIconOnlyIconSize` | `float` | `28` | `18` |
| `LargeIconOnlyMinSize` | `float` | `50` | `50` |
| `LargeMinHeight` | `Length` | `2.8em` | `3.1em` |
| `LargePaddingBottom` | `Length` | `14px` | `17px` |
| `LargePaddingTop` | `Length` | `14px` | `17px` |
| `LargePaddingX` | `Length` | `1em` | `1em` |
| `LetterSpacing` | `Length` | `0.06em` | `0px` |
| `MinHeight` | `Length` | `36px` | `3.1em` |
| `OutlineBorderWidth` | `Length` | `2px` | `1px` |
| `PaddingBottom` | `Length` | `8px` | `13px` |
| `PaddingEnd` | `Length` | `1.1em` | `1em` |
| `PaddingStart` | `Length` | `1.1em` | `1em` |
| `PaddingTop` | `Length` | `8px` | `13px` |
| `RoundBorderRadius` | `float` | `999` | `999` |
| `SmallBorderRadius` | `float` | `4` | `6` |
| `SmallFontSize` | `float` | `13` | `13` |
| `SmallIconOnlyIconSize` | `float` | `16` | `17` |
| `SmallIconOnlyMinSize` | `float` | `28` | `28` |
| `SmallMinHeight` | `Length` | `2.1em` | `2.1em` |
| `SmallPaddingBottom` | `Length` | `4px` | `4px` |
| `SmallPaddingTop` | `Length` | `4px` | `4px` |
| `SmallPaddingX` | `Length` | `0.9em` | `0.9em` |
| `SolidBackground` | `Color` | `#0054e9` | `#0054e9` |
| `SolidBoxShadow` | `List<BoxShadow>` | `0 3px 1px -2px rgba(0, 0, 0, 0.2), 0 2px 2px 0 rgba(0, 0, 0, 0.14), 0 1px 5px 0 rgba(0, 0, 0, 0.12)` | `none` |
| `SolidColor` | `Color` | `#ffffff` | `#ffffff` |
| `StrongFontWeight` | `FontWeight` | `FontWeight.Bold` | `FontWeight.SemiBold` |
| `TextColor` | `Color` | `#0054e9` | `#0054e9` |
| `TextTransform` | `TextTransform` | `TextTransform.Uppercase` | `TextTransform.None` |

- `Color`: Convenience alias for the default solid button color.
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-button` | The host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-button .button-native` | The painted, clickable surface: background, border, radius, padding. |
| `.ion-button .button-inner` | The row that centers the slotted content. |
| `.button-solid`, `.button-outline`, `.button-clear` | The resolved `Fill`. |
| `.button-block`, `.button-full` | `Expand`. |
| `.button-small`, `.button-large` | `Size`. |
| `.button-round` | `Shape="round"`. |
| `.button-strong`, `.button-disabled` | `Strong`, `Disabled`. |
| `.button-has-icon-only` | A button whose only content is in the `IconOnly` slot. |
| `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |
| `.in-toolbar`, `.in-buttons` | The button sits in a toolbar / an `IonButtons` group. |
| `.ion-buttons` | The `IonButtons` group. |

```csharp
// GlobalStyles.cs — a brand-specific "cta" variant: <IonButton Class="cta">…</IonButton>
[".ion-button.cta .button-native"] = new()
{
    BackgroundColor = Color.FromHex("#0e7490"),
    BorderRadius = new BorderRadius(12),
},
```
