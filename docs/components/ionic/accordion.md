# Accordion

Accordions are collapsible sections: each has a header row that shows or hides the content beneath it, so long pages can be organized into groups the user expands on demand.

`IonAccordionGroup` owns which accordions are open; `IonAccordion` is one section with a `Header` and a `Content` slot.

An `IonAccordion` must be inside an `IonAccordionGroup`; without a group it never expands. Give an accordion a `Value` when you want to open it from code or bind the group's `Value`. Without one it gets a generated value, `ion-accordion-0`, `-1`, …, numbered by its position among the group's accordions that have no `Value`.

## Basic usage

The `Header` is usually an [`IonItem`](./item.md); tapping anywhere on the header row toggles the accordion. In a single-select group (the default), opening one accordion closes the one that was open. The content is not rendered into the layout while collapsed (`display: none`); expanding and collapsing are not animated.

<!-- miko:example examples/accordion/Basic.razor -->
```razor
@* Every accordion needs a Value, and must sit inside an IonAccordionGroup. *@
<IonAccordionGroup>
    <IonAccordion Value="shipping">
        <Header>
            <IonItem><IonLabel>Shipping</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">Orders ship within two business days.</div>
        </Content>
    </IonAccordion>
    <IonAccordion Value="returns">
        <Header>
            <IonItem><IonLabel>Returns</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">Return any item within 30 days.</div>
        </Content>
    </IonAccordion>
    <IonAccordion Value="warranty">
        <Header>
            <IonItem><IonLabel>Warranty</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">All products carry a one-year warranty.</div>
        </Content>
    </IonAccordion>
</IonAccordionGroup>
```
<!-- /miko:example -->

While an accordion is collapsed, a header item without an explicit `Lines` shows a full-width divider; while it is expanded the divider is removed.

## Controlling which accordion is open

`Value` on the group is the open accordion's value (`null` when all are closed). Use `@bind-Value` to read it and to open or close accordions from code. Without a binding the group tracks the open accordion itself. A `Value` you pass without binding it is applied again on every re-render of the parent, so bind it when the parent has other state.

<!-- miko:example examples/accordion/Value.razor -->
```razor
@* @bind-Value tracks the open accordion (null when all are closed) and opens one from code. *@
<IonAccordionGroup @bind-Value="_open">
    <IonAccordion Value="first">
        <Header>
            <IonItem><IonLabel>First</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">First content</div>
        </Content>
    </IonAccordion>
    <IonAccordion Value="second">
        <Header>
            <IonItem><IonLabel>Second</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">Second content</div>
        </Content>
    </IonAccordion>
</IonAccordionGroup>

<p class="ion-padding">Open: @(_open ?? "none")</p>
<IonButton OnClick="@(() => _open = _open == "second" ? null : "second")">Toggle second</IonButton>

@code {
    private string? _open = "first";
}
```
<!-- /miko:example -->

## Multiple open accordions

With `Multiple="true"` any number of accordions can be open. The group then reads and reports `Values` (an `IReadOnlyList<string>`, bound with `@bind-Values`) and ignores `Value`.

<!-- miko:example examples/accordion/Multiple.razor -->
```razor
@* With Multiple, the open set is Values (not Value). *@
<IonAccordionGroup Multiple="true" @bind-Values="_open">
    <IonAccordion Value="first">
        <Header>
            <IonItem><IonLabel>First</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">First content</div>
        </Content>
    </IonAccordion>
    <IonAccordion Value="second">
        <Header>
            <IonItem><IonLabel>Second</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">Second content</div>
        </Content>
    </IonAccordion>
    <IonAccordion Value="third">
        <Header>
            <IonItem><IonLabel>Third</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">Third content</div>
        </Content>
    </IonAccordion>
</IonAccordionGroup>

<p class="ion-padding">Open: @string.Join(", ", _open)</p>

@code {
    private IReadOnlyList<string> _open = new[] { "first", "third" };
}
```
<!-- /miko:example -->

## Disabled and readonly

`Disabled` dims an accordion and blocks its header and content; `Readonly` blocks them without dimming. Set either on one accordion or on the whole group. Neither stops the group from closing an accordion when another one opens in a single-select group.

<!-- miko:example examples/accordion/Disabled.razor -->
```razor
@* Disabled dims and blocks one accordion (or the whole group); Readonly only blocks. *@
<IonAccordionGroup Value="readonly">
    <IonAccordion Value="enabled">
        <Header>
            <IonItem><IonLabel>Enabled</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">Enabled content</div>
        </Content>
    </IonAccordion>
    <IonAccordion Value="disabled" Disabled="true">
        <Header>
            <IonItem><IonLabel>Disabled</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">Disabled content</div>
        </Content>
    </IonAccordion>
    <IonAccordion Value="readonly" Readonly="true">
        <Header>
            <IonItem><IonLabel>Readonly</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">Readonly content</div>
        </Content>
    </IonAccordion>
</IonAccordionGroup>
```
<!-- /miko:example -->

## Toggle icon

The toggle icon (default `Ionicons.ChevronDown`) sits at the end of the header and rotates 180° while the accordion is open. `ToggleIcon` changes the glyph per accordion; `ToggleIconSlot="start"` moves it before the header content — on the group for all accordions, or on one accordion to override the group.

<!-- miko:example examples/accordion/ToggleIcon.razor -->
```razor
@* The icon rotates 180° when the accordion opens; ToggleIconSlot moves it before the header. *@
<IonAccordionGroup ToggleIconSlot="start">
    <IonAccordion Value="first" ToggleIcon="@Ionicons.CaretDownCircle">
        <Header>
            <IonItem><IonLabel>First</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">First content</div>
        </Content>
    </IonAccordion>
    <IonAccordion Value="second" ToggleIcon="@Ionicons.CaretDownCircle">
        <Header>
            <IonItem><IonLabel>Second</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">Second content</div>
        </Content>
    </IonAccordion>
</IonAccordionGroup>
```
<!-- /miko:example -->

