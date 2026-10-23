# Input OTP

Input OTP is a row of single-character boxes for entering one-time codes such as SMS or authenticator codes. `IonInputOtp` moves focus from box to box as the user types and reports the whole code as one string.

## Basic usage

By default there are four boxes that accept digits. Content between the tags is a description shown centered under the boxes (hidden when empty).

Typing a valid character into an empty box fills it and moves to the next one. Typed into a filled box, the character is inserted there and the following characters shift right when the last box is free; when every box is filled it replaces the box's character. An invalid character is ignored. Backspace deletes the focused box's character and shifts the following ones left, or moves back when the box is empty. Left moves to the previous box and Right to the next one (only from a filled box). Tapping an empty box focuses the first empty one; a filled box can be tapped directly. Text that arrives in one input event (paste, autofill, a composed IME string) is distributed across the boxes from the first one.

To change a character, delete it with Backspace first: typing into a box that already holds one currently treats the box's old and new character as a two-character paste, so the code is replaced by those two characters.

<!-- miko:example examples/input-otp/Basic.razor -->
```razor
@* Child content is the description line shown under the boxes. *@
<IonInputOtp>
    Didn't get a code? Resend it
</IonInputOtp>
```
<!-- /miko:example -->

## Binding and completion

Bind `Value` with `@bind-Value`. After every change `ValueChanged` and then `OnInput` are raised with the whole code, and `OnComplete` follows whenever the change leaves every box filled. `OnChange` is raised when focus leaves the component and the code differs from when it was focused; `OnFocus` and `OnBlur` fire once when focus enters or leaves the group, not per box. Setting `Value` to an empty string clears the boxes.

An unbound `IonInputOtp` keeps the entered characters when its parent re-renders, as an unbound `IonInput` does; bind `Value` to read or set the code.

<!-- miko:example examples/input-otp/Complete.razor -->
```razor
<IonInputOtp Length="6" Disabled="@_verifying" @bind-Value="_code" OnComplete="VerifyAsync">
    @_status
</IonInputOtp>

@code {
    private string? _code;
    private string _status = "Enter the 6-digit code we sent you";
    private bool _verifying;

    // Raised once every box holds a valid character.
    private async Task VerifyAsync(string code)
    {
        _verifying = true;
        _status = "Checking…";
        await Task.Delay(500); // call your API here
        _verifying = false;
        _status = code == "123456" ? "Verified" : "Wrong code, try again";
        if (_status != "Verified") _code = string.Empty;
    }
}
```
<!-- /miko:example -->

## Length and separators

`Length` sets the number of boxes. `Separators` draws a dot after the box at each listed 1-based position: a single position (`Separators="3"`), a comma-separated string (`"2,4"`), `"all"`, or an `int` collection. The parameter is typed `object`, so Razor treats the attribute as C#: a bare number is an `int`, and a string needs quotes — `Separators="@("2,4")"`.

<!-- miko:example examples/input-otp/Separators.razor -->
```razor
@* Separators is typed object, so the attribute is C#: a number, a quoted string, or an int array. *@
<IonInputOtp Length="6" Separators="3" />
<IonInputOtp Length="6" Separators="@(new[] { 2, 4 })" />
<IonInputOtp Length="4" Separators="@("all")" />
```
<!-- /miko:example -->

## Type and pattern

`Type="number"` (default) accepts digits, `Type="text"` letters and digits. `Pattern` replaces that rule with a regular expression that each single character must match; characters that do not match are dropped as they are typed. `Autocapitalize` and `Inputmode` are kept for API parity — the on-screen keyboard does not read them, so a case-sensitive `Pattern` simply rejects the other case.

<!-- miko:example examples/input-otp/Pattern.razor -->
```razor
@* Letters and digits. *@
<IonInputOtp Type="text" Length="6" />

@* Only uppercase letters and digits; anything else is rejected as it is typed. *@
<IonInputOtp Type="text" Pattern="[A-Z0-9]" Length="6" />
```
<!-- /miko:example -->

## Fill, shape, and size

`Fill` is `outline` (default) or `solid`. `Shape` sets the box corners: `round` (default, 16px), `soft` (8px), or `rectangular`. `Size` sets the box size: `small` (40px), `medium` (default, 48px), or `large` (56px).

<!-- miko:example examples/input-otp/Appearance.razor -->
```razor
<IonInputOtp Fill="solid" />
<IonInputOtp Shape="soft" />
<IonInputOtp Shape="rectangular" />
<IonInputOtp Size="small" />
<IonInputOtp Size="large" />
```
<!-- /miko:example -->

## Colors

`Color` takes a palette name. It tints the caret and the focused box's border; with the outline fill, the other boxes get a translucent border in that color. See [Theming](./theming.md#palette).

