# Miko Razor App

An empty [Miko](https://github.com/chaldea/miko) desktop application that uses Razor components for its UI,
rendered with SkiaSharp.

## Layouts

The template accepts `--layout=blank`, `tabs`, `sidemenu`, `list`, or `full`; write it with `=`,
because `dotnet new` reads a separate `list` argument as its own `list` command. The `full`
layout includes the Ionic component showcase and its embedded demo assets.

## App icon

`app.ico` contains the Miko icon at 16–256 pixels and is embedded in the executable by
the project's `ApplicationIcon` property. Replace this file to brand your application.

## Run

```bash
dotnet run
```

A window titled **Miko Razor App** opens showing the home screen (`Pages/Home.razor`).

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
@namespace MikoRazorApp.Pages

<h1>About</h1>
```

Routes are discovered automatically by `Miko.Razor.Compiler` via `UseGeneratedRoutes()`.

## Dependencies

- `Miko`
- `Miko.DevTools`
- `Miko.Razor.Compiler`
