# Miko Multiplatform App

An empty [Miko](https://github.com/chaldea/miko) cross-platform application that uses Razor
components for its UI, rendered with SkiaSharp on Desktop, Android and iOS.

## Project layout

| Project                         | Purpose                                                      |
| ------------------------------- | ------------------------------------------------------------ |
| `MikoApp3`          | Shared UI (Razor components, styles) and app configuration.  |
| `MikoApp3.Desktop`  | Desktop startup (Windows/Linux/macOS) — uses `Miko.Windowing`. |
| `MikoApp3.Android`  | Android startup — uses `Miko.Android`.                       |
| `MikoApp3.iOS`      | iOS startup — uses `Miko.iOS`.                               |

The shared project exposes `App.CreateContext()`, which returns a platform-neutral
`MikoAppContext`. Each startup project consumes that context and drives the render loop
for its platform.

## Run

Desktop:

```bash
dotnet run --project MikoApp3.Desktop
```

Android (device/emulator connected, `android` workload installed):

```bash
dotnet build MikoApp3.Android -t:Run
```

iOS (on macOS, `ios` workload installed):

```bash
dotnet build MikoApp3.iOS -t:Run
```

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/) or later
- For Android: `dotnet workload install android`
- For iOS: macOS with `dotnet workload install ios`

## Add a page

Create a new `.razor` file under `MikoApp3/Pages/` with a `@page` directive:

```razor
@page "/about"
@namespace MikoApp3.Pages

<h1>About</h1>
```

Routes are discovered automatically by `Miko.Razor.Compiler` via `UseGeneratedRoutes()`.
