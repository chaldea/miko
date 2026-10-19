# AGENTS.md

Guidance for AI coding agents when working in this app.

## What this app is

This is a **Miko** app. Miko is a native .NET UI engine: you author UI as **Razor components** (`.razor`) and Miko draws every pixel with **SkiaSharp**. There is **no browser, no WebView, and no HTML/CSS runtime**. Its elements, styling model, and components differ from the web — do not assume web behavior.

## Read the Miko reference before building UI

The Miko documentation for this app's Miko version is in `.miko/`. **Start with [`.miko/llms.txt`](.miko/llms.txt)**: it indexes every guide and every Ionic component page. Component pages list each parameter, event, and slot exactly — use them instead of guessing from Ionic or Blazor knowledge.

## Rules

<!-- miko:include .miko/rules.md -->
Miko renders Razor components natively with SkiaSharp. It looks like web development but it is **not** a browser: there is no HTML/CSS runtime, styles are C# objects, and only Miko's own elements, attributes and components exist. Check the reference before you write UI — do not assume web or Blazor behavior.

1. **Pages.** A `.razor` file under `Pages/` with `@page "/route"` is a route; routes are
   discovered at build time. Layouts inherit `LayoutComponentBase` and render `@Body`.
2. **Styles are typed C#.** Put rules in `GlobalStyles.cs` (`StyleSheet` + `CssObject` with
   selector-string keys) or bind a `Style` object: `style="@_box"`. There is no CSS text:
   `style="color:red"` does nothing. See [Styling](.miko/docs/guide/styling.md).
3. **Elements and attributes are a fixed set.** Attribute names are lowercase, and any attribute
   not listed in [Elements](.miko/docs/guide/elements.md) is dropped **silently** — e.g. `placeholder`
   on `<input>`, `disabled`, `<option value>`. `<select @bind>` does not show the bound value.
4. **Prefer Ionic components** for anything interactive or styled: buttons, inputs, toggles,
   lists, cards, tabs, menus, overlays. Start at [Ionic components](.miko/docs/components/ionic/index.md);
   each component page lists **every** parameter, event and slot — if it is not in the table, it
   does not exist. They need `builder.AddIonic()` and `@using Miko.Ionic.Components`.
5. **Follow the component structure.** `IonApp` › `IonPage` › `IonHeader` / `IonContent`; many
   children only work inside their parent — see
   [Where components go](.miko/docs/components/ionic/index.md#where-components-go). Toolbar buttons go
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
   [Events](.miko/docs/guide/events.md).
9. **Verify.** Build with `dotnet build` and fix every error and warning in your `.razor` files.
   If the Miko MCP server is configured (multiplatform template, `--agent claude`), run the
   simulator and inspect the live DOM and styles through it.
<!-- /miko:include -->

## Project layout

- `App.cs` — `MikoAppBuilder` configuration (title, size, routes, layout, `AddIonic()`).
- `Program.cs` — entry point; builds the context and runs the host.
- `Pages/` — routed pages. A `.razor` with `@page "/route"` is a route (auto-discovered).
- `MainLayout.razor` — default layout wrapping every page (`@Body`).
- `GlobalStyles.cs` — the app `StyleSheet`, built in C# with `CssObject`.
- `_Imports.razor` — shared `@using` directives.

In the multiplatform template these live in the shared `<AppName>/` project; `<AppName>.Desktop`,
`.Android`, `.iOS` and `.Simulator` are thin platform hosts.

## Build & run

```bash
dotnet build
dotnet run                                  # single-platform template
dotnet run --project <AppName>.Desktop      # multiplatform template
dotnet run --project <AppName>.Simulator    # multiplatform: device preview
```
