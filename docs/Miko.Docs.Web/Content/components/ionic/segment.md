# Segment

Segments display a row of buttons, of which exactly one is selected, for switching between views or filters within a page.

`IonSegment` is the row and owns the selected value; each `IonSegmentButton` inside it is one choice. `IonSegmentView` and `IonSegmentContent` show the content panel that matches the selected button.

## Basic usage

Bind the selected button with `@bind-Value`; each button's `Value` identifies it. `IonSegmentButton` only works inside an `IonSegment` — it reads the selection from it and does nothing on its own. The checked button is marked by an indicator: an underline bar in md mode, a raised pill in ios mode, which slides to the new button when the selection changes.

<!-- miko:example examples/segment/Basic.razor -->
```razor
<IonSegment @bind-Value="_filter">
    <IonSegmentButton Value="all">
        <IonLabel>All</IonLabel>
    </IonSegmentButton>
    <IonSegmentButton Value="unread">
        <IonLabel>Unread</IonLabel>
    </IonSegmentButton>
    <IonSegmentButton Value="flagged">
        <IonLabel>Flagged</IonLabel>
    </IonSegmentButton>
</IonSegment>

<p>Showing: @_filter</p>

@code {
    private string? _filter = "all";
}
```
<!-- /miko:example -->

## Icons

Put an `IonIcon`, an `IonLabel`, or both in a button. With both, `Layout` arranges them: `icon-top` (default), `icon-bottom`, `icon-start`, `icon-end`, or `icon-hide` / `label-hide` to drop one of them. The button detects its own content and adds the `segment-button-has-label`, `segment-button-has-icon`, `segment-button-has-label-only`, or `segment-button-has-icon-only` class.

<!-- miko:example examples/segment/Icons.razor -->
```razor
@* Icon-only buttons. *@
<IonSegment @bind-Value="_view">
    <IonSegmentButton Value="list">
        <IonIcon Icon="@Ionicons.List" />
    </IonSegmentButton>
    <IonSegmentButton Value="grid">
        <IonIcon Icon="@Ionicons.Grid" />
    </IonSegmentButton>
</IonSegment>

@* Icon + label: the icon sits above the label unless Layout says otherwise. *@
<IonSegment @bind-Value="_tab">
    <IonSegmentButton Value="call" Layout="icon-start">
        <IonIcon Icon="@Ionicons.Call" />
        <IonLabel>Call</IonLabel>
    </IonSegmentButton>
    <IonSegmentButton Value="favorite" Layout="icon-start">
        <IonIcon Icon="@Ionicons.Heart" />
        <IonLabel>Favorite</IonLabel>
    </IonSegmentButton>
</IonSegment>

@code {
    private string? _view = "list";
    private string? _tab = "call";
}
```
<!-- /miko:example -->

## Scrollable

By default the buttons share the row's width equally. With `Scrollable="true"` each button keeps its natural width and the row scrolls horizontally (the scrollbar is hidden); when the selection changes — by a tap or a new `Value` — the selected button is scrolled to the center.

<!-- miko:example examples/segment/Scrollable.razor -->
```razor
<IonSegment Scrollable="true" @bind-Value="_category">
    @foreach (var category in _categories)
    {
        <IonSegmentButton Value="@category">
            <IonLabel>@category</IonLabel>
        </IonSegmentButton>
    }
</IonSegment>

@code {
    private readonly string[] _categories =
        { "Top", "World", "Business", "Technology", "Science", "Health", "Sports", "Travel" };

    private string? _category = "Top";
}
```
<!-- /miko:example -->

## Colors and disabled buttons

