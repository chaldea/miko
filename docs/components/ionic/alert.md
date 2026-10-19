# Alert

An alert is a dialog that shows a message, or asks for a choice, on top of the app. The user has to answer it before going back to the app.

`IonAlert` is the declarative component. `IonAlertController` creates the same alert from code. See [Overlays](./overlays.md) for where overlays render, what has to be in place, and how dismiss results work.

## Basic usage

`Header`, `SubHeader`, and `Message` fill the alert. All three are plain text. `Buttons` takes a list of `IonAlertButton`; `IonAlertButton.FromText("OK")` builds a button from its label. Bind `IsOpen` with `@bind-IsOpen`. Without the binding, the alert stays on screen after a button tap.

<!-- miko:example examples/alert/Basic.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Show alert</IonButton>

<IonAlert @bind-IsOpen="_isOpen"
          Header="Update available"
          SubHeader="Version 2.4"
          Message="Restart the app to finish installing the update."
          Buttons="@_buttons" />

@code {
    private bool _isOpen;

    // FromText builds a plain button; the role is "cancel" only for the text "Cancel".
    private readonly IReadOnlyList<IonAlertButton> _buttons = [IonAlertButton.FromText("OK")];
}
```
<!-- /miko:example -->

## Buttons

Any button closes the alert. `OnDidDismiss` reports the tapped button's `Role`, or `"backdrop"` when the user tapped outside the alert. A button's optional `Handler` runs before the alert closes; it cannot keep the alert open. With one or two buttons they sit in a row; with three or more, they stack vertically. `BackdropDismiss="false"` leaves only the buttons to close it. The system back button does not close an alert in this port.

<!-- miko:example examples/alert/Buttons.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Sign out</IonButton>
<p>@_status</p>

<IonAlert @bind-IsOpen="_isOpen"
          Header="Sign out?"
          Message="You will need to sign in again to sync your notes."
          Buttons="@_buttons"
          OnDidDismiss="OnDismissed" />

@code {
    private bool _isOpen;
    private string _status = "Signed in";

    private readonly IReadOnlyList<IonAlertButton> _buttons =
    [
        new() { Text = "Cancel", Role = "cancel" },
        new() { Text = "Sign out", Role = "confirm" },
    ];

    // Role is the tapped button's role, or "backdrop" when the user tapped outside.
    private void OnDismissed(IonOverlayDismissEventArgs e)
    {
        if (e.Role == "confirm")
            _status = "Signed out";
    }
}
```
<!-- /miko:example -->

## Radio inputs

`Inputs` adds a group of `IonAlertInput`s. All inputs of one alert must be the same kind, because the alert renders the group matching the first input's `Type`. With `Type = "radio"`, tapping a row selects it. On dismiss, `Data` is the checked input's `Value`, or `null` when none is checked.

<!-- miko:example examples/alert/Radio.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Theme: @_theme</IonButton>

<IonAlert @bind-IsOpen="_isOpen"
          Header="Theme"
          Inputs="@_inputs"
          Buttons="@_buttons"
          OnDidDismiss="OnDismissed" />

@code {
    private bool _isOpen;
    private string _theme = "system";

    // Tapping a radio checks it and unchecks the others (it updates these objects' Checked).
    private readonly IReadOnlyList<IonAlertInput> _inputs =
    [
        new() { Type = "radio", Label = "Light", Value = "light" },
        new() { Type = "radio", Label = "Dark", Value = "dark" },
        new() { Type = "radio", Label = "System", Value = "system", Checked = true },
    ];

    private readonly IReadOnlyList<IonAlertButton> _buttons =
    [
        new() { Text = "Cancel", Role = "cancel" },
        new() { Text = "OK", Role = "confirm" },
    ];

    // For radio inputs Data is the checked input's Value.
    private void OnDismissed(IonOverlayDismissEventArgs e)
    {
        if (e.Role == "confirm" && e.Data is string theme)
            _theme = theme;
    }
}
```
<!-- /miko:example -->

Tapping a row changes `Checked` on your `IonAlertInput` objects, and the alert reports `Data` on every dismiss, including Cancel and backdrop taps. Check the role before applying it. Also note that a choice made before Cancel stays checked the next time the same list is shown.

## Checkbox inputs

With `Type = "checkbox"`, each row toggles on its own. `Data` is a `List<string?>` of the checked inputs' `Value`s.

<!-- miko:example examples/alert/Checkbox.razor -->
```razor
<IonButton OnClick="@(() => _isOpen = true)">Toppings (@_toppings.Count)</IonButton>

