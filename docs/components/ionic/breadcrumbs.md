# Breadcrumbs

Breadcrumbs show where the user is in a hierarchy as a row of links, from the top level down to the current page.

`IonBreadcrumbs` is the container — a wrapping flex row that decides which crumb is active, which crumbs show a separator, and which collapse. `IonBreadcrumb` is one item. Put the `IonBreadcrumb`s inside an `IonBreadcrumbs`: outside it they are never marked active, keep their separator, and never collapse.

## Basic usage

The last crumb is automatically made active (the `ColorActive` color and `ActiveFontWeight` weight) and loses its separator. The separator is `/` in `md` mode and a forward chevron in `ios` mode.

<!-- miko:example examples/breadcrumbs/Basic.razor -->
```razor
@* The last breadcrumb becomes active and drops its separator automatically. *@
<IonBreadcrumbs>
    <IonBreadcrumb Href="/">Home</IonBreadcrumb>
    <IonBreadcrumb Href="/electronics">Electronics</IonBreadcrumb>
    <IonBreadcrumb Href="/electronics/cameras">Cameras</IonBreadcrumb>
    <IonBreadcrumb>Film</IonBreadcrumb>
</IonBreadcrumbs>
```
<!-- /miko:example -->

## Handling taps

`Href` renders the crumb as an anchor with the link look (`ion-activatable`). A tap raises `OnClick` and then navigates to `Href` through the app's `NavigationManager` — a forward push with the mode's transition, or as set by `RouterDirection` (`"back"`, `"root"`), the same as [`IonItem`](./item.md). To handle the tap yourself instead, leave out `Href` and use `OnClick`.

<!-- miko:example examples/breadcrumbs/Navigation.razor -->
```razor
@* A folder path: tapping a crumb trims the path back to it. No Href, so OnClick handles the
   tap instead of navigating. *@
<IonBreadcrumbs>
    @foreach (var folder in _path)
    {
        var target = folder;
        <IonBreadcrumb OnClick="@(() => Open(target))">@target</IonBreadcrumb>
    }
</IonBreadcrumbs>

@code {
    private List<string> _path = new() { "Documents", "Projects", "Miko", "Docs" };

    private void Open(string folder) => _path = _path.Take(_path.IndexOf(folder) + 1).ToList();
}
```
<!-- /miko:example -->

## Active and disabled

`Active` marks a crumb as the current one; once any crumb is explicitly active, the last crumb is no longer made active automatically. `Disabled` dims a crumb and blocks pointer input.

<!-- miko:example examples/breadcrumbs/States.razor -->
```razor
<IonBreadcrumbs>
    <IonBreadcrumb Href="#">Home</IonBreadcrumb>
    <IonBreadcrumb Href="#" Active="true">Electronics</IonBreadcrumb>
    <IonBreadcrumb Href="#" Disabled="true">Cameras</IonBreadcrumb>
    <IonBreadcrumb>Film</IonBreadcrumb>
</IonBreadcrumbs>
```
<!-- /miko:example -->

## Icons

`Start` and `End` place content before and after the label; an [`IonIcon`](./icon.md) there is sized to 18px with an 8px gap to the label. When you use a named slot, put the label in `<ChildContent>`.

<!-- miko:example examples/breadcrumbs/Icons.razor -->
```razor
<IonBreadcrumbs>
    <IonBreadcrumb Href="/">
        <Start>
            <IonIcon Icon="@Ionicons.Home" />
        </Start>
        <ChildContent>Home</ChildContent>
    </IonBreadcrumb>
    <IonBreadcrumb Href="/files">
        <Start>
            <IonIcon Icon="@Ionicons.Folder" />
        </Start>
        <ChildContent>Files</ChildContent>
    </IonBreadcrumb>
    <IonBreadcrumb>
        <ChildContent>Report.pdf</ChildContent>
        <End>
            <IonIcon Icon="@Ionicons.DocumentText" />
        </End>
    </IonBreadcrumb>
</IonBreadcrumbs>
```
<!-- /miko:example -->

## Custom separator

The `Separator` slot replaces the default glyph for that crumb. It is still removed from the last crumb.

<!-- miko:example examples/breadcrumbs/Separator.razor -->
```razor
@* Each crumb supplies its own separator; it is still removed from the last one. *@
<IonBreadcrumbs>
    @foreach (var label in new[] { "Home", "Electronics", "Cameras" })
    {
        <IonBreadcrumb Href="#">
            <ChildContent>@label</ChildContent>
            <Separator>
                <IonIcon Icon="@Ionicons.ArrowForwardCircle" />
            </Separator>
        </IonBreadcrumb>
    }
</IonBreadcrumbs>
```
<!-- /miko:example -->

## Collapsing

When there are more crumbs than `MaxItems`, the crumbs between the first `ItemsBeforeCollapse` (default 1) and the last `ItemsAfterCollapse` (default 1) are hidden behind a "…" indicator. Nothing collapses when `MaxItems` is null (the default) or when `ItemsBeforeCollapse + ItemsAfterCollapse` exceeds `MaxItems`.

