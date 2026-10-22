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

- A working **OpenGL** driver (desktop uses OpenGL for rendering on Linux)
- glibc (the library is not compatible with musl-based distros like Alpine)

The templates include `SkiaSharp.NativeAssets.Linux` as a dependency of `Miko.Windowing`, so `libSkiaSharp.so` is included in the publish output automatically.

## Trimming and Native AOT

Miko 1.0.0-preview.1 does not officially support desktop trimming or Native AOT. The core engine and Ionic components are designed to be trimming-friendly, but:

- `Miko.DevTools` and `Miko.Simulator` reference Silk.NET, which has trimming warnings
- The templates do not enable `PublishTrimmed` or `PublishAot` for desktop by default

Trimming and AOT support for desktop is planned for a future release. iOS and Android use full AOT and are tested.

## DevTools in Release builds

The templates reference `Miko.DevTools` only in Debug configuration:

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <PackageReference Include="Miko.DevTools" Version="1.0.0-dev" />
</ItemGroup>
```

And the startup code wraps `AddDevTools()` in `#if DEBUG`:

```csharp
#if DEBUG
builder.AddDevTools();
#endif
```

This ensures DevTools and its native dependencies (Silk.NET, SDL2, DirectX interop) do not ship in Release builds.

### Simulator and MCP server

The `Miko.Simulator` host and `Miko.McpServer` are **development tools only**. The MCP server listens on `localhost:5800` without authentication and exposes DOM read, screenshot, and click simulation. **Never ship these packages in a distributed app.**

The templates include them only in the Simulator startup project, which is not intended for end-user distribution.

## What to distribute

For end users, distribute:

- The Desktop startup project published as shown above (framework-dependent or self-contained)
- **Do not** include the Simulator project or MCP server in distributed builds

Release builds of the Desktop head do not reference DevTools (via the `Condition` shown above), so they automatically exclude it and its dependencies.
