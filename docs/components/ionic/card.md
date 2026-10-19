# Card

Cards are raised, rounded surfaces that group related content — a header with a title and subtitle, body text, media, lists, and actions. A card can also be a single tap target.

`IonCard` is the surface. `IonCardHeader` holds an `IonCardTitle` and an `IonCardSubtitle`; `IonCardContent` is the padded body. All four are optional inside the card, in any combination.

## Basic usage

<!-- miko:example examples/card/Basic.razor -->
```razor
<IonCard>
    <IonCardHeader>
        <IonCardTitle>Card Title</IonCardTitle>
        <IonCardSubtitle>Card Subtitle</IonCardSubtitle>
    </IonCardHeader>
    <IonCardContent>
        Here's a small text description for the card content. Nothing more, nothing less.
    </IonCardContent>
</IonCard>
```
<!-- /miko:example -->

In `ios` mode the header stacks in reverse (`flex-direction: column-reverse`), so the subtitle is drawn above the title; write the title first in both modes. When an `IonCardContent` directly follows an `IonCardHeader`, its top padding is removed.

`IonCardTitle` and `IonCardSubtitle` have no typography of their own in this port — they use the card's font (14px) — unlike Ionic, which gives the title a larger font. Style `.ion-card-title` / `.ion-card-subtitle` in the app stylesheet if you need it (see [CSS classes](#css-classes)).

## Media

Put an image directly in the card, before the header, for a media card. The card clips its content to its rounded corners (`overflow: hidden`).

<!-- miko:example examples/card/Media.razor -->
```razor
@* An image placed directly in the card, before the header, spans the card's top edge. *@
<IonCard>
    <img src="res://Assets/card-media.png" />
    <IonCardHeader>
        <IonCardTitle>Madison, WI</IonCardTitle>
        <IonCardSubtitle>Destination</IonCardSubtitle>
    </IonCardHeader>
    <IonCardContent>
        Founded in 1829 on an isthmus between Lake Monona and Lake Mendota.
    </IonCardContent>
</IonCard>
```
<!-- /miko:example -->

## Buttons

Buttons placed in the card after `IonCardContent` sit in a row below the content; `Fill="clear"` is the usual choice.

<!-- miko:example examples/card/Buttons.razor -->
```razor
<IonCard>
    <IonCardHeader>
        <IonCardTitle>Card Title</IonCardTitle>
        <IonCardSubtitle>Card Subtitle</IonCardSubtitle>
    </IonCardHeader>
    <IonCardContent>
        Here's a small text description for the card content.
    </IonCardContent>

    <IonButton Fill="clear">Action 1</IonButton>
    <IonButton Fill="clear">Action 2</IonButton>
</IonCard>
```
<!-- /miko:example -->

## Lists

An [`IonList`](./list.md) inside `IonCardContent` gives the card a list body.

<!-- miko:example examples/card/List.razor -->
```razor
<IonCard>
    <IonCardHeader>
        <IonCardTitle>Recent files</IonCardTitle>
    </IonCardHeader>
    <IonCardContent>
        <IonList>
            <IonItem>
                <Start>
                    <IonIcon Icon="@Ionicons.DocumentText" />
                </Start>
                <ChildContent>
                    <IonLabel>Report.pdf</IonLabel>
                </ChildContent>
            </IonItem>
            <IonItem Lines="none">
                <Start>
                    <IonIcon Icon="@Ionicons.Image" />
                </Start>
                <ChildContent>
                    <IonLabel>Photo.jpg</IonLabel>
                </ChildContent>
            </IonItem>
        </IonList>
    </IonCardContent>
</IonCard>
```
<!-- /miko:example -->

## Clickable cards

`Button="true"` wraps the card's content in a `<button class="card-native">` so the whole card is one tap target that raises `OnClick`. `Disabled` dims the card, blocks pointer input, and suppresses `OnClick`.

`Href` also makes the card clickable, rendering the surface as an anchor (`Target` and `Rel` are passed through), but it does not navigate in this port — handle `OnClick` and navigate yourself.

<!-- miko:example examples/card/Clickable.razor -->
```razor
@* Button="true" makes the whole card one tap target; OnClick does not fire while Disabled. *@
<IonCard Button="true" OnClick="Open">
    <IonCardHeader>
        <IonCardTitle>Order #1024</IonCardTitle>
        <IonCardSubtitle>Opened @_opens times</IonCardSubtitle>
    </IonCardHeader>
</IonCard>

<IonCard Button="true" Disabled="true">
    <IonCardHeader>
        <IonCardTitle>Archived order</IonCardTitle>
    </IonCardHeader>
</IonCard>

@code {
    private int _opens;

    private void Open() => _opens++;
}
```
<!-- /miko:example -->

## Colors

