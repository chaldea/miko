# Ionic Components Reference (`Miko.Ionic`)

Ionic-style Razor components: buttons, lists, tabs, cards, menus, etc. — a structural/visual port of the Ionic Framework.

## Setup

1. Reference the `Miko.Ionic` package.
2. Register it: `builder.AddIonic();` in `App.cs`.
3. Import: add `@using Miko.Ionic` and `@using Miko.Ionic.Components` (usually in `_Imports.razor`).

```csharp
builder.AddIonic();                          // installs styles + registers icons
builder.AddIonic(cfg => cfg.Mode = IonicMode.Ios);  // force iOS mode
```

## Conventions

- **`Color`** parameter takes a palette token: `"primary"`, `"secondary"`, `"tertiary"`, `"success"`, `"warning"`, `"danger"`, `"light"`, `"medium"`, `"dark"`.
- **`Style`** parameter is a `Miko.Styling.Style` **object**, not a CSS string. `ClassName` adds extra CSS classes.
- **Mode**: `ios` on iOS hosts, `md` (Material Design) elsewhere. Auto-detected from the platform; some components change defaults/DOM by mode (noted below).
- **Named slots** map to `RenderFragment` parameters (e.g. `Start`, `End`, `IconOnly`, `Content`, `Bar`).
- Most components take default `ChildContent`.

## Icons — `<IonIcon>`

`Ionicons` exposes PascalCase constants → kebab names (`Ionicons.Heart` = `"heart"`), each with `-outline`/`-sharp` variants.

```razor
<IonIcon Icon="@Ionicons.Heart" />
<IonIcon Icon="settings-outline" />
<IonIcon Icon="@Ionicons.Menu" Slot="start" />
```

| Param | Type | Notes |
| --- | --- | --- |
| `Icon` | `string?` | Kebab name or `Ionicons.*` constant. |
| `Slot` | `string?` | `icon-only` / `start` / `end` — lets parents (e.g. `IonButton`) detect it. |
| `Class` | `string?` | Extra classes. |

---

## App shell / structure

| Component | Key params | ChildContent | Notes / children |
| --- | --- | --- | --- |
| `<IonApp>` | — | yes | Root container. Holds an `<IonPage>` and (for menus) a sibling `<IonMenu>`. |
| `<IonPage>` | — | yes | A routed view. Holds `IonHeader` + `IonContent` (+ `IonFooter`). |
| `<IonHeader>` | — | yes | Top region; holds `IonToolbar`(s). |
| `<IonFooter>` | `Translucent` (`bool`), `Collapse` (`string?`) | yes | Bottom region (iOS translucency). |
| `<IonContent>` | `Fullscreen` (`bool`) | yes | Scrollable main area. |
| `<IonToolbar>` | slots below | yes | Bar in header/footer. |
| `<IonTitle>` | — | yes | Toolbar title. |

`<IonToolbar>` slots (all `RenderFragment?`): `ChildContent` (center, usually `IonTitle`), `Start`, `End`, `Primary`, `Secondary`. Put `IonButtons` in the edge slots.

```razor
<IonPage>
    <IonHeader>
        <IonToolbar>
            <IonButtons Slot="start"><IonMenuButton OnClick="OpenMenu" /></IonButtons>
            <IonTitle>Inbox</IonTitle>
        </IonToolbar>
    </IonHeader>
    <IonContent>…</IonContent>
</IonPage>
```

## Grid

| Component | Key params | Children |
| --- | --- | --- |
| `<IonGrid>` | `Fixed` (`bool`) | `IonRow` |
| `<IonRow>` | — | `IonCol` |
| `<IonCol>` | `Size`, `Offset`, `Push`, `Pull` (all `string?`, `"1"`–`"12"` or `"auto"`) | any |

## Navigation