## Inset

`Expand="inset"` gives the group a margin and rounds each accordion's corners (with an elevation shadow in `md` mode).

<!-- miko:example examples/accordion/Inset.razor -->
```razor
<IonAccordionGroup Expand="inset">
    <IonAccordion Value="first">
        <Header>
            <IonItem><IonLabel>First</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">First content</div>
        </Content>
    </IonAccordion>
    <IonAccordion Value="second">
        <Header>
            <IonItem><IonLabel>Second</IonLabel></IonItem>
        </Header>
        <Content>
            <div class="ion-padding">Second content</div>
        </Content>
    </IonAccordion>
</IonAccordionGroup>
```
<!-- /miko:example -->

## API

<!-- miko:api IonAccordion IonAccordionGroup -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonAccordion

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | If true, this accordion is dimmed and non-interactive. |
| `Readonly` | `bool` | `false` | If true, this accordion is non-interactive but not dimmed. |
| `ToggleIcon` | `string` | `"chevron-down"` | The toggle icon rotated on expand/collapse. Defaults to a downward chevron. |
| `ToggleIconSlot` | `string?` | `null` | Slot in the header item for the toggle icon: `"start"` or `"end"`. Falls back to the group's setting, then `"end"`. |
| `Value` | `string?` | `null` | This accordion's value; matches the group's expanded value(s) to show its content. When unset, an `ion-accordion-{n}` value is generated (as in Ionic), numbered by the accordion's position among the group's accordions without a value. |

| Slot | Type | Description |
| --- | --- | --- |
| `Content` | `RenderFragment?` | The collapsible content (Ionic `slot="content"`). |
| `Header` | `RenderFragment?` | The header row content (Ionic `slot="header"`), typically an `IonItem`. |

### IonAccordionGroup

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Animated` | `bool` | `true` | Ionic's `animated`. Kept for API parity only: accordions never animate in this port. |
| `Disabled` | `bool` | `false` | If true, the group is dimmed and non-interactive. |
| `Expand` | `string` | `"compact"` | Expansion behavior: `"compact"` (default) or `"inset"`. |
| `Multiple` | `bool` | `false` | If true, more than one accordion can be expanded at once. |
| `Readonly` | `bool` | `false` | If true, the group is non-interactive but not dimmed. |
| `ToggleIconSlot` | `string` | `"end"` | Slot inside the header item to place the toggle icon: `"start"` or `"end"`. |
| `Value` | `string?` | `null` | The expanded accordion's value in a single-select group (null when all are collapsed). Ignored when [`Multiple`](#ionaccordiongroup) is true — use [`Values`](#ionaccordiongroup). Two-way bindable: `@bind-Value`. |
| `Values` | `IReadOnlyList<string>?` | `null` | The expanded accordion values when [`Multiple`](#ionaccordiongroup) is true; ignored otherwise. Two-way bindable: `@bind-Values`. |

| Event | Type | Description |
| --- | --- | --- |
| `ValueChanged` | `EventCallback<string?>` | Raised with the new single-select value when it changes (null when collapsed). |
| `ValuesChanged` | `EventCallback<IReadOnlyList<string>>` | Raised with the new multi-select value set when it changes. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Accordion -->
Theme token `IonicTheme.Accordion` (`AccordionToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Accordion = new AccordionToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `#ffffff` | `#ffffff` |
| `DisabledOpacity` | `float` | `0.4` | `0.4` |
| `InsetBorderRadius` | `float` | `6` | `6` |
| `InsetBoxShadow` | `List<BoxShadow>` | `0 3px 1px -2px rgba(0, 0, 0, 0.2), 0 2px 2px 0 rgba(0, 0, 0, 0.14), 0 1px 5px 0 rgba(0, 0, 0, 0.12)` | `none` |
| `InsetMargin` | `float` | `16` | `16` |
<!-- /miko:tokens -->

The header hover wash, the collapsed header's divider, and the toggle icon's end margin come from the [`Item`](./item.md) tokens (`Color`, `BorderColor`, `PaddingEnd`); the toggle icon uses the palette's `medium` color.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-accordion-group` | The group. Also carries the mode (`md` / `ios`) and the classes below. |
| `.accordion-group-expand-compact`, `.accordion-group-expand-inset` | `Expand`. |
| `.accordion-group-disabled`, `.accordion-group-readonly` | `Disabled`, `Readonly` on the group. |
| `.ion-accordion` | One accordion. |
| `.accordion-expanded`, `.accordion-collapsed` | The accordion's open state. |
| `.accordion-disabled`, `.accordion-readonly` | `Disabled`, `Readonly` (own or inherited from the group). |
| `.ion-accordion .accordion-header` | The clickable header row (the `Header` content plus the toggle icon). |
| `.ion-accordion .ion-accordion-toggle-icon` | The toggle icon wrapper; also `.accordion-toggle-icon-start` / `-end`. |
| `.ion-accordion .accordion-content` | The collapsible region (`display: none` while collapsed). |
| `.ion-accordion .accordion-content-wrapper` | Wraps the `Content`. |

```csharp
// GlobalStyles.cs — tint the toggle chevron with the brand color.
[".ion-accordion .ion-accordion-toggle-icon .ion-icon"] = new()
{
    Color = Color.FromHex("#0e7490"),
},
```
