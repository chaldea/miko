# Miko Multiplatform App

An empty [Miko](https://github.com/chaldea/miko) cross-platform application that uses Razor
components for its UI, rendered with SkiaSharp on Desktop, Android and iOS.

## Project layout

| Project                         | Purpose                                                      |
| ------------------------------- | ------------------------------------------------------------ |
| `MikoMultiplatformApp`          | Shared UI (Razor components, styles) and app configuration.  |
| `MikoMultiplatformApp.Desktop`  | Desktop startup (Windows/Linux/macOS) — uses `Miko.Windowing`. |
| `MikoMultiplatformApp.Simulator`| Device simulator (Windows/Linux/macOS) — uses `Miko.Simulator`. |
| `MikoMultiplatformApp.Android`  | Android startup — uses `Miko.Android`.                       |
| `MikoMultiplatformApp.iOS`      | iOS startup — uses `Miko.iOS`.                               |

The shared project exposes `App.CreateContext()`, which returns a platform-neutral
`MikoAppContext`. Each startup project consumes that context and drives the render loop
for its platform. `MikoMultiplatformApp.slnx` opens all five projects in Visual Studio, Rider or VS Code.

## App icon and launch screen

The default icon is the .NET bot from the .NET MAUI template. Replace the PNGs in place, keeping
their file names and pixel sizes:

- Android: `MikoMultiplatformApp.Android/Resources/mipmap-*/`. `appicon_foreground.png` and
  `appicon_background.png` form the adaptive icon (API 26+); `appicon.png` is the pre-rendered
  fallback. Android 12+ builds the launch splash from the icon.
- iOS: `MikoMultiplatformApp.iOS/Assets.xcassets/AppIcon.appiconset/` (sizes listed in
  `Contents.json`; `Icon1024.png` is the App Store icon). The launch screen is
  `Resources/LaunchScreen.storyboard`, a plain white view; App Store builds need one.

## Layouts

The template accepts `--layout blank`, `tabs`, `sidemenu`, `list`, or `full`. The `full`
layout includes the Ionic component showcase and its embedded demo assets.

## Application ID

The Android `ApplicationId` and the iOS `ApplicationId`/`CFBundleIdentifier` are both
`com.miko.multiplatformapp`. Pass `--appId com.example.myapp` to `dotnet new` to choose your own; to change
it later, edit all three places. Store submissions need an ID under a domain you own.

## Run

Desktop:

```bash
dotnet run --project MikoMultiplatformApp.Desktop
```

Device simulator (preview app with device chrome and settings panel):

```bash
dotnet run --project MikoMultiplatformApp.Simulator
```

Android (device/emulator connected, `android` workload installed):

```bash
dotnet build MikoMultiplatformApp.Android -t:Run
```

iOS (on macOS, `ios` workload installed):

```bash
dotnet build MikoMultiplatformApp.iOS -t:Run
```

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/) or later
- For Android: `dotnet workload install android`
- For iOS: macOS with `dotnet workload install ios`

## Add a page

Create a new `.razor` file under `MikoMultiplatformApp/Pages/` with a `@page` directive:

```razor
@page "/about"
@namespace MikoMultiplatformApp.Pages

<h1>About</h1>
```

Routes are discovered automatically by `Miko.Razor.Compiler` via `UseGeneratedRoutes()`.
