# Browser Apps (WebAssembly)

`Miko.Browser` runs the same Razor component tree and Skia renderer in a browser. It does **not**
turn Miko elements into HTML: the .NET runtime and SkiaSharp run as WebAssembly, render into an RGBA
surface, and present that surface to a Canvas 2D context. The page contains no HTML/CSS UI runtime.

## Requirements

- .NET 11.0 SDK or later
- The WebAssembly native build tools:

  ```bash
  dotnet workload install wasm-tools
  ```

The workload is required to link SkiaSharp and HarfBuzz into the WebAssembly runtime. The generated
multiplatform app includes a Browser head; set `WasmBuildNative` to `true` in a hand-written browser
startup project as well.

## Run the development server

```bash
dotnet new miko-multiplatform -o MyApp
cd MyApp
dotnet run --project MyApp.Browser
```

The `Microsoft.NET.Sdk.WebAssembly` project starts its development server and opens the default
browser. `Properties/launchSettings.json` controls the URL and whether the browser opens. Use
`dotnet watch run --project MyApp.Browser` for the SDK's WebAssembly hot-reload workflow; the shared
Miko app initializes its generated hot-reload handler just as the desktop head does.

For the repository's complete Ionic showcase:

```bash
dotnet run --project examples/Ionic/IonicDemo.Browser
```

## Browser entry point

A startup project uses `Microsoft.NET.Sdk.WebAssembly`, references `Miko.Browser`, and starts the
shared context with `RunBrowser`:

```csharp
using System.Runtime.Versioning;
using Miko.Browser;
using Miko.Browser.Native;

[SupportedOSPlatform("browser")]
public static class Program
{
    public static void Main()
    {
        var context = App.CreateContext(builder => builder.UseBrowserNative());
        App.InitializeHotReload(context);
        context.RunBrowser();
    }
}
```

`wwwroot/index.html` supplies the canvas and loads the platform module:

```html
<canvas id="miko-app" aria-label="My Miko application"></canvas>
<script type="module" src="_content/Miko.Browser/miko-browser.js"></script>
```

Give the canvas a CSS size (the template uses `100vw × 100dvh`). Miko lays out in CSS pixels and
allocates the Skia surface at `devicePixelRatio`, so high-DPI output remains sharp. A different canvas
ID can be passed to `RunBrowser("my-canvas-id")`.

When the app references Razor class libraries, the output contains more than one static-web-assets
manifest. Identify the application's own manifest by adding `runtimeconfig.template.json` next to the
startup `.csproj` (already present in generated heads):

```json
{
  "wasmHostProperties": {
    "mainAssembly": "MyApp.Browser.dll",
    "perHostConfig": [ { "name": "browser", "host": "browser" } ]
  }
}
```

Without `mainAssembly`, the development server can select a library's manifest and return 404 for
`index.html` and `_framework` assets.

## Publish as a static application

```bash
dotnet publish MyApp.Browser -c Release
```

Publish `bin/Release/net11.0/publish/wwwroot` to an HTTP(S) static host. Do not open `index.html`
through `file://`: WebAssembly and module loading require a web server with the normal `.wasm` MIME
type. No ASP.NET Core server is needed in production.

Routes are synchronized with URL hashes (`#/about`), so deep links work without server-side route
fallback and browser Back/Forward is connected to Miko navigation.

## Input and lifecycle

The host forwards pointer, wheel, keyboard, resize, visibility, and text-composition events to the
existing interaction controller. A hidden native input receives mobile keyboards and IME composition;
CSS `env(safe-area-inset-*)` values are forwarded to Miko's safe-area model. `visualViewport` supplies
the keyboard inset only while an editor is focused on a touch device at 1× scale, so pinch zoom is not
mistaken for a keyboard. Frames are produced on demand: idle applications do not copy or present pixels,
while animations and posted asynchronous work request another frame.

The initial browser presenter uses a CPU Skia surface and Canvas 2D, not WebGL. A painted frame is
copied to the canvas as unpremultiplied RGBA pixels; there is no PNG encode/decode step.

## Native capabilities

Call `UseBrowserNative()` in the Browser head. It registers only APIs the browser can provide: app
lifecycle and language, external browser windows, clipboard, device information and language,
geolocation, network status, vibration, keyboard visibility, and speech synthesis. Some members are
necessarily partial — for example the web has no screen-reader detection API, and browsers cannot
minimize or exit the application. Unsupported members throw `PlatformNotSupportedException`.

Clipboard, geolocation, vibration, and popup behavior also depend on browser policy. Use HTTPS (or
`localhost`), call permission-gated APIs from a user gesture, handle denial, and observe the target
site's CORS rules for `HttpClient` and remote images. See [Native Capabilities](./native.md).
