# Elements

The tags you write in `.razor` markup — `<div>`, `<p>`, `<input>`, … — are compiled into Miko's
own element classes (`DivElement`, `ParagraphElement`, `InputElement`, …). Miko has a fixed set
of elements and, per element, a fixed set of attributes. **An attribute that is not listed below
has no effect**; the compiler reports it with warning RZ10100 (`aria-*`, `data-*` and `role` are
ignored without a warning). A tag that is not listed is reported with warning RZ10101, because
rendering it throws `Unknown tag`.

::: tip For anything richer, use components
Buttons with variants, text fields with labels, toggles, lists, cards, tabs and menus are
[Ionic components](../components/ionic/index.md). Prefer them over styling raw elements.
:::

## Attribute rules

- Attribute names are **lowercase HTML names**: `type="checkbox"`, `src="…"`. `Type="Checkbox"`
  is a different (unknown) attribute and is ignored.
- Only the attributes in the tables below are applied. Values may be literals or expressions of
  any type (`<option value="@item.Id">`, `colspan="@n"`); numbers are formatted with the invariant
  culture.
- `disabled` puts the element (and its descendants) in the disabled state, which matches
  `:disabled` and stops it from receiving events. `disabled="@false"` leaves it enabled.
- `<select value>` (and `<select @bind>`) selects the option with that `value` once the options are
  built; `<option selected>` marks the initial choice when no value is given.
- To reach an element from C#, capture it with `@ref`: the field can be the element class itself
  (`CanvasElement? _canvas;`), `Element`, or `ElementReference`. A `<canvas>` is painted by
  assigning its `Paint` callback this way.
- `style` takes a `Style` **object** (`style="@_boxStyle"`), not a CSS string. See
  [Styling](./styling.md#inline-styles).
- Whitespace between tags is insignificant; `<pre>` preserves it verbatim (see
  [Razor Components](./razor-components.md#preformatted-text-code-blocks)).

## Reference

<!-- miko:elements -->
Every element accepts these attributes:

| Attribute | Sets |
| --- | --- |
| `class` | `Class` |
| `id` | `Id` |
| `style` | `Style` |
| `disabled` | `Disabled` |

Event attributes (`@onclick="Handler"` etc.): `@onclick`, `@onmouseenter`, `@onmouseleave`, `@onmousedown`, `@onmouseup`, `@onmousemove`, `@onpointerdown`, `@onpointerup`, `@onpointermove`, `@onpointercancel`, `@onlongpress`, `@onfocus`, `@onblur`, `@onchange`, `@onscroll`, `@onkeydown`, `@oninput`.

| Tag | Element class | Extra attributes | Binding |
| --- | --- | --- | --- |
| `<a>` | `AnchorElement` | `href`, `target`, `rel` | — |
| `<div>` | `DivElement` | — | — |
| `<span>` | `SpanElement` | — | — |
| `<p>` | `ParagraphElement` | — | — |
| `<button>` | `ButtonElement` | — | — |
| `<input>` | `InputElement` | `type`, `checked`, `value`, `placeholder`, `min`, `max` | `@bind` → `checked` (checkbox); `value` (text, password, search, range) |
| `<textarea>` | `TextAreaElement` | `placeholder`, `value`, `rows`, `cols` | `@bind` → `value` |
| `<select>` | `SelectElement` | `value`, `multiple` | `@bind` → `value` |
| `<option>` | `OptionElement` | `value`, `selected` | — |
| `<optgroup>` | `OptGroupElement` | `label` | — |
| `<label>` | `LabelElement` | `for` | — |
| `<br>` | `BrElement` | — | — |
| `<hr>` | `HrElement` | — | — |
| `<h1>` | `H1Element` | — | — |
| `<h2>` | `H2Element` | — | — |
| `<h3>` | `H3Element` | — | — |
| `<h4>` | `H4Element` | — | — |
| `<h5>` | `H5Element` | — | — |
| `<h6>` | `H6Element` | — | — |
| `<ul>` | `UlElement` | — | — |
| `<ol>` | `OlElement` | `start` | — |
| `<li>` | `LiElement` | `value` | — |
| `<img>` | `ImageElement` | `src`, `placeholder` | — |
| `<video>` | `VideoElement` | `src`, `poster`, `autoplay`, `loop`, `muted`, `controls` | — |
| `<table>` | `TableElement` | — | — |
| `<caption>` | `CaptionElement` | — | — |
| `<colgroup>` | `ColgroupElement` | `span` | — |
| `<col>` | `ColElement` | `span` | — |
| `<thead>` | `TheadElement` | — | — |
| `<tbody>` | `TbodyElement` | — | — |
| `<tfoot>` | `TfootElement` | — | — |
| `<tr>` | `TrElement` | — | — |
| `<th>` | `ThElement` | `colspan`, `rowspan` | — |
| `<td>` | `TdElement` | `colspan`, `rowspan` | — |
| `<nav>` | `NavElement` | — | — |
| `<strong>` | `StrongElement` | — | — |
| `<b>` | `BElement` | — | — |
| `<pre>` | `PreElement` | — | — |
| `<code>` | `CodeElement` | `language`, `highlight` | — |
| `<canvas>` | `CanvasElement` | — | — |
<!-- /miko:elements -->

`<input type>` accepts `text` (default), `password`, `checkbox`, `radio`, `range`, `search`, and
`number` / `tel` (both edit as text and ask the platform for a numeric keypad). Any other value
falls back to `text`.

```razor
<input type="checkbox" @bind="_agree" />
<input @bind="_name" />
<textarea placeholder="Notes" rows="4" @bind="_notes"></textarea>
<img src="res://Assets/logo.png" placeholder="res://Assets/blur.png" />

@code {
    private bool _agree;
    private string _name = "";
    private string _notes = "";
}
```

## Images and video

`src` accepts `file://`, `res://` (an embedded resource), `http(s)://`, `data:`, or a bare path.
`res://` paths mirror `file://`: **no assembly name**, `/` separators, relative to the project
root — `res://Assets/logo.png` for `Assets/logo.png` embedded (`<EmbeddedResource>`) in any
assembly registered with `builder.AddResourceAssembly(...)`. Renaming the assembly or moving the
project never rewrites resource paths.

`placeholder` on `<img>` is shown until the image has loaded. `<video>` decodes through the
platform's system decoder; see [Packages](../packages.md) for the platform hosts.

## Code blocks

`<code>` takes `language` (`csharp`, `javascript`, `typescript`, `json`, `xml`, `css`, `sql`,
`python`, `bash`, and aliases such as `c#`, `js`, `py`, `sh`) to enable syntax highlighting, and
`highlight="false"` to turn it off. Put it inside `<pre>` to keep line breaks — see
[Razor Components](./razor-components.md#preformatted-text-code-blocks).

## HTML elements Miko does not have

| Not available | Use instead |
| --- | --- |
| `<form>` | A `<div>` and a button's `@onclick` handler. |
| `<header>`, `<footer>`, `<section>`, `<article>`, `<main>`, `<aside>` | `<div>` with a class; in Ionic apps `IonHeader` / `IonFooter` / `IonContent`. |
| `<em>`, `<i>`, `<small>`, `<u>` | `<span>` with a `Style` (`FontStyle`, `FontSize`, `TextDecoration`). `<strong>` and `<b>` exist. |
| `<h1>`… inside text runs, `<sup>`, `<sub>` | `<span>` with a `Style`. |
| `<svg>` | An `<img>` whose `src` is an `.svg` file, or `IonIcon`. |
| `<iframe>` | Not available. |
| Any other tag | Reported at compile time (RZ10101); rendering it throws `Unknown tag`. |
