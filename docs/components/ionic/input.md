# Input

Inputs are single-line text fields with an optional label, placeholder, helper and error text, clear button, and character counter. `IonInput` is usually placed in an [`IonItem`](./item.md) inside an [`IonList`](./list.md), or used on its own with a `Fill` style.

For multi-line text use [Textarea](./textarea.md), for search fields [Searchbar](./searchbar.md), and for one-time codes [Input OTP](./input-otp.md).

## Basic usage

`Label` sets the visible label. `Placeholder` is drawn while the field is empty and not focused — it disappears as soon as the field gets focus. A `Readonly` field can be focused but not edited, and shows no on-screen keyboard; a `Disabled` field cannot be focused and is dimmed.

<!-- miko:example examples/input/Basic.razor -->
```razor
<IonList>
    <IonItem>
        <IonInput Label="Default input" />
    </IonItem>
    <IonItem>
        <IonInput Label="With placeholder" Placeholder="Enter company name" />
    </IonItem>
    <IonItem>
        <IonInput Label="With value" Value="121 S Pinckney St #300" />
    </IonItem>
    <IonItem>
        <IonInput Label="Readonly" Value="Madison" Readonly="true" />
    </IonItem>
    <IonItem>
        <IonInput Label="Disabled" Value="53703" Disabled="true" />
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Binding the value

`@bind-Value` keeps a field in sync with the text. `ValueChanged` is raised with the whole new value on every edit (each typed or deleted character, and the clear button), and `OnInput` is raised right after it with the same value. An unbound input keeps whatever the user typed when its parent re-renders.

`OnChange` (Ionic `ionChange`) is raised when the edit is committed: when the input loses focus, or Enter is pressed, with a value different from the one it had when it was focused. It is not raised while typing — use `ValueChanged` / `OnInput` for that.

<!-- miko:example examples/input/Binding.razor -->
```razor
<IonInput Label="Name" Placeholder="Your name" @bind-Value="_name" />
<p>Hello, @(_name ?? "stranger")!</p>

@code {
    // Updated on every keystroke (ValueChanged carries the whole new text).
    private string? _name;
}
```
<!-- /miko:example -->

## Label placement

`LabelPlacement` positions the label: `start` (default, before the field), `end` (after it), `fixed` (before it, 100px wide), or `stacked` (above it). Inline labels do not wrap and are capped at 200px. `floating` is accepted but is laid out exactly like `stacked` — the label always sits above the field; Ionic's float-into-the-field animation is not implemented.

<!-- miko:example examples/input/LabelPlacement.razor -->
```razor
<IonList>
    <IonItem>
        <IonInput Label="Start (default)" Placeholder="Enter text" />
    </IonItem>
    <IonItem>
        <IonInput Label="Fixed" LabelPlacement="fixed" Placeholder="Enter text" />
    </IonItem>
    <IonItem>
        <IonInput Label="Stacked" LabelPlacement="stacked" Placeholder="Enter text" />
    </IonItem>
    <IonItem>
        <IonInput Label="End" LabelPlacement="end" Placeholder="Enter text" />
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Fill and shape

