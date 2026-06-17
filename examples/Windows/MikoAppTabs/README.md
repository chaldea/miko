# MikoAppTabs

An empty [Miko](https://github.com/chaldea/miko) desktop application that uses Razor components for its UI,
rendered with SkiaSharp.

## Run

```bash
dotnet run
```

A window titled **MikoAppTabs** opens showing the home screen (`Pages/Home.razor`).

## Project layout

| File / folder            | Purpose                                                            |
| ------------------------ | ----------------------------------------------------------------- |
| `Program.cs`             | Entry point. Creates the app and starts the render loop.          |
| `App.cs`                 | App configuration (title, size, dev tools, routing, hot reload).  |
| `MainLayout.razor`       | The default layout that wraps every page.                         |
| `Pages/Home.razor`       | The home page, mapped to the `/` route.                           |
| `GlobalStyles.cs`        | Global stylesheet (CSS-like styles defined in C#).                |
| `_Imports.razor`         | Common `@using` directives shared by all `.razor` files.          |

## Add a page

Create a new `.razor` file under `Pages/` with a `@page` directive:

```razor
@page "/about"
@namespace MikoAppTabs.Pages

<h1>About</h1>
```

Routes are discovered automatically by `Miko.Razor.Compiler` via `UseGeneratedRoutes()`.

## Dependencies

- `Miko`
- `Miko.DevTools`
- `Miko.Razor.Compiler`