`Color` on `IonCard` fills the whole card with a palette color and switches the text to that color's contrast; on `IonCardHeader` it colors only the header band. `IonCardTitle` and `IonCardSubtitle` also accept `Color` and stamp the `ion-color-*` class, but no stylesheet rule colors them. See [Theming](./theming.md#palette).

<!-- miko:example examples/card/Colors.razor -->
```razor
<IonCard Color="primary">
    <IonCardHeader>
        <IonCardTitle>Primary card</IonCardTitle>
    </IonCardHeader>
    <IonCardContent>The whole card takes the palette color.</IonCardContent>
</IonCard>

<IonCard>
    <IonCardHeader Color="dark">
        <IonCardTitle>Dark header</IonCardTitle>
    </IonCardHeader>
    <IonCardContent>Only the header is colored.</IonCardContent>
</IonCard>
```
<!-- /miko:example -->

## API

<!-- miko:api IonCard IonCardHeader IonCardTitle IonCardSubtitle IonCardContent -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonCard

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Button` | `bool` | `false` | When true, wraps the content in a clickable `button.card-native` surface that raises [`OnClick`](#ioncard) (Ionic `button`). |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, .... |
| `Disabled` | `bool` | `false` | When true, the card is dimmed and non-interactive. |
| `Href` | `string?` | `null` | Link target (Ionic `href`). When set, the card is clickable and its surface is an anchor; the renderer does not navigate — handle [`OnClick`](#ioncard). |
| `Rel` | `string?` | `null` | Anchor rel, used only when [`Href`](#ioncard) is set. |
| `Target` | `string?` | `null` | Anchor target, used only when [`Href`](#ioncard) is set. |

| Event | Type | Description |
| --- | --- | --- |
| `OnClick` | `EventCallback` | Raised when the card is tapped; only when it is clickable ([`Button`](#ioncard) or [`Href`](#ioncard)) and not [`Disabled`](#ioncard). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonCardHeader

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, .... |
| `Translucent` | `bool` | `false` | When true, stamps the iOS translucent header marker class. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonCardTitle

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, .... Stamps `ion-color-*`, but no built-in rule colors the title — it inherits the color of the card / header. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonCardSubtitle

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, .... Stamps `ion-color-*`, but no built-in rule colors the subtitle — it inherits the color of the card / header. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonCardContent

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Card -->
Theme token `IonicTheme.Card` (`CardToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Card = new CardToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `#ffffff` | `#ffffff` |
| `BorderRadius` | `float` | `4` | `8` |
| `BoxShadow` | `List<BoxShadow>` | `0 3px 1px -2px rgba(0, 0, 0, 0.2), 0 2px 2px 0 rgba(0, 0, 0, 0.14), 0 1px 5px 0 rgba(0, 0, 0, 0.12)` | `0 4px 16px 0 rgba(0, 0, 0, 0.12)` |
| `Color` | `Color` | `#000000` | `#000000` |
| `ContentFontSize` | `float` | `14` | `16` |
| `ContentLineHeight` | `Length` | `1.5` | `1.4` |
| `ContentPaddingBottom` | `float` | `13` | `20` |
| `ContentPaddingEnd` | `float` | `16` | `20` |
| `ContentPaddingStart` | `float` | `16` | `20` |
| `ContentPaddingTop` | `float` | `13` | `20` |
| `FontSize` | `float` | `14` | `14` |
| `HeaderPaddingBottom` | `float` | `16` | `16` |
| `HeaderPaddingEnd` | `float` | `16` | `20` |
| `HeaderPaddingStart` | `float` | `16` | `20` |
| `HeaderPaddingTop` | `float` | `16` | `20` |
| `LineHeight` | `Length` | `1.5` | `1.4` |
| `MarginBottom` | `float` | `10` | `24` |
| `MarginEnd` | `float` | `10` | `16` |
| `MarginStart` | `float` | `10` | `16` |
| `MarginTop` | `float` | `10` | `24` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-card` | The card host: background, margin, radius, shadow. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-card .card-native` | The `<button>` / `<a>` surface of a clickable card (`Button` or `Href`). |
| `.ion-activatable` | The card is clickable. |
| `.card-disabled` | `Disabled`. |
| `.ion-card-header` | The header (a flex column; reversed in `ios`). |
| `.card-header-translucent` | `IonCardHeader` with `Translucent` (marker only; no rule uses it). |
| `.ion-card-title`, `.ion-card-subtitle` | Title and subtitle (no built-in rules). |
| `.ion-card-content` | The padded body. Also carries `card-content-md` / `card-content-ios`. |
| `.ion-color-{name}` | `Color` on any of the card components (e.g. `.ion-color-primary`). |

```csharp
// GlobalStyles.cs — give card titles Ionic's larger heading size.
[".ion-card-title"] = new()
{
    FontSize = Length.Px(20),
    FontWeight = FontWeight.Medium,
},
```
