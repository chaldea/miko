# Searchbar

Searchbars are rounded search fields with a search icon, a clear button, and an optional cancel button. `IonSearchbar` is typically placed in an [`IonToolbar`](./toolbar.md) above a list it filters.

## Basic usage

`Placeholder` defaults to `"Search"`; like other Miko text fields it is drawn only while the field is empty and not focused. `Disabled` dims the searchbar and blocks input.

<!-- miko:example examples/searchbar/Basic.razor -->
```razor
<IonSearchbar />
<IonSearchbar Placeholder="Search contacts" />
<IonSearchbar Disabled="true" />
```
<!-- /miko:example -->

## Filtering a list

Bind the text with `@bind-Value` and filter on it. `ValueChanged` is raised with the whole new value on every edit. If the work per keystroke is expensive (a query, a long list), handle `OnInput` and set `Debounce` instead: `OnInput` then fires once the user has paused for that many milliseconds, with the final value.

<!-- miko:example examples/searchbar/Filter.razor -->
```razor
<IonSearchbar Placeholder="Search cities" @bind-Value="_query" />
<IonList>
    @foreach (var city in Matches)
    {
        <IonItem>
            <IonLabel>@city</IonLabel>
        </IonItem>
    }
</IonList>

@code {
    private static readonly string[] Cities =
        ["Amsterdam", "Buenos Aires", "Cairo", "Geneva", "Kyoto", "Lima", "Oslo", "Seoul"];

    // Updated on every keystroke; there is no built-in debounce.
    private string? _query;

    private IEnumerable<string> Matches => string.IsNullOrWhiteSpace(_query)
        ? Cities
        : Cities.Where(c => c.Contains(_query, StringComparison.OrdinalIgnoreCase));
}
```
<!-- /miko:example -->

## Events

`OnInput` is raised on every edit, right after `ValueChanged`. `OnChange` is the commit event: it is raised on blur or Enter when the value changed during that focus (and after the cancel button clears a non-focused field), never per keystroke and never for programmatic changes. `OnFocus` and `OnBlur` follow the field's focus; tapping the clear or cancel button takes focus away from the field.

<!-- miko:example examples/searchbar/Events.razor -->
```razor
<IonSearchbar OnInput="@(v => _input = v)"
              OnChange="@(v => _committed = v)"
              OnFocus="@(() => _focused = true)"
              OnBlur="@(() => _focused = false)" />
<p>Typing: @_input</p>
<p>Committed: @_committed</p>
<p>@(_focused ? "Focused" : "Not focused")</p>

@code {
    private string? _input;      // OnInput: every edit
    private string? _committed;  // OnChange: on blur, if the value changed while focused
    private bool _focused;
}
```
<!-- /miko:example -->

## Clear button

