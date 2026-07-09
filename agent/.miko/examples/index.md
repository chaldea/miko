# Examples

Minimal, copy-pasteable Miko patterns. Each corresponds to a `dotnet new miko-razor --layout <x>` template.

| Example | Layout | Shows |
| --- | --- | --- |
| [Counter](counter.md) | `blank` | A routed page with state, an event handler, and a stylesheet. The default app. |
| [Tabs](tabs.md) | `tabs` | Ionic bottom tab bar with three routed tab pages sharing one layout. |
| [Side menu](sidemenu.md) | `sidemenu` | Ionic slide-in drawer over a page, with open/close state. |

Common wiring for all three lives in `App.cs` (`MikoAppBuilder`), `GlobalStyles.cs` (the `StyleSheet`), and `_Imports.razor` (`@using`). See [project.md](../project.md).

To add a page: drop a new `.razor` with `@page "/route"` into `Pages/`. It becomes a route automatically.