<IonAlert @bind-IsOpen="_isOpen"
          Header="Toppings"
          Inputs="@_inputs"
          Buttons="@_buttons"
          OnDidDismiss="OnDismissed" />

@code {
    private bool _isOpen;
    private List<string?> _toppings = ["cheese"];

    private readonly IReadOnlyList<IonAlertInput> _inputs =
    [
        new() { Type = "checkbox", Label = "Cheese", Value = "cheese", Checked = true },
        new() { Type = "checkbox", Label = "Mushrooms", Value = "mushrooms" },
        new() { Type = "checkbox", Label = "Olives", Value = "olives" },
    ];

    private readonly IReadOnlyList<IonAlertButton> _buttons =
    [
        new() { Text = "Cancel", Role = "cancel" },
        new() { Text = "OK", Role = "confirm" },
    ];

    // For checkbox inputs Data is a List<string?> of the checked inputs' Values.
    private void OnDismissed(IonOverlayDismissEventArgs e)
    {
        if (e.Role == "confirm" && e.Data is List<string?> values)
            _toppings = values;
    }
}
```
<!-- /miko:example -->

## Text inputs

Any other `Type` (`"text"`, `"number"`, `"password"`, `"email"`, `"tel"`, …) renders one text field per input, with `Placeholder` and an initial `Value`. **The port does not read the typed text back**: `Data` is a list of the inputs' original `Value`s. `"textarea"` renders a single-line field. To ask for text, use an [`IonModal`](./modal.md) with an [`IonInput`](./input.md) for now.

## Using the controller

`IonAlertController.CreateAsync` takes an `IonAlertOptions` with the same header, message, buttons, inputs, and `BackdropDismiss` settings. `OnDidDismissAsync()` completes with the same role and data as `OnDidDismiss`.

<!-- miko:example examples/alert/Controller.razor -->
```razor
@inject IonAlertController AlertController

<IonButton Color="danger" OnClick="DeleteAsync">Delete account</IonButton>

