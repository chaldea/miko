# Player Examples

Desktop:

```sh
dotnet run --project examples/Components/MikoApp.Player
```

The example includes the repository's local MP4 and a public HLS stream. A source input accepts other files and HTTP(S) URLs. Internet access is required for the HLS sample.

Headless verification using the real system decoder, with screenshots written to the OS temporary directory:

```sh
dotnet run --project examples/Components/MikoApp.Player -- --verify
dotnet run --project examples/Components/MikoApp.Player -- --verify https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8
```

Android x64 emulator:

```sh
dotnet build examples/Components/MikoApp.Player.Android -p:RuntimeIdentifier=android-x64
adb install --no-incremental -r examples/Components/MikoApp.Player.Android/bin/Debug/net10.0-android/android-x64/com.miko.playerdemo-Signed.apk
adb shell monkey -p com.miko.playerdemo 1
```

Use `android-arm64` for an ARM device. The APK embeds its managed assemblies and copies the bundled MP4 into app-private storage. The sample includes Local/HLS source buttons. A launch intent's `source` extra can supply another URI.

Tests:

```sh
dotnet test tests/Miko.Components.Tests
dotnet test miko.slnx
```

Verified on Windows and an Android API 36 x64 emulator with both the local MP4 and the public HLS stream. The Android controls, seeking, settings and viewport fullscreen were checked with ADB screenshots. Linux and Apple backends were compiled but not run. See [implementation and verification notes](../../src/Miko.Components/Player/DESIGN.md).