<!-- miko:example examples/input-otp/Colors.razor -->
```razor
<IonInputOtp Color="secondary" />
<IonInputOtp Color="success" Fill="solid" />
<IonInputOtp Color="danger" Value="12" />
```
<!-- /miko:example -->

## Readonly and disabled

`Readonly` boxes can be focused but reject edits; `Disabled` boxes are dimmed and cannot be focused.

<!-- miko:example examples/input-otp/States.razor -->
```razor
<IonInputOtp Value="1234" Readonly="true" />
<IonInputOtp Value="12" Disabled="true" />
```
<!-- /miko:example -->

## API

<!-- miko:api IonInputOtp -->
Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions](./index.md#conventions).

### IonInputOtp

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Tints the box borders (fully on the focused box) and the caret. |
| `Disabled` | `bool` | `false` | When true, every box is dimmed and rejects focus and input. |
| `Fill` | `string` | `"outline"` | Box fill (Ionic `fill`): `"outline"` (default) or `"solid"`. |
| `Inputmode` | `string?` | `null` | Which on-screen keyboard the boxes ask for (Ionic `inputmode`): `"numeric"`, `"decimal"` or `"tel"` request the number pad, any other value the standard keyboard. Null follows [`Type`](#ioninputotp): the number pad for `"number"`, the standard keyboard for `"text"`. |
| `Length` | `int` | `4` | Number of boxes, i.e. the code length (minimum 1). |
| `Pattern` | `string?` | `null` | Regular expression a single character must match to be accepted. Defaults to digits when [`Type`](#ioninputotp) is `"number"`, letters and digits when it is `"text"`. |
| `Readonly` | `bool` | `false` | When true, the boxes can be focused but their value cannot be changed. |
| `Separators` | `object?` | `null` | Separator positions (1-based; a separator follows that box). Accepts `"all"`, a comma-separated string such as `"1,3"`, a single `int`, or an `int` collection. The parameter is `object`, so Razor reads the attribute as C#: `Separators="3"` is the int 3, and a string needs quotes (`Separators="@("2,4")"`). |
| `Shape` | `string` | `"round"` | Box corners (Ionic `shape`): `"round"` (default), `"soft"`, or `"rectangular"`. |
| `Size` | `string` | `"medium"` | Box size (Ionic `size`): `"small"`, `"medium"` (default), or `"large"`. |
| `Type` | `string` | `"number"` | Accepted characters when [`Pattern`](#ioninputotp) is not set: `"number"` (default, digits only) or `"text"` (letters and digits). |
| `Value` | `string?` | `null` | The entered code. Two-way bindable with `@bind-Value`. Unbound, the boxes keep what was typed across parent re-renders. |

| Event | Type | Description |
| --- | --- | --- |
| `OnBlur` | `EventCallback` | Raised when focus leaves the component. |
| `OnChange` | `EventCallback<string?>` | Raised with the whole code when focus leaves the component after it changed (Ionic `ionChange`). |
| `OnComplete` | `EventCallback<string>` | Raised with the code once every box is filled (Ionic `ionComplete`). |
| `OnFocus` | `EventCallback` | Raised when focus enters the component (not again as it moves between boxes). |
| `OnInput` | `EventCallback<string?>` | Raised with the whole code after every keystroke (Ionic `ionInput`). |
| `ValueChanged` | `EventCallback<string?>` | Raised with the new code whenever a box changes (the `@bind-Value` callback). |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

`IonInputOtp` has no token of its own. It reads `HelperColor` (the description) and, in ios mode, `BorderColor` from `IonicTheme.Input` (see [Input](./input.md#theme-tokens)), and the palette's primary color for the focus border and caret.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-input-otp` | The host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-input-otp .input-otp-group` | The row of boxes. |
| `.ion-input-otp .native-wrapper` | The wrapper around one box. |
| `.ion-input-otp .native-input` | One box (a native text field); `.native-input:focus` is the focused box. |
| `.ion-input-otp .input-otp-separator` | A separator dot. |
| `.ion-input-otp .input-otp-description` | The description (child content); `.input-otp-description-hidden` when empty. |
| `.input-otp-fill-outline`, `.input-otp-fill-solid` | `Fill`. |
| `.input-otp-shape-round`, `-soft`, `-rectangular` | `Shape`. |
| `.input-otp-size-small`, `-medium`, `-large` | `Size`. |
| `.input-otp-disabled`, `.input-otp-readonly` | `Disabled`, `Readonly`. |
| `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |

```csharp
// GlobalStyles.cs — bigger digits in the boxes: <IonInputOtp Class="pin" … />
[".ion-input-otp.pin .native-input"] = new()
{
    FontSize = Length.Px(22),
    FontWeight = FontWeight.SemiBold,
},
```