@code {
    private async Task DeleteAsync()
    {
        var alert = await AlertController.CreateAsync(new IonAlertOptions
        {
            Header = "Delete account?",
            Message = "This cannot be undone.",
            BackdropDismiss = false, // only the buttons close it
            Buttons =
            [
                new() { Text = "Cancel", Role = "cancel" },
                new() { Text = "Delete", Role = "destructive" },
            ],
        });
        await alert.PresentAsync();

        var result = await alert.OnDidDismissAsync();
        if (result.Role == "destructive")
        {
            // delete the account…
        }
    }
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonAlert -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonAlert

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `BackdropDismiss` | `bool` | `true` | If true (default), tapping the backdrop dismisses the alert. |
| `Buttons` | `IReadOnlyList<IonAlertButton>` | `[]` | The buttons to show. Use [`IonAlertButton.FromText`](#ionalertbutton) to build them from bare strings (Ionic `buttons={['Action']}`). |
| `Header` | `string?` | `null` | The alert title. |
| `Inputs` | `IReadOnlyList<IonAlertInput>` | `[]` | Optional inputs (all the same kind: text, radio, or checkbox). |
| `IsOpen` | `bool` | `false` | Whether the alert is presented. Bind two-way with [`IsOpenChanged`](#ionalert). Two-way bindable: `@bind-IsOpen`. |
| `Message` | `string?` | `null` | The body message. |
| `OverlayId` | `string?` | `null` | Optional stable identifier used by the root overlay registry. |
| `SubHeader` | `string?` | `null` | The secondary title under the header. |
| `Translucent` | `bool` | `false` | If true, the surface is translucent (marker only in this port). |
| `Trigger` | `string?` | `null` | Id of the element that opens the alert (Ionic `trigger`). Kept for API parity; the host drives [`IsOpen`](#ionalert) in this port. |

| Event | Type | Description |
| --- | --- | --- |
| `IsOpenChanged` | `EventCallback<bool>` | Raised with the new open state when the alert opens/closes. |
| `OnDidDismiss` | `EventCallback<IonOverlayDismissEventArgs>` | Raised when the alert dismisses, with the tapped button's role (or `"backdrop"`). |
<!-- /miko:api -->

## Types

<!-- miko:type IonAlertButton IonAlertInput IonAlertOptions IonAlertController -->
### IonAlertButton

A single alert button. Mirrors Ionic's `AlertButton` interface. A string button (e.g. `['Action']`) is normalized to a button whose [`Text`](#ionalertbutton) is that string and whose [`Role`](#ionalertbutton) is `"cancel"` only when the text is "cancel" (case-insensitive) — see [`FromText`](#ionalertbutton). [`Role`](#ionalertbutton) is stamped as `alert-button-role-{role}`.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Text` | `string?` | `null` | The button label. |
| `Role` | `string?` | `null` | Optional role: `"cancel"`, `"destructive"`, or a custom string. |
| `CssClass` | `string?` | `null` | Extra CSS class(es) to add to the button. |
| `Handler` | `Action?` | `null` | Optional handler run when the button is tapped (cancel buttons included), before the alert dismisses. It cannot keep the alert open. |
| `IsCancel` | `bool` | `false` | True when [`Role`](#ionalertbutton) is `"cancel"` (case-insensitive). (read-only) |

| Method | Returns | Description |
| --- | --- | --- |
| `FromText(string text)` | `IonAlertButton` | Builds a button from a bare string, as Ionic's alert does for string buttons: the text becomes the label, and the role is `"cancel"` only when the text is "cancel" (case-insensitive). |

### IonAlertInput

A single alert input. Mirrors Ionic's `AlertInput` interface. Inputs of one alert must all be the same kind (all `text`/`textarea`-style, all `radio`, or all `checkbox`) — the alert renders the group matching the first input's [`Type`](#ionalertinput).

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Type` | `string` | `"text"` | Input type: `"radio"`, `"checkbox"`, or a text-field type such as `"text"` (default), `"number"`, `"password"`, `"email"`, `"tel"`. `"textarea"` renders a single-line field in this port. |
| `Name` | `string?` | `null` | Form field name (Ionic `name`). Not used by this port. |
| `Placeholder` | `string?` | `null` | Placeholder text for text-style inputs. |
| `Value` | `string?` | `null` | The input value, reported in the dismiss `Data`. For text fields it is the initial text only: text the user types is not read back in this port. |
| `Label` | `string?` | `null` | Label shown beside a radio/checkbox. |
| `Checked` | `bool` | `false` | Whether a radio/checkbox is checked. The alert updates it when a row is tapped. |
| `Disabled` | `bool` | `false` | When true, a radio/checkbox row ignores taps. A text field only gets the `alert-input-disabled` class. |
| `CssClass` | `string?` | `null` | Extra CSS class(es) (Ionic `cssClass`). Not applied by this port. |
| `IsRadio` | `bool` | `false` | True for a radio input. (read-only) |
| `IsCheckbox` | `bool` | `false` | True for checkbox inputs. (read-only) |
| `IsTextarea` | `bool` | `false` | True for a textarea input (rendered as a single-line field in this port). (read-only) |

### IonAlertOptions

Options for [`IonAlertController.CreateAsync`](#ionalertcontroller). Each property maps to the [`IonAlert`](#ionalert) parameter of the same name.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Header` | `string?` | `null` | The alert title. |
| `SubHeader` | `string?` | `null` | The secondary title under the header. |
| `Message` | `string?` | `null` | The body message. |
| `Buttons` | `IReadOnlyList<IonAlertButton>` | `[]` | The buttons to show. |
| `Inputs` | `IReadOnlyList<IonAlertInput>` | `[]` | Optional inputs, all of the same kind. |
| `BackdropDismiss` | `bool` | `true` | If true (default), tapping the backdrop dismisses the alert. |
| `CssClass` | `string?` | `null` | Extra CSS class(es) for the alert host (its `Class`). |

### IonAlertController

Creates alerts from code (Ionic's `alertController`). Inject it; `AddIonic()` registers it.

| Method | Returns | Description |
| --- | --- | --- |
| `CreateAsync(IonAlertOptions options)` | `Task<IonOverlayReference>` | Creates an alert from `options`. Call [`IonOverlayReference.PresentAsync`](./overlays.md#ionoverlayreference) on the result to show it. |
<!-- /miko:type -->

## Styling

### Theme tokens

<!-- miko:tokens Alert -->
Theme token `IonicTheme.Alert` (`AlertToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Alert = new AlertToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `BackdropColor` | `Color` | `#000000` | `#000000` |
| `BackdropOpacity` | `float` | `0.32` | `0.4` |
| `Background` | `Color` | `#ffffff` | `#f9f9f9` |
| `BorderRadius` | `float` | `4` | `13` |
| `BoxShadow` | `List<BoxShadow>` | `0 11px 15px -7px rgba(0, 0, 0, 0.2), 0 24px 38px 3px rgba(0, 0, 0, 0.14), 0 9px 46px 8px rgba(0, 0, 0, 0.12)` | `none` |
| `ButtonBorderRadius` | `float` | `2` | `0` |
| `ButtonColor` | `Color` | `#0054e9` | `#0054e9` |
| `ButtonFontSize` | `float` | `14` | `17` |
| `ButtonFontWeight` | `FontWeight` | `FontWeight.Medium` | `FontWeight.Normal` |
| `ButtonGroupFlexWrap` | `FlexWrap` | `FlexWrap.WrapReverse` | `FlexWrap.Wrap` |
| `ButtonGroupJustify` | `JustifyContent` | `JustifyContent.FlexEnd` | `JustifyContent.Center` |
| `ButtonGroupPadding` | `float` | `8` | `0` |
| `ButtonMarginX` | `float` | `8` | `0` |
| `ButtonPadding` | `float` | `10` | `8` |
| `ButtonTextTransform` | `TextTransform` | `TextTransform.Uppercase` | `TextTransform.None` |
| `ControlAccent` | `Color` | `#0054e9` | `#0054e9` |
| `ControlBorderColorOff` | `Color` | `#737373` | `#bfbfbf` |
| `HeadPaddingX` | `float` | `23` | `16` |
| `HeadPaddingY` | `float` | `20` | `12` |
| `HeadTextAlign` | `TextAlign` | `TextAlign.Left` | `TextAlign.Center` |
| `ListBorderColor` | `Color` | `#d9d9d9` | `rgba(0, 0, 0, 0.2)` |
| `MaxWidth` | `float` | `280` | `270` |
| `MessageColor` | `Color` | `#737373` | `#000000` |
| `MessageFontSize` | `float` | `16` | `13` |
| `MessagePaddingX` | `float` | `24` | `16` |
| `MessagePaddingY` | `float` | `20` | `12` |
| `MinWidth` | `float` | `250` | `250` |
| `SubTitleColor` | `Color` | `#000000` | `#666666` |
| `SubTitleFontSize` | `float` | `16` | `14` |
| `TappableHeight` | `float` | `48` | `44` |
| `TitleColor` | `Color` | `#000000` | `#000000` |
| `TitleFontSize` | `float` | `20` | `17` |
| `TitleFontWeight` | `FontWeight` | `FontWeight.Medium` | `FontWeight.SemiBold` |
<!-- /miko:tokens -->

`IonicTheme.Alert.BackdropColor` also colors the backdrop of [`IonModal`](./modal.md) and [`IonPopover`](./popover.md).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-alert` | The full-screen host that centers the alert. Also carries the mode (`md` / `ios`); `.overlay-hidden` while closed. |
| `.alert-backdrop` | The dim layer behind the alert (also `.ion-backdrop`). |
| `.alert-wrapper` | The alert card. |
| `.alert-head`, `.alert-title`, `.alert-sub-title` | The header block, `Header`, and `SubHeader`. |
| `.alert-message` | `Message`. |
| `.alert-button-group` | The button row; `.alert-button-group-vertical` with three or more buttons. |
| `.alert-button`, `.alert-button-inner` | One button and its label. |
| `.alert-button-role-{role}` | A button's `Role`, e.g. `.alert-button-role-cancel`. |
| `.alert-radio-group`, `.alert-checkbox-group`, `.alert-input-group` | The inputs group, by input kind. |
| `.alert-radio-button`, `.alert-checkbox-button` | One radio / checkbox row (also `.alert-tappable`); `-checked` / `-disabled` variants, e.g. `.alert-radio-button-checked`. |
| `.alert-radio-icon`, `.alert-radio-label`, `.alert-checkbox-icon`, `.alert-checkbox-label` | The parts of a radio / checkbox row. |
| `.alert-input` | A text field; `.alert-input-disabled` when `Disabled`. |
| `.alert-translucent` | `Translucent="true"`. Only a marker; no translucent style is applied. |

A button's `CssClass` and the alert's `Class` (or `IonAlertOptions.CssClass`) add your own classes:

```csharp
// GlobalStyles.cs — new IonAlertButton { Text = "Delete", CssClass = "alert-danger" }
[".ion-alert .alert-button.alert-danger"] = new()
{
    Color = Color.FromHex("#c5000f"),
},
```
