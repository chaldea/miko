# Action Sheet

An action sheet is a dialog that slides up from the bottom of the screen and offers a set of options. The user has to pick one or dismiss it before going back to the app.

`IonActionSheet` is the declarative component. `IonActionSheetController` creates the same sheet from code. See [Overlays](./overlays.md) for where overlays render, what has to be in place, and how dismiss results work.

## Basic usage

Show the sheet with `IsOpen`. `Buttons` takes a list of `IonActionSheetButton`. The first button with role `cancel` is moved into a separate group at the bottom; any further cancel buttons are not rendered. Tapping a button, tapping the backdrop, or pressing the system back button closes the sheet. With `BackdropDismiss="false"`, only the buttons close it.

<!-- miko:example examples/action-sheet/Basic.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Open action sheet</IonButton>

<IonActionSheet @bind-IsOpen="_isOpen"
                Header="Albums"
                Buttons="@_buttons" />

@code {
    private bool _isOpen;

    // The cancel-role button is pulled into its own group at the bottom.
    private readonly IReadOnlyList<IonActionSheetButton> _buttons =
    [
        new() { Text = "Delete", Role = "destructive" },
        new() { Text = "Share" },
        new() { Text = "Favorite" },
        new() { Text = "Cancel", Role = "cancel" },
    ];
}
```
<!-- /miko:example -->

## Buttons and results

`OnDidDismiss` reports the tapped button's `Role` and `Data`. A backdrop tap reports role `"backdrop"` and a back press role `"back"`, both with `null` data.

A button's `Role` also styles it: `destructive` uses the theme's destructive color, `selected` is bold, and `cancel` gets the cancel weight. `Icon` takes an Ionicons name and draws it before the text. `Handler` runs before the sheet closes. `Disabled` dims a non-cancel button and makes it ignore taps.

<!-- miko:example examples/action-sheet/Result.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Photo options</IonButton>
<p>Chosen: @(_chosen ?? "nothing")</p>

<IonActionSheet @bind-IsOpen="_isOpen"
                Header="Photo"
                SubHeader="IMG_0042.jpg"
                Buttons="@_buttons"
                OnDidDismiss="OnDismissed" />

@code {
    private bool _isOpen;
    private string? _chosen;

    private readonly IReadOnlyList<IonActionSheetButton> _buttons =
    [
        new() { Text = "Copy", Icon = Ionicons.Copy, Data = "copy" },
        new() { Text = "Edit", Icon = Ionicons.Pencil, Data = "edit" },
        new() { Text = "Archive", Icon = Ionicons.Archive, Data = "archive" },
        new() { Text = "Delete", Icon = Ionicons.TrashOutline, Role = "destructive", Data = "delete" },
        new() { Text = "Cancel", Icon = Ionicons.Close, Role = "cancel" },
    ];

    // Role is the tapped button's role ("cancel", "destructive", null), "backdrop" or "back";
    // Data is the tapped button's Data (null for the backdrop and the back button).
    private void OnDismissed(IonOverlayDismissEventArgs e)
        => _chosen = e.IsCancel ? null : e.Data as string;
}
```
<!-- /miko:example -->

## Using the controller

`IonActionSheetController.CreateAsync` takes an `IonActionSheetOptions` with the same header, buttons, and `BackdropDismiss` settings. `OnDidDismissAsync()` completes with the same role and data as `OnDidDismiss`.