`Fill="solid"` gives the field a tinted background and a bottom border; `Fill="outline"` draws a rounded 1px border (without Ionic's label notch). `Shape="round"` raises the corner radius to 16px. Both fills apply in md and ios mode (Ionic limits them to md).

In md mode, a field that is not inside an `IonItem` and not `Fill="outline"` also shows a 2px highlight bar along its bottom edge, tinted by `Color`. As in Ionic, the bar grows in while the field has focus and is hidden otherwise; it is the only thing `Color` changes.

<!-- miko:example examples/input/Fill.razor -->
```razor
@* Filled inputs are used on their own, not inside an IonItem. *@
<IonInput Label="Solid" LabelPlacement="stacked" Fill="solid" Placeholder="Enter text" />
<IonInput Label="Outline" LabelPlacement="stacked" Fill="outline" Placeholder="Enter text" />
<IonInput Label="Round outline" LabelPlacement="stacked" Fill="outline" Shape="round" Placeholder="Enter text" />
```
<!-- /miko:example -->

## Helper and error text

`HelperText` and `ErrorText` render in a row under the field. Miko does not track validity (there are no `ion-invalid` / `ion-touched` states), so both texts are shown whenever they are set — decide in your code which one to show.

<!-- miko:example examples/input/Validation.razor -->
```razor
@* Both texts render when both are set, so show one or the other. *@
<IonInput Label="Email"
          LabelPlacement="stacked"
          Placeholder="email@domain.com"
          Value="@_email"
          OnInput="Validate"
          HelperText="@(_error is null ? "Enter a valid email" : null)"
          ErrorText="@_error" />

@code {
    private string? _email;
    private string? _error;

    private void Validate(string? value)
    {
        _email = value;
        _error = string.IsNullOrEmpty(value) || value.Contains('@') ? null : "Invalid email";
    }
}
```
<!-- /miko:example -->

## Clear button

`ClearInput` adds a clear button while the field has a value and is neither readonly nor disabled. Tapping it sets the value to the empty string and raises `ValueChanged` and `OnInput`. The button follows the `Value` parameter, so bind `Value`; an unbound field loses the button whenever its parent re-renders. `ClearInputIcon` replaces the default icon (close-circle on ios, close-sharp on md).

<!-- miko:example examples/input/ClearInput.razor -->
```razor
@* Bind Value: the button only appears while the Value parameter is non-empty. *@
<IonInput Label="Search term" ClearInput="true" @bind-Value="_term" />

@code {
    private string? _term = "Clear me";
}
```
<!-- /miko:example -->

## Character counter

`Counter` shows `length / Maxlength` at the end of the bottom row and requires `Maxlength`. `Maxlength` limits what the user can enter: typed or pasted text beyond it is cut off. A `Value` set from code is not shortened.

<!-- miko:example examples/input/Counter.razor -->
```razor
<IonInput Label="Nickname"
          LabelPlacement="stacked"
          Counter="true"
          Maxlength="20"
          HelperText="Shown on your profile"
          @bind-Value="_nickname" />

@code {
    private string? _nickname;
}
```
<!-- /miko:example -->

## Input types

`Type` picks the native field: `password` masks the text, `number` and `tel` request the platform's numeric keypad (the value is still an unfiltered string), and every other type (`text`, `search`, `email`, `url`, …) is a plain text field.

<!-- miko:example examples/input/Types.razor -->
```razor
<IonList>
    <IonItem>
        <IonInput Label="Text" Placeholder="Enter text" />
    </IonItem>
    <IonItem>
        <IonInput Label="Password" Type="password" Value="secret" />
    </IonItem>
    <IonItem>
        <IonInput Label="Number" Type="number" Placeholder="000" />
    </IonItem>
    <IonItem>
        <IonInput Label="Telephone" Type="tel" Placeholder="888-888-8888" />
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Start and end slots

`StartSlot` and `EndSlot` place content inside the field, before and after the text: icons, units, or a button such as a show-password toggle.

<!-- miko:example examples/input/Slots.razor -->
```razor
<IonInput Label="Password"
          LabelPlacement="stacked"
          Type="@(_reveal ? "text" : "password")"
          @bind-Value="_password">
    <StartSlot>
        <IonIcon Icon="@Ionicons.LockClosedOutline" />
    </StartSlot>
    <EndSlot>
        <IonButton Fill="clear" OnClick="() => _reveal = !_reveal">
            <IconOnly>
                <IonIcon Icon="@(_reveal ? Ionicons.EyeOffOutline : Ionicons.EyeOutline)" />
            </IconOnly>
        </IonButton>
    </EndSlot>
</IonInput>

@code {
    private string? _password;
    private bool _reveal;
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonInput -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonInput

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ClearInput` | `bool` | `false` | If true, a clear icon appears while there is a value; clicking it clears the input. |
| `ClearInputIcon` | `string?` | `null` | The icon to use for the clear button. Defaults to close-circle (ios) / close-sharp (md). |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class that tints the md highlight bar (its only visible effect). |
| `Counter` | `bool` | `false` | If true, a character counter is shown below the field. Requires [`Maxlength`](#ioninput). |
| `Disabled` | `bool` | `false` | If true, the user cannot interact with the input. |
| `ErrorText` | `string?` | `null` | Error text displayed below the input whenever it is set (Miko does not track validity). |
| `Fill` | `string?` | `null` | The fill style: `"solid"` (filled) or `"outline"` (bordered). |
| `HelperText` | `string?` | `null` | Helper text displayed below the input. Shown even when [`ErrorText`](#ioninput) is also set. |
| `Label` | `string?` | `null` | The visible plaintext label associated with the input. |
| `LabelPlacement` | `string` | `"start"` | Where the label sits relative to the input: `"start"` (default), `"end"`, `"fixed"`, `"stacked"`, or `"floating"` (laid out like `"stacked"`; no float animation). |
| `Maxlength` | `int?` | `null` | The maximum number of characters the user can enter (Ionic `maxlength`), also shown by the counter. Typed or pasted text beyond it is cut off; a [`Value`](#ioninput) set from code is not shortened. |
| `Placeholder` | `string?` | `null` | Instructional text shown before the input has a value. |
| `Readonly` | `bool` | `false` | If true, the user cannot modify the value. The field can still be focused (no on-screen keyboard is shown). |
| `Shape` | `string?` | `null` | The shape. Use `"round"` for an increased corner radius. |
| `Type` | `string` | `"text"` | The native control type: `"password"` masks the text, `"number"` / `"tel"` request the numeric keypad (the value stays an unfiltered string), anything else (`"text"`, `"search"`, `"email"`, `"url"`, …) is a plain text field. |
| `Value` | `string?` | `null` | The value of the input. Two-way bindable: `@bind-Value`. |

| Event | Type | Description |
| --- | --- | --- |
| `OnChange` | `EventCallback<string?>` | Ionic `ionChange`: raised when the edit is committed — when the input loses focus or Enter is pressed, with a value different from the one it had on focus. Not raised while typing (see [`OnInput`](#ioninput)) or for programmatic value changes. |
| `OnInput` | `EventCallback<string?>` | Raised each time the user modifies the value (Ionic `ionInput`). |
| `ValueChanged` | `EventCallback<string?>` | Raised (with the new value) as the user types (two-way binding support). |

| Slot | Type | Description |
| --- | --- | --- |
| `EndSlot` | `RenderFragment?` | Content shown at the trailing edge of the input. |
| `StartSlot` | `RenderFragment?` | Content shown at the leading edge of the input. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Input -->
Theme token `IonicTheme.Input` (`InputToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Input = new InputToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `transparent` | `transparent` |
| `BorderColor` | `Color` | `rgba(0, 0, 0, 0.07)` | `rgba(0, 0, 0, 0.2)` |
| `BorderRadius` | `float` | `4` | `4` |
| `ClearIconColor` | `Color` | `#666666` | `#666666` |
| `DisabledOpacity` | `float` | `0.38` | `0.3` |
| `ErrorColor` | `Color` | `#c5000f` | `#c5000f` |
| `FontSize` | `float` | `16` | `17` |
| `HelperColor` | `Color` | `#666666` | `#666666` |
| `HighlightColor` | `Color` | `#0054e9` | `#0054e9` |
| `HighlightHeight` | `float` | `2` | `0` |
| `LabelColor` | `Color` | `#666666` | `#666666` |
| `MinHeight` | `float` | `44` | `44` |
| `PaddingEnd` | `float` | `0` | `0` |
| `PaddingStart` | `float` | `0` | `0` |
| `TextColor` | `Color` | `#000000` | `#000000` |
<!-- /miko:tokens -->

[`IonTextarea`](./textarea.md) uses the same tokens, and [`IonInputOtp`](./input-otp.md) reads `HelperColor` (and `BorderColor` in ios mode). `PlaceholderColor` currently has no effect: placeholders are always painted gray.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-input` | The host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-input .input-wrapper` | The row holding the label and the field; carries the fill background, border, and radius. |
| `.ion-input .label-text-wrapper`, `.ion-input .label-text` | The label (the wrapper also gets `.label-text-wrapper-hidden` when there is no label). |
| `.ion-input .native-wrapper` | Wraps the start slot, the native field, the clear button, and the end slot. |
| `.ion-input .native-input` | The native text field. |
| `.ion-input .input-clear-icon` | The clear button. |
| `.ion-input .input-highlight` | The md highlight bar. |
| `.ion-input .input-bottom` | The row under the field, holding `.helper-text`, `.error-text`, and `.counter`. |
| `.input-label-placement-start`, `-end`, `-fixed`, `-stacked`, `-floating` | `LabelPlacement`. |
| `.input-fill-solid`, `.input-fill-outline`, `.input-shape-round` | `Fill`, `Shape`. |
| `.has-value`, `.input-readonly`, `.input-disabled` | Non-empty `Value`, `Readonly`, `Disabled`. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |

```csharp
// GlobalStyles.cs — a brand outline field: <IonInput Class="brand" Fill="outline" … />
[".ion-input.brand.input-fill-outline .input-wrapper"] = new()
{
    BorderColor = Color.FromHex("#0e7490"),
    BorderRadius = new BorderRadius(12),
},
```
