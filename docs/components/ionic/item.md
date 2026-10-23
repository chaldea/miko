# Item

Items are the rows of a list: each one holds text plus optional icons, avatars, thumbnails, notes, badges, buttons, or a form control, and can be made tappable or swipeable.

`IonItem` is the row. `IonItemGroup` and `IonItemDivider` split rows into titled sections. `IonItemSliding`, `IonItemOptions`, and `IonItemOption` add swipe-to-reveal actions. Rows usually sit in an [`IonList`](./list.md); their text goes in an [`IonLabel`](./label.md).

## Basic usage

Put the text in an `IonLabel`. A label in the default slot grows to fill the row, so anything after it is pushed to the trailing edge.

<!-- miko:example examples/item/Basic.razor -->
```razor
<IonList>
    <IonItem>
        <IonLabel>Inbox</IonLabel>
    </IonItem>
    <IonItem>
        <IonLabel>Drafts</IonLabel>
    </IonItem>
    <IonItem>
        <IonLabel>Archive</IonLabel>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Slots

`Start` holds leading content (icons, avatars, thumbnails), `End` holds trailing content (notes, badges, buttons). Icons placed directly in `Start`/`End` get a 24px box and Ionic's slot margins; an avatar shrinks to 40px (md) / 36px (ios) and a thumbnail grows to 56px. Once you use a named slot, put the label in `<ChildContent>`.

<!-- miko:example examples/item/Slots.razor -->
```razor
@* Once a named slot (Start / End) is used, the label goes in <ChildContent>. *@
<IonList>
    <IonItem>
        <Start>
            <IonIcon Icon="@Ionicons.Wifi" />
        </Start>
        <ChildContent>
            <IonLabel>Wi-Fi</IonLabel>
        </ChildContent>
        <End>
            <IonNote>Home</IonNote>
        </End>
    </IonItem>
    <IonItem>
        <Start>
            <IonAvatar>
                <img src="https://ionicframework.com/docs/img/demos/avatar.svg" />
            </IonAvatar>
        </Start>
        <ChildContent>
            <IonLabel>Ada Lovelace</IonLabel>
        </ChildContent>
        <End>
            <IonBadge>3</IonBadge>
        </End>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Clickable items

An item becomes clickable when `Button` or `Href` is set: the row surface is rendered as a `button` (or an `a` for `Href`), gets a pointer cursor and a hover wash, and raises `OnClick` on tap. With `Href`, the item then navigates through the app's `NavigationManager`: a forward push with the mode's page transition by default, `RouterDirection="back"` for a pop transition, or `"root"` to clear the history stack without a transition. `Disabled` dims the row and blocks input; neither `OnClick` nor navigation runs.

<!-- miko:example examples/item/Clickable.razor -->
```razor
<IonList>
    @* Href: tapping navigates to the route (forward push with the mode's page transition). *@
    <IonItem Href="/settings">
        <IonLabel>Settings</IonLabel>
    </IonItem>

    @* Button: a tappable row; handle the tap in OnClick. *@
    <IonItem Button="true" OnClick="Refresh">
        <IonLabel>Refreshed @_refreshes times</IonLabel>
    </IonItem>

    <IonItem Button="true" Disabled="true">
        <IonLabel>Delete account</IonLabel>
    </IonItem>
</IonList>

@code {
    private int _refreshes;

    private void Refresh() => _refreshes++;
}
```
<!-- /miko:example -->

## Detail arrow

The trailing chevron follows `Detail`. When it is unset, the chevron appears only in ios mode and only on clickable items. `Detail="true"` / `"false"` forces it on or off in both modes; `DetailIcon` replaces the icon.

