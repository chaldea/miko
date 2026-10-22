# Publishing Desktop Apps

This guide covers publishing Miko apps for **Windows, macOS, and Linux** using the `Miko.Windowing` desktop host.

## Runtime vs self-contained

Miko desktop apps can be published in two modes:

- **Framework-dependent** — requires the .NET 10 runtime to be installed on the target machine. Produces a small binary (~few MB).
- **Self-contained** — bundles the .NET runtime with your app. No runtime installation required, but the binary is larger (~60–80 MB depending on platform and trimming).

### Framework-dependent publish

```bash
dotnet publish MyApp.Desktop.csproj -c Release -r win-x64
dotnet publish MyApp.Desktop.csproj -c Release -r osx-arm64
dotnet publish MyApp.Desktop.csproj -c Release -r linux-x64
```

The output is in `bin/Release/net10.0/<RID>/publish/`. Distribute the entire folder. Users need the [.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0) installed.

### Self-contained publish

```bash
dotnet publish MyApp.Desktop.csproj -c Release -r win-x64 --self-contained
dotnet publish MyApp.Desktop.csproj -c Release -r osx-arm64 --self-contained
dotnet publish MyApp.Desktop.csproj -c Release -r linux-x64 --self-contained
```

The output includes the .NET runtime. No installation required on the target machine.

## Platform-specific notes

### Windows

The output is a `.exe` file plus supporting DLLs. Distribute the entire `publish/` folder or package it as a zip / installer.

### macOS

The output is a native Mach-O executable. For a proper `.app` bundle:

1. Create `MyApp.app/Contents/MacOS/` and `MyApp.app/Contents/Resources/`
2. Copy the published executable and DLLs into `MacOS/`
3. Add an `Info.plist` in `Contents/` with `CFBundleExecutable` set to your executable name

For distribution outside the App Store, you must sign and notarize the app with a Developer ID certificate.

### Linux

The output is a native ELF executable. **Important:** the `SkiaSharp.NativeAssets.Linux` package dependency ships `libSkiaSharp.so`. The target system must have:

- A working **OpenGL** driver (the desktop host renders with OpenGL on Windows and Linux, and with Metal on macOS)
- glibc (the library is not compatible with musl-based distros like Alpine)

`Miko.Windowing` depends on `SkiaSharp.NativeAssets.Linux`, so `libSkiaSharp.so` is included in the `linux-x64` publish output automatically.

## Trimming and Native AOT

Miko 1.0.0-preview.1 does not support trimming or Native AOT for desktop apps, and the templates do not enable `PublishTrimmed` or `PublishAot`. The desktop host (Silk.NET, Vortice) and some dependencies (Svg.Skia, ExCSS) still produce trim warnings. iOS and Android builds are trimmed and AOT-compiled by their SDKs.

## DevTools in Release builds

The templates reference `Miko.DevTools` only in the Debug configuration:

```xml
<PackageReference Include="Miko.DevTools" Version="1.0.0-preview.1" Condition="'$(Configuration)' == 'Debug'" />
```

And the startup code wraps `AddDevTools()` in `#if DEBUG`:

```csharp
#if DEBUG
builder.AddDevTools();
#endif
```

Release builds therefore contain neither `Miko.DevTools` nor its F12 inspector. (Silk.NET and SDL2 are still in the output: they are the desktop host itself, `Miko.Windowing`.)

The template code uses `//-:cnd:noEmit` markers around `#if DEBUG` so that `dotnet new` keeps the directive; in your own app a plain `#if DEBUG` is enough.

### Simulator and MCP server

The `Miko.Simulator` host and `Miko.McpServer` are **development tools only**. The MCP server listens on `localhost:5800` without authentication and exposes DOM read, screenshot, and click simulation. **Never ship these packages in a distributed app.**

The templates reference them only from the Simulator head project, which is not meant for distribution.

## What to distribute

For end users, distribute:

- The Desktop startup project published as shown above (framework-dependent or self-contained)
- **Do not** include the Simulator project or MCP server in distributed builds

Release builds of the Desktop head do not reference DevTools (the `Condition` shown above), so nothing has to be removed by hand.
