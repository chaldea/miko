# Miko Multiplatform App

An empty [Miko](https://github.com/chaldea/miko) cross-platform application that uses Razor
components for its UI, rendered with SkiaSharp on Desktop, Android and iOS.

## Project layout

| Project                         | Purpose                                                      |
| ------------------------------- | ------------------------------------------------------------ |
| `MikoApp2`          | Shared UI (Razor components, styles) and app configuration.  |
| `MikoApp2.Desktop`  | Desktop startup (Windows/Linux/macOS) — uses `Miko.Windowing`. |
| `MikoApp2.Android`  | Android startup — uses `Miko.Android`.                       |
| `MikoApp2.iOS`      | iOS startup — uses `Miko.iOS`.                               |

The shared project exposes `App.CreateContext()`, which returns a platform-neutral
`MikoAppContext`. Each startup project consumes that context and drives the render loop
for its platform.

## Run

Desktop:

```bash
dotnet run --project MikoApp2.Desktop
```

Android (device/emulator connected, `android` workload installed):

```bash
dotnet build MikoApp2.Android -t:Run
```

iOS (on macOS, `ios` workload installed):

```bash
dotnet build MikoApp2.iOS -t:Run
```

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/) or later
- For Android: `dotnet workload install android`
- For iOS: macOS with `dotnet workload install ios`

## Add a page

Create a new `.razor` file under `MikoApp2/Pages/` with a `@page` directive:

```razor
@page "/about"
@namespace MikoApp2.Pages

<h1>About</h1>
```

Routes are discovered automatically by `Miko.Razor.Compiler` via `UseGeneratedRoutes()`.