<!-- miko:example examples/item/Detail.razor -->
```razor
<IonList>
    @* Detail unset: a clickable item shows the chevron in ios mode only. *@
    <IonItem Button="true">
        <IonLabel>Default</IonLabel>
    </IonItem>
    <IonItem Detail="true">
        <IonLabel>Always shown</IonLabel>
    </IonItem>
    <IonItem Button="true" Detail="false">
        <IonLabel>Never shown</IonLabel>
    </IonItem>
    <IonItem Button="true" Detail="true" DetailIcon="@Ionicons.CaretForwardOutline">
        <IonLabel>Custom icon</IonLabel>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Lines

By default an item draws a hairline under its content that starts after the `Start` slot and the leading padding (the inset look). `Lines="full"` draws it across the whole row and `Lines="none"` removes it. `Lines="inset"` looks like the default, but it also opts the item out of the list's `Lines` (see [List lines](./list.md#lines)): an item's own value always wins.

<!-- miko:example examples/item/Lines.razor -->
```razor
<IonList>
    @* Default: the divider starts after the Start slot and the leading padding. *@
    <IonItem>
        <Start>
            <IonIcon Icon="@Ionicons.Star" />
        </Start>
        <ChildContent>
            <IonLabel>Default</IonLabel>
        </ChildContent>
    </IonItem>
    <IonItem Lines="full">
        <Start>
            <IonIcon Icon="@Ionicons.Star" />
        </Start>
        <ChildContent>
            <IonLabel>Full</IonLabel>
        </ChildContent>
    </IonItem>
    <IonItem Lines="none">
        <Start>
            <IonIcon Icon="@Ionicons.Star" />
        </Start>
        <ChildContent>
            <IonLabel>None</IonLabel>
        </ChildContent>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Buttons in items

An [`IonButton`](./button.md) inside an item or item divider that has no `Size` is rendered small, as in Ionic. Set any `Size`, including `"default"`, to keep the button's own size.

<!-- miko:example examples/item/Buttons.razor -->
```razor
<IonList>
    @* A button in an item defaults to Size="small". *@
    <IonItem>
        <ChildContent>
            <IonLabel>Backup codes</IonLabel>
        </ChildContent>
        <End>
            <IonButton Fill="outline">Show</IonButton>
        </End>
    </IonItem>

    @* Any explicit Size (including "default") keeps the button's own size. *@
    <IonItem>
        <ChildContent>
            <IonLabel>Storage</IonLabel>
        </ChildContent>
        <End>
            <IonButton Size="default">Upgrade</IonButton>
        </End>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Inputs in items

Put a form control in the default slot; it renders its own label (`Label` on [`IonInput`](./input.md) and [`IonSelect`](./select.md), the child content of [`IonToggle`](./toggle.md) and [`IonCheckbox`](./checkbox.md)). The item cascades a marker the controls react to: `IonInput` and `IonSelect` drop their highlight bar because the item already draws a divider, and toggles, checkboxes, and radios stretch across the row.

<!-- miko:example examples/item/Inputs.razor -->
```razor
@* Form controls go in the default slot and bring their own label. *@
<IonList>
    <IonItem>
        <IonInput Label="Name" Placeholder="Your name" @bind-Value="_name" />
    </IonItem>
    <IonItem>
        <IonInput Label="Email" LabelPlacement="stacked" @bind-Value="_email" />
    </IonItem>
    <IonItem>
        <IonToggle @bind-Checked="_notifications">Notifications</IonToggle>
    </IonItem>
    <IonItem>
        <IonCheckbox @bind-Checked="_terms">I agree to the terms</IonCheckbox>
    </IonItem>
</IonList>

@code {
    private string? _name;
    private string? _email;
    private bool _notifications = true;
    private bool _terms;
}
```
<!-- /miko:example -->

## Color

`Color` takes a palette name: the row is filled with the color and its text — including `IonLabel`s without their own `Color` — switches to the color's contrast color, as in Ionic. Ionic also darkens the row's divider to the color's shade; here the divider keeps the theme's border color.

<!-- miko:example examples/item/Color.razor -->
```razor
@* The row is filled with the palette color; the label takes the contrast color. *@
<IonList>
    <IonItem Button="true" Color="danger">
        <IonLabel>Delete account</IonLabel>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Item groups and dividers

