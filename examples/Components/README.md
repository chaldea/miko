# Player Examples

The `MikoPlayer` demo page is shared; each platform head registers its system video backend and
native services, bundles the local sample clip and drives the render loop.

| Project | Contents |
| --- | --- |
| `MikoApp.Player` | Shared page (`PlayerDemo.razor`) and configuration (`App.CreateContext`): source input, Local / HLS buttons, the player with danmaku. |
| `MikoApp.Player.Desktop` | Windows / macOS / Linux window: `UseSystemVideo()`, `UseDesktopNative()`; clip copied next to the app. |
| `MikoApp.Player.Android` | `UseAndroidVideo()`, `UseAndroidNative()`; clip in the APK assets, extracted to app storage on start. |
| `MikoApp.Player.iOS` | `UseIosVideo()`, `UseIosNative()`; clip in the app bundle. |

The native services make fullscreen lock landscape, keep the screen on and hide the system bars, and
give the long press its haptic tick. The HLS button plays a public stream and needs internet access;
the source input accepts other files and HTTP(S) URLs.

## Desktop

```sh
dotnet run --project examples/Components/MikoApp.Player.Desktop
```

Headless verification with the real system decoder, screenshots written to the OS temporary directory:

```sh
dotnet run --project examples/Components/MikoApp.Player.Desktop -- --verify
dotnet run --project examples/Components/MikoApp.Player.Desktop -- --verify https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8
```

On macOS the headless run cannot get frames from AVFoundation (the item never becomes ready without
a window); run the app instead.

## Android

```sh
dotnet build examples/Components/MikoApp.Player.Android -t:Install -p:RuntimeIdentifier=android-arm64
adb shell am start -n com.miko.playerdemo/crc64c5c858f2e00d6bb8.MainActivity
```

Use `android-x64` for an x86_64 emulator. A launch intent's `source` extra opens another URI first
(`adb shell am start … --es source https://…`).

## iOS

Simulator (Apple silicon):

```sh
dotnet build examples/Components/MikoApp.Player.iOS -r iossimulator-arm64
xcrun simctl install booted examples/Components/MikoApp.Player.iOS/bin/Debug/net10.0-ios/iossimulator-arm64/MikoApp.Player.iOS.app
xcrun simctl launch booted com.miko.playerdemo
```

If the build reports that the iOS workload requires a different Xcode version, add
`-p:ValidateXcodeVersion=false`. A device build needs your signing settings (`-r ios-arm64`).

# Camera Examples

The camera demo page is shared too; each head registers its camera backend and native services.

| Project | Contents |
| --- | --- |
| `MikoApp.Camera` | Shared pages and configuration (`App.CreateContext`): the home page (`CameraDemo.razor`) opens `MikoBarcodeScanner` (QR only or every format), `MikoCamera`, and a diagnostics page (`CameraElementPage.razor`: preview, flip, torch, capture, restart, analysis rate). Scan and photo results show on the home page. |
| `MikoApp.Camera.Android` | `UseAndroidCamera()` (Camera2), `UseAndroidNative()`; asks for the `CAMERA` permission on first use. |
| `MikoApp.Camera.iOS` | `UseIosCamera()` (AVFoundation), `UseIosNative()`; `NSCameraUsageDescription` in `Info.plist`. |

There is no desktop head yet: the desktop camera backend is not implemented.

## Android

```sh
dotnet build examples/Components/MikoApp.Camera.Android
adb install -r examples/Components/MikoApp.Camera.Android/bin/Debug/net10.0-android/com.miko.camerademo-Signed.apk
adb shell am start -n com.miko.camerademo/crc649cf130a0106f6c98.MainActivity
```

The emulator's virtual cameras work for the preview and for photos; scanning needs a code in front of
a real camera.

## iOS

The simulator has no camera: the pages open and report "No camera available". Use a device for the
camera itself (`-r ios-arm64` with your signing settings).

```sh
dotnet build examples/Components/MikoApp.Camera.iOS -r iossimulator-arm64 -p:ValidateXcodeVersion=false
xcrun simctl install booted examples/Components/MikoApp.Camera.iOS/bin/Debug/net10.0-ios/iossimulator-arm64/MikoApp.Camera.iOS.app
xcrun simctl launch booted com.miko.camerademo
```

# Chart Examples

`MikoApp.Charts` is the `Miko.Charts` gallery (`ChartsDemo.razor`): a live line chart (timer, add and
remove points, pan by dragging, zoom buttons), columns and a line with a legend and a click handler,
doughnut, gauge, stacked area, scatter, candlesticks, radar and a world heat map. `App.CreateContext`
registers the charts with `AddMikoCharts()`.

| Project | Contents |
| --- | --- |
| `MikoApp.Charts` | Shared page and configuration. |
| `MikoApp.Charts.Desktop` | Windows / macOS / Linux window. |

```sh
dotnet run --project examples/Components/MikoApp.Charts.Desktop
```

## Tests

```sh
dotnet test tests/Miko.Components.Tests
dotnet test tests/Miko.Charts.Tests
```

See [implementation and verification notes](../../src/Miko.Components/Player/DESIGN.md).
