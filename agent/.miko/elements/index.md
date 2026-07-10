# DOM Elements Reference

Every built-in Miko element. In `.razor` you write the tag (`<div>`, `<h1>`, `<input>`); the compiler builds the matching `*Element`. All 36 concrete elements derive directly from `Element` — there is no intermediate hierarchy.

> **Only these elements exist.** See [Missing elements](#missing-elements-use-substitutes) at the bottom for what to use instead of `<textarea>`, `<form>`, `<section>`, etc.

## Common members (every element)

Set these on any element (in Razor via attributes, in code via properties):

| Member | Type | Notes |
| --- | --- | --- |
| `Id` | `string?` | Element id; `id="..."` in Razor. Matched by `IdSelector`. |
| `Class` | `string?` | Space-separated classes; `class="..."`. Matched by `ClassSelector`. |
| `Style` | `Style?` | Inline style object (highest cascade precedence). |
| `TextContent` | `string?` | Text of the element (facade over child text nodes). |
| `Children` | `List<Element>` | Direct children. |
| `Parent` | `Element?` | Set via `AddChild`/`RemoveChild`. |
| `State` | `ElementState` | `[Flags]`: `None`, `Hover`, `Active`, `Focus`, `Disabled`. |
| `IsDisabled` | `bool` (get) | True if this or any ancestor is `Disabled`. |

Event handler properties (all `MikoEventHandler<T>?`): `OnClick`, `OnMouseEnter/Leave`, `OnMouseDown/Up`, `OnFocus`, `OnBlur`, `OnChange`, `OnScroll`, `OnKeyDown`, `OnInput`. See [events.md](../events.md).

Methods: `AddChild`, `RemoveChild`, `FindById`, `FindByClass`, `FindByTagName`, `HasClass`, `AddEventListener<T>`, `RemoveEventListener<T>`, `SetState`, `ClearState`, `HasState`.

---

## Text & headings

| Tag | Class | Extra properties | Notes |
| --- | --- | --- | --- |
| `<h1>`…`<h6>` | `H1Element`…`H6Element` | — | Six classes, one per level. Text via `TextContent`/children. |
| `<p>` | `ParagraphElement` | — | Paragraph. |
| `<span>` | `SpanElement` | — | Inline container. |
| `<strong>` | `StrongElement` | — | Inline bold semantic. |

There is **no** `<em>`, `<b>`, `<i>`, `<br>` — style a `<span>` (e.g. `FontWeight`, `FontStyle`) instead.

## Containers & semantic

| Tag | Class | Notes |
| --- | --- | --- |
| `<div>` | `DivElement` | Block container (the workhorse). |
| `<nav>` | `NavElement` | Semantic navigation container. |

`<fragment>` (`FragmentElement`) and `#text` (`TextNode`) are engine-internal (multi-root wrapping, text nodes) — you don't author them.

## Interactive & form

| Tag | Class | Properties (type) | Notes |
| --- | --- | --- | --- |
| `<a>` | `AnchorElement` | `Href` (`string?`), `Target` (`string?`), `Rel` (`string?`) | Link. |
| `<button>` | `ButtonElement` | — | Label via `TextContent`/children; use `OnClick`. |
| `<input>` | `InputElement` | `Type` (`InputType`), `Value` (`string?`), `Placeholder` (`string?`), `Checked` (`bool`), `Min`/`Max`/`NumericValue` (`float`), `CursorPosition` (`int`) | Self-contained control. |
| `<select>` | `SelectElement` | `Value` (`string?`), `SelectedIndex` (`int`), `Multiple` (`bool`), `Size` (`int`), `IsOpen` (`bool`) | Requires `<option>`/`<optgroup>` children. |
| `<optgroup>` | `OptGroupElement` | `Label` (`string?`) | Groups options. |
| `<option>` | `OptionElement` | `Value` (`string?`), `Selected` (`bool`) | Value falls back to `TextContent`. |
| `<label>` | `LabelElement` | `For` (`string?`) | Associated control id. |

`InputType` enum: `Text`, `Password`, `Checkbox`, `Radio`, `Range`, `Search`.

- Range track/thumb/progress are styled via pseudo-elements `RangeThumb`/`RangeTrack`/`RangeProgress`.
- There is **no** `<textarea>` — use `<input Type="Text">` (or an Ionic component).
- There is **no** `<form>` — group inputs in a `<div>` and handle submission via a button `OnClick`.

Input example:

```razor
<input Type="Text" Placeholder="Your name" @bind="_name" />
<input Type="Checkbox" Checked="@_agree" @onchange="OnAgreeChanged" />
<input Type="Range" Min="0" Max="100" NumericValue="@_volume" />
```

Select example:

```razor
<select @bind="_choice">
    <option Value="a">Apple</option>
    <option Value="b">Banana</option>
</select>
```

## Media (replaced elements)

| Tag | Class | Properties (type) | Notes |
| --- | --- | --- | --- |
| `<img>` | `ImageElement` | `Source` (`MediaSource`), `Placeholder` (`string?`) | `Source` accepts a `string` (implicit convert). |
| `<video>` | `VideoElement` | `Source` (`MediaSource`), `AutoPlay` (`bool`), `Loop` (`bool`), `Muted` (`bool`), `Controls` (`bool`), `Poster` (`string?`) | Frame-composited; decode via platform `IVideoBackend`. |

`MediaSource` accepts schemes `file://`, `res://` (embedded resource), `http(s)://`, `data:`, or a bare path. `img.Source = "https://example.com/a.png"` just works.

```razor
<img src="res://Assets/logo.png" />
<img src="https://example.com/hero.jpg" Placeholder="res://Assets/blur.png" />
```

## Lists

| Tag | Class | Properties | Notes |
| --- | --- | --- | --- |
| `<ul>` | `UlElement` | — | Unordered list. |
| `<ol>` | `OlElement` | `Start` (`int`, default 1) | Ordered list. |
| `<li>` | `LiElement` | `Value` (`int?`) | List item. |

## Tables

All derive from `Element`. Structure mirrors HTML.

| Tag | Class | Properties |
| --- | --- | --- |
| `<table>` | `TableElement` | — |
| `<caption>` | `CaptionElement` | — |
| `<colgroup>` | `ColgroupElement` | `Span` (`int`, default 1) |
| `<col>` | `ColElement` | `Span` (`int`, default 1) |
| `<thead>` / `<tbody>` / `<tfoot>` | `TheadElement` / `TbodyElement` / `TfootElement` | — |
| `<tr>` | `TrElement` | — |
| `<th>` | `ThElement` | `ColSpan`/`RowSpan` (`int`), `Headers` (`string?`), `Scope` (`TableHeaderScope`) |
| `<td>` | `TdElement` | `ColSpan`/`RowSpan` (`int`), `Headers` (`string?`) |

`TableHeaderScope` enum: `Auto`, `Row`, `Col`, `RowGroup`, `ColGroup`.

```razor
<table>
    <thead><tr><th>Name</th><th>Qty</th></tr></thead>
    <tbody>
        <tr><td>Apples</td><td>3</td></tr>
        <tr><td>Pears</td><td>7</td></tr>
    </tbody>
</table>
```

---

## Missing elements — use substitutes

Miko has **no** element for the following. Use the substitute:

| Not available | Use instead |
| --- | --- |
| `<textarea>` | `<input Type="Text">` |
| `<form>` | `<div>` + a button `OnClick` |
| `<header>`, `<footer>`, `<section>`, `<article>`, `<main>`, `<aside>` | `<div>` (add a class for semantics) |
| `<em>`, `<b>`, `<i>`, `<small>` | `<span>` + `Style` (`FontWeight`, `FontStyle`, `FontSize`) |
| `<br>` | block layout / margins / a new `<div>` |
| `<hr>` | `<div>` with a border or background |
| `<iframe>`, `<canvas>`, `<svg>` | not supported |

For higher-level widgets (buttons with variants, cards, tabs, menus, chips, spinners), use the [Ionic components](../components/index.md) instead of styling raw elements.
