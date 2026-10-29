# Rules for building Miko UI

Miko renders Razor components natively with SkiaSharp. It looks like web development but it is **not** a browser: there is no HTML/CSS runtime, styles are C# objects, and only Miko's own elements, attributes and components exist. Check the reference before you write UI — do not assume web or Blazor behavior.

1. **Pages.** A `.razor` file under `Pages/` with `@page "/route"` is a route; routes are
   discovered at build time. Layouts inherit `LayoutComponentBase` and render `@Body`.
2. **Styles are typed C#.** Put rules in `GlobalStyles.cs` (`StyleSheet` + `CssObject` with
   selector-string keys) or bind a `Style` object: `style="@_box"`. CSS in `style="…"`,
   `<style>` and `.razor.css` is translated by the compiler, and what Miko cannot do is a build
   warning (RZ10102–RZ10109) — fix those. See [Styling](docs/guide/styling.md) and
   [CSS](docs/guide/css.md).
3. **Elements and attributes are a fixed set.** Attribute names are lowercase, and any attribute
   not listed in [Elements](docs/guide/elements.md) is dropped **silently** — e.g. `placeholder`
   on `<input>`, `disabled`, `<option value>`. `<select @bind>` does not show the bound value.
4. **Prefer Ionic components** for anything interactive or styled: buttons, inputs, toggles,
   lists, cards, tabs, menus, overlays. Start at [Ionic components](docs/components/ionic/index.md);
   each component page lists **every** parameter, event and slot — if it is not in the table, it
   does not exist. They need `builder.AddIonic()` and `@using Miko.Ionic.Components`.
5. **Follow the component structure.** `IonApp` › `IonPage` › `IonHeader` / `IonContent`; many
   children only work inside their parent — see
   [Where components go](docs/components/ionic/index.md#where-components-go). Toolbar buttons go
   in an `IonButtons` placed in the toolbar's `Start` / `End` slot.
6. **Named slots** are child elements (`<Start>`, `<End>`, …); once you use one, put the default
   content in `<ChildContent>`.
7. **Component events are parameters.** Use a component's own `EventCallback` parameters
   (`OnClick`, `OnChange`, `@bind-Value`, …). `@onclick` and other `@on…` directives work on
   elements only — on a component they compile but are ignored.
8. **Re-rendering.** A component re-renders after its own event handlers. When a child
   component's `EventCallback` changes state that a different component renders (e.g. a menu
   button opening an `IonMenu` declared in the page), call `StateHasChanged()` in that component.
   Do not use `@onscroll` / `@onpointermove` for per-frame work — see
   [Events](docs/guide/events.md). A parent's re-render keeps its child components (their fields
   and subscriptions survive, `OnInitialized` runs once); give the items of a list that can be
   reordered a `@key` — see [Razor Components](docs/guide/razor-components.md#component-identity-and-key).
9. **Verify.** Build with `dotnet build` and fix every error and warning in your `.razor` files.
   If the Miko MCP server is configured (multiplatform template, `--agent claude`), run the
   simulator and inspect the live DOM and styles through it.