`IonItemGroup` is a plain block that groups one section's rows. `IonItemDivider` is the section header: a tinted row with its own minimum height, `Start`/`End` slots, and a bottom hairline. `Color` fills the divider with the palette color. Ionic's `sticky` is not ported (Miko has no `position: sticky`), so the divider scrolls with the content.

<!-- miko:example examples/item/Groups.razor -->
```razor
<IonList>
    <IonItemGroup>
        <IonItemDivider>
            <IonLabel>Fruits</IonLabel>
        </IonItemDivider>
        <IonItem>
            <IonLabel>Apple</IonLabel>
        </IonItem>
        <IonItem>
            <IonLabel>Banana</IonLabel>
        </IonItem>
    </IonItemGroup>

    <IonItemGroup>
        <IonItemDivider>
            <IonLabel>Vegetables</IonLabel>
        </IonItemDivider>
        <IonItem>
            <IonLabel>Carrot</IonLabel>
        </IonItem>
        <IonItem>
            <IonLabel>Spinach</IonLabel>
        </IonItem>
    </IonItemGroup>
</IonList>
```
<!-- /miko:example -->

## Sliding items

`IonItemSliding` wraps one `IonItem` and up to two `IonItemOptions`, one per `Side` (`"end"` by default, or `"start"`). A horizontal swipe of at least 32px to the left opens the end options and a swipe to the right opens the start options; a swipe in the other direction closes the row again. A drag that is mostly vertical is left to scrolling. The options slide in over the row's edge when the swipe is released; the row does not follow the finger and the item itself does not move.

Keep the open state in your component. Bind `Open` with `@bind-Open`, pass `OpenSide`, and update the side from `OnSwipe`. A row keeps a swipe's open state when the parent re-renders, but the `OpenSide` you pass decides which options an open row shows: with only `@bind-Open`, a start swipe reveals the end options. To close the row from code, for example after an option was tapped, set the bound value to `false`.

Unlike Ionic, tapping the row does not close it, opening one row does not close the others, and `Expandable` does not add a full-swipe action. `Disabled` on `IonItemSliding` turns the swipe off.