<!-- miko:example examples/action-sheet/Controller.razor -->
```razor
@inject IonActionSheetController ActionSheetController

<IonButton OnClick="ShowAsync">Sort by…</IonButton>
<p>Sorted by: @_sort</p>

@code {
    private string _sort = "date";

    private async Task ShowAsync()
    {
        var sheet = await ActionSheetController.CreateAsync(new IonActionSheetOptions
        {
            Header = "Sort by",
            Buttons =
            [
                new() { Text = "Date", Data = "date" },
                new() { Text = "Name", Data = "name" },
                new() { Text = "Size", Data = "size" },
                new() { Text = "Cancel", Role = "cancel" },
            ],
        });
        await sheet.PresentAsync();

        var result = await sheet.OnDidDismissAsync();
        if (result.Data is string sort)
            _sort = sort;
    }
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonActionSheet -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonActionSheet

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `BackdropDismiss` | `bool` | `true` | If true (default), tapping the backdrop or pressing the system back button dismisses the sheet. |
| `Buttons` | `IReadOnlyList<IonActionSheetButton>` | `[]` | The buttons to show. The first cancel-role button is pulled into a separate bottom group; further cancel-role buttons are not rendered. |
| `Header` | `string?` | `null` | The optional title shown above the buttons. |
| `IsOpen` | `bool` | `false` | Whether the sheet is presented. Bind two-way with [`IsOpenChanged`](#ionactionsheet). Two-way bindable: `@bind-IsOpen`. |
| `OverlayId` | `string?` | `null` | Optional stable identifier used by the root overlay registry. |
| `SubHeader` | `string?` | `null` | The optional sub-title shown under the header. |
| `Trigger` | `string?` | `null` | Id of the element that opens the sheet when clicked (Ionic `trigger`), e.g. `<IonButton Id="open-sheet">`. Opening sets [`IsOpen`](#ionactionsheet) and raises [`IsOpenChanged`](#ionactionsheet). The page must be inside an [`IonApp`](./app.md#ionapp). |

| Event | Type | Description |
| --- | --- | --- |
| `IsOpenChanged` | `EventCallback<bool>` | Raised with the new open state when the sheet opens/closes. |
| `OnDidDismiss` | `EventCallback<IonOverlayDismissEventArgs>` | Raised when the sheet dismisses, with the tapped button's role and data, or role `"backdrop"` / `"back"` (null data) for a backdrop tap / system back press. |
<!-- /miko:api -->

## Types

<!-- miko:type IonActionSheetButton IonActionSheetOptions IonActionSheetController -->
### IonActionSheetButton

A single action-sheet button. Mirrors Ionic's `ActionSheetButton` interface. The [`Role`](#ionactionsheetbutton) drives styling and placement: the first `"cancel"` button is pulled into its own bottom group; `"destructive"` buttons render in the destructive (danger) color; `"selected"` renders bold. Every button — cancel included — runs its [`Handler`](#ionactionsheetbutton) on tap and then dismisses the sheet with its role and [`Data`](#ionactionsheetbutton).

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Text` | `string?` | `null` | The button label. |
| `Role` | `string?` | `null` | Optional role: `"cancel"`, `"destructive"`, `"selected"`, or a custom string. Stamped as `action-sheet-{role}`. |
| `Icon` | `string?` | `null` | Optional leading icon (an Ionicons name). |
| `CssClass` | `string?` | `null` | Extra CSS class(es) to add to the button. |
| `Data` | `object?` | `null` | Arbitrary data returned with the dismiss event when this button is tapped. |
| `Disabled` | `bool` | `false` | When true, the button is drawn dimmed and tapping it does nothing. Ignored for cancel buttons, as in Ionic. |
| `Handler` | `Action?` | `null` | Optional handler run when the button is tapped, before the sheet dismisses. |
| `IsCancel` | `bool` | `false` | True when [`Role`](#ionactionsheetbutton) is `"cancel"` (case-insensitive). (read-only) |

### IonActionSheetOptions

Options for [`IonActionSheetController.CreateAsync`](#ionactionsheetcontroller). Each property maps to the [`IonActionSheet`](#ionactionsheet) parameter of the same name.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Header` | `string?` | `null` | The optional title shown above the buttons. |
| `SubHeader` | `string?` | `null` | The optional sub-title shown under the header. |
| `Buttons` | `IReadOnlyList<IonActionSheetButton>` | `[]` | The buttons to show. |
| `BackdropDismiss` | `bool` | `true` | If true (default), a backdrop tap or the system back button dismisses the sheet. |
| `CssClass` | `string?` | `null` | Extra CSS class(es) for the sheet host (its `Class`). |

### IonActionSheetController

Creates action sheets from code (Ionic's `actionSheetController`). Inject it; `AddIonic()` registers it.

| Method | Returns | Description |
| --- | --- | --- |
| `CreateAsync(IonActionSheetOptions options)` | `Task<IonOverlayReference>` | Creates an action sheet from `options`. Call [`IonOverlayReference.PresentAsync`](./overlays.md#ionoverlayreference) on the result to show it. |
<!-- /miko:type -->

## Styling

### Theme tokens

<!-- miko:tokens ActionSheet -->
Theme token `IonicTheme.ActionSheet` (`ActionSheetToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.ActionSheet = new ActionSheetToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `BackdropColor` | `Color` | `#000000` | `#000000` |
| `BackdropOpacity` | `float` | `0.32` | `0.4` |
| `Background` | `Color` | `#ffffff` | `#f9f9f9` |
| `BorderRadius` | `float` | `0` | `13` |
| `ButtonBorderColor` | `Color` | `transparent` | `rgba(0, 0, 0, 0.08)` |
| `ButtonColor` | `Color` | `#262626` | `#0054e9` |
| `ButtonFontSize` | `float` | `16` | `20` |
| `ButtonHeight` | `float` | `52` | `56` |
| `ButtonJustify` | `JustifyContent` | `JustifyContent.FlexStart` | `JustifyContent.Center` |
| `ButtonPaddingX` | `float` | `16` | `14` |
| `ButtonPaddingY` | `float` | `12` | `14` |
| `CancelFontWeight` | `FontWeight` | `FontWeight.Normal` | `FontWeight.SemiBold` |
| `ContainerPaddingX` | `float` | `0` | `8` |
| `DestructiveColor` | `Color` | `#c5000f` | `#c5000f` |
| `EnterDuration` | `float` | `0.4` | `0.4` |
| `GroupMarginBottom` | `float` | `0` | `10` |
| `GroupMarginTop` | `float` | `0` | `10` |
| `IconFontSize` | `float` | `24` | `28` |
| `LeaveDuration` | `float` | `0.45` | `0.45` |
| `MaxWidth` | `float` | `500` | `500` |
| `SubTitleFontSize` | `float` | `14` | `13` |
| `TextAlign` | `TextAlign` | `TextAlign.Left` | `TextAlign.Center` |
| `TitleColor` | `Color` | `rgba(0, 0, 0, 0.54)` | `#999999` |
| `TitleFontSize` | `float` | `16` | `13` |
| `TitlePaddingX` | `float` | `16` | `10` |
| `TitlePaddingY` | `float` | `20` | `14` |
<!-- /miko:tokens -->

`IonicTheme.ActionSheet.BackdropColor` also colors the backdrop of [`IonLoading`](./loading.md).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-action-sheet` | The full-screen host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.action-sheet-open` | The sheet is open (drives the slide-up and backdrop fade). |
| `.action-sheet-mounted` | Open or still animating out: the host takes taps. |
| `.overlay-hidden` | Closed and settled: the host ignores taps. |
| `.action-sheet-backdrop` | The dim layer behind the sheet (also `.ion-backdrop`). |
| `.action-sheet-wrapper` | The sliding panel at the bottom edge. |
| `.action-sheet-container` | The column holding the button groups. |
| `.action-sheet-group` | A button group; `.action-sheet-group-cancel` is the separate cancel group. |
| `.action-sheet-title`, `.action-sheet-sub-title` | `Header` and `SubHeader`. |
| `.action-sheet-button` | One button; `.action-sheet-button-inner` is its icon + label row, `.action-sheet-icon` the icon. |
| `.action-sheet-{role}` | A button's `Role`, e.g. `.action-sheet-destructive`, `.action-sheet-cancel`, `.action-sheet-selected`. |

A button's `CssClass` and the sheet's `Class` (or `IonActionSheetOptions.CssClass`) add your own classes:

```csharp
// GlobalStyles.cs — <IonActionSheet Class="compact" …>
[".ion-action-sheet.compact .action-sheet-button"] = new()
{
    MinHeight = Length.Px(44),
},
```
