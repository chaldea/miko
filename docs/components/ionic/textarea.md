# Textarea

Textareas are multi-line text fields with an optional label, placeholder, helper and error text, and character counter. `IonTextarea` is the multi-line sibling of [`IonInput`](./input.md) and shares its label placements, fills, and theme tokens; it has no clear button and no `Type`.

## Basic usage

`Label` sets the visible label. Enter inserts a line break. `Placeholder` is drawn while the field is empty and not focused. A `Readonly` field can be focused but not edited, and shows no on-screen keyboard; a `Disabled` field cannot be focused and is dimmed.

<!-- miko:example examples/textarea/Basic.razor -->
```razor
<IonList>
    <IonItem>
        <IonTextarea Label="Comments" Placeholder="Type something here" />
    </IonItem>
    <IonItem>
        <IonTextarea Label="With value" Value="A multi-line value. Enter inserts a line break." />
    </IonItem>
    <IonItem>
        <IonTextarea Label="Readonly" Value="Cannot be edited" Readonly="true" />
    </IonItem>
    <IonItem>
        <IonTextarea Label="Disabled" Value="Cannot be focused" Disabled="true" />
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Binding the value

`@bind-Value` keeps a field in sync with the text. `ValueChanged` is raised with the whole new value on every edit, and `OnInput` right after it. `OnChange` (Ionic `ionChange`) is raised when the textarea loses focus with a value different from the one it had when it was focused; Enter inserts a line break rather than committing.

<!-- miko:example examples/textarea/Binding.razor -->
```razor
<IonTextarea Label="Notes" LabelPlacement="stacked" @bind-Value="_notes" />
<p>@(_notes?.Split('\n').Length ?? 0) line(s)</p>

@code {
    private string? _notes;
}
```
<!-- /miko:example -->

## Rows

`Rows` (default 2) sets the height of the field in lines of text. `AutoGrow` only stamps the `textarea-auto-grow` class — the field does not grow with its content.

<!-- miko:example examples/textarea/Rows.razor -->
```razor
<IonTextarea Label="Message" LabelPlacement="stacked" Rows="5" Placeholder="Write your message" />
```
<!-- /miko:example -->

## Label placement

`LabelPlacement` works as on [`IonInput`](./input.md#label-placement): `start` (default), `end`, `fixed` (100px wide), or `stacked`; `floating` is laid out like `stacked`. For a label with markup, use the `LabelContent` slot — it is rendered only when `Label` is not set.

<!-- miko:example examples/textarea/LabelPlacement.razor -->
```razor
<IonList>
    <IonItem>
        <IonTextarea Label="Start (default)" Placeholder="Enter text" />
    </IonItem>
    <IonItem>
        <IonTextarea Label="Fixed" LabelPlacement="fixed" Placeholder="Enter text" />
    </IonItem>
    <IonItem>
        <IonTextarea Label="Stacked" LabelPlacement="stacked" Placeholder="Enter text" />
    </IonItem>
    <IonItem>
        @* LabelContent is used only when Label is not set. *@
        <IonTextarea LabelPlacement="stacked" Placeholder="Enter text">
            <LabelContent>
                Description <IonText Color="danger">(required)</IonText>
            </LabelContent>
        </IonTextarea>
    </IonItem>
</IonList>
```
<!-- /miko:example -->

## Fill and shape

`Fill="solid"` gives the field a tinted background and a bottom border, `Fill="outline"` a rounded 1px border, and `Shape="round"` a 16px corner radius — in both modes. In md mode a field outside an `IonItem` and not `Fill="outline"` also shows a 2px highlight bar along its bottom edge, tinted by `Color` and always visible.

<!-- miko:example examples/textarea/Fill.razor -->
```razor
@* Filled textareas are used on their own, not inside an IonItem. *@
<IonTextarea Label="Solid" LabelPlacement="stacked" Fill="solid" Placeholder="Enter text" />
<IonTextarea Label="Outline" LabelPlacement="stacked" Fill="outline" Placeholder="Enter text" />
<IonTextarea Label="Round outline" LabelPlacement="stacked" Fill="outline" Shape="round" Placeholder="Enter text" />
```
<!-- /miko:example -->

## Helper text, error text, and counter

`HelperText` and `ErrorText` render under the field whenever they are set (there is no validity tracking), so choose which one to show in your code. `Counter` shows `length / Maxlength` and requires `Maxlength`, which also limits what the user can enter.

<!-- miko:example examples/textarea/Hints.razor -->
```razor
<IonTextarea Label="Bio"
             LabelPlacement="stacked"
             Counter="true"
             Maxlength="160"
             HelperText="@(TooLong ? null : "A short introduction")"
             ErrorText="@(TooLong ? "Keep it under 160 characters" : null)"
             @bind-Value="_bio" />

