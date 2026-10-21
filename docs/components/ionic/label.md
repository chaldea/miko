# Label

A label is the text content of an item, or a standalone line of text styled to match Ionic components.

`IonLabel` is a block that wraps its child content.

## Basic usage

Inside an [`IonItem`](./item.md), a label in the default slot grows to fill the row, so trailing content is pushed to the edge, and it gets 10px vertical margins (plus an 8px end margin in ios mode). Outside an item it is a plain block. Form controls such as [`IonInput`](./input.md) or [`IonToggle`](./toggle.md) render their own label, so they do not need an `IonLabel`.

<!-- miko:example examples/label/Basic.razor -->
```razor
<IonList>
    <IonItem>
        <IonLabel>Label in an item</IonLabel>
    </IonItem>
</IonList>

<IonLabel>Standalone label</IonLabel>
```
<!-- /miko:example -->

## Text wrapping

Long label text wraps by default and the item grows to fit. Add the `ion-text-nowrap` class to keep it on one line and cut it off with an ellipsis, or `ion-text-wrap` to wrap with a looser 1.5 line height (and 14px text in ios mode).

<!-- miko:example examples/label/Wrap.razor -->
```razor
<IonList>
    @* Default: long text wraps onto more lines and the item grows. *@
    <IonItem>
        <IonLabel>
            Multi-line text that wraps when it is too long to fit on one line. Lorem ipsum dolor sit
            amet, consectetur adipiscing elit.
        </IonLabel>
    </IonItem>

    @* ion-text-nowrap: one line, cut off with an ellipsis. *@
    <IonItem>
        <IonLabel Class="ion-text-nowrap">
            Text that is truncated with an ellipsis when it is too long to fit on one line. Lorem
            ipsum dolor sit amet, consectetur adipiscing elit.
        </IonLabel>
    </IonItem>

    @* ion-text-wrap: wraps with the looser 1.5 line height (and 14px text in ios mode). *@
    <IonItem>
        <IonLabel Class="ion-text-wrap">
            Multi-line text with the wrapped-label line height. Lorem ipsum dolor sit amet,
            consectetur adipiscing elit.
        </IonLabel>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Fixed labels

`Position="fixed"` turns the label into a 100px column that does not shrink, so the controls next to it line up across rows. It stays 100px wide beside a control that fills the row (as here) and beside another label.

`Position="stacked"` and `"floating"` are accepted but only adjust margins: the label stays beside the control instead of moving above it. For a stacked or floating label, use the control's own `LabelPlacement` (see [Input](./input.md)).

<!-- miko:example examples/label/Fixed.razor -->
```razor
@* A fixed label is a 100px column that does not shrink; the control beside it takes the rest. *@
<IonList>
    <IonItem>
        <IonLabel Position="fixed">Name</IonLabel>
        <IonInput Placeholder="Ada Lovelace" @bind-Value="_name" />
    </IonItem>
    <IonItem>
        <IonLabel Position="fixed">Email</IonLabel>
        <IonInput Placeholder="ada@example.com" @bind-Value="_email" />
    </IonItem>
</IonList>

@code {
    private string? _name;
    private string? _email;
}
```
<!-- /miko:example -->

## Colors

`Color` sets the text to a palette color — see [Theming](./theming.md#palette). A label without its own `Color` inside an item with `Color` takes the item's color.

<!-- miko:example examples/label/Colors.razor -->
```razor
<IonLabel Color="primary">Primary</IonLabel>
<IonLabel Color="secondary">Secondary</IonLabel>
<IonLabel Color="tertiary">Tertiary</IonLabel>
<IonLabel Color="success">Success</IonLabel>
<IonLabel Color="warning">Warning</IonLabel>
<IonLabel Color="danger">Danger</IonLabel>
<IonLabel Color="medium">Medium</IonLabel>
<IonLabel Color="dark">Dark</IonLabel>
```
<!-- /miko:example -->

## Headings and paragraphs

You can put `h1`–`h6` and `p` elements in a label for a title and a secondary line, but Ionic's compact label typography for them (a 16px `h2`, a smaller gray `p`) is not ported: they use the app-wide heading styles (an `h2` is 24px with an 18px top margin). Style them from the app stylesheet — see the snippet under [CSS classes](#css-classes).

## API

<!-- miko:api IonLabel -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonLabel

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class the stylesheet maps to the text color. |
| `Position` | `string?` | `null` | Where the label sits inside an `ion-item` (Ionic `position`): `"fixed"`, `"stacked"` or `"floating"`. Stamped as a `label-*` class. `"fixed"` gives the label a 100px, non-shrinking track. `"stacked"` / `"floating"` only adjust margins: the label stays beside the control in the item row (Ionic's stacking and float transforms depend on an item focus/value state this port does not have) — use the control's own `LabelPlacement` instead. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Label -->
Theme token `IonicTheme.Label` (`LabelToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Label = new LabelToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `StackedFontSize` | `float?` | `null` | `14` |
| `StackedMarginBottom` | `float` | `0` | `4` |
| `TextWrapFontSize` | `float?` | `null` | `14` |
| `TextWrapLineHeight` | `float` | `1.5` | `1.5` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-label` | The label host. Also carries the mode (`md` / `ios`). |
| `.label-fixed`, `.label-stacked`, `.label-floating` | `Position`. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |
| `.ion-text-wrap`, `.ion-text-nowrap` | Global Ionic text utilities; on a label they also set the line height / clipping described above. |
| `.ion-item .input-wrapper .ion-label` | A label in an item's default slot. |

```csharp
// GlobalStyles.cs — Ionic-like title and secondary line inside item labels
[".ion-item .ion-label h2"] = new()
{
    Margin = new Margin(Length.Px(2), Length.Px(0)),
    FontSize = Length.Px(16),
},
[".ion-item .ion-label p"] = new()
{
    Margin = new Margin(Length.Px(2), Length.Px(0)),
    FontSize = Length.Px(14),
    Color = Color.FromHex("#666666"),
},
```