`Color` takes a palette name (see [Theming](./theming.md#palette)). In md mode the checked button's text and the indicator take the color, while the other buttons keep the neutral text color; in ios mode only the segment's track is tinted. `Disabled` on the segment dims and blocks the whole row; `Disabled` on a button blocks just that button.

<!-- miko:example examples/segment/Colors.razor -->
```razor
<IonSegment Color="secondary" @bind-Value="_value">
    <IonSegmentButton Value="day">
        <IonLabel>Day</IonLabel>
    </IonSegmentButton>
    <IonSegmentButton Value="week">
        <IonLabel>Week</IonLabel>
    </IonSegmentButton>
    <IonSegmentButton Value="month" Disabled="true">
        <IonLabel>Month</IonLabel>
    </IonSegmentButton>
</IonSegment>

@code {
    private string? _value = "day";
}
```
<!-- /miko:example -->

## Segment views

`IonSegmentView` shows the one `IonSegmentContent` whose `Id` equals the view's `Value` and hides the others. The segment and the view are not linked automatically: bind the same field to both, with each button's `Value` matching a content `Id` (Ionic's `content-id` link is not ported). Unlike Ionic, the view switches instantly — there is no swipe between contents.

<!-- miko:example examples/segment/Views.razor -->
```razor
@* The segment and the view bind the same field; each button's Value matches a content Id. *@
<IonSegment @bind-Value="_section">
    <IonSegmentButton Value="details">
        <IonLabel>Details</IonLabel>
    </IonSegmentButton>
    <IonSegmentButton Value="reviews">
        <IonLabel>Reviews</IonLabel>
    </IonSegmentButton>
</IonSegment>

<IonSegmentView Value="@_section">
    <IonSegmentContent Id="details">
        <p>Product details go here.</p>
    </IonSegmentContent>
    <IonSegmentContent Id="reviews">
        <p>Customer reviews go here.</p>
    </IonSegmentContent>
</IonSegmentView>

@code {
    private string? _section = "details";
}
```
<!-- /miko:example -->

## In a toolbar

A segment can be the content of an [`IonToolbar`](./toolbar.md). In md mode it takes the toolbar's full width and height; in ios mode it shrinks to its buttons and is centered.

<!-- miko:example examples/segment/Toolbar.razor -->
```razor
<IonToolbar>
    <IonSegment @bind-Value="_mailbox">
        <IonSegmentButton Value="inbox">
            <IonLabel>Inbox</IonLabel>
        </IonSegmentButton>
        <IonSegmentButton Value="sent">
            <IonLabel>Sent</IonLabel>
        </IonSegmentButton>
    </IonSegment>
</IonToolbar>

@code {
    private string? _mailbox = "inbox";
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonSegment IonSegmentButton IonSegmentView IonSegmentContent -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonSegment

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named Ionic palette color (for example `primary` or `danger`). |
| `Disabled` | `bool` | `false` | When true the whole segment is dimmed and non-interactive. |
| `Scrollable` | `bool` | `false` | Allows the segment buttons to overflow horizontally and centers selections. |
| `Value` | `string?` | `null` | The selected button value. Bind this from the host (two-way with [`ValueChanged`](#ionsegment)). Two-way bindable: `@bind-Value`. |

| Event | Type | Description |
| --- | --- | --- |
| `ValueChanged` | `EventCallback<string>` | Raised with the new value when a button is tapped. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonSegmentButton

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | When true this individual button is dimmed and non-interactive. |
| `Layout` | `string?` | `"icon-top"` | Arrangement of an icon and a label in the button (Ionic `layout`): `"icon-top"` (default), `"icon-bottom"`, `"icon-start"`, `"icon-end"`, `"icon-hide"` (hides the icon) or `"label-hide"` (hides the label). |
| `Value` | `string` | `""` | This button's value; becomes the segment's selected value when tapped. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonSegmentView

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `string?` | `null` | The id of the currently visible [`IonSegmentContent`](#ionsegmentcontent). Bind the same field from the host that drives the segment's selected value so the segment and view coordinate. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonSegmentContent

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Segment -->
Theme token `IonicTheme.Segment` (`SegmentToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Segment = new SegmentToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `transparent` | `rgba(0, 0, 0, 0.07)` |
| `BorderRadius` | `float` | `0` | `8` |
| `ButtonCheckedColor` | `Color` | `#0054e9` | `#000000` |
| `ButtonColor` | `Color` | `rgba(0, 0, 0, 0.6)` | `#000000` |
| `ButtonFontSize` | `float` | `14` | `13` |
| `ButtonLetterSpacing` | `Length` | `0.06em` | `0px` |
| `ButtonLineHeight` | `float` | `40` | `37` |
| `ButtonMarginY` | `float` | `0` | `2` |
| `ButtonMinHeight` | `float` | `48` | `28` |
| `ButtonMinWidth` | `float` | `90` | `70` |
| `ButtonPaddingX` | `float` | `16` | `13` |
| `ButtonPaddingY` | `float` | `0` | `0` |
| `IndicatorBorderRadius` | `float` | `0` | `7` |
| `IndicatorBoxShadow` | `List<BoxShadow>` | `none` | `0 0 5px 0 rgba(0, 0, 0, 0.16)` |
| `IndicatorColor` | `Color` | `#0054e9` | `#ffffff` |
| `IndicatorHeight` | `Length` | `2px` | `100%` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-segment` | The segment row. Also carries the mode (`md` / `ios`) and the classes below. |
| `.segment-scrollable`, `.segment-disabled`, `.in-toolbar` | `Scrollable`, `Disabled`, inside an `IonToolbar`. |
| `.ion-color-{name}` | `Color` on the segment (e.g. `.ion-color-secondary`); also copied onto each button. |
| `.ion-segment-button` | One button (host). |
| `.ion-segment-button .button-native` | The clickable surface; padding and min-height live here. |
| `.ion-segment-button .button-inner` | Centers the icon and label. |
| `.ion-segment-button .segment-button-indicator` | The checked indicator (transparent until checked). |
| `.ion-segment-button .segment-button-indicator-background` | The indicator's painted bar (md) or pill (ios). |
| `.segment-button-checked`, `.segment-button-disabled` | The selected button, a disabled button. |
| `.segment-button-layout-{layout}` | `Layout` (e.g. `.segment-button-layout-icon-start`). |
| `.segment-button-has-label-only`, `.segment-button-has-icon-only` | Content markers (also `-has-label`, `-has-icon`). |
| `.in-segment-color` | A button whose segment has a `Color`. |
| `.ion-segment-view` | The content container. |
| `.ion-segment-content`, `.segment-content-hidden` | One content panel; `.segment-content-hidden` on the inactive ones. |

```csharp
// GlobalStyles.cs — a thicker md indicator
[".ion-segment-button.md .segment-button-indicator-background"] = new()
{
    Height = Length.Px(3),
},
```