@code {
    private string? _bio;

    // Maxlength stops typing at 160; a value set from code can still be longer.
    private bool TooLong => _bio?.Length > 160;
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonTextarea -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonTextarea

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `AutoGrow` | `bool` | `false` | If true, the textarea container grows and shrinks with its contents (Ionic `autoGrow`). Marker only: stamped as a host class; Miko has no auto-resize pseudo-element so no live growth happens here. |
| `Color` | `string?` | `null` | Named palette color (Ionic `color`): `"primary"`, `"danger"`, …. Stamped as an `ion-color-*` class that tints the md highlight bar (its only visible effect). |
| `Counter` | `bool` | `false` | If true, a character counter is shown below the field. Requires [`Maxlength`](#iontextarea). |
| `Disabled` | `bool` | `false` | If true, the user cannot interact with the textarea. |
| `ErrorText` | `string?` | `null` | Error text displayed below the textarea whenever it is set (Miko does not track validity). |
| `Fill` | `string?` | `null` | The fill style: `"solid"` (filled) or `"outline"` (bordered). |
| `HelperText` | `string?` | `null` | Helper text displayed below the textarea. Shown even when [`ErrorText`](#iontextarea) is also set. |
| `Label` | `string?` | `null` | The visible plaintext label associated with the textarea. |
| `LabelPlacement` | `string` | `"start"` | Where the label sits relative to the textarea: `"start"` (default), `"end"`, `"fixed"`, `"stacked"`, or `"floating"` (laid out like `"stacked"`; no float animation). |
| `Maxlength` | `int?` | `null` | The maximum number of characters the user can enter (Ionic `maxlength`), also shown by the counter. Typed or pasted text beyond it is cut off; a [`Value`](#iontextarea) set from code is not shortened. |
| `Name` | `string?` | `null` | Form field name marker. |
| `Placeholder` | `string?` | `null` | Instructional text shown before the textarea has a value. |
| `Readonly` | `bool` | `false` | If true, the user cannot modify the value. The field can still be focused (no on-screen keyboard is shown). |
| `Rows` | `int` | `2` | The number of visible text lines for the control (native `rows`). Defaults to 2. |
| `Shape` | `string?` | `null` | The shape. Use `"round"` for an increased corner radius. |
| `Value` | `string?` | `null` | The value of the textarea. Two-way bindable: `@bind-Value`. |

| Event | Type | Description |
| --- | --- | --- |
| `OnChange` | `EventCallback<string?>` | Ionic `ionChange`: raised when the textarea loses focus with a value different from the one it had on focus. Enter inserts a line break and does not commit. Not raised while typing (see [`OnInput`](#iontextarea)) or for programmatic value changes. |
| `OnInput` | `EventCallback<string?>` | Raised each time the user modifies the value (Ionic `ionInput`). |
| `ValueChanged` | `EventCallback<string?>` | Raised (with the new value) as the user types (two-way binding support). |

| Slot | Type | Description |
| --- | --- | --- |
| `EndSlot` | `RenderFragment?` | Content shown at the trailing edge of the textarea. |
| `LabelContent` | `RenderFragment?` | Custom label content (Ionic `slot="label"`), used when [`Label`](#iontextarea) is not set. Mirrors textarea.tsx renderLabel() where the `label` prop takes priority over the slot. |
| `StartSlot` | `RenderFragment?` | Content shown at the leading edge of the textarea. |
<!-- /miko:api -->

## Styling

### Theme tokens

`IonTextarea` has no token of its own: it reads `IonicTheme.Input` — see [Input](./input.md#theme-tokens).

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-textarea` | The host. Also carries the mode (`md` / `ios`) and the state classes below. |
| `.ion-textarea .textarea-wrapper` | The row holding the label and the field; carries the fill background, border, and radius. |
| `.ion-textarea .label-text-wrapper`, `.ion-textarea .label-text` | The label (the wrapper also gets `.label-text-wrapper-hidden` when there is no label). |
| `.ion-textarea .textarea-wrapper-inner` | Wraps the start slot, the field, and the end slot. |
| `.ion-textarea .native-textarea` | The native multi-line field (inside `.native-wrapper`). |
| `.ion-textarea .textarea-highlight` | The md highlight bar. |
| `.ion-textarea .textarea-bottom` | The row under the field, holding `.helper-text`, `.error-text`, and `.counter`. |
| `.textarea-label-placement-start`, `-end`, `-fixed`, `-stacked`, `-floating` | `LabelPlacement`. |
| `.textarea-fill-solid`, `.textarea-fill-outline`, `.textarea-shape-round` | `Fill`, `Shape`. |
| `.has-value`, `.textarea-readonly`, `.textarea-disabled`, `.textarea-auto-grow` | Non-empty `Value`, `Readonly`, `Disabled`, `AutoGrow`. |
| `.ion-color`, `.ion-color-{name}` | `Color` (e.g. `.ion-color-danger`). |

```csharp
// GlobalStyles.cs — a monospace editor: <IonTextarea Class="code" … />
[".ion-textarea.code .native-textarea"] = new()
{
    FontFamily = "monospace",
    FontSize = Length.Px(14),
},
```