<!-- miko:example examples/item/Sliding.razor -->
```razor
@* Swipe left reveals the end options, swipe right the start options. Keep Open and the side in
   your own state: an open row shows the options of the OpenSide you pass. *@
<IonList>
    @foreach (var message in _messages)
    {
        <IonItemSliding @bind-Open="message.Open" OpenSide="@message.Side" OnSwipe="side => message.Side = side">
            <IonItem>
                <IonLabel>@message.Subject</IonLabel>
            </IonItem>
            <IonItemOptions Side="start">
                <IonItemOption OnClick="() => message.Open = false">Unread</IonItemOption>
            </IonItemOptions>
            <IonItemOptions Side="end">
                <IonItemOption Color="danger" OnClick="() => _messages.Remove(message)">Delete</IonItemOption>
            </IonItemOptions>
        </IonItemSliding>
    }
</IonList>

@code {
    private readonly List<Message> _messages =
    [
        new("Lunch on Friday?"),
        new("Quarterly report"),
        new("Your order has shipped"),
    ];

    private sealed class Message(string subject)
    {
        public string Subject { get; } = subject;
        public bool Open { get; set; }
        public string Side { get; set; } = "end";
    }
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonItem IonItemDivider IonItemGroup IonItemSliding IonItemOptions IonItemOption -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonItem

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Button` | `bool` | `false` | When true, the item renders a clickable `button` native surface and becomes activatable (Ionic `button`). |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Fills the row with the palette color and switches its text — including [`IonLabel`](./label.md#ionlabel)s without their own color — to the color's contrast color, as in Ionic. |
| `Detail` | `bool?` | `null` | When set, forces the detail chevron on (`true`) or off (`false`). When null, Ionic shows it only on iOS for a clickable item. Mirrors Ionic `detail`. |
| `DetailIcon` | `string` | `"chevron-forward"` | Icon used for the detail chevron (Ionic `detailIcon`). Defaults to `chevron-forward`. |
| `Disabled` | `bool` | `false` | When true, the item is dimmed and non-interactive (Ionic `disabled`). |
| `Href` | `string?` | `null` | Link target (Ionic `href`). When set, the native surface is an `a` and the item is clickable; the default click behavior navigates to the target through the injected `NavigationManager` with a mode page transition (iOS push / MD fade-up). |
| `Lines` | `string?` | `null` | How the bottom border is displayed (Ionic `lines`): `"full"`, `"inset"`, or `"none"`. Null uses the theme default hairline. |
| `RouterDirection` | `string?` | `null` | Navigation direction when [`Href`](#ionitem) is clicked (mirrors Ionic's `routerDirection`): `"forward"` (default — push with transition), `"back"` (pop-style reverse transition), or `"root"` (clear the history stack, no transition). |

| Event | Type | Description |
| --- | --- | --- |
| `OnClick` | `EventCallback` | Raised when a clickable item is tapped (skipped when disabled). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
| `End` | `RenderFragment?` | Content for Ionic's `end` slot — placed after the label (right in LTR). Typically a trailing note or control. |
| `Start` | `RenderFragment?` | Content for Ionic's `start` slot — placed before the label (left in LTR). Typically a leading icon or avatar. |

### IonItemDivider

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Fills the divider with the palette color and switches its text to the color's contrast color, as in Ionic. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
| `End` | `RenderFragment?` | Content for Ionic's `end` slot — placed after the label (right in LTR). |
| `Start` | `RenderFragment?` | Content for Ionic's `start` slot — placed before the label (left in LTR). |

### IonItemGroup

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonItemSliding

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | When true, the row cannot be slid (Ionic `disabled` disables the swipe gesture). |
| `Open` | `bool` | `false` | When true the row is slid open, revealing the options on [`OpenSide`](#ionitemsliding). Bind it (`@bind-Open`) to open or close the row from code. Unbound, the row keeps a swipe's open state across re-renders of the parent until the value passed here changes. |
| `OpenSide` | `string` | `"end"` | Which side's options are revealed when [`Open`](#ionitemsliding) is true: `"end"` (default) or `"start"`. A swipe does not write this back — update it from [`OnSwipe`](#ionitemsliding) when the row has start options. |

| Event | Type | Description |
| --- | --- | --- |
| `OnSwipe` | `EventCallback<string>` | Raised after a recognized horizontal swipe (at least 32px), before [`OpenChanged`](#ionitemsliding), with the side it points to: `"end"` for a swipe to the left, `"start"` for a swipe to the right. |
| `OpenChanged` | `EventCallback<bool>` | Raised when a swipe opens or closes the row. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonItemOptions

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Side` | `string` | `"end"` | Edge these options are anchored to: `"end"` (default) or `"start"`. Mirrors Ionic's `side` attribute on `ion-item-options`. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonItemOption

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color for the fill (e.g. `"primary"`, `"danger"`, `"success"`). Null uses the default option fill. Mirrors Ionic's `color`. |
| `Disabled` | `bool` | `false` | When true the option is dimmed and non-interactive (Ionic `disabled`). |
| `Expandable` | `bool` | `false` | Ionic `expandable`. Stamps `item-option-expandable` (`flex-grow: 1` within its [`IonItemOptions`](#ionitemoptions)); Ionic's full-swipe expansion that covers the other options and triggers this one is not implemented. |
| `Href` | `string?` | `null` | Route to navigate to (Ionic `href`). When set, the native surface is an `a`, and a tap raises [`OnClick`](#ionitemoption) and then navigates through the app's `NavigationManager` with the mode's page transition, like [`IonItem`](#ionitem). |
| `RouterDirection` | `string?` | `null` | Navigation direction for [`Href`](#ionitemoption) (Ionic `routerDirection`): `"forward"` (default — push with the mode's transition), `"back"` (pop-style transition), or `"root"` (clear the history, no transition). |

| Event | Type | Description |
| --- | --- | --- |
| `OnClick` | `EventCallback` | Raised when the option is tapped (skipped when disabled). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Item -->
Theme token `IonicTheme.Item` (`ItemToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Item = new ItemToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `AvatarEndSlotMarginStart` | `float` | `16` | `8` |
| `AvatarSize` | `float` | `40` | `36` |
| `AvatarSlotMarginVertical` | `float` | `8` | `2` |
| `AvatarStartSlotMarginEnd` | `float` | `16` | `16` |
| `BorderColor` | `Color` | `rgba(0, 0, 0, 0.07)` | `rgba(0, 0, 0, 0.2)` |
| `Color` | `Color` | `#000000` | `#000000` |
| `DividerBackground` | `Color` | `#f2f2f2` | `#f7f7f7` |
| `DividerColor` | `Color` | `#404040` | `#000000` |
| `DividerMinHeight` | `float` | `30` | `28` |
| `IconEndSlotMarginStart` | `float` | `16` | `0` |
| `IconSlotMarginVertical` | `float` | `12` | `7` |
| `IconStartSlotMarginEnd` | `float` | `32` | `16` |
| `LabelMarginEnd` | `float` | `0` | `8` |
| `LabelMarginVertical` | `float` | `10` | `10` |
| `MinHeight` | `float` | `48` | `44` |
| `OptionBackground` | `Color` | `#0054e9` | `#0054e9` |
| `OptionColor` | `Color` | `#ffffff` | `#ffffff` |
| `PaddingEnd` | `float` | `16` | `16` |
| `PaddingStart` | `float` | `16` | `16` |
| `ThumbnailSize` | `float` | `56` | `56` |
<!-- /miko:tokens -->

The divider's font size is the `HeaderFontSize` token of the [List](./list.md#theme-tokens) theme. Standalone avatar and thumbnail sizes are on the [Avatar](./avatar.md#theme-tokens) and [Thumbnail](./thumbnail.md#theme-tokens) pages.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-item` | The item host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-item .item-native` | The row surface (`div`, `button` for `Button`, `a` for `Href`): flex row, minimum height, leading padding, hover wash. |
| `.ion-item .item-inner` | Everything after the `Start` slot. Carries the trailing padding and the default divider. |
| `.ion-item .input-wrapper` | Wraps the default slot. |
| `.ion-item .item-native > .ion-slot-start`, `.ion-item .item-inner > .ion-slot-end` | The `Start` / `End` slot wrappers. The `ion-slot-*` classes are shared with other components, so anchor them like this. |
| `.ion-item .item-detail-icon` | The detail chevron. |
| `.item-lines-default`, `.item-lines-full`, `.item-lines-inset`, `.item-lines-none` | The resolved `Lines` (`default` when unset). |
| `.ion-activatable`, `.item-disabled` | Clickable (`Button` / `Href`), `Disabled`. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |
| `.item-last-in-list` | The last item of an inset list (stamped by `IonList`). |
| `.ion-item-divider`, `.item-divider-inner`, `.item-divider-wrapper` | The divider host, its inner row, and the default-slot wrapper. |
| `.ion-item-group` | The group. |
| `.ion-item-sliding`, `.item-sliding-open`, `.item-sliding-open-start`, `.item-sliding-open-end`, `.item-sliding-disabled` | The sliding host and its open / disabled state. |
| `.ion-item-options`, `.item-options-start`, `.item-options-end` | An options container and its `Side`. |
| `.ion-item-option`, `.ion-item-option .button-native` | An option host (carries the fill) and its clickable surface. |
| `.item-option-expandable`, `.item-option-disabled` | `Expandable`, `Disabled` on an option. |

```csharp
// GlobalStyles.cs — roomier rows in one list: <IonList Class="roomy">…</IonList>
[".roomy .ion-item .item-native"] = new()
{
    MinHeight = Length.Px(64),
},
```