With the default `ShowClearButton="always"` a clear button appears while there is a value. It follows the `Value` parameter, so bind `Value`: without a binding the button disappears as soon as the parent re-renders (for example after any of the searchbar's own callbacks). Tapping it raises `OnClear`, then clears the field and raises `ValueChanged` and `OnInput` with an empty string. `ShowClearButton="never"` removes it; `"focus"` shows it only while the field has focus (and has a value).

<!-- miko:example examples/searchbar/Clear.razor -->
```razor
@* The clear button needs a bound Value to appear. *@
<IonSearchbar @bind-Value="_query" OnClear="() => _cleared++" />
<p>Cleared @_cleared times</p>

@* No clear button at all. *@
<IonSearchbar ShowClearButton="never" />

@code {
    private string? _query = "coffee";
    private int _cleared;
}
```
<!-- /miko:example -->

## Cancel button

`ShowCancelButton="always"` adds a cancel button: in md mode an icon inside the field at the start (`CancelButtonIcon`, default arrow-back-sharp), in ios mode a text button after the field (`CancelButtonText`, default `"Cancel"`). Tapping it raises `OnCancel`, then clears the value (raising `ValueChanged` and `OnInput`) and takes focus away. `"focus"` shows the cancel button only while the field has focus. `Animated` has no animation — in ios mode it only keeps the cancel button visible.

In md mode the cancel button takes the search icon's place: the search icon is hidden while it shows.

<!-- miko:example examples/searchbar/Cancel.razor -->
```razor
@* md shows CancelButtonIcon inside the field; ios shows CancelButtonText after it. *@
<IonSearchbar ShowCancelButton="always" CancelButtonText="Done" @bind-Value="_query" OnCancel="HandleCancel" />
<p>@_status</p>

@code {
    private string? _query;
    private string _status = string.Empty;

    // Raised before the searchbar clears its value.
    private void HandleCancel() => _status = $"Cancelled search for \"{_query}\"";
}
```
<!-- /miko:example -->

## Icons

`SearchIcon` replaces the leading magnifier (default search-sharp on md, search-outline on ios) and `ClearIcon` the clear button's icon (default close-sharp on md, close-circle on ios). Both take an icon name such as an `Ionicons` constant.

<!-- miko:example examples/searchbar/Icons.razor -->
```razor
<IonSearchbar SearchIcon="@Ionicons.PersonOutline" Placeholder="Find people" />
<IonSearchbar ClearIcon="@Ionicons.Trash" Value="Clear with a trash icon" />
```
<!-- /miko:example -->

## Colors

`Color` fills the field with a palette color (see [Theming](./theming.md#palette)) and switches the text and icons to the color's contrast color, as in Ionic.

<!-- miko:example examples/searchbar/Colors.razor -->
```razor
@* Color fills the field; the text and icons switch to the palette's contrast color. *@
<IonSearchbar Color="light" />
<IonSearchbar Color="dark" />
```
<!-- /miko:example -->

## In a toolbar

A searchbar placed directly in an `IonToolbar` fills the toolbar's width.

<!-- miko:example examples/searchbar/Toolbar.razor -->
```razor
<IonToolbar>
    <IonSearchbar Placeholder="Search contacts" @bind-Value="_query" />
</IonToolbar>

@code {
    private string? _query;
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonSearchbar -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonSearchbar

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `CancelButtonIcon` | `string?` | `null` | Cancel button icon, shown in md mode (Ionic `cancelButtonIcon`). Defaults to `arrow-back-sharp`. |
| `CancelButtonText` | `string` | `"Cancel"` | Cancel button text, shown in iOS mode (Ionic `cancelButtonText`). |
| `ClearIcon` | `string?` | `null` | Clear icon name (Ionic `clearIcon`). Defaults to `close-sharp` (md) / `close-circle` (ios). |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class the stylesheet maps to the input fill. |
| `Debounce` | `int?` | `null` | Milliseconds to wait after the last edit before raising [`OnInput`](#ionsearchbar) (Ionic `debounce`). Every edit restarts the wait, so a burst of typing raises one [`OnInput`](#ionsearchbar) with the final value — use it to avoid filtering or querying on every keystroke. [`ValueChanged`](#ionsearchbar) (`@bind-Value`) and [`OnChange`](#ionsearchbar) are not delayed. Null or 0 (default) raises [`OnInput`](#ionsearchbar) on every edit. |
| `Disabled` | `bool` | `false` | When true, the searchbar is dimmed and non-interactive (Ionic `disabled`). |
| `Placeholder` | `string` | `"Search"` | Placeholder text (Ionic `placeholder`). Defaults to `"Search"`. |
| `SearchIcon` | `string?` | `null` | Search icon name (Ionic `searchIcon`). Defaults to `search-sharp` (md) / `search-outline` (ios). |
| `ShowCancelButton` | `string` | `"never"` | Cancel button behavior (Ionic `showCancelButton`): `"never"` (default), `"focus"` (shown while the field has focus), or `"always"`. |
| `ShowClearButton` | `string` | `"always"` | Clear button behavior (Ionic `showClearButton`): `"always"` (default), `"focus"` (shown while the field has focus), or `"never"`. The button appears only while the [`Value`](#ionsearchbar) parameter is non-empty, so it needs a bound value. |
| `Type` | `string` | `"search"` | Input type (Ionic `type`). Defaults to `"search"`; any value renders a plain text field (no password masking or numeric keypad). |
| `Value` | `string?` | `null` | The current value of the search field (Ionic `value`). Two-way bindable: `@bind-Value`. |

| Event | Type | Description |
| --- | --- | --- |
| `OnBlur` | `EventCallback` | Raised when the search field loses focus (Ionic `ionBlur`). |
| `OnCancel` | `EventCallback` | Raised when the cancel button is clicked (Ionic `ionCancel`). Fires before the value is cleared, like the Ionic source. |
| `OnChange` | `EventCallback<string?>` | Raised when the value change is committed (Ionic `ionChange`): when the field loses focus or Enter is pressed with a value different from the one it had on focus — a clear-button tap counts as a change — or when the cancel button clears an unfocused field. Not raised for programmatic value changes, matching Ionic. |
| `OnClear` | `EventCallback` | Raised when the clear button is clicked (Ionic `ionClear`). Fires before the value is cleared, like the Ionic source. |
| `OnFocus` | `EventCallback` | Raised when the search field gains focus (Ionic `ionFocus`). |
| `OnInput` | `EventCallback<string?>` | Raised each time the user modifies the value (Ionic `ionInput`), or once per burst of edits when [`Debounce`](#ionsearchbar) is set. |
| `ValueChanged` | `EventCallback<string?>` | Raised (with the new value) as the user types — enables `@bind-Value`. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Searchbar -->
Theme token `IonicTheme.Searchbar` (`SearchbarToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Searchbar = new SearchbarToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `CancelButtonBackground` | `Color` | `transparent` | `transparent` |
| `CancelButtonColor` | `Color` | `#1a1a1a` | `#0054e9` |
| `CancelButtonFontSize` | `float` | `24` | `17` |
| `ClearIconColor` | `Color` | `#666666` | `#666666` |
| `ClearIconSize` | `float` | `22` | `18` |
| `InputBackground` | `Color` | `#ffffff` | `rgba(0, 0, 0, 0.07)` |
| `InputBorderRadius` | `float` | `2` | `10` |
| `InputBoxShadow` | `List<BoxShadow>` | `0 2px 2px 0 rgba(0, 0, 0, 0.14), 0 3px 1px -2px rgba(0, 0, 0, 0.2), 0 1px 5px 0 rgba(0, 0, 0, 0.12)` | `none` |
| `InputFontSize` | `float` | `16` | `17` |
| `InputHeight` | `Length` | `auto` | `100%` |
| `InputLineHeight` | `Length` | `30px` | `30px` |
| `InputMinHeight` | `float` | `0` | `36` |
| `InputTextColor` | `Color` | `#262626` | `#000000` |
| `PaddingBottom` | `float` | `8` | `12` |
| `PaddingEnd` | `float` | `8` | `12` |
| `PaddingStart` | `float` | `8` | `12` |
| `PaddingTop` | `float` | `8` | `12` |
| `SearchIconColor` | `Color` | `#666666` | `#666666` |
| `SearchIconSize` | `float` | `21` | `22` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-searchbar` | The host: the padded row. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-searchbar .searchbar-input-container` | The positioning box around the field and its buttons. |
| `.ion-searchbar .searchbar-input` | The rounded native text field. |
| `.ion-searchbar .searchbar-search-icon` | The leading search icon. |
| `.ion-searchbar .searchbar-clear-button`, `.searchbar-clear-icon` | The clear button and its icon. |
| `.ion-searchbar .searchbar-cancel-button` | The cancel button (inside the container on md, after it on ios). |
| `.searchbar-has-value` | `Value` is non-empty. |
| `.searchbar-should-show-clear`, `.searchbar-should-show-cancel` | `ShowClearButton="always"`, `ShowCancelButton="always"`. |
| `.searchbar-disabled`, `.searchbar-animated` | `Disabled`, `Animated`. |
| `.searchbar-left-aligned` | Always present (the port has no centered-placeholder layout). |
| `.ion-color-{name}` | `Color` (e.g. `.ion-color-dark`). |

```csharp
// GlobalStyles.cs — readable text on a dark searchbar.
[".ion-searchbar.ion-color-dark .searchbar-input"] = new()
{
    Color = Color.FromHex("#ffffff"),
},
```
