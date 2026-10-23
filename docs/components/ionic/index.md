# Ionic Components

`Miko.Ionic` is a port of the [Ionic Framework](https://ionicframework.com/docs/components) component library to Miko: the same components, slots and visual modes, rendered natively. Each page below shows usage examples, the full parameter / event / slot reference, the theme tokens with their default values, and the CSS classes an app stylesheet can target.

The parameter tables, token tables and code examples are generated from the library and compiled against it, so they always match the version you build with.

## Setup

1. Reference the `Miko.Ionic` package (every app template already does).
2. Register the library in `App.cs`:

   ```csharp
   builder.AddIonic();
   ```

3. Import the namespaces, usually in `_Imports.razor`:

   ```razor
   @using Miko.Ionic
   @using Miko.Ionic.Components
   ```

`AddIonic()` installs the global utility styles, registers the overlay controllers, and attaches
each component's styles the first time it renders. Icons are bundled as embedded SVGs; no font
registration is needed.

## Conventions

- **`Class`, `Style` and `Id`.** Every component accepts `Class` (extra CSS classes, added to the
  component's root element), `Style` (an inline `Style` **object**, not a CSS string) and `Id`
  (the root element's id — what an overlay's [`Trigger`](./overlays.md#opening-from-a-trigger)
  refers to). These are not repeated in each component's table.
- **`Color`** takes a palette name: `"primary"`, `"secondary"`, `"tertiary"`, `"success"`,
  `"warning"`, `"danger"`, `"light"`, `"medium"`, or `"dark"`. The colors come from the theme's
  [palette](./theming.md#palette).
- **String-valued options.** As in Ionic, options such as `Fill`, `Expand`, `Shape` and `Size`
  are strings (`Fill="outline"`). Unknown values fall back to the default.
- **Named slots** are `RenderFragment` parameters. When you fill a named slot, put the default
  content in an explicit `<ChildContent>`:

  ```razor
  <IonItem>
      <Start><IonIcon Icon="@Ionicons.Mail" /></Start>
      <ChildContent><IonLabel>Inbox</IonLabel></ChildContent>
  </IonItem>
  ```

- **Two-way binding.** A parameter with a matching `…Changed` callback can be bound with
  `@bind-…`, e.g. `@bind-Value`, `@bind-Checked`.
- **Events** are `EventCallback`s: the handler may be sync or async, and the component that
  owns the handler re-renders afterwards.
- **Mode.** Components render in `md` (Material Design) or `ios` style. The mode follows the
  host platform — `ios` on iOS, `md` everywhere else, including the desktop — and can be forced
  with `AddIonic(o => o.Platform = HostPlatform.Ios)`. Several defaults differ per mode; the
  token tables list both. See [Theming](./theming.md#mode).
- **Structure.** Many components only work inside the parent they belong to (see
  [Where components go](#where-components-go)). App pages follow `IonApp` › `IonPage` › `IonHeader` / `IonContent` / `IonFooter` — see
  [App and Page](./app.md).

## Where components go

Components marked **(context)** read state from their parent through a cascading value and do
not work without it — a `IonSegmentButton` outside an `IonSegment` cannot be selected. The other
placements are what the styles and layout expect.

| Component | Place inside |
| --- | --- |
| `IonPage` | `IonApp` (usually in the layout) |
| `IonHeader`, `IonContent`, `IonFooter` | `IonPage` |
| `IonToolbar` | `IonHeader` or `IonFooter` |
| `IonTitle`, `IonButtons` | `IonToolbar` — `IonButtons` in a toolbar slot such as `Start` / `End` |
| `IonBackButton`, `IonMenuButton` | `IonButtons` |
| `IonItem`, `IonListHeader`, `IonItemGroup`, `IonItemSliding` | `IonList` |
| `IonItemOptions` › `IonItemOption` | `IonItemSliding` |
| `IonCol` › `IonRow` › `IonGrid` | each inside the next |
| `IonCardHeader`, `IonCardContent` | `IonCard`; `IonCardTitle` / `IonCardSubtitle` inside `IonCardHeader` |
| `IonSegmentButton` **(context)** | `IonSegment` |
| `IonSegmentContent` | `IonSegmentView` |
| `IonSelectOption` **(context)** | `IonSelect` |
| `IonRadio` **(context)** | `IonRadioGroup` |
| `IonAccordion` **(context)** | `IonAccordionGroup` |
| `IonBreadcrumb` **(context)** | `IonBreadcrumbs` |
| `IonReorder` **(context)** | `IonReorderGroup` |
| `IonFabButton`, `IonFabList` **(context)** | `IonFab` |
| `IonPickerColumnOption` **(context)** › `IonPickerColumn` | `IonPicker` |
| `IonTabButton` **(context)** › `IonTabBar` | `IonTabs` |
| `IonSlide` | `IonSlides` |
| `IonRefresherContent` / `IonInfiniteScrollContent` | `IonRefresher` / `IonInfiniteScroll` |
| Overlays (`IonModal`, `IonAlert`, `IonToast`, …) | anywhere; controller-created overlays render in the overlay layer that `IonApp` hosts — see [Overlays](./overlays.md) for where declared ones render |
| `IonMenu` | `IonApp`, as a sibling of the page content |

Form controls (`IonInput`, `IonCheckbox`, `IonToggle`, `IonRadio`, `IonSelect`, `IonTextarea`)
also adapt when they sit inside an `IonItem`. Each component page states its requirements in
detail.

## Utility classes

`AddIonic()` also installs Ionic's global utility classes, usable on any element or component
through `class` / `Class`:

| Group | Classes |
| --- | --- |
| Padding / margin (16px) | `ion-padding`, `ion-padding-top` / `-bottom` / `-start` / `-end` / `-vertical` / `-horizontal`, `ion-no-padding`; the same for `ion-margin…` |
| Text | `ion-text-center`, `ion-text-start`, `ion-text-end`, `ion-text-left`, `ion-text-right`, `ion-text-justify`, `ion-text-wrap`, `ion-text-nowrap`, `ion-text-uppercase`, `ion-text-lowercase`, `ion-text-capitalize` |
| Display | `ion-hide`, `ion-display-none` / `-inline` / `-inline-block` / `-block` / `-flex` / `-inline-flex` / `-grid` |
| Flex container | `ion-flex-row` / `-row-reverse` / `-column` / `-column-reverse`, `ion-flex-wrap` / `-nowrap` / `-wrap-reverse`, `ion-justify-content-start` / `-end` / `-center` / `-between` / `-around` / `-evenly`, `ion-align-items-…`, `ion-align-content-…` |
| Flex item | `ion-flex-1`, `ion-flex-auto`, `ion-flex-initial`, `ion-flex-none`, `ion-flex-grow-0` / `-1`, `ion-flex-shrink-0` / `-1`, `ion-align-self-…` |

## Components

<!-- miko:component-index ./ -->
| Page | Components | Summary |
| --- | --- | --- |
| [Accordion](./accordion.md) | `IonAccordion`, `IonAccordionGroup` | Accordions are collapsible sections: each has a header row that shows or hides the content beneath it, so long pages can be organized into groups the user expands on demand. |
| [Action Sheet](./action-sheet.md) | `IonActionSheet` | An action sheet is a dialog that slides up from the bottom of the screen and offers a set of options. |
| [Alert](./alert.md) | `IonAlert` | An alert is a dialog that shows a message, or asks for a choice, on top of the app. |
| [App and Page](./app.md) | `IonApp`, `IonPage` | `IonApp` is the root container of an Ionic app, and `IonPage` is the root of each routed page; together they set up the screen structure every other Ionic component expects. |
| [Avatar](./avatar.md) | `IonAvatar` | An avatar is a circular image, usually a picture of a person or an object, shown on its own or inside a chip or an item. |
| [Back Button](./back-button.md) | `IonBackButton` | A back button navigates to the previous page: it pops the navigation history when there is something to pop, and otherwise goes to a default page. |
| [Badge](./badge.md) | `IonBadge` | A badge is a small pill of bold text, usually a count or a short status, shown next to another element to flag new or pending content. |
| [Breadcrumbs](./breadcrumbs.md) | `IonBreadcrumb`, `IonBreadcrumbs` | Breadcrumbs show where the user is in a hierarchy as a row of links, from the top level down to the current page. |
| [Button](./button.md) | `IonButton`, `IonButtons` | Buttons provide a clickable element for actions anywhere in an app. |
| [Card](./card.md) | `IonCard`, `IonCardContent`, `IonCardHeader`, `IonCardSubtitle`, `IonCardTitle` | Cards are raised, rounded surfaces that group related content — a header with a title and subtitle, body text, media, lists, and actions. |
| [Checkbox](./checkbox.md) | `IonCheckbox` | Checkboxes let the user select any number of options from a set, or switch a single option on or off. |
| [Chip](./chip.md) | `IonChip` | A chip is a compact, pill-shaped element for a tag, a filter, or a contact, holding text plus an optional icon or avatar. |
| [Content](./content.md) | `IonContent` | Content is the scrollable main area of a page, between the header and the footer. |
| [Datetime](./datetime.md) | `IonDatetime`, `IonDatetimeButton` | Datetime is an inline month calendar for picking a single date. |
| [Floating Action Button](./fab.md) | `IonFab`, `IonFabButton`, `IonFabList` | A floating action button (FAB) is a circular button that floats above the content at a corner, edge, or the center of the screen, for the screen's primary action; it can also open lists of related actions. |
| [Footer](./footer.md) | `IonFooter` | A footer is the bottom region of a page: a wrapper for one or more toolbars that stays below the scrolling content. |
| [Grid](./grid.md) | `IonCol`, `IonGrid`, `IonRow` | The grid is a 12-column flexbox layout system for arranging content in rows and columns. |
| [Header](./header.md) | `IonHeader` | A header is the top region of a page: a wrapper for one or more toolbars that stays above the scrolling content. |
| [Icon](./icon.md) | `IonIcon` | Icons display a glyph from the bundled Ionicons set, or a custom SVG, as a square box that scales with the font size and takes the surrounding text color. |
| [Infinite Scroll](./infinite-scroll.md) | `IonInfiniteScroll`, `IonInfiniteScrollContent` | Infinite scroll calls the app to load more data when the user scrolls near the end of a scrolling list. |
| [Input](./input.md) | `IonInput` | Inputs are single-line text fields with an optional label, placeholder, helper and error text, clear button, and character counter. |
| [Input OTP](./input-otp.md) | `IonInputOtp` | Input OTP is a row of single-character boxes for entering one-time codes such as SMS or authenticator codes. |
| [Item](./item.md) | `IonItem`, `IonItemDivider`, `IonItemGroup`, `IonItemOption`, `IonItemOptions`, `IonItemSliding` | Items are the rows of a list: each one holds text plus optional icons, avatars, thumbnails, notes, badges, buttons, or a form control, and can be made tappable or swipeable. |
| [Label](./label.md) | `IonLabel` | A label is the text content of an item, or a standalone line of text styled to match Ionic components. |
| [List](./list.md) | `IonList`, `IonListHeader` | A list is a vertical container of rows, usually items, for content such as settings, contacts, or search results. |
| [Loading](./loading.md) | `IonLoading` | A loading indicator is an overlay that shows a spinner and an optional message, and blocks interaction with the app while work is in progress. |
| [Menu](./menu.md) | `IonMenu`, `IonMenuButton` | A menu is a navigation drawer that slides in from the side of the screen over the current page, with a dimmed backdrop behind it. |
| [Modal](./modal.md) | `IonModal` | A modal is a dialog that covers the app with its own content, typically a header and scrollable content like a page, until the user dismisses it. |
| [Note](./note.md) | `IonNote` | A note is a short piece of muted, smaller text for metadata, such as a count, a size, or a timestamp beside a list item. |
| [Picker](./picker.md) | `IonPicker`, `IonPickerColumn`, `IonPickerColumnOption` | Pickers display one or more scrollable wheels of options, each with a single selected value, inline in the page. |
| [Popover](./popover.md) | `IonPopover` | A popover is a small floating panel that opens next to the element that triggered it. |
| [Progress Bar](./progress-bar.md) | `IonProgressBar` | A progress bar is a thin horizontal bar that shows how far an operation has progressed, or — when the amount is unknown — that it is still running. |
| [Radio](./radio.md) | `IonRadio`, `IonRadioGroup` | Radios let the user pick exactly one option from a set. |
| [Range](./range.md) | `IonRange` | Ranges are sliders for picking a number between a minimum and a maximum by dragging a knob along a bar. |
| [Refresher](./refresher.md) | `IonRefresher`, `IonRefresherContent` | The refresher provides pull-to-refresh on a scrolling content area: pulling down from the top reveals an indicator, and releasing far enough starts a refresh. |
| [Reorder](./reorder.md) | `IonReorder`, `IonReorderGroup` | Reorder lets the user drag items of a list into a new order by a handle. |
| [Searchbar](./searchbar.md) | `IonSearchbar` | Searchbars are rounded search fields with a search icon, a clear button, and an optional cancel button. |
| [Segment](./segment.md) | `IonSegment`, `IonSegmentButton`, `IonSegmentContent`, `IonSegmentView` | Segments display a row of buttons, of which exactly one is selected, for switching between views or filters within a page. |
| [Select](./select.md) | `IonSelect`, `IonSelectOption` | Selects are form controls that let the user pick one or several values from a set of options; tapping the select opens an overlay listing the options. |
| [Skeleton Text](./skeleton-text.md) | `IonSkeletonText` | Skeleton text is a gray placeholder bar shown in place of text or media while the real content loads. |
| [Slides](./slides.md) | `IonSlide`, `IonSlides` | Slides is a swipeable carousel that shows one slide at a time, with optional arrows, pagination bullets, a scrollbar, autoplay, looping, and a fade effect. |
| [Spinner](./spinner.md) | `IonSpinner` | A spinner is an animated indicator that shows something is loading or in progress, with no measure of how far along it is. |
| [Tabs](./tabs.md) | `IonTabBar`, `IonTabButton`, `IonTabs` | Tabs are a top-level navigation pattern: a tab bar of buttons at the bottom (or top) of the screen switches between independent root pages. |
| [Text](./text.md) | `IonText` | Text is an inline wrapper that colors the text inside it with a palette color, for anything from a single word to a whole heading. |
| [Textarea](./textarea.md) | `IonTextarea` | Textareas are multi-line text fields with an optional label, placeholder, helper and error text, and character counter. |
| [Thumbnail](./thumbnail.md) | `IonThumbnail` | A thumbnail is a square preview image, typically shown at the start or end of a list item. |
| [Title](./title.md) | `IonTitle` | A title is the text label of a toolbar, naming the current screen or section. |
| [Toast](./toast.md) | `IonToast` | A toast is a short, non-blocking notification that appears at the top, middle, or bottom of the screen, typically to confirm an action or report a status. |
| [Toggle](./toggle.md) | `IonToggle` | Toggles are switches that turn a single option on or off. |
| [Toolbar](./toolbar.md) | `IonToolbar` | A toolbar is a horizontal bar that holds a title, buttons, and other controls for the current screen, usually placed in an `IonHeader` or `IonFooter`. |
<!-- /miko:component-index -->

Related guides: [Theming](./theming.md) (tokens, palette, dark mode, overriding styles) and
[Overlays](./overlays.md) (modals, alerts, toasts and their controllers).