| Component | Key params | Notes |
| --- | --- | --- |
| `<IonMenu>` | `IsOpen` (`bool`), `Side` (`"start"`/`"end"`), `Type` (`"overlay"`), `OnClose` (`EventCallback`), `Animations` (`AnimationManager?`) | Slide-in drawer; sibling of `IonPage` inside `IonApp`. Host owns `IsOpen` and calls `StateHasChanged()` on toggle. |
| `<IonMenuButton>` | `OnClick` (`EventCallback`) | Hamburger; put in a toolbar's `IonButtons Slot="start"`. |
| `<IonTabs>` | `Content` (`RenderFragment?`), `Bar` (`RenderFragment?`) | Tab shell. `Content` = `@Body`; `Bar` = an `IonTabBar`. |
| `<IonTabBar>` | `Slot` (`"bottom"`/`"top"`) | Holds `IonTabButton`s. |
| `<IonTabButton>` | `Tab` (`string?`), `Href` (`string?`), `Selected` (`bool`), `OnClick` (`EventCallback`) | Usually `IonIcon` + `IonLabel` children. |
| `<IonBackButton>` | `DefaultHref` (`string?`), `Color`, `Icon`, `Text`, `Disabled`, `OnClick` | In `IonButtons`. Hidden when `DefaultHref` null. |

## Buttons

| Component | Key params | Notes |
| --- | --- | --- |
| `<IonButton>` | `Color`, `Fill` (`"solid"`/`"outline"`/`"clear"`), `Expand` (`"block"`/`"full"`), `Shape` (`"round"`), `Size` (`"small"`/`"default"`/`"large"`), `Strong` (`bool`), `Disabled` (`bool`), `Href` (`string?`), `OnClick` (`EventCallback`) | Label via `ChildContent`. Slots: `IconOnly`, `Start`, `End`. |
| `<IonButtons>` | `Slot` (`"start"`/`"end"`) | Toolbar button group; wraps `IonButton`/`IonBackButton`/`IonMenuButton`. |

```razor
<IonButton Color="primary" Expand="block" OnClick="Save">Save</IonButton>
<IonButton Fill="clear"><IonIcon Slot="icon-only" Icon="@Ionicons.Heart" /></IonButton>
```

## Lists

| Component | Key params | Notes |
| --- | --- | --- |
| `<IonList>` | `Id` (`string?`) | Container. Children: items/headers/groups. |
| `<IonItem>` | `Lines` (`string?`, `"none"` hides hairline) | A row. Holds `IonLabel`, `IonIcon`, `IonAvatar`, controls. |
| `<IonLabel>` | — | Text label inside items/segments/tabs. |
| `<IonListHeader>` | — | Header at top of a list. |
| `<IonItemGroup>` | — | Logical grouping. |
| `<IonItemDivider>` | — | Section divider (usually in `IonItemGroup`). |
| `<IonItemSliding>` | `Open` (`bool`), `OpenSide` (`"start"`/`"end"`) | Swipe-to-reveal row (an `IonItem` + `IonItemOptions`). |
| `<IonItemOptions>` | `Side` (`"start"`/`"end"`) | Container of `IonItemOption`. |
| `<IonItemOption>` | `Color`, `Disabled`, `OnClick` | One swipe action. |

```razor
<IonList>
    <IonListHeader>Inbox</IonListHeader>
    <IonItem><IonIcon Slot="start" Icon="@Ionicons.Mail" /><IonLabel>Message 1</IonLabel></IonItem>
    <IonItem Lines="none"><IonLabel>Message 2</IonLabel></IonItem>
</IonList>
```

## Cards

| Component | Key params | Notes |
| --- | --- | --- |
| `<IonCard>` | `Color`, `Button` (`bool`), `Href`/`Target`/`Rel`, `Disabled`, `OnClick` | Card container. |
| `<IonCardHeader>` | `Color`, `Translucent` (`bool`) | Holds title/subtitle. |
| `<IonCardTitle>` | `Color` | Title (inside header). |
| `<IonCardSubtitle>` | `Color` | Subtitle (inside header). |
| `<IonCardContent>` | — | Body. |

```razor
<IonCard>
    <IonCardHeader>
        <IonCardTitle>Card title</IonCardTitle>
        <IonCardSubtitle>Subtitle</IonCardSubtitle>
    </IonCardHeader>
    <IonCardContent>Body text.</IonCardContent>
</IonCard>
```

## Forms / inputs

