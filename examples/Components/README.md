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

## Tests

```sh
dotnet test tests/Miko.Components.Tests
```

See [implementation and verification notes](../../src/Miko.Components/Player/DESIGN.md).
