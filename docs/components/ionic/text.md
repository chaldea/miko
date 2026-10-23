# Text

Text is an inline wrapper that colors the text inside it with a palette color, for anything from a single word to a whole heading.

`IonText` renders a `<span>` around its content. With no `Color` it has no visible effect.

## Basic usage

Wrap a block element (a heading, a paragraph) or a run of inline text. Elements inside take the color unless they set their own `Color`.

<!-- miko:example examples/text/Basic.razor -->
```razor
@* IonText only sets the text color; wrap a block or a run of inline text. *@
<IonText Color="primary">
    <h2>Order confirmed</h2>
</IonText>

<p>
    Your payment was <IonText Color="success">accepted</IonText>;
    delivery is <IonText Color="danger">delayed</IonText> by one day.
</p>
```
<!-- /miko:example -->

`IonText` only changes the color — no font size, weight, or spacing. For typography use plain elements (`<h1>`…`<h6>`, `<p>`) or app styles; see [Styling](../../guide/styling.md).

## API

<!-- miko:api IonText -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonText

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class the stylesheet maps to the text color. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

`IonText` reads only the palette (see [Theming](./theming.md#palette)) and has no theme token of its own.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-text` | The wrapper span. Also carries the mode (`md` / `ios`). |
| `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |

```csharp
// GlobalStyles.cs — a bold "highlight" variant: <IonText Class="highlight">…</IonText>
[".ion-text.highlight"] = new()
{
    FontWeight = FontWeight.Bold,
    Color = Color.FromHex("#0e7490"),
},
```