Tapping the indicator raises `OnCollapsedClick` with the hidden crumbs; the component does not expand by itself. Expand by clearing `MaxItems`, or show the hidden crumbs elsewhere (Ionic suggests a popover) — the event carries their built `Element`s, so render such a list from your own crumb data.

<!-- miko:example examples/breadcrumbs/Collapse.razor -->
```razor
@* Six crumbs, MaxItems 4: the middle ones collapse behind "…". Tapping it expands them all. *@
<IonBreadcrumbs MaxItems="@_maxItems" OnCollapsedClick="Expand">
    <IonBreadcrumb Href="#">Home</IonBreadcrumb>
    <IonBreadcrumb Href="#">Electronics</IonBreadcrumb>
    <IonBreadcrumb Href="#">Photography</IonBreadcrumb>
    <IonBreadcrumb Href="#">Cameras</IonBreadcrumb>
    <IonBreadcrumb Href="#">Film</IonBreadcrumb>
    <IonBreadcrumb>35 mm</IonBreadcrumb>
</IonBreadcrumbs>

@code {
    private int? _maxItems = 4;

    private void Expand(IonBreadcrumbCollapsedClickEventArgs args) => _maxItems = null;
}
```
<!-- /miko:example -->

<!-- miko:example examples/breadcrumbs/CollapseCounts.razor -->
```razor
@* Keep two crumbs before the indicator and two after it. *@
<IonBreadcrumbs MaxItems="4" ItemsBeforeCollapse="2" ItemsAfterCollapse="2">
    <IonBreadcrumb Href="#">Home</IonBreadcrumb>
    <IonBreadcrumb Href="#">Electronics</IonBreadcrumb>
    <IonBreadcrumb Href="#">Photography</IonBreadcrumb>
    <IonBreadcrumb Href="#">Cameras</IonBreadcrumb>
    <IonBreadcrumb Href="#">Film</IonBreadcrumb>
    <IonBreadcrumb>35 mm</IonBreadcrumb>
</IonBreadcrumbs>
```
<!-- /miko:example -->

With `ItemsAfterCollapse="0"` the crumb carrying the indicator counts as the last one: it loses its separator and becomes active.

## Colors

`Color` on `IonBreadcrumbs` gives every crumb's label and separator that palette color, overriding the active crumb's color; `Color` on one `IonBreadcrumb` colors just that crumb's label. Slotted icons keep the `IconColor` / `IconColorActive` token colors either way. See [Theming](./theming.md#palette).

<!-- miko:example examples/breadcrumbs/Colors.razor -->
```razor
@* Color on the container recolors every crumb; on a crumb it recolors only that one. *@
<IonBreadcrumbs Color="primary">
    <IonBreadcrumb Href="#">Home</IonBreadcrumb>
    <IonBreadcrumb Href="#">Electronics</IonBreadcrumb>
    <IonBreadcrumb>Cameras</IonBreadcrumb>
</IonBreadcrumbs>

<IonBreadcrumbs>
    <IonBreadcrumb Href="#">Home</IonBreadcrumb>
    <IonBreadcrumb Href="#" Color="danger">Recalled</IonBreadcrumb>
    <IonBreadcrumb>Cameras</IonBreadcrumb>
</IonBreadcrumbs>
```
<!-- /miko:example -->

## API

