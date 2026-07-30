# MikoAppTransitions

Page navigation transition demo (ISSUE-108). The Miko core engine provides the
underlying support (keeping the old page tree alive, painting both pages as two
stacked layers, advancing the transition clock); the concrete effects are
implemented by the app in `Transitions.cs` via the `NavigationTransition`
abstract class — the same extension point a component library such as Miko.Ionic
uses for its platform transitions.

## Run

```bash
dotnet run
```

A window titled **MikoAppTransitions** opens on the home page (`/`). Tap an
effect to push (`Forward`) to its detail page with that transition; tap **返回**
to pop (`Back`) with the reversed transition, or **Root 返回** for an instant
root switch (tab-style, no transition, history cleared).

Navigation/transition lifecycle logs (started/completed/canceled) are printed to
the console (`Debug` level; set the minimum level to `Trace` in `App.cs` to see
per-frame dt/progress). An unattended auto demo is available via
`dotnet run -- --auto-demo` (cycles modal push/pop and fade every 2.5 s).

## Effects

| Key    | Forward                                    | Back                                       |
| ------ | ------------------------------------------ | ------------------------------------------ |
| `ios`  | New page slides in from the right, old page parallaxes −30 % | Old page slides out to the right on top |
| `slide`| Both pages slide left together             | Both pages slide right together            |
| `fade` | New page fades in over the old page        | Same (symmetric)                           |
| `modal`| New page slides up from the bottom         | Old page slides down off-screen            |
| `none` | Instant switch (no transition)             | Instant switch                             |

## Project layout

| File / folder            | Purpose                                                                 |
| ------------------------ | ----------------------------------------------------------------------- |
| `Program.cs`             | Entry point. Creates the app and starts the render loop.                |
| `App.cs`                 | App configuration (no default layout — each page is a full `IonPage`).  |
| `Transitions.cs`         | The transition effect implementations (`NavigationTransition` subclasses). |
| `Pages/HomePage.razor`   | Effect list; navigates `Forward` with the chosen transition.            |
| `Pages/DetailPage.razor` | One component, five routes; navigates `Back` / `Root`.                  |
| `GlobalStyles.cs`        | Global stylesheet (per-effect hero colors).                             |

## Dependencies

- `Miko`
- `Miko.DevTools`
- `Miko.Ionic`
- `Miko.Windowing`
- `Miko.Razor.Compiler`