| Component | Key params | Notes |
| --- | --- | --- |
| `<IonSelect>` | `Value`+`ValueChanged` (`string?`, two-way), `OnChange`, `Placeholder`, `Label`, `LabelPlacement`, `Multiple`, `Disabled`, `Fill`, `Shape`, `HelperText`, `ErrorText` | Wraps `IonSelectOption` children. |
| `<IonSelectOption>` | `Value` (`string?`), `Disabled`, `Selected` | Option text via `ChildContent`. |
| `<IonSearchbar>` | `Value`, `Placeholder`, `ShowCancelButton`, `ShowClearButton`, `Debounce` (`int?`) | Search input. |
| `<IonSegment>` | `Value`+`ValueChanged` (two-way), `Disabled` | Segmented control; wraps `IonSegmentButton`. |
| `<IonSegmentButton>` | `Value` (`string`), `ContentId` (`string?`), `Disabled`, `Layout` | Usually `IonLabel`/`IonIcon` child. |
| `<IonSegmentView>` | `Value` (`string?`) | Shows one `IonSegmentContent` matching `Value`. |
| `<IonSegmentContent>` | `Id` (`string?`) | One page; hidden when `Id != active`. |

Segment pattern: bind one field to both `IonSegment.Value` and `IonSegmentView.Value`; each `IonSegmentContent.Id` matches an `IonSegmentButton.Value`.

> There is no `IonCheckbox`/`IonInput`/`IonToggle` in this port. For a checkbox/text field use the raw `<input>` element (see [elements/index.md](../elements/index.md)).

## Display

| Component | Key params | Notes |
| --- | --- | --- |
| `<IonAvatar>` | — | Circular clip (wraps an `<img>`). |
| `<IonBadge>` | `Color` | Inline count/status. |
| `<IonChip>` | `Color`, `Outline` (`bool`), `Disabled` | Chip/tag. |
| `<IonIcon>` | see [Icons](#icons--ionicon) | — |
| `<IonSpinner>` | `Color`, `Name` (`string?`), `Duration` (`int?`), `Paused` (`bool`) | `Name`: `bubbles`/`circles`/`circular`/`crescent`/`dots`/`lines`/`lines-small`/… |

## Scroll interactions & slides

| Component | Key params | Notes |
| --- | --- | --- |
| `<IonRefresher>` + `<IonRefresherContent>` | `PullMin` (`int`), `Disabled`, `State`, `OnRefresh` (`EventCallback`) | Pull-to-refresh at top of `IonContent`. Call `Complete()` when done. |
| `<IonInfiniteScroll>` + `<IonInfiniteScrollContent>` | `Threshold` (`string`), `Position`, `Disabled`, `Loading`, `OnInfinite` | Load-more at bottom. Call `Complete()` when done. |
| `<IonSlides>` + `<IonSlide>` | `Pager`, `Scrollbar` (`bool`), `ActiveIndex` (`int`) | Carousel (resting position is declarative). |

---

## Parent/child rules (must-follow)

| Child | Must be inside |
| --- | --- |
| `IonItem`, `IonListHeader`, `IonItemGroup`, `IonItemDivider` | `IonList` |
| `IonCol` | `IonRow` → `IonGrid` |
| `IonRow` | `IonGrid` |
| `IonCardHeader`/`IonCardContent` | `IonCard`; `IonCardTitle`/`IonCardSubtitle` in `IonCardHeader` |
| `IonSegmentButton` | `IonSegment` |
| `IonSegmentContent` | `IonSegmentView` |
| `IonTabBar` (in `Bar` slot) → `IonTabButton` | `IonTabs` |
| `IonSelectOption` | `IonSelect` |
| `IonSlide` | `IonSlides` |
| `IonButtons` (→ buttons) | `IonToolbar` slots |
| `IonTitle`, `IonToolbar` | `IonToolbar` in `IonHeader`/`IonFooter` |
| `IonMenu` | `IonApp` (sibling of `IonPage`) |

See working layouts in [examples/tabs.md](../examples/tabs.md) and [examples/sidemenu.md](../examples/sidemenu.md).