<!-- miko:api IonBreadcrumbs IonBreadcrumb -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonBreadcrumbs

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`) applied to the breadcrumbs. |
| `ItemsAfterCollapse` | `int` | `1` | The number of breadcrumbs to show after the collapsed indicator (Ionic `itemsAfterCollapse`). |
| `ItemsBeforeCollapse` | `int` | `1` | The number of breadcrumbs to show before the collapsed indicator (Ionic `itemsBeforeCollapse`). If [`ItemsBeforeCollapse`](#ionbreadcrumbs) + [`ItemsAfterCollapse`](#ionbreadcrumbs) is greater than [`MaxItems`](#ionbreadcrumbs), the breadcrumbs will not be collapsed. |
| `MaxItems` | `int?` | `null` | The maximum number of breadcrumbs to show before collapsing (Ionic `maxItems`). When the crumb count exceeds this, the middle crumbs are collapsed behind a "…" indicator. No collapsing when null. |

| Event | Type | Description |
| --- | --- | --- |
| `OnCollapsedClick` | `EventCallback<IonBreadcrumbCollapsedClickEventArgs>` | Raised when the collapsed indicator is clicked (Ionic `ionCollapsedClick`); carries the collapsed crumbs so the app can reveal them (e.g. in a popover). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonBreadcrumb

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Active` | `bool` | `false` | If true, the breadcrumb takes on the active look. The enclosing [`IonBreadcrumbs`](#ionbreadcrumbs) sets this on the last breadcrumb when none is explicitly active. |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, .... |
| `Disabled` | `bool` | `false` | If true, the breadcrumb is dimmed and non-interactive. |
| `Href` | `string?` | `null` | The route the breadcrumb navigates to (Ionic `href`). When set the native element is an anchor with the link styling (`ion-activatable`), and a tap raises [`OnClick`](#ionbreadcrumb) and then navigates through the app's `NavigationManager`, as Ionic's `openURL` does. |
| `RouterDirection` | `string?` | `null` | Navigation direction for [`Href`](#ionbreadcrumb) (Ionic `routerDirection`): `"forward"` (default — push with the mode's transition), `"back"` (pop-style transition), or `"root"` (clear the history, no transition). |

| Event | Type | Description |
| --- | --- | --- |
| `OnClick` | `EventCallback` | Raised when the breadcrumb is tapped — before it navigates, when it has an [`Href`](#ionbreadcrumb). Not raised while [`Disabled`](#ionbreadcrumb). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
| `End` | `RenderFragment?` | Content shown after the label (Ionic `slot="end"`). |
| `Separator` | `RenderFragment?` | Custom separator content (Ionic `slot="separator"`). When set it replaces the default separator glyph (`"/"` on md, a forward chevron on ios). The enclosing [`IonBreadcrumbs`](#ionbreadcrumbs) still strips the whole separator from the last crumb. |
| `Start` | `RenderFragment?` | Content shown before the label (Ionic `slot="start"`). |
<!-- /miko:api -->

## Types

<!-- miko:type IonBreadcrumbCollapsedClickEventArgs -->
### IonBreadcrumbCollapsedClickEventArgs

Payload for [`IonBreadcrumbs.OnCollapsedClick`](#ionbreadcrumbs), raised when the collapsed indicator (the "…" button shown when [`IonBreadcrumbs.MaxItems`](#ionbreadcrumbs) hides the middle crumbs) is clicked. Mirrors Ionic's `ionCollapsedClick` event detail (`BreadcrumbCollapsedClickEventDetail`), which carries the collapsed breadcrumbs so the app can e.g. open them in a popover.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `CollapsedBreadcrumbs` | `IReadOnlyList<Element>` | — | The breadcrumb host elements currently hidden by the collapse, in document order (Ionic passes the `HTMLIonBreadcrumbElement`s; Miko passes their built `Element` subtrees). (read-only) |
<!-- /miko:type -->

## Styling

### Theme tokens

<!-- miko:tokens Breadcrumb -->
Theme token `IonicTheme.Breadcrumb` (`BreadcrumbToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Breadcrumb = new BreadcrumbToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `ActiveFontWeight` | `FontWeight` | `FontWeight.Medium` | `FontWeight.SemiBold` |
| `BorderRadius` | `float` | `0` | `4` |
| `Color` | `Color` | `#677483` | `#2d4665` |
| `ColorActive` | `Color` | `#03060b` | `#03060b` |
| `FontSize` | `float` | `16` | `16` |
| `IconColor` | `Color` | `#7d8894` | `#92a0b3` |
| `IconColorActive` | `Color` | `#222d3a` | `#242d39` |
| `IconFontSize` | `float` | `18` | `18` |
| `IconSlotMargin` | `float` | `8` | `8` |
| `IndicatorBackground` | `Color` | `#eef1f3` | `#e9edf3` |
| `IndicatorBorderRadius` | `float` | `2` | `4` |
| `IndicatorColor` | `Color` | `#73849a` | `#73849a` |
| `IndicatorHeight` | `float` | `18` | `18` |
| `IndicatorIconSize` | `float` | `22` | `22` |
| `IndicatorMarginX` | `float` | `14` | `14` |
| `IndicatorWidth` | `float` | `32` | `32` |
| `PaddingX` | `float` | `12` | `12` |
| `PaddingY` | `float` | `6` | `5` |
| `SeparatorColor` | `Color` | `#73849a` | `#73849a` |
| `SeparatorMarginX` | `float` | `10` | `10` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-breadcrumbs` | The container row. Also carries the mode (`md` / `ios`). |
| `.ion-breadcrumb` | One crumb host. |
| `.ion-breadcrumb .breadcrumb-native` | The label surface (`<a>` with `Href`, otherwise `<span>`), holding the label and the `Start` / `End` content. |
| `.ion-breadcrumb .breadcrumb-separator` | The separator (absent on the last crumb). |
| `.ion-breadcrumb .ion-slot-start`, `.ion-breadcrumb .ion-slot-end` | Wrappers around `Start` / `End` content. |
| `.breadcrumb-active`, `.breadcrumb-disabled` | The active crumb; `Disabled`. |
| `.ion-activatable` | A crumb with `Href`. |
| `.breadcrumb-collapsed` | A crumb hidden by `MaxItems` (its `.breadcrumb-native` is not displayed). |
| `.ion-breadcrumb .breadcrumbs-collapsed-indicator` | The "…" indicator. |
| `.ion-color-{name}` | `Color` on the container or a crumb (e.g. `.ion-color-primary`). |
| `.in-breadcrumbs-color` | A crumb inside a colored `IonBreadcrumbs`. |

```csharp
// GlobalStyles.cs — a larger, spaced trail: <IonBreadcrumbs Class="trail">…</IonBreadcrumbs>
[".ion-breadcrumbs.trail .ion-breadcrumb"] = new()
{
    FontSize = Length.Px(18),
},
[".ion-breadcrumbs.trail .breadcrumb-separator"] = new()
{
    MarginLeft = Length.Px(12),
    MarginRight = Length.Px(12),
},
```
